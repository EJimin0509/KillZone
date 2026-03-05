using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData data; // ScriptableObject 데이터

    // 컴포넌트 캐싱
    private SpriteRenderer _spriteRenderer;

    // 실시간 변동 스탯
    private float _currentHp;
    private float _lastAttackTime;
    private bool _isKnockbacking = false;

    private GameObject _currentTarget;
    private LayerMask _unitLayer;

    private void Awake()
    {
        // 컴포넌트 미리 가져오기 (성능 최적화)
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (data != null) _currentHp = data.maxHp;
        _unitLayer = LayerMask.GetMask("Unit");
    }

    private void Update()
    {
        if (_currentHp <= 0 || _isKnockbacking) return;

        HandleTargeting();

        if (_currentTarget != null)
        {
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            if (dist <= data.attackRange)
            {
                // 공격 중에도 타겟 방향을 바라보게 설정
                LookAtTarget(_currentTarget.transform.position);
                TryAttack();
            }
            else
            {
                MoveTowards(_currentTarget.transform.position);
            }
        }
        else
        {
            MoveTowardsBase();
        }
    }

    /// <summary>
    /// 대상 위치를 기반으로 스프라이트 좌우 반전 처리
    /// </summary>
    private void LookAtTarget(Vector2 targetPos)
    {
        if (_spriteRenderer == null) return;

        // 타겟이 나보다 왼쪽에 있으면 flipX = true (왼쪽 보기)
        // 타겟이 나보다 오른쪽에 있으면 flipX = false (오른쪽 보기)
        if (targetPos.x < transform.position.x)
        {
            _spriteRenderer.flipX = true;
        }
        else
        {
            _spriteRenderer.flipX = false;
        }
    }

    private void MoveTowards(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;

        // 이동 방향에 맞춰 스프라이트 반전
        LookAtTarget(targetPos);

        transform.Translate(dir * data.moveSpeed * Time.deltaTime);
    }

    private void MoveTowardsBase()
    {
        if (DefenseBase.Current != null)
        {
            float distToBase = Vector2.Distance(transform.position, DefenseBase.Current.transform.position);

            if (distToBase <= data.attackRange)
            {
                _currentTarget = DefenseBase.Current.gameObject;
            }
            else
            {
                MoveTowards(DefenseBase.Current.transform.position);
            }
        }
    }


    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        float finalDamage = Mathf.Max(damage - data.defense, 1f);
        _currentHp -= finalDamage;

        // 피격 시에도 공격자를 바라보게 하기 필요
        // LookAtTarget(attackerPos);

        if (gameObject.activeSelf && !_isKnockbacking)
        {
            StartCoroutine(KnockbackRoutine(attackerPos));
        }

        if (_currentHp <= 0) Die();
    }

    private IEnumerator KnockbackRoutine(Vector2 attackerPos)
    {
        _isKnockbacking = true;
        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;

        // 넉백 중에는 이동 방향의 반대로 튕기므로 스프라이트 방향은 유지하거나 고정

        float elapsed = 0f;
        float duration = 0.15f;
        while (elapsed < duration)
        {
            transform.Translate(dir * 2f * Time.deltaTime);
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
                _currentTarget.GetComponent<UnitStat>().TakeDamage(data.attackPower, transform.position);
            else if (_currentTarget.CompareTag("Base"))
                _currentTarget.GetComponent<DefenseBase>().TakeDamage(data.attackPower);

            _lastAttackTime = Time.time;
        }
    }
}