using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class UnitStat : MonoBehaviour
{
    public UnitData data; // 유닛 데이터
    public EquipmentData currentWeapon; // 착용 무기
    public EquipmentData currentHelm; // 착용 헬름
    public EquipmentData currentChest; // 착용 갑옷

    [Header("Visual Knockback")]
    [SerializeField] private Transform visualChild; // 밀려나는 것 처럼 보이게 할 자식 오브젝트
    [SerializeField] private float knockbackDistance = 0.3f; // 밀려나는 거리
    [SerializeField] private float knockbackDuration = 0.2f; // 복귀까지 걸리는 시간

    [Header("Faith Settings")]
    public float maxFaith = 100f;
    public float currentFaith;
    public bool isPanicking = false;
    public Color panicColor = Color.magenta; // 패닉 시 강조할 색상
    private Color _originalBGColor;
    private Coroutine _blinkCoroutine;
    private Image _faithBGImage;

    [Header("UI Reference")]
    public Slider hpSlider;
    public Slider faithSlider;

    private float _faithRegenTimer = 0f;
    private NavMeshAgent _agent;
    private int _originalLayer;
    private MonoBehaviour _unitCombat;
    private float baseSpeed;

    // 유닛 생성기에 의해 할당될 베이스 단계들 (기본 1단계 초기화)
    public Dictionary<StatBonusType, int> baseLevels = new Dictionary<StatBonusType, int>();

    private bool _isKnockbacking = false;                // 지금 넉백 중인지 판단
    
    //[Range(1, 10)] public int baseStatLevel = 1; // 테스트용 통합 레벨

    public float MaxHp { get; private set; }             // 최대 HP
    public float CurrentHp { get; private set; }         // 현재 HP
    public float AttackPower { get; private set; }       // 공격력
    public float AttackRange { get; private set; }       // 공격 범위
    public float AttackSpeed { get; private set; }       // 공격 속도
    public float Defense { get; private set; }           // 방어력


    // 보조 스탯들
    public float RangeAccuracy { get; private set; }     // 명중률
    public float RepairSpeed { get; private set; }       // 수리 속도
    public float HealPower { get; private set; }         // 의술 (HP 회복량)
    public float MaxFaith { get; private set; }          // 의지 (MTL_MAX)
    public float FaithRegenAmount { get; private set; }  // 신앙 (MTL_SPEED)

    private void Awake()
    {
        _unitCombat = GetComponent("UnitCombat") as MonoBehaviour;
        _originalLayer = gameObject.layer;
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null) baseSpeed = _agent.speed;

        // 1. 딕셔너리 및 기본 구조 초기화
        foreach (StatBonusType type in System.Enum.GetValues(typeof(StatBonusType)))
        {
            if (!baseLevels.ContainsKey(type)) baseLevels[type] = 1;
        }

        // 2. 스탯 계산 (여기서 MaxFaith가 결정됨)
        RefreshStats();

        // 3. [수정] 현재 수치들을 최대치로 초기화 (RefreshStats 이후에 수행)
        CurrentHp = MaxHp;
        currentFaith = MaxFaith;

        // 4. UI 및 이미지 참조
        if (faithSlider != null)
        {
            // 캔버스가 비활성화 상태여도 찾을 수 있게 true 인자 유지
            Image[] images = faithSlider.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name == "Background")
                {
                    _faithBGImage = img;
                    _originalBGColor = img.color;
                    break;
                }
            }
        }

        // 5. 최종 UI 갱신
        UpdateStatusUI();
    }

    void Update()
    {
        HandleFaithRegen();
        UpdateStatusUI();
    }

    /// <summary>
    /// 스탯 초기화 메서드
    /// </summary>
    public void RefreshStats()
    {
        if (data == null) return;

        MaxHp = CalculateFinalStat(data.hp, StatBonusType.Hp);
        AttackPower = CalculateFinalStat(data.melee, StatBonusType.AttackPower);
        RangeAccuracy = CalculateFinalStat(data.range, StatBonusType.RangeAccuracy);
        RepairSpeed = CalculateFinalStat(data.repair, StatBonusType.RepairSpeed);
        HealPower = CalculateFinalStat(data.medic, StatBonusType.HealSpeed);   // 의술
        MaxFaith = CalculateFinalStat(data.will, StatBonusType.MentalValue); // 의지(최대치)
        FaithRegenAmount = CalculateFinalStat(data.faith, StatBonusType.MentalHealAmount); // 신앙(회복량)

        // [무기 체크] 무기가 없으면 기본 근접 수치 적용
        if (currentWeapon != null)
        {
            AttackRange = currentWeapon.attackRange;
            AttackSpeed = 1f + currentWeapon.attackSpeedBonus;
        }
        else
        {
            AttackRange = 2f; // 무기 없을 때 기본 사거리 (근접)
            AttackSpeed = 1.0f; // 무기 없을 때 기본 공격 속도
        }

        // [방어구 체크] 헬름이나 갑옷이 없으면 0으로 처리 (Null 조건 연산자 사용)
        float helmDef = currentHelm ? currentHelm.defense : 0f;
        float chestDef = currentChest ? currentChest.defense : 0f;
        Defense = helmDef + chestDef;

        if (CurrentHp <= 0) CurrentHp = MaxHp;
        if (currentFaith <= 0) currentFaith = MaxFaith;
    }


    private void HandleFaithRegen()
    {
        _faithRegenTimer += Time.deltaTime;
        if (_faithRegenTimer >= 3f) // 3초에 1회 회복 (이미지 기준)
        {
            _faithRegenTimer = 0f;

            float finalRegen = FaithRegenAmount;

            GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
            if (baseObj != null && Vector2.Distance(transform.position, baseObj.transform.position) < 3f)
            {
                finalRegen *= 3f;
            }

            RecoverFaith(FaithRegenAmount); // 내 신앙(MTL_SPEED)만큼 회복

            // 주변 아군 전파 회복
            Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, 5f);
            foreach (var col in allies)
            {
                if (col.CompareTag("Unit") && col.gameObject != gameObject)
                    col.GetComponent<UnitStat>()?.RecoverFaith(FaithRegenAmount);
            }
        }
    }

    public void RecoverFaith(float amount)
    {
        if (currentFaith >= MaxFaith) return;

        currentFaith = Mathf.Min(currentFaith + amount, MaxFaith);

        if (isPanicking)
        {
            if (currentFaith >= MaxFaith * 0.5f)
            {
                StopPanic();
            }
        }

        UpdateStatusUI();
    }

    // 신앙 감소 로직
    public void OnHit() // 공격 받을 때 호출
    {
        ReduceFaith(5f);
    }

    public void OnAllyDeath() // 주변 아군 사망 시 호출
    {
        ReduceFaith(20f);
    }

    private void ReduceFaith(float amount)
    {
        if (isPanicking) return; // 이미 패닉이면 무시

        currentFaith = Mathf.Max(currentFaith - amount, 0);

        // 바로 이 부분에서 호출됩니다!
        if (currentFaith <= 0)
        {
            StartPanic();
        }

        UpdateStatusUI();
    }

    // 패닉 상태 시작
    private void StartPanic()
    {
        if (isPanicking) return;
        isPanicking = true;
        //Debug.Log($"{gameObject.name} 패닉 상태!");

        // 공격 불가
        if (_unitCombat != null) _unitCombat.enabled = false;

        // 타겟 지정 불가
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Base 근처 Y좌표로 강제 이동 (Base 위치를 찾아 이동)
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null)
        {
            Vector3 targetPos = new Vector3(transform.position.x, baseObj.transform.position.y, 0);
            _agent.SetDestination(targetPos);
            _agent.speed = baseSpeed * 1.5f; // 도망갈 땐 조금 더 빠르게
        }

        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkFaithBar());
    }

    private void StopPanic()
    {
        isPanicking = false;
        //Debug.Log($"{gameObject.name} 패닉 해제!");

        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        if (_faithBGImage != null) _faithBGImage.color = _originalBGColor;

        // 공격 가능 상태 복구
        if (_unitCombat != null) _unitCombat.enabled = true;

        // 타겟 지정 가능 복구: 원래 레이어로 복구
        gameObject.layer = _originalLayer;

        // 이동 중지 및 속도 복구
        if (_agent != null)
        {
            _agent.ResetPath();
            _agent.speed = baseSpeed;
        }
    }

    private IEnumerator BlinkFaithBar()
    {
        while (isPanicking)
        {
            if (_faithBGImage != null)
            {
                _faithBGImage.color = (_faithBGImage.color == _originalBGColor) ? panicColor : _originalBGColor;
            }
            yield return new WaitForSeconds(0.3f);
        }
        if (_faithBGImage != null) _faithBGImage.color = _originalBGColor;
    }

    /// <summary>
    /// 스탯을 기반으로 보너스 계산하는 메서드
    /// </summary>
    /// <param name="baseValue">기존 스탯 Value</param>
    /// <param name="type">스탯의 종류</param>
    /// <returns></returns>
    private float CalculateFinalStat(float baseValue, StatBonusType type)
    {
        // 1. 레벨 합산 (유닛 레벨 + 장비 보너스)
        int totalLevel = 0;
        switch (type)
        {
            case StatBonusType.Hp: totalLevel = data.hp; break;
            case StatBonusType.AttackPower: totalLevel = data.melee; break;
            case StatBonusType.RangeAccuracy: totalLevel = data.range; break;
            case StatBonusType.RepairSpeed: totalLevel = data.repair; break;
            case StatBonusType.HealSpeed: totalLevel = data.medic; break;
            case StatBonusType.MentalValue: totalLevel = data.will; break;
            case StatBonusType.MentalHealAmount: totalLevel = data.faith; break;
        }

        // 장비 보너스 레벨 합산
        totalLevel += GetEquipmentBonusLevel(type);
        totalLevel = Mathf.Clamp(totalLevel, 0, 10);

        // 2. 이미지의 상승 수치 로직 대입
        switch (type)
        {
            case StatBonusType.Hp: return 80f + (totalLevel * 5f);
            case StatBonusType.AttackPower: return 20f + (totalLevel * 1f);
            case StatBonusType.RangeAccuracy: return 0.15f + (totalLevel * 0.08f);
            case StatBonusType.RepairSpeed: return 0.05f + (totalLevel * 0.01375f);
            case StatBonusType.HealSpeed: return 0.1f + (totalLevel * 0.09f);   // 의술
            case StatBonusType.MentalValue: return 30f + (totalLevel * 2f);    // 의지(MTL_MAX)
            case StatBonusType.MentalHealAmount: return 0.5f + (totalLevel * 0.5f); // 신앙(MTL_SPEED)
            default: return 1f;
        }
    }

    private int GetEquipmentBonusLevel(StatBonusType type)
    {
        int bonus = 0;
        bonus += CheckBonus(currentWeapon, type);
        bonus += CheckBonus(currentHelm, type);
        bonus += CheckBonus(currentChest, type);
        return bonus;
    }

    private int CheckBonus(EquipmentData eq, StatBonusType type)
    {
        if (eq == null || eq.additionalStatBonuses == null) return 0;

        // 해당 타입의 보너스가 존재하는지 먼저 확인
        if (eq.additionalStatBonuses.Exists(x => x.type == type))
        {
            // 존재한다면 해당 항목을 찾아 bonusLevel 반환
            return eq.additionalStatBonuses.Find(x => x.type == type).bonusLevel;
        }

        return 0;
    }

    /// <summary>
    /// 대미지 입는 메서드
    /// </summary>
    /// <param name="rawDamage">대미지 감소 전 Raw 값</param>
    /// <param name="attackerPos">공격자 위치</param>
    public void TakeDamage(float rawDamage, Vector2 attackerPos)
    {
        // 현재 대미지 감소 로직
        // Raw 대미지 값 - Defense(방어력) 값
        float finalDamage = Mathf.Max(rawDamage - Defense, 0.1f);
        CurrentHp -= finalDamage;
        //Debug.Log($"유닛 남은 체력: {CurrentHp}");
        
        OnHit();

        // 넉백 실행
        if (gameObject.activeSelf && !_isKnockbacking)
        {
            StartCoroutine(KnockbackRoutine(attackerPos));
        }

        UpdateStatusUI();

        if (CurrentHp <= 0) Die();
    }

    /// <summary>
    /// 넉백 코루틴
    /// </summary>
    /// <param name="attackerPos">공격자 위치</param>
    /// <returns></returns>
    private IEnumerator KnockbackRoutine(Vector2 attackerPos)
    {
        if (visualChild == null) yield break; // 자식 오브젝트 없으면 리턴

        _isKnockbacking = true;

        // 방향 계산 (공격자로부터 반대 방향)
        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
        Vector3 startPos = Vector3.zero; // 로컬 위치이므로 0
        Vector3 targetPos = new Vector3(dir.x, dir.y, 0) * knockbackDistance;

        float elapsed = 0f; // 타이머 초기화

        // 뒤로 밀리기
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (knockbackDuration * 0.5f);
            visualChild.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        elapsed = 0f; // 타이머 초기화

        // 제자리로 복귀
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (knockbackDuration * 0.5f);
            visualChild.localPosition = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }

        visualChild.localPosition = startPos; // 위치 초기화
        _isKnockbacking = false;
    }

    public void UpdateStatusUI()
    {
        if (hpSlider != null) hpSlider.value = CurrentHp / MaxHp;
        if (faithSlider != null) faithSlider.value = currentFaith / MaxFaith;
    }

    public void UpdateEquipmentVisuals(UnitData data, GameObject unitGo)
    {
        // 자식 오브젝트에서 각 부위의 SpriteRenderer를 찾음
        SpriteRenderer helmSR = unitGo.transform.Find("Visual/Helm")?.GetComponent<SpriteRenderer>();
        SpriteRenderer chestSR = unitGo.transform.Find("Visual/Chest")?.GetComponent<SpriteRenderer>();
        SpriteRenderer weaponSR = unitGo.transform.Find("Visual/Weapon")?.GetComponent<SpriteRenderer>();

        // 장착된 아이템이 있다면 스프라이트 적용, 없으면 투명하게
        if (helmSR != null) helmSR.sprite = data.equippedHelm?.equipSprite;
        if (chestSR != null) chestSR.sprite = data.equippedChest?.equipSprite;
        if (weaponSR != null) weaponSR.sprite = data.equippedWeapon?.equipSprite;
    }

    public void RecoverHP(float amount)
    {
        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        UpdateStatusUI(); // 이전에 만든 UI 갱신 로직
    }
    private void Die()
    {
        // [추가] 주변 아군들에게 정신적 충격 전달
        Collider2D[] nearbyAllies = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var col in nearbyAllies)
        {
            // 나 자신이 아니고, 태그가 Unit인 아군 유닛만 탐색
            if (col.gameObject != gameObject && col.CompareTag("Unit"))
            {
                UnitStat allyStat = col.GetComponent<UnitStat>();
                if (allyStat != null)
                {
                    // 주변 아군의 OnAllyDeath 호출
                    allyStat.OnAllyDeath();
                }
            }
        }
        SoundManager.Instance.PlaySFX(SoundManager.Instance.maleDeathSound);

        //Debug.Log($"{gameObject.name} 사망. 주변 아군 의지 감소.");
        gameObject.SetActive(false);
    }
}