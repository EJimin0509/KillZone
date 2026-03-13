using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StatusEffectHandler : MonoBehaviour, IStatusEffectable
{
    private Dictionary<string, Coroutine> _activeEffects = new Dictionary<string, Coroutine>();
    private Coroutine _knockbackCoroutine;
    private IDamageable _healthComponent;
    private Vector3 _lockedPosition; // 넉백 중 좌표 강제 고정용

    public Dictionary<TargetStat, float> StatModifiers = new Dictionary<TargetStat, float>();

    public bool IsAttackDisabled { get; private set; }
    public bool IsMovementDisabled { get; private set; }
    public bool IsKnockingBack { get; private set; }

    private void Awake()
    {
        _healthComponent = GetComponent<IDamageable>();
    }

    // EnemyAI의 Update가 끝난 후 마지막에 실행되어 위치를 강제로 고정
    private void LateUpdate()
    {
        if (IsKnockingBack)
        {
            transform.position = _lockedPosition;
        }
    }

    public void ApplyStatusEffect(StatusEffectData effect)
    {
        if (effect == null) return;
        if (_activeEffects.ContainsKey(effect.effectName))
        {
            StopCoroutine(_activeEffects[effect.effectName]);
            _activeEffects.Remove(effect.effectName);
            RevertEffectModifiers(effect);
        }
        Coroutine c = StartCoroutine(EffectRoutine(effect));
        _activeEffects.Add(effect.effectName, c);
    }

    public void RemoveStatusEffect(StatusEffectData effect)
    {
        if (effect == null) return;
        if (_activeEffects.ContainsKey(effect.effectName))
        {
            StopCoroutine(_activeEffects[effect.effectName]);
            _activeEffects.Remove(effect.effectName);
            RevertEffectModifiers(effect);
        }
    }

    private IEnumerator EffectRoutine(StatusEffectData effect)
    {
        var agent = GetComponent<NavMeshAgent>();

        if (effect.disableAttack) IsAttackDisabled = true;
        if (effect.disableMovement)
        {
            IsMovementDisabled = true;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        if (effect.knockbackDistance > 0)
        {
            if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = StartCoroutine(KnockbackRoutine(effect.knockbackDistance, effect.knockbackDuration, effect.duration));
        }

        yield return new WaitForSeconds(effect.duration + (effect.knockbackDistance > 0 ? effect.knockbackDuration : 0f));

        RevertEffectModifiers(effect);
        _activeEffects.Remove(effect.effectName);
    }

    private IEnumerator KnockbackRoutine(float distance, float pushDuration, float stunDuration)
    {
        IsKnockingBack = true;
        var agent = GetComponent<NavMeshAgent>();

        // 유저님 버전의 로직: EnemyAI의 시각적 코루틴 중단
        MonoBehaviour targetScript = _healthComponent as MonoBehaviour;
        if (targetScript != null) targetScript.StopAllCoroutines();

        Vector3 knockbackDir = Vector3.left;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) knockbackDir = new Vector3(sr.flipX ? 1f : -1f, 0, 0);

        Vector3 startPos = transform.position;
        Vector3 finalTargetPos = startPos + (knockbackDir * distance);

        if (NavMesh.Raycast(startPos, finalTargetPos, out NavMeshHit hit, NavMesh.AllAreas))
            finalTargetPos = hit.position;

        if (agent != null) agent.enabled = false;

        float time = 0f;
        while (time < pushDuration)
        {
            time += Time.deltaTime;
            _lockedPosition = Vector3.Lerp(startPos, finalTargetPos, time / pushDuration);
            transform.position = _lockedPosition;
            yield return null;
        }

        _lockedPosition = finalTargetPos;
        yield return new WaitForSeconds(stunDuration);

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(finalTargetPos);
        }
        IsKnockingBack = false;
    }

    private void RevertEffectModifiers(StatusEffectData effect)
    {
        IsAttackDisabled = false;
        IsMovementDisabled = false;
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = false;
    }
}