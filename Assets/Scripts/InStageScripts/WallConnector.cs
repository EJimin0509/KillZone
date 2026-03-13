using UnityEngine;

public class WallConnector : MonoBehaviour
{
    [Header("Sprite Settings (0~15)")]
    public Sprite[] wallSprites = new Sprite[16];

    private SpriteRenderer _spriteRenderer;
    private LayerMask _wallLayer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _wallLayer = LayerMask.GetMask("Wall");
    }

    public void UpdateConnection()
    {
        int bitmask = 0;

        // 주변 벽 체크 (상, 우, 하, 좌 순서)
        if (CheckWallAt(Vector2.up)) bitmask += 1;
        if (CheckWallAt(Vector2.right)) bitmask += 2;
        if (CheckWallAt(Vector2.down)) bitmask += 4;
        if (CheckWallAt(Vector2.left)) bitmask += 8;

        if (_spriteRenderer != null && bitmask < wallSprites.Length)
        {
            if (wallSprites[bitmask] != null)
            {
                _spriteRenderer.sprite = wallSprites[bitmask];
                // 검게 보인다면 아래 코드가 도움이 될 수 있습니다.
                _spriteRenderer.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: 비트마스크 {bitmask}번에 스프라이트가 없습니다!");
            }
        }
    }

    private bool CheckWallAt(Vector2 direction)
    {
        // 0.4f 반경의 원으로 인접한 벽을 찾습니다. (레이캐스트보다 정확할 수 있음)
        Collider2D hit = Physics2D.OverlapCircle((Vector2)transform.position + direction, 0.2f, _wallLayer);
        return hit != null && hit.gameObject != gameObject;
    }

    public void NotifyNeighbors()
    {
        Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        foreach (Vector2 dir in directions)
        {
            Collider2D hit = Physics2D.OverlapCircle((Vector2)transform.position + dir, 0.2f, _wallLayer);
            if (hit != null)
            {
                var neighboringWall = hit.GetComponent<WallConnector>();
                if (neighboringWall != null) neighboringWall.UpdateConnection();
            }
        }
    }

    // 삭제될 때 주변 벽들에게 알림
    private void OnDestroy()
    {
        // 씬이 종료되는 중이 아닐 때만 실행
        if (!gameObject.scene.isLoaded) return;
        NotifyNeighbors();
    }
}