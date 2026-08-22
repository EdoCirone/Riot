using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ChargeResolverTests
{
    private sealed class TestUnit : AbstractUnitsRunTime
    {
        private readonly bool _canCharge;

        public TestUnit(
            HexCell position,
            int actionPoints = 4,
            bool canCharge = true)
            : base(position, UnitsStatus.Alive, 10, actionPoints)
        {
            _canCharge = canCharge;
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
            return action != ActionType.Charge || _canCharge;
        }
    }

    private GameObject _gridObject;
    private HexGrid _grid;
    private HexMapSO _mapData;
    private HexTypeSO _walkableType;

    [SetUp]
    public void SetUp()
    {
        _walkableType = ScriptableObject.CreateInstance<HexTypeSO>();

        SerializedObject serializedType =
            new SerializedObject(_walkableType);

        serializedType.FindProperty("_isWalkable").boolValue = true;
        serializedType.ApplyModifiedPropertiesWithoutUndo();

        _mapData = ScriptableObject.CreateInstance<HexMapSO>();
        _mapData.Initialize(7, 7, _walkableType);

        _gridObject = new GameObject("Charge Test Grid");
        _grid = _gridObject.AddComponent<HexGrid>();

        SerializedObject serializedGrid = new SerializedObject(_grid);
        serializedGrid.FindProperty("_hexMapData").objectReferenceValue =
            _mapData;
        serializedGrid.ApplyModifiedPropertiesWithoutUndo();

        _grid.GenerateGrid();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gridObject);
        Object.DestroyImmediate(_mapData);
        Object.DestroyImmediate(_walkableType);
    }

    private HexCell Cell(int q, int r)
    {
        bool found = _grid.TryGetCell(
            new HexCoordinates(q, r),
            out HexCell cell
        );

        Assert.That(found, Is.True);
        return cell;
    }

    [Test]
    public void CanStart_ReturnsDestinationForValidCharge()
    {
        TestUnit attacker = new TestUnit(Cell(1, 1));
        TestUnit defender = new TestUnit(Cell(4, 1));

        bool result = ChargeResolver.CanStart(
            attacker,
            defender,
            _grid,
            out HexCell destination
        );

        Assert.That(result, Is.True);
        Assert.That(destination, Is.SameAs(Cell(3, 1)));
    }

    [Test]
    public void CanStart_ReturnsFalseWhenUnitsAreNotAligned()
    {
        TestUnit attacker = new TestUnit(Cell(1, 1));
        TestUnit defender = new TestUnit(Cell(4, 0));

        bool result = ChargeResolver.CanStart(
            attacker,
            defender,
            _grid,
            out HexCell destination
        );

        Assert.That(result, Is.False);
        Assert.That(destination, Is.Null);
    }

    [Test]
    public void CanStart_ReturnsFalseWhenRunUpIsBlocked()
    {
        TestUnit attacker = new TestUnit(Cell(1, 1));
        TestUnit blocker = new TestUnit(Cell(2, 1));
        TestUnit defender = new TestUnit(Cell(4, 1));

        bool result = ChargeResolver.CanStart(
            attacker,
            defender,
            _grid,
            out HexCell destination
        );

        Assert.That(result, Is.False);
        Assert.That(destination, Is.Null);
        Assert.That(blocker.PositionCell, Is.SameAs(Cell(2, 1)));
    }

    [Test]
    public void CanStart_ReturnsFalseWithInsufficientActionPoints()
    {
        TestUnit attacker =
            new TestUnit(Cell(1, 1), actionPoints: 3);

        TestUnit defender = new TestUnit(Cell(4, 1));

        bool result = ChargeResolver.CanStart(
            attacker,
            defender,
            _grid,
            out _
        );

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanStart_ReturnsFalseWhenChargeActionIsNotAllowed()
    {
        TestUnit attacker =
            new TestUnit(Cell(1, 1), canCharge: false);

        TestUnit defender = new TestUnit(Cell(4, 1));

        bool result = ChargeResolver.CanStart(
            attacker,
            defender,
            _grid,
            out _
        );

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanStart_ReturnsFalseAgainstSeatedDefender()
    {
        TestUnit attacker = new TestUnit(Cell(1, 1));
        TestUnit defender = new TestUnit(Cell(4, 1));
        defender.SitDown();

        bool result = ChargeResolver.CanStart(
            attacker,
            defender,
            _grid,
            out _
        );

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanStart_ReturnsFalseForMissingReferences()
    {
        TestUnit attacker = new TestUnit(Cell(1, 1));
        TestUnit defender = new TestUnit(Cell(4, 1));

        Assert.That(
            ChargeResolver.CanStart(null, defender, _grid, out _),
            Is.False
        );

        Assert.That(
            ChargeResolver.CanStart(attacker, null, _grid, out _),
            Is.False
        );

        Assert.That(
            ChargeResolver.CanStart(attacker, defender, null, out _),
            Is.False
        );
    }
}
