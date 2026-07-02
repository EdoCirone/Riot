using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActionEventSO", menuName = "RIOT/Events/ActionEvent")]
public class ActionEventSO : ScriptableObject
{
    private List<System.Action<ActionType>> _actions = new();

    public void Subscribe(System.Action<ActionType> callback)
    {
        if (!_actions.Contains(callback))
            _actions.Add(callback);
    }

    public void Unsubscribe(System.Action<ActionType> callback)
    {
        _actions.Remove(callback);
    }

    public void Raise(ActionType action)
    {
        for (int i = _actions.Count - 1; i >= 0; i--)
            _actions[i]?.Invoke(action);
    }
}
