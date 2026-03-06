using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Building Settings")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private int maxCost = 100;
    [SerializeField] private int wallCost = 10;
    private int _currentCost;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask buildAreaLayer; // 새 타일맵(건설가능구역) 레이어
    [SerializeField] private LayerMask obstacleLayer;  // 벽 중복 설치 방지 레이어

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
    }

    private void Update()
    {
        if (!IsBuildingPhase) return;

        // PlayerMovement.cs와 동일하게 InputManager의 LeftClick 액션 사용
        if (InputManager.Instance.InputActions.Player.LeftClick.WasPerformedThisFrame())
        {
            // UI 위를 클릭하고 있다면 건설 무시
            if (EventSystem.current.IsPointerOverGameObject()) return;

            TryPlaceWall();
        }
    }

    private void TryPlaceWall()
    {
        if (_currentCost < wallCost) return;

        // Point 액션에서 마우스 화면 좌표 읽기
        Vector2 mouseScreenPos = InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // 타일 그리드 좌표로 스냅 (정수 단위)
        Vector3 spawnPos = new Vector3(Mathf.Round(mouseWorldPos.x), Mathf.Round(mouseWorldPos.y), 0);

        // 1. 건설 가능 타일맵 레이어 체크
        Collider2D buildAreaHit = Physics2D.OverlapPoint(spawnPos, buildAreaLayer);
        if (buildAreaHit == null) return;

        // 2. 이미 벽이 있는지 체크
        Collider2D obstacleHit = Physics2D.OverlapPoint(spawnPos, obstacleLayer);
        if (obstacleHit != null) return;

        // 3. 경로 차단 검사 (길이 완전히 막히는지 확인)
        if (IsPathAvailable())
        {
            Instantiate(wallPrefab, spawnPos, Quaternion.identity);
            _currentCost -= wallCost;
            UpdateCostUI();
        }
        else
        {
            Debug.LogWarning("길을 완전히 막을 수 없습니다!");
        }
    }

    private bool IsPathAvailable()
    {
        // 현재 NavMesh 상태에서 적 스폰지점 -> 베이스까지 경로 계산
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(enemySpawnPoint.position, defenseBase.position, NavMesh.AllAreas, path);

        // 경로가 끊기지 않고 완전한 상태인지 확인
        return path.status == NavMeshPathStatus.PathComplete;
    }

    private void UpdateCostUI()
    {
        if (costText != null)
            costText.text = $"Cost: {_currentCost} / {maxCost}";
    }

    public void OnClickStartWave()
    {
        IsBuildingPhase = false;
        if (startButton != null) startButton.gameObject.SetActive(false);
        Debug.Log("건설 종료 - 웨이브가 시작됩니다.");
    }
}