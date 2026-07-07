using FluentAssertions;
using VrcOscAutomator.Models;
using Xunit;

namespace VrcOscAutomator.Tests.Models;

public class RandomWaitSlotTests
{
    [Fact]
    public void GetDurationMs_NormalRange_ReturnsWithinBounds()
    {
        var slot = new RandomWaitSlot(300, 1000);

        for (int i = 0; i < 100; i++)
        {
            int d = slot.GetDurationMs();
            d.Should().BeInRange(300, 1000);
        }
    }

    [Fact]
    public void GetDurationMs_MinEqualsMax_ReturnsThatValue()
    {
        var slot = new RandomWaitSlot(500, 500);

        slot.GetDurationMs().Should().Be(500);
    }

    [Fact]
    public void GetDurationMs_MinGreaterThanMax_DoesNotThrow_ReturnsMin()
    {
        var slot = new RandomWaitSlot(1000, 300);

        int d = 0;
        Action act = () => d = slot.GetDurationMs();

        act.Should().NotThrow();
        d.Should().Be(1000);
    }

    [Fact]
    public void GetDurationMs_MaxIsIntMaxValue_DoesNotOverflowOrThrow()
    {
        var slot = new RandomWaitSlot(0, int.MaxValue);

        int d = 0;
        Action act = () => d = slot.GetDurationMs();

        act.Should().NotThrow();
        d.Should().BeInRange(0, int.MaxValue);
    }

    [Fact]
    public void GetDurationMs_MinEqualsMaxAtIntMaxValue_ReturnsMin()
    {
        var slot = new RandomWaitSlot(int.MaxValue, int.MaxValue);

        slot.GetDurationMs().Should().Be(int.MaxValue);
    }
}
