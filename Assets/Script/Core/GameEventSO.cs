using System.Collections.Generic;
using UnityEngine;

// Interfaccia che ogni listener deve implementare.
// Chi vuole ricevere l'evento deve implementare questo contratto.
public interface IGameEventListener
{
    // Chiamato da GameEventSO.Raise() quando l'evento viene lanciato.
    void OnEventRaised();
}

// ScriptableObject che funge da canale evento (pattern Ryan Hipple).
// Vive come asset nel progetto: non dipende da nessuna scena.
// Gli oggetti in scena si iscrivono e disiscrivono a runtime.
[CreateAssetMenu(fileName = "NuovoEventoGioco", menuName = "RIOT/Core/Evento Gioco")]
public class GameEventSO : ScriptableObject
{
    // Lista di tutti gli oggetti attualmente in ascolto su questo canale.
    // Uso List invece di array perché il numero di listener cambia a runtime.
    private List<IGameEventListener> _ascoltatori = new List<IGameEventListener>();

    // Iscrive un listener: da questo momento riceverà le notifiche.
    // Chiamare in OnEnable() dei MonoBehaviour che vogliono ascoltare.
    public void Subscribe(IGameEventListener ascoltatore)
    {
        if (!_ascoltatori.Contains(ascoltatore))
            _ascoltatori.Add(ascoltatore);
    }

    // Disiscrive un listener: smette di ricevere notifiche.
    // Chiamare in OnDisable() per evitare riferimenti a oggetti distrutti.
    public void Unsubscribe(IGameEventListener ascoltatore)
    {
        _ascoltatori.Remove(ascoltatore);
    }

    // Lancia l'evento: notifica tutti i listener attualmente iscritti.
    // Itera al contrario per gestire in sicurezza eventuali disiscrizioni
    // che avvengono durante la notifica stessa.
    public void Raise()
    {
        for (int i = _ascoltatori.Count - 1; i >= 0; i--)
            _ascoltatori[i].OnEventRaised();
    }
}
