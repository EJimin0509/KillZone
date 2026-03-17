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
    private PlayerMovement _movement; // 추가: 이동 스크립트 참조

    void Awake()
    {
        _myStat = GetComponent<UnitStat>();
        _agent = GetComponent<NavMeshAgent>();
        _movement = GetComponent<PlayerMovement>();
    }

    public void StartHealCommand(UnitStat target)
    {
        if (target == null || target == _myStat) return;

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

        // [수정] 도착 판정 로직 보강: 경로 계산 대기 및 거리 체크
        while (_agent.pathPending || _agent.remainingDistance > 0.6f)
        {
            yield return null;
        }

        // 2. 본진 진입 (스프라이트와 UI 끄기)
        currentState = MedicState.InsideBase;
        ToggleVisuals(false);
        yield return new WaitForSeconds(0.8f);

        // [중요] 다시 나타날 때 본진 위치에서 재시작하도록 보장
        ToggleVisuals(true);

        // 3. 다시 치료 대상에게 이동
        currentState = MedicState.MovingToTarget;

        // [수정] 대상 추적 로직: 대상에게 도착할 때까지 반복
        while (_targetUnit != null)
        {
            if (_targetUnit.CurrentHp >= _targetUnit.MaxHp || _targetUnit.CurrentHp <= 0) break;

            _agent.SetDestination(_targetUnit.transform.position);

            // 대상과 충분히 가까워졌는지 체크
            float dist = Vector2.Distance(transform.position, _targetUnit.transform.position);
            if (dist <= 1.3f) // 치료 사거리 안이면 루프 탈출
            {
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }

        // 4. 실제로 치료 수행 (이 부분이 실행되어야 힐이 들어감)
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

        // 대상이 존재하고, 살아있으며, 체력이 최대치보다 낮을 때 계속 반복
        while (_targetUnit != null && _targetUnit.CurrentHp < _targetUnit.MaxHp)
        {
            // 타겟이 죽으면 즉시 중단
            if (_targetUnit.CurrentHp <= 0) break;

            // 너무 멀어지면 (자비롭게 2.5m까지 허용) 추적 모드로 돌아가거나 중단
            float dist = Vector2.Distance(transform.position, _targetUnit.transform.position);
            if (dist > 2.5f)
            {
                Debug.Log("대상이 너무 멀어져서 치료를 일시 중단하고 다시 추격합니다.");
                yield return StartCoroutine(HealSequence()); // 다시 추격 시퀀스로
                yield break;
            }

            float medicLevel = _myStat.data.medic;
            float healValue = 0.1f + (medicLevel * 0.09f);

            // RecoverHP 내부에서 MaxHP를 넘지 않도록 처리되어 있어야 함
            _targetUnit.RecoverHP(healValue * Time.deltaTime);

            yield return null; // 매 프레임 체크
        }

        Debug.Log("[치료 완료] 대상의 체력이 가득 찼습니다.");
        StopHeal();
    }

    public void StopHeal()
    {
        StopAllCoroutines();
        currentState = MedicState.Idle;
        _targetUnit = null;

        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = false;
        }
    }

    private void ToggleVisuals(bool show)
    {
        // [수정] 스프라이트 렌더러가 자식에 있을 수도 있으므로 GetComponentInChildren 사용
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = show;

        // [중요] "UnitCanvas" 이름을 정확히 찾기 위해 GetComponentsInChildren 또는 하위 탐색
        Canvas unitCanvas = GetComponentInChildren<Canvas>();
        if (unitCanvas != null)
        {
            unitCanvas.enabled = show; // setActive 대신 enabled를 끄는 것이 안전할 때가 있음
        }
        else
        {
            // 만약 오브젝트 이름으로 찾고 싶다면:
            Transform canvasTrans = transform.Find("UnitCanvas");
            if (canvasTrans != null) canvasTrans.gameObject.SetActive(show);
        }
    }
}