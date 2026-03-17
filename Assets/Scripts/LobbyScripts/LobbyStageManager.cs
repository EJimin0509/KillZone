using UnityEngine;
using UnityEngine.UI;

public class LobbyStageManager : MonoBehaviour
{
    [System.Serializable]
    public class StageButtonInfo
    {
        public Button stageButton;      // 스테이지 입장 버튼
        public GameObject lockImage;    // 잠금 상태일 때 보여줄 이미지 (자물쇠 등)
    }

    [Header("Stage Settings")]
    public StageButtonInfo[] stageButtons; // 스테이지 1, 2, 3 순서대로 할당

    private void Start()
    {
        RefreshStageUI();
    }

    public void RefreshStageUI()
    {
        if (GameManager.Instance == null) return;

        bool[] unlocked = GameManager.Instance.stageUnlocked;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (i >= unlocked.Length) break;

            // [수정] i == 0 (첫 번째 스테이지)이면 무조건 true, 그 외에는 저장된 데이터를 따름
            bool isOpen = (i == 0) ? true : unlocked[i];

            // 2. 버튼 활성화/비활성화
            stageButtons[i].stageButton.interactable = isOpen;

            // 3. 잠금 이미지 표시/숨김
            if (stageButtons[i].lockImage != null)
            {
                stageButtons[i].lockImage.SetActive(!isOpen);
            }
        }
    }
}