using NUnit.Framework;
using UnityEngine;

public class GameplayInputGateTests
{
    private GameObject _gateObject;
    private GameObject _firstOwner;
    private GameObject _secondOwner;
    private GameplayInputGate _gate;

    [SetUp]
    public void SetUp()
    {
        _gateObject = new GameObject("GameplayInputGate");
        _gate = _gateObject.AddComponent<GameplayInputGate>();

        _firstOwner = new GameObject("FirstOwner");
        _secondOwner = new GameObject("SecondOwner");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_firstOwner);
        Object.DestroyImmediate(_secondOwner);
        Object.DestroyImmediate(_gateObject);
    }

    [Test]
    public void NewGate_IsNotBlocked()
    {
        Assert.That(_gate.IsBlocked, Is.False);
    }

    [Test]
    public void Acquire_BlocksInput()
    {
        _gate.Acquire(_firstOwner);

        Assert.That(_gate.IsBlocked, Is.True);
    }

    [Test]
    public void Release_RemovesBlock()
    {
        _gate.Acquire(_firstOwner);
        _gate.Release(_firstOwner);

        Assert.That(_gate.IsBlocked, Is.False);
    }

    [Test]
    public void ReleaseOneOwner_KeepsOtherOwnerBlock()
    {
        _gate.Acquire(_firstOwner);
        _gate.Acquire(_secondOwner);

        _gate.Release(_firstOwner);

        Assert.That(_gate.IsBlocked, Is.True);
    }

    [Test]
    public void AcquireSameOwnerTwice_CreatesSingleBlock()
    {
        _gate.Acquire(_firstOwner);
        _gate.Acquire(_firstOwner);

        _gate.Release(_firstOwner);

        Assert.That(_gate.IsBlocked, Is.False);
    }

    [Test]
    public void AcquireNull_DoesNotBlock()
    {
        _gate.Acquire(null);

        Assert.That(_gate.IsBlocked, Is.False);
    }
}
