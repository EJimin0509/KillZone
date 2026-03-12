using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "UnitAppearanceDatabase", menuName = "Gacha/Appearance Database")]
public class UnitAppearanceDatabase : ScriptableObject
{
    [Header("Exterior Sprites (스프라이트 리스트)")]
    [Tooltip("머리 부분 스프라이트들")]
    public List<Sprite> headSprites;

    [Tooltip("몸통 부분 스프라이트들")]
    public List<Sprite> bodySprites;

    [Header("Name Parts (이름 조합 리스트)")]
    [Tooltip("이름의 앞부분 (예: 용맹한, 거대한)")]
    public List<Sprite> firstNames; // 이름 앞부분을 Sprite로 할 경우 (현재는 Sprite 리스트로 되어있음)

    [Tooltip("이름의 뒷부분 (예: 전사, 용병, 이)")]
    public List<string> lastNames; // 이름 뒷부분 (예: "이", "자", "랑") - string 리스트

    /// <summary>
    /// 랜덤한 머리 스프라이트를 반환합니다.
    /// </summary>
    public Sprite GetRandomHead()
    {
        if (headSprites == null || headSprites.Count == 0) return null;
        return headSprites[Random.Range(0, headSprites.Count)];
    }

    /// <summary>
    /// 랜덤한 몸통 스프라이트를 반환합니다.
    /// </summary>
    public Sprite GetRandomBody()
    {
        if (bodySprites == null || bodySprites.Count == 0) return null;
        return bodySprites[Random.Range(0, bodySprites.Count)];
    }

    /// <summary>
    /// 랜덤한 조합 이름을 생성합니다.
    /// </summary>
    public string GetRandomName()
    {
        // 1. 성(First Name) 선택
        string firstName = "무명";
        if (firstNames != null && firstNames.Count > 0)
        {
            firstName = firstNames[Random.Range(0, firstNames.Count)].name; // Sprite 이름을 성으로 사용
        }

        // 2. 이름(Last Name) 선택
        string lastName = "이";
        if (lastNames != null && lastNames.Count > 0)
        {
            lastName = lastNames[Random.Range(0, lastNames.Count)];
        }

        return $"{firstName}{lastName}";
    }
}