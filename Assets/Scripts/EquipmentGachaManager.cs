using UnityEngine;

public class EquipmentGachaManager : MonoBehaviour
{
    public EquipmentGenerator generator;
    public EquipmentGachaUI gachaUI;

    private EquipmentData _lastGeneratedItem;

    private void Start()
    {
        gachaUI.SetButtonState(true, false);
    }

    public void OnClickEquipmentGacha()
    {
        // 1. 등급 결정 확률
        float rand = Random.Range(0f, 100f);
        string grade;
        if (rand <= 1f) grade = "최상급";
        else if (rand <= 10f) grade = "상급";
        else if (rand <= 30f) grade = "중급";
        else grade = "하급";

        // 2. 장비 생성 (부위 랜덤)
        EquipmentData result = generator.GenerateRandomEquipment();

        InventoryManager.Instance.AddEquipment(result); // 인벤토리에 저장

        // 3. UI 출력
        gachaUI.DisplayEquipment(_lastGeneratedItem, grade);
        gachaUI.SetButtonState(false, true);
    }

    public void OnClickConfirm()
    {
        if (_lastGeneratedItem == null) return;

        // 최종 인벤토리 저장
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddEquipment(_lastGeneratedItem);
        }

        // 초기화
        _lastGeneratedItem = null;
        gachaUI.ClearDisplay();
        gachaUI.SetButtonState(true, false);
    }
}