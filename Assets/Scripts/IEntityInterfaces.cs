using UnityEngine;

// 체력을 가지고 데미지를 받을 수 있는 모든 객체
public interface IDamageable
{
    float CurrentHp { get; }
    void TakeDamage(float amount, Vector2 attackerPos = default);
}

// 상태이상(버프/디버프)을 받을 수 있는 모든 객체
public interface IStatusEffectable
{
    void ApplyStatusEffect(StatusEffectData effectData);
    void RemoveStatusEffect(StatusEffectData effectData);
}