using UnityEngine;
using UnityEngine.Events;

// MonoBehaviour che fa da ponte tra un GameEventSO e le risposte in scena.
// Si aggiunge a qualsiasi GameObject che deve reagire a un evento di gioco.
public class GameEventListenerMB : MonoBehaviour, IGameEventListener
{
    // L'evento ScriptableObject a cui questo listener si iscrive.
    // Si assegna dall'Inspector trascinando l'asset GameEventSO.
    [SerializeField] private GameEventSO _evento;

    // La risposta da eseguire quando l'evento viene lanciato.
    // UnityEvent permette di collegare metodi direttamente dall'Inspector,
    // senza scrivere codice aggiuntivo per ogni caso d'uso.
    [SerializeField] private UnityEvent _risposta;

    // Quando il GameObject diventa attivo, ci si iscrive al canale evento.
    // OnEnable viene chiamato anche al primo avvio, dopo Awake e Start.
    private void OnEnable()
    {
        _evento.Subscribe(this);
    }

    // Quando il GameObject viene disattivato o distrutto, ci si disiscrive.
    // Fondamentale per evitare che Raise() chiami un oggetto già distrutto.
    private void OnDisable()
    {
        _evento.Unsubscribe(this);
    }

    // Chiamato da GameEventSO.Raise(). Esegue tutte le risposte collegate
    // nell'Inspector tramite il campo _risposta.
    public void OnEventRaised()
    {
        _risposta.Invoke();
    }
}
