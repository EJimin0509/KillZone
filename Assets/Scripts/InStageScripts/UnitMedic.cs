using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class UnitMedic : MonoBehaviour
{
    public enum MedicState { Idle, MovingToBase, InsideBase, MovingToTarget, Healing }
    public MedicState currentState = MedicState.Idle;

    private UnitStat _myStat;
    private NavMeshAgent _agent;
    private UnitStat _targetUnit;

    void Awake()
    {
        _myStat = GetComponent<UnitStat>();
        _agent = GetComponent<NavMeshAgent>();
    }

    public void StartHealCommand(UnitStat target)
    {
        // [추가] 체력이 이미 100%면 치료 거부
        if (target == null || target == _myStat) return;
        if (target.CurrentHp >= target.MaxHp)
        {
            Debug.Log($"{target.name}은 이미 체력이 가득 찼습니다.");
            return;
        }

        _targetUnit = target;
        StopAllCoroutines();
        StartCoroutine(HealSequence());
    }

    private IEnumerator HealSequence()
    {
        // 1. 본진으로 이동
        currentState = MedicState.MovingToBase;
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj == null) { currentState = MedicState.Idle; yield break; }

        _agent.isStopped = false;
        _agent.SetDestination(baseObj.transform.position);

        while (_agent.pathPending || _agent.remainingDistance > 0.6f)
        {
            yield return null;
        }

        // 2. 본진 진입 (가시성 및 충돌 제거)
        currentState = MedicState.InsideBase;
        ToggleVisuals(false);
        yield return new WaitForSeconds(0.8f);
        ToggleVisuals(true);

        // 3. 다시 치료 대상에게 이동
        currentState = MedicState.MovingToTarget;

        while (_targetUnit != null)
        {
            // 대상이 풀피가 되거나 죽으면 중단
            if (_targetUnit.CurrentHp >= _targetUnit.MaxHp || _targetUnit.CurrentHp <= 0) break;

            _agent.SetDestination(_targetUnit.transform.position);

            float dist = Vector2.Distance(transform.position, _targetUnit.transform.position);
            // [조정] 콜라이더 크기를 고려하여 사거리를 조금 더 여유있게(1.5f) 설정
            if (dist <= 1.5f) break;

            yield return new WaitForSeconds(0.1f);
        }

        // 4. 치료 수행
        if (_targetUnit != null && _targetUnit.CurrentHp > 0 && _targetUnit.CurrentHp < _targetUnit.MaxHp)
        {
            yield return StartCoroutine(PerformHeal());
        }

        StopHeal();
    }

    private IEnumerator PerformHeal()
    {
        currentState = MedicState.Healing;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        Debug.Log($"[치료 시작] 대상: {_targetUnit.name}");

        while (_targetUnit != null && _targetUnit.CurrentHp < _targetUnit.MaxHp)
        {
            if (_targetUnit.CurrentHp <= 0) break;

            float dist = Vector2.Distance(transform.position, _targetUnit.transform.position);
            if (dist > 2.5f) // 타겟이 도망가면 다시 추격
            {
                StartCoroutine(HealSequence());
                yield break;
            }

            // [수정] 힐량 대폭 상향: 초당 약 5~15 정도 회복되도록 설정
            // UnitStat의 MaxHp가 80~130이므로 이 정도는 되어야 눈에 보입니다.
            float medicLevel = _myStat.data.medic;
            float healValue = (5f + (medicLevel * 2f)) * Time.deltaTime;

            _targetUnit.RecoverHP(healValue);

            yield return null;
        }

        Debug.Log("[치료 완료]");
        StopHeal();
    }

    public void StopHeal()
    {
        StopAllCoroutines();
        currentState = MedicState.Idle;
        _targetUnit = null;

        if (_agent.isActiveAndEnabled)
            _agent.isStopped = false;
    }

    private void ToggleVisuals(bool show)
    {
        // 스프라이트 끄기
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = show;

        // UI 끄기
        Canvas unitCanvas = GetComponentInChildren<Canvas>();
        if (unitCanvas != null) unitCanvas.enabled = show;

        // [중요] 본진 들어갔을 때 물리 충돌 제거 (유닛끼리 끼이는 현상 방지)
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = show;
    }
}