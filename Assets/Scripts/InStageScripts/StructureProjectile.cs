using UnityEngine;

public class StructureProjectile : MonoBehaviour
{
    private Vector3 _targetPoint; // 최종 착탄 지점
    private float _speed;
    private float _damage;
    private float _explosionRadius; // 3x3 범위를 커버할 반지름 (약 1.5f)
    private LayerMask _enemyLayer;
    private bool _isInitialized = false;

    public void Setup(Vector3 targetCenter, float speed, float damage, float accuracy, float areaRadius, LayerMask layer)
    {
        // 1. 명중률(20%) 보정 로직
        // 20% 확률로 정확히 명중, 80% 확률로 주변 오차 발생
        Vector3 finalPoint = targetCenter;
        if (Random.value > accuracy) // accuracy가 0.2f(20%)라면
        {
            // 오차 범위 생성 (3x3 공간 내외로 빗나가게 설정)
            float offset = 1.5f;
            finalPoint += new Vector3(Random.Range(-offset, offset), Random.Range(-offset, offset), 0);
        }

        _targetPoint = new Vector3(finalPoint.x, finalPoint.y, 0);
        _speed = speed;
        _damage = damage;
        _explosionRadius = areaRadius; // 3x3 이라면 보통 1.5f 정도가 적당합니다.
        _enemyLayer = layer;

        // 투사체 회전 (날아가는 방향 보기)
        Vector2 dir = (_targetPoint - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        _isInitialized = true;
        Destroy(gameObject, 5f); // 맵 밖으로 나갈 경우 대비
    }

    private void Update()
    {
        if (!_isInitialized) return;

        // 목표 지점으로 이동
        transform.position = Vector3.MoveTowards(transform.position, _targetPoint, _speed * Time.deltaTime);

        // 도착 시 폭발
        if (Vector3.Distance(transform.position, _targetPoint) < 0.1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 3x3 크기의 공간 판정 (OverlapCircle 또는 OverlapBox)
        // 반지름 1.5f의 원형 판정이 3x3 정사각형과 가장 유사합니다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _enemyLayer);

        bool hitSomething = false;
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(_damage, transform.position);
                hitSomething = true;
            }
        }

        if (hitSomething) Debug.Log($"<color=red>[Explosion]</color> {_damage} 데미지 입힘");

        // 폭발 프리팹이 있다면 여기서 생성 가능
        Destroy(gameObject);
    }

    // 에디터에서 공격 범위를 시각적으로 확인하기 위함
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}