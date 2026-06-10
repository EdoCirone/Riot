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


    public void Subscribe(IGameEventListener ascoltatore)
    {
        if (!_listeners.Contains(ascoltatore))
            _listeners.Add(ascoltatore);
    }


    public void Unsubscribe(IGameEventListener ascoltatore)
    {
        _listeners.Remove(ascoltatore);
    }


    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnEventRaised();
    }
}
