using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PoliceReturnCoordinatorTests
{
    private PoliceSO _policeData;
    private HexTypeSO _walkableType;
    private HexTypeSO _stationType;
    private HexMapSO _mapData;

    private GameObject _gridObject;
    private HexGrid _grid;

    [SetUp]
    public void SetUp()
    {
        _policeData =
            ScriptableObject.CreateInstance<PoliceSO>();

        _walkableType =
            ScriptableObject.CreateInstance<HexTypeSO>();

        _stationType =
            ScriptableObject.CreateInstance<HexTypeSO>();

        ConfigureHexType(_walkableType, isStation: false);
        ConfigureHexType(_stationType, isStation: true);

        _mapData =
            ScriptableObject.CreateInstance<HexMapSO>();

        _mapData.Initialize(
            width: 7,
            height: 7,
            defaultType: _walkableType
        );

        _mapData.SetCellType(
            col: 3,
            row: 3,
            type: _stationType
        );

        _gridObject =
            new GameObject("Police Return Test Grid");

        _grid = _gridObject.AddComponent<HexGrid>();

        SerializedObject serializedGrid = new(_grid);

        serializedGrid
            .FindProperty("_hexMapData")
            .objectReferenceValue = _mapData;

        serializedGrid.ApplyModifiedPropertiesWithoutUndo();

        _grid.GenerateGrid();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gridObject);
        Object.DestroyImmediate(_policeData);
        Object.DestroyImmediate(_walkableType);
        Object.DestroyImmediate(_stationType);
        Object.DestroyImmediate(_mapData);
    }

    private static void ConfigureHexType(
        HexTypeSO type,
        bool isStation)
    {
        SerializedObject serializedType = new(type);

        serializedType
            .FindProperty("_isWalkable")
            .boolValue = true;

        serializedType
            .FindProperty("_isPoliceStation")
            .boolValue = isStation;

        serializedType.ApplyModifiedPropertiesWithoutUndo();
    }

    private HexCell GridCell(int q, int r)
    {
        bool found = _grid.TryGetCell(
            new HexCoordinates(q, r),
            out HexCell cell
        );

        Assert.That(found, Is.True);
        return cell;
    }

    private PoliceRuntime CreatePolice(HexCell position)
    {
        return new PoliceRuntime(
            position,
            UnitsStatus.Alive,
            _policeData,
            morale: 10,
            actionPoint: 4
        );
    }

    [Test]
    public void ProcessTurnStart_NewlyDispersedPoliceWaitsOneFullTurn()
    {
        PoliceReturnCoordinator coordinator = new();

        PoliceRuntime police =
            CreatePolice(GridCell(0, 0));

        police.LoseMorale(
            police.Morale,
            MoraleLossCause.Other
        );

        Assert.That(
            police.Status,
            Is.EqualTo(UnitsStatus.Disperse)
        );

        var firstTurnReturns =
            coordinator.ProcessTurnStart(
                new[] { police },
                _grid
            );

        Assert.That(firstTurnReturns, Is.Empty);
        Assert.That(
            police.Status,
            Is.EqualTo(UnitsStatus.Disperse)
        );

        var secondTurnReturns =
            coordinator.ProcessTurnStart(
                new[] { police },
                _grid
            );

        Assert.That(secondTurnReturns, Has.Count.EqualTo(1));
        Assert.That(secondTurnReturns[0], Is.SameAs(police));
        Assert.That(police.Status, Is.EqualTo(UnitsStatus.Alive));

        Assert.That(
            _grid.PoliceStations[0].OccupiedBy,
            Is.SameAs(police)
        );
    }
    [Test]
    public void ProcessTurnStart_MultiplePoliceReturnInSameTurn()
    {
        PoliceReturnCoordinator coordinator = new();

        PoliceRuntime firstPolice =
            CreatePolice(GridCell(0, 0));

        PoliceRuntime secondPolice =
            CreatePolice(GridCell(0, 1));

        firstPolice.LoseMorale(
            firstPolice.Morale,
            MoraleLossCause.Other
        );

        secondPolice.LoseMorale(
            secondPolice.Morale,
            MoraleLossCause.Other
        );

        PoliceRuntime[] policeUnits =
        {
        firstPolice,
        secondPolice
    };

        var firstTurnReturns =
            coordinator.ProcessTurnStart(policeUnits, _grid);

        Assert.That(firstTurnReturns, Is.Empty);

        var secondTurnReturns =
            coordinator.ProcessTurnStart(policeUnits, _grid);

        HexCell station = _grid.PoliceStations[0];

        Assert.That(secondTurnReturns, Has.Count.EqualTo(2));
        Assert.That(firstPolice.Status, Is.EqualTo(UnitsStatus.Alive));
        Assert.That(secondPolice.Status, Is.EqualTo(UnitsStatus.Alive));

        Assert.That(firstPolice.PositionCell, Is.SameAs(station));

        Assert.That(
            secondPolice.PositionCell,
            Is.Not.SameAs(firstPolice.PositionCell)
        );

        Assert.That(
            secondPolice.PositionCell.Coordinates.Distance(
                station.Coordinates
            ),
            Is.EqualTo(1)
        );
    }
    [Test]
    public void ProcessTurnStart_WhenStationAreaIsBlocked_RetriesNextTurn()
    {
        PoliceReturnCoordinator coordinator = new();

        HexCell station = _grid.PoliceStations[0];

        PoliceRuntime stationBlocker =
            CreatePolice(station);

        HexCoordinates[] neighbors =
            station.Coordinates.GetNeighbors();

        PoliceRuntime[] adjacentBlockers =
            new PoliceRuntime[neighbors.Length];

        for (int i = 0; i < neighbors.Length; i++)
        {
            bool found = _grid.TryGetCell(
                neighbors[i],
                out HexCell adjacent
            );

            Assert.That(found, Is.True);

            adjacentBlockers[i] =
                CreatePolice(adjacent);
        }

        PoliceRuntime returningPolice =
            CreatePolice(GridCell(0, 0));

        returningPolice.LoseMorale(
            returningPolice.Morale,
            MoraleLossCause.Other
        );

        PoliceRuntime[] returningUnits =
        {
        returningPolice
    };

        coordinator.ProcessTurnStart(
            returningUnits,
            _grid
        );

        var blockedTurnReturns =
            coordinator.ProcessTurnStart(
                returningUnits,
                _grid
            );

        Assert.That(blockedTurnReturns, Is.Empty);
        Assert.That(
            returningPolice.Status,
            Is.EqualTo(UnitsStatus.Disperse)
        );

        adjacentBlockers[0].RemoveFromBoard(
            MoraleLossCause.Other
        );

        var retryReturns =
            coordinator.ProcessTurnStart(
                returningUnits,
                _grid
            );

        Assert.That(retryReturns, Has.Count.EqualTo(1));
        Assert.That(retryReturns[0], Is.SameAs(returningPolice));
        Assert.That(
            returningPolice.PositionCell.Coordinates,
            Is.EqualTo(neighbors[0])
        );

        Assert.That(
            station.OccupiedBy,
            Is.SameAs(stationBlocker)
        );
    }
}
