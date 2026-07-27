using UnityEngine;

public class ActionButtonPanel : MonoBehaviour
{
    [SerializeField] private ActionSlotUI[] _slots;

    [Header("Events")]
    [SerializeField] private ActionEventSO _actionButtonClickedEvent; 
    [SerializeField] private ActionEventSO _actionSelectedEvent;       

    private bool _isValid;

    private void Awake()
    {
        if (_slots == null || _slots.Length == 0 || _actionButtonClickedEvent == null || _actionSelectedEvent == null)
        {
            Debug.LogWarning("Refs missing in ActionButtonPanel");
            return;
        }
        _isValid = true;
        foreach (var slot in _slots) slot.Init(this);
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _actionSelectedEvent.Subscribe(OnActionSelected);
    }
    private void OnDisable()
    {
        if (!_isValid) return;
        _actionSelectedEvent.Unsubscribe(OnActionSelected);
    }

    private void OnActionSelected(ActionType action)
    {
        if (action == ActionType.None)
        {
            foreach (var slot in _slots)
                slot.SetActive(true); 
            return;
        }

        foreach (var slot in _slots)
            slot.SetActive(slot.Action == action);
    }

    public void OnSlotClicked(ActionType action)
    {
        _actionButtonClickedEvent?.Raise(action);
    }
}