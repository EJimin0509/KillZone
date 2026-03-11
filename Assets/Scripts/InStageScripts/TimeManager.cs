using UnityEngine;
using UnityEngine.InputSystem;

public class TimeManager : MonoBehaviour
{
    private float[] _timeScales = { 1.0f, 2.0f, 0.5f };
    private int _currentIndex = 0;

    private void Start()
    {
        // 탭 키(SpeedControl) 액션 구독
        InputManager.Instance.InputActions.Player.SpeedControl.performed += OnSpeedToggle;
    }

    private void OnSpeedToggle(InputAction.CallbackContext context)
    {
        // 인덱스 순환
        _currentIndex = (_currentIndex + 1) % _timeScales.Length;
        float newScale = _timeScales[_currentIndex];

        Time.timeScale = newScale;

        // 성능 최적화를 위해 fixedDeltaTime도 배속에 맞춰 조정
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Debug.Log($"<color=yellow>[배속 변경]</color> 현재 배속: {newScale}x");
    }

    // 싱글톤이나 다른 매니저에서 현재 배속 확인용
    public float GetCurrentSpeed() => _timeScales[_currentIndex];
}