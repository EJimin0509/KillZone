using UnityEngine;

public class EquipmentGachaManager : MonoBehaviour
{
    public EquipmentGenerator generator;

    public void OnClickEquipmentGacha()
    {
        // 1. 등급 결정 확률 (기획서 20P)
        float rand = Random.Range(0f, 100f);
        string grade;
        if (rand <= 1f) grade = "최상급";
        else if (rand <= 10f) grade = "상급";
        else if (rand <= 30f) grade = "중급";
        else grade = "하급";

        // 2. 장비 생성 (부위 랜덤)
        EquipmentData result = generator.GenerateRandomEquipment();

        InventoryManager.Instance.AddEquipment(result); // 인벤토리에 저장

        // 3. 결과 출력
        ShowResult(result, grade);
    }

    private void ShowResult(EquipmentData item, string grade)
    {
        string bonusText = "";
        foreach (var b in item.additionalStatBonuses)
        {
            bonusText += $"[{b.type}: +{b.bonusLevel}] ";
        }

        Debug.Log($"<b><color=cyan>[장비 뽑기 결과]</color></b>");
        Debug.Log($"등급: {grade} | 부위: {item.type} | 이름: {item.equipName}");

        if (item.type == EquipmentType.Melee || item.type == EquipmentType.Bow)
            Debug.Log($"기본 사거리: {item.attackRange}");
        else
            Debug.Log($"기본 방어력: {item.defense}");

        Debug.Log($"부여된 보너스: {bonusText}");
    }
}