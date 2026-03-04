using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
public class UnitData : ScriptableObject
{
    public string unitName;

    [Header("Base Stats (Level 1)")]
    public float baseHp = 100f; // HP
    public float baseAttackPower = 10f; // 공격력
    public float baseRangeAccuracy = 70f; // 원거리 명중률
    public float baseRepairSpeed = 1f; // 수리 속도
    public float baseHealSpeed = 1f; // 치료 속도
    public float baseMentalValue = 100f; // 정신력
    public float baseMentalRegenRadius = 5f; // 멘탈 회복 범위
    public float baseMentalRegenAmount = 1f; // 초당 회복량

    [Header("Growth Settings")]
    public float upgradeMultiplier = 0.1f; // 단계당 10%씩 증가한다고 가정
}
