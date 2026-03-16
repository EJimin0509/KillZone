using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MannedStructure : MonoBehaviour, IDamageable
{
    public StructureData data;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Garrison Settings")]
    [SerializeField] private Vector3 garrisonOffset = new Vector3(0, 0.8f, -10f);

    [Header("Visual Settings")]
    [SerializeField] private Sprite brokenSprite;
    private SpriteRenderer _structureSR;
    private bool _isBroken = false;

    [Header("Status (Debug)")]
    [SerializeField] private float _currentHp; // 인스펙터에서 실시간 체력 확인용

    // 속성 정의 (오류 방지)
    public float CurrentHp => _currentHp;
    public bool IsBroken => _isBroken;
    private UnitStat _garrisonedUnit;
    public bool HasGarrisonedUnit => _garrisonedUnit != null;

    private Animator _animator;
    private float _lastAttackTime = 0f;

    [Header("Attack Settings")]
    [SerializeField] private float customCooldown = 5.0f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float baseDamage = 15f;

    private void Awake()
    {
        _structureSR = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponentInChildren<Animator>();

        // 초기 체력 설정 확인
        if (data != null)
        {
            _currentHp = data.hpOrDurability;
            Debug.Log($"<color=cyan>[Structure]</color> {gameObject.name} 초기 체력: {_currentHp}");
        }
        else
        {
            _currentHp = 100f; // 데이터가 없을 경우를 대비한 기본값
            Debug.LogError($"{gameObject.name}의 StructureData가 할당되지 않았습니다!");
        }
    }

    // IDamageable 인터페이스 실제 구현 부분
    public void TakeDamage(float amount, Vector2 attackerPos = default)
    {
        if (_isBroken) return;

        _currentHp -= amount;
        Debug.Log($"<color=yellow>[Structure Damage]</color> {gameObject.name} 남은 체력: {_currentHp}");

        if (_currentHp <= 0)
        {
            BreakStructure();
        }
    }

    private void BreakStructure()
    {
        if (_isBroken) return;
        _isBroken = true;
        _currentHp = 0;

        Debug.Log($"<color=red><b>[Structure Destroyed]</b></color> {gameObject.name}이 파괴되었습니다!");

        // 1. 유닛 사망 처리
        if (_garrisonedUnit != null)
        {
            Debug.Log($"<color=red>[Unit Death]</color> 배치된 유닛 {_garrisonedUnit.name} 사망.");
            _garrisonedUnit.TakeDamage(9999f, transform.position);
            _garrisonedUnit.transform.SetParent(null); // 부모 관계 해제
            _garrisonedUnit = null;
        }

        // 2. 비주얼 변경
        if (_animator != null) _animator.enabled = false;
        if (brokenSprite != null)
        {
            _structureSR.sprite = brokenSprite;
            // 부서진 이미지가 구조물보다 뒤에 보이지 않도록 순서 조정
            _structureSR.sortingOrder = 10;
        }

        // 3. 적 타겟팅 제외
        gameObject.tag = "Untagged";

        // 4. 콜라이더 유지 여부 (필요 시 콜라이더도 비활성화 가능)
        // GetComponent<Collider2D>().enabled = false;
    }

    // --- 배치/하차 함수들 (기존과 동일) ---
    public void GarrisonUnit(UnitStat unit)
    {
        if (_isBroken || _garrisonedUnit != null) return;
        _garrisonedUnit = unit;
        unit.GetComponent<UnitCombat>().enabled = false;
        if (unit.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;
        unit.transform.SetParent(this.transform);
        unit.transform.localPosition = garrisonOffset;
        foreach (var sr in unit.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder = 30000;
    }

    public void UnGarrisonUnit(Vector2 targetPos)
    {
        if (_isBroken || _garrisonedUnit == null) return;
        _garrisonedUnit.transform.SetParent(null);
        _garrisonedUnit.transform.localScale = Vector3.one;
        _garrisonedUnit.GetComponent<UnitCombat>().enabled = true;
        if (_garrisonedUnit.TryGetComponent(out NavMeshAgent agent))
        {
            agent.enabled = true;
            agent.Warp(new Vector3(transform.position.x, transform.position.y - 2.0f, 0));
        }
        foreach (var sr in _garrisonedUnit.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder = 5;
        _garrisonedUnit = null;
    }

    private void Update()
    {
        if (_isBroken) return;
        if (_garrisonedUnit != null)
        {
            _garrisonedUnit.transform.localPosition = garrisonOffset;
            HandleCombat();
        }
    }

    private void HandleCombat() { /* 투사체 발사 로직 */ }
}