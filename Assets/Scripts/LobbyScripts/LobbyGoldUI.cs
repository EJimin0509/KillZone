using UnityEngine;
using TMPro;

public class LobbyGoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalGoldText;

    private void OnEnable()
    {
        // 로비로 돌아올 때마다 실행
        RefreshGoldDisplay();
    }

    private void Start()
    {
        RefreshGoldDisplay();
    }

    public void RefreshGoldDisplay()
    {
        if (GameManager.Instance != null && totalGoldText != null)
        {
            // GameManager에 저장된 현재 총 골드 표시
            totalGoldText.text = GameManager.Instance.currentGold.ToString("N0"); // N0는 천단위 콤마(,) 표시
        }
    }
}