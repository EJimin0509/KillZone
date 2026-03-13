using UnityEngine;

public class StructureProjectile : MonoBehaviour
{
    private Vector3 _targetPos;
    private float _speed = 10f;
    private bool _isCatapult;
    private float _centerDamage;
    private float _outerDamage;
    private Vector2 _aoeSize;
    private LayerMask _enemyLayer;

    // 회전 계산을 위한 변수
    private bool _hasTarget = false;

    public void Setup(Vector3 target, float speed, bool isCatapult, float centerDmg, float outerDmg, Vector2 aoe, LayerMask layer)
    {
        _targetPos = target;
        _speed = speed;
        _isCatapult = isCatapult;
        _centerDamage = centerDmg;
        _outerDamage = outerDmg;
        _aoeSize = aoe;
        _enemyLayer = layer;
        _hasTarget = true;

        // [핵심] 생성 즉시 목표 방향을 바라보게 회전
        RotateTowardsTarget();

        Debug.Log($"<color=yellow>[Projectile]</color> 생성됨. 목표: {_targetPos}, 속도: {_speed}");
    }

    private void Update()
    {
        if (!_hasTarget) return;

        // 이동 중에도 목표를 향한 회전을 유지 (목표가 움직일 경우 대비)
        RotateTowardsTarget();

        // 이동 로직
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, _speed * Time.deltaTime);

        // 도달 체크
        if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
        {
            Explode();
            Destroy(gameObject);
        }
    }

    // 목표 지점을 바라보게 rotation을 계산하는 함수
    private void RotateTowardsTarget()
    {
        Vector3 dir = _targetPos - transform.position;

        // 방향 벡터가 0이 아닐 때만 회전 계산
        if (dir != Vector3.zero)
        {
            // 2D 탑다운 게임에서 아크탄젠트를 이용해 각도(degree)를 구합니다.
            // 이 계산의 결과는 목표가 '오른쪽(X축 양의 방향)'일 때 0도입니다.
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // [핵심 해결책]
            // 스프라이트가 기본적으로 '위(Y축 양의 방향)'를 향하고 있으므로,
            // 계산된 각도에서 -90도를 빼서 보정해줍니다.
            // (위(+90)에서 오른쪽(0)으로 90도만큼 돌려야 목표를 바라보게 됩니다.)
            float correctedAngle = angle - 90f;

            // Z축을 기준으로 보정된 각도만큼 회전시킵니다.
            transform.rotation = Quaternion.AngleAxis(correctedAngle, Vector3.forward);
        }
    }

    private void Explode()
    {
        Debug.Log($"<color=orange>[Projectile]</color> 목표 도착! 폭발 범위: {_aoeSize}");

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(transform.position, _aoeSize, 0f, _enemyLayer);

        Debug.Log($"<color=orange>[Projectile]</color> 감지된 적 수: {hitEnemies.Length}");

        foreach (var enemyCollider in hitEnemies)
        {
            EnemyAI enemy = enemyCollider.GetComponent<EnemyAI>();
            if (enemy == null) continue;

            if (_isCatapult)
            {
                float dist = Vector2.Distance(transform.position, enemyCollider.transform.position);
                float damage = (dist <= 0.7f) ? _centerDamage : _outerDamage;
                enemy.TakeDamage(damage, transform.position);
            }
            else
            {
                // Ballista: 모든 범위 균일 데미지
                enemy.TakeDamage(_centerDamage, transform.position);
            }
        }
    }
}