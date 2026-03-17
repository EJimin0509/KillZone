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
        if (iconImage != null)
        {
            if (data.equipSprite != null)
            {
                iconImage.sprite = data.equipSprite;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false; // 슬롯 아이콘이 없을 때 처리
            }
        }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onSelect());
    }
}