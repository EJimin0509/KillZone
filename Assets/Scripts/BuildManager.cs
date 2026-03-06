using UnityEngine;
using UnityEngine.AI;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Settings")]
    [SerializeField] private GameObject wallPrefab; // 방어벽 프리팹
    [SerializeField] private int maxCost = 100;      // 최대 코스트
    private int _currentCost;

    [Header("Reference")]
    [SerializeField] private Transform baseTransform; // DefenseBase 위치
    [SerializeField] private Transform enemySpawnPoint; // 적 스폰 위치 (대표 지점 하나)

    public bool IsBuildingPhase = true; // 웨이브 전 건설 단계 여부

    private void Awake() => Instance = this;

    private void Update()
    {
        if (!IsBuildingPhase) return;

        if (Input.GetMouseButtonDown(0)) // 좌클릭 설치
        {
            HandleBuild();
        }
    }

    private void HandleBuild()
    {
        // 1. 마우스 위치를 그리드(타일) 좌표로 변환
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = new Vector3Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y), 0);

        // 2. 코스트 체크 (벽 하나당 10이라 가정)
        if (_currentCost + 10 > maxCost) return;

        // 3. 임시로 벽을 설치해보고 경로가 끊기는지 테스트
        if (CanReachBase(gridPos))
        {
            Instantiate(wallPrefab, (Vector3)gridPos, Quaternion.identity);
            _currentCost += 10;
            Debug.Log($"설치 완료! 현재 코스트: {_currentCost}/{maxCost}");
        }
        else
        {
            Debug.Log("길을 완전히 막을 수 없습니다!");
        }
    }

    /// <summary>
    /// 특정 위치에 벽을 세웠을 때 적이 베이스까지 올 수 있는지 검사
    /// </summary>
    private bool CanReachBase(Vector3 testPos)
    {
        NavMeshPath path = new NavMeshPath();

        // NavMesh.CalculatePath를 사용해 스폰 지점에서 베이스까지 경로가 생성되는지 확인
        // NavMesh.AllAreas를 사용해야 Not Walkable을 피해서 경로를 찾음
        NavMesh.CalculatePath(enemySpawnPoint.position, baseTransform.position, NavMesh.AllAreas, path);

        // 경로의 상태가 Complete(도달 가능)일 때만 true 반환
        return path.status == NavMeshPathStatus.PathComplete;
    }

    public void StartWave()
    {
        IsBuildingPhase = false;
        // 여기에 웨이브 시작 이벤트 호출 (EnemySpawner 활성화 등)
    }
}