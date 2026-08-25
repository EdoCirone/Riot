using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class LevelCoverageDiagnostics
{
    public static string Build(
        HexGrid map,
        MeetingPointSO meetingPointData,
        ObjectiveRuntime declaredObjective,
        IReadOnlyList<SpezzoneRuntime> spezzoni,
        IReadOnlyList<PoliceRuntime> police)
    {
        if (map == null || map.Objectives == null)
            return string.Empty;

        MeetingPointRuntime meeting = ResolveMeetingPoint(map, meetingPointData);

        Dictionary<HexCoordinates, int> steps = meeting != null ? StepsFrom(map, meeting.Cells) : null;

        int slow = int.MaxValue;
        int fast = 1;

        foreach (SpezzoneRuntime spezzone in spezzoni)
        {
            if (!spezzone.IsAlive)
                continue;

            if (spezzone.MaxActionPoints < slow)
                slow = spezzone.MaxActionPoints;

            if (spezzone.MaxActionPoints > fast)
                fast = spezzone.MaxActionPoints;
        }

        if (slow == int.MaxValue || slow <= 0)
            slow = 1;

        StringBuilder report = new();

        report.AppendLine(
            $"[COVERAGE] {map.Objectives.Count} objective(s), " +
            $"{police.Count} police, " +
            $"corteo of {spezzoni.Count} from " +
            $"{(meeting != null ? meeting.ToString() : "NO MEETING POINT")}, " +
            $"pace {slow}-{fast} AP/turn"
        );

        report.AppendLine(
            "[COVERAGE] objective                     " +
            "cells  garrison   steps  packed   solo"
        );

        int unguarded = 0;
        int unreachable = 0;

        foreach (ObjectiveRuntime objective in map.Objectives)
        {
            int garrison = 0;

            foreach (PoliceRuntime policeUnit in police)
            {
                if (policeUnit.IsAlive
                    && policeUnit.GuardedObjective == objective)
                {
                    garrison++;
                }
            }

            if (garrison == 0)
                unguarded++;

            int best = int.MaxValue;

            if (steps != null)
            {
                foreach (HexCell cell in objective.Cells)
                {
                    if (steps.TryGetValue(cell.Coordinates, out int distance) && distance < best)
                    {
                        best = distance;
                    }
                }
            }

            bool reachable = best != int.MaxValue;

            if (!reachable)
                unreachable++;

            string stepText = reachable ? best.ToString() : "--";

            string slowText = reachable ? Mathf.CeilToInt(best / (float)slow).ToString() : "--";

            string fastText = reachable ? Mathf.CeilToInt(best / (float)fast).ToString() : "--";

            string mark = objective == declaredObjective ? "   <<< DECLARED" : "";

            report.AppendLine(
                $"[COVERAGE] " +
                $"{objective.ToString().PadRight(30)}" +
                $"{objective.Cells.Count,5}" +
                $"{garrison,10}" +
                $"{stepText,8}" +
                $"{slowText,8}" +
                $"{fastText,7}" +
                $"{mark}"
            );
        }

        report.AppendLine(
            $"[COVERAGE] {unguarded} objective(s) " +
            "with no garrison"
        );

        if (unreachable > 0)
        {
            report.AppendLine(
                $"[COVERAGE] WARNING: {unreachable} " +
                "objective(s) cannot be reached on foot"
            );
        }

        return report.ToString();
    }

    private static MeetingPointRuntime ResolveMeetingPoint(HexGrid map, MeetingPointSO meetingPointData)
    {
        if (map == null || meetingPointData == null)
            return null;

        foreach (MeetingPointRuntime candidate
                 in map.MeetingPoints)
        {
            if (candidate.Data == meetingPointData)
                return candidate;
        }

        return null;
    }

    private static Dictionary<HexCoordinates, int> StepsFrom(HexGrid map, IReadOnlyList<HexCell> sources)
    {
        Dictionary<HexCoordinates, int> steps = new();
        Queue<HexCoordinates> queue = new();

        foreach (HexCell source in sources)
        {
            if (source == null)
                continue;

            steps[source.Coordinates] = 0;
            queue.Enqueue(source.Coordinates);
        }

        while (queue.Count > 0)
        {
            HexCoordinates current = queue.Dequeue();
            int next = steps[current] + 1;

            foreach (HexCoordinates direction
                     in HexCoordinates.Directions)
            {
                HexCoordinates neighbor = current + direction;

                if (steps.ContainsKey(neighbor))
                    continue;

                if (!map.TryGetCell(neighbor, out HexCell cell))
                {
                    continue;
                }

                if (cell.Type == null
                    || !cell.Type.IsWalkable)
                {
                    continue;
                }

                steps[neighbor] = next;
                queue.Enqueue(neighbor);
            }
        }

        return steps;
    }
}
