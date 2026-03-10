using UnityEngine;
using System.Collections;

public class TrapStructure : MonoBehaviour
{
    public StructureData data; // 인스펙터에서 구조물 데이터 할당

    private float _currentDurability;
    private bool _isRepairing = false;

    private void Awake()
    {
        _currentDurability = data.hpOrDurability;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            if (data.type == StructureType.Pad)
            {
                TriggerPad(collision.gameObject);
            }
            else if (data.type == StructureType.Mine)
            {
                TriggerMine();
            }
        }
    }

    // --- 발판(Pad) 로직 ---
    private void TriggerPad(GameObject enemyObj)
    {
        if (_currentDurability <= 0) return;

        // 명중률 체크
        if (Random.Range(0f, 100f) > data.accuracy) return;

        EnemyAI enemy = enemyObj.GetComponent<EnemyAI>();
        if (enemy != null && enemy.CurrentHp > 0)
        {
            if (data.damage > 0) enemy.TakeDamage(data.damage, transform.position);
            ApplyEffect(enemyObj);

            _currentDurability -= 10f; // 1회 작동 시 차감되는 내구도 (기획에 맞게 조절 가능)
            Debug.Log($"{data.structureName} 작동! 남은 내구도: {_currentDurability}");
        }
    }

    // --- 지뢰(Mine) 로직 ---
    private void TriggerMine()
    {
        Debug.Log($"{data.structureName} 폭발!");

        // 사거리(range)를 폭발 반경으로 사용하여 주변 적 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.range);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null && enemy.CurrentHp > 0)
                {
                    if (data.damage > 0) enemy.TakeDamage(data.damage, transform.position);
                    ApplyEffect(hit.gameObject);
                }
            }
        }

        // 1회성이므로 폭발 후 파괴
        Destroy(gameObject);
    }

    private void ApplyEffect(GameObject target)
    {
        if (data.applyEffect != null)
        {
            IStatusEffectable effectable = target.GetComponent<IStatusEffectable>();
            effectable?.ApplyStatusEffect(data.applyEffect);
        }
    }

    // --- 수리 로직 (발판 전용) ---
    public void StartRepair(UnitStat repairingUnit)
    {
        if (data.type != StructureType.Pad || _isRepairing || _currentDurability >= data.hpOrDurability) return;
        StartCoroutine(RepairRoutine(repairingUnit.RepairSpeed));
    }

    private IEnumerator RepairRoutine(float unitRepairSpeed)
    {
        _isRepairing = true;
        float repairTime = 5f / Mathf.Max(unitRepairSpeed, 0.1f);
        yield return new WaitForSeconds(repairTime);

        float healAmount = data.hpOrDurability * 0.5f; // 최대치의 50% 회복
        _currentDurability = Mathf.Min(_currentDurability + healAmount, data.hpOrDurability);

        Debug.Log($"수리 완료! 현재 내구도: {_currentDurability}");
        _isRepairing = false;
    }
}