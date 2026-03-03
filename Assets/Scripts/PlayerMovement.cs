using UnityEngine;
using UnityEngine.AI;

// NavMeshAgent를 사용한 플레이어(유닛) 길찾기 알고리즘
// 1. 플레이어 좌클릭(선택)
// 2. 땅 위에 우클릭(이동 명령)
// 3. 최단거리 이동 명령
// 4. 갈 수 있는 경로인지 판단
public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent _agent; // 참조
    private bool _isSelected = false; // 1번 과정으로 유닛이 선택되었는지 확인하기 위한 bool
    private SpriteRenderer _spriteRenderer; // 좌우 반전 로직을 위한 스프라이트 가져오기

    // UI 및 이팩트
    //[Header("Settings")]
    //[SerializeField] private GameObject selectionVisual; // 선택 시 표시될 원형 UI 등

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>(); // NavMesh 참조
        _spriteRenderer = GetComponent<SpriteRenderer>(); // 스프라이트 참조

        // 2D이므로 회전축 고정
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }

    private void Start()
    {
        // 입력 이벤트 구독
        InputManager.Instance.InputActions.Player.LeftClick.performed += _ => TrySelect(); // 마우스 좌클릭
        InputManager.Instance.InputActions.Player.RightClick.performed += _ => TryMove(); // 마우스 우클릭
    }

    void Update()
    {
        FlipSprite(); // 좌우 반전

        // 이동 중인데 경로가 끊겼거나 더 이상 갈 수 없다면 중지
        if (_agent.pathStatus == NavMeshPathStatus.PathPartial || !_agent.hasPath)
        {
            if (_agent.remainingDistance < 0.1f)
            {
                _agent.ResetPath(); // 목적지 도착 또는 이동 불가 시 경로 초기화
            }
        }
    }

    /// <summary>
    /// 1. 플레이어 좌클릭(선택)
    /// </summary>
    private void TrySelect()
    {
        Vector2 mousePos = GetMouseWorldPos(); // 마우스 위치값
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        // 게임 오브젝트 콜라이더에 hit이 적중했을 경우
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            Debug.Log("유닛 선택");
            _isSelected = true;
            //if (selectionVisual) selectionVisual.SetActive(true); // 이펙트 변화
            // UI 로직 추가
        }
        else // 적중하지 않았을 경우
        {
            _isSelected = false;
            //if (selectionVisual) selectionVisual.SetActive(false); // 이펙트 변화
            // UI 로직 추가
        }
    }

    /// <summary>
    /// 2. 땅 위에 우클릭(이동 명령)
    /// </summary>
    private void TryMove()
    {
        if (!_isSelected) return;

        Vector2 mousePos = GetMouseWorldPos();

        // 3. 최단거리 이동 명령
        NavMeshPath path = new NavMeshPath();
        _agent.CalculatePath(mousePos, path);
        Debug.Log("이동 시작");

        // 4. 갈 수 없는 경로인지 판단
        if (path.status == NavMeshPathStatus.PathComplete)
        {
            _agent.SetDestination(mousePos);
        }
        else
        {
            // 경로가 막혔거나 불완전할 경우 이동 중지
            Debug.Log("경로가 막혀 이동할 수 없습니다.");
            _agent.ResetPath();
        }
    }

    /// <summary>
    /// 화면상의 마우스 좌표를 게임 속 월드좌표로 변경
    /// </summary>
    /// <returns>계산된 게임 월드상의 좌표를 반환</returns>
    private Vector2 GetMouseWorldPos()
    {
        Vector2 screenPos = InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>();
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    /// <summary>
    /// 좌우반전 메서드
    /// 현재 속도의 x값이 0.01f를 기준으로 좌우 반전을 판단
    /// </summary>
    private void FlipSprite()
    {
        // 아주 미세한 떨림으로 인해 반전되는 것을 방지하기 위해 0.01f 사용
        if (_agent.velocity.x > 0.01f)
        {
            // 오른쪽으로 이동 중
            _spriteRenderer.flipX = false;
            Debug.Log("오른쪽으로 플립");
        }
        else if (_agent.velocity.x < -0.01f)
        {
            // 왼쪽으로 이동 중
            _spriteRenderer.flipX = true;
            Debug.Log("왼쪽으로 플립");
        }
    }
}