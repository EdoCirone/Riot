using NUnit.Framework;
using UnityEngine;

public class InventoryTests
{
    private Inventory _inventory;
    private ThrowItemSO _throwItem;
    private BarricadeSO _barricade;

    [SetUp]
    public void SetUp()
    {
        _inventory = new Inventory();
        _throwItem = ScriptableObject.CreateInstance<ThrowItemSO>();
        _barricade = ScriptableObject.CreateInstance<BarricadeSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_throwItem);
        Object.DestroyImmediate(_barricade);
    }

    [Test]
    public void NewInventory_StartsEmpty()
    {
        Assert.That(_inventory.Slots, Is.Empty);
    }

    [Test]
    public void AddItem_CreatesSlotWithRequestedQuantity()
    {
        _inventory.AddItem(_throwItem, 2);

        Assert.That(_inventory.Slots.Count, Is.EqualTo(1));
        Assert.That(_inventory.Slots[0].Item, Is.SameAs(_throwItem));
        Assert.That(_inventory.Slots[0].Quantity, Is.EqualTo(2));
        Assert.That(_inventory.HasItem(_throwItem), Is.True);
    }

    [Test]
    public void AddItem_StacksIdenticalItemsInSameSlot()
    {
        _inventory.AddItem(_throwItem, 2);
        _inventory.AddItem(_throwItem, 3);

        Assert.That(_inventory.Slots.Count, Is.EqualTo(1));
        Assert.That(_inventory.Slots[0].Quantity, Is.EqualTo(5));
    }

    [Test]
    public void AddItem_CreatesSeparateSlotsForDifferentItems()
    {
        _inventory.AddItem(_throwItem);
        _inventory.AddItem(_barricade);

        Assert.That(_inventory.Slots.Count, Is.EqualTo(2));
        Assert.That(_inventory.HasItem(_throwItem), Is.True);
        Assert.That(_inventory.HasItem(_barricade), Is.True);
    }

    [Test]
    public void ConsumeItem_DecreasesQuantityAndReturnsTrue()
    {
        _inventory.AddItem(_throwItem, 2);

        bool consumed = _inventory.ConsumeItem(_throwItem);

        Assert.That(consumed, Is.True);
        Assert.That(_inventory.Slots.Count, Is.EqualTo(1));
        Assert.That(_inventory.Slots[0].Quantity, Is.EqualTo(1));
    }

    [Test]
    public void ConsumeItem_RemovesSlotWhenQuantityReachesZero()
    {
        _inventory.AddItem(_throwItem);

        bool consumed = _inventory.ConsumeItem(_throwItem);

        Assert.That(consumed, Is.True);
        Assert.That(_inventory.Slots, Is.Empty);
        Assert.That(_inventory.HasItem(_throwItem), Is.False);
    }

    [Test]
    public void ConsumeItem_ReturnsFalseWhenItemIsMissing()
    {
        _inventory.AddItem(_throwItem);

        bool consumed = _inventory.ConsumeItem(_barricade);

        Assert.That(consumed, Is.False);
        Assert.That(_inventory.Slots[0].Quantity, Is.EqualTo(1));
    }

    [Test]
    public void AddItem_DoesNotCreateSlotWhenItemIsNull()
    {
        _inventory.AddItem(null);

        Assert.That(_inventory.Slots, Is.Empty);
        Assert.That(_inventory.HasItem(null), Is.False);
    }

    [Test]
    public void AddItem_DoesNotCreateSlotWhenAmountIsNotPositive()
    {
        _inventory.AddItem(_throwItem, 0);
        _inventory.AddItem(_throwItem, -2);

        Assert.That(_inventory.Slots, Is.Empty);
        Assert.That(_inventory.HasItem(_throwItem), Is.False);
    }
}
