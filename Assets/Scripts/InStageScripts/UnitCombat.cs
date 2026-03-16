using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class UnitCombat : MonoBehaviour
{
    private UnitStat _myStat;
    private NavMeshAgent _agent;
    private NavMeshObstacle _obstacle;
    private float _lastAttackTime;
    private float _scanTimer;

    public bool IsForceMoving = false;
    public GameObject CurrentTarget;

    [Header("Combat Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float scanInterval = 0.2f;
    [SerializeField] private GameObject arrowPrefab;

    private void Awake()
    {
        _myStat = GetComponent<UnitStat>();
        _agent = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();

        // 초기 설정: Obstacle은 꺼두고 Agent는 켭니다.
        if (_obstacle != null)
        {
            _obstacle.enabled = false;
            _obstacle.carving = true;
        }

        if (_agent != null)
        {
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            _agent.enabled = true;
        }
    }

    private void Update()
    {
        if (_myStat.CurrentHp <= 0) return;

        // 강제 이동 중 도착 체크
        if (IsForceMoving && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                IsForceMoving = false;
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

    private void HandleCombatAI()
    {
        if (IsForceMoving) return;

        if (CurrentTarget != null)
        {
            float dist = Vector2.Distance(transform.position, CurrentTarget.transform.position);
            EnemyAI targetEnemy = CurrentTarget.GetComponent<EnemyAI>();

            if (targetEnemy == null || targetEnemy.CurrentHp <= 0)
            {
                ResetCombat();
                return;
            }

            // 사거리 안인 경우 (공격)
            if (dist <= _myStat.AttackRange)
            {
                EnableObstacle(); // 공격 시에는 장애물 모드 가동
                HandleFlip(CurrentTarget.transform.position);
                TryAttack(targetEnemy);
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

    // --- [핵심 수정: 장애물/에이전트 스위칭 로직] ---

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

    // --- [기존 전투 로직 유지] ---

    public void ResetCombat() => CurrentTarget = null;

    private void SearchTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _myStat.AttackRange, enemyLayer);
        float closestDist = float.MaxValue;
        GameObject closestEnemy = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null && enemy.CurrentHp > 0)
                {
                    float d = Vector2.Distance(transform.position, hit.transform.position);
                    if (d < closestDist) { closestDist = d; closestEnemy = hit.gameObject; }
                }
            }
        }
        CurrentTarget = closestEnemy;
    }

    private void TryAttack(EnemyAI target)
    {
        if (Time.time >= _lastAttackTime + (1f / _myStat.AttackSpeed))
        {
            if (_myStat.currentWeapon != null && _myStat.currentWeapon.type == EquipmentType.Bow)
            {
                GameObject arrowObj = SimpleObjectPool.Instance.SpawnFromPool(arrowPrefab, transform.position, Quaternion.identity);
                Projectile p = arrowObj.GetComponent<Projectile>();
                p.Launch(_myStat.AttackPower, transform.position, target.transform.position, _myStat.RangeAccuracy, "Enemy");
            }
            else target.TakeDamage(_myStat.AttackPower, transform.position);
            _lastAttackTime = Time.time;
        }
    }

    private void HandleFlip(Vector3 targetPos)
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = targetPos.x < transform.position.x;
    }
}