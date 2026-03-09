using UnityEngine;
using System.Collections.Generic;

public class FormationManager : MonoBehaviour
{
    public static FormationManager Instance;

    [Header("Formation Settings")]
    [SerializeField] private int maxSlotCount = 5;
    public UnitData[] formationSlots = new UnitData[5]; // 0~4번 슬롯

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

    // 특정 슬롯에 용병 배치
    public bool SetUnitToSlot(int slotIndex, UnitData unit)
    {
        if (slotIndex < 0 || slotIndex >= maxSlotCount) return false;

        // 이미 다른 슬롯에 편성된 유닛인지 체크 (중복 방지)
        for (int i = 0; i < formationSlots.Length; i++)
        {
            if (formationSlots[i] == unit)
            {
                formationSlots[i] = null; // 기존 슬롯 해제
            }
        }

        formationSlots[slotIndex] = unit;
        Debug.Log($"{slotIndex + 1}번 슬롯에 {unit.unitName} 배치 완료");
        return true;
    }

    // 슬롯 비우기
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxSlotCount)
            formationSlots[slotIndex] = null;
    }

    // 현재 편성된 용병 수 확인
    public int GetActiveUnitCount()
    {
        int count = 0;
        foreach (var unit in formationSlots)
        {
            if (unit != null) count++;
        }
        return count;
    }
}