using System.Collections.Generic;
using UnityEngine;

// 이 enum과 클래스들이 없으면 EnemyAI에서 에러가 날 수 있으므로 여기에 정의합니다.
public enum TargetStat { MoveSpeed, AttackPower, AttackSpeed }

[System.Serializable]
public class StatModifier
{
    public TargetStat stat;
    public float value;
}

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Status Effects/Effect Data")]
public class StatusEffectData : ScriptableObject
{
    public string effectName;
    public float duration;
    public float tickInterval = 1f;
    public float dotDamage = 0f;

    [Header("Knockback Settings")]
    public float knockbackDistance = 0f;
    public float knockbackDuration = 0.2f;

    [Header("Restrictions")]
    public bool disableMovement;
    public bool disableAttack;

    public List<StatModifier> statModifiers = new List<StatModifier>();
}