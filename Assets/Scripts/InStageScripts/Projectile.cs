using UnityEngine;

// 투사체 이동, 명중률에 따른 궤적 뒤틀림, 그리고 경로상 충돌을 처리
public class Projectile : MonoBehaviour
{
    private float _damage;
    private Vector2 _targetPos;
    private float _speed = 10f;
    private string _targetTag; // "Enemy" 또는 "Unit"
    private bool _isInitialized = false;

    // 투사체 초기화 메서드
    public void Launch(float damage, Vector2 startPos, Vector2 targetPos, float accuracy, string targetTag)
    {
        _damage = damage;
        _targetTag = targetTag;

        // 명중률에 따른 타겟 지점 오차 계산
        float errorRange = (100f - accuracy) * 0.02f; // 명중률이 낮을수록 오차 범위 증가
        _targetPos = targetPos + new Vector2(Random.Range(-errorRange, errorRange), Random.Range(-errorRange, errorRange));

        transform.position = startPos;

        // 투사체가 날아가는 방향을 바라보게 회전
        Vector2 dir = (_targetPos - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // [수정] 화살촉이 위쪽을 향하고 있으므로 angle에서 90도를 빼서 방향을 맞춤
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowSound);

        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        // 목표 지점을 향해 이동
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, _speed * Time.deltaTime);

        // [추가] 매 프레임 이동 방향을 바라보도록 회전 (포물선이나 경로 변경 대비)
        // 현재 위치와 목표 위치의 차이를 계산하여 실시간 회전
        Vector2 currentDir = (_targetPos - (Vector2)transform.position).normalized;
        if (currentDir != Vector2.zero)
        {
            float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle - 90f);
        }

        // 목표 지점 도달 시 반납
        if (Vector2.Distance(transform.position, _targetPos) < 0.1f)
        {
            Release();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isInitialized) return;

        // 경로상 타겟 태그와 충돌 시 대미지
        if (collision.CompareTag(_targetTag))
        {
            // [수정] BossAI 대응 추가 (EnemyAI와 로직 통합 가능성 고려)
            if (_targetTag == "Enemy")
            {
                collision.GetComponent<EnemyAI>()?.TakeDamage(_damage, transform.position);
                collision.GetComponent<BossAI>()?.TakeDamage(_damage, transform.position); // 보스 대응 추가
            }
            else if (_targetTag == "Unit")
                collision.GetComponent<UnitStat>()?.TakeDamage(_damage, transform.position);
            else if (_targetTag == "Base")
                collision.GetComponent<DefenseBase>()?.TakeDamage(_damage);

            Release();
            return;
        }

        // 적이 발사한 투사체(_targetTag == "Unit")인 경우에만 Base를 공격 가능
        //if (_targetTag == "Base" && collision.CompareTag("Base"))
        //{
        //    collision.GetComponent<DefenseBase>()?.TakeDamage(_damage);
        //    Release();
        //}
    }

    private void Release()
    {
        _isInitialized = false;
        if (SimpleObjectPool.Instance != null)
            SimpleObjectPool.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }
}