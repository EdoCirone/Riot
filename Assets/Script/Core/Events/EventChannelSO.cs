using System.Collections.Generic;
using UnityEngine;
public abstract class EventChannelSO<T> : ScriptableObject
{
    private List<System.Action<T>> _actions = new();
    public void Subscribe(System.Action<T> cb) { if (!_actions.Contains(cb)) _actions.Add(cb); }
    public void Unsubscribe(System.Action<T> cb) { _actions.Remove(cb); }
    public void Raise(T value) { for (int i = _actions.Count - 1; i >= 0; i--) _actions[i]?.Invoke(value); }
}