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

    private UnitData _selectedUnit;

    private void Start()
    {
        RefreshUnitList();
        // 첫 번째 용병이 있다면 자동으로 선택
        if (InventoryManager.Instance.myUnits.Count > 0)
        {
            SelectUnit(InventoryManager.Instance.myUnits[0]);
        }
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
}