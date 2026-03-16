using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DefenseBase : MonoBehaviour
{
    // 현재 활성화 된 Baes를 참조하기 위함
    public static DefenseBase Current { get; private set; }

    [Header("Base Stats")]
    public float maxHp = 300f; // 최대 체력
    public float currentHp;     // 현재 체력
    public TextMeshProUGUI HPText; // 체력 표시 텍스트

    [Header("UI Connection")]
    public Slider hpSlider;

    private void OnEnable()
    {
        // 구조물이 생성되거나 활성화될 때 자신을 등록
        Current = this;

        currentHp = maxHp;
        UpdateBaseUI();
    }

    private void OnDisable()
    {
        // 파괴되거나 스테이지가 끝나면 참조 해제
        if (Current == this) Current = null;
    }

    /// <summary>
    /// 대미지를 받는 메서드
    /// </summary>
    /// <param name="damage">공격자 대미지</param>
    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        //Debug.Log($"구조물 피격! 남은 HP: {currentHp}");
        UpdateBaseUI();

        if (currentHp <= 0) Die();
    }

    private void UpdateBaseUI()
    {
        if (hpSlider != null)
        {
            // 슬라이더의 value를 0~1 사이 비율로 설정
            hpSlider.value = currentHp / maxHp;
        }

        HPText.text = $"{currentHp} / {maxHp}";
    }

    private void Die()
    {
        Debug.Log("구조물 파괴 - 게임 오버");
        
        // 스테이지 매니저에게 패배 알림 로직
    }
}