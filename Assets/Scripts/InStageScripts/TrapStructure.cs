using UnityEngine;
using System.Collections;

public class TrapStructure : MonoBehaviour
{
    [Header("Trap Settings")]
    public float directDamage = 0f;
    public StatusEffectData applyEffect;

    [Header("AoE Settings (광역 효과)")]
    [Tooltip("체크하면 밟았을 때 주변 반경 내의 모든 적에게 효과를 줍니다.")]
    public bool isAoE = false;
    public float aoeRadius = 3f;
    public LayerMask enemyLayer;

    [Header("Trigger Settings")]
    [Tooltip("체크하면 한 번 밟았을 때 파괴됩니다. 쿨타임을 쓰려면 체크를 해제하세요.")]
    public bool destroyOnTrigger = false;
    [Tooltip("함정 재발동 대기시간 (초 단위)")]
    public float cooldownTime = 1f;

    private bool _isOnCooldown = false;

    // 적이 함정 영역 안으로 들어오는 순간 감지
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryTriggerTrap(collision);
    }

    // 적이 함정 영역 안에 계속 머물러 있을 때 감지 (쿨타임마다 작동하게 만듦)
    private void OnTriggerStay2D(Collider2D collision)
    {
        TryTriggerTrap(collision);
    }

    private void TryTriggerTrap(Collider2D collision)
    {
        if (_isOnCooldown) return;

        EnemyAI enemy = collision.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            if (isAoE)
            {
                ApplyAoEEffect();
            }
            else
            {
                ApplySingleEffect(enemy);
            }

            if (destroyOnTrigger)
            {
                Destroy(gameObject);
            }
            else
            {
                StartCoroutine(CooldownRoutine());
            }
        }
    }

    private IEnumerator CooldownRoutine()
    {
        _isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        _isOnCooldown = false;
    }

    private void ApplySingleEffect(EnemyAI target)
    {
        if (target == null || target.CurrentHp <= 0) return;

        if (directDamage > 0)
        {
            target.TakeDamage(directDamage, transform.position);
        }

        if (applyEffect != null)
        {
            StatusEffectHandler handler = target.GetComponent<StatusEffectHandler>();
            if (handler != null)
            {
                handler.ApplyStatusEffect(applyEffect);
                Debug.Log($"[함정] {target.name}에게 {applyEffect.effectName} 적용!");
            }
        }
    }

    private void ApplyAoEEffect()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            EnemyAI target = hit.GetComponent<EnemyAI>();
            if (target != null)
            {
                ApplySingleEffect(target);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (isAoE)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, aoeRadius);
        }
    }
}