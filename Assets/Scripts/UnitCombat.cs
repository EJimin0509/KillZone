using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 아군 유닛의 전투 스크립트
/// 범위 내 적을 탐색하고 적을 자동 공격한다.
/// 적이 범위를 벗어나면 추적한다.
/// 적이 사망했거나, 다른 명령이 강제되면 전투를 종료한다.
/// </summary>
public class UnitCombat : MonoBehaviour
{
    private UnitStat _myStat; // 유닛의 Stat 데이터 참조
    private NavMeshAgent _agent; // 유닛이 NaveMesh 사용 중이므로 NaveMesh 참조
    private GameObject _currentTarget; // 현재 공격중인 타겟을 담을 변수
    private float _lastAttackTime; // 공격 속도
    private bool _isForceMoving = false; // 현재 유닛이 강제 이동 명령을 받았는지 확인
    private float _scanTimer; // 스캔 탐색용 타이머

    [Header("Combat Settings")]
    [SerializeField] private LayerMask enemyLayer;      // 적 유닛의 레이어
    [SerializeField] private float scanInterval = 0.2f; // 타겟 탐색 주기
    
    private void Awake()
    {
        _myStat = GetComponent<UnitStat>(); // UnitStat 컴포넌트 참조
        _agent = GetComponent<NavMeshAgent>(); // NavMeshAgent 컴포넌트 참조
    }

    private void Update()
    {
        // 사망 상태라면 리턴
        if (_myStat.CurrentHp <= 0) return;

        // 1. 스캔 타이머 가동
        _scanTimer -= Time.deltaTime;

        // 2. 타겟 상태 업데이트 및 추적 로직
        HandleCombatAI();
    }

    /// <summary>
    /// 1. 강제 이동 중이거나 타겟이 없으면 적을 자동으로 탐색한다.
    /// 2. 타겟의 생존 여부를 확인하고, 전투 상태 여부를 결정한다.
    /// 3. 타겟을 공격하고, 범위를 벗어나면 추적한다.
    /// </summary>
    private void HandleCombatAI()
    {
        // 강제 이동 중이거나 타겟이 없으면 자동 탐색
        if (_currentTarget == null)
        {
            if (_scanTimer <= 0)              // 스캔 가능 할 때
            {
                SearchTarget();               // 적을 탐색하고
                _scanTimer = scanInterval;    // 스캔 타이머를 초기화
            }
            return;
        }

        // 타겟의 생존 여부 확인
        UnitStat targetStat = _currentTarget.GetComponent<UnitStat>();
        if (targetStat == null || targetStat.CurrentHp <= 0) // 타겟이 없거나 사망
        {
            ResetCombat(); // 전투 종료 후 상태 초기화
            return;        // 리턴
        }

        // 타겟과 유닛 사이의 거리 Vector2
        float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

        // 3. 사거리 및 추적 로직
        if (dist <= _myStat.AttackRange)
        {
            // 사거리 안이면 정지 후 공격
            if (!_isForceMoving)
            {
                _agent.isStopped = true; // 정지
                TryAttack(targetStat);   // 공격
            }
        }
        else
        {
            // 사거리 밖이고 강제 이동 중이 아니라면 추격
            if (!_isForceMoving)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_currentTarget.transform.position); // 새로운 경로 설정
            }
        }
    }

    /// <summary>
    /// 전투가 종료되면 상태를 초기화하는 메서드
    /// </summary>
    public void ResetCombat()
    {
        _currentTarget = null; // 타겟 NULL
        _isForceMoving = false; // 정지
    }

    /// <summary>
    /// Circle Collider 만큼의 범위 내 적을 탐색하는 메서드
    /// </summary>
    private void SearchTarget()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, _myStat.AttackRange, enemyLayer);
        if (hit != null) _currentTarget = hit.gameObject;
    }

    /// <summary>
    /// 1. 공격 속도에 맞춰 적을 공격한다.
    /// </summary>
    /// <param name="target">공격 대상</param>
    private void TryAttack(UnitStat target)
    {
        if (Time.time >= _lastAttackTime + (1f / _myStat.AttackSpeed)) // 공격 속도 체크
        {
            // 원거리 명중률 체크
            if (_myStat.currentWeapon != null && _myStat.currentWeapon.type == EquipmentType.Bow)
            {
                // 임시 명중률 로직. 추후 불렛 탄착 로직으로 변경
                if (Random.Range(0f, 100f) > _myStat.RangeAccuracy) return;
            }

            target.TakeDamage(_myStat.AttackPower, transform.position); // 내 위치 정보를 넘겨 넉백 방향 계산
            _lastAttackTime = Time.time; // 초기화
        }
    }

    /// <summary>
    /// PlayerMovement에서 명령을 내릴 때 호출하여 이동 정의
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="isMoveCommand">다른 이동 명령이 부여되었는지</param>
    public void SetManualCommand(GameObject target, bool isMoveCommand)
    {
        _currentTarget = target;
        _isForceMoving = isMoveCommand;
        _agent.isStopped = false;
    }
}