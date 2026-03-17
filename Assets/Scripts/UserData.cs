using System;
using System.Collections.Generic;

[Serializable]
public class UnitSaveData
{
    public string unitName;
    public string spriteKey;
    public int hp, melee, range, repair, medic, will, faith;
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