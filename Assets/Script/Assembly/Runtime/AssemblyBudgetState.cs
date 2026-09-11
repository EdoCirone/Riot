using UnityEngine;

public sealed class AssemblyBudgetState : MonoBehaviour
{
    [SerializeField, Min(0)] private int _basePoints = 15;

    public int BasePoints => _basePoints;
    public int TemporaryPoints { get; private set; }
    public int AvailablePoints => BasePoints + TemporaryPoints;

    public bool TrySetFlyerBonus(
        ObjectiveSO objective,
        int startMinutesFromMidnight)
    {
        TemporaryPoints = 0;

        if (objective == null)
            return false;

        if (!AssemblyBudgetRules.TryCalculateTemporaryBonus(
                startMinutesFromMidnight,
                objective.DeadlineMinutesFromMidnight,
                out int temporaryBonus))
        {
            return false;
        }

        TemporaryPoints = temporaryBonus;
        return true;
    }

    public void ClearFlyerBonus()
    {
        TemporaryPoints = 0;
    }

    public bool TrySpend(int cost)
    {
        if (cost <= 0 || cost > AvailablePoints)
            return false;

        int spentFromTemporary =
            Mathf.Min(cost, TemporaryPoints);

        TemporaryPoints -= spentFromTemporary;

        int remainingCost =
            cost - spentFromTemporary;

        _basePoints -= remainingCost;

        return true;
    }
}
