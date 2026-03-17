using UnityEngine;

public enum EnemyType { Melee, Range }

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;

    [Header("Base Stats")]
    public float maxHp = 50f;
    public float attackPower = 5f;
    public float defense = 1f;
    public float moveSpeed = 2f;

    [Header("Combat Settings")]
    public EnemyType attackType;
    public float attackRange = 1.5f;
    public float attackSpeed = 1f;
    [Range(0, 100)] public float accuracy = 80f;

    [Header("Reward")]
    public int killReward = 3;      // 기본 처치 보상 (3원)
    public bool isBoss = false;     // 보스 여부
    public int bossBonus = 50;      // 보스일 경우 추가 금액

    // 최종 보상을 계산해서 가져오는 함수 (편의용)
    public int GetTotalReward()
    {
        return isBoss ? killReward + bossBonus : killReward;
    }
}