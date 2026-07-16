using FluentAssertions;
using NSubstitute;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class StartupRegistrationCoordinatorTests
{
    private readonly IStartupRegistration _registration = Substitute.For<IStartupRegistration>();

    [Fact]
    public void Apply_WhenEnableTrue_PurgesLegacyAndSetsCurrent()
    {
        _registration.GetExecutablePath().Returns(@"C:\Apps\TaskbarSense.exe");

        StartupRegistrationCoordinator.Apply(_registration, enable: true);

        _registration.Received(1).PurgeLegacyEntries();
        _registration.Received(1).SetEnabled(@"C:\Apps\TaskbarSense.exe");
        _registration.DidNotReceive().RemoveCurrent();
    }

    [Fact]
    public void Apply_WhenEnableFalse_PurgesLegacyAndRemovesCurrent()
    {
        StartupRegistrationCoordinator.Apply(_registration, enable: false);

        _registration.Received(1).PurgeLegacyEntries();
        _registration.Received(1).RemoveCurrent();
        _registration.DidNotReceive().SetEnabled(Arg.Any<string>());
    }

    [Fact]
    public void Apply_WhenEnableTrueButNoExePath_PurgesOnly()
    {
        _registration.GetExecutablePath().Returns((string?)null);

        StartupRegistrationCoordinator.Apply(_registration, enable: true);

        _registration.Received(1).PurgeLegacyEntries();
        _registration.DidNotReceive().SetEnabled(Arg.Any<string>());
        _registration.DidNotReceive().RemoveCurrent();
    }

    [Fact]
    public void IsEnabled_WhenCurrentOrLegacyPresent_ReturnsTrue()
    {
        _registration.IsCurrentEnabled().Returns(false);
        _registration.IsAnyLegacyEnabled().Returns(true);

        StartupRegistrationCoordinator.IsEffectivelyEnabled(_registration).Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenNothingPresent_ReturnsFalse()
    {
        _registration.IsCurrentEnabled().Returns(false);
        _registration.IsAnyLegacyEnabled().Returns(false);

        StartupRegistrationCoordinator.IsEffectivelyEnabled(_registration).Should().BeFalse();
    }
}