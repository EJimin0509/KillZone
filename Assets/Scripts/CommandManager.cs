using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask structureLayer;

    private PlayerMovement _selectedUnit;
    private MannedStructure _selectedStructure;

    private bool _isDeployMode = false;
    private bool _isUndeployMode = false;

    private PlayerMovement _deployingUnit;
    private MannedStructure _targetStructure;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // 배치 명령을 받은 유닛이 타워에 근접했는지 감시
        if (_deployingUnit != null && _targetStructure != null)
        {
            if (!_deployingUnit.gameObject.activeInHierarchy)
            {
                _deployingUnit = null;
                _targetStructure = null;
            }
            else if (Vector2.Distance(_deployingUnit.transform.position, _targetStructure.transform.position) <= 2.0f)
            {
                _targetStructure.GarrisonUnit(_deployingUnit.GetComponent<UnitStat>());
                _deployingUnit = null;
                _targetStructure = null;
            }
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        // [배치 모드 진입] 'i'
        if (kb.iKey.wasPressedThisFrame)
        {
            // PlayerMovement가 스스로 선택 관리를 하므로, 씬에 있는 모든 유닛 중 선택된 유닛을 찾음
            _selectedUnit = null;
            PlayerMovement[] allUnits = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var u in allUnits)
            {
                if (u.IsSelected) _selectedUnit = u;
            }

            if (_selectedUnit != null)
            {
                _isDeployMode = true;
                _isUndeployMode = false;
                Debug.Log("<color=yellow>[명령]</color> 배치 모드(i) 활성화. 배치할 구조물을 좌클릭하세요.");
            }
        }

        // [해제 모드 진입] 'o'
        if (kb.oKey.wasPressedThisFrame && _selectedStructure != null && _selectedStructure.HasGarrisonedUnit)
        {
            _isUndeployMode = true;
            _isDeployMode = false;
            Debug.Log("<color=yellow>[명령]</color> 해제 모드(o) 활성화. 유닛을 내보낼 바닥을 우클릭하세요.");
        }

        if (InputManager.Instance.InputActions.Player.LeftClick.WasPerformedThisFrame())
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>());

            if (_isDeployMode) HandleDeployClick(mousePos);
            else HandleStructureSelection(mousePos);
        }

        if (InputManager.Instance.InputActions.Player.RightClick.WasPerformedThisFrame())
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>());

            if (_isUndeployMode)
            {
                HandleUndeployClick(mousePos);
            }
            else
            {
                // 일반 이동 명령 발생 시 배치 취소 처리
                PlayerMovement[] allUnits = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
                foreach (var u in allUnits)
                {
                    if (u.IsSelected && _deployingUnit == u)
                    {
                        _deployingUnit = null;
                        _targetStructure = null;
                    }
                }
            }
        }
    }

    private void HandleStructureSelection(Vector2 mousePos)
    {
        Collider2D structHit = Physics2D.OverlapCircle(mousePos, 0.2f, structureLayer);
        if (structHit != null)
        {
            MannedStructure ms = structHit.GetComponentInParent<MannedStructure>();
            if (ms != null)
            {
                _selectedStructure = ms;
                if (ms.HasGarrisonedUnit) Debug.Log("<color=green>[선택]</color> 유닛이 배치된 구조물 선택됨. (해제: o)");
                else Debug.Log("<color=green>[선택]</color> 구조물이 선택되었습니다.");

                _isDeployMode = false;
                _isUndeployMode = false;
            }
        }
        else
        {
            _selectedStructure = null;
        }
    }

    private void HandleDeployClick(Vector2 mousePos)
    {
        Collider2D hit = Physics2D.OverlapCircle(mousePos, 0.2f, structureLayer);
        if (hit != null)
        {
            MannedStructure structure = hit.GetComponentInParent<MannedStructure>();
            if (structure != null)
            {
                _deployingUnit = _selectedUnit;
                _targetStructure = structure;

                Vector3 safeDest = structure.transform.position;
                if (UnityEngine.AI.NavMesh.SamplePosition(structure.transform.position, out UnityEngine.AI.NavMeshHit navHit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    safeDest = navHit.position;
                }

                _selectedUnit.CommandMove(safeDest);
                Debug.Log($"<color=cyan>[명령]</color> 구조물로 이동 후 배치됩니다.");
            }
        }
        _isDeployMode = false;
    }

    private void HandleUndeployClick(Vector2 mousePos)
    {
        if (_selectedStructure != null)
        {
            _selectedStructure.UnGarrisonUnit(mousePos);
        }
        _isUndeployMode = false;
    }
}