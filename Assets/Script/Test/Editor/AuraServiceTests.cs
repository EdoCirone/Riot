using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class AuraServiceTests
{
    private GameObject _gridObject;
    private HexGrid _grid;
    private HexMapSO _mapData;

    private SpezzoneSO _neutralSpezzone;
    private SpezzoneSO _auraSpezzone;
    private PoliceSO _neutralPolice;

    private readonly List<ScriptableObject> _createdAssets = new();

    [SetUp]
    public void SetUp()
    {
        _mapData = CreateAsset<HexMapSO>();
        _mapData.Initialize(6, 6, defaultType: null);

        _gridObject = new GameObject("Aura Test Grid");
        _grid = _gridObject.AddComponent<HexGrid>();

        SerializedObject serializedGrid = new(_grid);

        serializedGrid.FindProperty("_hexMapData").objectReferenceValue = _mapData;

        serializedGrid.ApplyModifiedPropertiesWithoutUndo();
        _grid.GenerateGrid();

        _neutralSpezzone = CreateAsset<SpezzoneSO>();
        ConfigureMoraleAura(_neutralSpezzone, 0);

        _auraSpezzone = CreateAsset<SpezzoneSO>();
        ConfigureMoraleAura(_auraSpezzone, 2);

        _neutralPolice = CreateAsset<PoliceSO>();
        ConfigureMoraleAura(_neutralPolice, 0);
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

    private static void ConfigureMoraleAura(UnitsSO data, int auraMorale)
    {
        SerializedObject serialized = new(data);

        serialized.FindProperty("_auraMor").intValue = auraMorale;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private HexCell Cell(int q, int r)
    {
        bool found = _grid.TryGetCell(new HexCoordinates(q, r), out HexCell cell);

        Assert.That(found, Is.True);
        return cell;
    }

    private SpezzoneRuntime CreateSpezzone(int q, int r, SpezzoneSO data, int morale = 10)
    {
        return new SpezzoneRuntime(Cell(q, r), UnitsStatus.Alive, data, morale, actionPoints: 4);
    }

    private PoliceRuntime CreatePolice(int q, int r, int morale = 10)
    {
        return new PoliceRuntime(Cell(q, r), UnitsStatus.Alive, _neutralPolice, morale, actionPoint: 4);
    }

    [Test]
    public void Resolve_ReturnsEmptyForMissingMap()
    {
        List<SpezzoneRuntime> spezzoni = new();
        List<PoliceRuntime> police = new();

        AuraService.AuraResult result = AuraService.Resolve(spezzoni, police, map: null);

        Assert.That(result.RemovedUnits, Is.Empty);
    }

    [Test]
    public void Resolve_AppliesAdjacentMoraleAuraToAlly()
    {
        SpezzoneRuntime donor = CreateSpezzone(1, 1, _auraSpezzone);

        SpezzoneRuntime recipient = CreateSpezzone(2, 1, _neutralSpezzone);

        List<SpezzoneRuntime> spezzoni = new()
        {
            donor,
            recipient
        };

        AuraService.AuraResult result = AuraService.Resolve(spezzoni, new List<PoliceRuntime>(), _grid);

        Assert.That(result.RemovedUnits, Is.Empty);
        Assert.That(recipient.Morale, Is.EqualTo(12));
        Assert.That(recipient.BaseMorale, Is.EqualTo(10));
        Assert.That(recipient.MaxMorale, Is.EqualTo(12));
    }

    [Test]
    public void Resolve_DoesNotShareAuraAcrossOpposingSides()
    {
        SpezzoneRuntime donor = CreateSpezzone(1, 1, _auraSpezzone);

        PoliceRuntime police = CreatePolice(2, 1);

        AuraService.Resolve(new List<SpezzoneRuntime> { donor }, new List<PoliceRuntime> { police }, _grid);

        Assert.That(donor.Morale, Is.EqualTo(10));
        Assert.That(police.Morale, Is.EqualTo(10));
    }

    [Test]
    public void Resolve_PanickedUnitDoesNotReceiveAura()
    {
        SpezzoneRuntime donor = CreateSpezzone(1, 1, _auraSpezzone);

        SpezzoneRuntime recipient = CreateSpezzone(2, 1, _neutralSpezzone);

        recipient.ApplyPanic(3);

        AuraService.Resolve(
            new List<SpezzoneRuntime>
            {
                donor,
                recipient
            },
            new List<PoliceRuntime>(),
            _grid
        );

        Assert.That(recipient.Morale, Is.EqualTo(10));
        Assert.That(recipient.MaxMorale, Is.EqualTo(10));
    }

    [Test]
    public void Resolve_RepeatsUntilCascadeIsStable()
    {
        SpezzoneRuntime first = CreateSpezzone(1, 1, _auraSpezzone, morale: 1);

        SpezzoneRuntime middle = CreateSpezzone(2, 1, _auraSpezzone, morale: 1);

        SpezzoneRuntime tail = CreateSpezzone(3, 1, _neutralSpezzone, morale: 1);

        List<SpezzoneRuntime> spezzoni = new()
        {
            first,
            middle,
            tail
        };

        AuraService.Resolve(spezzoni, new List<PoliceRuntime>(), _grid);

        Assert.That(first.Morale, Is.EqualTo(3));
        Assert.That(middle.Morale, Is.EqualTo(3));
        Assert.That(tail.Morale, Is.EqualTo(3));

        middle.LoseMorale(2);
        tail.LoseMorale(2);
        first.LoseMorale(3);

        AuraService.AuraResult result = AuraService.Resolve(spezzoni, new List<PoliceRuntime>(), _grid);

        Assert.That(result.RemovedUnits.Count, Is.EqualTo(2));
        Assert.That(result.RemovedUnits, Does.Contain(middle));
        Assert.That(result.RemovedUnits, Does.Contain(tail));

        Assert.That(middle.Status, Is.EqualTo(UnitsStatus.Disperse));

        Assert.That(tail.Status, Is.EqualTo(UnitsStatus.Disperse));

        Assert.That(Cell(2, 1).OccupiedBy, Is.Null);
        Assert.That(Cell(3, 1).OccupiedBy, Is.Null);
    }
}
