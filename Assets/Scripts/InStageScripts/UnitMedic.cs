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
    private Coroutine _healingRoutine;

    void Awake()
    {
        _myStat = GetComponent<UnitStat>();
        _agent = GetComponent<NavMeshAgent>();
    }

    // 치료 명령 시작 (UI에서 호출)
    public void StartHealCommand(UnitStat target)
    {
        if (target == null || target == _myStat) return;

        _targetUnit = target;
        StopAllCoroutines();
        StartCoroutine(HealSequence());
    }

    private IEnumerator HealSequence()
    {
        // 5. 현재 위치에서 Base까지 이동
        currentState = MedicState.MovingToBase;
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj == null) yield break;

        _agent.SetDestination(baseObj.transform.position);
        yield return new WaitUntil(() => _agent.remainingDistance < 0.5f);

        // 위치에 도달하면 0.8초간 사라짐 (Base 진입 연출)
        currentState = MedicState.InsideBase;
        ToggleVisuals(false); // 유닛 이미지와 UI 숨김
        yield return new WaitForSeconds(0.8f);
        ToggleVisuals(true);  // 다시 나타남

        // 다시 나온 후, 치료 대상까지 이동
        currentState = MedicState.MovingToTarget;
        while (_targetUnit != null)
        {
            _agent.SetDestination(_targetUnit.transform.position);

            // 대상 근처에 도달하면 치료 시작
            if (Vector2.Distance(transform.position, _targetUnit.transform.position) < 1.5f)
            {
                break;
            }
            yield return new WaitForSeconds(0.2f);
        }

        // 치료 시작
        if (_targetUnit != null)
        {
            StartCoroutine(PerformHeal());
        }
    }

    private IEnumerator PerformHeal()
    {
        currentState = MedicState.Healing;
        _agent.enabled = false; // 치료 중 장애물화 준비

        while (_targetUnit != null)
        {
            if (_targetUnit.CurrentHp >= _targetUnit.MaxHp) break;

            // unitData.medic이 현재 레벨 값이라고 가정할 때의 계산입니다.
            float medicLevel = _myStat.data.medic;
            float healValue = 0.1f + (medicLevel * 0.09f);

            // 초당 회복량으로 적용
            _targetUnit.RecoverHP(healValue * Time.deltaTime);

            yield return null;
        }
        StopHeal();
    }

    public void StopHeal()
    {
        StopAllCoroutines();
        if (GetComponent<NavMeshObstacle>()) Destroy(GetComponent<NavMeshObstacle>());
        _agent.enabled = true;
        currentState = MedicState.Idle;
        _targetUnit = null;
    }

    private void ToggleVisuals(bool show)
    {
        // SpriteRenderer와 자식 UI(Canvas)를 켜고 끔
        GetComponent<SpriteRenderer>().enabled = show;
        transform.Find("UnitCanvas")?.gameObject.SetActive(show);
    }
}