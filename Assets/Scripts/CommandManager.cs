using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask unitLayer;       // 유닛 레이어 (Unit)
    [SerializeField] private LayerMask structureLayer;  // 구조물 레이어 (Structure 등)

    private PlayerMovement _selectedUnit;
    private MannedStructure _selectedStructure;

    private bool _isDeployMode = false;
    private bool _isUndeployMode = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // [배치 모드 진입] 유닛 선택 후 'i' 
        if (kb.iKey.wasPressedThisFrame && _selectedUnit != null)
        {
            _isDeployMode = true;
            _isUndeployMode = false;
            Debug.Log("<color=yellow>[명령]</color> 배치 모드(i) 활성화. 배치할 구조물을 클릭하세요.");
        }

        // [해제 모드 진입] 구조물 선택 후 'o'
        if (kb.oKey.wasPressedThisFrame && _selectedStructure != null)
        {
            _isUndeployMode = true;
            _isDeployMode = false;
            Debug.Log("<color=yellow>[명령]</color> 해제 모드(o) 활성화. 유닛을 내보낼 바닥을 클릭하세요.");
        }

        // --- 마우스 좌클릭 (선택 및 명령 실행) ---
        if (InputManager.Instance.InputActions.Player.LeftClick.WasPerformedThisFrame())
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>());

            if (_isDeployMode) HandleDeployClick(mousePos);
            else if (_isUndeployMode) HandleUndeployClick(mousePos);
            else HandleSelection(mousePos);
        }

        // --- 마우스 우클릭 (일반 이동) ---
        if (InputManager.Instance.InputActions.Player.RightClick.WasPerformedThisFrame())
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (_selectedUnit != null && !_isDeployMode && !_isUndeployMode)
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>());
                _selectedUnit.CommandMove(mousePos);
            }
        }
    }

    private void HandleSelection(Vector2 mousePos)
    {
        // 1. 유닛 선택
        Collider2D unitHit = Physics2D.OverlapPoint(mousePos, unitLayer);
        if (unitHit != null)
        {
            if (_selectedUnit != null) _selectedUnit.SetSelection(false);
            _selectedUnit = unitHit.GetComponent<PlayerMovement>();
            _selectedUnit.SetSelection(true);
            _selectedStructure = null;
            Debug.Log("<color=green>[선택]</color> 유닛이 선택되었습니다. (배치: i)");
            return;
        }

        // 2. 구조물 선택
        Collider2D structHit = Physics2D.OverlapPoint(mousePos, structureLayer);
        if (structHit != null)
        {
            if (_selectedUnit != null) _selectedUnit.SetSelection(false);
            _selectedUnit = null;
            _selectedStructure = structHit.GetComponent<MannedStructure>();
            Debug.Log("<color=green>[선택]</color> 구조물이 선택되었습니다. (해제: o)");
            return;
        }

        // 빈 공간 클릭 시 선택 해제
        if (_selectedUnit != null) _selectedUnit.SetSelection(false);
        _selectedUnit = null;
        _selectedStructure = null;
    }

    private void HandleDeployClick(Vector2 mousePos)
    {
        Collider2D hit = Physics2D.OverlapPoint(mousePos, structureLayer);
        if (hit != null)
        {
            MannedStructure structure = hit.GetComponent<MannedStructure>();
            if (structure != null)
            {
                _selectedUnit.CommandDeploy(structure);
                Debug.Log($"<color=cyan>[명령]</color> 해당 구조물로 이동 후 배치됩니다.");
            }
        }
        else Debug.Log("<color=red>[취소]</color> 구조물이 아닙니다. 배치 명령이 취소되었습니다.");

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