using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TensionSettingsTests
{
    private TensionSettingsSO _settings;

    [SetUp]
    public void SetUp()
    {
        _settings =
            ScriptableObject.CreateInstance<TensionSettingsSO>();

        SerializedObject serialized =
            new SerializedObject(_settings);

        serialized.FindProperty(
            "_containmentLeashRadius"
        ).intValue = 3;

        serialized.FindProperty(
            "_engageLeashRadius"
        ).intValue = 7;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_settings);
    }

    [TestCase(
        EngagementRules.Containment,
        3)]
    [TestCase(
        EngagementRules.Engage,
        7)]
    [TestCase(
        EngagementRules.Sweep,
        7)]
    public void GetLeashRadius_ReturnsConfiguredValue(
        EngagementRules rules,
        int expected)
    {
        Assert.That(
            _settings.GetLeashRadius(rules),
            Is.EqualTo(expected)
        );
    }
}
