using UnityEngine;
using UnityEngine.Tilemaps;

public class RiverZone : MonoBehaviour
{
    [SerializeField] private float slowMultiplier = 0.1f;
    private Tilemap _tilemap;

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    private void Update()
    {
        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            // 적 위치를 타일 좌표로 변환
            Vector3Int tilePos = _tilemap.WorldToCell(enemy.transform.position);
            bool isInRiver = _tilemap.HasTile(tilePos);

            Debug.Log("타일 위치: " + tilePos + " 강 타일 있음: " + isInRiver);

            if (isInRiver)
            {
                enemy.ApplySpeedModifier(slowMultiplier);
            }
            else
            {
                enemy.RemoveSpeedModifier();
            }
        }
    }
}