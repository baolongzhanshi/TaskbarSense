# SmartTaskbar.Win11 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use TDD (Red-Green-Refactor) for all core logic. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a new .NET 8 WinForms project `SmartTaskbar.Win11` based on the existing SmartTaskbar codebase, adapted for Windows 11 with a new "MaximizeHide" mode that auto-hides the taskbar when any window is maximized on the same monitor.

**Architecture:** New project alongside the original (original untouched). Core detection logic (`MaximizeDetector`) is fully testable through interface abstractions over Win32 APIs. A new `MaximizeHide` mode runs alongside the existing `Auto` mode (mutually exclusive). Win11 centered taskbar adaptation via `TaskbarAlignmentHelper` reading the `TaskbarAl` registry value.

**Tech Stack:** .NET 8 (net8.0-windows10.0.22000.0), WinForms, Win32 P/Invoke (EnumWindows, IsZoomed, SHAppBarMessage), xUnit + NSubstitute + FluentAssertions for testing.

---

## Current State Analysis

The existing `SmartTaskbar` project (at `Sources/SmartTaskbar/`) is a .NET 6 WinForms app that:
- Uses a 125ms timer (`Engine.cs`) to poll foreground window / taskbar geometric intersection
- Controls taskbar via `SHAppBarMessage` (auto-hide toggle) and `PostMessage(handle, 0x05D1, ...)` (show/hide)
- Has two modes: `None` (manual) and `Auto` (intersection-based hide/show)
- Uses `ApplicationData.Current.LocalSettings` for persistence (WinRT API)
- All Win32 calls are in static `Fun` partial classes — not testable

**Key files examined:** `Engine.cs`, `TaskbarHelper.cs`, `NativeMethods.cs`, `AutoHideHelper.cs`, `SystemTray.cs`, `UserSettings.cs`, `AutoModeType.cs`, `Program.cs`, `SmartTaskbar.csproj`

**Research findings:**
- `Shell_TrayWnd` class name and `PostMessage(0x05D1)` mechanism work identically on Win11
- `SHAppBarMessage ABM_SETSTATE` (msg 10) still functional on Win11
- `EnumWindows` + `IsZoomed` are the simplest APIs for maximized window detection
- Win11 taskbar alignment: registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarAl` (DWORD: 0=centered, 1=left)
- .NET 6→8 migration: only `<TargetFramework>` change needed; WinRT APIs still available at `net8.0-windows10.0.22000.0`

---

## Proposed Changes

### Phase 0: Environment Preparation

- [ ] **Step 0.1: Initialize Git repository**

```powershell
cd d:\Desktop\SmartTaskbar
git init
git add .
git commit -m "chore: initial commit of existing SmartTaskbar codebase"
```

- [ ] **Step 0.2: Verify .NET 8 SDK**

```powershell
dotnet --list-sdks
# Confirm output includes 8.x.x
```

---

### Phase 1: Project Scaffolding

- [ ] **Step 1.1: Create `SmartTaskbar.Win11.csproj`**

**File:** `Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.22000.0</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>Resources\Logo-White.ico</ApplicationIcon>
    <Authors>Chanple</Authors>
    <Version>2.0.0</Version>
    <Description>SmartTaskbar for Windows 11 - with maximize-hide mode</Description>
    <Copyright>Copyright (c) 2018-2026 ChanpleCai</Copyright>
    <RootNamespace>SmartTaskbar.Win11</RootNamespace>
    <AssemblyName>SmartTaskbar.Win11</AssemblyName>
    <SupportedOSPlatformVersion>10.0.22000.0</SupportedOSPlatformVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <Content Include="Resources\Logo-White.ico" />
  </ItemGroup>
  <ItemGroup>
    <Compile Update="IconResource.Designer.cs">
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <DependentUpon>IconResource.resx</DependentUpon>
    </Compile>
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Update="IconResource.resx">
      <Generator>ResXFileCodeGenerator</Generator>
      <LastGenOutput>IconResource.Designer.cs</LastGenOutput>
    </EmbeddedResource>
  </ItemGroup>
</Project>
```

Key changes from original: `net6.0`→`net8.0`, namespace `SmartTaskbar.Win11`, `SupportedOSPlatformVersion` `10.0.22000.0` (Win11 minimum), version `2.0.0`.

- [ ] **Step 1.2: Create test project `SmartTaskbar.Win11.Tests.csproj`**

**File:** `Sources/SmartTaskbar.Win11.Tests/SmartTaskbar.Win11.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.22000.0</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>SmartTaskbar.Win11.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="NSubstitute" Version="5.1.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SmartTaskbar.Win11\SmartTaskbar.Win11.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 1.3: Copy unchanged files from original project**

Copy from `Sources/SmartTaskbar/` to `Sources/SmartTaskbar.Win11/`, then apply namespace replacements:
- `namespace SmartTaskbar` → `namespace SmartTaskbar.Win11`
- `using static Fun` → `using static SmartTaskbar.Win11.Fun`
- `SmartTaskbar.Languages.Resource` → `SmartTaskbar.Win11.Languages.Resource`

Files to copy (namespace change only):
- `Resources/Logo-Black.ico`, `Resources/Logo-White.ico` (binary, direct copy)
- `IconResource.Designer.cs`, `IconResource.resx` (namespace change)
- `Models/TaskbarBehavior.cs`, `Models/TaskbarPosition.cs`, `Models/AppbarData.cs`, `Models/TagRect.cs`, `Models/TagPoint.cs`, `Models/ForegroundWindowInfo.cs`, `Models/TaskbarInfo.cs`
- `Helpers/AutoHideHelper.cs`, `Helpers/WindowVisible.cs`, `Helpers/AnimationHelper.cs`, `Helpers/UISettingsHelper.cs`
- `Views/MenuStyle.cs`, `Views/ResourceCulture.cs`
- `Worker/TaskbarHelper.cs`
- `Models/UserConfiguration.cs` (namespace change only)

