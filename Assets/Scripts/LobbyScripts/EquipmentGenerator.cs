using UnityEngine;
using System.Collections.Generic;

public class EquipmentGenerator : MonoBehaviour
{
    [Header("Default Sprites")]
    public Sprite helmSprite;
    public Sprite chestSprite;
    public Sprite meleeSprite;
    public Sprite bowSprite;

    public static EquipmentGenerator Instance;

    // 기획서 20P 사양 정의
    private struct EquipmentSpec
    {
        public string name;
        public int bonusCount; // 무작위 스탯 개수
        public int maxLevel; // 0 ~ n 증가 수치
        public float baseValue; // 무기: 사거리 / 방어구: 방어력
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
            new EquipmentSpec { name = "단검", bonusCount = 1, maxLevel = 1, baseValue = 1.2f },
            new EquipmentSpec { name = "롱소드", bonusCount = 1, maxLevel = 2, baseValue = 1.5f },
            new EquipmentSpec { name = "워해머", bonusCount = 2, maxLevel = 1, baseValue = 1.5f },
            new EquipmentSpec { name = "그레이트소드", bonusCount = 2, maxLevel = 2, baseValue = 2f }
        }},
        { EquipmentType.Bow, new List<EquipmentSpec> {
            new EquipmentSpec { name = "숏보우", bonusCount = 1, maxLevel = 1, baseValue = 5f },
            new EquipmentSpec { name = "롱보우", bonusCount = 1, maxLevel = 2, baseValue = 8f },
            new EquipmentSpec { name = "크로스보우", bonusCount = 2, maxLevel = 1, baseValue = 10f },
            new EquipmentSpec { name = "헤비 크로스보우", bonusCount = 2, maxLevel = 2, baseValue = 12f }
        }}
    };

    public EquipmentData GenerateRandomEquipment()
    {
        // 1. 부위 랜덤 (Melee=0, Bow=1, Helm=2, Chest=3)
        List<EquipmentType> availableTypes = new List<EquipmentType>(_itemPool.Keys);
        EquipmentType randomType = availableTypes[Random.Range(0, availableTypes.Count)];

        // 해당 부위 풀 확인
        if (!_itemPool.ContainsKey(randomType) || _itemPool[randomType].Count == 0)
        {
            Debug.LogError($"{randomType} 타입의 장비 풀이 비어있습니다! Dictionary 설정을 확인하세요.");
            return null;
        }

        List<EquipmentSpec> specs = _itemPool[randomType];
        EquipmentSpec selectedSpec = specs[Random.Range(0, specs.Count)];

        // 2. ScriptableObject 생성
        EquipmentData newItem = ScriptableObject.CreateInstance<EquipmentData>();
        newItem.equipName = selectedSpec.name;
        newItem.type = randomType;
        newItem.additionalStatBonuses = new List<StatBonus>();

        switch (randomType)
        {
            case EquipmentType.Helm:
                newItem.equipSprite = helmSprite;
                newItem.spriteKey = helmSprite.name; // [추가] 이름 저장
                break;
            case EquipmentType.Chest:
                newItem.equipSprite = chestSprite;
                newItem.spriteKey = chestSprite.name; // [추가]
                break;
            case EquipmentType.Melee:
                newItem.equipSprite = meleeSprite;
                newItem.spriteKey = meleeSprite.name; // [추가]
                break;
            case EquipmentType.Bow:
                newItem.equipSprite = bowSprite;
                newItem.spriteKey = bowSprite.name; // [추가]
                break;
        }

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