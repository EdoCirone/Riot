using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CorteoSelectionSO",
    menuName = "RIOT/Assembly/CorteoSelectionSO")]
public sealed class CorteoSelectionSO : ScriptableObject
{

    [System.NonSerialized]
    private List<SpezzoneSO> _selectedUnits = new();

    private List<SpezzoneSO> MutableSelectedUnits =>
        _selectedUnits ??= new List<SpezzoneSO>();

    public IReadOnlyList<SpezzoneSO> SelectedUnits =>
        MutableSelectedUnits;

    public int Count => MutableSelectedUnits.Count;
    public bool HasSelection => Count > 0;

    public bool TryAdd(
     SpezzoneSO unit,
     int meetingPointCapacity)
    {
        if (unit == null
            || meetingPointCapacity <= 0
            || Count >= meetingPointCapacity)
        {
            return false;
        }

        MutableSelectedUnits.Add(unit);
        return true;
    }

    public bool TryRemoveOne(SpezzoneSO unit)
    {
        if (unit == null)
            return false;

        int index = MutableSelectedUnits.LastIndexOf(unit);

        if (index < 0)
            return false;

        MutableSelectedUnits.RemoveAt(index);
        return true;
    }

    public int CountOf(SpezzoneSO unit)
    {
        if (unit == null)
            return 0;

        int count = 0;

        foreach (SpezzoneSO selectedUnit in MutableSelectedUnits)
        {
            if (selectedUnit == unit)
                count++;
        }

        return count;
    }

    public void ClearSelection()
    {
        MutableSelectedUnits.Clear();
    }
}
