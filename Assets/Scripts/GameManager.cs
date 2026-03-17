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
            LoadGame();
        }
        else Destroy(gameObject);
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

            // 리스트 참조 유지를 위해 새로 갈아끼우지 말고 내용을 채움
            ownedUnits.Clear();
            if (data.ownedUnits != null) ownedUnits.AddRange(data.ownedUnits);

            ownedEquips.Clear();
            if (data.ownedEquips != null) ownedEquips.AddRange(data.ownedEquips);

            Debug.Log($"데이터 불러오기 성공! 유닛: {ownedUnits.Count}개, 장비: {ownedEquips.Count}개");

            if (InventoryManager.Instance != null)
            {
                // 앞서 만든 동기화 함수 호출 (데이터가 들어왔으니 SO로 변환)
                InventoryManager.Instance.RefreshInventoryFromSaveData();
            }

            // [추가] 현재 씬에 UI 매니저가 있다면 리스트 갱신 명령
            var unitUI = FindAnyObjectByType<UnitInventorySceneManager>();
            if (unitUI != null) unitUI.RefreshUnitList();

            var equipUI = FindAnyObjectByType<EquipmentInventoryUI>();
            if (equipUI != null) equipUI.RefreshList();
        }
    
    }

    private void OnApplicationQuit() => SaveGame();

    // --- 데이터 추가 메서드 ---

    public void AddUnit(UnitData unit)
    {
        UnitSaveData newUnit = new UnitSaveData
        {
            unitName = unit.unitName,
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