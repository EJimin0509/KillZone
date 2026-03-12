using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(StatusEffectHandler))]
public class MannedStructure : MonoBehaviour, IDamageable
{
    public StructureData data;
    [SerializeField] private LayerMask enemyLayer;
    public StatusEffectData breakdownEffect;

    [Header("AOE Settings (3x3)")]
    [Tooltip("공격 범위 크기 (Ballista/Catapult용 3x3 권장)")]
    [SerializeField] private Vector2 areaSize = new Vector2(3f, 3f);
    [Tooltip("Catapult 중심부 데미지/넉백 판정 거리")]
    [SerializeField] private float centerThreshold = 0.7f;

    [Header("Attack Speed Settings")]
    [Tooltip("이 구조물 고유의 공격 쿨타임 (초)")]
    [SerializeField] private float customCooldown = 2.0f;
    [Tooltip("체크 시: customCooldown / 유닛공속 공식 적용 | 체크 해제 시: customCooldown 고정 적용")]
    [SerializeField] private bool useUnitAttackSpeed = false;

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

        // 유닛이 배치되어 있고 공격 불가 상태가 아닐 때만 전투 로직 실행
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
        // 1. 개별 쿨타임 계산
        float finalCooldown = customCooldown;
        if (useUnitAttackSpeed && _garrisonedUnit != null && _garrisonedUnit.AttackSpeed > 0)
        {
            finalCooldown = customCooldown / _garrisonedUnit.AttackSpeed;
        }

        // 2. 쿨타임 체크 (각 구조물 인스턴스별로 독립 작동)
        if (Time.time < _lastAttackTime + finalCooldown) return;

        float currentRange = data.type == StructureType.Tower ? _garrisonedUnit.AttackRange + data.range : data.range;

        // 3. 메인 타겟 탐색 (공격의 중심점을 잡기 위함)
        Collider2D mainTarget = Physics2D.OverlapCircle(transform.position, currentRange, enemyLayer);

        if (mainTarget != null)
        {
            Vector3 attackPoint = mainTarget.transform.position;
            PerformAOEAttack(attackPoint);

            _lastAttackTime = Time.time;

            // 연속 공격 시 과열/고장 누적 로직
            _continuousAttackTimer += finalCooldown;
            if (_continuousAttackTimer >= 30f)
            {
                TriggerBreakdown();
                _continuousAttackTimer = 0f;
            }
        }
    }

    private void PerformAOEAttack(Vector3 centerPoint)
    {
        // 중심점 기준으로 3x3 박스 영역 안의 모든 적 검출
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(centerPoint, areaSize, 0f, enemyLayer);

        bool isBallista = data.structureName.Contains("Ballista");
        bool isCatapult = data.structureName.Contains("Catapult");

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyAI enemy = enemyCollider.GetComponent<EnemyAI>();
            if (enemy == null || enemy.CurrentHp <= 0) continue;

            float distance = Vector2.Distance(centerPoint, enemyCollider.transform.position);

            if (isBallista)
            {
                // Ballista: 3x3 전체 동일 데미지
                float damage = data.type == StructureType.Tower ? _garrisonedUnit.AttackPower : data.damage;
                enemy.TakeDamage(damage, transform.position);

                if (data.applyEffect != null)
                {
                    enemy.GetComponent<IStatusEffectable>()?.ApplyStatusEffect(data.applyEffect);
                }
            }
            else if (isCatapult)
            {
                // Catapult: 중심부/주변부 차등 데미지 및 조건부 넉백
                float damage_1 = data.damage;
                float damage_2 = data.damage * 0.5f;

                if (distance <= centerThreshold)
                {
                    enemy.TakeDamage(damage_1, transform.position);
                    if (data.applyEffect != null)
                    {
                        enemy.GetComponent<IStatusEffectable>()?.ApplyStatusEffect(data.applyEffect);
                    }
                }
                else
                {
                    enemy.TakeDamage(damage_2, transform.position);
                }
            }
            else // Tower 및 기타: 단일 타겟 처리
            {
                // OverlapBoxAll 중 첫 번째 적(주 타겟)만 공격
                if (enemyCollider.gameObject == hitEnemies[0].gameObject)
                {
                    float damage = data.type == StructureType.Tower ? _garrisonedUnit.AttackPower : data.damage;
                    enemy.TakeDamage(damage, transform.position);
                }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float range = (data != null) ? data.range : 5f;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0));
    }
}