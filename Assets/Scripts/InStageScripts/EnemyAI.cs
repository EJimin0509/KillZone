using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private GameObject enemyArrowPrefab;
    private SpriteRenderer _spriteRenderer;
    private NavMeshAgent _agent;
    // private NavMeshObstacle _obstacle;

    [Header("Visual Knockback")]
    [SerializeField] private Transform visualChild;
    [SerializeField] private float knockbackDistance = 0.3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("UI Reference")]
    [SerializeField] private Slider hpSlider;

    private float baseSpeed;
    [Header("Biome Settings")]
    public float slowMultiplier = 0.5f;
    private int biomeLayer;

    public float CurrentHp;
    private float _lastAttackTime;
    private bool _isKnockbacking = false;

    private GameObject _currentTarget;

    private float _pathUpdateInterval = 1.0f;
    private float _nextUpdateTime;

    private LayerMask _unitLayer;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _agent = GetComponent<NavMeshAgent>();

        // [중요] 튕김 방지: NavMeshObstacle이 있다면 런타임에서 아예 꺼버립니다.
        NavMeshObstacle obs = GetComponent<NavMeshObstacle>();
        if (obs != null) obs.enabled = false;

        baseSpeed = data.moveSpeed; // ScriptableObject에서 기본 속도 가져옴

        if (_agent != null)
        {
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            _agent.enabled = true;
            _agent.speed = baseSpeed;

            _agent.radius = 0.15f;
            _agent.stoppingDistance = 0.1f;

            // 기본 회피 우선순위 (50)
            _agent.avoidancePriority = 50;
        }

        biomeLayer = LayerMask.NameToLayer("Biome");
        if (data != null) CurrentHp = data.maxHp;
        _unitLayer = LayerMask.GetMask("Unit");
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    private void OnEnable()
    {
        if (data != null) CurrentHp = data.maxHp;
        _isKnockbacking = false;
        _currentTarget = null;
        _nextUpdateTime = Time.time;

        if (_agent != null)
        {
            _agent.enabled = false;
            StartCoroutine(ResetAgentRoutine(_agent));
        }
    }

    private IEnumerator ResetAgentRoutine(NavMeshAgent agent)
    {
        yield return null;
        agent.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private void Update()
    {
        if (CurrentHp <= 0 || _isKnockbacking) return;

        HandleTargeting();

        if (_currentTarget != null)
        {
            if (!IsTargetAlive(_currentTarget))
            {
                _currentTarget = null;
                return;
            }

            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            // 공격 사거리 내 진입 (0.1f 여유)
            if (dist <= data.attackRange + 0.5f)
            {
                StopAndAttack();
            }
            else
            {
                MoveToTarget();
            }
        }
        else
        {
            if (_agent.isActiveAndEnabled)
            {
                _agent.isStopped = true;
                _agent.avoidancePriority = 50;
            }
        }
    }

    // [수정] 공격 상태 로직
    private void StopAndAttack()
    {
        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;

            // [핵심] Carving 대신 우선순위를 최고(0)로 높여서 '말뚝' 상태가 됩니다.
            // 이렇게 하면 다른 움직이는 적들이 이 유닛을 밀지 못하고 돌아갑니다.
            _agent.avoidancePriority = 0;
        }

        HandleSpriteFlip(_currentTarget.transform.position);
        TryAttack();
    }

    // [수정] 이동 상태 로직
    private void MoveToTarget()
    {
        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = false;

            // 이동 중에는 다른 공격 중인 적들 사이를 지나갈 수 있게 우선순위를 낮춥니다.
            _agent.avoidancePriority = 50;

            if (Time.time >= _nextUpdateTime)
            {
                _agent.SetDestination(_currentTarget.transform.position);
                _nextUpdateTime = Time.time + _pathUpdateInterval;
            }

            HandleSpriteFlip(_agent.steeringTarget);
        }
    }

    // ... (OnTriggerEnter2D, OnTriggerExit2D 기존과 동일) ...

    private void HandleSpriteFlip(Vector3 targetPos)
    {
        float diff = targetPos.x - transform.position.x;
        if (Mathf.Abs(diff) < 0.01f) return;
        _spriteRenderer.flipX = diff < 0;
    }

    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        float finalDamage = Mathf.Max(damage - data.defense, 1f);
        CurrentHp -= finalDamage;
        UpdateHealthUI();

        HandleSpriteFlip(attackerPos);

        if (gameObject.activeSelf && !_isKnockbacking)
        {
            StartCoroutine(KnockbackRoutine(attackerPos));
        }

        if (CurrentHp <= 0) Die();
    }

    private IEnumerator KnockbackRoutine(Vector2 attackerPos)
    {
        if (visualChild == null) yield break;
        _isKnockbacking = true;

        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
        Vector3 startPos = Vector3.zero;
        Vector3 targetPos = new Vector3(dir.x, dir.y, 0) * knockbackDistance;

        float elapsed = 0f;
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            visualChild.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / (knockbackDuration * 0.5f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            visualChild.localPosition = Vector3.Lerp(targetPos, startPos, elapsed / (knockbackDuration * 0.5f));
            yield return null;
        }

        visualChild.localPosition = startPos;
        _isKnockbacking = false;
    }

    private void Die()
    {
        if (_agent.isActiveAndEnabled) _agent.avoidancePriority = 50;

        if (SimpleObjectPool.Instance != null)
            SimpleObjectPool.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }

    private bool IsTargetAlive(GameObject target)
    {
        if (target == null) return false;
        var unit = target.GetComponent<UnitStat>();
        if (unit != null) return unit.CurrentHp > 0;
        var b = target.GetComponent<DefenseBase>();
        if (b != null) return b.currentHp > 0;
        return false;
    }

    private void TryAttack()
    {
        if (Time.time >= _lastAttackTime + (1f / data.attackSpeed))
        {
            if (data.attackType == EnemyType.Range)
            {
                string tagToHit = _currentTarget.CompareTag("Base") ? "Base" : "Unit";
                GameObject arrowObj = SimpleObjectPool.Instance.SpawnFromPool(enemyArrowPrefab, transform.position, Quaternion.identity);
                Projectile p = arrowObj.GetComponent<Projectile>();
                p.Launch(data.attackPower, transform.position, _currentTarget.transform.position, data.accuracy, tagToHit);
            }
            else
            {
                if (_currentTarget.CompareTag("Unit"))
                    _currentTarget.GetComponent<UnitStat>().TakeDamage(data.attackPower, transform.position);
                else if (_currentTarget.CompareTag("Base"))
                    _currentTarget.GetComponent<DefenseBase>().TakeDamage(data.attackPower);
            }
            _lastAttackTime = Time.time;
        }
    }

    private void HandleTargeting()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, data.attackRange * 2f, _unitLayer);
        if (hit != null)
        {
            _currentTarget = hit.gameObject;
            return;
        }

        if (!IsTargetAlive(_currentTarget))
        {
            _currentTarget = DefenseBase.Current != null ? DefenseBase.Current.gameObject : null;
        }
    }

    public void UpdateHealthUI()
    {
        if (hpSlider != null) hpSlider.value = CurrentHp / data.maxHp;
    }
}