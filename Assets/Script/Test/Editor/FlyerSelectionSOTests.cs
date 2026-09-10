using NUnit.Framework;
using UnityEngine;

public sealed class FlyerSelectionSOTests
{
    private FlyerSelectionSO _selection;
    private ObjectiveSO _objective;
    private MeetingPointSO _meetingPoint;

    [SetUp]
    public void SetUp()
    {
        _selection = ScriptableObject.CreateInstance<FlyerSelectionSO>();
        _objective = ScriptableObject.CreateInstance<ObjectiveSO>();
        _meetingPoint = ScriptableObject.CreateInstance<MeetingPointSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_selection);
        Object.DestroyImmediate(_objective);
        Object.DestroyImmediate(_meetingPoint);
    }
    [Test]
    public void NewSelection_HasNoSelection()
    {
        Assert.That(_selection.HasSelection, Is.False);
        Assert.That(_selection.DeclaredObjective, Is.Null);
        Assert.That(_selection.MeetingPoint, Is.Null);
        Assert.That(_selection.StartMinutesFromMidnight, Is.EqualTo(-1));
    }

    [Test]
    public void TrySetSelection_WithValidValues_StoresSelection()
    {
        const int startMinutes = 12 * 60 + 30;

        bool result = _selection.TrySetSelection(
            _objective,
            _meetingPoint,
            startMinutes);

        Assert.That(result, Is.True);
        Assert.That(_selection.HasSelection, Is.True);
        Assert.That(_selection.DeclaredObjective, Is.SameAs(_objective));
        Assert.That(_selection.MeetingPoint, Is.SameAs(_meetingPoint));
        Assert.That(
            _selection.StartMinutesFromMidnight,
            Is.EqualTo(startMinutes));
    }
    [TestCase(-1)]
    [TestCase(24 * 60)]
    public void TrySetSelection_WithInvalidTime_DoesNotOverwriteSelection(
    int invalidTime)
    {
        const int validTime = 12 * 60;

        Assert.That(
            _selection.TrySetSelection(
                _objective,
                _meetingPoint,
                validTime),
            Is.True);

        bool result = _selection.TrySetSelection(
            _objective,
            _meetingPoint,
            invalidTime);

        Assert.That(result, Is.False);
        Assert.That(_selection.HasSelection, Is.True);
        Assert.That(
            _selection.StartMinutesFromMidnight,
            Is.EqualTo(validTime));
    }
    [Test]
    public void TrySetSelection_WithoutObjective_RejectsSelection()
    {
        bool result = _selection.TrySetSelection(
            null,
            _meetingPoint,
            12 * 60);

        Assert.That(result, Is.False);
        Assert.That(_selection.HasSelection, Is.False);
    }

    [Test]
    public void TrySetSelection_WithoutMeetingPoint_RejectsSelection()
    {
        bool result = _selection.TrySetSelection(
            _objective,
            null,
            12 * 60);

        Assert.That(result, Is.False);
        Assert.That(_selection.HasSelection, Is.False);
    }
    [Test]
    public void ClearSelection_AfterValidSelection_RemovesAllData()
    {
        Assert.That(
            _selection.TrySetSelection(
                _objective,
                _meetingPoint,
                12 * 60),
            Is.True);

        _selection.ClearSelection();

        Assert.That(_selection.HasSelection, Is.False);
        Assert.That(_selection.DeclaredObjective, Is.Null);
        Assert.That(_selection.MeetingPoint, Is.Null);
        Assert.That(
            _selection.StartMinutesFromMidnight,
            Is.EqualTo(-1));
    }
}
