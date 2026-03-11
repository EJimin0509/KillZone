using UnityEngine;

public class DefenseBase : MonoBehaviour
{
    // 현재 활성화 된 Baes를 참조하기 위함
    public static DefenseBase Current { get; private set; }

    [Header("Base Stats")]
    public float maxHp = 5000f; // 최대 체력
    public float currentHp;     // 현재 체력

    private void OnEnable()
    {
        // 구조물이 생성되거나 활성화될 때 자신을 등록
        Current = this;

        // 업그레이드 요소를 여기에 반영해야 함

        currentHp = maxHp;
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
        Debug.Log($"구조물 피격! 남은 HP: {currentHp}");

        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("구조물 파괴 - 게임 오버");
        
        // 스테이지 매니저에게 패배 알림 로직
    }
}