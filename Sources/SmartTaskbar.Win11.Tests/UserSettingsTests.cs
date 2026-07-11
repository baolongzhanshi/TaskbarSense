using FluentAssertions;
using NSubstitute;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class UserSettingsTests
{
    private readonly ISettingsStore _store;

    public UserSettingsTests()
    {
        _store = Substitute.For<ISettingsStore>();
    }

    [Fact]
    public void AutoModeType_WhenStoreEmpty_DefaultsToNone()
    {
        _store.GetValue<string>("AutoModeType").Returns((string?)null);
        new UserSettings(_store).AutoModeType.Should().Be(AutoModeType.None);
    }

    [Fact]
    public void AutoModeType_WhenStoreHasAuto_LoadsAuto()
    {
        _store.GetValue<string>("AutoModeType").Returns("Auto");
        new UserSettings(_store).AutoModeType.Should().Be(AutoModeType.Auto);
    }

    [Fact]
    public void AutoModeType_WhenStoreHasMaximizeHide_LoadsMaximizeHide()
    {
        _store.GetValue<string>("AutoModeType").Returns("MaximizeHide");
        new UserSettings(_store).AutoModeType.Should().Be(AutoModeType.MaximizeHide);
    }

    [Fact]
    public void AutoModeType_WhenStoreHasUnknownValue_DefaultsToNone()
    {
        _store.GetValue<string>("AutoModeType").Returns("UnknownMode");
        new UserSettings(_store).AutoModeType.Should().Be(AutoModeType.None);
    }

    [Fact]
    public void AutoModeType_WhenChanged_PersistsToStore()
    {
        _store.GetValue<string>("AutoModeType").Returns("None");
        _store.GetValue<bool?>("ShowTaskbarWhenExit").Returns(true);
        var settings = new UserSettings(_store);
        settings.AutoModeType = AutoModeType.MaximizeHide;
        _store.Received().SetValue("AutoModeType", "MaximizeHide");
    }

    [Fact]
    public void AutoModeType_WhenSetToSameValue_DoesNotPersist()
    {
        _store.GetValue<string>("AutoModeType").Returns("Auto");
        _store.GetValue<bool?>("ShowTaskbarWhenExit").Returns(true);
        var settings = new UserSettings(_store);
        settings.AutoModeType = AutoModeType.Auto;
        _store.DidNotReceive().SetValue("AutoModeType", Arg.Any<string>());
    }

    [Fact]
    public void ShowTaskbarWhenExit_WhenStoreEmpty_DefaultsToTrue()
    {
        _store.GetValue<string>("AutoModeType").Returns("None");
        _store.GetValue<bool?>("ShowTaskbarWhenExit").Returns((bool?)null);
        new UserSettings(_store).ShowTaskbarWhenExit.Should().BeTrue();
    }

    [Fact]
    public void ShowTaskbarWhenExit_WhenChanged_PersistsToStore()
    {
        _store.GetValue<string>("AutoModeType").Returns("None");
        _store.GetValue<bool?>("ShowTaskbarWhenExit").Returns(true);
        var settings = new UserSettings(_store);
        settings.ShowTaskbarWhenExit = false;
        _store.Received().SetValue("ShowTaskbarWhenExit", false);
    }
}
