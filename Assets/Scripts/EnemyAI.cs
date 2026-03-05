using UnityEngine;
using System.Collections;
using UnityEngine.AI; // NavMesh API 사용을 위해 추가

/// <summary>
/// 다익스트라 알고리즘(NavMesh 데이터 활용)을 통해 경로 찾기 및 전투를 담당하는 적 AI
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData data; // 적 기본 데이터(ScriptableObject)
    private SpriteRenderer _spriteRenderer;

    public float CurrentHp; // 현재 체력 (아군 UnitCombat에서 참조함)
    private float _lastAttackTime; // 마지막 공격 시점
    private bool _isKnockbacking = false; // 현재 넉백 중인지 여부

    private GameObject _currentTarget; // 현재 공격 대상 (아군 유닛 또는 베이스)
    private Vector3[] _pathCorners; // NavMesh로 계산된 경로의 점들
    private int _pathIndex; // 현재 이동 중인 경로의 인덱스

    // 경로 갱신 최적화: 모든 적이 서로 다른 타이밍에 갱신하도록 설정 (부하 분산)
    private float _pathUpdateInterval = 1.0f;
    private float _nextUpdateTime;

    private LayerMask _unitLayer; // 아군 유닛 레이어

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 데이터가 존재할 경우 초기 체력 설정
        if (data != null) CurrentHp = data.maxHp;

        // 유니티 에디터에서 설정한 "Unit" 레이어를 가져옴
        _unitLayer = LayerMask.GetMask("Unit");

        // 모든 적이 동시에 길찾기를 연산하지 않도록 첫 업데이트 시간을 랜덤하게 분산
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    private void Update()
    {
        // 사망 상태이거나 넉백 중일 때는 모든 행동(이동/공격)을 중지
        if (CurrentHp <= 0 || _isKnockbacking) return;

        // 1. 타겟팅 처리 (주변 아군 탐색 및 유효성 검사)
        HandleTargeting();

        // 2. 행동 결정
        if (_currentTarget != null)
        {
            // 타겟과의 거리 계산
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            if (dist <= data.attackRange)
            {
                // 공격 사거리 내에 있다면 멈춰서 공격 방향을 바라보고 공격
                LookAtTarget(_currentTarget.transform.position);
                TryAttack();
            }
            else
            {
                // 사거리 밖이라면 타겟을 향해 이동 (근접 시에는 단순 직선 이동으로 추적)
                MoveStraight(_currentTarget.transform.position);
            }
        }
        else
        {
            // 공격 대상(아군)이 없을 경우 디팬딩 구조물(Base)을 향해 지형을 회피하며 이동
            MoveToLongDistanceTarget();
        }
    }

    /// <summary>
    /// NavMesh 데이터를 활용하여 지형(이동 불가 구역)을 회피하며 목적지로 이동
    /// </summary>
    private void MoveToLongDistanceTarget()
    {
        if (DefenseBase.Current == null) return;

        // 성능 최적화: 정해진 주기마다만 길찾기 경로를 재계산
        if (Time.time >= _nextUpdateTime)
        {
            NavMeshPath path = new NavMeshPath();
            // 목적지까지의 경로를 계산하여 corners(꺾임점들) 배열에 저장
            // [주의] NavMeshAgent 없이 연산만 수행하므로 부하가 적음
            if (NavMesh.CalculatePath(transform.position, DefenseBase.Current.transform.position, NavMesh.AllAreas, path))
            {
                _pathCorners = path.corners;
                _pathIndex = 1; // 0번은 현재 위치이므로 1번부터 시작
            }
            _nextUpdateTime = Time.time + _pathUpdateInterval;
        }

        // 계산된 경로의 점들을 하나씩 따라감
        if (_pathCorners != null && _pathIndex < _pathCorners.Length)
        {
            Vector3 targetWayPoint = _pathCorners[_pathIndex];
            MoveStraight(targetWayPoint);

            // 해당 경유점에 충분히 가까워지면 다음 경유점으로 타겟 변경
            if (Vector2.Distance(transform.position, targetWayPoint) < 0.2f)
            {
                _pathIndex++;
            }
        }
    }

    /// <summary>
    /// 특정 좌표를 향해 직선으로 이동하며 스프라이트 방향을 갱신
    /// </summary>
    private void MoveStraight(Vector3 targetPos)
    {
        LookAtTarget(targetPos);
        Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        transform.Translate(dir * data.moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 대상의 X축 위치에 따라 스프라이트 좌우 반전
    /// </summary>
    private void LookAtTarget(Vector2 targetPos)
    {
        if (_spriteRenderer == null) return;
        _spriteRenderer.flipX = targetPos.x < transform.position.x;
    }

    /// <summary>
    /// 외부(아군)로부터 데미지를 입었을 때 호출되는 함수
    /// </summary>
    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        float finalDamage = Mathf.Max(damage - data.defense, 1f);
        CurrentHp -= finalDamage;
        Debug.Log($"적 체력: {CurrentHp}");

        // 피격 시 즉시 공격자를 돌아봄
        LookAtTarget(attackerPos);

        if (gameObject.activeSelf && !_isKnockbacking)
        {
            StartCoroutine(KnockbackRoutine(attackerPos));
        }

        if (CurrentHp <= 0) Die();
    }

    /// <summary>
    /// 피격 시 뒤로 밀려나는 효과를 주는 코루틴
    /// </summary>
    private IEnumerator KnockbackRoutine(Vector2 attackerPos)
    {
        _isKnockbacking = true;
        Vector2 knockbackDir = ((Vector2)transform.position - attackerPos).normalized;

        float elapsed = 0f;
        float duration = 0.15f;
        float force = 1.5f;

        while (elapsed < duration)
        {
            transform.Translate(knockbackDir * force * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _isKnockbacking = false;
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void HandleTargeting()
    {
        if (_currentTarget == null || !IsTargetAlive(_currentTarget))
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, data.attackRange * 2f, _unitLayer);
            if (hit != null) _currentTarget = hit.gameObject;
            else _currentTarget = null;
        }
    }

    private bool IsTargetAlive(GameObject target)
    {
        var unit = target.GetComponent<UnitStat>();
        if (unit != null) return unit.CurrentHp > 0;

        var b = target.GetComponent<DefenseBase>();
        if (b != null) return b.currentHp > 0;

        return false;
    }

    private void TryAttack()
    {
        if (Time.time >= _lastAttackTime + (1f / data.attackSpeed))
        {
            if (data.attackType == EnemyType.Range && Random.Range(0f, 100f) > data.accuracy)
            {
                _lastAttackTime = Time.time;
                return;
            }

            if (_currentTarget.CompareTag("Unit"))
            {
                _currentTarget.GetComponent<UnitStat>().TakeDamage(data.attackPower, transform.position);
            }
            else if (_currentTarget.CompareTag("Base"))
            {
                _currentTarget.GetComponent<DefenseBase>().TakeDamage(data.attackPower);
            }

            _lastAttackTime = Time.time;
        }
    }
}