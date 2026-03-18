using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EquipmentInventoryUI : MonoBehaviour
{
    [Header("List Settings")]
    public GameObject equipmentSlotPrefab; // 장비 항목 프리팹
    public Transform listParent;          // Scroll View의 Content

    [Header("Detail Panel")]
    public TextMeshProUGUI equipNameText;
    public TextMeshProUGUI baseStatText;  // 사거리 또는 방어력
    public TextMeshProUGUI bonusStatText; // 무작위 보너스들
    public Image equipLargeImage;         // 중앙 장비 이미지

    private EquipmentType _currentTab = EquipmentType.Melee; // 기본 탭: 무기

    private void Start()
    {
        ClearDetailPanel();

        // 첫 화면은 무기 탭으로 시작
        ChangeTab((int)EquipmentType.Melee);
    }

    // 탭 버튼에서 호출할 함수
    public void ChangeTab(int typeIndex)
    {
        _currentTab = (EquipmentType)typeIndex;
        RefreshList();
    }

    public void RefreshList()
    {
        if (listParent == null) return;

        foreach (Transform child in listParent) Destroy(child.gameObject);

        if (InventoryManager.Instance == null) return;

        List<EquipmentData> allEquips = InventoryManager.Instance.myEquipments;

        foreach (var equip in allEquips)
        {
            // 탭 필터링 로직
            if (_currentTab == EquipmentType.Melee)
            {
                if (equip.type != EquipmentType.Melee && equip.type != EquipmentType.Bow) continue;
            }
            else
            {
                if (equip.type != _currentTab) continue;
            }

            GameObject slot = Instantiate(equipmentSlotPrefab, listParent);
            EquipmentInventorySlot slotScript = slot.GetComponent<EquipmentInventorySlot>();

            if (slotScript != null)
            {
                slotScript.Setup(equip, () => SelectEquipment(equip));
            }
        }
    }

    public void SelectEquipment(EquipmentData data)
    {
        if (data == null)
        {
            ClearDetailPanel();
            return;
        }

        equipNameText.text = data.equipName;

        // 기본 수치 표시 (사거리 또는 방어력)
        if (data.type == EquipmentType.Melee || data.type == EquipmentType.Bow)
            baseStatText.text = $"사거리: {data.attackRange}";
        else
            baseStatText.text = $"방어력: {data.defense}";

        // --- 보너스 스탯 표시 (한글 매핑 적용) ---
        string bonus = "";
        foreach (var b in data.additionalStatBonuses)
        {
            // Enum 타입을 한글 명칭으로 변환하여 추가
            string korName = GetStatKoreanName(b.type);
            bonus += $"{korName}: +{b.bonusLevel}\n";
        }
        bonusStatText.text = bonus;

        if (equipLargeImage != null)
        {
            if (data.equipSprite != null)
            {
                equipLargeImage.sprite = data.equipSprite;
                equipLargeImage.enabled = true;
                equipLargeImage.color = Color.white;
            }
            else
            {
                equipLargeImage.sprite = null;
                equipLargeImage.enabled = false;
            }
        }
    }

    /// <summary>
    /// StatBonusType Enum을 기획서상의 한글 명칭으로 매핑합니다.
    /// </summary>
    private string GetStatKoreanName(StatBonusType type)
    {
        switch (type)
        {
            case StatBonusType.Hp: return "체력";
            case StatBonusType.AttackPower: return "격투";
            case StatBonusType.RangeAccuracy: return "사격";
            case StatBonusType.RepairSpeed: return "수리";
            case StatBonusType.HealSpeed: return "의료";
            case StatBonusType.MentalValue: return "의지";
            case StatBonusType.MentalHealAmount: return "신앙";
            default: return type.ToString(); // 혹시 모를 예외 시 영어 이름 출력
        }
    }

    private void ClearDetailPanel()
    {
        equipNameText.text = "";
        baseStatText.text = "";
        bonusStatText.text = "";
        if (equipLargeImage != null)
        {
            equipLargeImage.sprite = null;
            equipLargeImage.enabled = false;
        }
    }
}