using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultUIController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject resultPanel;      // 결과 창 부모 오브젝트

    [Header("Texts")]
    public TextMeshProUGUI resultTitleText; // "VICTORY" 또는 "DEFEAT"
    public TextMeshProUGUI goldValueText;   // 획득한 골드 수치 (숫자만)

    [Header("Buttons")]
    public Button retryButton;          // 다시하기 버튼
    public Button lobbyButton;          // 로비로 버튼

    private void Start()
    {
        // 시작할 때는 결과창 숨기기
        if (resultPanel != null) resultPanel.SetActive(false);

        // 버튼 리스너 등록
        if (retryButton != null)
            retryButton.onClick.AddListener(RestartStage);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(GoToLobby);
    }

    /// <summary>
    /// 게임 결과를 화면에 표시합니다.
    /// </summary>
    /// <param name="isVictory">승리 여부 (true: Victory, false: Defeat)</param>
    public void ShowResult(bool isVictory)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        // 1. 결과 타이틀 및 색상 설정
        if (resultTitleText != null)
        {
            resultTitleText.text = isVictory ? "VICTORY" : "DEFEAT";
            resultTitleText.color = isVictory ? Color.yellow : Color.red;
        }

        // [추가] 2. 승리 시 스테이지 언락 처리 (9, 10번 요구사항)
        if (isVictory && GameManager.Instance != null)
        {
            // 현재 씬 이름을 기반으로 스테이지 인덱스 계산 (예: "Stage1" -> 0)
            // 혹은 GameManager에 현재 스테이지 인덱스를 저장해두고 사용하세요.
            int currentIdx = GameManager.Instance.currentStageIndex;
            GameManager.Instance.ClearStage(currentIdx);
        }

        // 3. 획득 재화량 표시
        if (goldValueText != null && GameManager.Instance != null)
        {
            goldValueText.text = GameManager.Instance.stageGold.ToString();
        }
    }

    // 다시하기 버튼 로직
    public void RestartStage()
    {
        Time.timeScale = 1f; // 시간 초기화 필수

        // 현재 활성화된 씬을 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 로비로 돌아가기 로직
    public void GoToLobby()
    {
        Time.timeScale = 1f; // 시간 초기화 필수

        // 스테이지 골드를 영구 골드에 합산하고 저장 (GameManager에서 처리 권장)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FinalizeStageGold();
        }

        // 로비 씬으로 이동 (빌드 인덱스 1번)
        SceneManager.LoadScene(1);
    }
}