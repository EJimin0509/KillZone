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

    // --- 배치(탑승) 로직 ---
    public void GarrisonUnit(UnitStat unit)
    {
        if (_garrisonedUnit != null) return;

        _garrisonedUnit = unit;

        // 망루 스탯 보너스 부여
        if (data.type == StructureType.Tower)
        {
            if (!unit.baseLevels.ContainsKey(StatBonusType.RangeAccuracy))
                unit.baseLevels[StatBonusType.RangeAccuracy] = unit.baseStatLevel;
            unit.baseLevels[StatBonusType.RangeAccuracy] += 2;
            unit.RefreshStats();
        }

        // 컴포넌트 비활성화 (이동/물리 끄기)
        unit.GetComponent<Collider2D>().enabled = false;
        unit.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        unit.GetComponent<PlayerMovement>().enabled = false;

        // 크기 축소 및 중앙 부착 (시각적 연출)
        unit.transform.SetParent(this.transform);
        unit.transform.localPosition = Vector3.zero;
        unit.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        Debug.Log($"{unit.data.unitName} 유닛이 {data.structureName}에 배치되었습니다!");
    }

    // --- 해제(내리기) 로직 ---
    public void UnGarrisonUnit(Vector2 targetPos)
    {
        if (_garrisonedUnit == null) return;

        UnitStat unit = _garrisonedUnit;

        // 망루 보너스 원상복구
        if (data.type == StructureType.Tower)
        {
            unit.baseLevels[StatBonusType.RangeAccuracy] -= 2;
            unit.RefreshStats();
        }

        // 부모 해제 및 크기 원상복구
        unit.transform.SetParent(null);
        unit.transform.localScale = Vector3.one;

        // 컴포넌트 재활성화
        unit.GetComponent<Collider2D>().enabled = true;
        var agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
        agent.enabled = true;

        // NavMesh 위로 스냅 (구조물 밖으로 정상적으로 이동하기 위함)
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        var movement = unit.GetComponent<PlayerMovement>();
        movement.enabled = true;

        // 지정된 바닥으로 이동 명령
        movement.CommandMove(targetPos);

        _garrisonedUnit = null;
        Debug.Log("유닛 배치 해제! 지정된 위치로 이동합니다.");
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
        // 파괴 시 강제로 밖으로 떨어짐
        if (_garrisonedUnit != null) UnGarrisonUnit(transform.position);
        gameObject.SetActive(false);
    }
}