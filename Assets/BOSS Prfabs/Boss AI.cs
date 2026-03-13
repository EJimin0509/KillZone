using UnityEngine;
using System.Collections;
using UnityEngine.AI; // NavMesh 기능을 사용하기 위한 네임스페이스

/// <summary>
/// 보스 AI 공용 스크립트
/// 이동(NavMesh), 다단계 공격 패턴(약/강), 광폭화, 사망 시 물리 제거 로직을 포함합니다.
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("--- 보스 설정 데이터 ---")]
    [SerializeField] private EnemyData data;          // 보스 기본 스탯 (HP, 공격력, 사거리 등)
    [SerializeField] private GameObject enemyArrowPrefab; // 원거리 보스용 발사체 프리팹

    [Header("--- 참조 컴포넌트 ---")]
    private SpriteRenderer _spriteRenderer;
    private NavMeshAgent _agent;
    private NavMeshObstacle _obstacle;
    private Animator _animator;
    private Collider2D _collider; // 사망 시 길막 방지를 위해 즉시 꺼야 하는 충돌체

    [Header("--- 피격 비주얼 ---")]
    [SerializeField] private Transform visualChild;   // 피격 시 이미지(자식)만 흔들기 위한 트랜스폼
    [SerializeField] private float knockbackDistance = 0.3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("--- 실시간 상태 변수 ---")]
    public float CurrentHp;
    private float _lastAttackTime;                    // 공격 쿨타임 계산용 시점
    private bool _isKnockbacking = false;             // 피격 연출 중 행동 제어 플래그
    private GameObject _currentTarget;                // 현재 추적 및 공격 대상

    private float _pathUpdateInterval = 1.0f;         // NavMesh 경로 갱신 최적화 주기
    private float _nextUpdateTime;
    private LayerMask _unitLayer;                     // 유닛 탐색용 레이어

    private int _attackPatternIndex = 0;              // 공격 패턴 순서 카운터 (3타 주기 계산용)
    private bool _isAttacking = false;                // 공격 애니메이션 중 이동 간섭 방지 플래그
    private bool _isFrenzy = false;                   // 광폭화 상태 확인
    private bool _isDead = false;                     // 사망 상태 확인
    private float _currentDefenseModifier = 1f;       // 특수 상황(기 모으기 등)에서의 방어력 배수

    private void Awake()
    {
        // 컴포넌트 자동 할당
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _agent = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();
        _collider = GetComponent<Collider2D>();

        // NavMesh 초기 설정: 이동 시 Agent 사용, 정지 시 Obstacle 사용
        if (_obstacle != null) { _obstacle.carving = true; _obstacle.enabled = false; }
        if (_agent != null) { _agent.updateRotation = false; _agent.updateUpAxis = false; _agent.enabled = true; }

        if (data != null)
        {
            CurrentHp = data.maxHp;
            if (_agent != null) _agent.speed = data.moveSpeed; // 소환 시 즉시 기본 속도 적용
        }
        _unitLayer = LayerMask.GetMask("Unit");
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    private void OnEnable()
    {
        // 오브젝트 풀(Object Pool)에서 재활성화될 때 초기화
        if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        if (_collider != null) _collider.enabled = true;
        if (_agent != null) _agent.enabled = true;

        _isDead = false;
        _isAttacking = false;
        _attackPatternIndex = 0;
        if (data != null) CurrentHp = data.maxHp;
    }

    private void Update()
    {
        if (_isDead) return;
        if (CurrentHp <= 0) { StartCoroutine(DieRoutine()); return; }

        // 피격 중이거나 공격 중일 때는 이동 및 타겟팅 로직을 수행하지 않음
        if (_isKnockbacking || _isAttacking) return;

        CheckFrenzy();      // 체력 30% 이하 시 광폭화 체크
        HandleTargeting();  // 주변 타겟 탐색

        if (_currentTarget != null)
        {
            if (!IsTargetAlive(_currentTarget))
            {
                _currentTarget = null;
                _animator.SetBool("isWalking", false);
                return;
            }

            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            // 거리 체크: 사거리 이내면 공격 모드, 멀면 이동 모드
            if (dist <= data.attackRange + 0.5f) StopAndAttack();
            else MoveToTarget();
        }
        else
        {
            // 타겟이 없을 때 정지 애니메이션 및 Agent 정지
            _animator.SetBool("isWalking", false);
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
        }
    }

    /// <summary>
    /// 공격을 위해 이동을 멈추고 장애물(Obstacle) 모드를 활성화합니다.
    /// </summary>
    private void StopAndAttack()
    {
        _animator.SetBool("isWalking", false);
        if (_agent.enabled)
        {
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.velocity = Vector3.zero;
                _agent.isStopped = true;
            }
            _agent.enabled = false;
            if (_obstacle != null) _obstacle.enabled = true; // 다른 유닛이 밀지 못하게 장애물 설정
        }

        // 공격 쿨타임 체크 후 공격 패턴 실행
        if (Time.time >= _lastAttackTime + (1f / data.attackSpeed))
        {
            StartCoroutine(AttackPatternRoutine());
        }
    }

    /// <summary>
    /// [핵심 공격 패턴] 
    /// 일반 상태: 약 - 약 - 강(0.8초 기 모으기)
    /// 광폭 상태: 약 - 강(즉시 발동) - 강(즉시 발동)
    /// </summary>
    private IEnumerator AttackPatternRoutine()
    {
        _isAttacking = true; // 이동 로직의 간섭 방지 플래그 ON

        int step = _attackPatternIndex % 3; // 3타 주기 계산

        if (_isFrenzy) // --- 광폭화 모드 (약 - 강 - 강) ---
        {
            if (step == 0) // 1타: 약공격
            {
                _animator.SetTrigger("attack");
                ExecuteAttack(1f, 0f);
            }
            else // 2, 3타: 광폭 강공격
            {
                // 광폭 강공격은 '기 모으기' 딜레이 없이 즉시 2배 대미지 시전
                _animator.SetTrigger("enrageAttack");
                ExecuteAttack(2f, 0.5f);
            }
        }
        else // --- 일반 모드 (약 - 약 - 강) ---
        {
            if (step == 2) // 3타: 특수 강공격 (충전식)
            {
                // 특수 강공격은 0.8초간 기를 모으며 방어력이 50% 약화되는 약점이 발생함
                _animator.SetTrigger("specialAttack");
                _currentDefenseModifier = 0.5f;
                yield return new WaitForSeconds(0.8f);

                ExecuteAttack(1.5f, 0.2f); // 1.5배 대미지 발사
                _currentDefenseModifier = 1f;  // 방어력 복구
            }
            else // 1, 2타: 약공격
            {
                _animator.SetTrigger("attack");
                ExecuteAttack(1f, 0f);
            }
        }

        _attackPatternIndex++;
        _lastAttackTime = Time.time;

        // 패턴 간 딜레이 (광폭화 시 0.3초로 매우 빨라짐)
        yield return new WaitForSeconds(_isFrenzy ? 0.3f : 0.8f);

        _isAttacking = false; // 이동 로직 허용 플래그 OFF
    }

    /// <summary>
    /// 실제 대미지 연산 및 투사체 생성 로직
    /// </summary>
    private void ExecuteAttack(float multiplier, float effect)
    {
        if (_currentTarget == null) return;
        float damage = data.attackPower * multiplier;

        if (data.attackType == EnemyType.Range) // 원거리 보스
        {
            GameObject arrow = SimpleObjectPool.Instance.SpawnFromPool(enemyArrowPrefab, transform.position, Quaternion.identity);
            arrow.GetComponent<Projectile>().Launch(damage, transform.position, _currentTarget.transform.position, data.accuracy, _currentTarget.tag);
        }
        else // 근거리 보스
        {
            if (_currentTarget.CompareTag("Unit")) _currentTarget.GetComponent<UnitStat>().TakeDamage(damage, transform.position);
            else if (_currentTarget.GetComponent<DefenseBase>() != null) _currentTarget.GetComponent<DefenseBase>().TakeDamage(damage);
        }
    }

    /// <summary>
    /// 보스 사망 처리 로직. 
    /// 애니메이션을 보여준 뒤 하이어라키와 화면에서 강제 제거하여 최적화 및 길막을 방지합니다.
    /// </summary>
    private IEnumerator DieRoutine()
    {
        if (_isDead) yield break;
        _isDead = true;

        // [최종 해결책] 죽자마자 즉시 물리 판정 제거 (다른 유닛들이 시체를 뚫고 지나갈 수 있도록)
        if (_agent != null) { _agent.isStopped = true; _agent.enabled = false; }
        if (_obstacle != null) { _obstacle.carving = false; _obstacle.enabled = false; }
        if (_collider != null) { _collider.enabled = false; }

        StopAllCoroutines(); // 진행 중인 모든 코루틴 즉시 중지

        // 사망 애니메이션 트리거 실행
        _animator.SetTrigger("die");

        // 애니메이션 상영 시간 동안 대기 (1.5초)
        // 만약 사라지는 게 너무 느리다면 이 시간을 줄이세요.
        yield return new WaitForSeconds(1.5f);

        // 시각적 삭제 및 오브젝트 비활성화
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;

        // 이 코드가 실행되어야 하이어라키 창에서 비활성화(회색) 처리됩니다.
        this.gameObject.SetActive(false);

        // 오브젝트 풀(Object Pool) 반환
        if (SimpleObjectPool.Instance != null)
            SimpleObjectPool.Instance.ReturnToPool(this.gameObject);
        else
            Destroy(this.gameObject);

        Debug.Log("<color=cyan>보스가 제거되어 풀에 반환되었습니다.</color>");
    }

    private void MoveToTarget()
    {
        if (_obstacle != null && _obstacle.enabled) { _obstacle.enabled = false; StartCoroutine(EnableAgentNextFrame()); return; }
        if (_agent.isActiveAndEnabled)
        {
            if (_agent.isOnNavMesh) _agent.isStopped = false;

            // [중요] 공격 중(_isAttacking)이 아닐 때만 걷기 애니메이션 활성화 (발 움찔거림 방지)
            if (!_isAttacking)
                _animator.SetBool("isWalking", true);

            if (Time.time >= _nextUpdateTime)
            {
                _agent.SetDestination(_currentTarget.transform.position);
                _nextUpdateTime = Time.time + _pathUpdateInterval;
            }
            HandleSpriteFlip(_agent.steeringTarget);
        }
    }

    private IEnumerator EnableAgentNextFrame() { yield return null; if (_agent != null) _agent.enabled = true; }

    private void CheckFrenzy()
    {
        if (!_isFrenzy && CurrentHp <= data.maxHp * 0.3f)
        {
            _isFrenzy = true;
            _animator.SetBool("isFrenzy", true);
            Debug.Log("<b>보스 광폭화! (약-강-강 패턴)</b>");
        }
    }

    private void HandleTargeting()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, data.attackRange * 2f, _unitLayer);
        if (hit != null) { _currentTarget = hit.gameObject; return; }

        if (!IsTargetAlive(_currentTarget))
            _currentTarget = DefenseBase.Current != null ? DefenseBase.Current.gameObject : null;
    }

    private bool IsTargetAlive(GameObject t)
    {
        if (t == null) return false;
        var u = t.GetComponent<UnitStat>();
        if (u != null) return u.CurrentHp > 0;
        var b = t.GetComponent<DefenseBase>();
        if (b != null) return b.currentHp > 0;
        return false;
    }

    private IEnumerator KnockbackRoutine(Vector2 pos)
    {
        if (visualChild == null) yield break;
        _isKnockbacking = true;
        Vector2 dir = ((Vector2)transform.position - pos).normalized;
        Vector3 tPos = (Vector3)dir * knockbackDistance;

        float el = 0f;
        while (el < knockbackDuration * 0.5f) { el += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(Vector3.zero, tPos, el / (knockbackDuration * 0.5f)); yield return null; }
        el = 0f;
        while (el < knockbackDuration * 0.5f) { el += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(tPos, Vector3.zero, el / (knockbackDuration * 0.5f)); yield return null; }

        _isKnockbacking = false;
    }

    private void HandleSpriteFlip(Vector3 t)
    {
        float d = t.x - transform.position.x;
        if (Mathf.Abs(d) < 0.01f) return;
        _spriteRenderer.flipX = d < 0;
    }

    private void LateUpdate() { transform.rotation = Quaternion.identity; }
}