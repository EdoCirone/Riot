using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Reflection;

public sealed class CorteoCompositionControllerTests
{
    private GameObject _controllerObject;
    private CorteoSelectionSO _corteoSelection;
    private FlyerSelectionSO _flyerSelection;
    private ObjectiveSO _objective;
    private MeetingPointSO _meetingPoint;
    private HexMapSO _map;
    private HexTypeSO _meetingGroundType;
    private SpezzoneSO _unit;

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_unit);
        Object.DestroyImmediate(_meetingGroundType);
        Object.DestroyImmediate(_map);
        Object.DestroyImmediate(_meetingPoint);
        Object.DestroyImmediate(_objective);
        Object.DestroyImmediate(_flyerSelection);
        Object.DestroyImmediate(_corteoSelection);
        Object.DestroyImmediate(_controllerObject);
    }

    [Test]
    public void TryAdd_WithConfirmedFlyer_AddsUnit()
    {
        _controllerObject =
            new GameObject("CorteoCompositionControllerTests");

        AssemblyBudgetState budget =
            _controllerObject.AddComponent<AssemblyBudgetState>();

        CorteoCompositionController controller =
            _controllerObject.AddComponent<CorteoCompositionController>();

        _corteoSelection =
            ScriptableObject.CreateInstance<CorteoSelectionSO>();

        _flyerSelection =
            ScriptableObject.CreateInstance<FlyerSelectionSO>();

        _objective =
            ScriptableObject.CreateInstance<ObjectiveSO>();

        _meetingPoint =
            ScriptableObject.CreateInstance<MeetingPointSO>();

        _meetingGroundType =
            ScriptableObject.CreateInstance<HexTypeSO>();

        _unit =
            ScriptableObject.CreateInstance<SpezzoneSO>();

        SerializedObject serializedType =
            new SerializedObject(_meetingGroundType);

        serializedType
            .FindProperty("_isMeetingGround")
            .boolValue = true;

        serializedType.ApplyModifiedPropertiesWithoutUndo();

        _map =
            ScriptableObject.CreateInstance<HexMapSO>();

        _map.Initialize(
            width: 1,
            height: 1,
            defaultType: _meetingGroundType);

        Assert.That(
            _flyerSelection.TrySetSelection(
                _objective,
                _meetingPoint,
                720),
            Is.True);

        SerializedObject serializedController =
            new SerializedObject(controller);

        serializedController
            .FindProperty("_corteoSelection")
            .objectReferenceValue = _corteoSelection;

        serializedController
            .FindProperty("_flyerSelection")
            .objectReferenceValue = _flyerSelection;

        serializedController
            .FindProperty("_budgetState")
            .objectReferenceValue = budget;

        serializedController
            .FindProperty("_mapData")
            .objectReferenceValue = _map;

        serializedController.ApplyModifiedPropertiesWithoutUndo();

        MethodInfo awakeMethod =
            typeof(CorteoCompositionController).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(awakeMethod, Is.Not.Null);

        awakeMethod.Invoke(
            controller,
            null);
        bool result = controller.TryAdd(_unit);

        Assert.That(result, Is.True);
        Assert.That(_corteoSelection.Count, Is.EqualTo(1));
        Assert.That(_corteoSelection.CountOf(_unit), Is.EqualTo(1));
    }
    [Test]
    public void TryAdd_WithMissingReferences_ReturnsFalse()
    {
        _controllerObject =
            new GameObject("CorteoCompositionControllerTests");

        CorteoCompositionController controller =
            _controllerObject.AddComponent<CorteoCompositionController>();

        _unit =
            ScriptableObject.CreateInstance<SpezzoneSO>();

        bool result = controller.TryAdd(_unit);

        Assert.That(result, Is.False);
    }
}
