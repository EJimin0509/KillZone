using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
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

    private Vector3 _lastBuildPos = Vector3.positiveInfinity;
    private Vector3 _lastRemovePos = Vector3.positiveInfinity;

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

        if (InputManager.Instance.InputActions.Player.LeftClick.IsPressed())
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                TryPlaceStructure();
            }
        }
        else
        {
            _lastBuildPos = Vector3.positiveInfinity;
        }

        if (InputManager.Instance.InputActions.Player.RightClick.IsPressed())
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                TryRemoveStructure();
            }
        }
        else
        {
            _lastRemovePos = Vector3.positiveInfinity;
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
            Debug.Log($"[건설 모드] 현재 설치 구조물 변경: {selected.structureName} (비용: {selected.cost})");
        }
    }

    private void TryPlaceStructure()
    {
        if (buildOptions.Count == 0 || buildOptions[_selectedIndex].prefab == null) return;

        Vector2 mouseScreenPos = InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 spawnPos = new Vector3(Mathf.Round(mouseWorldPos.x), Mathf.Round(mouseWorldPos.y), 0);

        if (spawnPos == _lastBuildPos) return;

        StructureOption selectedStructure = buildOptions[_selectedIndex];

        if (_currentCost < selectedStructure.cost) return;

        Collider2D buildAreaHit = Physics2D.OverlapPoint(spawnPos, buildAreaLayer);
        if (buildAreaHit == null) return;

        Collider2D obstacleHit = Physics2D.OverlapCircle(spawnPos, 0.4f, obstacleLayer);
        if (obstacleHit != null) return;

        _lastBuildPos = spawnPos;

        StartCoroutine(PlaceAndCheckPathRoutine(selectedStructure, spawnPos));
    }

    private IEnumerator PlaceAndCheckPathRoutine(StructureOption structure, Vector3 pos)
    {
        GameObject tempStructure = Instantiate(structure.prefab, pos, Quaternion.identity);

        yield return null;

        if (IsPathAvailable())
        {
            _currentCost -= structure.cost;
            UpdateCostUI();

            // [추가] 설치 성공 시 벽 연결 처리
            WallConnector connector = tempStructure.GetComponent<WallConnector>();
            if (connector != null)
            {
                connector.UpdateConnection(); // 내 이미지 갱신
                connector.NotifyNeighbors();  // 주변 벽 갱신
            }
        }
        else
        {
            Destroy(tempStructure);
            Debug.LogWarning("해당 위치에 설치하면 적의 이동 경로가 완전히 차단되므로 설치가 취소되었습니다.");

            if (_lastBuildPos == pos)
            {
                _lastBuildPos = Vector3.positiveInfinity;
            }
        }
    }

    private void TryRemoveStructure()
    {
        Vector2 mouseScreenPos = InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 checkPos = new Vector3(Mathf.Round(mouseWorldPos.x), Mathf.Round(mouseWorldPos.y), 0);

        if (checkPos == _lastRemovePos) return;

        Collider2D obstacleHit = Physics2D.OverlapCircle(checkPos, 0.4f, obstacleLayer);

        if (obstacleHit != null)
        {
            string hitName = obstacleHit.gameObject.name.Replace("(Clone)", "").Trim();
            bool isPlayerStructure = false;
            int refundAmount = 0;

            foreach (var option in buildOptions)
            {
                if (option.prefab.name == hitName)
                {
                    refundAmount = option.cost;
                    isPlayerStructure = true;
                    break;
                }
            }

            if (isPlayerStructure)
            {
                _currentCost += refundAmount;
                if (_currentCost > maxCost) _currentCost = maxCost;

                // [추가] 삭제 전 벽 연결 데이터 캐싱
                WallConnector connector = obstacleHit.GetComponent<WallConnector>();

                Destroy(obstacleHit.gameObject);
                UpdateCostUI();

                // [추가] 삭제 직후 주변 벽들에게 나(벽)가 사라졌음을 알려서 이미지를 다시 그리게 함
                if (connector != null)
                {
                    // 오브젝트가 파괴되는 프레임 이후에 주변 벽들이 체크할 수 있도록 짧은 대기 후 실행하거나, 
                    // 아래 NotifyNeighbors 내부에서 현재 삭제된 위치를 무시하도록 처리되어야 함.
                    // 간단히 하기 위해 NotifyNeighbors 기능을 활용
                    connector.NotifyNeighbors();
                }

                Debug.Log($"[건설 취소] {hitName} 판매됨. 환불: {refundAmount}");

                _lastRemovePos = checkPos;
            }
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
            costText.text = $"Cost: {_currentCost}";
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