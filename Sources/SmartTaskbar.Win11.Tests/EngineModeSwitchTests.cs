using FluentAssertions;
using NSubstitute;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class EngineModeSwitchTests
{
    private readonly ISettingsStore _store;

    public EngineModeSwitchTests()
    {
        _store = Substitute.For<ISettingsStore>();
        _store.GetValue<string>("AutoModeType").Returns("None");
        _store.GetValue<bool?>("ShowTaskbarWhenExit").Returns(true);
    }

    [Fact]
    public void SwitchingToAuto_FromMaximizeHide_PersistsAuto()
    {
        var settings = new UserSettings(_store);
        settings.AutoModeType = AutoModeType.MaximizeHide;
        settings.AutoModeType = AutoModeType.Auto;
        settings.AutoModeType.Should().Be(AutoModeType.Auto);
    }

    [Fact]
    public void SwitchingToMaximizeHide_FromAuto_PersistsMaximizeHide()
    {
        var settings = new UserSettings(_store);
        settings.AutoModeType = AutoModeType.Auto;
        settings.AutoModeType = AutoModeType.MaximizeHide;
        settings.AutoModeType.Should().Be(AutoModeType.MaximizeHide);
    }

    [Fact]
    public void SwitchingToNone_FromMaximizeHide_PersistsNone()
    {
        var settings = new UserSettings(_store);
        settings.AutoModeType = AutoModeType.MaximizeHide;
        settings.AutoModeType = AutoModeType.None;
        settings.AutoModeType.Should().Be(AutoModeType.None);
    }

    [Fact]
    public void ModeRoundTrip_AllPersistCorrectly()
    {
        var settings = new UserSettings(_store);
        settings.AutoModeType = AutoModeType.Auto;
        _store.Received(1).SetValue("AutoModeType", "Auto");
        settings.AutoModeType = AutoModeType.MaximizeHide;
        _store.Received(1).SetValue("AutoModeType", "MaximizeHide");
        settings.AutoModeType = AutoModeType.None;
        _store.Received(1).SetValue("AutoModeType", "None");
    }
}
