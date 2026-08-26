using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PushResolverTests
{
    private sealed class TestUnit : AbstractUnitsRunTime
    {
        private readonly bool _canBeArrested;

        protected override bool CanBeArrested => _canBeArrested;

        public TestUnit(HexCell position, bool canBeArrested = false)
            : base(position, UnitsStatus.Alive, 10, 4)
        {
            _canBeArrested = canBeArrested;
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
    private HexTypeSO _walkableType;
    private readonly List<ScriptableObject> _createdAssets = new();

    [SetUp]
    public void SetUp()
    {
        _walkableType = ScriptableObject.CreateInstance<HexTypeSO>();

        _createdAssets.Add(_walkableType);

        SerializedObject serializedType = new(_walkableType);

        serializedType.FindProperty("_isWalkable").boolValue = true;
        serializedType.ApplyModifiedPropertiesWithoutUndo();

        _mapData = ScriptableObject.CreateInstance<HexMapSO>();

        _createdAssets.Add(_mapData);
        _mapData.Initialize(8, 8, _walkableType);

        _gridObject = new GameObject("Push Test Grid");
        _grid = _gridObject.AddComponent<HexGrid>();

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
            Object.DestroyImmediate(asset);

        _createdAssets.Clear();
    }

    private HexCell Cell(int q, int r)
    {
        bool found = _grid.TryGetCell(new HexCoordinates(q, r), out HexCell cell);

        Assert.That(found, Is.True);
        return cell;
    }

    private PoliceRuntime CreatePolice(HexCell position)
    {
        PoliceSO data = ScriptableObject.CreateInstance<PoliceSO>();

        _createdAssets.Add(data);

        return new PoliceRuntime(position, UnitsStatus.Alive, data, morale: 10, actionPoint: 4);
    }

    [Test]
    public void Resolve_PushesSingleUnitBackward()
    {
        TestUnit pusher = new TestUnit(Cell(1, 1));
        TestUnit pushed = new TestUnit(Cell(2, 1));

        PushResolver.PushResult result = PushResolver.Resolve(pusher, pushed, _grid);

        Assert.That(result.IsResolved, Is.True);
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.Moves.Count, Is.EqualTo(1));
        Assert.That(pushed.PositionCell, Is.SameAs(Cell(3, 1)));
        Assert.That(Cell(2, 1).OccupiedBy, Is.Null);
        Assert.That(Cell(3, 1).OccupiedBy, Is.SameAs(pushed));
    }

    [Test]
    public void Resolve_PushesCompleteUnitChainBackward()
    {
        TestUnit pusher = new TestUnit(Cell(1, 1));
        TestUnit pushed = new TestUnit(Cell(2, 1));
        TestUnit blocker = new TestUnit(Cell(3, 1));

        PushResolver.PushResult result = PushResolver.Resolve(pusher, pushed, _grid);

        Assert.That(result.IsResolved, Is.True);
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.Moves.Count, Is.EqualTo(2));
        Assert.That(pushed.PositionCell, Is.SameAs(Cell(3, 1)));
        Assert.That(blocker.PositionCell, Is.SameAs(Cell(4, 1)));
    }

    [Test]
    public void Resolve_ReleasesBlockedChainSideways()
    {
        TestUnit pusher = new TestUnit(Cell(1, 1));
        TestUnit pushed = new TestUnit(Cell(2, 1));
        TestUnit blocker = new TestUnit(Cell(3, 1));

        TestUnit seatedStopper = new(Cell(4, 1));

        seatedStopper.SitDown();

        TestUnit sideBlocker = new(Cell(3, 2));

        PushResolver.PushResult result = PushResolver.Resolve(pusher, pushed, _grid);

        Assert.That(result.IsResolved, Is.True);
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.Moves.Count, Is.EqualTo(2));

        Assert.That(pushed.PositionCell, Is.SameAs(Cell(3, 1)));

        Assert.That(blocker.PositionCell, Is.SameAs(Cell(4, 0)));

        Assert.That(seatedStopper.PositionCell, Is.SameAs(Cell(4, 1)));

        Assert.That(sideBlocker.PositionCell, Is.SameAs(Cell(3, 2)));
    }

    [Test]
    public void Resolve_ArrestsUnitWhenPolicePushHasNoExit()
    {
        PoliceRuntime pusher = CreatePolice(Cell(6, 4));

        TestUnit pushed = new(Cell(7, 4), canBeArrested: true);

        PushResolver.PushResult result = PushResolver.Resolve(pusher, pushed, _grid);

        Assert.That(result.IsResolved, Is.True);
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.Moves, Is.Empty);
        Assert.That(pushed.Status, Is.EqualTo(UnitsStatus.Arrested));
        Assert.That(Cell(7, 4).OccupiedBy, Is.Null);
    }

    [Test]
    public void Resolve_ReturnsInvalidWhenUnitsAreNotAdjacent()
    {
        TestUnit pusher = new TestUnit(Cell(1, 1));
        TestUnit pushed = new TestUnit(Cell(3, 1));

        PushResolver.PushResult result = PushResolver.Resolve(pusher, pushed, _grid);

        Assert.That(result.IsResolved, Is.False);
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.Moves, Is.Empty);
        Assert.That(pusher.PositionCell, Is.SameAs(Cell(1, 1)));
        Assert.That(pushed.PositionCell, Is.SameAs(Cell(3, 1)));
    }
}
