using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private LayerMask structureLayer;

    private PlayerMovement _selectedUnit;
    private MannedStructure _selectedStructure;
    private bool _isDeployMode = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // 1. 배치 모드 진입 (i키)
        if (Keyboard.current.iKey.wasPressedThisFrame && _selectedUnit != null)
        {
            _isDeployMode = true;
            Debug.Log("<color=yellow>[배치]</color> 구조물을 선택하세요.");
        }

        // 2. 왼쪽 클릭 처리
        if (InputManager.Instance.InputActions.Player.LeftClick.WasPerformedThisFrame())
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>());

            if (_isDeployMode) HandleDeployClick(mousePos);
            else HandleSelection(mousePos);
        }

        // 3. 오른쪽 클릭 처리 (일반 이동)
        if (InputManager.Instance.InputActions.Player.RightClick.WasPerformedThisFrame())
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>());

            if (_selectedUnit != null && _selectedUnit.IsSelected)
            {
                _selectedUnit.CommandMove(mousePos); // 일반 이동 시 배치 취소됨
                _isDeployMode = false;
            }
        }

        // 4. [핵심] 도착 시 배치 실행 (실시간 체크)
        CheckDeploymentArrival();
    }

    private void HandleSelection(Vector2 mousePos)
    {
        Collider2D unitHit = Physics2D.OverlapCircle(mousePos, 0.2f, unitLayer);
        if (unitHit != null)
        {
            if (_selectedUnit != null) _selectedUnit.SetSelected(false);
            _selectedUnit = unitHit.GetComponent<PlayerMovement>();
            _selectedUnit.SetSelected(true);
            return;
        }

        Collider2D structHit = Physics2D.OverlapCircle(mousePos, 0.2f, structureLayer);
        if (structHit != null)
        {
            _selectedStructure = structHit.GetComponentInParent<MannedStructure>();
            Debug.Log("구조물 선택됨");
        }
    }

    private void HandleDeployClick(Vector2 mousePos)
    {
        Collider2D hit = Physics2D.OverlapCircle(mousePos, 0.3f, structureLayer);
        if (hit != null && _selectedUnit != null)
        {
            MannedStructure target = hit.GetComponentInParent<MannedStructure>();
            if (target != null && !target.HasGarrisonedUnit)
            {
                // 목적지 샘플링 후 명령 전달
                Vector3 dest = target.transform.position;
                if (UnityEngine.AI.NavMesh.SamplePosition(dest, out UnityEngine.AI.NavMeshHit navHit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                    dest = navHit.position;

                _selectedUnit.CommandDeploy(target, dest);
                Debug.Log("배치 이동 시작...");
            }
        }
        _isDeployMode = false;
    }

    private void CheckDeploymentArrival()
    {
        // 씬에 있는 모든 유닛 중 배치 대기 중인 녀석들 체크
        PlayerMovement[] allUnits = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit.IsPendingDeployment && unit.TargetStructure != null)
            {
                float dist = Vector2.Distance(unit.transform.position, unit.TargetStructure.transform.position);
                if (dist <= 1.5f) // 충분히 가까워지면
                {
                    unit.TargetStructure.GarrisonUnit(unit.GetComponent<UnitStat>());
                    unit.CancelDeployment();
                    Debug.Log("<color=cyan>배치 완료!</color>");
                }
            }
        }
    }
}