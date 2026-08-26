using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ItemActionResolverTests
{
    private GameObject _gridObject;
    private HexGrid _grid;

    private HexMapSO _mapData;
    private HexTypeSO _walkableType;
    private SpezzoneSO _spezzoneData;
    private PoliceSO _policeData;
    private ThrowItemSO _throwItem;
    private BarricadeSO _barricadeItem;

    private readonly List<ScriptableObject> _createdAssets = new();

    [SetUp]
    public void SetUp()
    {
        _walkableType = CreateAsset<HexTypeSO>();

        SerializedObject serializedType = new(_walkableType);

        serializedType.FindProperty("_isWalkable").boolValue = true;
        serializedType.ApplyModifiedPropertiesWithoutUndo();

        _mapData = CreateAsset<HexMapSO>();
        _mapData.Initialize(6, 6, _walkableType);

        _gridObject = new GameObject("Item Action Test Grid");
        _grid = _gridObject.AddComponent<HexGrid>();

        SerializedObject serializedGrid = new(_grid);

        serializedGrid.FindProperty("_hexMapData").objectReferenceValue = _mapData;

        serializedGrid.ApplyModifiedPropertiesWithoutUndo();
        _grid.GenerateGrid();

        _spezzoneData = CreateAsset<SpezzoneSO>();
        ConfigureActions(_spezzoneData, ActionType.Throw | ActionType.Barricade);

        _policeData = CreateAsset<PoliceSO>();

        _throwItem = CreateAsset<ThrowItemSO>();
        ConfigureThrowItem(_throwItem, actionPointCost: 2, moraleLost: 2);

        _barricadeItem = CreateAsset<BarricadeSO>();
        ConfigureItem(_barricadeItem, actionPointCost: 2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gridObject);

        foreach (ScriptableObject asset in _createdAssets)
            Object.DestroyImmediate(asset);

        _createdAssets.Clear();
    }

    private T CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        _createdAssets.Add(asset);
        return asset;
    }

    private static void ConfigureActions(UnitsSO data, ActionType actions)
    {
        SerializedObject serialized = new(data);

        serialized.FindProperty("_allowedActions").intValue = (int)actions;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureItem(ItemSO item, int actionPointCost)
    {
        SerializedObject serialized = new(item);

        serialized.FindProperty("_actionPointCost").intValue = actionPointCost;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureThrowItem(ThrowItemSO item, int actionPointCost, int moraleLost)
    {
        SerializedObject serialized = new(item);

        serialized.FindProperty("_actionPointCost").intValue = actionPointCost;

        serialized.FindProperty("_moralLost").intValue = moraleLost;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private HexCell Cell(int q, int r)
    {
        bool found = _grid.TryGetCell(new HexCoordinates(q, r), out HexCell cell);

        Assert.That(found, Is.True);
        return cell;
    }

    private SpezzoneRuntime CreateActor(HexCell position, int actionPoints = 4)
    {
        return new SpezzoneRuntime(position, UnitsStatus.Alive, _spezzoneData, morale: 10, actionPoints);
    }

    private PoliceRuntime CreatePolice(HexCell position, int morale = 5)
    {
        return new PoliceRuntime(position, UnitsStatus.Alive, _policeData, morale, actionPoint: 4);
    }

    [Test]
    public void ResolveThrow_SpendsOneItemAndAppliesMoraleDamage()
    {
        SpezzoneRuntime actor = CreateActor(Cell(1, 1));
        PoliceRuntime target = CreatePolice(Cell(3, 1));

        actor.Inventory.AddItem(_throwItem, 2);

        ItemActionResolver.ItemActionResult result = ItemActionResolver.ResolveThrow(actor, target, _throwItem, _grid);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Failure, Is.EqualTo(ItemActionFailure.None));
        Assert.That(actor.ActionPoints, Is.EqualTo(2));
        Assert.That(actor.Inventory.Slots.Count, Is.EqualTo(1));
        Assert.That(actor.Inventory.Slots[0].Quantity, Is.EqualTo(1));
        Assert.That(target.Morale, Is.EqualTo(3));
    }

    [Test]
    public void ResolveThrow_DoesNotMutateStateWhenItemIsMissing()
    {
        SpezzoneRuntime actor = CreateActor(Cell(1, 1));
        PoliceRuntime target = CreatePolice(Cell(3, 1));

        ItemActionResolver.ItemActionResult result = ItemActionResolver.ResolveThrow(actor, target, _throwItem, _grid);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.EqualTo(ItemActionFailure.MissingItem));

        Assert.That(actor.ActionPoints, Is.EqualTo(4));
        Assert.That(target.Morale, Is.EqualTo(5));
    }

    [Test]
    public void ResolveThrow_RejectsTargetAtInvalidDistance()
    {
        SpezzoneRuntime actor = CreateActor(Cell(1, 1));
        PoliceRuntime target = CreatePolice(Cell(2, 1));

        actor.Inventory.AddItem(_throwItem);

        ItemActionResolver.ItemActionResult result = ItemActionResolver.ResolveThrow(actor, target, _throwItem, _grid);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.EqualTo(ItemActionFailure.InvalidTarget));

        Assert.That(actor.ActionPoints, Is.EqualTo(4));
        Assert.That(actor.Inventory.HasItem(_throwItem), Is.True);
        Assert.That(target.Morale, Is.EqualTo(5));
    }

    [Test]
    public void ResolveBarricade_ConsumesExactlyOneItem()
    {
        SpezzoneRuntime actor = CreateActor(Cell(1, 1));
        HexCell target = Cell(2, 1);

        actor.Inventory.AddItem(_barricadeItem, 2);

        ItemActionResolver.ItemActionResult result = ItemActionResolver.ResolveBarricade(actor, target, _barricadeItem);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(actor.ActionPoints, Is.EqualTo(2));
        Assert.That(actor.Inventory.Slots.Count, Is.EqualTo(1));
        Assert.That(actor.Inventory.Slots[0].Quantity, Is.EqualTo(1));
        Assert.That(target.Barricade, Is.SameAs(result.PlacedBarricade));
    }

    [Test]
    public void ResolveBarricade_DoesNotSpendResourcesOnOccupiedCell()
    {
        SpezzoneRuntime actor = CreateActor(Cell(1, 1));
        HexCell target = Cell(2, 1);

        CreatePolice(target);
        actor.Inventory.AddItem(_barricadeItem);

        ItemActionResolver.ItemActionResult result = ItemActionResolver.ResolveBarricade(actor, target, _barricadeItem);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.EqualTo(ItemActionFailure.InvalidTarget));

        Assert.That(actor.ActionPoints, Is.EqualTo(4));
        Assert.That(actor.Inventory.HasItem(_barricadeItem), Is.True);
        Assert.That(target.Barricade, Is.Null);
    }

    [Test]
    public void ResolveBarricade_RejectsInsufficientActionPoints()
    {
        SpezzoneRuntime actor = CreateActor(Cell(1, 1), actionPoints: 1);

        HexCell target = Cell(2, 1);
        actor.Inventory.AddItem(_barricadeItem);

        ItemActionResolver.ItemActionResult result = ItemActionResolver.ResolveBarricade(actor, target, _barricadeItem);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.EqualTo(ItemActionFailure.InsufficientActionPoints));

        Assert.That(actor.ActionPoints, Is.EqualTo(1));
        Assert.That(actor.Inventory.HasItem(_barricadeItem), Is.True);
        Assert.That(target.Barricade, Is.Null);
    }
}
