using UnityEngine;

public class UnitGachaManager : MonoBehaviour
{
    public UnitGenerator generator;
    public UnitGachaUI gachaUI; // 유닛 표시 UI
    
    private UnitData _lastGeneratedStat;
    private bool _canReroll = false;

    private void Start()
    {
        // 처음에는 Draw만 활성화된 상태로 시작
        gachaUI.SetButtonState(true, false, false);
    }

    // 처음 뽑기 버튼
    // 결과 보여줌 (UI에서 호출)
    public void OnClickDraw()
    {
        if (generator == null)
        {
            Debug.LogError("UnitGenerator가 인스펙터에 할당되지 않았습니다!");
            return;
        }

        _lastGeneratedStat = generator.GenerateRandomUnit();
        _lastGeneratedStat.unitName = "용병이"; // 추후 랜덤 네이밍 설정

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("씬에 InventoryManager 오브젝트가 없습니다!");
            return;
        }

        _canReroll = true; // 재분배 기회 활성화

        if (gachaUI == null)
        {
            Debug.LogError("UnitGachaUI가 인스펙터에 할당되지 않았습니다!");
            return;
        }

        gachaUI.DisplayUnitStats(_lastGeneratedStat);
        gachaUI.SetButtonState(false, true, true); // Draw 비활성, 리롤/컨펌 활성
    }

    // 1회 다시 분배 가능
    public void OnClickReroll()
    {
        if (!_canReroll) return;

        _lastGeneratedStat = generator.GenerateRandomUnit();
        _lastGeneratedStat.unitName = "용병이";

        _canReroll = false; // 재분배 기회 소진

        gachaUI.DisplayUnitStats(_lastGeneratedStat);

        // 리롤 버튼 비활성화
        gachaUI.SetButtonState(false, false, true);
    }

    public void OnClickConfirm()
    {
        if (_lastGeneratedStat == null) return;

        // 최종 저장
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddUnit(_lastGeneratedStat);
        }

        // 초기화: 다시 Draw만 가능하게
        _lastGeneratedStat = null;
        gachaUI.ClearDisplay();
        gachaUI.SetButtonState(true, false, false);
    }
}