using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UnitGenerator : MonoBehaviour
{
    private const int TOTAL_EXTRA_POINTS = 15;
    private const int MAX_STAT_LIMIT = 7;

    public UnitData GenerateRandomUnit()
    {
        UnitData newStat = ScriptableObject.CreateInstance<UnitData>();
        newStat.SetDefault(); // 1. 기본 포인트 부여

        int remainingPoints = TOTAL_EXTRA_POINTS;
        List<string> statNames = new List<string> { "hp", "melee", "range", "repair", "medic", "will", "faith" };
        List<string> currentPool = new List<string>(statNames);

        // 2~7. 포인트 분배 로직
        while (remainingPoints > 0)
        {
            // 리스트가 비면 다시 7개 채움
            if (currentPool.Count == 0) currentPool = new List<string>(statNames);

            // 리스트에서 랜덤하게 하나 뽑기
            int randomIndex = Random.Range(0, currentPool.Count);
            string selectedStat = currentPool[randomIndex];

            // 현재 스탯의 값 확인 및 최대치(7)까지 남은 양 계산
            int currentVal = GetStatValue(newStat, selectedStat);
            int canAdd = MAX_STAT_LIMIT - currentVal;

            if (canAdd > 0)
            {
                // 최소 0 ~ (남은 포인트와 스탯 최대치 중 작은 값) 사이 랜덤 부여
                int addAmount = Random.Range(0, Mathf.Min(remainingPoints, canAdd) + 1);
                SetStatValue(newStat, selectedStat, currentVal + addAmount);
                remainingPoints -= addAmount;
            }

            // 스탯 부여 완료(또는 최대치 도달) 시 리스트에서 제거
            currentPool.RemoveAt(randomIndex);
        }

        return newStat;
    }

    // 리플렉션을 쓰지 않고 깔끔하게 처리하기 위한 헬퍼
    private int GetStatValue(UnitData data, string name)
    {
        return name switch
        {
            "hp" => data.hp,
            "melee" => data.melee,
            "range" => data.range,
            "repair" => data.repair,
            "medic" => data.medic,
            "will" => data.will,
            "faith" => data.faith,
            _ => 0
        };
    }

    private void SetStatValue(UnitData data, string name, int val)
    {
        switch (name)
        {
            case "hp": data.hp = val; break;
            case "melee": data.melee = val; break;
            case "range": data.range = val; break;
            case "repair": data.repair = val; break;
            case "medic": data.medic = val; break;
            case "will": data.will = val; break;
            case "faith": data.faith = val; break;
        }
    }
}