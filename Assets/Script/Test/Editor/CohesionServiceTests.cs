using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CohesionServiceTests
{
    private GameObject _gridObject;
    private HexGrid _grid;
    private HexMapSO _mapData;
    private SpezzoneSO _spezzoneData;

    [SetUp]
    public void SetUp()
    {
        _mapData = ScriptableObject.CreateInstance<HexMapSO>();
        _mapData.Initialize(6, 6, defaultType: null);

        _gridObject = new GameObject("Cohesion Test Grid");
        _grid = _gridObject.AddComponent<HexGrid>();

        SerializedObject serializedGrid = new(_grid);

        serializedGrid.FindProperty("_hexMapData").objectReferenceValue = _mapData;

        serializedGrid.ApplyModifiedPropertiesWithoutUndo();
        _grid.GenerateGrid();

        _spezzoneData = ScriptableObject.CreateInstance<SpezzoneSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gridObject);
        Object.DestroyImmediate(_mapData);
        Object.DestroyImmediate(_spezzoneData);
    }

    private HexCell Cell(int q, int r)
    {
        bool found = _grid.TryGetCell(new HexCoordinates(q, r), out HexCell cell);

        Assert.That(found, Is.True);
        return cell;
    }

    private SpezzoneRuntime CreateUnit(int q, int r)
    {
        return new SpezzoneRuntime(Cell(q, r), UnitsStatus.Alive, _spezzoneData, morale: 10, actionPoints: 4);
    }

    [Test]
    public void Calculate_ReturnsZeroForMissingDependencies()
    {
        List<SpezzoneRuntime> units = new();

        Assert.That(CohesionService.Calculate(null, _grid), Is.Zero);

        Assert.That(CohesionService.Calculate(units, null), Is.Zero);
    }

    [Test]
    public void Calculate_ReturnsZeroForEmptyCorteo()
    {
        List<SpezzoneRuntime> units = new();

        int cohesion = CohesionService.Calculate(units, _grid);

        Assert.That(cohesion, Is.Zero);
    }

    [Test]
    public void Calculate_ReturnsZeroForSingleUnit()
    {
        List<SpezzoneRuntime> units = new()
        {
            CreateUnit(1, 1)
        };

        int cohesion = CohesionService.Calculate(units, _grid);

        Assert.That(cohesion, Is.Zero);
    }

    [Test]
    public void Calculate_CountsAdjacentPairInBothDirections()
    {
        List<SpezzoneRuntime> units = new()
        {
            CreateUnit(1, 1),
            CreateUnit(2, 1)
        };

        int cohesion = CohesionService.Calculate(units, _grid);

        Assert.That(cohesion, Is.EqualTo(20));
    }

    [Test]
    public void Calculate_CountsAllLinksInConnectedChain()
    {
        List<SpezzoneRuntime> units = new()
        {
            CreateUnit(1, 1),
            CreateUnit(2, 1),
            CreateUnit(3, 1)
        };

        int cohesion = CohesionService.Calculate(units, _grid);

        Assert.That(cohesion, Is.EqualTo(40));
    }

    [Test]
    public void Calculate_IgnoresRemovedUnits()
    {
        SpezzoneRuntime alive = CreateUnit(1, 1);
        SpezzoneRuntime removed = CreateUnit(2, 1);

        List<SpezzoneRuntime> units = new()
        {
            alive,
            removed
        };

        removed.LoseMorale(10, MoraleLossCause.Other);

        int cohesion = CohesionService.Calculate(units, _grid);

        Assert.That(cohesion, Is.Zero);
    }
}
