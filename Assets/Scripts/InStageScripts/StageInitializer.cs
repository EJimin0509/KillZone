using UnityEngine;

public class StageInitializer : MonoBehaviour
{
    [SerializeField] private int stageIndexForThisScene; // 1스테이지는 0, 2스테이지는 1...

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            // 현재 씬에 맞는 인덱스를 게임 매니저에 주입
            GameManager.Instance.currentStageIndex = stageIndexForThisScene;
            Debug.Log($"현재 씬의 스테이지 인덱스를 {stageIndexForThisScene}으로 설정함");
        }
    }
}