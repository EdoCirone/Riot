using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    private List<InventorySlot> _slots = new();

    public bool HasItem(ItemSO item)
    {
        foreach (var slot in _slots)
        {
            if (slot.Item == item && slot.Quantity >0)
            {
                return true;
            }
        }
        return false;
    }

    public bool ConsumeItem(ItemSO item)
    {
        foreach (var slot in _slots)
        {
            if (slot.Item == item && slot.Quantity > 0)
            {
                slot.Quantity--;
                if (slot.Quantity == 0) _slots.Remove(slot);
                return true; 
            }
        }
        return false;  
    }

    public void AddItem(ItemSO item, int amount = 1) 
    {
        foreach (var slot in _slots)
        {
            if (slot.Item == item)
            {
                slot.Quantity += amount;   
                return;
            }
        }
        _slots.Add(new InventorySlot(item, amount));  
    }

    public IReadOnlyList<InventorySlot> Slots => _slots;
}
