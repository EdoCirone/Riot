public static class CorteoCompositionRules
{
    public static bool CanAdd(
        CorteoSelectionSO selection,
        SpezzoneSO unit,
        int meetingPointCapacity,
        int availablePoints)
    {
        if (selection == null
            || unit == null
            || meetingPointCapacity <= 0
            || availablePoints < 0)
        {
            return false;
        }

        if (selection.Count >= meetingPointCapacity)
            return false;

        return selection.TotalActivationCost
            + unit.ActivationCost
            <= availablePoints;
    }
}
