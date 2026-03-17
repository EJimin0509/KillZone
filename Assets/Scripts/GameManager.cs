using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Current Status")]
    public int currentGold;
    public int currentStage = 1;
    public bool[] stageUnlocked = { true, false, false };

    [Header("Inventory")]
    public List<UnitSaveData> ownedUnits = new List<UnitSaveData>();
    public List<EquipSaveData> ownedEquips = new List<EquipSaveData>();

    private string _savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _savePath = Path.Combine(Application.persistentDataPath, "saveData.json");
            
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadGame();
    }

    public void SaveGame()
    {
        UserData data = new UserData
        {
            gold = currentGold,
            currentStage = currentStage,
            stageUnlocked = stageUnlocked,
            ownedUnits = this.ownedUnits,   // 리스트 통째로 전달
            ownedEquips = this.ownedEquips  // 리스트 통째로 전달
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_savePath, json);
        Debug.Log("게임 저장 완료!");
    }

    public void LoadGame()
    {
        if (File.Exists(_savePath))
        {
            string json = File.ReadAllText(_savePath);
            UserData data = JsonUtility.FromJson<UserData>(json);

            currentGold = data.gold;
            currentStage = data.currentStage;
            stageUnlocked = data.stageUnlocked;

            // 리스트 데이터 복사
            ownedUnits.Clear();
            if (data.ownedUnits != null) ownedUnits.AddRange(data.ownedUnits);

            ownedEquips.Clear();
            if (data.ownedEquips != null) ownedEquips.AddRange(data.ownedEquips);

            Debug.Log($"[1] GameManager 로드 완료: 유닛 {ownedUnits.Count}개");

            // [핵심] InventoryManager가 존재한다면 즉시 SO 복구 실행
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RefreshInventoryFromSaveData();
            }
            else
            {
                Debug.LogError("InventoryManager Instance를 찾을 수 없습니다!");
            }

            // [핵심] UI 갱신 (씬에 UI가 있을 때만)
            RefreshAllUI();
        }
    }

    // UI를 찾는 로직은 별도로 빼서 관리하면 편합니다.
    private void RefreshAllUI()
    {
        var unitUI = FindAnyObjectByType<UnitInventorySceneManager>();
        if (unitUI != null) unitUI.RefreshUnitList();

        var equipUI = FindAnyObjectByType<EquipmentInventoryUI>();
        if (equipUI != null) equipUI.RefreshList();
    }

    private void OnApplicationQuit() => SaveGame();

    // --- 데이터 추가 메서드 ---

    public void AddUnit(UnitData unit)
    {
        UnitSaveData newUnit = new UnitSaveData
        {
            unitName = unit.unitName,
            spriteKey = unit.spriteKey,
            hp = unit.hp,
            melee = unit.melee,
            range = unit.range,
            repair = unit.repair,
            medic = unit.medic,
            will = unit.will,
            faith = unit.faith
        };
        ownedUnits.Add(newUnit);
        SaveGame();
    }

    public void AddEquipment(EquipmentData equip)
    {
        EquipSaveData newEquip = new EquipSaveData
        {
            equipName = equip.equipName,
            spriteKey = equip.spriteKey,
            type = equip.type,
            attackRange = equip.attackRange,
            defense = equip.defense,
            bonuses = new List<StatBonus>(equip.additionalStatBonuses)
        };
        ownedEquips.Add(newEquip);
        SaveGame();
    }

    public void AddGold(int amount) => currentGold += amount;
}