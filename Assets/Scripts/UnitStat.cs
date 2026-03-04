using UnityEngine;
using System.Collections.Generic;

public class UnitStat : MonoBehaviour
{
    public UnitData data;
    public EquipmentData currentWeapon;
    public EquipmentData currentHelm;
    public EquipmentData currentChest;

    // 유닛 생성기에 의해 할당될 베이스 단계들 (기본 1단계 초기화)
    public Dictionary<StatBonusType, int> baseLevels = new Dictionary<StatBonusType, int>();

    [Range(1, 10)] public int baseStatLevel = 1; // 테스트용 통합 레벨

    public float MaxHp { get; private set; }
    public float CurrentHp { get; private set; }
    public float AttackPower { get; private set; }
    public float AttackRange { get; private set; }
    public float AttackSpeed { get; private set; }
    public float Defense { get; private set; }

    // 보조 스탯들
    public float RangeAccuracy { get; private set; }
    public float RepairSpeed { get; private set; }
    public float HealSpeed { get; private set; }
    public float MentalValue { get; private set; }

    private void Awake()
    {
        // 생성기로 생성되지 않았을 경우를 대비해 딕셔너리 초기화
        foreach (StatBonusType type in System.Enum.GetValues(typeof(StatBonusType)))
        {
            if (!baseLevels.ContainsKey(type)) baseLevels[type] = 1;
        }
        RefreshStats();
    }

    public void RefreshStats()
    {
        if (data == null) return;

        MaxHp = CalculateFinalStat(data.baseHp, StatBonusType.Hp);
        AttackPower = CalculateFinalStat(data.baseAttackPower, StatBonusType.AttackPower);
        RangeAccuracy = CalculateFinalStat(data.baseRangeAccuracy, StatBonusType.RangeAccuracy);
        RepairSpeed = CalculateFinalStat(data.baseRepairSpeed, StatBonusType.RepairSpeed);
        HealSpeed = CalculateFinalStat(data.baseHealSpeed, StatBonusType.HealSpeed);
        MentalValue = CalculateFinalStat(data.baseMentalValue, StatBonusType.MentalValue);

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

        // [수정] 아무리 합산되어도 10단계를 넘기지 않음
        totalLevel = Mathf.Clamp(totalLevel, 1, 10);

        float multiplier = 1f + (totalLevel - 1) * data.upgradeMultiplier;
        return baseValue * multiplier;
    }

    public void TakeDamage(float rawDamage)
    {
        float finalDamage = Mathf.Max(rawDamage - Defense, 1f);
        CurrentHp -= finalDamage;
        Debug.Log("Hit!");
        if (CurrentHp <= 0) Die();
    }

    private void Die() => gameObject.SetActive(false);
}