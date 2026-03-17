using UnityEngine;
using UnityEngine.UI;
using System;

public class UnitInventorySlot : MonoBehaviour
{
    public Image unitIcon; // 유닛 아이콘 (UI Image)
    public Button clickButton;

    public void Setup(UnitData data, Action onClickAction)
    {
        if (unitIcon != null)
        {
            if (data.unitSprite != null)
            {
                unitIcon.sprite = data.unitSprite;
                unitIcon.enabled = true;
            }
            else
            {
                unitIcon.enabled = false; // 슬롯 아이콘이 없을 때 처리
            }
        }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onClickAction.Invoke());
    }
}