using UnityEngine;

public enum EnemyType { Melee, Range }

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;

    [Header("Base Stats")]
    public float maxHp = 50f;
    public float attackPower = 5f;
    public float defense = 2f;
    public float moveSpeed = 2f;

    [Header("Combat Settings")]
    public EnemyType attackType;
    public float attackRange = 1.5f;
    public float attackSpeed = 1f; // 초당 공격 횟수
    [Range(0, 100)] public float accuracy = 80f; // 원거리 전용
}