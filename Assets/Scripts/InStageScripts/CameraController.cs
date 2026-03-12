using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 15f; // 속도를 조금 더 높여 쾌적하게 설정
    [SerializeField] private Vector2 mapMin = new Vector2(-18, -20);
    [SerializeField] private Vector2 mapMax = new Vector2(18, 20);

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 20f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 10f;

    private Camera _cam;
    private PlayerInputAction.PlayerActions _playerActions; // 액션 직접 참조

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // InputManager의 인스턴스를 통해 Player 액션 맵을 가져옴
        _playerActions = InputManager.Instance.InputActions.Player;

        // 스크롤 휠(줌)은 이벤트 방식이 효율적이므로 그대로 유지
        _playerActions.MiddleScroll.performed += HandleZoom;
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 input = _playerActions.MoveCamera.ReadValue<Vector2>();

        if (input == Vector2.zero)
        {
            // 움직이지 않더라도 줌 변경 등으로 인해 범위를 벗어날 수 있으므로 항상 Clamp 적용
            ApplyBoundaryClamp();
            return;
        }

        // deltaTime을 곱해 프레임 독립적인 이동 구현
        Vector3 moveDir = new Vector3(input.x, input.y, 0);
        transform.position += moveDir * (moveSpeed * Time.unscaledDeltaTime);

        ApplyBoundaryClamp();
    }

    private void HandleZoom(InputAction.CallbackContext context)
    {
        float scrollDelta = context.ReadValue<Vector2>().y;
        if (Mathf.Abs(scrollDelta) < 0.01f) return;

        // Orthographic Size 조절로 줌 인/아웃
        float newSize = _cam.orthographicSize - (scrollDelta * zoomSpeed * 0.01f);
        _cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
    }

    // 카메라의 크기를 고려하여 절대 좌표를 벗어나지 않게 가두는 로직
    private void ApplyBoundaryClamp()
    {
        float camHeight = _cam.orthographicSize;
        float camWidth = _cam.orthographicSize * _cam.aspect;

        // 카메라 중심점이 가질 수 있는 최소/최대 좌표 계산
        // 맵의 끝 좌표에서 카메라 절반 크기만큼 안쪽으로 들어온 지점이 한계점입니다.
        float minX = mapMin.x + camWidth;
        float maxX = mapMax.x - camWidth;
        float minY = mapMin.y + camHeight;
        float maxY = mapMax.y - camHeight;

        // 만약 카메라 크기가 맵 크기보다 커지면 중앙에 고정
        if (minX > maxX) minX = maxX = (mapMin.x + mapMax.x) / 2f;
        if (minY > maxY) minY = maxY = (mapMin.y + mapMax.y) / 2f;

        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

        transform.position = clampedPos;
    }
}