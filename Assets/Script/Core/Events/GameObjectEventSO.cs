using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameObjectEvent", menuName = "RIOT/Events/GameObjectEvent")]
public class GameObjectEventSO : ScriptableObject
{
    private List<System.Action<GameObject>> _actions = new();

    public void Subscribe(System.Action<GameObject> callback)
    {
        if (!_actions.Contains(callback))
            _actions.Add(callback);
    }

    public void Unsubscribe(System.Action<GameObject> callback)
    {
        _actions.Remove(callback);
    }

    public void Raise(GameObject go)
    {
        for (int i = _actions.Count - 1; i >= 0; i--)
            _actions[i]?.Invoke(go);
    }
}