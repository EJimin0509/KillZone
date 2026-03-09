using UnityEngine;

public class UnitGachaManager : MonoBehaviour
{
    public UnitGenerator generator;
    private UnitData _lastGeneratedStat;
    private bool _canReroll = false;

    // 8. 결과 보여줌 (UI에서 호출)
    public void OnClickDraw()
    {
        _lastGeneratedStat = generator.GenerateRandomUnit();
        _canReroll = true; // 9. 재분배 기회 활성화
        ShowResultUI(_lastGeneratedStat);
    }

    // 9. 1회 다시 분배 가능
    public void OnClickReroll()
    {
        if (!_canReroll) return;

        _lastGeneratedStat = generator.GenerateRandomUnit();
        _canReroll = false; // 재분배 기회 소진
        ShowResultUI(_lastGeneratedStat);
    }

    private void ShowResultUI(UnitData stat)
    {
        Debug.Log($"생성 결과 - 체력:{stat.hp}, 격투:{stat.melee}, 사격:{stat.range}, " +
                  $"수리:{stat.repair}, 의술:{stat.medic}, 의지:{stat.will}, 신앙:{stat.faith}");
    }
}