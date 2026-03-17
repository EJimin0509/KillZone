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

    [Header("Squad Visuals")]
    public Image[] squadImages; // 인스펙터에서 5개의 스쿼드 이미지 슬롯 연결

    public Button formationButton; // 전투 선택

    private UnitData _selectedUnit;

    private void Start()
    {
        RefreshUnitList();
        RefreshSquadVisuals();

        // 첫 번째 용병이 있다면 자동으로 선택
        if (InventoryManager.Instance != null && InventoryManager.Instance.myUnits.Count > 0)
        {
            SelectUnit(InventoryManager.Instance.myUnits[0]);
        }

        // 버튼 리스너 연결
        helmButton.onClick.AddListener(() => selectPopup.Open(EquipmentType.Helm, (e) => EquipItem(e, 0)));
        chestButton.onClick.AddListener(() => selectPopup.Open(EquipmentType.Chest, (e) => EquipItem(e, 1)));
        weaponButton.onClick.AddListener(() => selectPopup.Open(EquipmentType.Melee, (e) => EquipItem(e, 2)));

        if (formationButton != null)
        {
            formationButton.onClick.AddListener(OnFormationButtonClick);
        }
    }

    // 용병 리스트에서 용병을 선택했을 때 호출
    public void OnSelectUnit(UnitData unit)
    {
        _selectedUnit = unit;

        detailNameText.text = unit.unitName;

        unitIllust.sprite = unit.unitSprite;

        RefreshEquipVisuals();
    }

    private void EquipItem(EquipmentData item, int slot)
    {
        if (_selectedUnit == null || item == null) return;

        // 1. [해제] 이미 내가 끼고 있는 장비를 팝업에서 또 선택했다면?
        if (item.ownerUnit == _selectedUnit)
        {
            // 현재 슬롯에 장착된 장비가 바로 그 아이템인지 확인 후 해제
            if (GetItemInSlot(slot) == item)
            {
                item.ownerUnit = null; // 장비에서 주인 정보 삭제
                SetItemInSlot(slot, null); // 유닛 슬롯 비움
                RefreshEquipVisuals();
                return;
            }
        }

        // 2. [뺏어오기] 다른 유닛이 이미 끼고 있는 장비라면?
        if (item.ownerUnit != null && item.ownerUnit != _selectedUnit)
        {
            UnitData oldOwner = item.ownerUnit;
            if (oldOwner.equippedHelm == item) oldOwner.equippedHelm = null;
            if (oldOwner.equippedChest == item) oldOwner.equippedChest = null;
            if (oldOwner.equippedWeapon == item) oldOwner.equippedWeapon = null;

            Debug.Log($"{oldOwner.unitName}에게서 {item.equipName}을(를) 가져왔습니다.");
        }

        // 3. [교체] 내가 현재 이 슬롯에 끼고 있던 기존 장비의 주인 정보 초기화
        EquipmentData oldItem = GetItemInSlot(slot);
        if (oldItem != null) oldItem.ownerUnit = null;

        // 4. [장착] 이제 새 장비를 내 슬롯에 넣고, 장비에게 내가 주인이라고 알려줌
        SetItemInSlot(slot, item);
        item.ownerUnit = _selectedUnit;

        RefreshEquipVisuals();

        // 최종적으로 게임 데이터 저장 (편의성)
        if (GameManager.Instance != null) GameManager.Instance.SaveGame();
    }

    private EquipmentData GetItemInSlot(int slot)
    {
        if (slot == 0) return _selectedUnit.equippedHelm;
        if (slot == 1) return _selectedUnit.equippedChest;
        if (slot == 2) return _selectedUnit.equippedWeapon;
        return null;
    }

    private void SetItemInSlot(int slot, EquipmentData item)
    {
        if (slot == 0) _selectedUnit.equippedHelm = item;
        else if (slot == 1) _selectedUnit.equippedChest = item;
        else if (slot == 2) _selectedUnit.equippedWeapon = item;
    }

    private void RefreshEquipVisuals()
    {
        if (_selectedUnit == null) return;

        // 장비 버튼의 Image 컴포넌트를 가져와서 스프라이트 교체
        // 예: helmButton 자식에 있는 Image 컴포넌트 등
        UpdateSlotVisual(helmButton, _selectedUnit.equippedHelm);
        UpdateSlotVisual(chestButton, _selectedUnit.equippedChest);
        UpdateSlotVisual(weaponButton, _selectedUnit.equippedWeapon);
    }

    private void UpdateSlotVisual(Button button, EquipmentData data)
    {
        if (button == null) return;

        // 자식 중에서 "Icon" 오브젝트를 찾음
        Transform iconTransform = button.transform.Find("Icon");
        if (iconTransform == null) return;

        Image iconImage = iconTransform.GetComponent<Image>();
        if (iconImage == null) return;

        // [수정] data.unitSprite가 아니라 data.equipSprite를 참조해야 함
        if (data != null && data.equipSprite != null)
        {
            iconImage.sprite = data.equipSprite; // EquipmentData의 변수명과 일치시킴
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            //iconImage.color = new Color(1, 1, 1, 0.2f); // 빈 슬롯 표시
        }
    }

    // 편성 해제 버튼
    public void OnRemoveFormationButtonClick()
    {
        if (_selectedUnit == null) return;

        for (int i = 0; i < FormationManager.Instance.formationSlots.Length; i++)
        {
            if (FormationManager.Instance.formationSlots[i] == _selectedUnit)
            {
                FormationManager.Instance.SetUnitToSlot(i, null); // 해당 슬롯 비움
                //Debug.Log($"{_selectedUnit.unitName}을(를) 편성에서 해제했습니다.");
                RefreshSquadVisuals(); // 비주얼 갱신
                return;
            }
        }
    }

    /// <summary>
    /// 편성 이미지 갱신
    /// </summary>
    public void RefreshSquadVisuals()
    {
        for (int i = 0; i < squadImages.Length; i++)
        {
            if (i < FormationManager.Instance.formationSlots.Length)
            {
                UnitData unitInSlot = FormationManager.Instance.formationSlots[i];
                if (unitInSlot != null && unitInSlot.unitSprite != null)
                {
                    squadImages[i].sprite = unitInSlot.unitSprite;
                    squadImages[i].color = Color.white;
                    squadImages[i].enabled = true;
                }
                else
                {
                    squadImages[i].sprite = null;
                    squadImages[i].enabled = false;
                    //squadImages[i].color = new Color(1, 1, 1, 0.2f); // 빈 슬롯은 반투명하게
                }
            }
        }
    }

    // 아군 리스트 생성 및 갱신
    public void RefreshUnitList()
    {
        // 1. 부모 오브젝트 체크
        if (listParent == null)
        {
            //Debug.LogError("listParent(Content)가 할당되지 않았습니다!");
            return;
        }

        foreach (Transform child in listParent) Destroy(child.gameObject);

        // 2. 인벤토리 매니저 체크
        if (InventoryManager.Instance == null)
        {
            //Debug.LogError("InventoryManager를 찾을 수 없습니다!");
            return;
        }

        foreach (UnitData unit in InventoryManager.Instance.myUnits)
        {
            // 3. 프리팹 체크
            if (unitSlotPrefab == null)
            {
                //Debug.LogError("unitSlotPrefab이 할당되지 않았습니다!");
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
                //Debug.LogError("프리팹에 UnitInventorySlot 스크립트가 없습니다!");
            }
        }
    }

    // 우측 상세창 정보 업데이트
    public void SelectUnit(UnitData unit)
    {
        if (unit == null)
        {
            ClearDetailPanel();
            return;
        }

        _selectedUnit = unit;
        detailNameText.text = unit.unitName;

        if (unitIllust != null)
        {
            unitIllust.sprite = unit.unitSprite;
            unitIllust.color = Color.white;
            unitIllust.enabled = true;
        }
        else
        {
            unitIllust.sprite = null;
            unitIllust.enabled = false;
        }

        // 스탯 배열 순서대로 매핑 (UnitData 구조에 맞춰서)
        statTexts[0].text = unit.hp.ToString();
        statTexts[1].text = unit.melee.ToString();
        statTexts[2].text = unit.range.ToString();
        statTexts[3].text = unit.repair.ToString();
        statTexts[4].text = unit.medic.ToString();
        statTexts[5].text = unit.will.ToString();
        statTexts[6].text = unit.faith.ToString();

        RefreshEquipVisuals();
        RefreshSquadVisuals();

        //Debug.Log($"{unit.unitName} 상세 정보 표시 중");
    }

    private void ClearDetailPanel()
    {
        _selectedUnit = null;
        detailNameText.text = "선택된 유닛 없음";

        if (unitIllust != null)
        {
            unitIllust.sprite = null;
            unitIllust.enabled = false; // 흰색 사각형 방지
        }

        // 스탯 텍스트 초기화 (0 또는 "-"으로 표시)
        foreach (var txt in statTexts)
        {
            txt.text = "-";
        }

        // 장비 슬롯 이미지도 모두 비활성화
        UpdateSlotVisual(helmButton, null);
        UpdateSlotVisual(chestButton, null);
        UpdateSlotVisual(weaponButton, null);
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
                //Debug.Log("이미 배치된 유닛입니다.");
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
            //Debug.Log($"{_selectedUnit.unitName}을(를) 편성에 추가했습니다!");
            RefreshSquadVisuals();
        }
        else
        {
            //Debug.LogWarning("편성 슬롯이 가득 찼습니다!");
        }
    }

}