using System.Collections.Generic;
using UnityEngine;

public sealed class GameplayInputGate : MonoBehaviour
{
    private readonly HashSet<EntityId> _owners = new();
    public bool IsBlocked => _owners.Count > 0;

    public void Acquire(Object owner)
    {
        if (owner == null) return;

        _owners.Add(owner.GetEntityId());
    }

    public void Release(Object owner)
    {
        if (owner == null) return;

        _owners.Remove(owner.GetEntityId());
    }

    private void OnDisable()
    {
        _owners.Clear();
    }
}
