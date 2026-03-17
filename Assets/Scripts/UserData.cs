using System;
using System.Collections.Generic;

[Serializable]
public class UnitSaveData
{
    public string unitName;
    public string spriteKey;
    public int hp, melee, range, repair, medic, will, faith;

    public int squadIndex = -1;      // -1이면 미편성, 0~9면 해당 스쿼드 슬롯 번호
    public string equippedHelmKey;   // 장착 중인 투구의 이름(또는 spriteKey)
    public string equippedChestKey;  // 장착 중인 갑옷의 이름
    public string equippedWeaponKey; // 장착 중인 무기의 이름
}

[Serializable]
public class EquipSaveData
{
    public string equipName;
    public string spriteKey;
    public EquipmentType type;
    public float attackRange;
    public float defense;
    public List<StatBonus> bonuses;
}

[Serializable]
public class UserData
{
    public int gold;
    public int currentStage;
    public bool[] stageUnlocked;
    public List<UnitSaveData> ownedUnits = new List<UnitSaveData>();
    public List<EquipSaveData> ownedEquips = new List<EquipSaveData>();
}