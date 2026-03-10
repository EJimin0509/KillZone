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
}