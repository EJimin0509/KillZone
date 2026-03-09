using UnityEngine;
using System.Collections;
using UnityEngine.AI; // NavMesh API 사용을 위해 추가

/// <summary>
/// A* 알고리즘(NavMesh 데이터 활용)을 통해 경로 찾기 및 전투를 담당하는 적 AI
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData data; // 적 기본 데이터(ScriptableObject)
    private SpriteRenderer _spriteRenderer;

    [Header("Visual Knockback")]
    [SerializeField] private Transform visualChild; // 자식 오브젝트인 Visual을 드래그 앤 드롭
    [SerializeField] private float knockbackDistance = 0.3f; // 밀려나는 거리
    [SerializeField] private float knockbackDuration = 0.2f; // 복귀까지 걸리는 시간

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
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // 자식 스프라이트 렌더러 참조

        // 데이터가 존재할 경우 초기 체력 설정
        if (data != null) CurrentHp = data.maxHp;

        // 유니티 에디터에서 설정한 "Unit" 레이어를 가져옴
        _unitLayer = LayerMask.GetMask("Unit");

        // 모든 적이 동시에 길찾기를 연산하지 않도록 첫 업데이트 시간을 랜덤하게 분산
        _nextUpdateTime = Time.time + Random.Range(0f, _pathUpdateInterval);
    }

    // 오브젝트 풀 초기화
    private void OnEnable()
    {
        // 체력 및 상태 초기화
        if (data != null) CurrentHp = data.maxHp;
        _isKnockbacking = false;
        _currentTarget = null;
        _pathIndex = 0;
        _pathCorners = null;
        _nextUpdateTime = Time.time;

        // NavMeshAgent 재활성화 및 위치 보정
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false; // 위치를 옮기기 위해 잠시 끔
            StartCoroutine(ResetAgentRoutine(agent));
        }
    }

    private IEnumerator ResetAgentRoutine(NavMeshAgent agent)
    {
        yield return null; // 한 프레임 대기 (위치가 셋팅된 후 활성화하기 위함)
        agent.enabled = true;

        // 에이전트가 베이크된 바닥 위로 강제 스냅
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
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
            // 타겟이 파괴되었거나 죽었는지 한 번 더 체크
            if (!IsTargetAlive(_currentTarget))
            {
                _currentTarget = null;
                return;
            }

            // 타겟과의 거리 계산
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            if (dist <= data.attackRange)
            {
                // 공격 사거리 내에 있다면 멈춰서 공격 방향을 바라보고 공격
                HandleSpriteFlip(_currentTarget.transform.position);
                TryAttack();
            }
            else
            {
                // 사거리 밖이라면 타겟을 향해 이동
                HandleAStarMovement();
            }
        }
    }

    private void HandleAStarMovement()
    {
        // 타겟 위치가 변하므로 주기적으로 경로 갱신
        if (Time.time >= _nextUpdateTime)
        {
            UpdatePath();
            _nextUpdateTime = Time.time + _pathUpdateInterval;
        }

        if (_pathCorners != null && _pathIndex < _pathCorners.Length)
        {
            Vector3 targetPos = _pathCorners[_pathIndex];

            // 직접 좌표 이동으로 장애물 끼임 물리 연산 우회
            transform.position = Vector3.MoveTowards(transform.position, targetPos, data.moveSpeed * Time.deltaTime);

            HandleSpriteFlip(targetPos);

            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                _pathIndex++;
            }
        }
    }

    private void UpdatePath()
    {
        if (_currentTarget == null) return;

        NavMeshPath path = new NavMeshPath();
        // NavMesh 데이터로부터 A* 경로만 계산해서 가져옴
        if (NavMesh.CalculatePath(transform.position, _currentTarget.transform.position, NavMesh.AllAreas, path))
        {
            _pathCorners = path.corners;
            _pathIndex = 1;
        }
    }

    /// <summary>
    /// 대상의 X축 위치에 따라 스프라이트 좌우 반전
    /// </summary>
    private void HandleSpriteFlip(Vector3 targetPos)
    {
        float diff = targetPos.x - transform.position.x;
        if (Mathf.Abs(diff) < 0.01f) return;
        _spriteRenderer.flipX = diff < 0;
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
        HandleSpriteFlip(attackerPos);

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
        if (visualChild == null) yield break; // 자식 오브젝트 없으면 리턴

        _isKnockbacking = true;

        // 방향 계산 (공격자로부터 반대 방향)
        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
        Vector3 startPos = Vector3.zero; // 로컬 위치이므로 0
        Vector3 targetPos = new Vector3(dir.x, dir.y, 0) * knockbackDistance;

        float elapsed = 0f; // 타이머 초기화

        // 뒤로 밀리기
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (knockbackDuration * 0.5f);
            visualChild.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        elapsed = 0f; // 타이머 초기화

        // 제자리로 복귀
        while (elapsed < knockbackDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (knockbackDuration * 0.5f);
            visualChild.localPosition = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }

        visualChild.localPosition = startPos; // 위치 초기화
        _isKnockbacking = false;
    }

    private void Die()
    {
        // 죽는 로직

        // 오브젝트 풀 반납
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void HandleTargeting()
    {
        bool isTargetingUnit = _currentTarget != null && _currentTarget.CompareTag("Unit");

        if (!isTargetingUnit)
        {
            // _unitLayer를 우선적으로 타겟팅
            // _unitLayer에는 아군 유닛 뿐만 아니라 체력이 존재하는 구조물도 포함
            Collider2D hit = Physics2D.OverlapCircle(transform.position, data.attackRange * 2f, _unitLayer);
            
            if (hit != null)
            {
                // 유닛을 발견하면 즉시 타겟 교체 및 경로 갱신
                _currentTarget = hit.gameObject;
                UpdatePath();
                return; // 유닛을 잡았으므로 아래 Base 설정 로직 건너뜀
            }
        }

        // 유닛을 타겟팅 중이 아니거나, 기존 타겟이 죽었다면 Base로 설정
        if (!IsTargetAlive(_currentTarget))
        {
            if (DefenseBase.Current != null)
            {
                _currentTarget = DefenseBase.Current.gameObject;
                UpdatePath();
            }
            else
            {
                _currentTarget = null;
            }
        }
    }

    private bool IsTargetAlive(GameObject target)
    {
        // 1. 오브젝트 자체가 null이거나 파괴되었는지 체크
        if (target == null) return false;

        // 2. 유닛인 경우 체력 체크
        var unit = target.GetComponent<UnitStat>();
        if (unit != null) return unit.CurrentHp > 0;

        // 3. 베이스인 경우 체력 체크
        var b = target.GetComponent<DefenseBase>();
        if (b != null) return b.currentHp > 0;

        return false;
    }

    /// <summary>
    /// 유닛이나 Base일 경우 공격하는 메서드
    /// </summary>
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