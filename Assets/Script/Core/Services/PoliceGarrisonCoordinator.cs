using System.Collections.Generic;
using UnityEngine;

public static class PoliceGarrisonCoordinator
{
    public static void Assign(
        IReadOnlyList<PoliceRuntime> policeUnits,
        IReadOnlyList<ObjectiveRuntime> objectives,
        ObjectiveRuntime declaredObjective,
        UnitsRenderer unitsRenderer,
        EngagementRules defaultRules,
        int defaultLeashRadius,
        float declaredReinforcement)
    {
        if (policeUnits == null || objectives == null)
            return;

        List<(PoliceRuntime police, bool pinned)> assigned = new();

        foreach (PoliceRuntime police in policeUnits)
        {
            if (police == null)
                continue;

            ObjectiveRuntime target = null;
            bool pinned = false;

            GameObject go =
                unitsRenderer != null
                    ? unitsRenderer.GetGameObject(police)
                    : null;

            UnitsSetup setup =
                go != null
                    ? go.GetComponent<UnitsSetup>()
                    : null;

            if (setup != null &&
                setup.GuardedObjective != null)
            {
                foreach (ObjectiveRuntime candidate in objectives)
                {
                    if (candidate.Data != setup.GuardedObjective)
                        continue;

                    target = candidate;
                    break;
                }

                if (target == null)
                {
                    Debug.LogError(
                        $"[GARRISON] {police}: declared objective " +
                        $"'{setup.GuardedObjective.name}' is not on this map"
                    );
                }
                else
                {
                    pinned = true;
                }
            }

            if (target == null)
            {
                target = NearestObjective(
                    police.PositionCell.Coordinates,
                    objectives
                );
            }

            bool overridesEngagementRules = setup != null && setup.OverrideEngagement;

            EngagementRules rules =
                overridesEngagementRules
                    ? setup.EngagementRules
                    : defaultRules;

            int radius =
                setup != null &&
                setup.LeashRadiusOverride >= 0
                    ? setup.LeashRadiusOverride
                    : defaultLeashRadius;

            police.AssignGuard(target, rules, radius, overridesEngagementRules);

            Debug.Log(
                target != null
                    ? $"[GARRISON] {police} guards {target} — " +
                      $"{rules}, radius {radius}"
                    : $"[GARRISON] {police} has no objective " +
                      "to guard: it will roam"
            );

            assigned.Add((police,pinned));
        }

        ReinforceDeclaredObjective(
            assigned,
            declaredObjective,
            declaredReinforcement
        );
    }

    private static void ReinforceDeclaredObjective(
    List<(
        PoliceRuntime police,
        bool pinned)> assigned,
        ObjectiveRuntime declaredObjective,
        float declaredReinforcement)
    {
        if (declaredObjective == null ||
            declaredReinforcement <= 0f)
        {
            return;
        }

        List<( PoliceRuntime police,int distance)> candidates = new();

        foreach (var entry in assigned)
        {
            if (entry.pinned ||
                !entry.police.IsAlive ||
                entry.police.GuardedObjective ==
                declaredObjective)
            {
                continue;
            }

            candidates.Add((
                entry.police,
                DistanceToObjective(
                    entry.police.PositionCell.Coordinates,
                    declaredObjective
                )
            ));
        }

        if (candidates.Count == 0)
        {
            Debug.Log(
                "[GARRISON] flyer is public but nobody can " +
                "answer it: every free unit already guards " +
                $"{declaredObjective}, the rest are pinned"
            );

            return;
        }

        candidates.Sort(
            (a, b) => a.distance.CompareTo(b.distance)
        );

        int toMove = Mathf.CeilToInt(
            candidates.Count * declaredReinforcement
        );

        toMove = Mathf.Min(toMove, candidates.Count);

        for (int i = 0; i < toMove; i++)
        {
            var candidate = candidates[i];

            ObjectiveRuntime previousObjective =
                candidate.police.GuardedObjective;

            candidate.police.ReassignGuard(declaredObjective);

            Debug.Log(
                $"[GARRISON] {candidate.police} pulled from " +
                $"{previousObjective} to the declared " +
                $"{declaredObjective} " +
                $"(distance {candidate.distance})"
            );
        }

        Debug.Log(
            $"[GARRISON] flyer is public: {toMove} of " +
            $"{candidates.Count} free unit(s) reinforce " +
            $"{declaredObjective}"
        );
    }

    private static ObjectiveRuntime NearestObjective(
        HexCoordinates origin,
        IReadOnlyList<ObjectiveRuntime> objectives)
    {
        ObjectiveRuntime nearest = null;
        int bestDistance = int.MaxValue;

        foreach (ObjectiveRuntime objective in objectives)
        {
            int distance = DistanceToObjective(
                origin,
                objective
            );

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = objective;
        }

        return nearest;
    }

    private static int DistanceToObjective(
        HexCoordinates origin,
        ObjectiveRuntime objective)
    {
        if (objective == null)
            return int.MaxValue;

        int bestDistance = int.MaxValue;

        foreach (HexCell cell in objective.Cells)
        {
            int distance =
                origin.Distance(cell.Coordinates);

            if (distance < bestDistance)
                bestDistance = distance;
        }

        return bestDistance;
    }
}
