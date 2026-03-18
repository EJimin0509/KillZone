using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Current Status")]
    public int currentGold;
    public int currentStage = 1;
    public int currentStageIndex;
    public bool[] stageUnlocked = { true, false, false };

    [Header("Inventory")]
    public List<UnitSaveData> ownedUnits = new List<UnitSaveData>();
    public List<EquipSaveData> ownedEquips = new List<EquipSaveData>();

    public int stageGold;
    private string _savePath;
    private bool _isGameOver = false;

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

    public void GameOver(bool isVictory)
    {
        // 1. 중복 종료 방지
        if (_isGameOver) return;
        _isGameOver = true;

        // 2. 씬에 있는 ResultUIController를 찾아서 UI 출력을 명령함
        // (매번 찾기 번거롭다면 Awake에서 미리 참조해둬도 좋습니다)
        ResultUIController resultUI = FindFirstObjectByType<ResultUIController>();

        if (resultUI != null)
        {
            resultUI.ShowResult(isVictory);
        }
        else
        {
            //Debug.LogError("씬에 ResultUIController가 없습니다!");
        }
    }

    public void ClearStage(int stageIndex)
    {
        Debug.Log($"스테이지 클리어 호출됨! 현재 클리어한 인덱스: {stageIndex}");

        int nextStage = stageIndex + 1;
        if (nextStage < stageUnlocked.Length)
        {
            stageUnlocked[nextStage] = true;
            Debug.Log($"{nextStage + 1} 스테이지 잠금 해제 완료!");
        }

        FinalizeStageGold();
    }

    public void SaveGame()
    {
        if (InventoryManager.Instance != null)
        {
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

        UserData data = new UserData();
        data.gold = currentGold;
        data.currentStage = currentStage;
        // [수정] 배열의 값을 복사해서 저장 (참조 오류 방지)
        data.stageUnlocked = (bool[])this.stageUnlocked.Clone();
        data.ownedUnits = this.ownedUnits;
        data.ownedEquips = this.ownedEquips;

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

            if (data.stageUnlocked != null)
            {
                this.stageUnlocked = (bool[])data.stageUnlocked.Clone();
            }

            ownedUnits.Clear();
            if (data.ownedUnits != null) ownedUnits.AddRange(data.ownedUnits);

            ownedEquips.Clear();
            if (data.ownedEquips != null) ownedEquips.AddRange(data.ownedEquips);
        }
        else
        {
            // --- [신규 유저를 위한 초기 설정] ---
            Debug.Log("저장 파일을 찾을 수 없습니다. 신규 유저 초기 데이터 생성 시도.");

            currentGold = 500;            // 기본 재화 500원 지급
            currentStage = 1;             // 1스테이지부터 시작
            stageUnlocked = new bool[] { true, false, false }; // 첫 스테지만 오픈

            // 필요하다면 여기서 기본 유닛 1개를 강제로 넣어줄 수도 있습니다.

            SaveGame(); // 초기 상태를 즉시 파일로 저장
        }

        // 로드(또는 초기화) 직후 UI 갱신
        RefreshLobbyUI();
        RefreshAllUI();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RefreshInventoryFromSaveData();
    }

    public void RefreshLobbyUI()
    {
        var lobbyUI = FindAnyObjectByType<LobbyStageManager>();
        if (lobbyUI != null)
        {
            lobbyUI.RefreshStageUI();
            Debug.Log("로비 UI 갱신 완료");
        }
    }

    private void RefreshAllUI()
    {
        var unitUI = FindAnyObjectByType<UnitInventorySceneManager>();
        if (unitUI != null) unitUI.RefreshUnitList();

        var equipUI = FindAnyObjectByType<EquipmentInventoryUI>();
        if (equipUI != null) equipUI.RefreshList();

        var goldUI = FindAnyObjectByType<LobbyGoldUI>();
        if (goldUI != null) goldUI.RefreshGoldDisplay();
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