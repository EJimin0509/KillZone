using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("보유 목록")]
    public List<UnitData> myUnits = new List<UnitData>();
    public List<EquipmentData> myEquipments = new List<EquipmentData>();

    [Header("스테이지 편성 (최대 5명)")]
    public UnitData[] formationSlots = new UnitData[5];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 1. 뽑기 결과 저장 (UI에서 '확정' 버튼 누를 때 호출)
    public void AddUnit(UnitData unit)
    {
        if (unit == null) return;
        myUnits.Add(unit);
        Debug.Log($"[Inventory] {unit.unitName} 저장 완료. 현재 보유 수: {myUnits.Count}");
    }

    public void AddEquipment(EquipmentData equip)
    {
        if (equip == null) return;
        myEquipments.Add(equip);
        Debug.Log($"[Inventory] {equip.equipName} 저장 완료.");
    }

    // 2. 스테이지 편성 로직
    public void SetFormation(int slotIndex, UnitData unit)
    {
        if (slotIndex < 0 || slotIndex >= 5) return;

        // 중복 편성 방지: 다른 슬롯에 이미 있으면 비움
        for (int i = 0; i < formationSlots.Length; i++)
        {
            if (formationSlots[i] == unit) formationSlots[i] = null;
        }

        formationSlots[slotIndex] = unit;
    }
}