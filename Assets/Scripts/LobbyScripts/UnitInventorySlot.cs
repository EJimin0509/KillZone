using UnityEngine;
using UnityEngine.UI;
using System;

public class UnitInventorySlot : MonoBehaviour
{
    public Image unitIcon; // 유닛 아이콘 (UI Image)
    public Button clickButton;

    public void Setup(UnitData data, Action onClickAction)
    {
        if (unitIcon != null && data.unitSprite != null)
        {
            unitIcon.sprite = data.unitSprite;
            unitIcon.color = Color.white; // 투명도 방지
        }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onClickAction.Invoke());
    }
}