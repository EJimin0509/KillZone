using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private UnitCombat _unitCombat;
    private SpriteRenderer _spriteRenderer;

    public bool IsSelected { get; private set; }

    // 배치 관련 상태 추가
    public bool IsPendingDeployment { get; private set; }
    public MannedStructure TargetStructure { get; private set; }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _unitCombat = GetComponent<UnitCombat>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (_spriteRenderer != null)
            _spriteRenderer.color = selected ? Color.green : Color.white;
    }

    // [중요] 일반 이동 시에는 배치 예약 해제
    public void CommandMove(Vector3 destination)
    {
        CancelDeployment();
        MoveTo(destination);
    }

    // [중요] 배치 전용 이동 명령
    public void CommandDeploy(MannedStructure structure, Vector3 destination)
    {
        IsPendingDeployment = true;
        TargetStructure = structure;
        MoveTo(destination);
    }

    private void MoveTo(Vector3 dest)
    {
        if (_agent != null && _agent.isActiveAndEnabled)
        {
            _unitCombat.SetManualCommand(null, true);
            _agent.isStopped = false;
            _agent.SetDestination(dest);
        }
    }

    public void CancelDeployment()
    {
        IsPendingDeployment = false;
        TargetStructure = null;
    }
}