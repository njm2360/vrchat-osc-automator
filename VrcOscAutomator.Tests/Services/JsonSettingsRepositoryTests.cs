using System.IO;
using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.Services;
using Xunit;

namespace VrcOscAutomator.Tests.Services;

public class JsonSettingsRepositoryTests : IDisposable
{
    private readonly string _dir;
    private readonly string _filePath;
    private readonly JsonSettingsRepository _sut;

    public JsonSettingsRepositoryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "VrcOscAutomatorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _filePath = Path.Combine(_dir, "settings.json");
        _sut = new JsonSettingsRepository(_filePath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); }
        catch { }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettings()
    {
        var settings = new AppSettings
        {
            Targets =
            [
                new OscTarget { IpAddress = "127.0.0.1", Port = 9000, IsEnabled = true },
                new OscTarget { IpAddress = "10.0.0.1", Port = 9001, IsEnabled = false },
            ],
            Profiles =
            [
                new Profile
                {
                    Name = "P1",
                    IsLoopMode = true,
                    Slots = [new WaitSlot(500), new IntSlot("/input/Jump", 1, 200, false, TransitionMode.None)],
                },
            ],
        };

        await _sut.SaveAsync(settings);
        SettingsLoadResult result = await _sut.LoadAsync();

        result.WasCorrupted.Should().BeFalse();
        result.Settings.Targets.Should().HaveCount(2);
        result.Settings.Targets[0].IpAddress.Should().Be("127.0.0.1");
        result.Settings.Targets[1].IsEnabled.Should().BeFalse();
        result.Settings.Profiles.Should().ContainSingle();
        result.Settings.Profiles[0].Name.Should().Be("P1");
        result.Settings.Profiles[0].IsLoopMode.Should().BeTrue();
        result.Settings.Profiles[0].Slots.Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveAsync_DoesNotLeaveTempFile()
    {
        await _sut.SaveAsync(new AppSettings());

        File.Exists(_filePath + ".tmp").Should().BeFalse();
        File.Exists(_filePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        await _sut.SaveAsync(new AppSettings { Profiles = [new Profile { Name = "First" }] });
        await _sut.SaveAsync(new AppSettings { Profiles = [new Profile { Name = "Second" }] });

        SettingsLoadResult result = await _sut.LoadAsync();

        result.Settings.Profiles.Should().ContainSingle();
        result.Settings.Profiles[0].Name.Should().Be("Second");
        File.Exists(_filePath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenFileMissing_ReturnsDefaultsWithoutCorruption()
    {
        SettingsLoadResult result = await _sut.LoadAsync();

        result.WasCorrupted.Should().BeFalse();
        result.Settings.Should().NotBeNull();
    }
}
