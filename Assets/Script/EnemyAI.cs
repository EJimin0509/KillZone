using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float targetY = -8f;
    private PlayerObj _playerObj;

    private void Start()
    {
        // 모든 자식 Sprite Renderer Sorting Layer를 Object로 변경
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sortingLayerName = "Object";
        }

        _playerObj = GetComponent<PlayerObj>();
        Vector2 destination = new Vector2(transform.position.x, targetY);
        _playerObj.SetMovePos(destination);
    }
}