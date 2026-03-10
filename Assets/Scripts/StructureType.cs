using UnityEngine;

public enum StructureType { Obstacle, Pad, Mine, Artillery, Tower }

[CreateAssetMenu(fileName = "New Structure", menuName = "Scriptable Objects/StructureData")]
public class StructureData : ScriptableObject
{
    public string structureName;     // 구조물 이름 (예: 독 발판, 신기전)
    public StructureType type;       // 장애물, 발판, 지뢰, 공성무기, 망루

    [Header("Stats")]
    public int cost;                 // 건설 비용
    public float range;              // 사거리 (지뢰는 폭발/감지 범위)
    public float damage;             // 데미지
    public float hpOrDurability;     // HP 또는 내구도
    [Range(0, 100)] public float accuracy; // 명중률

    [Header("Effects")]
    public StatusEffectData applyEffect; // 적용할 상태이상 (둔화, 출혈, 중독, 기절 등)

    [TextArea]
    public string description;       // 구조물 설명
}