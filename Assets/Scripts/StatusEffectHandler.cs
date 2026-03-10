using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectHandler : MonoBehaviour, IStatusEffectable
{
    private Dictionary<string, Coroutine> _activeEffects = new Dictionary<string, Coroutine>();
    private IDamageable _healthComponent;

    // 외부(UnitStat 등)에서 읽어가서 최종 스탯 계산에 더해줄 변동값 딕셔너리
    public Dictionary<TargetStat, float> StatModifiers = new Dictionary<TargetStat, float>();

    // 상태이상으로 인한 중단 상태 여부 확인
    public bool IsAttackDisabled { get; private set; }
    public bool IsMovementDisabled { get; private set; }

    private void Awake()
    {
        _healthComponent = GetComponent<IDamageable>();
    }

    public void ApplyStatusEffect(StatusEffectData effect)
    {
        if (_activeEffects.ContainsKey(effect.effectName))
        {
            // 이미 걸려있는 상태이상이면 지속시간 초기화를 위해 기존 코루틴 정지
            StopCoroutine(_activeEffects[effect.effectName]);
            _activeEffects.Remove(effect.effectName);
        }

        Coroutine c = StartCoroutine(EffectRoutine(effect));
        _activeEffects.Add(effect.effectName, c);
    }

    public void RemoveStatusEffect(StatusEffectData effect)
    {
        if (_activeEffects.ContainsKey(effect.effectName))
        {
            StopCoroutine(_activeEffects[effect.effectName]);
            _activeEffects.Remove(effect.effectName);
            RevertEffectModifiers(effect);
        }
    }

    private IEnumerator EffectRoutine(StatusEffectData effect)
    {
        float timer = 0f;

        // 1. 스탯 변동 및 중단 상태 적용
        if (effect.effectType == EffectType.StatChange)
        {
            if (!StatModifiers.ContainsKey(effect.targetStat)) StatModifiers[effect.targetStat] = 0;
            StatModifiers[effect.targetStat] += effect.applyValue;
        }
        else if (effect.effectType == EffectType.Interrupt)
        {
            if (effect.disableAttack) IsAttackDisabled = true;
            if (effect.disableMovement) IsMovementDisabled = true;
        }

        // 2. 시간 경과 및 DoT 처리
        while (effect.duration <= 0 || timer < effect.duration)
        {
            if (effect.effectType == EffectType.DoT && _healthComponent != null)
            {
                _healthComponent.TakeDamage(effect.applyValue); // 매 틱마다 데미지
            }

            yield return new WaitForSeconds(effect.tickInterval);
            timer += effect.tickInterval;

            if (effect.duration <= 0) break; // 즉발/영구 효과면 1회 적용 후 루프 탈출
        }

        // 3. 지속시간 종료 시 스탯 및 상태 복구
        RevertEffectModifiers(effect);
        _activeEffects.Remove(effect.effectName);
    }

    private void RevertEffectModifiers(StatusEffectData effect)
    {
        if (effect.effectType == EffectType.StatChange && StatModifiers.ContainsKey(effect.targetStat))
        {
            StatModifiers[effect.targetStat] -= effect.applyValue;
        }
        else if (effect.effectType == EffectType.Interrupt)
        {
            if (effect.disableAttack) IsAttackDisabled = false;
            if (effect.disableMovement) IsMovementDisabled = false;
        }
    }
}