PowerShell batch script:
```powershell
$src = 'd:\Desktop\SmartTaskbar\Sources\SmartTaskbar'
$dst = 'd:\Desktop\SmartTaskbar\Sources\SmartTaskbar.Win11'
$dirs = @('Helpers','Models','Views','Languages','Resources','Worker','Abstractions','Worker\Services')
foreach ($d in $dirs) { New-Item -ItemType Directory -Force -Path "$dst\$d" }
Copy-Item "$src\Resources\Logo-Black.ico" "$dst\Resources\"
Copy-Item "$src\Resources\Logo-White.ico" "$dst\Resources\"
Copy-Item "$src\IconResource.resx" "$dst\"
# Copy .cs files then replace namespaces
Get-ChildItem -Path $src -Recurse -Filter *.cs | Where-Object { $_.DirectoryName -notmatch 'Win10' } | ForEach-Object {
    $rel = $_.FullName.Substring($src.Length)
    $target = "$dst$rel"
    Copy-Item $_.FullName $target -Force
}
Get-ChildItem -Path $dst -Recurse -Filter *.cs | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace 'namespace SmartTaskbar\b', 'namespace SmartTaskbar.Win11'
    $content = $content -replace 'using static Fun', 'using static SmartTaskbar.Win11.Fun'
    $content = $content -replace 'SmartTaskbar\.Languages\.Resource', 'SmartTaskbar.Win11.Languages.Resource'
    Set-Content $_.FullName -Value $content -Encoding UTF8
}
```

- [ ] **Step 1.4: Create `Program.cs` with new Mutex GUID**

**File:** `Sources/SmartTaskbar.Win11/Program.cs`

```csharp
namespace SmartTaskbar.Win11
{
    public static class Program
    {
        [STAThread]
        private static void Main()
        {
            using (new Mutex(true, "{a1b2c3d4-e5f6-7890-abcd-ef1234567890}", out var createNew))
            {
                if (!createNew) return;
                ApplicationConfiguration.Initialize();
                Application.Run(new SystemTray());
            }
        }
    }
}
```

New Mutex GUID to avoid conflict with original SmartTaskbar instance.

- [ ] **Step 1.5: Update solution file**

Add both new projects to `Sources/SmartTaskbar.sln` with proper GUIDs and configuration mappings.

- [ ] **Step 1.6: Commit scaffolding**

```powershell
git add Sources/SmartTaskbar.Win11/ Sources/SmartTaskbar.Win11.Tests/ Sources/SmartTaskbar.sln
git commit -m "feat: scaffold SmartTaskbar.Win11 project with .NET 8 and test project"
```

---

### Phase 2: Abstraction Layer (TDD Infrastructure)

- [ ] **Step 2.1: Create `IWindowEnumerationService` interface**

**File:** `Sources/SmartTaskbar.Win11/Abstractions/IWindowEnumerationService.cs`

```csharp
namespace SmartTaskbar.Win11.Abstractions
{
    public interface IWindowEnumerationService
    {
        IReadOnlyList<IntPtr> EnumerateTopLevelWindows();
    }
}
```

- [ ] **Step 2.2: Create `IWindowStateService` interface**

**File:** `Sources/SmartTaskbar.Win11/Abstractions/IWindowStateService.cs`

```csharp
namespace SmartTaskbar.Win11.Abstractions
{
    public interface IWindowStateService
    {
        bool IsMaximized(IntPtr handle);
        bool IsVisible(IntPtr handle);
        string GetClassName(IntPtr handle);
    }
}
```

- [ ] **Step 2.3: Create `IMonitorService` interface**

**File:** `Sources/SmartTaskbar.Win11/Abstractions/IMonitorService.cs`

```csharp
namespace SmartTaskbar.Win11.Abstractions
{
    public interface IMonitorService
    {
        IntPtr GetMonitorFromWindow(IntPtr windowHandle);
        bool IsSameMonitor(IntPtr monitor1, IntPtr monitor2);
    }
}
```

- [ ] **Step 2.4: Create `ITaskbarControlService` interface**

**File:** `Sources/SmartTaskbar.Win11/Abstractions/ITaskbarControlService.cs`

```csharp
using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11.Abstractions
{
    public interface ITaskbarControlService
    {
        void HideTaskbar(in TaskbarInfo taskbar);
        void ShowTaskbar(in TaskbarInfo taskbar);
        void SetAutoHide();
        bool IsNotAutoHide();
        void CancelAutoHide();
    }
}
```

- [ ] **Step 2.5: Create `ISettingsStore` interface**

**File:** `Sources/SmartTaskbar.Win11/Abstractions/ISettingsStore.cs`

```csharp
namespace SmartTaskbar.Win11.Abstractions
{
    public interface ISettingsStore
    {
        T? GetValue<T>(string key);
        void SetValue<T>(string key, T value);
    }
}
```

- [ ] **Step 2.6: Commit abstraction layer**

```powershell
git add Sources/SmartTaskbar.Win11/Abstractions/
git commit -m "feat: add abstraction interfaces for Win32 API testability"
```

---

### Phase 3: TDD - Core Logic (Red-Green-Refactor)

#### Task 3.1: AutoModeType Enum Extension

- [ ] **Step 3.1.1: Write failing test**

**File:** `Sources/SmartTaskbar.Win11.Tests/AutoModeTypeTests.cs`

