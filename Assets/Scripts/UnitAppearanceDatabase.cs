using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "UnitAppearanceDatabase", menuName = "Gacha/Appearance Database")]
public class UnitAppearanceDatabase : ScriptableObject
{
    [Header("전체 외형 스프라이트")]
    public List<Sprite> unitSprites;

    [Header("Name Combination (Text)")]
    public List<string> firstNames = new List<string> { "용맹한", "거대한", "날렵한", "고독한" };
    public List<string> lastNames = new List<string> { "용병", "기사", "도적", "전사" };

    /// <summary>
    /// 랜덤한 스프라이트 선택
    /// </summary>
    /// <returns></returns>
    public Sprite GetRandomSprite()
    {
        if (unitSprites == null || unitSprites.Count == 0) return null;
        return unitSprites[Random.Range(0, unitSprites.Count)];
    }

    /// <summary>
    /// 랜덤한 조합 이름을 생성합니다.
    /// </summary>
    public string GetRandomName()
    {
        string f = firstNames[Random.Range(0, firstNames.Count)];
        string l = lastNames[Random.Range(0, lastNames.Count)];
        return $"{f} {l}";
    }
}