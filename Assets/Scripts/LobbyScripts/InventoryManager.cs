using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("보유 목록 (실제 게임용 ScriptableObject)")]
    public List<UnitData> myUnits = new List<UnitData>();
    public List<EquipmentData> myEquipments = new List<EquipmentData>();

    [Header("스테이지 편성")]
    public UnitData[] formationSlots = new UnitData[10];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    // 게임 시작 시 혹은 데이터가 로드된 후 한 번 호출해서 ScriptableObject 리스트를 만듭니다.
    public void RefreshInventoryFromSaveData()
    {
        if (GameManager.Instance == null) return;

        // 1. 유닛 복구
        myUnits.Clear();
        foreach (var u in GameManager.Instance.ownedUnits)
        {
            UnitData unit = ScriptableObject.CreateInstance<UnitData>();
            unit.unitName = u.unitName;
            unit.hp = u.hp; unit.melee = u.melee; unit.range = u.range;
            unit.repair = u.repair; unit.medic = u.medic; unit.will = u.will; unit.faith = u.faith;

            if (!string.IsNullOrEmpty(u.spriteKey))
                unit.unitSprite = Resources.Load<Sprite>($"Sprites/{u.spriteKey}");

            myUnits.Add(unit);
        }

        // 2. 장비 복구
        myEquipments.Clear();
        foreach (var e in GameManager.Instance.ownedEquips)
        {
            EquipmentData equip = ScriptableObject.CreateInstance<EquipmentData>();
            equip.equipName = e.equipName;
            equip.type = e.type;
            equip.attackRange = e.attackRange;
            equip.defense = e.defense;
            equip.additionalStatBonuses = new List<StatBonus>(e.bonuses);

            if (!string.IsNullOrEmpty(e.spriteKey))
                equip.equipSprite = Resources.Load<Sprite>($"Sprites/{e.spriteKey}");

            myEquipments.Add(equip);
        }

        Debug.Log("인벤토리 동기화 완료!");
    }

    // 가챠 성공 시 호출
    public void AddUnit(UnitData unit)
    {
        if (unit == null) return;
        myUnits.Add(unit); // 현재 리스트에 추가
        GameManager.Instance.AddUnit(unit); // 실제 저장용 리스트에 추가 (GameManager가 저장함)
    }

    public void AddEquipment(EquipmentData equip)
    {
        if (equip == null) return;
        myEquipments.Add(equip);
        GameManager.Instance.AddEquipment(equip);
    }
}