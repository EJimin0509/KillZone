using UnityEngine;
using System.Collections.Generic;
using System;

public class EquipmentSelectPopup : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform contentParent;
    public GameObject popupRoot; // 팝업 전체 오브젝트

    private Action<EquipmentData> _onSelected;

    // 팝업 열기 (타입별 필터링)
    public void Open(EquipmentType filterType, Action<EquipmentData> onSelected)
    {
        _onSelected = onSelected;
        popupRoot.SetActive(true);

        // 기존 리스트 초기화
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // 인벤토리에서 해당 타입만 가져오기
        foreach (var equip in InventoryManager.Instance.myEquipments)
        {
            if (IsMatchingType(equip.type, filterType))
            {
                GameObject slot = Instantiate(slotPrefab, contentParent);
                slot.GetComponent<EquipmentInventorySlot>().Setup(equip, () => {
                    _onSelected?.Invoke(equip);
                    Close();
                });
            }
        }
    }

    private bool IsMatchingType(EquipmentType itemType, EquipmentType filter)
    {
        if (filter == EquipmentType.Melee)
            return itemType == EquipmentType.Melee || itemType == EquipmentType.Bow;
        return itemType == filter;
    }

    public void Close() => popupRoot.SetActive(false);
}