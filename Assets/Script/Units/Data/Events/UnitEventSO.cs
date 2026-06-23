using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitsEvent", menuName = "RIOT/Events/UnitEvent")]
public class UnitEventSO : ScriptableObject
{
    private List<System.Action<AbstractUnitsRunTime>> _actions = new();

    public void Subscribe(System.Action<AbstractUnitsRunTime> callback)
    {
        if (!_actions.Contains(callback))
            _actions.Add(callback);
    }

    public void Unsubscribe(System.Action<AbstractUnitsRunTime> callback)
    {
        _actions.Remove(callback);
    }

    public void Raise(AbstractUnitsRunTime unit)
    {
        for (int i = _actions.Count - 1; i >= 0; i--)
            _actions[i]?.Invoke(unit);
    }
}