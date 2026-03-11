using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public struct StructureOption
{
    public string structureName;
    public GameObject prefab;
    public int cost;
}

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Building Settings")]
    [SerializeField] private List<StructureOption> buildOptions;
    [SerializeField] private int maxCost = 100;

    private int _currentCost;
    private int _selectedIndex = 0;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask buildAreaLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Path Check Settings")]
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform defenseBase;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button startButton;

    public bool IsBuildingPhase { get; private set; } = true;

    private void Awake()
    {
        Instance = this;
        _currentCost = maxCost;
    }

    private void Start()
    {
        UpdateCostUI();
        if (startButton != null)
            startButton.onClick.AddListener(OnClickStartWave);

        if (buildOptions.Count > 0)
        {
            SelectStructure(0);
        }
    }

    private void Update()
    {
        if (!IsBuildingPhase) return;

        HandleNumberInput();

        if (InputManager.Instance.InputActions.Player.LeftClick.WasPerformedThisFrame())
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            TryPlaceStructure();
        }
    }

    private void HandleNumberInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) SelectStructure(0);
        if (keyboard.digit2Key.wasPressedThisFrame) SelectStructure(1);
        if (keyboard.digit3Key.wasPressedThisFrame) SelectStructure(2);
        if (keyboard.digit4Key.wasPressedThisFrame) SelectStructure(3);
        if (keyboard.digit5Key.wasPressedThisFrame) SelectStructure(4);
        if (keyboard.digit6Key.wasPressedThisFrame) SelectStructure(5);
        if (keyboard.digit7Key.wasPressedThisFrame) SelectStructure(6);
        if (keyboard.digit8Key.wasPressedThisFrame) SelectStructure(7);
        if (keyboard.digit9Key.wasPressedThisFrame) SelectStructure(8);
        if (keyboard.digit0Key.wasPressedThisFrame) SelectStructure(9);
    }

    private void SelectStructure(int index)
    {
        if (index >= 0 && index < buildOptions.Count)
        {
            _selectedIndex = index;
            StructureOption selected = buildOptions[_selectedIndex];
            Debug.Log($"<color=cyan>[건설 모드]</color> 현재 설치 구조물 변경: <b>{selected.structureName}</b> (비용: {selected.cost})");
        }
        else
        {
            Debug.LogWarning($"<color=red>[건설 모드]</color> {index + 1}번에 할당된 구조물이 없습니다.");
        }
    }

    private void TryPlaceStructure()
    {
        if (buildOptions.Count == 0 || buildOptions[_selectedIndex].prefab == null) return;

        StructureOption selectedStructure = buildOptions[_selectedIndex];

        if (_currentCost < selectedStructure.cost)
        {
            Debug.LogWarning("비용이 부족합니다!");
            return;
        }

        Vector2 mouseScreenPos = InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 spawnPos = new Vector3(Mathf.Round(mouseWorldPos.x), Mathf.Round(mouseWorldPos.y), 0);

        // 1. 건설 가능 타일맵 레이어 체크
        Collider2D buildAreaHit = Physics2D.OverlapPoint(spawnPos, buildAreaLayer);
        if (buildAreaHit == null)
        {
            Debug.Log("건설할 수 없는 구역입니다.");
            return;
        }

        // 2. 구조물 중복 설치 체크 (OverlapCircle로 넓게 판정)
        Collider2D obstacleHit = Physics2D.OverlapCircle(spawnPos, 0.4f, obstacleLayer);
        if (obstacleHit != null)
        {
            Debug.LogWarning("이미 해당 위치에 다른 구조물이 있습니다!");
            return;
        }

        // 3. 경로 차단 검사
        if (IsPathAvailable())
        {
            Instantiate(selectedStructure.prefab, spawnPos, Quaternion.identity);
            _currentCost -= selectedStructure.cost;
            UpdateCostUI();
            Debug.Log($"<color=green>[건설 성공]</color> {selectedStructure.structureName} 설치됨.");
        }
        else
        {
            Debug.LogWarning("길을 완전히 막을 수 없습니다!");
        }
    }

    private bool IsPathAvailable()
    {
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(enemySpawnPoint.position, defenseBase.position, NavMesh.AllAreas, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }

    private void UpdateCostUI()
    {
        if (costText != null)
            costText.text = $"Cost: {_currentCost} / {maxCost}";
    }

    public void OnClickStartWave()
    {
        if (!IsBuildingPhase) return;

        IsBuildingPhase = false;
        if (startButton != null) startButton.gameObject.SetActive(false);

        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StartWaveSystem();
        }
        Debug.Log("건설 종료 - 웨이브가 시작됩니다.");
    }
}