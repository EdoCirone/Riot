
public static class TensionRules
{
    public const int MinValue = 0;
    public const int MaxValue = 100;

    public static int GetInitialTension(int repression)
    {
        int value = ClampToScale(repression);

        if (value <= 30)
            return 0;

        if (value <= 60)
            return 10;

        if (value <= 90)
            return 30;

        return 40;
    }

    public static EngagementRules GetEngagementRules(int tension)
    {
        int value = ClampToScale(tension);
        if (value <= 29)
            return EngagementRules.Containment;
        if (value <= 59)
            return EngagementRules.Engage;

        return EngagementRules.Sweep;
    }

    public static int ApplyDelta(int currentTension, int delta)
    {

       long result = (long)ClampToScale(currentTension) + delta;

        if(result < MinValue)
            return MinValue;
        if(result > MaxValue)
            return MaxValue;

        return (int)result;

    }

    private static int ClampToScale(int value)
    {
        if (value < MinValue)
            return MinValue;
        if (value > MaxValue)
            return MaxValue;
        return value;
    }

}
