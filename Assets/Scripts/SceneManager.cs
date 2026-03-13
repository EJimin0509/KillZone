using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLauncher : MonoBehaviour
{
    // 가차 씬에서 인벤토리로 이동
    public void GoToInventory()
    {
        SceneManager.LoadScene("ally"); // 인벤토리 씬 이동
    }

    // 인벤토리에서 다시 가차 씬으로 이동
    public void GoToGacha()
    {
        SceneManager.LoadScene("StageHandle"); // 가차 씬 이동
    }

    public void GoToEquipmentInven()
    {
        SceneManager.LoadScene(3); // 장비 인벤토리 씬으로 이동
    }

    public void GoToInStage()
    {
        // 데이터 유실 방지를 위한 최종 동기화
        for (int i = 0; i < FormationManager.Instance.formationSlots.Length; i++)
        {
            InventoryManager.Instance.formationSlots[i] = FormationManager.Instance.formationSlots[i];
        }

        int count = 0;
        foreach (var slot in InventoryManager.Instance.formationSlots) if (slot != null) count++;

        if (count == 0)
        {
            Debug.LogWarning("편성된 유닛이 없습니다! 최소 한 명은 배치하세요.");
            return;
        }
        SceneManager.LoadScene(4); // 스테이지 씬으로 이동
    }

    // 스테이지 씬으로 이동
    public void GoToStage()
    {
        SceneManager.LoadScene(1); // 스테이지 선택 씬 이동
    }

    // 메인으로 돌아가기
    public void BackToMain()
    {
        SceneManager.LoadScene(0); // 메인으로 이동
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        // 유니티 에디터에서 실행 중일 때 재생 모드를 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 게임을 종료
        Application.Quit();
#endif
    }
}