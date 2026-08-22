using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    private List<InventorySlot> _slots = new();

    public bool HasItem(ItemSO item)
    {
        if (item == null)
            return false;

        foreach (InventorySlot slot in _slots)
        {
            if (slot.Item == item && slot.Quantity > 0)
                return true;
        }

        return false;
    }

    public bool ConsumeItem(ItemSO item)
    {
        if (item == null)
            return false;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];

            if (slot.Item != item || slot.Quantity <= 0)
                continue;

            slot.Quantity--;

            if (slot.Quantity == 0)
                _slots.RemoveAt(i);

            return true;
        }

        return false;
    }

    public void AddItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        foreach (InventorySlot slot in _slots)
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
