using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class EquipmentInventorySlot : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Button clickButton;

    public void Setup(EquipmentData data, Action onSelect)
    {
        nameText.text = data.equipName;
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onSelect());
    }
}