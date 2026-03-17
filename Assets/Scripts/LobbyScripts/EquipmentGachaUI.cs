using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EquipmentGachaUI : MonoBehaviour
{
    [Header("Result Texts")]
    public TextMeshProUGUI equipmentInfoText; // 이름, 등급, 기본 스탯 통합
    public TextMeshProUGUI bonusStatText;    // 보너스 스탯 리스트

    // 장비 정보 표시
    public void DisplayEquipment(EquipmentData item, string grade)
    {
        if (item == null) return;

        // 1. 기본 정보 및 주 능력치 설정
        string mainStatInfo = "";
        if (item.type == EquipmentType.Melee || item.type == EquipmentType.Bow)
        {
            mainStatInfo = $"사거리: {item.attackRange}\n공속 보너스: {item.attackSpeedBonus}";
        }
        else // Helm, Chest
        {
            mainStatInfo = $"방어력: {item.defense}";
        }

        equipmentInfoText.text =
            $"이름: {item.equipName}\n" +
            $"등급: {grade}\n" +
            $"{mainStatInfo}";

        // 2. 보너스 스탯 한국어 치환 및 리스트 생성
        string bonusContent = "보너스 스탯:\n";
        if (item.additionalStatBonuses == null || item.additionalStatBonuses.Count == 0)
        {
            bonusContent += "- 없음";
        }
        else
        {
            foreach (var b in item.additionalStatBonuses)
            {
                bonusContent += $"- {GetStatKoreanName(b.type)} +{b.bonusLevel}\n";
            }
        }
        bonusStatText.text = bonusContent;
    }

    // StatBonusType을 기획서 양식에 맞는 한국어 이름으로 치환
    private string GetStatKoreanName(StatBonusType type)
    {
        return type switch
        {
            StatBonusType.Hp => "체력",
            StatBonusType.AttackPower => "격투",
            StatBonusType.RangeAccuracy => "사격",
            StatBonusType.RepairSpeed => "수리",
            StatBonusType.HealSpeed => "의술",
            StatBonusType.MentalValue => "의지",
            StatBonusType.MentalHealAmount => "신앙",
            _ => type.ToString()
        };
    }

    public void ClearDisplay()
    {
        equipmentInfoText.text = "이름: -\n등급: -\n스탯: -";
        bonusStatText.text = "보너스 스탯:\n-";
    }
}