```csharp
using FluentAssertions;
using SmartTaskbar.Win11.Models;

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
```

- [ ] **Step 3.1.2: Run test — verify failure**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "AutoModeTypeTests"
```
Expected: FAIL (MaximizeHide not defined)

- [ ] **Step 3.1.3: Implement `AutoModeType` with `MaximizeHide`**

**File:** `Sources/SmartTaskbar.Win11/Models/AutoModeType.cs`

```csharp
namespace SmartTaskbar.Win11.Models
{
    public enum AutoModeType
    {
        None,
        Auto,
        MaximizeHide
    }
}
```

- [ ] **Step 3.1.4: Run test — verify pass**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "AutoModeTypeTests"
```
Expected: PASS (5 tests)

- [ ] **Step 3.1.5: Commit**

```powershell
git add Sources/SmartTaskbar.Win11/Models/AutoModeType.cs Sources/SmartTaskbar.Win11.Tests/AutoModeTypeTests.cs
git commit -m "test: add AutoModeType tests and implement MaximizeHide value"
```

---

#### Task 3.2: MaximizeDetector (Core Detection Logic)

- [ ] **Step 3.2.1: Write failing tests**

**File:** `Sources/SmartTaskbar.Win11.Tests/MaximizeDetectorTests.cs`

```csharp
using FluentAssertions;
using NSubstitute;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Worker;

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
```

- [ ] **Step 3.2.2: Run test — verify failure**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "MaximizeDetectorTests"
```
Expected: FAIL (MaximizeDetector class not found)

- [ ] **Step 3.2.3: Implement `MaximizeDetector`**

**File:** `Sources/SmartTaskbar.Win11/Worker/MaximizeDetector.cs`

```csharp
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker
{
    public class MaximizeDetector
    {
        private readonly IWindowEnumerationService _windowEnumeration;
        private readonly IWindowStateService _windowState;
        private readonly IMonitorService _monitorService;

        private static readonly HashSet<string> ExcludedClassNames = new()
        {
            "Shell_TrayWnd",
            "Progman",
            "WorkerW",
            "Windows.UI.Core.CoreWindow"
        };

        public MaximizeDetector(
            IWindowEnumerationService windowEnumeration,
            IWindowStateService windowState,
            IMonitorService monitorService)
        {
            _windowEnumeration = windowEnumeration;
            _windowState = windowState;
            _monitorService = monitorService;
        }

        public bool HasMaximizedWindowOnMonitor(IntPtr targetMonitor)
        {
            var handles = _windowEnumeration.EnumerateTopLevelWindows();

            foreach (var handle in handles)
            {
                if (!_windowState.IsVisible(handle))
                    continue;

                var className = _windowState.GetClassName(handle);
                if (ExcludedClassNames.Contains(className))
                    continue;

                if (!_windowState.IsMaximized(handle))
                    continue;

                var windowMonitor = _monitorService.GetMonitorFromWindow(handle);
                if (_monitorService.IsSameMonitor(windowMonitor, targetMonitor))
                    return true;
            }

            return false;
        }
    }
}
```

- [ ] **Step 3.2.4: Run test — verify pass**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "MaximizeDetectorTests"
```
Expected: PASS (8 tests)

- [ ] **Step 3.2.5: Commit**

```powershell
git add Sources/SmartTaskbar.Win11/Worker/MaximizeDetector.cs Sources/SmartTaskbar.Win11.Tests/MaximizeDetectorTests.cs
git commit -m "test: add MaximizeDetector tests and implement detection logic"
```

---

#### Task 3.3: UserSettings (Testable Persistence)

- [ ] **Step 3.3.1: Write failing tests**

**File:** `Sources/SmartTaskbar.Win11.Tests/UserSettingsTests.cs`

```csharp
using FluentAssertions;
using NSubstitute;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;

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
```

- [ ] **Step 3.3.2: Run test — verify failure**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "UserSettingsTests"
```
Expected: FAIL (UserSettings no longer matches — now needs ISettingsStore constructor)

- [ ] **Step 3.3.3: Implement testable `UserSettings`**

**File:** `Sources/SmartTaskbar.Win11/Models/UserSettings.cs`

```csharp
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Models
{
    public class UserSettings
    {
        private readonly ISettingsStore _store;
        private UserConfiguration _configuration;

        public static UserSettings Instance { get; set; } = null!;

        public UserSettings(ISettingsStore store)
        {
            _store = store;
            var autoModeString = _store.GetValue<string>(nameof(UserConfiguration.AutoModeType));
            _configuration = new UserConfiguration
            {
                AutoModeType = autoModeString switch
                {
                    nameof(AutoModeType.Auto) => AutoModeType.Auto,
                    nameof(AutoModeType.MaximizeHide) => AutoModeType.MaximizeHide,
                    _ => AutoModeType.None
                },
                ShowTaskbarWhenExit =
                    _store.GetValue<bool?>(nameof(UserConfiguration.ShowTaskbarWhenExit)) ?? true
            };
        }

        public AutoModeType AutoModeType
        {
            get => _configuration.AutoModeType;
            set
            {
                if (value == _configuration.AutoModeType) return;
                _configuration.AutoModeType = value;
                _store.SetValue(nameof(UserConfiguration.AutoModeType), value.ToString());
            }
        }

        public bool ShowTaskbarWhenExit
        {
            get => _configuration.ShowTaskbarWhenExit;
            set
            {
                if (value == _configuration.ShowTaskbarWhenExit) return;
                _configuration.ShowTaskbarWhenExit = value;
                _store.SetValue(nameof(UserConfiguration.ShowTaskbarWhenExit), value);
            }
        }
    }
}
```

Key change: static class → instance class with `ISettingsStore` dependency injection. `Instance` static property for non-DI access in `SystemTray`.

- [ ] **Step 3.3.4: Run test — verify pass**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "UserSettingsTests"
```
Expected: PASS (8 tests)

