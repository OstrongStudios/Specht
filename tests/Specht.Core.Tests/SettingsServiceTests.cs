using FluentAssertions;
using Specht.Core.Services;
using Xunit;

namespace Specht.Core.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempPath;

    public SettingsServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"specht-test-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Fact]
    public void Current_OnFreshFile_ReturnsDefaults()
    {
        var sut = new SettingsService(_tempPath);

        var current = sut.Current;

        current.Autostart.Should().BeFalse();
        current.ToastOnNewDevice.Should().BeTrue();
        current.Theme.Should().Be("System");
        current.Language.Should().Be("System");
        current.ScanIntervalSeconds.Should().Be(0);
        current.HiddenCategories.Should().BeEmpty();
    }

    [Fact]
    public void Update_PersistsToFile()
    {
        var sut = new SettingsService(_tempPath);

        sut.Update(s =>
        {
            s.Autostart = true;
            s.Theme = "Dark";
            s.HiddenCategories.Add("IoT");
        });

        var reloaded = new SettingsService(_tempPath);
        reloaded.Current.Autostart.Should().BeTrue();
        reloaded.Current.Theme.Should().Be("Dark");
        reloaded.Current.HiddenCategories.Should().ContainSingle().Which.Should().Be("IoT");
    }

    [Fact]
    public void Update_RaisesChangedEventWithClonedSettings()
    {
        var sut = new SettingsService(_tempPath);
        AppSettings? captured = null;
        sut.Changed += (_, s) => captured = s;

        sut.Update(s => s.Language = "en");

        captured.Should().NotBeNull();
        captured!.Language.Should().Be("en");
    }

    [Fact]
    public void Current_ReturnsClone_NotMutableLiveSettings()
    {
        var sut = new SettingsService(_tempPath);
        sut.Update(s => s.HiddenCategories.Add("Cast"));

        var snapshot = sut.Current;
        snapshot.HiddenCategories.Add("Audio"); // mutate outside Update

        sut.Current.HiddenCategories.Should().ContainSingle().Which.Should().Be("Cast");
    }

    [Fact]
    public void Load_CorruptFile_FallsBackToDefaults()
    {
        File.WriteAllText(_tempPath, "{ not valid json");

        var sut = new SettingsService(_tempPath);

        sut.Current.Autostart.Should().BeFalse();
        sut.Current.Theme.Should().Be("System");
    }
}
