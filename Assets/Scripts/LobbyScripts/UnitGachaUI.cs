using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitGachaUI : MonoBehaviour
{
    //[Header("Stat Texts")]
    //public TextMeshProUGUI hpText;
    //public TextMeshProUGUI meleeText;
    //public TextMeshProUGUI rangeText;
    //public TextMeshProUGUI repairText;
    //public TextMeshProUGUI medicText;
    //public TextMeshProUGUI willText;
    //public TextMeshProUGUI faithText;

    [Header("Buttons")]
    public Button rerollButton;
    public Button drawButton;
    public Button confirmButton;

    [Header("Stat Texts")]
    public TextMeshProUGUI unitNameText; // 이름 표시용 추가
    public TextMeshProUGUI totalStatText;

    private UnitData _currentGeneratedUnit;

    // 버튼 상태 일괄 제어
    public void SetButtonState(bool draw, bool reroll, bool confirm)
    {
        if (drawButton) drawButton.interactable = draw;
        if (rerollButton) rerollButton.interactable = reroll;
        if (confirmButton) confirmButton.interactable = confirm;
    }

    // 뽑기 로직 실행 후 호출될 함수
    public void DisplayUnitStats(UnitData unit)
    {
        if (unit == null) return;

        // 이름 및 전체 스탯 출력 양식
        unitNameText.text = $"이름: {unit.unitName}";

        totalStatText.text =
            $"체력: {unit.hp}\n" +
            $"격투: {unit.melee}\n" +
            $"사격: {unit.range}\n" +
            $"수리: {unit.repair}\n" +
            $"의술: {unit.medic}\n" +
            $"의지: {unit.will}\n" +
            $"신앙: {unit.faith}";
    }

    // 텍스트 초기화
    public void ClearDisplay()
    {
        unitNameText.text = "이름: -";
        totalStatText.text = "체력: 0\n격투: 0\n사격: 0\n수리: 0\n의술: 0\n의지: 0\n신앙: 0";
    }
}