using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType { Melee, Bow, Helm, Chest } // 근거리, 원거리, 헬름, 갑옷

// 무기가 강화해줄 수 있는 스탯 종류
public enum StatBonusType { Hp, AttackPower, RangeAccuracy, RepairSpeed, HealSpeed, MentalValue }

[System.Serializable]
public struct StatBonus
{
    public StatBonusType type;
    [Range(1, 3)] public int bonusLevel; // 이 무기가 더해줄 단계 (예: 1~3단계)
}

[CreateAssetMenu(fileName = "EquipmentData", menuName = "Scriptable Objects/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    public string equipName;
    public EquipmentType type;

    [Header("Combat Stats")]
    public float attackSpeedBonus = 0f; // 기본 1에서 가감
    public float attackRange = 1.5f;    // 무기별 사거리

    [Header("Defense Stats")]
    public float defense = 0f;          // 헬름/갑옷 공통 방어력 수치

    [Header("Random Stat Bonuses")]
    // 이 리스트에 담긴 스탯이 유닛에 합산
    public List<StatBonus> additionalStatBonuses;
}
