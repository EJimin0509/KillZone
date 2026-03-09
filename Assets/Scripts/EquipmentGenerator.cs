using UnityEngine;
using System.Collections.Generic;

public class EquipmentGenerator : MonoBehaviour
{
    // 기획서 20P 사양 정의
    private struct EquipmentSpec
    {
        public string name;
        public int bonusCount;
        public int maxLevel;
        public float baseValue; // 사거리 또는 방어력
    }

    private Dictionary<EquipmentType, List<EquipmentSpec>> _itemPool = new Dictionary<EquipmentType, List<EquipmentSpec>>()
    {
        { EquipmentType.Helm, new List<EquipmentSpec> {
            new EquipmentSpec { name = "가죽 헬름", bonusCount = 1, maxLevel = 1, baseValue = 1f },
            new EquipmentSpec { name = "사슬 코이프", bonusCount = 1, maxLevel = 2, baseValue = 2f },
            new EquipmentSpec { name = "그레이트헬름", bonusCount = 2, maxLevel = 1, baseValue = 3f },
            new EquipmentSpec { name = "주스팅 헬름", bonusCount = 2, maxLevel = 2, baseValue = 5f }
        }},
        { EquipmentType.Chest, new List<EquipmentSpec> {
            new EquipmentSpec { name = "가죽 아머", bonusCount = 1, maxLevel = 1, baseValue = 2f },
            new EquipmentSpec { name = "라멜러 아머", bonusCount = 1, maxLevel = 2, baseValue = 4f },
            new EquipmentSpec { name = "퀴레시어 아머", bonusCount = 2, maxLevel = 1, baseValue = 6f },
            new EquipmentSpec { name = "플레이트 아머", bonusCount = 2, maxLevel = 2, baseValue = 10f }
        }},
        { EquipmentType.Melee, new List<EquipmentSpec> {
            new EquipmentSpec { name = "브로드 소드", bonusCount = 1, maxLevel = 1, baseValue = 1.2f },
            new EquipmentSpec { name = "모닝스타", bonusCount = 2, maxLevel = 2, baseValue = 1.0f }
        }},
        { EquipmentType.Bow, new List<EquipmentSpec> {
            new EquipmentSpec { name = "컴포지트 보우", bonusCount = 1, maxLevel = 2, baseValue = 5.0f },
            new EquipmentSpec { name = "헤비 크로스보우", bonusCount = 2, maxLevel = 2, baseValue = 7.0f }
        }}
    };

    public EquipmentData GenerateRandomEquipment()
    {
        // 1. 부위 랜덤 (Melee=0, Bow=1, Helm=2, Chest=3)
        EquipmentType randomType = (EquipmentType)Random.Range(0, 4);

        List<EquipmentSpec> specs = _itemPool[randomType];
        EquipmentSpec selectedSpec = specs[Random.Range(0, specs.Count)];

        // 2. ScriptableObject 생성
        EquipmentData newItem = ScriptableObject.CreateInstance<EquipmentData>();
        newItem.equipName = selectedSpec.name;
        newItem.type = randomType;
        newItem.additionalStatBonuses = new List<StatBonus>();

        // 부위별 기본 수치 설정 (무기는 사거리, 방어구는 방어력)
        if (randomType == EquipmentType.Melee || randomType == EquipmentType.Bow)
            newItem.attackRange = selectedSpec.baseValue;
        else
            newItem.defense = selectedSpec.baseValue;

        // 3. 무작위 스탯 부여 (신앙 포함 7종)
        ApplyRandomBonuses(newItem, selectedSpec.bonusCount, selectedSpec.maxLevel);

        return newItem;
    }

    private void ApplyRandomBonuses(EquipmentData item, int count, int maxLevel)
    {
        // 새로 추가된 MentalHealAmount(신앙) 포함 총 7종
        List<StatBonusType> types = new List<StatBonusType> {
            StatBonusType.Hp, StatBonusType.AttackPower, StatBonusType.RangeAccuracy,
            StatBonusType.RepairSpeed, StatBonusType.HealSpeed, StatBonusType.MentalValue,
            StatBonusType.MentalHealAmount
        };

        for (int i = 0; i < count; i++)
        {
            if (types.Count == 0) break;

            int randIdx = Random.Range(0, types.Count);
            StatBonus bonus;
            bonus.type = types[randIdx];
            bonus.bonusLevel = Random.Range(0, maxLevel + 1);

            item.additionalStatBonuses.Add(bonus);
            types.RemoveAt(randIdx);
        }
    }
}