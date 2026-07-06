using UnityEngine;

public class InventoryView : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private InventorySlotUI[] _slots;   

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private ActionEventSO _actionSelectedEvent;

    private bool _isValid;

    private void Awake()
    {
        if (_slots == null || _slots.Length == 0 || _unitSelectedEvent == null || _unitDeselectedEvent == null)
        {
            Debug.LogWarning("Refs missing in InventoryView");
            return;
        }
        _isValid = true;
        ClearAllSlots();
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _actionSelectedEvent.Subscribe(OnActionSelected);
        _unitSelectedEvent.Subscribe(OnUnitSelected);
        _unitDeselectedEvent.Subscribe(OnUnitDeselected);
    }
    private void OnDisable()
    {
        if (!_isValid) return;
        _actionSelectedEvent.Unsubscribe(OnActionSelected);
        _unitSelectedEvent.Unsubscribe(OnUnitSelected);
        _unitDeselectedEvent.Unsubscribe(OnUnitDeselected);
    }

    private void OnUnitSelected(AbstractUnitsRunTime unit)
    {
        if (unit is SpezzoneRuntime spezzone)
            ShowInventory(spezzone.Inventory);
        else
            ClearAllSlots();  
    }

    private void OnUnitDeselected() => ClearAllSlots();

    private void ShowInventory(Inventory inventory)
    {
        var slots = inventory.Slots;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < slots.Count)
                _slots[i].SetItem(slots[i].Item, slots[i].Quantity);   
            else
                _slots[i].Clear();                                      
        }
    }

    private void OnActionSelected(ActionType action)
    {
        if (action == ActionType.None)
        {
            foreach (var slot in _slots)
                slot.SetHighlighted(true);   // o uno stato "neutro" — vedi sotto
            return;
        }

        foreach (var slot in _slots)
            slot.SetHighlighted(slot.IsCompatibleWith(action));
    }

    private void ClearAllSlots()
    {
        foreach (var slot in _slots)
            slot.Clear();
    }
}