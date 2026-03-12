using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class BossAi : MonoBehaviour
{
    [Header("--- 보스 설정 데이터 ---")]
    [SerializeField] private EnemyData data;
    [SerializeField] private GameObject enemyArrowPrefab;
    [SerializeField] private GameObject[] minionPrefabs;

    [Header("--- 참조 컴포넌트 ---")]
    private SpriteRenderer _spriteRenderer;
    private NavMeshAgent _agent;
    private NavMeshObstacle _obstacle;
    private Animator _animator;

    [Header("--- 피격 비주얼 ---")]
    [SerializeField] private Transform visualChild;
    [SerializeField] private float knockbackDistance = 0.3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("--- 등장 연출 설정 ---")]
    [SerializeField] private float introDashSpeed = 12f;
    [SerializeField] private float introDashDuration = 1.5f;
    private float _originalSpeed;

    [Header("--- 실시간 상태 ---")]
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
    private float _spawnTimer = 0f;
    private float _currentDefenseModifier = 1f;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _agent = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();

        if (_obstacle != null) { _obstacle.carving = true; _obstacle.enabled = false; }
        if (_agent != null) { _agent.updateRotation = false; _agent.updateUpAxis = false; _agent.enabled = true; }

        if (data != null) { CurrentHp = data.maxHp; _originalSpeed = data.moveSpeed; }
        _unitLayer = LayerMask.GetMask("Unit");
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    private void OnEnable() { StartCoroutine(IntroDashRoutine()); }

    private void Update()
    {
        if (_isDead) return;
        if (CurrentHp <= 0) { StartCoroutine(DieRoutine()); return; }
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
        if (_isFrenzy) HandleMinionSummon();
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

        if (_isFrenzy)
        {
            // 광폭화 패턴: 약-강-강 (3타 주기)
            int step = _attackPatternIndex % 3;
            if (step == 0)
            {
                Debug.Log("<color=#FF4500><b>[Frenzy]</b></color> 약공격 발사!");
                _animator.ResetTrigger("attack");
                _animator.SetTrigger("attack");
                ExecuteAttack(1f, 0f);
            }
            else
            {
                // 강공격 (enrageAttack) - 기 모으기 없음
                Debug.Log("<color=#FF0000><b>[Frenzy] Enrage Attack (즉시 강타)!</b></color>");
                _animator.ResetTrigger("enrageAttack");
                _animator.SetTrigger("enrageAttack");
                ExecuteAttack(2f, 0.5f);
            }
        }
        else
        {
            // 일반 패턴: 약-약-강 (3타 주기)
            int step = _attackPatternIndex % 3;
            if (step == 2)
            {
                // 일반 강공격 (specialAttack) - 기 모으기 0.8초
                Debug.Log("<color=#FFFF00><b>[Normal] Special Attack (기 모으는 중...)</b></color>");
                _animator.ResetTrigger("specialAttack");
                _animator.SetTrigger("specialAttack");

                _currentDefenseModifier = 0.5f;
                yield return new WaitForSeconds(0.8f);

                Debug.Log("<color=#FFD700><b>[Normal] Special Attack 발사!</b></color>");
                ExecuteAttack(1.5f, 0.2f);
                _currentDefenseModifier = 1f;
            }
            else
            {
                // 일반 약공격
                Debug.Log("<color=#FFFFFF>[Normal]</color> 약공격 발사");
                _animator.ResetTrigger("attack");
                _animator.SetTrigger("attack");
                ExecuteAttack(1f, 0f);
            }
        }

        _attackPatternIndex++;
        _lastAttackTime = Time.time;

        yield return new WaitForSeconds(_isFrenzy ? 0.3f : 0.8f);
        _isAttacking = false;
    }

    private void ExecuteAttack(float multiplier, float effect)
    {
        if (_currentTarget == null) return;
        float damage = data.attackPower * multiplier;
        if (data.attackType == EnemyType.Range)
        {
            GameObject arrow = SimpleObjectPool.Instance.SpawnFromPool(enemyArrowPrefab, transform.position, Quaternion.identity);
            arrow.GetComponent<Projectile>().Launch(damage, transform.position, _currentTarget.transform.position, data.accuracy, _currentTarget.tag);
        }
        else
        {
            if (_currentTarget.CompareTag("Unit")) _currentTarget.GetComponent<UnitStat>().TakeDamage(damage, transform.position);
            else if (_currentTarget.GetComponent<DefenseBase>() != null) _currentTarget.GetComponent<DefenseBase>().TakeDamage(damage);
        }
    }

    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        if (_isDead) return;
        CurrentHp -= Mathf.Max(damage - (data.defense * _currentDefenseModifier), 1f);
        if (CurrentHp <= 0) StartCoroutine(DieRoutine());
        else if (!_isKnockbacking) StartCoroutine(KnockbackRoutine(attackerPos));
    }

    private IEnumerator DieRoutine()
    {
        if (_isDead) yield break;
        _isDead = true;
        StopAllCoroutines();
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.enabled = false; }
        if (_obstacle != null) _obstacle.enabled = false;
        _animator.SetTrigger("die");
        yield return new WaitForSeconds(1.5f);
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        gameObject.SetActive(false);
        if (SimpleObjectPool.Instance != null) SimpleObjectPool.Instance.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }

    private void MoveToTarget() { if (_obstacle != null && _obstacle.enabled) { _obstacle.enabled = false; StartCoroutine(EnableAgentNextFrame()); return; } if (_agent.isActiveAndEnabled) { if (_agent.isOnNavMesh) _agent.isStopped = false; _animator.SetBool("isWalking", true); if (Time.time >= _nextUpdateTime) { _agent.SetDestination(_currentTarget.transform.position); _nextUpdateTime = Time.time + _pathUpdateInterval; } HandleSpriteFlip(_agent.steeringTarget); } }
    private IEnumerator EnableAgentNextFrame() { yield return null; if (_agent != null) _agent.enabled = true; }

    private void CheckFrenzy()
    {
        if (!_isFrenzy && CurrentHp <= data.maxHp * 0.3f)
        {
            _isFrenzy = true;
            _animator.SetBool("isFrenzy", true);
            Debug.Log("<b><color=red>!!!!!!!!!!!!!!!!!!!! 보스 광폭화 돌입 (패턴: 약-강-강) !!!!!!!!!!!!!!!!!!!!</color></b>");
        }
    }

    private void HandleMinionSummon() { _spawnTimer += Time.deltaTime; if (_spawnTimer >= 5f) { _spawnTimer = 0f; SummonMinions(); } }
    private void SummonMinions() { for (int i = 0; i < 2; i++) { if (minionPrefabs != null && minionPrefabs.Length > 0) { GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)]; Vector3 pos = transform.position + (Vector3)Random.insideUnitCircle * 2f; SimpleObjectPool.Instance.SpawnFromPool(prefab, pos, Quaternion.identity); } } }
    private void HandleTargeting() { Collider2D hit = Physics2D.OverlapCircle(transform.position, data.attackRange * 2f, _unitLayer); if (hit != null) { _currentTarget = hit.gameObject; return; } if (!IsTargetAlive(_currentTarget)) _currentTarget = DefenseBase.Current != null ? DefenseBase.Current.gameObject : null; }
    private bool IsTargetAlive(GameObject t) { if (t == null) return false; var u = t.GetComponent<UnitStat>(); if (u != null) return u.CurrentHp > 0; var b = t.GetComponent<DefenseBase>(); if (b != null) return b.currentHp > 0; return false; }
    private IEnumerator KnockbackRoutine(Vector2 pos) { if (visualChild == null) yield break; _isKnockbacking = true; Vector2 dir = ((Vector2)transform.position - pos).normalized; Vector3 tPos = (Vector3)dir * knockbackDistance; float el = 0f; while (el < knockbackDuration * 0.5f) { el += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(Vector3.zero, tPos, el / (knockbackDuration * 0.5f)); yield return null; } el = 0f; while (el < knockbackDuration * 0.5f) { el += Time.deltaTime; visualChild.localPosition = Vector3.Lerp(tPos, Vector3.zero, el / (knockbackDuration * 0.5f)); yield return null; } _isKnockbacking = false; }
    private void HandleSpriteFlip(Vector3 t) { float d = t.x - transform.position.x; if (Mathf.Abs(d) < 0.01f) return; _spriteRenderer.flipX = d < 0; }
    private void LateUpdate() { transform.rotation = Quaternion.identity; }
    private IEnumerator IntroDashRoutine() { yield return new WaitUntil(() => _agent.isActiveAndEnabled); float el = 0f; _agent.speed = introDashSpeed; while (el < introDashDuration) { el += Time.deltaTime; _agent.speed = Mathf.Lerp(introDashSpeed, _originalSpeed, el / introDashDuration); yield return null; } _agent.speed = _originalSpeed; }
}