using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 아군 유닛의 전투 스크립트
/// 범위 내 적을 탐색하고 적을 자동 공격한다.
/// 적이 범위를 벗어나면 추적한다.
/// 적이 사망했거나, 다른 명령이 강제되면 전투를 종료한다.
/// </summary>
public class UnitCombat : MonoBehaviour
{
    private UnitStat _myStat; // 유닛의 Stat 데이터 참조
    private NavMeshAgent _agent; // 유닛이 NaveMesh 사용 중이므로 NaveMesh 참조
    private float _lastAttackTime; // 공격 속도
    private float _scanTimer; // 스캔 탐색용 타이머
    private NavMeshObstacle _obstacle; // 장애물 판정용

    public bool IsForceMoving = false; // 현재 유닛이 강제 이동 명령을 받았는지 확인
    public GameObject CurrentTarget; // 현재 공격중인 타겟을 담을 변수

    [Header("Combat Settings")]
    [SerializeField] private LayerMask enemyLayer;      // 적 유닛의 레이어
    [SerializeField] private float scanInterval = 0.2f; // 타겟 탐색 주기
    [SerializeField] private GameObject arrowPrefab; // 화살 프리팹

    private void Awake()
    {
        _myStat = GetComponent<UnitStat>(); // UnitStat 컴포넌트 참조
        _agent = GetComponent<NavMeshAgent>(); // NavMeshAgent 컴포넌트 참조
        _obstacle = GetComponent<NavMeshObstacle>();

        if (_obstacle != null)
        {
            _obstacle.carving = true; // 실시간 경로 재계산 활성화
            _obstacle.enabled = false; // 기본은 비활성화
        }

        if (_agent != null)
        {
            _agent.updateRotation = false; // 2D이므로 회전 끄기
            _agent.updateUpAxis = false;
            _agent.enabled = true;
        }
    }

    private void Update()
    {
        // 사망 상태라면 리턴
        if (_myStat.CurrentHp <= 0) return;

        // 강제 이동 중 목적지에 도착했다면 다시 전투 모드로 복귀
        if (IsForceMoving && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                IsForceMoving = false; // 강제 이동 완료 -> 다시 자동 사냥 시작
            }
        }

        // 1. 스캔 타이머 가동
        if (_scanTimer > 0)  _scanTimer -= Time.deltaTime;

