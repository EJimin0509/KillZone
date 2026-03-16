using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CommandMenuManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject commandPanel;
    public Button healButton;

    // [중요] 외부에서 현재 선택된 유닛(메딕)을 주입해주거나, 
    // 전역 선택 매니저에서 가져와야 합니다.
    private UnitMedic _selectedMedic;
    private UnitStat _clickedTarget;

    private void Start()
    {
        // PlayerMovement와 동일한 방식으로 구독
        InputManager.Instance.InputActions.Player.RightClick.performed += OnRightClick;

        if (commandPanel != null) commandPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 구독 해제
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.RightClick.performed -= OnRightClick;
        }
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        // 1. 현재 조종 중인 유닛이 있는지 확인
        // (예시: 유닛 선택 시스템에서 현재 선택된 유닛을 가져옴)
        // _selectedMedic = SelectionManager.Instance.SelectedUnit?.GetComponent<UnitMedic>();

        HandleRightClick();
    }

    private void HandleRightClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Unit"))
        {
            _clickedTarget = hit.collider.GetComponent<UnitStat>();

            // 조건: 메딕을 선택 중이고, 대상이 내가 아니며, 아군일 때
            if (_selectedMedic != null && _clickedTarget != null && _clickedTarget.gameObject != _selectedMedic.gameObject)
            {
                ShowCommandMenu(mousePos);
            }
        }
        else
        {
            // 빈 땅 클릭 시 메뉴 닫기
            if (commandPanel != null) commandPanel.SetActive(false);
        }
    }

    private void ShowCommandMenu(Vector2 screenPos)
    {
        commandPanel.SetActive(true);
        commandPanel.transform.position = screenPos;

        healButton.onClick.RemoveAllListeners();
        healButton.onClick.AddListener(() => {
            _selectedMedic.StartHealCommand(_clickedTarget);
            commandPanel.SetActive(false);
        });
    }

    // 외부(예: 선택 매니저)에서 호출하여 현재 명령을 내릴 유닛을 설정
    public void SetSelectedMedic(UnitMedic medic)
    {
        _selectedMedic = medic;
    }
}