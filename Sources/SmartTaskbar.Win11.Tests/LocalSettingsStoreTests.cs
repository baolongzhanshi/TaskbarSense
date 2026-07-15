using FluentAssertions;
using SmartTaskbar.Win11.Worker.Services;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class LocalSettingsStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public LocalSettingsStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SmartTaskbar.Win11.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void GetValue_BoolNullable_False_IsPreservedAcrossRestart()
    {
        var writer = new LocalSettingsStore(_settingsPath);
        writer.SetValue("ShowTaskbarWhenExit", false);

        var reader = new LocalSettingsStore(_settingsPath);
        var value = reader.GetValue<bool?>("ShowTaskbarWhenExit");

        value.Should().BeFalse();
    }

    [Fact]
    public void GetValue_String_IsPreservedAcrossRestart()
    {
        var writer = new LocalSettingsStore(_settingsPath);
        writer.SetValue("AutoModeType", "MaximizeHide");

        var reader = new LocalSettingsStore(_settingsPath);
        reader.GetValue<string>("AutoModeType").Should().Be("MaximizeHide");
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsDefault()
    {
        var store = new LocalSettingsStore(_settingsPath);
        store.GetValue<string>("MissingKey").Should().BeNull();
        store.GetValue<bool?>("MissingBool").Should().BeNull();
    }

    [Fact]
    public void SetValue_DoesNotLeaveTempFile()
    {
        var store = new LocalSettingsStore(_settingsPath);
        store.SetValue("ShowTaskbarWhenExit", true);

        File.Exists(_settingsPath).Should().BeTrue();
        File.Exists(_settingsPath + ".tmp").Should().BeFalse();
    }
}