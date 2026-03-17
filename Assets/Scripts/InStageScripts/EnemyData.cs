using UnityEngine;

public enum EnemyType { Melee, Range } // 근접인지 원거리인지

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName; // 이름

    [Header("Base Stats")]
    public float maxHp = 50f;      // HP
    public float attackPower = 5f; // 공격력
    public float defense = 1f;     // 방어력
    public float moveSpeed = 2f;   // 이동 속도

    [Header("Combat Settings")]
    public EnemyType attackType;   // 공격 타입(근, 원)
    public float attackRange = 1.5f; // 공격 사거리
    public float attackSpeed = 1f; // 초당 공격 횟수
    [Range(0, 100)] public float accuracy = 80f; // 원거리 전용

    [Header("Reward")]
    public int killReward = 3;
}