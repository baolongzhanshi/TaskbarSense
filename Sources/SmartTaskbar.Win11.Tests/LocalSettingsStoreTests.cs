using FluentAssertions;
using SmartTaskbar.Win11.Worker.Services;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class LocalSettingsStoreTests : IDisposable
{
    private readonly string _backupPath;
    private readonly string _settingsDir;
    private readonly string _settingsPath;

    public LocalSettingsStoreTests()
    {
        _settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartTaskbar.Win11");
        _settingsPath = Path.Combine(_settingsDir, "settings.json");
        _backupPath = _settingsPath + ".bak-test";

        Directory.CreateDirectory(_settingsDir);
        if (File.Exists(_settingsPath))
            File.Copy(_settingsPath, _backupPath, true);
        if (File.Exists(_settingsPath))
            File.Delete(_settingsPath);
    }

    public void Dispose()
    {
        if (File.Exists(_settingsPath))
            File.Delete(_settingsPath);

        if (File.Exists(_backupPath))
        {
            File.Copy(_backupPath, _settingsPath, true);
            File.Delete(_backupPath);
        }
    }

    [Fact]
    public void GetValue_BoolNullable_False_IsPreservedAcrossRestart()
    {
        var writer = new LocalSettingsStore();
        writer.SetValue("ShowTaskbarWhenExit", false);

        var reader = new LocalSettingsStore();
        var value = reader.GetValue<bool?>("ShowTaskbarWhenExit");

        value.Should().BeFalse();
    }

    [Fact]
    public void GetValue_String_IsPreservedAcrossRestart()
    {
        var writer = new LocalSettingsStore();
        writer.SetValue("AutoModeType", "MaximizeHide");

        var reader = new LocalSettingsStore();
        reader.GetValue<string>("AutoModeType").Should().Be("MaximizeHide");
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsDefault()
    {
        var store = new LocalSettingsStore();
        store.GetValue<string>("MissingKey").Should().BeNull();
        store.GetValue<bool?>("MissingBool").Should().BeNull();
    }
}