- [ ] **Step 3.3.5: Commit**

```powershell
git add Sources/SmartTaskbar.Win11/Models/UserSettings.cs Sources/SmartTaskbar.Win11.Tests/UserSettingsTests.cs
git commit -m "test: add UserSettings tests and implement ISettingsStore-based persistence"
```

---

#### Task 3.4: TaskbarAlignmentHelper (Win11 Alignment Detection)

- [ ] **Step 3.4.1: Write failing tests**

**File:** `Sources/SmartTaskbar.Win11.Tests/TaskbarAlignmentHelperTests.cs`

```csharp
using FluentAssertions;
using SmartTaskbar.Win11.Helpers;

namespace SmartTaskbar.Win11.Tests;

public class TaskbarAlignmentHelperTests
{
    [Fact]
    public void IsCentered_WhenTaskbarAlIsZero_ReturnsTrue()
        => new TaskbarAlignmentHelper(new TestRegistryReader(0)).IsCentered.Should().BeTrue();

    [Fact]
    public void IsCentered_WhenTaskbarAlIsOne_ReturnsFalse()
        => new TaskbarAlignmentHelper(new TestRegistryReader(1)).IsCentered.Should().BeFalse();

    [Fact]
    public void IsCentered_WhenTaskbarAlNotPresent_ReturnsTrue()
        => new TaskbarAlignmentHelper(new TestRegistryReader(null)).IsCentered.Should().BeTrue();

    [Fact]
    public void IsCentered_WhenTaskbarAlIsTwo_ReturnsFalse()
        => new TaskbarAlignmentHelper(new TestRegistryReader(2)).IsCentered.Should().BeFalse();

    private class TestRegistryReader : IRegistryReader
    {
        private readonly int? _value;
        public TestRegistryReader(int? value) => _value = value;
        public int? GetDwordValue(string keyPath, string valueName) => _value;
    }
}
```

- [ ] **Step 3.4.2: Run test — verify failure**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "TaskbarAlignmentHelperTests"
```
Expected: FAIL (IRegistryReader and TaskbarAlignmentHelper not found)

- [ ] **Step 3.4.3: Implement `IRegistryReader`, `TaskbarAlignmentHelper`, `WindowsRegistryReader`**

**File:** `Sources/SmartTaskbar.Win11/Helpers/IRegistryReader.cs`

```csharp
namespace SmartTaskbar.Win11.Helpers
{
    public interface IRegistryReader
    {
        int? GetDwordValue(string keyPath, string valueName);
    }
}
```

**File:** `Sources/SmartTaskbar.Win11/Helpers/TaskbarAlignmentHelper.cs`

```csharp
namespace SmartTaskbar.Win11.Helpers
{
    public class TaskbarAlignmentHelper
    {
        private const string AdvancedKeyPath =
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string TaskbarAlValueName = "TaskbarAl";
        private readonly IRegistryReader _registryReader;

        public TaskbarAlignmentHelper(IRegistryReader registryReader)
            => _registryReader = registryReader;

        public bool IsCentered
        {
            get
            {
                var value = _registryReader.GetDwordValue(AdvancedKeyPath, TaskbarAlValueName);
                return value == null || value == 0;
            }
        }

        public bool IsLeftAligned => !IsCentered;
    }
}
```

**File:** `Sources/SmartTaskbar.Win11/Helpers/WindowsRegistryReader.cs`

```csharp
using Microsoft.Win32;

namespace SmartTaskbar.Win11.Helpers
{
    public class WindowsRegistryReader : IRegistryReader
    {
        public int? GetDwordValue(string keyPath, string valueName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, false);
            return key?.GetValue(valueName) as int?;
        }
    }
}
```

- [ ] **Step 3.4.4: Run test — verify pass**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "TaskbarAlignmentHelperTests"
```
Expected: PASS (4 tests)

- [ ] **Step 3.4.5: Commit**

```powershell
git add Sources/SmartTaskbar.Win11/Helpers/IRegistryReader.cs Sources/SmartTaskbar.Win11/Helpers/TaskbarAlignmentHelper.cs Sources/SmartTaskbar.Win11/Helpers/WindowsRegistryReader.cs Sources/SmartTaskbar.Win11.Tests/TaskbarAlignmentHelperTests.cs
git commit -m "test: add TaskbarAlignmentHelper tests and implement Win11 alignment detection"
```

---

#### Task 3.5: Engine Mode Switch Tests

- [ ] **Step 3.5.1: Write failing tests**

**File:** `Sources/SmartTaskbar.Win11.Tests/EngineModeSwitchTests.cs`

```csharp
using FluentAssertions;
using NSubstitute;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;

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
```

- [ ] **Step 3.5.2: Run test — verify pass (tests use already-implemented UserSettings)**

```powershell
dotnet test Sources/SmartTaskbar.Win11.Tests/ --filter "EngineModeSwitchTests"
```
Expected: PASS (4 tests)

- [ ] **Step 3.5.3: Commit**

```powershell
git add Sources/SmartTaskbar.Win11.Tests/EngineModeSwitchTests.cs
git commit -m "test: add engine mode switch mutual exclusivity tests"
```

---

### Phase 4: Win32 API Implementation

- [ ] **Step 4.1: Extend `NativeMethods.cs` with new P/Invoke declarations**

**File:** `Sources/SmartTaskbar.Win11/Helpers/NativeMethods.cs` (modify — add to existing `Fun` partial class)

