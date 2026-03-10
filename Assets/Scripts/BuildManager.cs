using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // 숫자 키보드 입력을 위해 추가
using System.Collections.Generic;

// 인스펙터에서 구조물 종류별로 이름, 프리팹, 비용을 설정할 수 있는 구조체
[System.Serializable]
public struct StructureOption
{
    public string structureName; // 디버그 및 관리용 이름 (예: 독 발판, 망루)
    public GameObject prefab;    // 설치할 프리팹
    public int cost;             // 설치 시 소모되는 비용
}

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Building Settings")]
    [SerializeField] private List<StructureOption> buildOptions; // 설치 가능한 구조물 리스트
    [SerializeField] private int maxCost = 100;

    private int _currentCost;
    private int _selectedIndex = 0; // 현재 선택된 구조물의 인덱스

    [Header("Layer Settings")]
    [SerializeField] private LayerMask buildAreaLayer; // 새 타일맵(건설가능구역) 레이어
    [SerializeField] private LayerMask obstacleLayer;  // 중복 설치 방지 레이어

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

        // 게임 시작 시 첫 번째 구조물 정보 출력
        if (buildOptions.Count > 0)
        {
            SelectStructure(0);
        }
    }

    private void Update()
    {
        if (!IsBuildingPhase) return;

        // 1. 키보드 숫자키를 통한 구조물 변경
        HandleNumberInput();

        // 2. 마우스 좌클릭을 통한 건설
        if (InputManager.Instance.InputActions.Player.LeftClick.WasPerformedThisFrame())
        {
            // UI 위를 클릭하고 있다면 건설 무시
            if (EventSystem.current.IsPointerOverGameObject()) return;

            TryPlaceStructure();
        }
    }

    /// <summary>
    /// 1~0 숫자 키 입력을 감지하여 설치할 구조물을 변경합니다.
    /// </summary>
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

    /// <summary>
    /// 지정된 인덱스의 구조물을 선택하고 콘솔에 정보를 출력합니다.
    /// </summary>
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
            Debug.LogWarning($"<color=red>[건설 모드]</color> {index + 1}번에 할당된 구조물이 없습니다. 인스펙터의 Build Options를 확인해주세요.");
        }
    }

    private void TryPlaceStructure()
    {
        // 리스트가 비어있거나 프리팹이 등록되지 않은 경우 방어 로직
        if (buildOptions.Count == 0 || buildOptions[_selectedIndex].prefab == null) return;

        StructureOption selectedStructure = buildOptions[_selectedIndex];

        // 비용이 모자라면 건설 불가
        if (_currentCost < selectedStructure.cost)
        {
            Debug.LogWarning("비용이 부족합니다!");
            return;
        }

        // 마우스 화면 좌표 읽기
        Vector2 mouseScreenPos = InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // 타일 그리드 좌표로 스냅 (정수 단위)
        Vector3 spawnPos = new Vector3(Mathf.Round(mouseWorldPos.x), Mathf.Round(mouseWorldPos.y), 0);

        // 1. 건설 가능 타일맵 레이어 체크
        Collider2D buildAreaHit = Physics2D.OverlapPoint(spawnPos, buildAreaLayer);
        if (buildAreaHit == null) return;

        // 2. 이미 벽이나 다른 구조물이 있는지 체크
        Collider2D obstacleHit = Physics2D.OverlapPoint(spawnPos, obstacleLayer);
        if (obstacleHit != null) return;

        // 3. 경로 차단 검사 (길이 완전히 막히는지 확인)
        if (IsPathAvailable())
        {
            Instantiate(selectedStructure.prefab, spawnPos, Quaternion.identity);

            // 선택된 구조물의 개별 비용만큼 차감
            _currentCost -= selectedStructure.cost;
            UpdateCostUI();

            Debug.Log($"<color=green>[건설 성공]</color> {selectedStructure.structureName} 설치됨. 남은 Cost: {_currentCost}");
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