using UnityEngine;
using UnityEngine.Events;

public class GameEventListenerMB : MonoBehaviour, IGameEventListener
{

    [SerializeField] private GameEventSO _event;

    [SerializeField] private UnityEvent _response;

    private void OnEnable()
    {
        _event.Subscribe(this);
    }

    private void OnDisable()
    {
        _event.Unsubscribe(this);
    }

    public void OnEventRaised()
    {
        _response.Invoke();
    }
}
