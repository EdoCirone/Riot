using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PanicWaveTests
{
    private sealed class TestUnit : AbstractUnitsRunTime
    {
        public TestUnit(HexCell position)
            : base(position, UnitsStatus.Alive, 10, 4)
        {
            position.TryOccupy(this);
        }

        public override string DisplayName => "Test Unit";
        public override Sprite Avatar => null;
        public override int Atk => 0;
        public override int Def => 0;
        public override int AuraAtk => 0;
        public override int AuraDef => 0;
        public override int AuraMor => 0;
        public override GameObject GraphicsPrefab => null;

        public override bool CanPerformAction(ActionType action)
        {
            return true;
        }
    }

    private GameObject _gridObject;
    private HexGrid _grid;
    private HexMapSO _mapData;
    private readonly List<ScriptableObject> _createdAssets = new();

    [SetUp]
    public void SetUp()
    {
        _gridObject = new GameObject("Test Grid");
        _grid = _gridObject.AddComponent<HexGrid>();

        _mapData = ScriptableObject.CreateInstance<HexMapSO>();
        _mapData.Initialize(width: 5, height: 5, defaultType: null);
        _createdAssets.Add(_mapData);

        SerializedObject serializedGrid = new(_grid);
        serializedGrid.FindProperty("_hexMapData").objectReferenceValue = _mapData;
        serializedGrid.ApplyModifiedPropertiesWithoutUndo();

        _grid.GenerateGrid();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gridObject);

        foreach (ScriptableObject asset in _createdAssets)
        {
            Object.DestroyImmediate(asset);
        }

        _createdAssets.Clear();
    }

    private HexCell Cell(int q, int r)
    {
        bool found = _grid.TryGetCell(new HexCoordinates(q, r), out HexCell cell);

        Assert.That(found, Is.True, $"La cella ({q}, {r}) non esiste nella mappa di test");

        return cell;
    }

    private static int? StepsOf(List<(AbstractUnitsRunTime unit, int steps)> wave, AbstractUnitsRunTime target)
    {
        foreach ((AbstractUnitsRunTime unit, int steps) entry in wave)
        {
            if (ReferenceEquals(entry.unit, target))
                return entry.steps;
        }

        return null;
    }

    private PoliceRuntime CreatePolice(HexCell position)
    {
        PoliceSO data = ScriptableObject.CreateInstance<PoliceSO>();
        _createdAssets.Add(data);

        return new PoliceRuntime(position, UnitsStatus.Alive, data, morale: 10, actionPoint: 4);
    }

    [Test]
    public void GetPanicWave_ReturnsEmptyForInvalidInputs()
    {
        List<(AbstractUnitsRunTime unit, int steps)> missingOrigin =
            TacticalQuery.GetPanicWave(null, new TestUnit(Cell(1, 1)), _grid);

        List<(AbstractUnitsRunTime unit, int steps)> missingEpicentre =
            TacticalQuery.GetPanicWave(Cell(1, 1), null, _grid);

        List<(AbstractUnitsRunTime unit, int steps)> missingMap =
            TacticalQuery.GetPanicWave(
                Cell(1, 1),
                Cell(1, 1).OccupiedBy,
                null
            );

        Assert.That(missingOrigin, Is.Empty);
        Assert.That(missingEpicentre, Is.Empty);
        Assert.That(missingMap, Is.Empty);
    }

    [Test]
    public void GetPanicWave_PropagatesThroughConnectedAllies()
    {
        TestUnit epicentre = new TestUnit(Cell(1, 1));
        TestUnit first = new TestUnit(Cell(2, 1));
        TestUnit second = new TestUnit(Cell(3, 1));

        List<(AbstractUnitsRunTime unit, int steps)> wave =
            TacticalQuery.GetPanicWave(
                epicentre.PositionCell,
                epicentre,
                _grid
            );

        Assert.That(wave.Count, Is.EqualTo(3));
        Assert.That(StepsOf(wave, epicentre), Is.EqualTo(0));
        Assert.That(StepsOf(wave, first), Is.EqualTo(1));
        Assert.That(StepsOf(wave, second), Is.EqualTo(2));
    }

    [Test]
    public void GetPanicWave_DoesNotPropagateBeyondMaximumSteps()
    {
        TestUnit epicentre = new TestUnit(Cell(1, 1));
        TestUnit first = new TestUnit(Cell(2, 1));
        TestUnit second = new TestUnit(Cell(3, 1));
        TestUnit beyondRange = new TestUnit(Cell(4, 1));

        List<(AbstractUnitsRunTime unit, int steps)> wave =
            TacticalQuery.GetPanicWave(
                epicentre.PositionCell,
                epicentre,
                _grid
            );

        Assert.That(StepsOf(wave, epicentre), Is.EqualTo(0));
        Assert.That(StepsOf(wave, first), Is.EqualTo(1));
        Assert.That(StepsOf(wave, second), Is.EqualTo(2));
        Assert.That(StepsOf(wave, beyondRange), Is.Null);
    }

    [Test]
    public void GetPanicWave_SeatedUnitStopsPropagation()
    {
        TestUnit epicentre = new TestUnit(Cell(1, 1));
        TestUnit seated = new TestUnit(Cell(2, 1));
        TestUnit behind = new TestUnit(Cell(3, 1));

        seated.SitDown();

        List<(AbstractUnitsRunTime unit, int steps)> wave =
            TacticalQuery.GetPanicWave(
                epicentre.PositionCell,
                epicentre,
                _grid
            );

        Assert.That(wave.Count, Is.EqualTo(1));
        Assert.That(StepsOf(wave, seated), Is.Null);
        Assert.That(StepsOf(wave, behind), Is.Null);
    }

    [Test]
    public void GetPanicWave_OpposingUnitStopsPropagation()
    {
        TestUnit epicentre = new TestUnit(Cell(1, 1));
        PoliceRuntime police = CreatePolice(Cell(2, 1));
        TestUnit behind = new TestUnit(Cell(3, 1));

        List<(AbstractUnitsRunTime unit, int steps)> wave =
            TacticalQuery.GetPanicWave(
                epicentre.PositionCell,
                epicentre,
                _grid
            );

        Assert.That(wave.Count, Is.EqualTo(1));
        Assert.That(StepsOf(wave, police), Is.Null);
        Assert.That(StepsOf(wave, behind), Is.Null);
    }

    [Test]
    public void GetPanicWave_RemovedEpicentreIsExcludedButWaveStillStarts()
    {
        HexCell origin = Cell(1, 1);
        TestUnit epicentre = new TestUnit(origin);
        TestUnit adjacent = new TestUnit(Cell(2, 1));

        epicentre.LoseMorale(10, MoraleLossCause.Other);

        List<(AbstractUnitsRunTime unit, int steps)> wave = TacticalQuery.GetPanicWave(origin, epicentre, _grid);

        Assert.That(StepsOf(wave, epicentre), Is.Null);
        Assert.That(StepsOf(wave, adjacent), Is.EqualTo(1));
        Assert.That(wave.Count, Is.EqualTo(1));
    }

    [Test]
    public void GetPanicWave_DoesNotMutateUnitState()
    {
        TestUnit epicentre = new TestUnit(Cell(1, 1));
        TestUnit adjacent = new TestUnit(Cell(2, 1));

        TacticalQuery.GetPanicWave(epicentre.PositionCell, epicentre, _grid);

        Assert.That(epicentre.IsPanicked, Is.False);
        Assert.That(adjacent.IsPanicked, Is.False);
        Assert.That(epicentre.PanicTurnsLeft, Is.Zero);
        Assert.That(adjacent.PanicTurnsLeft, Is.Zero);
    }

    [Test]
    public void Resolve_AppliesCorteoPanicDurationByDistance()
    {
        TestUnit epicentre = new TestUnit(Cell(1, 1));
        TestUnit first = new TestUnit(Cell(2, 1));
        TestUnit second = new TestUnit(Cell(3, 1));

        IReadOnlyList<PanicResolver.PanicEffect> effects =
            PanicResolver.Resolve(
                epicentre.PositionCell,
                epicentre,
                _grid
            );

        Assert.That(effects.Count, Is.EqualTo(3));
        Assert.That(epicentre.PanicTurnsLeft, Is.EqualTo(3));
        Assert.That(first.PanicTurnsLeft, Is.EqualTo(2));
        Assert.That(second.PanicTurnsLeft, Is.EqualTo(1));
    }

    [Test]
    public void Resolve_PolicePanicNeverFallsBelowOneTurn()
    {
        PoliceRuntime epicentre = CreatePolice(Cell(1, 1));
        PoliceRuntime adjacent = CreatePolice(Cell(2, 1));

        IReadOnlyList<PanicResolver.PanicEffect> effects =
            PanicResolver.Resolve(
                epicentre.PositionCell,
                epicentre,
                _grid
            );

        Assert.That(effects.Count, Is.EqualTo(2));
        Assert.That(epicentre.PanicTurnsLeft, Is.EqualTo(1));
        Assert.That(adjacent.PanicTurnsLeft, Is.EqualTo(1));
    }

    [Test]
    public void Resolve_DoesNotReplaceLongerExistingPanic()
    {
        TestUnit epicentre = new TestUnit(Cell(1, 1));
        TestUnit adjacent = new TestUnit(Cell(2, 1));

        adjacent.ApplyPanic(5);

        PanicResolver.Resolve(epicentre.PositionCell, epicentre, _grid);

        Assert.That(epicentre.PanicTurnsLeft, Is.EqualTo(3));
        Assert.That(adjacent.PanicTurnsLeft, Is.EqualTo(5));
    }
}