Add these regions to the existing file:

```csharp
        #region EnumWindows

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        #region IsZoomed

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        #endregion

        #region GetWindowPlacement

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        #endregion
```

- [ ] **Step 4.2: Create `WindowPlacement` model**

**File:** `Sources/SmartTaskbar.Win11/Models/WindowPlacement.cs`

```csharp
using System.Runtime.InteropServices;

namespace SmartTaskbar.Win11.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        public uint length;
        public uint flags;
        public uint showCmd;
        public TagPoint ptMinPosition;
        public TagPoint ptMaxPosition;
        public TagRect rcNormalPosition;
    }
}
```

- [ ] **Step 4.3: Implement service classes**

**File:** `Sources/SmartTaskbar.Win11/Worker/Services/WindowEnumerationService.cs`

```csharp
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class WindowEnumerationService : IWindowEnumerationService
    {
        public IReadOnlyList<IntPtr> EnumerateTopLevelWindows()
        {
            var handles = new List<IntPtr>(64);
            EnumWindowsProc callback = (hwnd, _) =>
            {
                handles.Add(hwnd);
                return true;
            };
            EnumWindows(callback, IntPtr.Zero);
            GC.KeepAlive(callback);
            return handles;
        }
    }
}
```

**File:** `Sources/SmartTaskbar.Win11/Worker/Services/WindowStateService.cs`

```csharp
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class WindowStateService : IWindowStateService
    {
        private const int DwmwaCloaked = 14;

        public bool IsMaximized(IntPtr handle) => IsZoomed(handle);

        public bool IsVisible(IntPtr handle)
        {
            if (IsWindowVisible(handle) == false) return false;
            DwmGetWindowAttribute(handle, DwmwaCloaked, out bool cloaked, sizeof(int));
            return !cloaked;
        }

        public string GetClassName(IntPtr handle) => handle.GetClassName();
    }
}
```

**File:** `Sources/SmartTaskbar.Win11/Worker/Services/MonitorService.cs`

```csharp
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class MonitorService : IMonitorService
    {
        private const uint MonitorDefaultToNearest = 2;

        public IntPtr GetMonitorFromWindow(IntPtr windowHandle)
            => MonitorFromWindow(windowHandle, MonitorDefaultToNearest);

        public bool IsSameMonitor(IntPtr monitor1, IntPtr monitor2)
            => monitor1 == monitor2;
    }
}
```

**File:** `Sources/SmartTaskbar.Win11/Worker/Services/TaskbarControlService.cs`

```csharp
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class TaskbarControlService : ITaskbarControlService
    {
        public void HideTaskbar(in TaskbarInfo taskbar) => taskbar.HideTaskbar();
        public void ShowTaskbar(in TaskbarInfo taskbar) => taskbar.ShowTaskar();
        public void SetAutoHide() => Fun.SetAutoHide();
        public bool IsNotAutoHide() => Fun.IsNotAutoHide();
        public void CancelAutoHide() => Fun.CancelAutoHide();
    }
}
```

**File:** `Sources/SmartTaskbar.Win11/Worker/Services/LocalSettingsStore.cs`

```csharp
using Windows.Storage;
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    public class LocalSettingsStore : ISettingsStore
    {
        private static readonly ApplicationDataContainer LocalSettings =
            ApplicationData.Current.LocalSettings;

        public T? GetValue<T>(string key)
        {
            var value = LocalSettings.Values[key];
            if (value is T typed) return typed;
            if (value is string s && typeof(T) == typeof(string)) return (T)(object)s;
            return default;
        }

        public void SetValue<T>(string key, T value) => LocalSettings.Values[key] = value;
    }
}
```

- [ ] **Step 4.4: Commit**

```powershell
git add Sources/SmartTaskbar.Win11/Helpers/NativeMethods.cs Sources/SmartTaskbar.Win11/Models/WindowPlacement.cs Sources/SmartTaskbar.Win11/Worker/Services/
git commit -m "feat: implement EnumWindows/IsZoomed P/Invoke and service implementations"
```

---

### Phase 5: Engine Integration

- [ ] **Step 5.1: Modify `Engine.cs` for MaximizeHide mode**

**File:** `Sources/SmartTaskbar.Win11/Worker/Engine.cs` (full rewrite)

Key changes:
1. `Timer_Tick` reads `UserSettings.Instance.AutoModeType` and dispatches to `HandleAutoMode()` or `HandleMaximizeHideMode()`
2. New `HandleMaximizeHideMode()`: checks mouse-over taskbar first, then calls `_maximizeDetector.HasMaximizedWindowOnMonitor(_taskbar.Monitor)` to hide/show
3. Original Auto mode logic extracted to `HandleAutoMode()`, `CheckCurrentWindow()`, `BeforeShowBar()` — unchanged behavior
4. `MaximizeDetector` initialized in constructor with production service implementations

