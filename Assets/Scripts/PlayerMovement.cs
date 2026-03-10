using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private SpriteRenderer _spriteRenderer;
    private UnitCombat _unitCombat;

    public bool IsSelected { get; private set; }
    private MannedStructure _targetStructure; // 배치 명령이 내려진 목적지 구조물

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _unitCombat = GetComponent<UnitCombat>();

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }

    void Update()
    {
        if (BuildManager.Instance != null && BuildManager.Instance.IsBuildingPhase) return;
        if (!_agent.isOnNavMesh || !_agent.isActiveAndEnabled) return;

        if (_agent.pathStatus == NavMeshPathStatus.PathPartial || !_agent.hasPath)
        {
            if (_agent.remainingDistance < 0.1f) _agent.ResetPath();
        }

        // 구조물로 이동 중, 거리가 1.5 이내로 가까워지면 탑승 완료
        if (_targetStructure != null && _agent.hasPath && !_agent.pathPending)
        {
            if (_agent.remainingDistance <= 1.5f)
            {
                _targetStructure.GarrisonUnit(GetComponent<UnitStat>());
                _targetStructure = null;
            }
        }

        HandleSpriteFlip();
    }

    public void SetSelection(bool value)
    {
        IsSelected = value;
    }

    public void CommandMove(Vector2 targetPos)
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

        _targetStructure = null; // 단순 이동 명령 시 기존 배치 명령 취소

        NavMeshPath path = new NavMeshPath();
        Vector3 dest = new Vector3(targetPos.x, targetPos.y, 0);

        if (_agent.CalculatePath(dest, path))
        {
            _agent.SetPath(path);
            if (_unitCombat != null) _unitCombat.SetManualCommand(null, true);
        }
    }

    // CommandManager에서 호출됨
    public void CommandDeploy(MannedStructure structure)
    {
        _targetStructure = structure;
        CommandMove(structure.transform.position); // 일단 구조물 위치로 걸어감
    }

    private void HandleSpriteFlip()
    {
        if (_unitCombat != null && _unitCombat.CurrentTarget != null && !_unitCombat.IsForceMoving)
        {
            _spriteRenderer.flipX = _unitCombat.CurrentTarget.transform.position.x < transform.position.x;
            return;
        }

        if (_agent.velocity.x > 0.01f) _spriteRenderer.flipX = false;
        else if (_agent.velocity.x < -0.01f) _spriteRenderer.flipX = true;
    }
}