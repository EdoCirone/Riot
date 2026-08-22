using System.Collections.Generic;
using NUnit.Framework;

public class HexCoordinatesTests
{
    [TestCase(0, 0)]
    [TestCase(1, -1)]
    [TestCase(-4, 7)]
    [TestCase(12, -3)]
    public void CubeCoordinates_AlwaysRespectInvariant(int q, int r)
    {
        HexCoordinates coordinates = new HexCoordinates(q, r);

        Assert.That(
            coordinates.Q + coordinates.R + coordinates.S,
            Is.EqualTo(0)
        );
    }

    [Test]
    public void Distance_FromCoordinateToItself_IsZero()
    {
        HexCoordinates coordinates = new HexCoordinates(3, -2);

        int distance = coordinates.Distance(coordinates);

        Assert.That(distance, Is.Zero);
    }

    [TestCase(0, 0, 1, 0, 1)]
    [TestCase(0, 0, 1, -1, 1)]
    [TestCase(0, 0, 3, -2, 3)]
    [TestCase(-2, 4, 3, -1, 5)]
    public void Distance_ReturnsExpectedValue(
        int startQ,
        int startR,
        int endQ,
        int endR,
        int expectedDistance)
    {
        HexCoordinates start = new HexCoordinates(startQ, startR);
        HexCoordinates end = new HexCoordinates(endQ, endR);

        int distance = start.Distance(end);

        Assert.That(distance, Is.EqualTo(expectedDistance));
        Assert.That(end.Distance(start), Is.EqualTo(expectedDistance));
    }

    [Test]
    public void GetNeighbors_ReturnsSixUniqueAdjacentCoordinates()
    {
        HexCoordinates center = new HexCoordinates(4, -2);

        HexCoordinates[] neighbors = center.GetNeighbors();
        HashSet<HexCoordinates> uniqueNeighbors =
            new HashSet<HexCoordinates>(neighbors);

        Assert.That(neighbors.Length, Is.EqualTo(6));
        Assert.That(uniqueNeighbors.Count, Is.EqualTo(6));

        foreach (HexCoordinates neighbor in neighbors)
        {
            Assert.That(center.Distance(neighbor), Is.EqualTo(1));
        }
    }

    [TestCase(0, 0)]
    [TestCase(1, -1)]
    [TestCase(-4, 7)]
    [TestCase(12, -3)]
    public void WorldConversion_RoundTripPreservesCoordinates(int q, int r)
    {
        HexCoordinates original = new HexCoordinates(q, r);

        HexCoordinates converted = HexCoordinates.FromWorldPosition(
            original.ToWorldPosition(1f),
            1f
        );

        Assert.That(converted, Is.EqualTo(original));
    }
}
