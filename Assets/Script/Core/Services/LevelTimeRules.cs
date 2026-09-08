public static class LevelTimeRules
{
    public static int CalculateCurrentMinutes(int startMinutes, int turnIndex, int minutesPerTurn)
    {
        return startMinutes + turnIndex * minutesPerTurn;
    }

    public static bool HasReachedDeadline(int currentMinutes, int deadlineMinutes)
    {
        return currentMinutes >= deadlineMinutes;
    }
}
