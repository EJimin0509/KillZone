using UnityEngine;
using UnityEngine.UI; // Slider를 쓰기 위해 필요합니다.

public class DefenseBase : MonoBehaviour
{
    public static DefenseBase Current;

    [Header("HP Settings")]
    public float maxHp = 100f;
    public float currentHp;

    [Header("UI References")]
    [SerializeField] private Slider hpSlider; // 유니티 인스펙터에서 체력바 Slider를 여기에 드래그하세요.

    private bool _isDestroyed = false;

    private void Awake()
    {
        Current = this;
        currentHp = maxHp;
    }

    private void Start()
    {
        // 시작할 때 슬라이더 초기화
        UpdateHPUI();
    }

    public void TakeDamage(float damage, Vector2 attackerPosition)
    {
        if (_isDestroyed) return;

        currentHp -= damage;

        // 데미지 입을 때마다 UI 갱신
        UpdateHPUI();

        Debug.Log($"Base HP: {currentHp}");

        if (currentHp <= 0)
        {
            currentHp = 0;
            UpdateHPUI(); // 0인 상태 마지막 갱신
            OnBaseDestroyed();
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    // [추가] 체력바 슬라이더를 갱신하는 함수
    private void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }
    }

    private void OnBaseDestroyed()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        Debug.Log("Base Destroyed! Game Over.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(false);
        }
    }
}