        // 2. 타겟 상태 업데이트 및 추적 로직
        HandleCombatAI();
    }

    /// <summary>
    /// 1. 강제 이동 중이거나 타겟이 없으면 적을 자동으로 탐색한다.
    /// 2. 타겟의 생존 여부를 확인하고, 전투 상태 여부를 결정한다.
    /// 3. 타겟을 공격하고, 범위를 벗어나면 추적한다.
    /// </summary>
    private void HandleCombatAI()
    {
        // 에이전트가 활성화되어 있고 NavMesh 위에 있을 때만 제어
        bool canControlAgent = _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;

        // 강제 이동 중이거나 타겟이 없으면 자동 탐색
        if (CurrentTarget == null)
        {
            if (_scanTimer <= 0)              // 스캔 가능 할 때
            {
                SearchTarget();               // 적을 탐색하고
                _scanTimer = scanInterval;    // 스캔 타이머를 초기화
            }
            return;
        }

        // 타겟의 생존 여부 확인
        EnemyAI targetEnemy = CurrentTarget.GetComponent<EnemyAI>();
        if (targetEnemy == null || targetEnemy.CurrentHp <= 0) // 타겟이 없거나 사망
        {
            ResetCombat(); // 전투 종료 후 상태 초기화
            // 타겟이 죽으면 멈추지 않고 다시 탐색 대기 상태로 전환
            if (canControlAgent && !IsForceMoving) _agent.isStopped = false;
            return;        // 리턴
        }

        // 타겟과 유닛 사이의 거리 Vector2
        float dist = Vector2.Distance(transform.position, CurrentTarget.transform.position);

        // 3. 사거리 및 추적 로직 (여유값 0.2f)
        if (dist <= _myStat.AttackRange + 0.2f)
        {
            // 사거리 안이면 정지
            if (!IsForceMoving)
            {
                if (_agent.enabled)
                {
                    _agent.enabled = false; // 에이전트 끄기
                    if (_obstacle != null) _obstacle.enabled = true; // 장애물 켜기
                }

                // 공격 시 적을 바라보게 함
                HandleFlip(CurrentTarget.transform.position);
                TryAttack(targetEnemy); // 공격
            }
        }
        else
        {
            // 사거리 밖이고 강제 이동 중이 아니라면 추격
            if (!IsForceMoving)
            {
                if (_obstacle != null && _obstacle.enabled)
                {
                    _obstacle.enabled = false;
                    StartCoroutine(EnableAgentNextFrame()); // 한 프레임 뒤 Agent 활성화
                    return;
                }

                if (_agent.isActiveAndEnabled)
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(CurrentTarget.transform.position); // 새로운 경로 설정
                }
            }
        }
    }

   /// <summary>
   /// 에이전트 재활성화를 위한 코루틴
   /// </summary>
   /// <returns></returns>
    private IEnumerator EnableAgentNextFrame()
    {
        yield return null;
        _agent.enabled = true;
    }

    /// <summary>
    /// 전투가 종료되면 상태를 초기화하는 메서드
    /// </summary>
    public void ResetCombat()
    {
        CurrentTarget = null; // 타겟 NULL
        IsForceMoving = false; // 정지
    }

    /// <summary>
    /// Circle Collider 만큼의 범위 내 적을 탐색하는 메서드
    /// </summary>
    private void SearchTarget()
    {
        // enemyLayer에 속한 오브젝트 찾기
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _myStat.AttackRange, enemyLayer);

        float closestDist = float.MaxValue;
        GameObject closestEnemy = null;

        foreach (var hit in hits)
        {
            // 태그가 "Enemy"인지 확인
            if (hit.CompareTag("Enemy"))
            {
                // EnemyAI 컴포넌트가 있고 살아있는지 확인
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null && enemy.CurrentHp > 0)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestEnemy = hit.gameObject;
                    }
                }
            }
        }
        CurrentTarget = closestEnemy;
    }

    /// <summary>
    /// 1. 공격 속도에 맞춰 적을 공격한다.
    /// </summary>
    /// <param name="target">공격 대상</param>
    private void TryAttack(EnemyAI target)
    {
        if (Time.time >= _lastAttackTime + (1f / _myStat.AttackSpeed)) // 공격 속도 체크
        {
            // 원거리 명중률 체크
            if (_myStat.currentWeapon != null && _myStat.currentWeapon.type == EquipmentType.Bow)
            {
                // 오브젝트 풀링 사용
                GameObject arrowObj = SimpleObjectPool.Instance.SpawnFromPool(arrowPrefab, transform.position, Quaternion.identity);
                Projectile p = arrowObj.GetComponent<Projectile>();

                // 발사 (대미지, 시작위치, 적위치, 명중률, 타겟태그)
                p.Launch(_myStat.AttackPower, transform.position, target.transform.position, _myStat.RangeAccuracy, "Enemy");
            }
            else // 근접 공격
            {
                target.TakeDamage(_myStat.AttackPower, transform.position); // 내 위치 정보를 넘겨 넉백 방향 계산
            }

            _lastAttackTime = Time.time; // 초기화
        }
    }

    /// <summary>
    /// PlayerMovement에서 명령을 내릴 때 호출하여 이동 정의
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="isMoveCommand">다른 이동 명령이 부여되었는지</param>
    public void SetManualCommand(GameObject target, bool isMoveCommand)
    {
        CurrentTarget = target;
        IsForceMoving = isMoveCommand;

        // 에이전트 가동 상태 체크
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }
    }

    /// <summary>
    /// 공격 중인 방향으로 스프라이트 반전
    /// </summary>
    /// <param name="targetPos"></param>
    private void HandleFlip(Vector3 targetPos)
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;
        sr.flipX = targetPos.x < transform.position.x;
    }
}