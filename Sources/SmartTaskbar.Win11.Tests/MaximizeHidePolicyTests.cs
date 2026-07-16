using NSubstitute;
using SmartTaskbar.Win11;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Worker;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

/// <summary>
/// Behavioral slice of MaximizeHide: detector drives Hide/Show via control service.
/// </summary>
public class MaximizeHidePolicyTests
{
    private static readonly IntPtr Monitor = new(42);
    private static readonly TaskbarInfo Taskbar = new(
        handle: new IntPtr(1),
        rect: new TagRect { left = 0, top = 1000, right = 1920, bottom = 1080 },
        isShow: true,
        position: TaskbarPosition.Bottom,
        monitor: Monitor);

    [Fact]
    public void Apply_WhenDetectorFindsMaximized_HidesTaskbar()
    {
        var detector = Substitute.For<IMaximizePresence>();
        var control = Substitute.For<ITaskbarControlService>();
        detector.HasMaximizedWindowOnMonitor(Monitor).Returns(true);

        MaximizeHidePolicy.Apply(Taskbar, detector, control);

        control.Received(1).HideTaskbar(Taskbar);
        control.DidNotReceive().ShowTaskbar(Arg.Any<TaskbarInfo>());
    }

    [Fact]
    public void Apply_WhenDetectorFindsNone_ShowsTaskbar()
    {
        var detector = Substitute.For<IMaximizePresence>();
        var control = Substitute.For<ITaskbarControlService>();
        detector.HasMaximizedWindowOnMonitor(Monitor).Returns(false);

        MaximizeHidePolicy.Apply(Taskbar, detector, control);

        control.Received(1).ShowTaskbar(Taskbar);
        control.DidNotReceive().HideTaskbar(Arg.Any<TaskbarInfo>());
    }

    [Fact]
    public void Apply_WhenTaskbarHandleZero_DoesNothing()
    {
        var empty = TaskbarInfo.Empty;
        var detector = Substitute.For<IMaximizePresence>();
        var control = Substitute.For<ITaskbarControlService>();

        MaximizeHidePolicy.Apply(empty, detector, control);

        detector.DidNotReceive().HasMaximizedWindowOnMonitor(Arg.Any<IntPtr>());
        control.DidNotReceive().HideTaskbar(Arg.Any<TaskbarInfo>());
        control.DidNotReceive().ShowTaskbar(Arg.Any<TaskbarInfo>());
    }
}