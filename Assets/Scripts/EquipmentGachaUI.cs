using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EquipmentGachaUI : MonoBehaviour
{
    [Header("Result Texts")]
    public TextMeshProUGUI gradeText;     // 등급 (최상급, 상급 등)
    public TextMeshProUGUI nameText;      // 장비 이름
    public TextMeshProUGUI baseStatText;  // 기본 수치 (사거리 또는 방어력)
    public TextMeshProUGUI bonusStatText; // 보너스 스탯 목록

    [Header("Buttons")]
    public Button drawButton;
    public Button confirmButton;

    // 버튼 상태 제어
    public void SetButtonState(bool draw, bool confirm)
    {
        if (drawButton) drawButton.interactable = draw;
        if (confirmButton) confirmButton.interactable = confirm;
    }

    // 장비 정보 표시
    public void DisplayEquipment(EquipmentData item, string grade)
    {
        gradeText.text = $"등급: {grade}";
        nameText.text = item.equipName;

        // 기본 수치 (무기면 사거리, 방어구면 방어력)
        if (item.type == EquipmentType.Melee || item.type == EquipmentType.Bow)
            baseStatText.text = $"기본 사거리: {item.attackRange}";
        else
            baseStatText.text = $"기본 방어력: {item.defense}";

        // 보너스 스탯 정리
        string bonus = "보너스 스탯:\n";
        foreach (var b in item.additionalStatBonuses)
        {
            bonus += $"- {b.type}: +{b.bonusLevel}\n";
        }
        bonusStatText.text = bonus;
    }

    public void ClearDisplay()
    {
        gradeText.text = "-";
        nameText.text = "장비를 뽑아주세요";
        baseStatText.text = "-";
        bonusStatText.text = "-";
    }
}