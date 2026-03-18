using UnityEngine;

public class DefenseBase : MonoBehaviour
{
    public static DefenseBase Current;
    public float maxHp = 100f;
    public float currentHp;

    private bool _isDestroyed = false; // 중복 호출 방지

    private void Awake()
    {
        Current = this;
        currentHp = maxHp;
    }

    public void TakeDamage(float damage, Vector2 attackerPosition)
    {
        if (_isDestroyed) return;

        currentHp -= damage;
        Debug.Log($"Base HP: {currentHp}");

        if (currentHp <= 0)
        {
            currentHp = 0;
            OnBaseDestroyed();
        }
    }

    // 인터페이스 미사용 시를 위한 오버로딩 (단순 수치만 받는 경우)
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    private void OnBaseDestroyed()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        Debug.Log("Base Destroyed! Game Over.");

        // GameManager에게 패배 상황을 전달합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(false); // false는 패배를 의미
        }
    }
}