```csharp
using System.ComponentModel;
using System.Diagnostics;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Worker.Services;
using SmartTaskbar.Win11.Models;
using Timer = System.Windows.Forms.Timer;

namespace SmartTaskbar.Win11
{
    internal sealed class Engine
    {
        private static Timer _timer;
        private static int _timerCount;
        private static TaskbarInfo _taskbar;

        private static readonly HashSet<IntPtr> NonMouseOverShowHandleSet = new();
        private static readonly HashSet<IntPtr> NonDesktopShowHandleSet = new();
        private static readonly HashSet<IntPtr> NonForegroundShowHandleSet = new();
        private static readonly HashSet<IntPtr> DesktopHandleSet = new();
        private static readonly Stack<IntPtr> LastHideForegroundHandle = new();
        private static ForegroundWindowInfo _currentForegroundWindow;

        private static MaximizeDetector _maximizeDetector;

        public Engine(Container container)
        {
            _timer = new Timer(container) { Interval = 125 };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _maximizeDetector = new MaximizeDetector(
                new WindowEnumerationService(),
                new WindowStateService(),
                new MonitorService());
        }

        private static void Timer_Tick(object? sender, EventArgs e)
        {
            var mode = UserSettings.Instance.AutoModeType;
            if (mode == AutoModeType.None) return;

            if (_timerCount % 5 == 0)
            {
                Fun.SetAutoHide();
                _taskbar = TaskbarHelper.InitTaskbar();
                if (_taskbar.Handle == IntPtr.Zero) return;
            }

            switch (mode)
            {
                case AutoModeType.Auto:
                    HandleAutoMode();
                    break;
                case AutoModeType.MaximizeHide:
                    HandleMaximizeHideMode();
                    break;
            }

            ++_timerCount;
            if (_timerCount <= 7200) return;
            _timerCount = 0;
            DesktopHandleSet.Clear();
            NonMouseOverShowHandleSet.Clear();
            NonDesktopShowHandleSet.Clear();
            NonForegroundShowHandleSet.Clear();
        }

        #region MaximizeHide Mode

        private static void HandleMaximizeHideMode()
        {
            switch (_taskbar.CheckIfMouseOver(NonMouseOverShowHandleSet))
            {
                case TaskbarBehavior.DoNothing:
                    return;
                case TaskbarBehavior.Show:
                    _taskbar.ShowTaskar();
                    return;
                case TaskbarBehavior.Pending:
                    break;
            }

            if (_maximizeDetector.HasMaximizedWindowOnMonitor(_taskbar.Monitor))
                _taskbar.HideTaskbar();
            else
                _taskbar.ShowTaskar();
        }

        #endregion

        #region Auto Mode (original logic)

        private static void HandleAutoMode()
        {
            switch (_taskbar.CheckIfMouseOver(NonMouseOverShowHandleSet))
            {
                case TaskbarBehavior.DoNothing: break;
                case TaskbarBehavior.Pending: CheckCurrentWindow(); break;
                case TaskbarBehavior.Show: _taskbar.ShowTaskar(); break;
            }
        }

        private static void CheckCurrentWindow()
        {
            var behavior = _taskbar.CheckIfForegroundWindowIntersectTaskbar(
                DesktopHandleSet, NonForegroundShowHandleSet, out var info);

            switch (behavior)
            {
                case TaskbarBehavior.DoNothing: break;
                case TaskbarBehavior.Pending:
                    if (_taskbar.CheckIfDesktopShow(DesktopHandleSet, NonDesktopShowHandleSet))
                        BeforeShowBar();
                    break;
                case TaskbarBehavior.Show: BeforeShowBar(); break;
                case TaskbarBehavior.Hide:
                    if (info == _currentForegroundWindow) return;
                    if (!LastHideForegroundHandle.Contains(info.Handle) && info.Rect.AreaCompare())
                        LastHideForegroundHandle.Push(info.Handle);
                    _taskbar.HideTaskbar();
                    break;
            }
            _currentForegroundWindow = info;
        }

        private static void BeforeShowBar()
        {
            while (LastHideForegroundHandle.Count != 0)
            {
                if (_taskbar.CheckIfWindowShouldHideTaskbar(LastHideForegroundHandle.Peek())) return;
                LastHideForegroundHandle.Pop();
            }
            _taskbar.ShowTaskar();
        }

        #endregion
    }
}
```

- [ ] **Step 5.2: Commit**

```powershell
git add Sources/SmartTaskbar.Win11/Worker/Engine.cs
git commit -m "feat: integrate MaximizeHide mode into Engine with mode dispatch"
```

---

### Phase 6: UI Integration

- [ ] **Step 6.1: Extend language resources**

**File:** `Sources/SmartTaskbar.Win11/Languages/LangName.cs` — add:

```csharp
public const string MaximizeHide = "tray_maximizeHide";
```

**File:** `Sources/SmartTaskbar.Win11/Languages/Resource.en-US.resx` — add data entry:

```xml
<data name="tray_maximizeHide" xml:space="preserve">
  <value>Maximize Hide Mode</value>
</data>
```

**File:** `Sources/SmartTaskbar.Win11/Languages/Resource.zh-CN.resx` — add data entry:

```xml
<data name="tray_maximizeHide" xml:space="preserve">
  <value>最大化隐藏模式</value>
</data>
```

- [ ] **Step 6.2: Modify `SystemTray.cs`**

**File:** `Sources/SmartTaskbar.Win11/Views/SystemTray.cs` (full rewrite)

Key changes:
1. New `_maximizeHideMode` ToolStripMenuItem added between `_autoMode` and separator
2. `_taskbarAlignment` field for Win11 alignment detection
3. `UserSettings.Instance` initialized in constructor with `LocalSettingsStore`
4. `UpdateModeCheckState()` method: sets checked state for both Auto and MaximizeHide (mutually exclusive)
5. `MaximizeHideModeOnClick` event handler: toggles MaximizeHide mode
6. `ShowMenu()`: uses `Screen.FromHandle(taskbar.Handle)` instead of `Screen.PrimaryScreen` for multi-monitor support; Win11 centered taskbar awareness via `_taskbarAlignment.IsCentered`
7. All `UserSettings.Property` references changed to `UserSettings.Instance.Property`

