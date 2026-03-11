using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UnitInventorySlot : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Button clickButton;

    public void Setup(UnitData data, Action onClickAction)
    {
        nameText.text = data.unitName;
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onClickAction.Invoke());
    }
}