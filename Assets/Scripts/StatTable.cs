public static class StatTable
{
    public static readonly float[] HP = { 0, 85, 90, 95, 100, 105, 110, 115, 120, 125, 130 };
    public static readonly float[] ATK = { 0, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
    public static readonly float[] ACC = { 0.15f, 0.23f, 0.31f, 0.39f, 0.47f, 0.55f, 0.63f, 0.71f, 0.79f, 0.87f, 0.95f };
    public static readonly float[] REPAIR = { 0, 0.05f, 0.06375f, 0.0775f, 0.09125f, 0.105f, 0.105f, 0.11875f, 0.1325f, 0.14625f, 0.16f };
    public static readonly float[] HEAL = { 0.1f, 0.19f, 0.28f, 0.37f, 0.46f, 0.55f, 0.64f, 0.73f, 0.82f, 0.91f, 1f };
    public static readonly float[] MTL_MAX = { 0, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50 };
    public static readonly float[] MTL_SPEED = { 0, 1, 1.5f, 2, 2.5f, 3, 3.5f, 4, 4.5f, 5, 5.5f };

    public static float GetValue(StatBonusType type, int level)
    {
        // 직접 범위 제한 (0~10단계)
        if (level < 0) level = 0;
        if (level > 10) level = 10;

        return type switch
        {
            StatBonusType.Hp => HP[level],
            StatBonusType.AttackPower => ATK[level],
            StatBonusType.RangeAccuracy => ACC[level],
            StatBonusType.RepairSpeed => REPAIR[level],
            StatBonusType.HealSpeed => HEAL[level],
            StatBonusType.MentalValue => MTL_MAX[level],
            StatBonusType.MentalHealAmount => MTL_SPEED[level],
            _ => 0
        };
    }
}