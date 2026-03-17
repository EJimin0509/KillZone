using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Current Status")]
    public int currentGold;
    public int currentStage = 1; // 로드용
    public int currentStageIndex; // 현재 플레이 중인 스테이지 (0부터 시작)
    public bool[] stageUnlocked = { true, false, false };

    [Header("Inventory")]
    public List<UnitSaveData> ownedUnits = new List<UnitSaveData>();
    public List<EquipSaveData> ownedEquips = new List<EquipSaveData>();

    public int stageGold;
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

    // 1번, 2번 해결: 결과창에서 호출됨
    public void ClearStage(int stageIndex)
    {
        int nextStage = stageIndex + 1;
        if (nextStage < stageUnlocked.Length)
        {
            stageUnlocked[nextStage] = true;
        }
        // 골드 합산 및 전체 저장 실행
        FinalizeStageGold();
    }

    public void SaveGame()
    {
        if (InventoryManager.Instance != null)
        {
            // 리스트 개수 불일치 방지용 안전장치
            int count = Mathf.Min(InventoryManager.Instance.myUnits.Count, ownedUnits.Count);
            for (int i = 0; i < count; i++)
            {
                var unitSO = InventoryManager.Instance.myUnits[i];
                var unitSave = ownedUnits[i];

                unitSave.equippedHelmKey = unitSO.equippedHelm != null ? unitSO.equippedHelm.equipName : "";
                unitSave.equippedChestKey = unitSO.equippedChest != null ? unitSO.equippedChest.equipName : "";
                unitSave.equippedWeaponKey = unitSO.equippedWeapon != null ? unitSO.equippedWeapon.equipName : "";

                unitSave.squadIndex = -1;
                if (FormationManager.Instance != null)
                {
                    for (int s = 0; s < FormationManager.Instance.formationSlots.Length; s++)
                    {
                        if (FormationManager.Instance.formationSlots[s] == unitSO)
                        {
                            unitSave.squadIndex = s;
                            break;
                        }
                    }
                }
            }
        }

        UserData data = new UserData
        {
            gold = currentGold,
            currentStage = currentStage,
            stageUnlocked = stageUnlocked,
            ownedUnits = this.ownedUnits,
            ownedEquips = this.ownedEquips
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_savePath, json);
        Debug.Log("Game Saved Successfully");
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

            ownedUnits.Clear();
            if (data.ownedUnits != null) ownedUnits.AddRange(data.ownedUnits);

            ownedEquips.Clear();
            if (data.ownedEquips != null) ownedEquips.AddRange(data.ownedEquips);

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.RefreshInventoryFromSaveData();

            RefreshAllUI();
        }
    }

    private void RefreshAllUI()
    {
        var unitUI = FindAnyObjectByType<UnitInventorySceneManager>();
        if (unitUI != null) unitUI.RefreshUnitList();
    }

    private void OnApplicationQuit() => SaveGame();

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

    public void AddGold(int amount) => stageGold += amount;

    public void FinalizeStageGold()
    {
        currentGold += stageGold;
        stageGold = 0;
        SaveGame();
    }
}