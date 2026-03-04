using UnityEngine;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// 유닛의 전투 AI 및 넉백을 담당하는 스크립트
/// </summary>
public class UnitCombat : MonoBehaviour
{
    private UnitStat _myStat;
    private NavMeshAgent _agent;
    private GameObject _currentTarget;
    private float _lastAttackTime;

    [Header("Combat Settings")]
    [SerializeField] private LayerMask enemyLayer;      // 적 유닛의 레이어
    [SerializeField] private float knockbackForce = 5f; // 넉백 위력
    [SerializeField] private float scanInterval = 0.2f; // 타겟 탐색 주기 (성능 최적화)

    private void Awake()
    {
        _myStat = GetComponent<UnitStat>();
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // 1. 사망 상태면 정지
        if (_myStat.CurrentHp <= 0) return;

        // 2. 타겟 유효성 검사 및 탐색
        UpdateTarget();

        // 3. 공격 로직
        if (_currentTarget != null)
        {
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            // [수정] 사거리 안에 들어왔을 때만 처리
            if (dist <= _myStat.AttackRange)
            {
                // 이동 경로가 남아있다면, 사거리 안이므로 정지
                // 하지만 사용자가 강제로 이동 명령을 내린 경우(속도가 빠를 때)는 공격보다 이동 우선
                if (_agent.velocity.sqrMagnitude < 0.2f)
                {
                    TryAttack();
                }
            }
        }
    }

    /// <summary>
    /// 타겟이 죽었는지 확인하거나, 사거리(AttackRange) 내 가장 가까운 적을 찾습니다.
    /// </summary>
    private void UpdateTarget()
    {
        // 기존 타겟 검사
        if (_currentTarget != null)
        {
            UnitStat targetStat = _currentTarget.GetComponent<UnitStat>();
            // 타겟이 죽었거나 컴포넌트가 없다면 타겟 초기화
            if (targetStat == null || targetStat.CurrentHp <= 0)
            {
                Debug.Log($"[{gameObject.name}] 타겟 처치 완료.");
                _currentTarget = null;
                return;
            }
            else if (_agent.destination != _currentTarget.transform.position)
            {
                // 타겟이 사거리 밖으로 도망갔다면 다시 추격
                 //_agent.SetDestination(_currentTarget.transform.position);
            }
        }

        // 타겟이 없을 때만 자동 탐색
        if (_currentTarget == null)
        {
            // 자신의 AttackRange를 반지름으로 주변 적 탐색
            Collider2D hit = Physics2D.OverlapCircle(transform.position, _myStat.AttackRange, enemyLayer);
            if (hit != null)
            {
                _currentTarget = hit.gameObject;
                Debug.Log($"[{gameObject.name}] 새 타겟 발견: {_currentTarget.name}");
            }
        }
    }

    private void TryAttack()
    {
        if (Time.time >= _lastAttackTime + (1f / _myStat.AttackSpeed))
        {
            PerformAttack();
            _lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// 공격 수행 및 넉백 적용
    /// </summary>
    private void PerformAttack()
    {
        if (_currentTarget == null) return;

        // 원거리 명중률 체크
        if (_myStat.currentWeapon != null && _myStat.currentWeapon.type == EquipmentType.Bow)
        {
            if (Random.Range(0f, 100f) > _myStat.RangeAccuracy) return;
        }

        UnitStat targetStat = _currentTarget.GetComponent<UnitStat>();
        if (targetStat != null)
        {
            targetStat.TakeDamage(_myStat.AttackPower);

            // [넉백 로직] 적에게 NavMesh가 없어도 Rigidbody2D만 있으면 작동함
            Rigidbody2D targetRb = _currentTarget.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                Vector2 dir = (_currentTarget.transform.position - transform.position).normalized;
                targetRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    public void SetManualTarget(GameObject target)
    {
        _currentTarget = target;
    }
}