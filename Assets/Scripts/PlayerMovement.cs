using UnityEngine;
using UnityEngine.AI; // NavMesh 이용
using UnityEngine.EventSystems; // UI 클릭 방지

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
    private bool _isLeftClickPending = false; // 클릭 신호를 담을 변수
    private UnitCombat _unitCombat; // 컴포넌트 참조용 추가

    // UI 및 이팩트
    //[Header("Settings")]
    //[SerializeField] private GameObject selectionVisual; // 선택 시 표시될 원형 UI 등

    // 유닛 레이어만 검사하도록 설정 (Inspector에서 Unit 레이어 선택)
    [SerializeField] private LayerMask unitLayer;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>(); // NavMesh 참조
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // 자식 스프라이트 참조
        _unitCombat = GetComponent<UnitCombat>(); // UnitCombat.cs 참조

        // 2D이므로 회전축 고정
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }

    private void Start()
    {
        if (InputManager.Instance != null && InputManager.Instance.InputActions != null)
        {
            // 입력 이벤트 구독
            InputManager.Instance.InputActions.Player.LeftClick.performed += _ => _isLeftClickPending = true; // 좌클릭 신호
            InputManager.Instance.InputActions.Player.RightClick.performed += ctx => // 마우스 우클릭
            {
                if(_isSelected) // 선택 될 상태일 때만 이동을 시도
                { 
                    TryMove(); 
                }
            };
        }
        else
        {
            Debug.LogError("InputManager를 찾을 수 없습니다! 씬에 InputManager 오브젝트가 있는지 확인하세요.");
        }
    }

    void Update()
    {
        // 건설 단계라면 유닛 조작(선택/이동) 불가
        if (BuildManager.Instance != null && BuildManager.Instance.IsBuildingPhase) return;

        if (!_agent.isOnNavMesh || !_agent.isActiveAndEnabled) return; // 선택 중이 아니라면 리턴

        // 이동 중인데 경로가 끊겼거나 더 이상 갈 수 없다면 중지
        if (_agent.pathStatus == NavMeshPathStatus.PathPartial || !_agent.hasPath)
        {
            if (_agent.remainingDistance < 0.1f)
            {
                _agent.ResetPath(); // 목적지 도착 또는 이동 불가 시 경로 초기화
            }
        }

        if (_isLeftClickPending)
        {
            TrySelectOrDeselect();
            _isLeftClickPending = false; // 처리 후 신호 초기화
        }

        HandleSpriteFlip(); // 좌우 반전
    }

    /// <summary>
    /// 1. 플레이어 좌클릭(선택)
    /// 2. 다른 곳 좌클릭 시 해제
    /// </summary>
    private void TrySelectOrDeselect()
    {
        // 만약 클릭한 곳이 UI 위라면 로직 실행 안함
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 mousePos = GetMouseWorldPos(); // 마우스 위치값
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, unitLayer);

        // 클릭한 대상이 있을 경우
        if (hit.collider != null)
        {
            // 그 대상이 '나'라면 선택
            if (hit.collider.gameObject == gameObject)
            {
                SetSelection(true);
                Debug.Log("유닛 본인 선택됨");
            }
            else
            {
                // 그 대상이 '다른 오브젝트'라면 선택 해제
                SetSelection(false);
                Debug.Log("다른 대상 클릭으로 인한 해제");
            }
        }
        // 아예 빈 공간(바닥)을 클릭했을 경우
        else
        {
            // 디펜스 게임 특성상 이동을 위해 바닥을 찍는 경우가 많으므로, 
            // 좌클릭으로 빈 바닥을 찍었을 때만 해제하도록 합니다.
            SetSelection(false);
            Debug.Log("빈 공간 클릭으로 인한 해제");
        }
    }

    /// <summary>
    /// 선택 여부에 따라 bool 값을 판정하고, 이펙트 및 UI를 변화시키는 메서드
    /// </summary>
    /// <param name="value">선택 여부에 따른 bool</param>
    private void SetSelection(bool value)
    {
        _isSelected = value;
        //if (selectionVisual) selectionVisual.SetActive(value);
    }

    /// <summary>
    /// 2. 땅 위에 우클릭(이동 명령)
    /// </summary>
    private void TryMove()
    {
        // 에이전트가 활성화 상태이고 NavMesh 위에 있을 때만 이동 명령 수행
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
        {
            Debug.LogWarning($"{gameObject.name}: NavMeshAgent가 준비되지 않아 이동할 수 없습니다.");
            return;
        }

        Vector2 mousePos = InputManager.Instance.InputActions.Player.Point.ReadValue<Vector2>();
        Vector3 targetPos = Camera.main.ScreenToWorldPoint(mousePos);
        targetPos.z = 0;

        // UI 클릭 방지
        if (EventSystem.current.IsPointerOverGameObject()) return;

        NavMeshPath path = new NavMeshPath();

        if (_agent.CalculatePath(targetPos, path))
        {
            _agent.SetPath(path);

            // 이동 명령을 내렸으므로 전투 스크립트에 강제 이동 상태임을 알림
            UnitCombat combat = GetComponent<UnitCombat>();
            if (combat != null)
            {
                combat.SetManualCommand(null, true);
            }
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
    private void HandleSpriteFlip()
    {
        // 공격 타겟이 있고 강제 이동 중이 아니라면 타겟을 바라봄
        if (_unitCombat != null && _unitCombat.CurrentTarget != null && !_unitCombat.IsForceMoving)
        {
            _spriteRenderer.flipX = _unitCombat.CurrentTarget.transform.position.x < transform.position.x;
            return;
        }

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

    /// <summary>
    /// 에디터 뷰에서 길찾기 경로와 목적지를 시각화합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        // 1. 에이전트와 경로 데이터가 없으면 그리지 않음
        if (_agent == null || _agent.path == null) return;

        // 2. 현재 이동 경로를 선으로 표시 (녹색)
        Gizmos.color = Color.green;
        var path = _agent.path;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
        }

        // 3. 최종 목적지에 작은 구체 표시 (빨간색)
        if (_agent.hasPath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_agent.destination, 0.2f);
        }

        // 공격 범위 시각화
        UnitStat stat = GetComponent<UnitStat>();
        if (stat != null)
        {
            // 선택되었을 때만 범위를 보고 싶다면 if (_isSelected)를 감싸주세요.
            if (_isSelected)
            {
                // 공격 범위 원의 색상 설정 (반투명한 빨간색)
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

                // 유닛 위치를 중심으로 원 그리기
                // 2D에서는 DrawWireSphere가 가장 간편합니다.
                Gizmos.DrawWireSphere(transform.position, stat.AttackRange);
            }
        }
    }
}