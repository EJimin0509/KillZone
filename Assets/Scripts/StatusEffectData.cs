using UnityEngine;

public enum EffectType { DoT, StatChange, Interrupt }
public enum TargetStat { None, HP, HP_MAX, ATK, AS, ACC, DEF, MOVE_SPEED, FIX_SPEED, HP_HEAL, MTL_MAX, MTL_NOW, MTL_SPEED }

[CreateAssetMenu(fileName = "New Status Effect", menuName = "Scriptable Objects/StatusEffect")]
public class StatusEffectData : ScriptableObject
{
    public string effectName;            // 상태이상 이름 (예: 출혈, 고장)
    public EffectType effectType;        // 도트데미지, 스탯값 변동, 중단
    public TargetStat targetStat;        // 영향을 줄 스탯 종류

    public float applyValue;             // 적용 수치 (예: -0.4, 20)
    public float duration;               // 지속 시간 (0이면 영구 적용)
    public float tickInterval = 1f;      // DoT 데미지일 경우 틱 간격 (기본 1초)

    [Header("Interrupt Settings (중단 타입 전용)")]
    public bool disableAttack = false;   // 공격 비활성화 (고장, 공황 등에 사용)
    public bool disableMovement = false; // 이동 비활성화 (밀치면서 기절 등에 사용)
}