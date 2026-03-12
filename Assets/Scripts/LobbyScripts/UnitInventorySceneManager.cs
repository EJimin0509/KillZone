using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UnitInventorySceneManager : MonoBehaviour
{
    [Header("Left: Unit List")]
    public GameObject unitSlotPrefab;   // 리스트에 들어갈 항목 프리팹
    public Transform listParent;        // Scroll View의 Content

    [Header("Right: Detail Panel")]
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI[] statTexts; // HP, 격투, 사격, 수리, 의술, 의지, 신앙 순서
    public Image unitIllust;            // 용병 전신샷/초상화 이미지

    [Header("Equipment Buttons")]
    public Button helmButton;
    public Button chestButton;
    public Button weaponButton;
    public EquipmentSelectPopup selectPopup;

    public Button formationButton; // 전투 선택

    private UnitData _selectedUnit;

    private void Start()
    {
        RefreshUnitList();
        // 첫 번째 용병이 있다면 자동으로 선택
        if (InventoryManager.Instance.myUnits.Count > 0)
        {
            SelectUnit(InventoryManager.Instance.myUnits[0]);
        }

        // 버튼 리스너 연결
        helmButton.onClick.AddListener(() => selectPopup.Open(EquipmentType.Helm, (e) => EquipItem(e, 0)));
        chestButton.onClick.AddListener(() => selectPopup.Open(EquipmentType.Chest, (e) => EquipItem(e, 1)));
        weaponButton.onClick.AddListener(() => selectPopup.Open(EquipmentType.Melee, (e) => EquipItem(e, 2)));
    }

    // 용병 리스트에서 용병을 선택했을 때 호출
    public void OnSelectUnit(UnitData unit)
    {
        _selectedUnit = unit;
        RefreshEquipVisuals();
    }

    private void EquipItem(EquipmentData item, int slot)
    {
        if (_selectedUnit == null) return;

        if (slot == 0) _selectedUnit.equippedHelm = item;
        else if (slot == 1) _selectedUnit.equippedChest = item;
        else _selectedUnit.equippedWeapon = item;

        RefreshEquipVisuals();
    }

    private void RefreshEquipVisuals()
    {
        // 각 버튼의 Image나 Text를 _selectedUnit의 장착 데이터에 맞게 갱신하는 로직 추가
    }

    // 아군 리스트 생성 및 갱신
    public void RefreshUnitList()
    {
        // 1. 부모 오브젝트 체크
        if (listParent == null)
        {
            Debug.LogError("listParent(Content)가 할당되지 않았습니다!");
            return;
        }

        foreach (Transform child in listParent) Destroy(child.gameObject);

        // 2. 인벤토리 매니저 체크
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager를 찾을 수 없습니다!");
            return;
        }

        foreach (UnitData unit in InventoryManager.Instance.myUnits)
        {
            // 3. 프리팹 체크
            if (unitSlotPrefab == null)
            {
                Debug.LogError("unitSlotPrefab이 할당되지 않았습니다!");
                break;
            }

            GameObject slot = Instantiate(unitSlotPrefab, listParent);
            UnitInventorySlot slotScript = slot.GetComponent<UnitInventorySlot>();

            if (slotScript != null)
            {
                slotScript.Setup(unit, () => SelectUnit(unit));
            }
            else
            {
                Debug.LogError("프리팹에 UnitInventorySlot 스크립트가 없습니다!");
            }
        }
    }

    // 우측 상세창 정보 업데이트
    public void SelectUnit(UnitData unit)
    {
        _selectedUnit = unit;
        detailNameText.text = unit.unitName;

        // 스탯 배열 순서대로 매핑 (UnitData 구조에 맞춰서)
        statTexts[0].text = unit.hp.ToString();
        statTexts[1].text = unit.melee.ToString();
        statTexts[2].text = unit.range.ToString();
        statTexts[3].text = unit.repair.ToString();
        statTexts[4].text = unit.medic.ToString();
        statTexts[5].text = unit.will.ToString();
        statTexts[6].text = unit.faith.ToString();

        Debug.Log($"{unit.unitName} 상세 정보 표시 중");
    }

    public void OnFormationButtonClick()
    {
        if (_selectedUnit == null) return;

        // 빈 슬롯을 찾아 유닛을 넣습니다.
        bool success = false;
        for (int i = 0; i < FormationManager.Instance.formationSlots.Length; i++)
        {
            // 이미 배치된 유닛인지 확인 (중복 방지)
            if (FormationManager.Instance.formationSlots[i] == _selectedUnit)
            {
                Debug.Log("이미 배치된 유닛입니다.");
                return;
            }

            // 빈 자리가 있다면 배치
            if (FormationManager.Instance.formationSlots[i] == null)
            {
                FormationManager.Instance.SetUnitToSlot(i, _selectedUnit);
                success = true;
                break;
            }
        }

        if (success)
        {
            Debug.Log($"{_selectedUnit.unitName}을(를) 편성에 추가했습니다!");
            // 여기서 편성 UI가 있다면 갱신해주는 로직을 넣으면 좋습니다.
        }
        else
        {
            Debug.LogWarning("편성 슬롯이 가득 찼습니다!");
        }
    }
}