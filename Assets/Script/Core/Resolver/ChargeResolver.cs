public static class ChargeResolver
{
    public static bool CanStart(
        AbstractUnitsRunTime attacker,
        AbstractUnitsRunTime defender,
        HexGrid map,
        out HexCell destinationCell)
    {
        destinationCell = null;

        if (attacker == null || defender == null || map == null)
            return false;

        if (attacker.PositionCell == null || defender.PositionCell == null)
            return false;

        if (!attacker.IsAlive || !defender.IsAlive)
            return false;

        if (defender.IsSeated)
            return false;

        if (!attacker.CanPerformAction(ActionType.Charge))
            return false;

        if (attacker.ActionPoints < TacticalQuery.ChargeCost)
            return false;

        if (!TacticalQuery.HasChargeRoom(
                attacker.PositionCell.Coordinates,
                defender.PositionCell.Coordinates,
                map,
                out HexCoordinates destination))
        {
            return false;
        }

        return map.TryGetCell(destination, out destinationCell);
    }
}
