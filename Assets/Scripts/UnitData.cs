using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
[System.Serializable]
public class UnitData : ScriptableObject
{
    public string unitName;

    public int hp;      // 체력
    public int melee;   // 격투
    public int range;   // 사격
    public int repair;  // 수리
    public int medic;   // 의술
    public int will;    // 의지
    public int faith;   // 신앙


    [Header("Growth Settings")]
    public float upgradeMultiplier = 0.1f; // 단계당 10%씩 증가한다고 가정

    public void SetDefault()
    {
        hp = 1; melee = 1; range = 0; repair = 1; medic = 0; will = 1; faith = 1;
    }
}
