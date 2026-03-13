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
        // 첫 화면은 무기 탭으로 시작
        ChangeTab((int)EquipmentType.Melee);
    }

    // 탭 버튼(Weapon: 0, Helmet: 2, Armor: 3)에서 호출할 함수
    public void ChangeTab(int typeIndex)
    {
        _currentTab = (EquipmentType)typeIndex;
        RefreshList();
    }

    public void RefreshList()
    {
        if (listParent == null)
        {
            Debug.LogError("listParent(Content)가 비어있습니다!");
            return;
        }

        foreach (Transform child in listParent) Destroy(child.gameObject);

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager가 씬에 없습니다!");
            return;
        }

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

            if (equipmentSlotPrefab == null)
            {
                Debug.LogError("equipmentSlotPrefab이 할당되지 않았습니다!");
                break;
            }

            GameObject slot = Instantiate(equipmentSlotPrefab, listParent);
            EquipmentInventorySlot slotScript = slot.GetComponent<EquipmentInventorySlot>();

            if (slotScript != null)
            {
                slotScript.Setup(equip, () => SelectEquipment(equip));
            }
            else
            {
                Debug.LogError($"{equipmentSlotPrefab.name} 프리팹에 EquipmentInventorySlot 스크립트가 없습니다!");
            }
        }
    }

    public void SelectEquipment(EquipmentData data)
    {
        equipNameText.text = data.equipName;

        // 기본 수치 표시
        if (data.type == EquipmentType.Melee || data.type == EquipmentType.Bow)
            baseStatText.text = $"사거리: {data.attackRange}";
        else
            baseStatText.text = $"방어력: {data.defense}";

        // 보너스 스탯 표시
        string bonus = "";
        foreach (var b in data.additionalStatBonuses)
        {
            bonus += $"{b.type}: +{b.bonusLevel}\n";
        }
        bonusStatText.text = bonus;

        if (equipLargeImage != null)
        {
            if (data.equipSprite != null)
            {
                equipLargeImage.sprite = data.equipSprite;
                equipLargeImage.color = Color.white; // 불투명하게 설정
                equipLargeImage.enabled = true;
            }
            else
            {
                // 이미지가 없는 경우 투명하게 처리
                equipLargeImage.sprite = null;
                equipLargeImage.color = new Color(1, 1, 1, 0);
            }
        }
    }

}