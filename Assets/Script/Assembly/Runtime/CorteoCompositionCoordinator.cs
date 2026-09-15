public sealed class CorteoCompositionCoordinator
{
    private readonly CorteoSelectionSO _selection;
    private readonly AssemblyBudgetState _budget;

    public CorteoCompositionCoordinator(
        CorteoSelectionSO selection,
        AssemblyBudgetState budget)
    {
        _selection = selection;
        _budget = budget;
    }
    public bool TryAdd(
        SpezzoneSO unit,
        HexMapSO map,
        MeetingPointSO meetingPoint)
    {
        if (!MeetingPointCapacityQuery.TryCalculate(
                map,
                meetingPoint,
                out int meetingPointCapacity))
        {
            return false;
        }

        return TryAdd(
            unit,
            meetingPointCapacity);
    }

    public bool TryAdd(
        SpezzoneSO unit,
        int meetingPointCapacity)
    {
        if (_selection == null || _budget == null)
            return false;

        bool canAdd = CorteoCompositionRules.CanAdd(
            _selection,
            unit,
            meetingPointCapacity,
            _budget.AvailablePoints);

        if (!canAdd)
            return false;

        return _selection.TryAdd(
            unit,
            meetingPointCapacity);
    }

    public bool TryRemoveOne(SpezzoneSO unit)
    {
        return _selection != null
            && _selection.TryRemoveOne(unit);
    }
}