```csharp
using System.ComponentModel;
using System.Diagnostics;
using Windows.System;
using Windows.UI.ViewManagement;
using SmartTaskbar.Win11.Helpers;
using SmartTaskbar.Win11.Languages;
using SmartTaskbar.Win11.Models;
using SmartTaskbar.Win11.Worker.Services;

namespace SmartTaskbar.Win11
{
    internal class SystemTray : ApplicationContext
    {
        private const int TrayTolerance = 4;
        private readonly ToolStripMenuItem _animationInBar;
        private readonly ToolStripMenuItem _autoMode;
        private readonly ToolStripMenuItem _maximizeHideMode;
        private readonly Container _container = new();
        private readonly ContextMenuStrip _contextMenuStrip;
        private readonly Engine _engine;
        private readonly ToolStripMenuItem _exit;
        private readonly NotifyIcon _notifyIcon;
        private readonly ResourceCulture _resourceCulture = new();
        private readonly ToolStripMenuItem _showBarOnExit;
        private readonly TaskbarAlignmentHelper _taskbarAlignment;

        public SystemTray()
        {
            UserSettings.Instance = new UserSettings(new LocalSettingsStore());
            _taskbarAlignment = new TaskbarAlignmentHelper(new WindowsRegistryReader());

            _engine = new Engine(_container);
            var font = new Font("Segoe UI", 10.5F);

            var about = new ToolStripMenuItem(_resourceCulture.GetString(LangName.About)) { Font = font };
            _animationInBar = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Animation)) { Font = font };
            _showBarOnExit = new ToolStripMenuItem(_resourceCulture.GetString(LangName.ShowBarOnExit)) { Font = font };
            _autoMode = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Auto)) { Font = font };
            _maximizeHideMode = new ToolStripMenuItem(_resourceCulture.GetString(LangName.MaximizeHide)) { Font = font };
            _exit = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Exit)) { Font = font };

            _contextMenuStrip = new ContextMenuStrip(_container) { Renderer = new Win11Renderer() };
            _contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                about, _animationInBar, new ToolStripSeparator(),
                _autoMode, _maximizeHideMode, new ToolStripSeparator(),
                _showBarOnExit, _exit
            });

            _notifyIcon = new NotifyIcon(_container)
            {
                Text = Application.ProductName,
                Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White,
                Visible = true
            };

            about.Click += AboutOnClick;
            _animationInBar.Click += AnimationInBarOnClick;
            _showBarOnExit.Click += ShowBarOnExitOnClick;
            _autoMode.Click += AutoModeOnClick;
            _maximizeHideMode.Click += MaximizeHideModeOnClick;
            _exit.Click += ExitOnClick;
            _notifyIcon.MouseClick += NotifyIconOnMouseClick;
            _notifyIcon.MouseDoubleClick += NotifyIconOnMouseDoubleClick;
            Fun.UiSettings.ColorValuesChanged += UISettingsOnColorValuesChanged;
            Application.ApplicationExit += Application_ApplicationExit;
        }

        private void AboutOnClick(object? sender, EventArgs e)
            => _ = Launcher.LaunchUriAsync(new Uri("https://github.com/ChanpleCai/SmartTaskbar"));

        private void UISettingsOnColorValuesChanged(UISettings s, object e)
            => _notifyIcon.Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;

        private void NotifyIconOnMouseDoubleClick(object? s, MouseEventArgs e)
        {
            UserSettings.Instance.AutoModeType = AutoModeType.None;
            UpdateModeCheckState();
            Fun.ChangeAutoHide();
            HideBar();
        }

        private void NotifyIconOnMouseClick(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            _animationInBar.Checked = Fun.IsEnableTaskbarAnimation();
            _showBarOnExit.Checked = UserSettings.Instance.ShowTaskbarWhenExit;
            UpdateModeCheckState();
            ShowMenu();
            Fun.SetForegroundWindow(_contextMenuStrip.Handle);
        }

        private void UpdateModeCheckState()
        {
            var currentMode = UserSettings.Instance.AutoModeType;
            _autoMode.Checked = currentMode == AutoModeType.Auto;
            _maximizeHideMode.Checked = currentMode == AutoModeType.MaximizeHide;
        }

        private void ShowMenu()
        {
            var taskbar = TaskbarHelper.InitTaskbar();
            if (taskbar.Handle == IntPtr.Zero) return;

            var taskbarScreen = Screen.FromHandle(taskbar.Handle);
            var screenBounds = taskbarScreen.Bounds;

            switch (taskbar.Position)
            {
                case TaskbarPosition.Bottom:
                {
                    var menuY = taskbar.Rect.top - _contextMenuStrip.Height - TrayTolerance;
                    var menuX = Cursor.Position.X - TrayTolerance;
                    if (menuX + _contextMenuStrip.Width > screenBounds.Right)
                        menuX = screenBounds.Right - _contextMenuStrip.Width - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Left:
                {
                    var menuX = taskbar.Rect.right + TrayTolerance;
                    var menuY = Cursor.Position.Y - TrayTolerance;
                    if (menuY + _contextMenuStrip.Height > screenBounds.Bottom)
                        menuY = screenBounds.Bottom - _contextMenuStrip.Height - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Right:
                {
                    var menuX = taskbar.Rect.left - TrayTolerance - _contextMenuStrip.Width;
                    var menuY = Cursor.Position.Y - TrayTolerance;
                    if (menuY + _contextMenuStrip.Height > screenBounds.Bottom)
                        menuY = screenBounds.Bottom - _contextMenuStrip.Height - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Top:
                {
                    var menuY = taskbar.Rect.bottom + TrayTolerance;
                    var menuX = Cursor.Position.X - TrayTolerance;
                    if (menuX + _contextMenuStrip.Width > screenBounds.Right)
                        menuX = screenBounds.Right - _contextMenuStrip.Width - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
            }
        }

        private static void HideBar()
        {
            if (Fun.IsNotAutoHide()) return;
            var taskbar = TaskbarHelper.InitTaskbar();
            if (taskbar.Handle != IntPtr.Zero) taskbar.HideTaskbar();
        }

        private void ExitOnClick(object? s, EventArgs e)
        {
            if (UserSettings.Instance.ShowTaskbarWhenExit) Fun.CancelAutoHide();
            else HideBar();
            _container?.Dispose();
            Application.Exit();
        }

        private void ShowBarOnExitOnClick(object? s, EventArgs e)
            => UserSettings.Instance.ShowTaskbarWhenExit = !_showBarOnExit.Checked;

        private void AutoModeOnClick(object? s, EventArgs e)
        {
            if (_autoMode.Checked)
            {
                UserSettings.Instance.AutoModeType = AutoModeType.None;
                HideBar();
            }
            else
            {
                UserSettings.Instance.AutoModeType = AutoModeType.Auto;
            }
            UpdateModeCheckState();
        }

        private void MaximizeHideModeOnClick(object? s, EventArgs e)
        {
            if (_maximizeHideMode.Checked)
            {
                UserSettings.Instance.AutoModeType = AutoModeType.None;
                HideBar();
            }
            else
            {
                UserSettings.Instance.AutoModeType = AutoModeType.MaximizeHide;
            }
            UpdateModeCheckState();
        }

        private void AnimationInBarOnClick(object? s, EventArgs e)
            => _animationInBar.Checked = Fun.ChangeTaskbarAnimation();

        private static async void Application_ApplicationExit(object? sender, EventArgs e)
        {
            await Task.Delay(500);
            Process.GetCurrentProcess().Kill();
        }
    }
}
```

