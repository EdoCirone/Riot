using NUnit.Framework;

public class HexDirectionFinderTests
{
    [Test]
    public void FindDirection_ReturnsExpectedDirection_ForAllSixDirections()
    {
        HexCoordinates from = new HexCoordinates(4, -2);
        const int distance = 3;

        foreach (HexCoordinates expectedDirection in HexCoordinates.Directions)
        {
            HexCoordinates to = new HexCoordinates(
                from.Q + expectedDirection.Q * distance,
                from.R + expectedDirection.R * distance
            );

            HexCoordinates? result =
                HexDirectionFinder.FindDirection(from, to);

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(expectedDirection));
        }
    }

    [Test]
    public void FindDirection_ReturnsNull_WhenCellsAreNotAligned()
    {
        HexCoordinates from = new HexCoordinates(0, 0);
        HexCoordinates to = new HexCoordinates(2, -1);

        HexCoordinates? result =
            HexDirectionFinder.FindDirection(from, to);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindDirection_ReturnsNull_WhenCellsAreTheSame()
    {
        HexCoordinates coordinates = new HexCoordinates(3, -2);

        HexCoordinates? result =
            HexDirectionFinder.FindDirection(coordinates, coordinates);

        Assert.That(result, Is.Null);
    }
}
