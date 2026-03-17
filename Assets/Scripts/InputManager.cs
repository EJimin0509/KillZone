using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    private static bool _isQuitting = false;

    public static InputManager Instance
    {
        get
        {
            if (_isQuitting) return null;

            if (_instance == null)
            {
                _instance = FindAnyObjectByType<InputManager>();
                // 주의: 씬 종속형이므로 자동으로 생성하지 않습니다. 
                // 필요하다면 각 스테이지 씬에 직접 배치하거나 여기서 생성 후 DontDestroy를 빼야 합니다.
            }
            return _instance;
        }
    }

    private PlayerInputAction _inputActions;
    public PlayerInputAction InputActions => _inputActions;

    private void Awake()
    {
        // 1. 싱글톤 중복 방지
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // 2. [핵심 수정] DontDestroyOnLoad를 제거합니다.
        // 이제 이 오브젝트는 스테이지 씬이 끝나면 자동으로 파괴됩니다.

        // 3. 인스턴스 생성
        _inputActions = new PlayerInputAction();
    }

    private void OnEnable()
    {
        if (_inputActions != null) _inputActions.Enable();
    }

    private void OnDisable()
    {
        if (_inputActions != null) _inputActions.Disable();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            // 4. 파괴될 때 인풋 액션 메모리 해제 및 구독 연결 강제 종료
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions = null;
            }
            _instance = null;
            //Debug.Log("<color=yellow>InputManager 파괴: 모든 이벤트 연결이 해제되었습니다.</color>");
        }
    }
}