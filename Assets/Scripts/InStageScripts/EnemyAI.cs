using System.Collections;
using UnityEngine;
using UnityEngine.AI; // NavMesh API 사용을 위해 추가
using UnityEngine.Tilemaps;

/// <summary>
/// A* 알고리즘(NavMesh 데이터 활용)을 통해 경로 찾기 및 전투를 담당하는 적 AI
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData data; // 적 기본 데이터(ScriptableObject)
    [SerializeField] private GameObject enemyArrowPrefab; // 적 전용 화살 프리팹
    private SpriteRenderer _spriteRenderer;
    private NavMeshAgent _agent; // NavMeshAgent
    private NavMeshObstacle _obstacle; // 전투 시 장애물 판정을 위한 컴포넌트

    [Header("Visual Knockback")]
    [SerializeField] private Transform visualChild; // 자식 오브젝트인 Visual을 드래그 앤 드롭
    [SerializeField] private float knockbackDistance = 0.3f; // 밀려나는 거리
    [SerializeField] private float knockbackDuration = 0.2f; // 복귀까지 걸리는 시간

    private float baseSpeed; // 기본 이동 속도

    [Header("Biome Settings")]
    public float slowMultiplier = 0.5f; // 감속 비율
    private int biomeLayer;

    public float CurrentHp; // 현재 체력 (아군 UnitCombat에서 참조함)
    private float _lastAttackTime; // 마지막 공격 시점
    private bool _isKnockbacking = false; // 현재 넉백 중인지 여부

    private GameObject _currentTarget; // 현재 공격 대상 (아군 유닛 또는 베이스)

    // 경로 갱신 최적화: 모든 적이 서로 다른 타이밍에 갱신하도록 설정 (부하 분산)
    private float _pathUpdateInterval = 1.0f;
    private float _nextUpdateTime;

    private LayerMask _unitLayer; // 아군 유닛 레이어

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // 자식 스프라이트 렌더러 참조
        _agent = GetComponent<NavMeshAgent>(); // 컴포넌트 할당
        _obstacle = GetComponent<NavMeshObstacle>(); // 컴포넌트 할당

        baseSpeed = _agent.speed; // 초기 속도 저장
        biomeLayer = LayerMask.NameToLayer("Biome");

        if (_obstacle != null)
        {
            _obstacle.carving = true; // 실시간 경로 재계산 활성화
            _obstacle.enabled = false; // 기본적으론 꺼둠
        }

        if (_agent != null)
        {
            _agent.updateRotation = false; // 2D이므로 회전 끄기
            _agent.updateUpAxis = false;
            _agent.enabled = true;
        }

        // 데이터가 존재할 경우 초기 체력 설정
        if (data != null) CurrentHp = data.maxHp;

        // 유니티 에디터에서 설정한 "Unit" 레이어를 가져옴
        _unitLayer = LayerMask.GetMask("Unit");

        // 모든 적이 동시에 길찾기를 연산하지 않도록 첫 업데이트 시간을 랜덤하게 분산
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    // 오브젝트 풀 초기화
    private void OnEnable()
    {
        // 체력 및 상태 초기화
        if (data != null) CurrentHp = data.maxHp;
        _isKnockbacking = false;
        _currentTarget = null;
        _nextUpdateTime = Time.time;

        // NavMeshAgent 재활성화 및 위치 보정
        if (_agent != null)
        {
            _agent.enabled = false; // 위치를 옮기기 위해 잠시 끔
            StartCoroutine(ResetAgentRoutine(_agent));
        }
    }

    private IEnumerator ResetAgentRoutine(NavMeshAgent agent)
    {
        yield return null; // 한 프레임 대기 (위치가 셋팅된 후 활성화하기 위함)
        agent.enabled = true;

        // 에이전트가 베이크된 바닥 위로 강제 스냅
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private void Update()
    {
        // 사망 상태이거나 넉백 중일 때는 모든 행동(이동/공격)을 중지
        if (CurrentHp <= 0 || _isKnockbacking) return;

        // 1. 타겟팅 처리 (주변 아군 탐색 및 유효성 검사)
        HandleTargeting();

        // 2. 행동 결정
        if (_currentTarget != null)
        {
            // 타겟이 파괴되었거나 죽었는지 한 번 더 체크
            if (!IsTargetAlive(_currentTarget))
            {
                _currentTarget = null;
                // 타겟이 죽으면 장애물 해제하고 다시 이동 준비
                if (_obstacle != null) _obstacle.enabled = false;
                return;
            }

            // 타겟과의 거리 계산
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            // 공격 사거리 내 진입 여부 판단(오차 보정 0.1f
            if (dist <= data.attackRange + 0.1f)
            {
                StopAndAttack(); // 멈춰서 공격 상태 돌입
            }
            else
            {
                // 사거리 밖이라면 타겟을 향해 이동
                MoveToTarget();
            }
        }
        else
        {
            // 타겟이 없으면 정지
            if (_agent.isActiveAndEnabled) _agent.isStopped = true;
        }
    }

    // 트리거 구역(늪, 숲 등)에 진입했을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == biomeLayer)
        {
            _agent.speed = baseSpeed * slowMultiplier;
            //Debug.Log(_agent.speed);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == biomeLayer)
        {
            _agent.speed = baseSpeed;
            //Debug.Log(_agent.speed);
        }
    }

    // 공격 상태일 때 Agent를 끄고 Obstacle을 켜서 장애물로 변신 (요청 사항 2, 3번)
    private void StopAndAttack()
    {
        if (_agent.enabled)
        {
            _agent.enabled = false; // 이동 중지
            if (_obstacle != null) _obstacle.enabled = true; // 장애물 판정 활성화
        }

        HandleSpriteFlip(_currentTarget.transform.position);
        TryAttack();
    }

    // 이동 상태일 때 Obstacle을 끄고 Agent를 켜서 길찾기 수행 (요청 사항 1, 3번)
    private void MoveToTarget()
    {
        if (_obstacle != null && _obstacle.enabled)
        {
            _obstacle.enabled = false; // 장애물 해제
            StartCoroutine(EnableAgentNextFrame()); // 에이전트 재활성화
            return;
        }

        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = false;
            //_agent.speed = data.moveSpeed;

            // 주기적인 경로 갱신으로 연산 부하 분산
            if (Time.time >= _nextUpdateTime)
            {
                _agent.SetDestination(_currentTarget.transform.position);
                _nextUpdateTime = Time.time + _pathUpdateInterval;
            }

            HandleSpriteFlip(_agent.steeringTarget); // 다음 길목을 바라봄
        }
    }

    private IEnumerator EnableAgentNextFrame()
    {
        yield return null;
        _agent.enabled = true;
    }

    /// <summary>
    /// 대상의 X축 위치에 따라 스프라이트 좌우 반전
    /// </summary>
    private void HandleSpriteFlip(Vector3 targetPos)
    {
        float diff = targetPos.x - transform.position.x;
        if (Mathf.Abs(diff) < 0.01f) return;
        _spriteRenderer.flipX = diff < 0;
    }

    /// <summary>
    /// 외부(아군)로부터 데미지를 입었을 때 호출되는 함수
    /// </summary>
    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        float finalDamage = Mathf.Max(damage - data.defense, 1f);
        CurrentHp -= finalDamage;
        Debug.Log($"적 체력: {CurrentHp}");

        // 피격 시 즉시 공격자를 돌아봄
        HandleSpriteFlip(attackerPos);

        if (gameObject.activeSelf && !_isKnockbacking)
        {
            StartCoroutine(KnockbackRoutine(attackerPos));
        }

        if (CurrentHp <= 0) Die();
    }

    /// <summary>
    /// 피격 시 뒤로 밀려나는 효과를 주는 코루틴
    /// </summary>
    private IEnumerator KnockbackRoutine(Vector2 attackerPos)
    {
        if (visualChild == null) yield break; // 자식 오브젝트 없으면 리턴

        _isKnockbacking = true;

        // 방향 계산 (공격자로부터 반대 방향)
        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
        Vector3 startPos = Vector3.zero; // 로컬 위치이므로 0
        Vector3 targetPos = new Vector3(dir.x, dir.y, 0) * knockbackDistance;

        float elapsed = 0f; // 타이머 초기화

        // 뒤로 밀리기
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (knockbackDuration * 0.5f);
            visualChild.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        elapsed = 0f; // 타이머 초기화

        // 제자리로 복귀
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (knockbackDuration * 0.5f);
            visualChild.localPosition = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }

        visualChild.localPosition = startPos; // 위치 초기화
        _isKnockbacking = false;
    }

    private void Die()
    {
        // 죽는 로직
        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(data.killReward);

        if (_obstacle != null) _obstacle.enabled = false; // 죽을 때 장애물 제거

        // 오브젝트 풀 반납
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool IsTargetAlive(GameObject target)
    {
        if (target == null) return false;

        // 구조물인 경우 체크
        var structure = target.GetComponent<MannedStructure>();
        if (structure != null)
        {
            // 체력이 0 이하이거나 IsBroken 상태면 false 반환
            return structure.CurrentHp > 0 && !structure.IsBroken;
        }

        // 유닛인 경우
        var unit = target.GetComponent<UnitStat>();
        if (unit != null) return unit.CurrentHp > 0;

        // 베이스인 경우
        var b = target.GetComponent<DefenseBase>();
        if (b != null) return b.currentHp > 0;

        return false;
    }

    /// <summary>
    /// 유닛, 구조물, 또는 Base일 경우 공격하는 메서드
    /// </summary>
    private void TryAttack()
    {
        if (_currentTarget == null) return;

        if (Time.time >= _lastAttackTime + (1f / data.attackSpeed))
        {
            if (data.attackType == EnemyType.Range) // 원거리 공격일 경우
            {
                // 원거리 공격 타겟 태그 결정 (Base, Unit, Structure, Tower 모두 대응)
                string tagToHit = "Unit"; // 기본값
                if (_currentTarget.CompareTag("Base")) tagToHit = "Base";
                else if (_currentTarget.CompareTag("Structure") || _currentTarget.CompareTag("Tower")) tagToHit = "Structure";

                // 적 원거리 공격 발사
                GameObject arrowObj = SimpleObjectPool.Instance.SpawnFromPool(enemyArrowPrefab, transform.position, Quaternion.identity);
                Projectile p = arrowObj.GetComponent<Projectile>();

                if (p != null)
                {
                    p.Launch(data.attackPower, transform.position, _currentTarget.transform.position, data.accuracy, tagToHit);
                }
            }
            else // 근접 공격일 경우
            {
                // [핵심 수정] 인터페이스를 통해 유닛/구조물/베이스 구분 없이 데미지 전달
                if (_currentTarget.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(data.attackPower, transform.position);
                    Debug.Log($"<color=red>[근접 공격]</color> {_currentTarget.name}에게 데미지를 입혔습니다.");
                }
                // 만약 인터페이스가 없는 예외 케이스(직접 참조)
                else if (_currentTarget.CompareTag("Base"))
                {
                    _currentTarget.GetComponent<DefenseBase>().TakeDamage(data.attackPower);
                }
            }
            _lastAttackTime = Time.time;
        }
    }

    private void HandleTargeting()
    {
        // 1. 기본 목표인 Base 확인
        GameObject baseObj = DefenseBase.Current != null ? DefenseBase.Current.gameObject : null;

        // 2. 주변 모든 레이어의 대상 탐색 (사거리의 2배 범위)
        // 기존 _unitLayer 대신 모든 것을 감지할 수 있도록 하거나, 구조물 레이어가 포함되었는지 확인이 필요합니다.
        // 여기서는 안전하게 모든 레이어를 체크하되, 필요한 레이어만 필터링합니다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.attackRange * 2f);

        GameObject bestTarget = null;
        int highestPriority = -1;

        foreach (var hit in hits)
        {
            // 타겟이 살아있는지 먼저 체크
            if (!IsTargetAlive(hit.gameObject)) continue;

            int currentPriority = 0;

            // --- [우선순위 판별 로직 보강] ---

            // 순위 1: Base (최우선)
            if (hit.CompareTag("Base"))
            {
                currentPriority = 3;
            }
            // 순위 2: Structure 또는 Tower (태그 + 컴포넌트 교차 체크)
            else if (hit.CompareTag("Structure") || hit.CompareTag("Tower") || hit.GetComponent<MannedStructure>() != null)
            {
                currentPriority = 2;
                // Debug.Log($"<color=cyan>[감지]</color> 구조물 발견: {hit.name}");
            }
            // 순위 3: Unit
            else if (hit.CompareTag("Unit"))
            {
                currentPriority = 1;
            }

            // 우선순위가 더 높거나, 같으면 더 가까운 것 선택
            if (currentPriority > highestPriority && currentPriority > 0)
            {
                highestPriority = currentPriority;
                bestTarget = hit.gameObject;
            }
            else if (currentPriority == highestPriority && bestTarget != null)
            {
                if (Vector2.Distance(transform.position, hit.transform.position) <
                    Vector2.Distance(transform.position, bestTarget.transform.position))
                {
                    bestTarget = hit.gameObject;
                }
            }
        }

        // --- [최종 타겟 할당] ---
        if (bestTarget != null)
        {
            _currentTarget = bestTarget;
        }
        else
        {
            _currentTarget = baseObj;
        }
    }
}