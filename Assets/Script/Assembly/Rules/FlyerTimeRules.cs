public static class FlyerTimeRules
{
    private const int MinutesPerDay = 24 * 60;

    public const int EarliestStartMinutes = 12 * 60;
    public const int TimeStepMinutes = 15;
    public const int MinimumTimeBeforeDeadlineMinutes = 60;

    public static int CalculateLatestStartMinutes(
        int objectiveDeadlineMinutes)
    {
        return objectiveDeadlineMinutes
            - MinimumTimeBeforeDeadlineMinutes;
    }

    public static bool IsValidStartTime(
        int startMinutes,
        int objectiveDeadlineMinutes)
    {
        if (startMinutes < 0 || startMinutes >= MinutesPerDay)
            return false;

        if (objectiveDeadlineMinutes < 0
            || objectiveDeadlineMinutes >= MinutesPerDay)
        {
            return false;
        }

        if (startMinutes < EarliestStartMinutes)
            return false;

        if (startMinutes >
            CalculateLatestStartMinutes(objectiveDeadlineMinutes))
        {
            return false;
        }

        return (startMinutes - EarliestStartMinutes)
            % TimeStepMinutes == 0;
    }
}
