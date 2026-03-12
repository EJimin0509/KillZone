using UnityEngine;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// [Anubis Boss AI System]
/// 일반 상태: 약-강-약 패턴 (강공격 시 1초 기 모으기 / 방어력 약화)
/// 광폭 상태: 강-약-강 패턴 (강공격 딜레이 삭제 / 공격 속도 증가)
/// </summary>
public class BossAi : MonoBehaviour
{
    [Header("--- 보스 설정 데이터 ---")]
    [SerializeField] private EnemyData data;                // 기본 스탯 (SO)
    [SerializeField] private GameObject enemyArrowPrefab;   // 투사체 프리팹
    [SerializeField] private GameObject[] minionPrefabs;    // 광폭 시 소환할 몹

    [Header("--- 참조 컴포넌트 ---")]
    private SpriteRenderer _spriteRenderer;
    private NavMeshAgent _agent;
    private NavMeshObstacle _obstacle;
    private Animator _animator;

    [Header("--- 피격 비주얼 ---")]
    [SerializeField] private Transform visualChild;         // 넉백 연출용 자식 오브젝트
    [SerializeField] private float knockbackDistance = 0.3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("--- 등장 연출 설정 ---")]
    [SerializeField] private float introDashSpeed = 12f;     // 초기 돌진 속도
    [SerializeField] private float introDashDuration = 1.5f; // 돌진 지속 시간
    private float _originalSpeed;                           // 기본 속도 저장용

    [Header("--- 실시간 상태 ---")]
    public float CurrentHp;                                 // 현재 체력
    private float _lastAttackTime;                          // 공격 쿨타임 계산용
    private bool _isKnockbacking = false;                   // 넉백 중 행동 제어
    private GameObject _currentTarget;                      // 타겟 유닛/베이스

    private float _pathUpdateInterval = 1.0f;               // 길찾기 갱신 주기
    private float _nextUpdateTime;
    private LayerMask _unitLayer;                           // 유닛 레이어

    // --- 공격 패턴 관련 변수 ---
    private int _attackPatternIndex = 0;                    // 3타 주기 관리 (0, 1, 2)
    private bool _isAttacking = false;                      // 공격 애니메이션 중 이동 방지
    private bool _isFrenzy = false;                         // 광폭화 여부 (HP 30% 이하)
    private bool _isDead = false;                           // 사망 체크
    private float _spawnTimer = 0f;                         // 쫄몹 소환 타이머
    private float _currentDefenseModifier = 1f;             // 강공격 중 방어력 페널티 수치

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _agent = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();

        // 2D 환경 최적화 설정
        if (_obstacle != null) { _obstacle.carving = true; _obstacle.enabled = false; }
        if (_agent != null) { _agent.updateRotation = false; _agent.updateUpAxis = false; _agent.enabled = true; }

        if (data != null) { CurrentHp = data.maxHp; _originalSpeed = data.moveSpeed; }
        _unitLayer = LayerMask.GetMask("Unit");
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    private void OnEnable() { StartCoroutine(IntroDashRoutine()); } // 생성 시 등장 돌진 연출 실행

    private void Update()
    {
        if (_isDead || _isKnockbacking || _isAttacking) return;

        CheckFrenzy();      // 매 프레임 체력 확인
        HandleTargeting();  // 적 탐색

        if (_currentTarget != null)
        {
            if (!IsTargetAlive(_currentTarget)) // 타겟 사망 시 초기화
            {
                _currentTarget = null;
                _animator.SetBool("isWalking", false);
                if (_obstacle != null) _obstacle.enabled = false;
                return;
            }
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);
            if (dist <= data.attackRange + 0.5f) StopAndAttack(); // 사거리 내면 공격
            else MoveToTarget(); // 사거리 밖이면 추격
        }
        else
        {
            _animator.SetBool("isWalking", false);
            if (_agent.isActiveAndEnabled) _agent.isStopped = true;
        }

        if (_isFrenzy) HandleMinionSummon(); // 광폭화 시 5초마다 몹 소환
    }

    private void LateUpdate() { transform.rotation = Quaternion.identity; } // 물리 충돌로 인한 각도 틀어짐 방지

    private IEnumerator IntroDashRoutine() // 보스 등장 시 가속 후 감속하며 접근
    {
        yield return new WaitUntil(() => _agent.isActiveAndEnabled);
        float elapsed = 0f;
        _agent.speed = introDashSpeed;
        _agent.acceleration = 100f;
        while (elapsed < introDashDuration)
        {
            elapsed += Time.deltaTime;
            _agent.speed = Mathf.Lerp(introDashSpeed, _originalSpeed, elapsed / introDashDuration);
            yield return null;
        }
        _agent.speed = _originalSpeed;
        _agent.acceleration = 8f;
    }

    private void CheckFrenzy() // HP 30% 이하 시 1회 실행
    {
        if (!_isFrenzy && CurrentHp <= data.maxHp * 0.3f)
        {
            _isFrenzy = true;
            _animator.SetBool("isFrenzy", true);
            Debug.Log("<color=red><b>[System] 보스 광폭화! [강-약-강] 패턴 강화! (딜레이 삭제)</b></color>");
        }
    }

    private void HandleMinionSummon() // 5초 주기 소환 관리
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= 5f) { _spawnTimer = 0f; SummonMinions(); }
    }

    private void SummonMinions() // 보스 주변 랜덤 위치 소환
    {
        for (int i = 0; i < 2; i++)
        {
            if (minionPrefabs != null && minionPrefabs.Length > 0)
            {
                GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
                Vector3 spawnPos = transform.position + (Vector3)Random.insideUnitCircle * 2f;
                SimpleObjectPool.Instance.SpawnFromPool(prefab, spawnPos, Quaternion.identity);
            }
        }
    }

    private void MoveToTarget() // 추격 로직
    {
        if (_obstacle != null && _obstacle.enabled) { _obstacle.enabled = false; StartCoroutine(EnableAgentNextFrame()); return; }
        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = false;
            _animator.SetBool("isWalking", true);
            _agent.speed = _isFrenzy ? _originalSpeed * 1.5f : _agent.speed; // 광폭 시 이속 1.5배
            if (Time.time >= _nextUpdateTime) { _agent.SetDestination(_currentTarget.transform.position); _nextUpdateTime = Time.time + _pathUpdateInterval; }
            HandleSpriteFlip(_agent.steeringTarget);
        }
    }

    private void StopAndAttack() // 공격 모드 전환
    {
        _animator.SetBool("isWalking", false);
        if (_agent.enabled) { _agent.enabled = false; if (_obstacle != null) _obstacle.enabled = true; }
        HandleSpriteFlip(_currentTarget.transform.position);
        if (Time.time >= _lastAttackTime + (1f / data.attackSpeed)) { StartCoroutine(AttackPatternRoutine()); }
    }

    /// <summary>
    /// [최종 수정된 공격 패턴]
    /// 1. 타입 결정: (일반) 약-강-약 / (광폭) 강-약-강
    /// 2. 딜레이 결정: 강공격 시 (일반) 1초 대기 / (광폭) 즉시 타격
    /// </summary>
    private IEnumerator AttackPatternRoutine()
    {
        _isAttacking = true;
        int currentStep = _attackPatternIndex % 3;
        bool isStrongAttack = false;

        // [단계 1] 이번 공격이 약공격인지 강공격인지 결정합니다.
        if (!_isFrenzy)
        {
            // 일반 상태: 약(0) -> 강(1) -> 약(2)
            isStrongAttack = (currentStep == 1);
        }
        else
        {
            // 광폭 상태: 강(0) -> 약(1) -> 강(2)
            isStrongAttack = (currentStep == 0 || currentStep == 2);
        }

        // [단계 2] 결정된 공격을 실행합니다.
        if (isStrongAttack)
        {
            // --- [강공격 실행] ---
            if (!_isFrenzy)
            {
                // 일반 강공격: 플레이어에게 피할 틈을 주는 1초 기 모으기 (방어력 약화 페널티)
                Debug.Log($"<color=orange><b>[READY] 일반 강공격 준비! 1초 대기...</b></color>");
                _currentDefenseModifier = 0.5f;        // 기 모으는 중 방어력 50% 약화
                _animator.SetTrigger("specialAttack");
                yield return new WaitForSeconds(1.0f); // 1초 물리적 선딜레이
                ExecuteAttack(2f, 0.5f);               // 데미지 2배 타격
                _currentDefenseModifier = 1f;          // 방어력 복구
            }
            else
            {
                // 광폭 강공격: 딜레이 없이 즉시 강공격 (폭주 연출)
                Debug.Log($"<color=red><b>[FRENZY] 폭주 강공격! 즉시 타격!!</b></color>");
                _animator.SetTrigger("specialAttack");
                ExecuteAttack(2f, 0.5f);               // 기다리지 않고 바로 타격
            }
        }
        else
        {
            // --- [약공격 실행] ---
            // 일반/광폭 공통: 약공격은 항상 딜레이 없이 즉시 실행
            Debug.Log($"<color=white>[Action] 약공격 발동 (패턴단계:{currentStep})</color>");
            _animator.SetTrigger("attack");
            ExecuteAttack(1f, 0f); // 즉시 타격
        }

        _attackPatternIndex++;
        _lastAttackTime = Time.time;

        // 공격 후 경직 시간 (광폭 시에는 0.5초로 단축하여 더 빠르게 다음 공격 준비)
        float waitTime = _isFrenzy ? 0.5f : 0.8f;
        yield return new WaitForSeconds(waitTime);
        _isAttacking = false;
    }

    private void ExecuteAttack(float damageMultiplier, float statusEffectChance) // 데미지 처리 로직
    {
        if (_currentTarget == null) return;
        float finalDamage = data.attackPower * damageMultiplier;
        if (data.attackType == EnemyType.Range)
        {
            string tagToHit = _currentTarget.CompareTag("Base") ? "Base" : "Unit";
            GameObject arrowObj = SimpleObjectPool.Instance.SpawnFromPool(enemyArrowPrefab, transform.position, Quaternion.identity);
            Projectile p = arrowObj.GetComponent<Projectile>();
            p.Launch(finalDamage, transform.position, _currentTarget.transform.position, data.accuracy, tagToHit);
        }
        else
        {
            if (_currentTarget.CompareTag("Unit")) _currentTarget.GetComponent<UnitStat>().TakeDamage(finalDamage, transform.position);
            else if (_currentTarget.GetComponent<DefenseBase>() != null) _currentTarget.GetComponent<DefenseBase>().TakeDamage(finalDamage);
        }
    }

    public void TakeDamage(float damage, Vector2 attackerPos) // 피격 처리 (강공격 중엔 더 아프게 맞음)
    {
        if (_isDead) return;
        float effectiveDefense = data.defense * _currentDefenseModifier;
        float finalDamage = Mathf.Max(damage - effectiveDefense, 1f);
        CurrentHp -= finalDamage;
        if (CurrentHp > 0 && !_isKnockbacking) StartCoroutine(KnockbackRoutine(attackerPos));
        if (CurrentHp <= 0) StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine() // 사망 연출
    {
        _isDead = true;
        StopAllCoroutines();
        if (_agent != null) _agent.enabled = false;
        if (_obstacle != null) _obstacle.enabled = false;
        _animator.SetTrigger("die");
        yield return new WaitForSeconds(1.5f);
        if (SimpleObjectPool.Instance != null) SimpleObjectPool.Instance.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }

    private IEnumerator EnableAgentNextFrame() { yield return null; _agent.enabled = true; }
    private void HandleSpriteFlip(Vector3 targetPos) { float diff = targetPos.x - transform.position.x; if (Mathf.Abs(diff) < 0.01f) return; _spriteRenderer.flipX = diff < 0; }
    private bool IsTargetAlive(GameObject target) { if (target == null) return false; var unit = target.GetComponent<UnitStat>(); if (unit != null) return unit.CurrentHp > 0; var b = target.GetComponent<DefenseBase>(); if (b != null) return b.currentHp > 0; return false; }
    private void HandleTargeting() { Collider2D hit = Physics2D.OverlapCircle(transform.position, data.attackRange * 2f, _unitLayer); if (hit != null) { _currentTarget = hit.gameObject; return; } if (!IsTargetAlive(_currentTarget)) _currentTarget = DefenseBase.Current != null ? DefenseBase.Current.gameObject : null; }
    private IEnumerator KnockbackRoutine(Vector2 attackerPos) { if (visualChild == null) yield break; _isKnockbacking = true; Vector2 dir = ((Vector2)transform.position - attackerPos).normalized; Vector3 targetPos = (Vector3)dir * knockbackDistance; float elapsed = 0f; while (elapsed < knockbackDuration * 0.5f) { elapsed += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(Vector3.zero, targetPos, elapsed / (knockbackDuration * 0.5f)); yield return null; } elapsed = 0f; while (elapsed < knockbackDuration * 0.5f) { elapsed += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(targetPos, Vector3.zero, elapsed / (knockbackDuration * 0.5f)); yield return null; } _isKnockbacking = false; }
}