using NUnit.Framework;
using UnityEngine;

public class PoliceEngagementRulesTests
{
    private PoliceSO _policeData;

    [SetUp]
    public void SetUp()
    {
        _policeData =
            ScriptableObject.CreateInstance<PoliceSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_policeData);
    }

    private PoliceRuntime CreatePolice()
    {
        HexCell cell = new HexCell(
            new HexCoordinates(0, 0),
            type: null
        );

        return new PoliceRuntime(
            cell,
            UnitsStatus.Alive,
            _policeData,
            morale: 10,
            actionPoint: 4
        );
    }

    [Test]
    public void LevelDrivenUnit_AcceptsNewEngagementRules()
    {
        PoliceRuntime police = CreatePolice();

        police.AssignGuard(
            objective: null,
            EngagementRules.Containment,
            leashRadius: 4,
            overridesEngagementRules: false
        );

        bool changed =
            police.ApplyLevelEngagementRules(
                EngagementRules.Engage
            );

        Assert.That(changed, Is.True);
        Assert.That(
            police.EngagementRules,
            Is.EqualTo(EngagementRules.Engage)
        );
    }

    [Test]
    public void OverriddenUnit_IgnoresLevelEngagementRules()
    {
        PoliceRuntime police = CreatePolice();

        police.AssignGuard(
            objective: null,
            EngagementRules.Sweep,
            leashRadius: 4,
            overridesEngagementRules: true
        );

        bool changed =
            police.ApplyLevelEngagementRules(
                EngagementRules.Containment
            );

        Assert.That(changed, Is.False);
        Assert.That(
            police.EngagementRules,
            Is.EqualTo(EngagementRules.Sweep)
        );
    }
}
