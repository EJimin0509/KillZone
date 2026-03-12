using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
[System.Serializable]
public class UnitData : ScriptableObject
{
    [Header("Equipped Items")]
    public EquipmentData equippedHelm;
    public EquipmentData equippedChest;
    public EquipmentData equippedWeapon;

    [Header("외형 정보")]
    public string unitName;
    public Sprite unitSprite;

    [Header("Base Stat Levels (0-10)")]
    public int hp;      // 체력
    public int melee;   // 격투
    public int range;   // 사격
    public int repair;  // 수리
    public int medic;   // 의술
    public int will;    // 의지
    public int faith;   // 신앙

    public void SetDefault()
    {
        hp = 1; melee = 1; range = 0; repair = 1; medic = 0; will = 1; faith = 1;
    }

    /// <summary>
    /// 기본 레벨 + 장비 보너스 레벨을 합산하여 StatTable의 실제 수치를 반환
    /// </summary>
    public float GetFinalStat(StatBonusType type)
    {
        // 1. 유닛의 기본 레벨 가져오기
        int baseLevel = GetBaseLevel(type);

        // 2. 장착한 장비들로부터 보너스 레벨 합산
        int bonusLevel = 0;
        bonusLevel += GetEquipmentBonus(equippedHelm, type);
        bonusLevel += GetEquipmentBonus(equippedChest, type);
        bonusLevel += GetEquipmentBonus(equippedWeapon, type);

        // 3. 최종 레벨(최대 10) 결정 및 테이블 값 반환
        return StatTable.GetValue(type, baseLevel + bonusLevel);
    }

    private int GetEquipmentBonus(EquipmentData data, StatBonusType type)
    {
        if (data == null || data.additionalStatBonuses == null) return 0;

        int total = 0;
        foreach (var bonus in data.additionalStatBonuses)
        {
            if (bonus.type == type) total += bonus.bonusLevel;
        }
        return total;
    }

    private int GetBaseLevel(StatBonusType type)
    {
        return type switch
        {
            StatBonusType.Hp => this.hp,
            StatBonusType.AttackPower => this.melee,
            StatBonusType.RangeAccuracy => this.range,
            StatBonusType.RepairSpeed => this.repair,
            StatBonusType.HealSpeed => this.medic,
            StatBonusType.MentalValue => this.will,
            StatBonusType.MentalHealAmount => this.faith,
            _ => 0
        };
    }
}