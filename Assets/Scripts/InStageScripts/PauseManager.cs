using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("UI Panels")]
    public GameObject pausePanel; // 일시정지 팝업창

    [Header("Speed Buttons")]
    public Button speed05Btn;
    public Button speed10Btn;
    public Button speed20Btn;

    private bool _isPaused = false;
    private float[] _timeScales = { 1.0f, 2.0f, 0.5f }; // 순환할 배속 목록
    private int _speedIndex = 0;
    private float _currentActiveScale = 1.0f; // 일시정지 전 기록용

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        // 1. 버튼 리스너 연결
        speed05Btn?.onClick.AddListener(() => SetGameSpeed(0.5f));
        speed10Btn?.onClick.AddListener(() => SetGameSpeed(1.0f));
        speed20Btn?.onClick.AddListener(() => SetGameSpeed(2.0f));

        // 2. [구독 방식] InputManager를 통한 탭 키(SpeedControl) 액션 구독
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.SpeedControl.performed += OnSpeedToggle;
        }

        // 초기 배속 설정 (1배속)
        SetGameSpeed(1.0f);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 구독 해제
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.SpeedControl.performed -= OnSpeedToggle;
        }
    }

    private void Update()
    {
        // ESC 키는 UI 조작이므로 일반 Update에서 체크 (혹은 별도 액션 바인딩 가능)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // 탭 키 입력 시 호출되는 콜백
    private void OnSpeedToggle(InputAction.CallbackContext context)
    {
        if (_isPaused) return; // 일시정지 중엔 배속 변경 불가

        _speedIndex = (_speedIndex + 1) % _timeScales.Length;
        SetGameSpeed(_timeScales[_speedIndex]);
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        pausePanel.SetActive(_isPaused);

        if (_isPaused)
        {
            _currentActiveScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            ApplyScale(_currentActiveScale);
        }
    }

    public void SetGameSpeed(float speed)
    {
        _currentActiveScale = speed;

        // 인덱스 동기화 (탭 키 순환을 위해)
        for (int i = 0; i < _timeScales.Length; i++)
        {
            if (Mathf.Approximately(_timeScales[i], speed))
            {
                _speedIndex = i;
                break;
            }
        }

        if (!_isPaused) ApplyScale(speed);
        UpdateSpeedButtonColors(speed);
    }

    private void ApplyScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * scale;
    }

    private void UpdateSpeedButtonColors(float currentSpeed)
    {
        if (speed05Btn != null) speed05Btn.GetComponent<Image>().color = Mathf.Approximately(currentSpeed, 0.5f) ? Color.yellow : Color.white;
        if (speed10Btn != null) speed10Btn.GetComponent<Image>().color = Mathf.Approximately(currentSpeed, 1.0f) ? Color.yellow : Color.white;
        if (speed20Btn != null) speed20Btn.GetComponent<Image>().color = Mathf.Approximately(currentSpeed, 2.0f) ? Color.yellow : Color.white;
    }

    // --- 팝업 UI 버튼용 기능 ---
    public void Resume() => TogglePause();

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }
}