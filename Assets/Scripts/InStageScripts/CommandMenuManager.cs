using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Linq;

public class CommandMenuManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject commandPanel;
    public Button healButton;

    private UnitMedic _selectedMedic;
    private UnitStat _clickedTarget;

    private void Start()
    {
        if (commandPanel != null) commandPanel.SetActive(false);
    }

    private void Update()
    {
        // 1. 우클릭 체크
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // 2. E 키 체크
            if (Keyboard.current.eKey.isPressed)
            {
                //Debug.Log("[CommandMenu] E + 우클릭 감지됨. 로직 시작.");
                TryOpenHealPanel();
            }
            else if (commandPanel != null && commandPanel.activeSelf)
            {
                commandPanel.SetActive(false);
            }
        }
    }

    private void TryOpenHealPanel()
    {
        // 1. 현재 씬에서 선택(IsSelected)된 모든 유닛들 중 메딕 컴포넌트 찾기
        var selectedUnits = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
                            .Where(u => u.IsSelected)
                            .ToList();

        // 선택된 유닛 중 첫 번째 메딕을 가져옴
        _selectedMedic = selectedUnits.Select(u => u.GetComponent<UnitMedic>()).FirstOrDefault(m => m != null);

        if (_selectedMedic == null)
        {
            //Debug.LogWarning("[CommandMenu] 선택된 유닛 중 메딕이 없습니다! 먼저 메딕을 선택하세요.");
            return;
        }

        // 2. 마우스 월드 좌표 계산
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        // 3. [중요] 레이어 마스크 설정 (BuildArea 등을 무시하고 Unit 레이어만 체크)
        // 유니티 인스펙터 상의 레이어 이름이 "Unit"이어야 합니다.
        int unitLayerMask = LayerMask.GetMask("Unit");

        // 4. 레이캐스트 실행 (Unit 레이어만 필터링)
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, unitLayerMask);

        if (hit.collider != null)
        {
            // 5. 콜라이더가 있는 오브젝트나 그 부모로부터 UnitStat 컴포넌트 추출
            _clickedTarget = hit.collider.GetComponentInParent<UnitStat>();

            if (_clickedTarget != null)
            {
                // 메딕 본인을 클릭한 경우 제외
                if (_clickedTarget.gameObject == _selectedMedic.gameObject)
                {
                    //Debug.Log("[CommandMenu] 메딕 본인은 치료 대상으로 선택할 수 없습니다.");
                    return;
                }

                // 모든 조건 만족 시 패널 표시
                //Debug.Log($"[CommandMenu] 치료 대상 감지: {_clickedTarget.name}");
                ShowCommandMenu(mousePos);
            }
        }
        else
        {
            // 레이어 마스크에 걸리는게 없을 때 로그 (디버깅용)
            //Debug.Log("[CommandMenu] 'Unit' 레이어에서 클릭된 오브젝트가 없습니다. 유닛의 Layer 설정을 확인하세요.");
        }
    }

    private void ShowCommandMenu(Vector2 screenPos)
    {
        if (commandPanel == null) return;

        //Debug.Log("[CommandMenu] 패널 표시!");
        commandPanel.SetActive(true);
        commandPanel.transform.position = screenPos;

        healButton.onClick.RemoveAllListeners();
        healButton.onClick.AddListener(() => {
            _selectedMedic.StartHealCommand(_clickedTarget);
            commandPanel.SetActive(false);
        });
    }
}