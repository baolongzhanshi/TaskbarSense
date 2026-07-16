using FluentAssertions;
using SmartTaskbar.Win11.Worker;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

/// <summary>
/// Pure geometry rule used by Auto mode: window is "large" when
/// 3 * windowArea > screenArea (same formula as classic SmartTaskbar).
/// </summary>
public class ScreenAreaComparerTests
{
    [Fact]
    public void IsLargeEnough_WhenTripleWindowAreaExceedsScreen_ReturnsTrue()
    {
        // 800x600 window → area 480000; 3* = 1_440_000
        // 1000x1000 screen → 1_000_000 → should be large
        ScreenAreaComparer.IsLargeEnough(
            left: 0, top: 0, right: 800, bottom: 600,
            screenWidth: 1000, screenHeight: 1000).Should().BeTrue();
    }

    [Fact]
    public void IsLargeEnough_WhenTripleWindowAreaDoesNotExceedScreen_ReturnsFalse()
    {
        // 200x200 → 40000; 3* = 120000
        // 1000x1000 → 1_000_000 → not large
        ScreenAreaComparer.IsLargeEnough(
            left: 0, top: 0, right: 200, bottom: 200,
            screenWidth: 1000, screenHeight: 1000).Should().BeFalse();
    }

    [Fact]
    public void IsLargeEnough_UsesProvidedScreenSize_NotImplicitPrimary()
    {
        // Same window: large on small screen, not large on big screen
        const int left = 0, top = 0, right = 400, bottom = 300; // area 120000; 3* = 360000

        ScreenAreaComparer.IsLargeEnough(left, top, right, bottom, screenWidth: 500, screenHeight: 500)
            .Should().BeTrue(); // screen 250000 < 360000

        ScreenAreaComparer.IsLargeEnough(left, top, right, bottom, screenWidth: 1920, screenHeight: 1080)
            .Should().BeFalse(); // screen 2073600 > 360000
    }

    [Fact]
    public void IsLargeEnough_WhenScreenAreaIsZero_ReturnsFalse()
    {
        ScreenAreaComparer.IsLargeEnough(0, 0, 100, 100, 0, 0).Should().BeFalse();
    }
}