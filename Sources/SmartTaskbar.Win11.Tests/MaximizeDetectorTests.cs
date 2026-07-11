using FluentAssertions;
using NSubstitute;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Worker;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class MaximizeDetectorTests
{
    private readonly IWindowEnumerationService _windowEnum;
    private readonly IWindowStateService _windowState;
    private readonly IMonitorService _monitorService;
    private readonly MaximizeDetector _detector;

    private static readonly IntPtr Monitor1 = new(1001);
    private static readonly IntPtr Monitor2 = new(1002);
    private static readonly IntPtr Hwnd1 = new(0x001);
    private static readonly IntPtr Hwnd2 = new(0x002);
    private static readonly IntPtr Hwnd3 = new(0x003);
    private static readonly IntPtr Hwnd4 = new(0x004);

    public MaximizeDetectorTests()
    {
        _windowEnum = Substitute.For<IWindowEnumerationService>();
        _windowState = Substitute.For<IWindowStateService>();
        _monitorService = Substitute.For<IMonitorService>();
        _detector = new MaximizeDetector(_windowEnum, _windowState, _monitorService);
    }

    [Fact]
    public void NoWindows_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(Array.Empty<IntPtr>());
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void MaximizedVisibleWindowOnSameMonitor_ReturnsTrue()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        _windowState.IsVisible(Hwnd1).Returns(true);
        _windowState.IsMaximized(Hwnd1).Returns(true);
        _windowState.GetClassName(Hwnd1).Returns("Notepad");
        _monitorService.GetMonitorFromWindow(Hwnd1).Returns(Monitor1);
        _monitorService.IsSameMonitor(Monitor1, Monitor1).Returns(true);
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();
    }

    [Fact]
    public void MaximizedWindowOnDifferentMonitor_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        _windowState.IsVisible(Hwnd1).Returns(true);
        _windowState.IsMaximized(Hwnd1).Returns(true);
        _windowState.GetClassName(Hwnd1).Returns("Notepad");
        _monitorService.GetMonitorFromWindow(Hwnd1).Returns(Monitor2);
        _monitorService.IsSameMonitor(Monitor2, Monitor1).Returns(false);
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void InvisibleMaximizedWindow_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        _windowState.IsVisible(Hwnd1).Returns(false);
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
        _windowState.DidNotReceive().IsMaximized(Arg.Any<IntPtr>());
    }

    [Fact]
    public void VisibleButNotMaximized_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        _windowState.IsVisible(Hwnd1).Returns(true);
        _windowState.IsMaximized(Hwnd1).Returns(false);
        _windowState.GetClassName(Hwnd1).Returns("Notepad");
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void MultipleWindowsOneMaximized_ReturnsTrue()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1, Hwnd2, Hwnd3, Hwnd4 });
        _windowState.IsVisible(Hwnd1).Returns(true);
        _windowState.IsVisible(Hwnd2).Returns(true);
        _windowState.IsVisible(Hwnd3).Returns(true);
        _windowState.IsVisible(Hwnd4).Returns(true);
        _windowState.IsMaximized(Hwnd1).Returns(false);
        _windowState.IsMaximized(Hwnd2).Returns(false);
        _windowState.IsMaximized(Hwnd3).Returns(true);
        _windowState.GetClassName(Arg.Any<IntPtr>()).Returns("Chrome");
        _monitorService.GetMonitorFromWindow(Hwnd3).Returns(Monitor1);
        _monitorService.IsSameMonitor(Monitor1, Monitor1).Returns(true);
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();
    }

    [Fact]
    public void SkipsShellTrayWnd()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        _windowState.IsVisible(Hwnd1).Returns(true);
        _windowState.IsMaximized(Hwnd1).Returns(true);
        _windowState.GetClassName(Hwnd1).Returns("Shell_TrayWnd");
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void SkipsProgmanAndWorkerW()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1, Hwnd2 });
        _windowState.IsVisible(Arg.Any<IntPtr>()).Returns(true);
        _windowState.IsMaximized(Arg.Any<IntPtr>()).Returns(true);
        _windowState.GetClassName(Hwnd1).Returns("Progman");
        _windowState.GetClassName(Hwnd2).Returns("WorkerW");
        _detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }
}
