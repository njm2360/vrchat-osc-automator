using FluentAssertions;
using Moq;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;

namespace VrcOscAutomator.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<ISequencePlayer> _player = new();
    private readonly Mock<ISettingsRepository> _repository = new();
    private readonly Mock<IOscSender> _oscSender = new();
    private readonly Mock<IDialogService> _dialog = new();
    private readonly Mock<ISequenceImportExportService> _importExport = new();
    private readonly Mock<IGlobalHotkeyService> _hotkeys = new();
    private readonly Mock<IKeyboardSender> _keyboard = new();
    private readonly Mock<IMouseSender> _mouse = new();

    private readonly MainWindowViewModel _sut;

    public MainWindowViewModelTests()
    {
        _repository.Setup(r => r.SaveAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        _repository.Setup(r => r.LoadAsync()).ReturnsAsync(new SettingsLoadResult(new AppSettings()));

        _sut = new MainWindowViewModel(
            _player.Object, _repository.Object, _oscSender.Object,
            _dialog.Object, _importExport.Object,
            _hotkeys.Object, _keyboard.Object, _mouse.Object);
    }

    // ─── 初期状態 ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_CreatesOneDefaultProfile()
    {
        _sut.Profiles.Should().ContainSingle();
        _sut.Profiles[0].Name.Should().Be("Profile 1");
    }

    // ─── AddProfileCommand ────────────────────────────────────────────────

    [Fact]
    public async Task AddProfile_IncreasesProfileCount()
    {
        await _sut.AddProfileCommand.ExecuteAsync(null);

        _sut.Profiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddProfile_SelectsNewProfile()
    {
        await _sut.AddProfileCommand.ExecuteAsync(null);

        _sut.SelectedProfileIndex.Should().Be(1);
    }

    [Fact]
    public async Task AddProfile_AssignsUniqueAutoName()
    {
        await _sut.AddProfileCommand.ExecuteAsync(null);
        await _sut.AddProfileCommand.ExecuteAsync(null);

        var names = _sut.Profiles.Select(p => p.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AddProfile_CallsSave()
    {
        await _sut.AddProfileCommand.ExecuteAsync(null);

        _repository.Verify(r => r.SaveAsync(It.IsAny<AppSettings>()), Times.Once);
    }

    [Fact]
    public async Task AddProfile_WhenNameCollides_GeneratesDistinctName()
    {
        // デフォルトが "Profile 1" なので "Profile 2" が衝突しないことを確認
        await _sut.AddProfileCommand.ExecuteAsync(null);
        string secondName = _sut.Profiles[1].Name;

        secondName.Should().NotBe(_sut.Profiles[0].Name);
    }

    [Fact]
    public void AddProfile_CanExecute_TrueWhenNotPlaying()
    {
        _sut.IsPlaying = false;

        _sut.AddProfileCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddProfile_CanExecute_FalseWhilePlaying()
    {
        _sut.IsPlaying = true;

        _sut.AddProfileCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddProfile_CanExecute_BecomesTrueAfterStopping()
    {
        _sut.IsPlaying = true;
        _sut.AddProfileCommand.CanExecute(null).Should().BeFalse();

        _sut.IsPlaying = false;

        _sut.AddProfileCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void DeleteProfile_CanExecute_FalseWhilePlaying()
    {
        _sut.IsPlaying = true;

        _sut.DeleteProfileCommand.CanExecute(_sut.Profiles[0]).Should().BeFalse();
    }

    // ─── DeleteProfileCommand ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteProfile_Cancelled_NoChange()
    {
        _dialog.Setup(d => d.ConfirmDeleteProfile(It.IsAny<string>())).Returns(false);
        ProfileViewModel target = _sut.Profiles[0];

        await _sut.DeleteProfileCommand.ExecuteAsync(target);

        _sut.Profiles.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteProfile_Confirmed_RemovesProfile()
    {
        await _sut.AddProfileCommand.ExecuteAsync(null);
        _dialog.Setup(d => d.ConfirmDeleteProfile(It.IsAny<string>())).Returns(true);
        ProfileViewModel target = _sut.Profiles[0];

        await _sut.DeleteProfileCommand.ExecuteAsync(target);

        _sut.Profiles.Should().NotContain(target);
    }

    [Fact]
    public async Task DeleteProfile_Confirmed_CallsSave()
    {
        await _sut.AddProfileCommand.ExecuteAsync(null);
        _dialog.Setup(d => d.ConfirmDeleteProfile(It.IsAny<string>())).Returns(true);

        await _sut.DeleteProfileCommand.ExecuteAsync(_sut.Profiles[0]);

        // AddProfile で1回 + DeleteProfile で1回
        _repository.Verify(r => r.SaveAsync(It.IsAny<AppSettings>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteProfile_Middle_AdjustsSelectedIndex()
    {
        await _sut.AddProfileCommand.ExecuteAsync(null);
        await _sut.AddProfileCommand.ExecuteAsync(null);
        // 3件: index=0,1,2 → index=1 を削除
        _sut.SelectedProfileIndex = 1;
        _dialog.Setup(d => d.ConfirmDeleteProfile(It.IsAny<string>())).Returns(true);
        ProfileViewModel middle = _sut.Profiles[1];

        await _sut.DeleteProfileCommand.ExecuteAsync(middle);

        _sut.SelectedProfileIndex.Should().BeInRange(0, _sut.Profiles.Count - 1);
    }

    [Fact]
    public async Task DeleteProfile_LastRemaining_SetsIndexToMinusOne()
    {
        _dialog.Setup(d => d.ConfirmDeleteProfile(It.IsAny<string>())).Returns(true);

        await _sut.DeleteProfileCommand.ExecuteAsync(_sut.Profiles[0]);

        _sut.Profiles.Should().BeEmpty();
        _sut.SelectedProfileIndex.Should().Be(-1);
    }

    [Fact]
    public async Task DeleteProfile_ShowsConfirmDialog_WithProfileName()
    {
        _sut.Profiles[0].Name = "MyProfile";
        _dialog.Setup(d => d.ConfirmDeleteProfile("MyProfile")).Returns(false);

        await _sut.DeleteProfileCommand.ExecuteAsync(_sut.Profiles[0]);

        _dialog.Verify(d => d.ConfirmDeleteProfile("MyProfile"), Times.Once);
    }

    // ─── CanStart / StatusMessage (空状態ガード) ──────────────────────────

    [Fact]
    public void CanStart_WhenNoProfiles_IsFalse()
    {
        _dialog.Setup(d => d.ConfirmDeleteProfile(It.IsAny<string>())).Returns(true);
        _ = _sut.DeleteProfileCommand.ExecuteAsync(_sut.Profiles[0]);

        _sut.StartCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void StatusMessage_WhenNoProfiles_IsEmpty()
    {
        _dialog.Setup(d => d.ConfirmDeleteProfile(It.IsAny<string>())).Returns(true);
        _ = _sut.DeleteProfileCommand.ExecuteAsync(_sut.Profiles[0]);

        _sut.StatusMessage.Should().BeEmpty();
    }

    // ─── LoadedAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task LoadedAsync_ReplacesDefaultProfilesWithSettings()
    {
        _repository.Setup(r => r.LoadAsync()).ReturnsAsync(new SettingsLoadResult(new AppSettings
        {
            Profiles =
            [
                new() { Name = "A" },
                new() { Name = "B" },
                new() { Name = "C" },
            ],
        }));

        await _sut.LoadedCommand.ExecuteAsync(null);

        _sut.Profiles.Should().HaveCount(3);
        _sut.Profiles.Select(p => p.Name).Should().Equal("A", "B", "C");
    }

    [Fact]
    public async Task LoadedAsync_EmptyProfilesInSettings_CreatesDefaultProfile()
    {
        _repository.Setup(r => r.LoadAsync()).ReturnsAsync(new SettingsLoadResult(new AppSettings { Profiles = [] }));

        await _sut.LoadedCommand.ExecuteAsync(null);

        _sut.Profiles.Should().ContainSingle();
        _sut.Profiles[0].Name.Should().Be("Profile 1");
    }

    [Fact]
    public async Task LoadedAsync_SetsSelectedIndexToZero()
    {
        _repository.Setup(r => r.LoadAsync()).ReturnsAsync(new SettingsLoadResult(new AppSettings
        {
            Profiles = [new() { Name = "X" }, new() { Name = "Y" }],
        }));

        await _sut.LoadedCommand.ExecuteAsync(null);

        _sut.SelectedProfileIndex.Should().Be(0);
    }
}
