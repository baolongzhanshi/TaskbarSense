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

    private static readonly IntPtr Monitor1 = new(1001);
    private static readonly IntPtr Monitor2 = new(1002);
    private static readonly IntPtr Hwnd1 = new(0x001);
    private static readonly IntPtr Hwnd2 = new(0x002);
    private static readonly IntPtr Hwnd3 = new(0x003);
    private static readonly IntPtr Hwnd4 = new(0x004);
    private static readonly IntPtr HwndForeground = new(0x0F0);

    public MaximizeDetectorTests()
    {
        _windowEnum = Substitute.For<IWindowEnumerationService>();
        _windowState = Substitute.For<IWindowStateService>();
        _monitorService = Substitute.For<IMonitorService>();
    }

    private MaximizeDetector CreateDetector(Func<IntPtr>? getForeground = null)
        => new(_windowEnum, _windowState, _monitorService, getForeground ?? (() => IntPtr.Zero));

    private void SetupVisibleAppWindow(IntPtr hwnd, IntPtr monitor, bool maximized = false, bool fullscreen = false, string className = "Notepad")
    {
        _windowState.IsVisible(hwnd).Returns(true);
        _windowState.IsMaximized(hwnd).Returns(maximized);
        _windowState.IsFullscreen(hwnd).Returns(fullscreen);
        _windowState.GetClassName(hwnd).Returns(className);
        _monitorService.GetMonitorFromWindow(hwnd).Returns(monitor);
        _monitorService.IsSameMonitor(monitor, Monitor1).Returns(monitor == Monitor1);
        _monitorService.IsSameMonitor(monitor, monitor).Returns(true);
    }

    [Fact]
    public void NoWindows_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(Array.Empty<IntPtr>());
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void MaximizedVisibleWindowOnSameMonitor_ReturnsTrue()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, maximized: true);
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();
    }

    [Fact]
    public void FullscreenVisibleWindowOnSameMonitor_ReturnsTrue()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, fullscreen: true, className: "GameWindow");
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();
    }

    [Fact]
    public void ForegroundFullscreen_ReturnsTrue_WithoutDependingOnEnumOrder()
    {
        // Enum returns only unrelated windows; foreground is the real fullscreen hit.
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, maximized: false, fullscreen: false);
        SetupVisibleAppWindow(HwndForeground, Monitor1, fullscreen: true, className: "GameWindow");

        var detector = CreateDetector(() => HwndForeground);
        detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();

        // Foreground path should succeed without needing IsFullscreen on the enum-only window.
        _windowState.Received().IsFullscreen(HwndForeground);
    }

    [Fact]
    public void CachedHit_DoesNotReEnumerateWindows()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, maximized: true);

        var detector = CreateDetector();
        detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();
        detector.HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();

        _windowEnum.Received(1).EnumerateTopLevelWindows();
    }

    [Fact]
    public void FullscreenGeometryChecks_AreCappedPerScan()
    {
        // Many same-monitor visible non-maximized windows: only MaxFullscreenChecksPerScan
        // should call IsFullscreen.
        var handles = Enumerable.Range(1, 20).Select(i => new IntPtr(i)).ToArray();
        _windowEnum.EnumerateTopLevelWindows().Returns(handles);

        foreach (var h in handles)
            SetupVisibleAppWindow(h, Monitor1, maximized: false, fullscreen: false, className: "App");

        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();

        _windowState.Received(MaximizeDetector.MaxFullscreenChecksPerScan)
            .IsFullscreen(Arg.Any<IntPtr>());
    }

    [Fact]
    public void DifferentMonitor_SkipsBeforeFullscreenProbe()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor2, maximized: false, fullscreen: true);

        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();

        // Same-monitor filter should prevent fullscreen geometry work for other displays.
        _windowState.DidNotReceive().IsFullscreen(Hwnd1);
        _windowState.DidNotReceive().IsMaximized(Hwnd1);
    }

    [Fact]
    public void MaximizedWindowOnDifferentMonitor_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor2, maximized: true);
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void InvisibleMaximizedWindow_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        _windowState.IsVisible(Hwnd1).Returns(false);
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
        _windowState.DidNotReceive().IsMaximized(Arg.Any<IntPtr>());
        _windowState.DidNotReceive().GetClassName(Arg.Any<IntPtr>());
    }

    [Fact]
    public void VisibleButNotMaximized_ReturnsFalse()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, maximized: false, fullscreen: false);
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void MultipleWindowsOneMaximized_ReturnsTrue()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1, Hwnd2, Hwnd3, Hwnd4 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, maximized: false);
        SetupVisibleAppWindow(Hwnd2, Monitor1, maximized: false);
        SetupVisibleAppWindow(Hwnd3, Monitor1, maximized: true, className: "Chrome");
        SetupVisibleAppWindow(Hwnd4, Monitor1, maximized: false);

        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeTrue();
    }

    [Fact]
    public void SkipsShellTrayWnd()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, maximized: true, className: "Shell_TrayWnd");
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }

    [Fact]
    public void SkipsProgmanAndWorkerW()
    {
        _windowEnum.EnumerateTopLevelWindows().Returns(new[] { Hwnd1, Hwnd2 });
        SetupVisibleAppWindow(Hwnd1, Monitor1, maximized: true, className: "Progman");
        SetupVisibleAppWindow(Hwnd2, Monitor1, maximized: true, className: "WorkerW");
        CreateDetector().HasMaximizedWindowOnMonitor(Monitor1).Should().BeFalse();
    }
}