using UnityEngine;

public class WallConnector : MonoBehaviour
{
    [Header("Sprite Settings (0~15 순서대로 넣어주세요)")]
    [Tooltip("0:없음, 1:상, 2:우, 3:상우, 4:하, 5:상하, 6:우하, 7:상우하, 8:좌... 15:상하좌우")]
    public Sprite[] wallSprites = new Sprite[16];

    private SpriteRenderer _spriteRenderer;
    private LayerMask _wallLayer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _wallLayer = LayerMask.GetMask("Wall"); // 벽 레이어를 Wall로 설정해야 합니다.
    }

    // (1) 현재 내 벽의 이미지를 갱신하는 함수
    public void UpdateConnection()
    {
        int bitmask = 0;

        // 상 (0, 1)
        if (CheckWallAt(Vector2.up)) bitmask += 1;
        // 우 (1, 0)
        if (CheckWallAt(Vector2.right)) bitmask += 2;
        // 하 (0, -1)
        if (CheckWallAt(Vector2.down)) bitmask += 4;
        // 좌 (-1, 0)
        if (CheckWallAt(Vector2.left)) bitmask += 8;

        // 계산된 bitmask 값에 해당하는 스프라이트로 교체
        if (bitmask < wallSprites.Length && wallSprites[bitmask] != null)
        {
            _spriteRenderer.sprite = wallSprites[bitmask];
        }
    }

    // 주변 1칸 거리에 벽이 있는지 체크
    private bool CheckWallAt(Vector2 direction)
    {
        // 0.8f~1.0f 정도의 거리로 레이캐스트를 쏘아 인접한 벽 확인
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.1f, _wallLayer);
        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    // (2) 내 주변 4방향의 벽들도 갱신하도록 명령
    public void NotifyNeighbors()
    {
        Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

        foreach (Vector2 dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 1.1f, _wallLayer);
            if (hit.collider != null)
            {
                var neighboringWall = hit.collider.GetComponent<WallConnector>();
                if (neighboringWall != null)
                {
                    neighboringWall.UpdateConnection();
                }
            }
        }
    }
}