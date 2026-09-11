public static class AssemblyBudgetRules
{
    public const int RecruitmentPointsPerTimeStep = 1;

    public static bool TryCalculateTemporaryBonus(
        int startMinutesFromMidnight,
        int objectiveDeadlineMinutes,
        out int temporaryBonus)
    {
        temporaryBonus = 0;

        if (!FlyerTimeRules.IsValidStartTime(
                startMinutesFromMidnight,
                objectiveDeadlineMinutes))
        {
            return false;
        }

        int delayMinutes =
            startMinutesFromMidnight
            - FlyerTimeRules.EarliestStartMinutes;

        int delaySteps =
            delayMinutes / FlyerTimeRules.TimeStepMinutes;

        temporaryBonus =
            delaySteps * RecruitmentPointsPerTimeStep;

        return true;
    }
}
