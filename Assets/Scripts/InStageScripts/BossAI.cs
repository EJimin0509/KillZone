using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    [Header("--- 보스 설정 데이터 ---")]
    [SerializeField] private EnemyData data;
    [SerializeField] private GameObject enemyArrowPrefab;

    [Header("--- 참조 컴포넌트 ---")]
    private SpriteRenderer _spriteRenderer;
    private NavMeshAgent _agent;
    private NavMeshObstacle _obstacle;
    private Animator _animator;
    private Collider2D _collider;

    [Header("--- 피격 비주얼 ---")]
    [SerializeField] private Transform visualChild;
    [SerializeField] private float knockbackDistance = 0.3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("--- 실시간 상태 변수 ---")]
    public float CurrentHp;
    private float _lastAttackTime;
    private bool _isKnockbacking = false;
    private GameObject _currentTarget;

    private float _pathUpdateInterval = 1.0f;
    private float _nextUpdateTime;
    private LayerMask _unitLayer;

    private int _attackPatternIndex = 0;
    private bool _isAttacking = false;
    private bool _isFrenzy = false;
    private bool _isDead = false;
    private float _currentDefenseModifier = 1f;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _agent = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();
        _collider = GetComponent<Collider2D>();

        if (_obstacle != null) { _obstacle.carving = true; _obstacle.enabled = false; }
        if (_agent != null) { _agent.updateRotation = false; _agent.updateUpAxis = false; _agent.enabled = true; }

        if (data != null)
        {
            CurrentHp = data.maxHp;
            if (_agent != null) _agent.speed = data.moveSpeed;
        }
        _unitLayer = LayerMask.GetMask("Unit");
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    private void OnEnable()
    {
        _isDead = false;
        _isAttacking = false;
        _isFrenzy = false;
        _attackPatternIndex = 0;
        _currentDefenseModifier = 1f;
        if (data != null) CurrentHp = data.maxHp;
    }

    private void Update()
    {
        if (_isDead) return;
        if (_isKnockbacking || _isAttacking) return;

        CheckFrenzy();
        HandleTargeting();

        if (_currentTarget != null)
        {
            if (!IsTargetAlive(_currentTarget))
            {
                _currentTarget = null;
                _animator.SetBool("isWalking", false);
                return;
            }

            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);
            if (dist <= data.attackRange + 0.5f) StopAndAttack();
            else MoveToTarget();
        }
        else
        {
            _animator.SetBool("isWalking", false);
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
        }
    }

    // --- [중요] 인터페이스 없이 직접 호출받는 대미지 함수 ---
    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        if (_isDead) return;

        float finalDamage = Mathf.Max((damage - data.defense) * _currentDefenseModifier, 1f);
        CurrentHp -= finalDamage;

        HandleSpriteFlip(attackerPos);

        if (gameObject.activeSelf && !_isKnockbacking)
        {
            StartCoroutine(KnockbackRoutine(attackerPos));
        }

        if (CurrentHp <= 0) StartCoroutine(DieRoutine());
    }

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
            if (_obstacle != null) _obstacle.enabled = true;
        }

        if (Time.time >= _lastAttackTime + (1f / data.attackSpeed))
        {
            StartCoroutine(AttackPatternRoutine());
        }
    }

    private IEnumerator AttackPatternRoutine()
    {
        _isAttacking = true;
        int step = _attackPatternIndex % 3;

        if (_isFrenzy)
        {
            if (step == 0) { _animator.SetTrigger("attack"); ExecuteAttack(1f); }
            else { _animator.SetTrigger("enrageAttack"); ExecuteAttack(2f); }
        }
        else
        {
            if (step == 2)
            {
                _animator.SetTrigger("specialAttack");
                _currentDefenseModifier = 1.5f;
                yield return new WaitForSeconds(0.8f);
                ExecuteAttack(1.5f);
                _currentDefenseModifier = 1f;
            }
            else { _animator.SetTrigger("attack"); ExecuteAttack(1f); }
        }

        _attackPatternIndex++;
        _lastAttackTime = Time.time;
        yield return new WaitForSeconds(_isFrenzy ? 0.3f : 0.8f);
        _isAttacking = false;
    }

    private void ExecuteAttack(float multiplier)
    {
        if (_currentTarget == null) return;
        float damage = data.attackPower * multiplier;

        // --- 태그 기반 공격 판정 ---
        if (data.attackType == EnemyType.Range)
        {
            GameObject arrow = SimpleObjectPool.Instance.SpawnFromPool(enemyArrowPrefab, transform.position, Quaternion.identity);
            // 타겟의 태그를 넘겨서 Projectile이 알아서 처리하게 함
            arrow.GetComponent<Projectile>().Launch(damage, transform.position, _currentTarget.transform.position, data.accuracy, _currentTarget.tag);
        }
        else
        {
            // 근접 공격: 태그에 따라 컴포넌트 직접 참조
            if (_currentTarget.CompareTag("Unit"))
            {
                _currentTarget.GetComponent<UnitStat>().TakeDamage(damage, transform.position);
            }
            else if (_currentTarget.CompareTag("Base"))
            {
                _currentTarget.GetComponent<DefenseBase>().TakeDamage(damage);
            }
        }
    }

    private IEnumerator DieRoutine()
    {
        if (_isDead) yield break;
        _isDead = true;

        if (data != null && GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(data.killReward + data.bossBonus);
        }

        if (_agent != null) { _agent.isStopped = true; _agent.enabled = false; }
        if (_obstacle != null) _obstacle.enabled = false;
        if (_collider != null) _collider.enabled = false;

        StopAllCoroutines();
        _animator.SetTrigger("die");
        yield return new WaitForSeconds(1.5f);

        gameObject.SetActive(false);
        if (SimpleObjectPool.Instance != null) SimpleObjectPool.Instance.ReturnToPool(this.gameObject);
        else Destroy(gameObject);
    }

    private void MoveToTarget()
    {
        if (_obstacle != null && _obstacle.enabled) { _obstacle.enabled = false; StartCoroutine(EnableAgentNextFrame()); return; }
        if (_agent.isActiveAndEnabled)
        {
            if (_agent.isOnNavMesh) _agent.isStopped = false;
            if (!_isAttacking) _animator.SetBool("isWalking", true);
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
        if (t.CompareTag("Unit")) return t.GetComponent<UnitStat>().CurrentHp > 0;
        if (t.CompareTag("Base")) return t.GetComponent<DefenseBase>().currentHp > 0;
        return false;
    }

    private IEnumerator KnockbackRoutine(Vector2 attackerPos)
    {
        if (visualChild == null) yield break;
        _isKnockbacking = true;
        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
        Vector3 targetPos = (Vector3)dir * knockbackDistance;

        float el = 0f;
        while (el < knockbackDuration * 0.5f) { el += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(Vector3.zero, targetPos, el / (knockbackDuration * 0.5f)); yield return null; }
        el = 0f;
        while (el < knockbackDuration * 0.5f) { el += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(targetPos, Vector3.zero, el / (knockbackDuration * 0.5f)); yield return null; }

        _isKnockbacking = false;
    }

    private void HandleSpriteFlip(Vector3 target)
    {
        float diff = target.x - transform.position.x;
        if (Mathf.Abs(diff) < 0.1f) return;
        _spriteRenderer.flipX = diff < 0;
    }

    private void LateUpdate() { transform.rotation = Quaternion.identity; }
}