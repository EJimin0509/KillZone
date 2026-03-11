using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private PlayerObj _playerObj;
    private float _baseSpeed;
    private bool _isSlowed = false;

    private void Awake()
    {
        _playerObj = GetComponent<PlayerObj>();
        _baseSpeed = _playerObj._charMS;
    }

    public void ApplySpeedModifier(float multiplier)
    {
        if (_isSlowed) return; // 이미 슬로우 중이면 무시
        _isSlowed = true;
        _playerObj._charMS = _baseSpeed * multiplier;
        Debug.Log("슬로우 적용! 속도: " + _playerObj._charMS);
    }

    public void RemoveSpeedModifier()
    {
        if (!_isSlowed) return; // 슬로우 아니면 무시
        _isSlowed = false;
        _playerObj._charMS = _baseSpeed;
        Debug.Log("슬로우 해제! 속도: " + _playerObj._charMS);
    }
}