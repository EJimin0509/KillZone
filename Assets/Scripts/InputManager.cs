using UnityEngine;

// 플레이어 입력 스크립트
// 어디서든 InputManager.Instance로 접근할 수 있게 해주는 싱글톤 적용

public class InputManager : MonoBehaviour
{
    private static InputManager _instance; // 싱글톤 인스턴스
    private static bool _isQuitting = false;

    /// <summary>
    /// 싱글톤 패턴
    /// </summary>
    public static InputManager Instance
    {
        get
        {
            if (_isQuitting) return null;

            if (_instance == null)
            {
                // 씬에 없으면 새로 생성
                _instance = FindAnyObjectByType<InputManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("@InputManager");
                    _instance = go.AddComponent<InputManager>();
                }
            }
            return _instance;
        }
    }

    private PlayerInputAction _inputActions; // PlayerInputAction Input Action 파일
    /// <summary>
    /// PlayerInputAction을 참조한 인스턴스 생성
    /// </summary>
    public PlayerInputAction InputActions => _inputActions;

    private void Awake()
    {
        // 싱글톤 중복 방지
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음

        // 인스턴스 생성
        _inputActions = new PlayerInputAction();
    }

    private void OnEnable()
    {
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        // 싱글톤 참조 해제
        if (_instance == this)
        {
            _instance = null;
        }
    }
}