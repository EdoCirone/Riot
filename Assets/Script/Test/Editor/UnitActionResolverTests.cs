using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class UnitActionResolverTests
{
    private GameObject _gridObject;
    private HexGrid _grid;
    private HexMapSO _mapData;

    private SpezzoneSO _allowedData;
    private SpezzoneSO _blockedData;

    private readonly List<ScriptableObject> _createdAssets = new();

    [SetUp]
    public void SetUp()
    {
        _mapData = CreateAsset<HexMapSO>();
        _mapData.Initialize(6, 6, defaultType: null);

        _gridObject = new GameObject("Unit Action Test Grid");

        _grid = _gridObject.AddComponent<HexGrid>();

        SerializedObject serializedGrid = new(_grid);

        serializedGrid.FindProperty("_hexMapData").objectReferenceValue = _mapData;

        serializedGrid.ApplyModifiedPropertiesWithoutUndo();
        _grid.GenerateGrid();

        _allowedData = CreateAsset<SpezzoneSO>();

        ConfigureUnit(_allowedData, ActionType.Chant | ActionType.SitStand, defence: 2);

        _blockedData = CreateAsset<SpezzoneSO>();

        ConfigureUnit(_blockedData, ActionType.None, defence: 2);
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

    private static void ConfigureUnit(UnitsSO data, ActionType actions, int defence)
    {
        SerializedObject serialized = new(data);

        serialized.FindProperty("_allowedActions").intValue = (int)actions;

        serialized.FindProperty("_def").intValue = defence;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private HexCell Cell(int q, int r)
    {
        bool found = _grid.TryGetCell(new HexCoordinates(q, r), out HexCell cell);

        Assert.That(found, Is.True);
        return cell;
    }

    private SpezzoneRuntime CreateUnit(int q, int r, SpezzoneSO data, int actionPoints = 4)
    {
        return new SpezzoneRuntime(Cell(q, r), UnitsStatus.Alive, data, morale: 10, actionPoints);
    }

    [Test]
    public void ResolveChant_AffectsCasterAndAdjacentSpezzoni()
    {
        SpezzoneRuntime caster = CreateUnit(1, 1, _allowedData);

        SpezzoneRuntime adjacent = CreateUnit(2, 1, _allowedData);

        SpezzoneRuntime distant = CreateUnit(4, 1, _allowedData);

        caster.LoseMorale(2);
        adjacent.LoseMorale(2);
        distant.LoseMorale(2);

        caster.ApplyPanic(3);
        adjacent.ApplyPanic(3);
        distant.ApplyPanic(3);

        UnitActionResolver.UnitActionResult result = UnitActionResolver.ResolveChant(caster, _grid);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ActionPointCost, Is.EqualTo(3));
        Assert.That(result.AffectedUnits.Count, Is.EqualTo(2));
        Assert.That(result.AffectedUnits[0], Is.SameAs(caster));

        Assert.That(result.AffectedUnits[1], Is.SameAs(adjacent));

        Assert.That(caster.ActionPoints, Is.EqualTo(1));
        Assert.That(caster.Morale, Is.EqualTo(9));
        Assert.That(adjacent.Morale, Is.EqualTo(9));
        Assert.That(distant.Morale, Is.EqualTo(8));

        Assert.That(caster.IsPanicked, Is.False);
        Assert.That(adjacent.IsPanicked, Is.False);
        Assert.That(distant.IsPanicked, Is.True);
    }

    [Test]
    public void ResolveChant_InsufficientApDoesNotMutateState()
    {
        SpezzoneRuntime caster = CreateUnit(1, 1, _allowedData, actionPoints: 2);

        caster.LoseMorale(2);
        caster.ApplyPanic(3);

        UnitActionResolver.UnitActionResult result = UnitActionResolver.ResolveChant(caster, _grid);

        Assert.That(result.Succeeded, Is.False);

        Assert.That(result.Failure, Is.EqualTo(UnitActionFailure.InsufficientActionPoints));

        Assert.That(caster.ActionPoints, Is.EqualTo(2));
        Assert.That(caster.Morale, Is.EqualTo(8));
        Assert.That(caster.PanicTurnsLeft, Is.EqualTo(3));
    }

    [Test]
    public void ResolveChant_RejectsDisallowedAction()
    {
        SpezzoneRuntime caster = CreateUnit(1, 1, _blockedData);

        UnitActionResolver.UnitActionResult result = UnitActionResolver.ResolveChant(caster, _grid);

        Assert.That(result.Succeeded, Is.False);

        Assert.That(result.Failure, Is.EqualTo(UnitActionFailure.ActionNotAllowed));

        Assert.That(caster.ActionPoints, Is.EqualTo(4));
    }

    [Test]
    public void ResolveSitStand_SittingCostsOneApAndAddsDefence()
    {
        SpezzoneRuntime unit = CreateUnit(1, 1, _allowedData);

        UnitActionResolver.UnitActionResult result = UnitActionResolver.ResolveSitStand(unit);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.WasSeated, Is.False);
        Assert.That(result.ActionPointCost, Is.EqualTo(1));

        Assert.That(unit.IsSeated, Is.True);
        Assert.That(unit.ActionPoints, Is.EqualTo(3));
        Assert.That(unit.Def, Is.EqualTo(7));
    }

    [Test]
    public void ResolveSitStand_StandingCostsTwoApAndRemovesDefence()
    {
        SpezzoneRuntime unit = CreateUnit(1, 1, _allowedData);

        unit.SitDown();

        UnitActionResolver.UnitActionResult result = UnitActionResolver.ResolveSitStand(unit);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.WasSeated, Is.True);
        Assert.That(result.ActionPointCost, Is.EqualTo(2));

        Assert.That(unit.IsSeated, Is.False);
        Assert.That(unit.ActionPoints, Is.EqualTo(2));
        Assert.That(unit.Def, Is.EqualTo(2));
    }

    [Test]
    public void ResolveSitStand_InsufficientApPreservesState()
    {
        SpezzoneRuntime unit = CreateUnit(1, 1, _allowedData, actionPoints: 1);

        unit.SitDown();

        UnitActionResolver.UnitActionResult result = UnitActionResolver.ResolveSitStand(unit);

        Assert.That(result.Succeeded, Is.False);

        Assert.That(result.Failure, Is.EqualTo(UnitActionFailure.InsufficientActionPoints));

        Assert.That(result.ActionPointCost, Is.EqualTo(2));
        Assert.That(unit.IsSeated, Is.True);
        Assert.That(unit.ActionPoints, Is.EqualTo(1));
        Assert.That(unit.Def, Is.EqualTo(7));
    }

    [Test]
    public void ResolveSitStand_RejectsDisallowedAction()
    {
        SpezzoneRuntime unit = CreateUnit(1, 1, _blockedData);

        UnitActionResolver.UnitActionResult result = UnitActionResolver.ResolveSitStand(unit);

        Assert.That(result.Succeeded, Is.False);

        Assert.That(result.Failure, Is.EqualTo(UnitActionFailure.ActionNotAllowed));

        Assert.That(unit.IsSeated, Is.False);
        Assert.That(unit.ActionPoints, Is.EqualTo(4));
    }
}
