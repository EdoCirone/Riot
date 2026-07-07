using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemEventSO", menuName = "RIOT/Events/ItemEventSO")]
public class ItemEventSO : ScriptableObject
{

    private List<System.Action<ItemSO>> _items = new();

    public void Subscribe(System.Action<ItemSO> callback)
    {
        if (!_items.Contains(callback))
            _items.Add(callback);
    }

    public void Unsubscribe(System.Action<ItemSO> callback)
    {
        _items.Remove(callback);
    }

    public void Raise(ItemSO item)
    {
        for (int i = _items.Count - 1; i >= 0; i--)
            _items[i]?.Invoke(item);
    }
}
