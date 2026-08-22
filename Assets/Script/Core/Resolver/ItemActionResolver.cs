
public static class ItemActionResolver
{
    public sealed class ItemActionResult
    {
        public bool Succeeded { get; }
        public ItemActionFailure Failure { get; }
        public BarricadeRuntime PlacedBarricade { get; }

        private ItemActionResult(
            bool succeeded,
            ItemActionFailure failure,
            BarricadeRuntime placedBarricade = null)
        {
            Succeeded = succeeded;
            Failure = failure;
            PlacedBarricade = placedBarricade;
        }

        public static ItemActionResult Success(
            BarricadeRuntime placedBarricade = null)
        {
            return new ItemActionResult(
                true,
                ItemActionFailure.None,
                placedBarricade
            );
        }

        public static ItemActionResult Fail(
            ItemActionFailure failure)
        {
            return new ItemActionResult(false, failure);
        }
    }

    public static ItemActionResult ResolveThrow(
        SpezzoneRuntime actor,
        PoliceRuntime target,
        ThrowItemSO item,
        HexGrid map)
    {
        if (actor == null || !actor.IsAlive
            || actor.PositionCell == null)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidActor
            );
        }

        if (item == null)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidItem
            );
        }

        if (target == null || map == null)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidTarget
            );
        }

        if (!actor.CanPerformAction(ActionType.Throw))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.ActionNotAllowed
            );
        }

        if (!actor.Inventory.HasItem(item))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.MissingItem
            );
        }

        if (actor.ActionPoints < item.ActionPointCost)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InsufficientActionPoints
            );
        }

        if (!TacticalQuery.CanThrow(
                actor,
                target.PositionCell,
                item,
                map))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidTarget
            );
        }

        if (!actor.TrySpendActionPoint(item.ActionPointCost))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.ResolutionFailed
            );
        }

        if (!actor.Inventory.ConsumeItem(item))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.ResolutionFailed
            );
        }

        target.LoseMorale(item.MoralLost);

        return ItemActionResult.Success();
    }

    public static ItemActionResult ResolveBarricade(
        SpezzoneRuntime actor,
        HexCell target,
        BarricadeSO item)
    {
        if (actor == null || !actor.IsAlive
            || actor.PositionCell == null)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidActor
            );
        }

        if (item == null)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidItem
            );
        }

        if (target == null)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidTarget
            );
        }

        if (!actor.CanPerformAction(ActionType.Barricade))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.ActionNotAllowed
            );
        }

        if (!actor.Inventory.HasItem(item))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.MissingItem
            );
        }

        if (actor.ActionPoints < item.ActionPointCost)
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InsufficientActionPoints
            );
        }

        if (!TacticalQuery.CanPlaceBarricade(
                actor,
                target,
                item))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.InvalidTarget
            );
        }

        if (!actor.TrySpendActionPoint(item.ActionPointCost))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.ResolutionFailed
            );
        }

        if (!actor.Inventory.ConsumeItem(item))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.ResolutionFailed
            );
        }

        BarricadeRuntime barricade =
            new BarricadeRuntime(item);

        if (!target.TryPlaceBarricade(barricade))
        {
            return ItemActionResult.Fail(
                ItemActionFailure.ResolutionFailed
            );
        }

        return ItemActionResult.Success(barricade);
    }
}
