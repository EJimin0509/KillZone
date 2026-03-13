using UnityEngine;
using UnityEngine.UI;
using System;

public class EquipmentInventorySlot : MonoBehaviour
{
    public Image iconImage; // 장비 아이콘 표시용
    public Button clickButton;

    public void Setup(EquipmentData data, Action onSelect)
    {
        // 아이콘 설정
        if (iconImage != null && data.equipSprite != null)
        {
            iconImage.sprite = data.equipSprite;
            iconImage.color = Color.white;
        }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onSelect());
    }
}