using System.Collections.Generic;
using UnityEngine;

// Interfaccia che ogni listener deve implementare.
// Chi vuole ricevere l'evento deve implementare questo contratto.
public interface IGameEventListener
{
    void OnEventRaised();
}


[CreateAssetMenu(fileName = "NuovoEventoGioco", menuName = "RIOT/Core/Evento Gioco")]
public class GameEventSO : ScriptableObject
{
    
    private List<IGameEventListener> _listeners = new List<IGameEventListener>();
    private List<System.Action> _actions = new List<System.Action>();

    public void Subscribe(IGameEventListener listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void Unsubscribe(IGameEventListener listener)
    {
        _listeners.Remove(listener);
    }

    public void Subscribe(System.Action callback)
    {
        if (!_actions.Contains(callback))
            _actions.Add(callback);
    }

    public void Unsubscribe(System.Action callback)
    {
        _actions.Remove(callback);
    }

    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnEventRaised();
        for (int i = _actions.Count - 1; i >= 0; i--)
            _actions[i]?.Invoke();
    }
}
