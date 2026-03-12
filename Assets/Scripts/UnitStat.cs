using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    // 유닛 생성기에 의해 할당될 베이스 단계들 (기본 1단계 초기화)
    public Dictionary<StatBonusType, int> baseLevels = new Dictionary<StatBonusType, int>();

    private bool _isKnockbacking = false;                // 지금 넉백 중인지 판단
    
    [Range(1, 10)] public int baseStatLevel = 1; // 테스트용 통합 레벨

    public float MaxHp { get; private set; }             // 최대 HP
    public float CurrentHp { get; private set; }         // 현재 HP
    public float AttackPower { get; private set; }       // 공격력
    public float AttackRange { get; private set; }       // 공격 범위
    public float AttackSpeed { get; private set; }       // 공격 속도
    public float Defense { get; private set; }           // 방어력

    // 보조 스탯들
    public float RangeAccuracy { get; private set; }     // 명중률
    public float RepairSpeed { get; private set; }       // 수리 속도
    public float HealSpeed { get; private set; }         // 치료 속도
    public float MentalValue { get; private set; }       // 정신력

    private void Awake()
    {
        // 생성기로 생성되지 않았을 경우를 대비해 딕셔너리 초기화
        foreach (StatBonusType type in System.Enum.GetValues(typeof(StatBonusType)))
        {
            if (!baseLevels.ContainsKey(type)) baseLevels[type] = 1;
        }
        RefreshStats(); // 스탯 초기화
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
        HealSpeed = CalculateFinalStat(data.medic, StatBonusType.HealSpeed);
        MentalValue = CalculateFinalStat(data.faith, StatBonusType.MentalValue);

        // [무기 체크] 무기가 없으면 기본 근접 수치 적용
        if (currentWeapon != null)
        {
            AttackRange = currentWeapon.attackRange;
            AttackSpeed = 1f + currentWeapon.attackSpeedBonus;
        }
        else
        {
            AttackRange = 1.2f; // 무기 없을 때 기본 사거리 (근접)
            AttackSpeed = 1.0f; // 무기 없을 때 기본 공격 속도
        }

        // [방어구 체크] 헬름이나 갑옷이 없으면 0으로 처리 (Null 조건 연산자 사용)
        float helmDef = currentHelm ? currentHelm.defense : 0f;
        float chestDef = currentChest ? currentChest.defense : 0f;
        Defense = helmDef + chestDef;

        if (CurrentHp <= 0) CurrentHp = MaxHp;
    }

    /// <summary>
    /// 스탯을 기반으로 보너스 계산하는 메서드
    /// </summary>
    /// <param name="baseValue">기존 스탯 Value</param>
    /// <param name="type">스탯의 종류</param>
    /// <returns></returns>
    private float CalculateFinalStat(float baseValue, StatBonusType type)
    {
        // 1. 유닛의 베이스 단계 가져오기
        int totalLevel = baseLevels.ContainsKey(type) ? baseLevels[type] : baseStatLevel;

        // 2. 무기 보너스 합산
        if (currentWeapon != null && currentWeapon.additionalStatBonuses != null)
        {
            var bonus = currentWeapon.additionalStatBonuses.Find(b => b.type == type);
            totalLevel += bonus.bonusLevel;
        }

        // 무리 합산되어도 10단계를 넘기지 않음
        totalLevel = Mathf.Clamp(totalLevel, 1, 10);

        //float multiplier = 1f + (totalLevel - 1) * data.upgradeMultiplier;
        float multiplier = 1f; // 임시
        return baseValue * multiplier;
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
        float finalDamage = Mathf.Max(rawDamage - Defense, 1f);
        CurrentHp -= finalDamage;
        Debug.Log($"유닛 남은 체력: {CurrentHp}");

        // 넉백 실행
        if (gameObject.activeSelf && !_isKnockbacking)
        {
            StartCoroutine(KnockbackRoutine(attackerPos));
        }

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

    private void Die() => gameObject.SetActive(false);
}