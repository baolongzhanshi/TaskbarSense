using FluentAssertions;
using SmartTaskbar.Win11.Models;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class AutoModeTypeTests
{
    [Fact]
    public void AutoModeType_ShouldHaveThreeValues()
    {
        var values = Enum.GetValues<AutoModeType>();
        values.Should().HaveCount(3);
    }

    [Fact]
    public void AutoModeType_None_ShouldBeZero() => ((int)AutoModeType.None).Should().Be(0);

    [Fact]
    public void AutoModeType_Auto_ShouldBeOne() => ((int)AutoModeType.Auto).Should().Be(1);

    [Fact]
    public void AutoModeType_MaximizeHide_ShouldBeTwo() => ((int)AutoModeType.MaximizeHide).Should().Be(2);

    [Fact]
    public void MaximizeHide_ShouldBeDistinctFromAuto()
    {
        AutoModeType.MaximizeHide.Should().NotBe(AutoModeType.Auto);
        AutoModeType.MaximizeHide.Should().NotBe(AutoModeType.None);
    }
}
