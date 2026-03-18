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
            _obstacle.enabled = false;
            _obstacle.carving = true;
        }

        if (_agent != null)
        {
            _agent.updateRotation = false; // 2D이므로 회전 끄기
            _agent.updateUpAxis = false;
            _agent.enabled = true;

            _agent.avoidancePriority = 50;
            _agent.radius = 0.15f;
            _agent.stoppingDistance = 0.1f;
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

        if (_scanTimer > 0) _scanTimer -= Time.deltaTime;

        // 3. 타겟이 없거나 강제 이동 중이면 장애물 판정 제거
        if (IsForceMoving || CurrentTarget == null)
        {
            DisableObstacle();
        }

        HandleCombatAI();
    }

    private void DisableObstacle()
    {
        // 이동 중: 장애물을 끄고 에이전트를 켬
        if (_obstacle != null && _obstacle.enabled)
        {
            _obstacle.enabled = false;
        }

        if (!_agent.enabled)
        {
            _agent.enabled = true;
        }
    }

    /// <summary>
    /// 1. 강제 이동 중이거나 타겟이 없으면 적을 자동으로 탐색한다.
    /// 2. 타겟의 생존 여부를 확인하고, 전투 상태 여부를 결정한다.
    /// 3. 타겟을 공격하고, 범위를 벗어나면 추적한다.
    /// </summary>
    private void HandleCombatAI()
    {
        if (IsForceMoving) return;

        if (CurrentTarget != null)
        {
            float dist = Vector2.Distance(transform.position, CurrentTarget.transform.position);

            // --- [수정] 타겟이 일반 적인지 보스인지 모두 체크하도록 변경 ---
            bool isAlive = false;
            EnemyAI targetEnemy = CurrentTarget.GetComponent<EnemyAI>();
            BossAI targetBoss = CurrentTarget.GetComponent<BossAI>();

            if (targetEnemy != null && targetEnemy.CurrentHp > 0) isAlive = true;
            else if (targetBoss != null && targetBoss.CurrentHp > 0) isAlive = true;

            if (!isAlive)
            {
                ResetCombat();
                return;
            }

            // 사거리 안인 경우 (공격)
            if (dist <= _myStat.AttackRange)
            {
                EnableObstacle(); // 공격 시에는 장애물 모드 가동
                HandleFlip(CurrentTarget.transform.position);

                // 공격 시 EnemyAI 혹은 BossAI를 판단하여 공격 실행
                TryAttackLogic(targetEnemy, targetBoss);
            }
            // 사거리 밖인 경우 (추격)
            else
            {
                DisableObstacle(); // 이동 시에는 장애물 모드 해제
                if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(CurrentTarget.transform.position);
                }
            }
        }
        else
        {
            if (_scanTimer <= 0) { SearchTarget(); _scanTimer = scanInterval; }
        }
    }

    private void EnableObstacle()
    {
        // 공격 중: 에이전트를 멈추고 장애물을 켬
        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        if (_obstacle != null && !_obstacle.enabled)
        {
            _agent.enabled = false; // 에이전트를 아예 꺼야 충돌이 안 일어남
            _obstacle.enabled = true;
        }
    }

    /// <summary>
    /// 에이전트 재활성화를 위한 코루틴
    /// </summary>
    /// <returns></returns>
    //private IEnumerator EnableAgentNextFrame()
    //{
    //    yield return null;
    //    _agent.enabled = true;
    //}

    /// <summary>
    /// 전투가 종료되면 상태를 초기화하는 메서드
    /// </summary>
    public void ResetCombat()
    {
        CurrentTarget = null; // 타겟 NULL
    }

    /// <summary>
    /// Circle Collider 만큼의 범위 내 적을 탐색하는 메서드
    /// </summary>
    private void SearchTarget()
    {
        // enemyLayer에 속한 오브젝트 찾기
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _myStat.AttackRange + 2f, enemyLayer);

        float closestDist = float.MaxValue;
        GameObject closestEnemy = null;

        foreach (var hit in hits)
        {
            // 태그가 "Enemy"인지 확인 (보스도 Enemy 태그를 가지고 있어야 탐색됨)
            if (hit.CompareTag("Enemy"))
            {
                // EnemyAI 혹은 BossAI 컴포넌트가 있고 살아있는지 확인
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                BossAI boss = hit.GetComponent<BossAI>();

                bool isTargetValid = false;
                if (enemy != null && enemy.CurrentHp > 0) isTargetValid = true;
                else if (boss != null && boss.CurrentHp > 0) isTargetValid = true;

                if (isTargetValid)
                {
                    float d = Vector2.Distance(transform.position, hit.transform.position);
                    if (d < closestDist) { closestDist = d; closestEnemy = hit.gameObject; }
                }
            }
        }
        CurrentTarget = closestEnemy;
    }

    /// <summary>
    /// 1. 공격 속도에 맞춰 적을 공격한다. (EnemyAI와 BossAI 모두 대응)
    /// </summary>
    private void TryAttackLogic(EnemyAI targetEnemy, BossAI targetBoss)
    {
        if (Time.time >= _lastAttackTime + (1f / _myStat.AttackSpeed)) // 공격 속도 체크
        {
            Vector3 targetPos = targetEnemy != null ? targetEnemy.transform.position : targetBoss.transform.position;
            string targetTag = targetEnemy != null ? targetEnemy.tag : targetBoss.tag;

            // 원거리 명중률 체크
            if (_myStat.currentWeapon != null && _myStat.currentWeapon.type == EquipmentType.Bow)
            {
                // 오브젝트 풀링 사용
                GameObject arrowObj = SimpleObjectPool.Instance.SpawnFromPool(arrowPrefab, transform.position, Quaternion.identity);
                Projectile p = arrowObj.GetComponent<Projectile>();

                // 발사 (대미지, 시작위치, 적위치, 명중률, 타겟태그)
                p.Launch(_myStat.AttackPower, transform.position, targetPos, _myStat.RangeAccuracy, targetTag);
            }
            else // 근접 공격
            {
                if (targetEnemy != null) targetEnemy.TakeDamage(_myStat.AttackPower, transform.position); // 내 위치 정보를 넘겨 넉백 방향 계산
                else if (targetBoss != null) targetBoss.TakeDamage(_myStat.AttackPower, transform.position);
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

        DisableObstacle(); // 강제 명령 시 즉시 장애물 제거

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