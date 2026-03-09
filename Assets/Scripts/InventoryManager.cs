using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory Lists")]
    public List<UnitData> myUnits = new List<UnitData>();
    public List<EquipmentData> myEquipments = new List<EquipmentData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 뽑은 용병 추가
    public void AddUnit(UnitData unit)
    {
        myUnits.Add(unit);
        Debug.Log($"보관함에 용병 추가: {unit.unitName}");
    }

    // 뽑은 장비 추가
    public void AddEquipment(EquipmentData equipment)
    {
        myEquipments.Add(equipment);
        Debug.Log($"보관함에 장비 추가: {equipment.equipName}");
    }
}