using UnityEngine;

public sealed class CorteoCompositionController : MonoBehaviour
{
    [Header("Runtime state")]
    [SerializeField] private CorteoSelectionSO _corteoSelection;
    [SerializeField] private FlyerSelectionSO _flyerSelection;
    [SerializeField] private AssemblyBudgetState _budgetState;

    [Header("Map")]
    [SerializeField] private HexMapSO _mapData;

    private CorteoCompositionCoordinator _coordinator;

    private void Awake()
    {
        if (_corteoSelection != null)
            _corteoSelection.ClearSelection();

        _coordinator =
            new CorteoCompositionCoordinator(
                _corteoSelection,
                _budgetState);
    }

    public bool TryAdd(SpezzoneSO unit)
    {
        if (_coordinator == null
            || _flyerSelection == null
            || !_flyerSelection.HasSelection
            || _mapData == null)
        {
            return false;
        }

        return _coordinator.TryAdd(
            unit,
            _mapData,
            _flyerSelection.MeetingPoint);
    }

    public bool TryRemoveOne(SpezzoneSO unit)
    {
        return _coordinator != null
            && _coordinator.TryRemoveOne(unit);
    }
}