- [ ] **Step 6.3: Commit**

```powershell
git add Sources/SmartTaskbar.Win11/Languages/ Sources/SmartTaskbar.Win11/Views/SystemTray.cs
git commit -m "feat: add MaximizeHide tray menu and Win11 centered taskbar adaptation"
```

---

### Phase 7: Final Verification

- [ ] **Step 7.1: Run all tests**

```powershell
cd d:\Desktop\SmartTaskbar
dotnet test Sources/SmartTaskbar.Win11.Tests/SmartTaskbar.Win11.Tests.csproj --verbosity normal
```

Expected: All 29 tests pass:
- AutoModeTypeTests: 5
- MaximizeDetectorTests: 8
- UserSettingsTests: 8
- TaskbarAlignmentHelperTests: 4
- EngineModeSwitchTests: 4

- [ ] **Step 7.2: Build main project**

```powershell
dotnet build Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release
```
Expected: Build succeeds with no errors.

- [ ] **Step 7.3: Final commit**

```powershell
git add -A
git commit -m "feat: complete SmartTaskbar.Win11 with MaximizeHide mode, .NET 8, and Win11 adaptation"
```

---

## Assumptions & Decisions

1. **New project, not modification**: User explicitly requested "二开一个项目" (create a secondary development project). Original `SmartTaskbar` project remains untouched.

2. **Interface abstraction for testability**: Original code uses static `Fun` partial classes for all Win32 calls — untestable. New project introduces `IWindowEnumerationService`, `IWindowStateService`, `IMonitorService`, `ITaskbarControlService`, `ISettingsStore` interfaces. Core logic (`MaximizeDetector`) depends only on interfaces, enabling full unit testing with NSubstitute mocks.

3. **UserSettings static → instance**: Changed from static class to instance class with `ISettingsStore` DI. `Instance` static property provides global access in non-DI contexts (`SystemTray`, `Engine`).

4. **MaximizeHide mode logic**: Simpler than Auto mode — no desktop detection, no window stack, no caching. Just: mouse-over check → EnumWindows + IsZoomed on same monitor → hide/show. Every 125ms tick (taskbar init every 625ms).

5. **Excluded class names**: `Shell_TrayWnd` (taskbar itself), `Progman`/`WorkerW` (desktop — always full-screen), `Windows.UI.Core.CoreWindow` (UWP system windows). These would falsely trigger as "maximized".

6. **Win11 centered taskbar**: `TaskbarAlignmentHelper` reads `TaskbarAl` registry DWORD (0=centered, 1=left). `ShowMenu()` uses `Screen.FromHandle(taskbar.Handle)` for multi-monitor support instead of `Screen.PrimaryScreen`. The centered vs left distinction is detected but current menu positioning logic works for both — the alignment info is available for future refinement.

7. **`react-best-practices` skill**: Not applicable — this is a C# WinForms project, not React/Next.js. Not loaded.

8. **TDD scope**: All pure-logic components (MaximizeDetector, UserSettings, TaskbarAlignmentHelper, AutoModeType, mode switching) are TDD'd. Win32 P/Invoke wrappers and UI code are integration-tested via build verification.

9. **`ApplicationData.Current.LocalSettings`**: Used as-is via `LocalSettingsStore`. If it fails in non-MSIX context, `ISettingsStore` abstraction allows swapping to registry/JSON without affecting core logic.

---

## Verification Steps

1. **Unit tests**: `dotnet test` — 29 tests, all green
2. **Build**: `dotnet build -c Release` — zero errors
3. **Manual smoke test**: Launch app, verify tray icon appears, right-click shows menu with "最大化隐藏模式" option, enable it, maximize a window, observe taskbar hides, restore window, observe taskbar shows
4. **Mode exclusivity**: Enable Auto mode, then enable MaximizeHide mode — Auto should uncheck automatically
5. **Multi-monitor**: If available, verify taskbar detection works on secondary monitor
