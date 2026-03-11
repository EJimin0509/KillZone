using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitGachaUI : MonoBehaviour
{
    [Header("Stat Texts")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI meleeText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI repairText;
    public TextMeshProUGUI medicText;
    public TextMeshProUGUI willText;
    public TextMeshProUGUI faithText;

    [Header("Buttons")]
    public Button rerollButton;
    public Button drawButton;
    public Button confirmButton;

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
        _currentGeneratedUnit = unit;

        // 텍스트 반영
        hpText.text = unit.hp.ToString();
        meleeText.text = unit.melee.ToString();
        rangeText.text = unit.range.ToString();
        repairText.text = unit.repair.ToString();
        medicText.text = unit.medic.ToString();
        willText.text = unit.will.ToString();
        faithText.text = unit.faith.ToString();
    }

    // 텍스트 초기화
    public void ClearDisplay()
    {
        hpText.text = "0";
        meleeText.text = "0";
        rangeText.text = "0";
        repairText.text = "0";
        medicText.text = "0";
        willText.text = "0";
        faithText.text = "0";
    }
}