using UnityEngine;
using UnityEngine.UI;

public class StageSelectUI : MonoBehaviour
{
    public Button[] stageButtons; // 스테이지 버튼들
    public GameObject[] lockImages; // 잠금 아이콘들

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (GameManager.Instance == null) return;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (i < GameManager.Instance.stageUnlocked.Length)
            {
                bool isUnlocked = GameManager.Instance.stageUnlocked[i];

                // 버튼 활성화 여부
                stageButtons[i].interactable = isUnlocked;

                // 잠금 이미지 활성화 여부 (언락되면 꺼짐)
                if (i < lockImages.Length && lockImages[i] != null)
                {
                    lockImages[i].SetActive(!isUnlocked);
                }
            }
        }
    }
}