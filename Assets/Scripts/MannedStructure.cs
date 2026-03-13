using UnityEngine;
using System.Collections;

[RequireComponent(typeof(StatusEffectHandler))]
public class MannedStructure : MonoBehaviour, IDamageable
{
    public StructureData data;
    [SerializeField] private LayerMask enemyLayer;
    public StatusEffectData breakdownEffect;

    [Header("AOE Settings")]
    [SerializeField] private Vector2 areaSize = new Vector2(3f, 3f);
    [SerializeField] private float centerThreshold = 0.7f;

    [Header("Attack Settings")]
    [SerializeField] private float customCooldown = 5.0f;
    [SerializeField] private bool useUnitAttackSpeed = false;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float centerDamage = 20f;
    [SerializeField] private float outerDamage = 10f;

    [Header("Visual Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    private float _currentHp;
    private UnitStat _garrisonedUnit;
    private StatusEffectHandler _effectHandler;
    private Animator _animator;
    private float _lastAttackTime = 0f;
    private float _continuousAttackTimer = 0f;

    public float CurrentHp => _currentHp;
    public bool HasGarrisonedUnit => _garrisonedUnit != null;

    private void Awake()
    {
        _currentHp = data.hpOrDurability;
        _effectHandler = GetComponent<StatusEffectHandler>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_currentHp <= 0) return;
        if (_garrisonedUnit != null && !_effectHandler.IsAttackDisabled)
        {
            HandleCombat();
        }
    }

    private void HandleCombat()
    {
        float finalCooldown = customCooldown;
        if (useUnitAttackSpeed && _garrisonedUnit != null && _garrisonedUnit.AttackSpeed > 0)
        {
            finalCooldown = customCooldown / _garrisonedUnit.AttackSpeed;
        }

        if (Time.time < _lastAttackTime + finalCooldown) return;

        float range = data.type == StructureType.Tower ? _garrisonedUnit.AttackRange + data.range : data.range;
        Collider2D target = Physics2D.OverlapCircle(transform.position, range, enemyLayer);

        if (target != null)
        {
            _lastAttackTime = Time.time;
            Debug.Log($"<color=cyan>[Structure]</color> 공격 대상 발견: {target.name}");

            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
                Debug.Log("<color=cyan>[Structure]</color> 애니메이터 트리거 발동");
            }

            bool isCatapult = data.structureName.Contains("Catapult");
            float delay = isCatapult ? 0.3f : 0.1f;

            StartCoroutine(ExecuteAttackAfterDelay(target.transform.position, delay, isCatapult));

            _continuousAttackTimer += finalCooldown;
            if (_continuousAttackTimer >= 30f)
            {
                TriggerBreakdown();
                _continuousAttackTimer = 0f;
            }
        }
    }

    // MannedStructure.cs 내부

    private IEnumerator ExecuteAttackAfterDelay(Vector3 targetPos, float delay, bool isCatapult)
    {
        Debug.Log($"<color=green>[Coroutine]</color> {delay}초 대기 시작");
        yield return new WaitForSeconds(delay);
        Debug.Log("<color=green>[Coroutine]</color> 투사체 생성 시점 도달");

        if (isCatapult)
        {
            // Catapult: 기존 유지 (1발 발사)
            SpawnProjectile(targetPos, true, centerDamage, outerDamage, areaSize);
        }
        else
        {
            // Ballista: 3x3 공간에 9개 투사체 '동시' 발사
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3 offsetPos = targetPos + new Vector3(x, y, 0);

                    // SpawnProjectile 함수를 호출하여 9발을 연속으로 생성합니다.
                    // 루프 안에 yield return이 없으므로, 프레임 지연 없이 즉시 생성됩니다.
                    SpawnProjectile(offsetPos, false, centerDamage, 0, new Vector2(1f, 1f));
                }
            }

            // (선택 사항) 9발이 동시에 나갈 때의 효과음이나 파티클을 여기서 한 번 재생하면 좋습니다.
            Debug.Log("<color=cyan>[Ballista]</color> 9발 동시 발사!");
        }
    }

    private void SpawnProjectile(Vector3 target, bool isCatapult, float cDmg, float oDmg, Vector2 aoe)
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Setup(target, projectileSpeed, isCatapult, cDmg, oDmg, aoe, enemyLayer);
            }
        }
        else
        {
            Debug.LogError("<color=red>[Error]</color> ProjectilePrefab 또는 FirePoint가 설정되지 않았습니다!");
        }
    }

    // --- IDamageable 및 Garrison 관련 로직 ---
    public void TakeDamage(float amount, Vector2 attackerPos = default) { _currentHp -= amount; if (_currentHp <= 0) Die(); }
    public void GarrisonUnit(UnitStat unit)
    {
        if (_garrisonedUnit != null) return; _garrisonedUnit = unit;
        unit.GetComponent<UnitCombat>().enabled = false;
        unit.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        unit.transform.SetParent(this.transform);
        unit.transform.localPosition = new Vector3(0, 0, -1f);
        unit.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }
    public void UnGarrisonUnit(Vector2 targetPos)
    {
        if (_garrisonedUnit == null) return;
        _garrisonedUnit.transform.SetParent(null);
        _garrisonedUnit.transform.localScale = Vector3.one;
        _garrisonedUnit.GetComponent<UnitCombat>().enabled = true;
        var agent = _garrisonedUnit.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) { agent.enabled = true; agent.Warp(transform.position); }
        _garrisonedUnit = null;
    }
    private void TriggerBreakdown() { if (breakdownEffect != null) _effectHandler.ApplyStatusEffect(breakdownEffect); }
    private void Die() { if (_garrisonedUnit != null) UnGarrisonUnit(transform.position); gameObject.SetActive(false); }
}