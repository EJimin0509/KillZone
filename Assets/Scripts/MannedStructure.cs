using UnityEngine;

[RequireComponent(typeof(StatusEffectHandler))]
public class MannedStructure : MonoBehaviour, IDamageable
{
    public StructureData data;
    [SerializeField] private LayerMask enemyLayer;
    public StatusEffectData breakdownEffect;

    private float _currentHp;
    private UnitStat _garrisonedUnit;
    private StatusEffectHandler _effectHandler;

    private float _lastAttackTime = 0f;
    private float _continuousAttackTimer = 0f;
    private float _recentDamageTaken = 0f;
    private float _recentDamageTimer = 0f;

    public float CurrentHp => _currentHp;
    public bool HasGarrisonedUnit => _garrisonedUnit != null;

    private void Awake()
    {
        _currentHp = data.hpOrDurability;
        _effectHandler = GetComponent<StatusEffectHandler>();
    }

    private void Update()
    {
        if (_currentHp <= 0) return;

        _recentDamageTimer += Time.deltaTime;
        if (_recentDamageTimer > 1f)
        {
            _recentDamageTaken = 0f;
            _recentDamageTimer = 0f;
        }

        if (_garrisonedUnit != null && !_effectHandler.IsAttackDisabled)
        {
            HandleCombat();
        }
        else if (Time.time - _lastAttackTime > 5f)
        {
            _continuousAttackTimer = 0f;
        }
    }

    private void HandleCombat()
    {
        if (Time.time < _lastAttackTime + (1f / _garrisonedUnit.AttackSpeed)) return;

        float currentRange = data.type == StructureType.Tower ? _garrisonedUnit.AttackRange + data.range : data.range;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, currentRange, enemyLayer);
        if (hit != null)
        {
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null && enemy.CurrentHp > 0)
            {
                PerformAttack(enemy);
                _lastAttackTime = Time.time;
                _continuousAttackTimer += Time.deltaTime;
                if (_continuousAttackTimer >= 30f)
                {
                    TriggerBreakdown();
                    _continuousAttackTimer = 0f;
                }
            }
        }
    }

    private void PerformAttack(EnemyAI enemy)
    {
        float accuracy = data.type == StructureType.Tower ? _garrisonedUnit.RangeAccuracy : data.accuracy;
        float damage = data.type == StructureType.Tower ? _garrisonedUnit.AttackPower : data.damage;

        if (Random.Range(0f, 100f) <= accuracy)
        {
            enemy.TakeDamage(damage, transform.position);
            if (data.applyEffect != null)
            {
                enemy.GetComponent<IStatusEffectable>()?.ApplyStatusEffect(data.applyEffect);
            }
        }
    }

    public void GarrisonUnit(UnitStat unit)
    {
        if (_garrisonedUnit != null) return;
        _garrisonedUnit = unit;

        if (data.type == StructureType.Tower)
        {
            if (!unit.baseLevels.ContainsKey(StatBonusType.RangeAccuracy))
                unit.baseLevels[StatBonusType.RangeAccuracy] = unit.baseStatLevel;
            unit.baseLevels[StatBonusType.RangeAccuracy] += 2;
            unit.RefreshStats();
        }

        // 컴포넌트 강제 종료하여 오작동 방지
        var combat = unit.GetComponent<UnitCombat>();
        if (combat != null) combat.enabled = false;

        var agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var movement = unit.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var collider = unit.GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        unit.transform.SetParent(this.transform);
        unit.transform.localPosition = new Vector3(0, 0, -1f);
        unit.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer[] srs = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs) sr.sortingOrder = 100;

        Debug.Log($"<color=green>[배치 성공]</color> {unit.data.unitName} 유닛 배치 완료!");
    }

    public void UnGarrisonUnit(Vector2 targetPos)
    {
        if (_garrisonedUnit == null) return;
        UnitStat unit = _garrisonedUnit;

        if (data.type == StructureType.Tower)
        {
            unit.baseLevels[StatBonusType.RangeAccuracy] -= 2;
            unit.RefreshStats();
        }

        unit.transform.SetParent(null);
        unit.transform.localScale = Vector3.one;
        unit.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        SpriteRenderer[] srs = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs) sr.sortingOrder = 0;

        // 컴포넌트 재활성화
        var combat = unit.GetComponent<UnitCombat>();
        if (combat != null) combat.enabled = true;

        var collider = unit.GetComponent<Collider2D>();
        if (collider != null) collider.enabled = true;

        var agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        var movement = unit.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = true;
            movement.CommandMove(targetPos);
        }

        _garrisonedUnit = null;
        Debug.Log("<color=yellow>[배치 해제]</color> 지정된 위치로 이동합니다.");
    }

    public void TakeDamage(float amount, Vector2 attackerPos = default)
    {
        _currentHp -= amount;
        _recentDamageTaken += amount;
        if (_recentDamageTaken >= data.hpOrDurability * 0.4f)
        {
            TriggerBreakdown();
            _recentDamageTaken = 0f;
        }
        if (_currentHp <= 0) Die();
    }

    private void TriggerBreakdown()
    {
        if (breakdownEffect != null) _effectHandler.ApplyStatusEffect(breakdownEffect);
    }

    private void Die()
    {
        if (_garrisonedUnit != null) UnGarrisonUnit(transform.position);
        gameObject.SetActive(false);
    }
}