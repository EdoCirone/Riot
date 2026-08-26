using System.Collections.Generic;
using NUnit.Framework;

public class TacticalQueryTests
{
    [Test]
    public void GetAttackOption_ReturnsInvalidWhenBudgetIsZero()
    {
        HexCoordinates from = new(0, 0);
        HexCoordinates target = new(1, 0);

        TacticalQuery.AttackOption option = TacticalQuery.GetAttackOption(from, target, budget: 0, map: null);

        Assert.That(option.IsValid, Is.False);
    }

    [Test]
    public void GetAttackOption_AdjacentTargetRequiresNoMovement()
    {
        HexCoordinates from = new(0, 0);
        HexCoordinates target = new(1, 0);

        TacticalQuery.AttackOption option = TacticalQuery.GetAttackOption(from, target, budget: 1, map: null);

        Assert.That(option.IsValid, Is.True);
        Assert.That(option.RequiresMovement, Is.False);
        Assert.That(option.MoveCost, Is.Zero);
    }

    [Test]
    public void GetAttackOption_NonAdjacentTargetUsesReachableNeighbor()
    {
        HexCoordinates from = new(0, 0);
        HexCoordinates target = new(2, 0);
        HexCoordinates reachableNeighbor = new(1, 0);

        Dictionary<HexCoordinates, int> visited = new()
        {
            [reachableNeighbor] = 1
        };

        TacticalQuery.AttackOption option =
            TacticalQuery.GetAttackOption(
                from,
                target,
                budget: 2,
                map: null,
                precomputedVisited: visited
            );

        Assert.That(option.IsValid, Is.True);
        Assert.That(option.RequiresMovement, Is.True);
        Assert.That(option.MoveDestination, Is.EqualTo(reachableNeighbor));
        Assert.That(option.MoveCost, Is.EqualTo(1));
    }

    [Test]
    public void GetAttackOption_ReturnsInvalidWhenMovementAndAttackExceedBudget()
    {
        HexCoordinates from = new(0, 0);
        HexCoordinates target = new(2, 0);
        HexCoordinates reachableNeighbor = new(1, 0);

        Dictionary<HexCoordinates, int> visited = new()
        {
            [reachableNeighbor] = 2
        };

        TacticalQuery.AttackOption option =
            TacticalQuery.GetAttackOption(
                from,
                target,
                budget: 2,
                map: null,
                precomputedVisited: visited
            );

        Assert.That(option.IsValid, Is.False);
    }

    [Test]
    public void GetAttackOption_SelectsLowestCostReachableNeighbor()
    {
        HexCoordinates from = new(0, 0);
        HexCoordinates target = new(2, 0);

        HexCoordinates expensiveNeighbor = new(2, -1);
        HexCoordinates cheapestNeighbor = new(1, 0);

        Dictionary<HexCoordinates, int> visited = new()
        {
            [expensiveNeighbor] = 2,
            [cheapestNeighbor] = 1
        };

        TacticalQuery.AttackOption option =
            TacticalQuery.GetAttackOption(
                from,
                target,
                budget: 3,
                map: null,
                precomputedVisited: visited
            );

        Assert.That(option.IsValid, Is.True);
        Assert.That(option.RequiresMovement, Is.True);
        Assert.That(option.MoveDestination, Is.EqualTo(cheapestNeighbor));
        Assert.That(option.MoveCost, Is.EqualTo(1));
    }
}
