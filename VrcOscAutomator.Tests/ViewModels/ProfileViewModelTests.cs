using FluentAssertions;
using Moq;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;

namespace VrcOscAutomator.Tests.ViewModels;

public class ProfileViewModelTests
{
    private readonly Mock<IDialogService> _dialog = new();
    private readonly Mock<ISequenceImportExportService> _importExport = new();
    private readonly ProfileViewModel _sut;

    public ProfileViewModelTests()
    {
        _sut = new ProfileViewModel(_dialog.Object, _importExport.Object);
    }

    // ─── AddSlot / RemoveSlot ─────────────────────────────────────────────

    [Fact]
    public void AddSlot_AppendsNewSlot()
    {
        _sut.AddSlotCommand.Execute(null);

        _sut.Slots.Should().ContainSingle();
    }

    [Fact]
    public void AddSlot_InsertsAfterSelectedSlot_WhenSelectionExists()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        SequenceSlotViewModel first = _sut.Slots[0];
        SequenceSlotViewModel second = _sut.Slots[1];
        _sut.SelectedSlot = first;

        _sut.AddSlotCommand.Execute(null);

        _sut.Slots.Should().HaveCount(3);
        _sut.Slots[0].Should().Be(first);
        _sut.Slots[2].Should().Be(second);
        _sut.SelectedSlot.Should().Be(_sut.Slots[1]);
    }

    [Fact]
    public void AddSlot_AppendsToEnd_WhenNoSelection()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        SequenceSlotViewModel last = _sut.Slots[1];
        _sut.SelectedSlot = null;

        _sut.AddSlotCommand.Execute(null);

        _sut.Slots.Should().HaveCount(3);
        _sut.Slots[1].Should().Be(last);
        _sut.SelectedSlot.Should().Be(_sut.Slots[2]);
    }

    [Fact]
    public void RemoveSlot_CanExecute_FalseWhenNothingSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = null;

        _sut.RemoveSlotCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RemoveSlot_CanExecute_TrueWhenSlotSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[0];

        _sut.RemoveSlotCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RemoveSlot_RemovesSelectedSlot()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        SequenceSlotViewModel target = _sut.Slots[0];
        _sut.SelectedSlot = target;

        _sut.RemoveSlotCommand.Execute(null);

        _sut.Slots.Should().NotContain(target);
        _sut.Slots.Should().HaveCount(1);
    }

    // ─── CopySlot ─────────────────────────────────────────────────────────

    [Fact]
    public void CopySlot_CanExecute_FalseWhenNothingSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = null;

        _sut.CopySlotCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CopySlot_CanExecute_TrueWhenSlotSelected()
    {
        _sut.AddSlotCommand.Execute(null);

        _sut.CopySlotCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CopySlot_InsertsNewSlotAfterSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        SequenceSlotViewModel first = _sut.Slots[0];
        SequenceSlotViewModel second = _sut.Slots[1];
        _sut.SelectedSlot = first;

        _sut.CopySlotCommand.Execute(null);

        _sut.Slots.Should().HaveCount(3);
        _sut.Slots[0].Should().Be(first);
        _sut.Slots[2].Should().Be(second);
    }

    [Fact]
    public void CopySlot_SelectsNewCopy()
    {
        _sut.AddSlotCommand.Execute(null);
        SequenceSlotViewModel original = _sut.Slots[0];

        _sut.CopySlotCommand.Execute(null);

        _sut.SelectedSlot.Should().NotBeNull();
        _sut.SelectedSlot.Should().NotBe(original);
        _sut.Slots[1].Should().Be(_sut.SelectedSlot);
    }

    [Fact]
    public void CopySlot_CopiesSlotContent()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.Slots[0].DurationMs = 1234;

        _sut.CopySlotCommand.Execute(null);

        _sut.Slots[1].DurationMs.Should().Be(1234);
    }

    // ─── MoveUp / MoveDown ────────────────────────────────────────────────

    [Fact]
    public void MoveUp_CanExecute_FalseWhenNothingSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = null;

        _sut.MoveUpCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void MoveDown_CanExecute_FalseWhenNothingSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);

        _sut.MoveDownCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void MoveUp_CanExecute_FalseWhenFirstSlotSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[0];

        _sut.MoveUpCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void MoveUp_CanExecute_TrueWhenNotFirstSlotSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[1];

        _sut.MoveUpCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void MoveDown_CanExecute_FalseWhenLastSlotSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[1];

        _sut.MoveDownCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void MoveDown_CanExecute_TrueWhenNotLastSlotSelected()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[0];

        _sut.MoveDownCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void MoveUp_AfterMovingToTop_CanExecuteBecomeseFalse()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[1];

        _sut.MoveUpCommand.Execute(null);

        _sut.MoveUpCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void MoveDown_AfterMovingToBottom_CanExecuteBecomesFalse()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[0];

        _sut.MoveDownCommand.Execute(null);

        _sut.MoveDownCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void MoveUp_MovesSlotUpByOne()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        SequenceSlotViewModel first = _sut.Slots[0];
        SequenceSlotViewModel second = _sut.Slots[1];
        _sut.SelectedSlot = second;

        _sut.MoveUpCommand.Execute(null);

        _sut.Slots[0].Should().Be(second);
        _sut.Slots[1].Should().Be(first);
    }

    [Fact]
    public void MoveDown_MovesSlotDownByOne()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.AddSlotCommand.Execute(null);
        SequenceSlotViewModel first = _sut.Slots[0];
        SequenceSlotViewModel second = _sut.Slots[1];
        _sut.SelectedSlot = first;

        _sut.MoveDownCommand.Execute(null);

        _sut.Slots[0].Should().Be(second);
        _sut.Slots[1].Should().Be(first);
    }

    // ─── AllSlotsValid ────────────────────────────────────────────────────

    [Fact]
    public void AllSlotsValid_EmptySlots_True()
    {
        _sut.AllSlotsValid.Should().BeTrue();
    }

    [Fact]
    public void AllSlotsValid_AllValidSlots_True()
    {
        _sut.AddSlotCommand.Execute(null); // デフォルト = FloatMovement → IsValid = true

        _sut.AllSlotsValid.Should().BeTrue();
    }

    [Fact]
    public void AllSlotsValid_CustomSlotWithNoAddress_False()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.Slots[0].SelectedPreset = SlotPreset.All.First(p => p.IsCustom);
        _sut.Slots[0].CustomAddress = ""; // IsValid = false

        _sut.AllSlotsValid.Should().BeFalse();
    }

    [Fact]
    public void AllSlotsValid_UpdatesWhenSlotBecomesValid()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.Slots[0].SelectedPreset = SlotPreset.All.First(p => p.IsCustom);
        _sut.Slots[0].CustomAddress = "";
        _sut.AllSlotsValid.Should().BeFalse();

        _sut.Slots[0].CustomAddress = "/valid/addr";

        _sut.AllSlotsValid.Should().BeTrue();
    }

    [Fact]
    public void AllSlotsValid_RaisesPropertyChanged_WhenSlotValidityChanges()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.Slots[0].SelectedPreset = SlotPreset.All.First(p => p.IsCustom);

        var changed = new List<string?>();
        _sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _sut.Slots[0].CustomAddress = "/ok";

        changed.Should().Contain(nameof(ProfileViewModel.AllSlotsValid));
    }

    [Fact]
    public void AllSlotsValid_UpdatesWhenSlotAdded()
    {
        var changed = new List<string?>();
        _sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _sut.AddSlotCommand.Execute(null);

        changed.Should().Contain(nameof(ProfileViewModel.AllSlotsValid));
    }

    [Fact]
    public void AllSlotsValid_UpdatesWhenSlotRemoved()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.SelectedSlot = _sut.Slots[0];

        var changed = new List<string?>();
        _sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _sut.RemoveSlotCommand.Execute(null);

        changed.Should().Contain(nameof(ProfileViewModel.AllSlotsValid));
    }

    // ─── Export ───────────────────────────────────────────────────────────

    [Fact]
    public void Export_CallsImportExportServiceAndShowsDialog()
    {
        _sut.AddSlotCommand.Execute(null);
        _importExport.Setup(s => s.Export(It.IsAny<string>(), It.IsAny<IEnumerable<SequenceSlot>>(), It.IsAny<bool>())).Returns("jsondata");

        _sut.ExportCommand.Execute(null);

        _importExport.Verify(s => s.Export(It.IsAny<string>(), It.IsAny<IEnumerable<SequenceSlot>>(), It.IsAny<bool>()), Times.Once);
        _dialog.Verify(d => d.ShowExportDialog("jsondata"), Times.Once);
    }

    [Fact]
    public void Export_PassesIsLoopModeToService()
    {
        _sut.AddSlotCommand.Execute(null);
        _sut.IsLoopMode = true;
        _importExport.Setup(s => s.Export(It.IsAny<string>(), It.IsAny<IEnumerable<SequenceSlot>>(), true)).Returns("jsondata");

        _sut.ExportCommand.Execute(null);

        _importExport.Verify(s => s.Export(It.IsAny<string>(), It.IsAny<IEnumerable<SequenceSlot>>(), true), Times.Once);
    }

    // ─── Import ───────────────────────────────────────────────────────────

    [Fact]
    public void Import_DialogCancelled_NoChange()
    {
        _sut.AddSlotCommand.Execute(null);
        _dialog.Setup(d => d.ShowImportDialog()).Returns((string?)null);

        _sut.ImportCommand.Execute(null);

        _sut.Slots.Should().HaveCount(1);
    }

    [Fact]
    public void Import_DialogReturnsEmpty_NoChange()
    {
        _sut.AddSlotCommand.Execute(null);
        _dialog.Setup(d => d.ShowImportDialog()).Returns("");

        _sut.ImportCommand.Execute(null);

        _sut.Slots.Should().HaveCount(1);
    }

    [Fact]
    public void Import_InvalidJson_ShowsError()
    {
        _dialog.Setup(d => d.ShowImportDialog()).Returns("{ not json }");
        _importExport.Setup(s => s.Import("{ not json }")).Throws<System.Text.Json.JsonException>();

        _sut.ImportCommand.Execute(null);

        _dialog.Verify(d => d.ShowError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Import_EmptySlotList_ShowsError()
    {
        _dialog.Setup(d => d.ShowImportDialog()).Returns("validjson");
        _importExport.Setup(s => s.Import("validjson")).Returns((ProfileExportData?)null);

        _sut.ImportCommand.Execute(null);

        _dialog.Verify(d => d.ShowError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Import_ExistingSlotsAndOverwriteDenied_NoChange()
    {
        _sut.AddSlotCommand.Execute(null);
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data")).Returns(
            new ProfileExportData("", [new IntSlot("/input/Jump", 1, 500, true, TransitionMode.None)], false));
        _dialog.Setup(d => d.ConfirmOverwrite()).Returns(false);

        _sut.ImportCommand.Execute(null);

        _sut.Slots.Should().HaveCount(1); // 元のスロットのまま
    }

    [Fact]
    public void Import_ExistingSlotsAndOverwriteConfirmed_ReplacesSlots()
    {
        _sut.AddSlotCommand.Execute(null);
        var importedSlots = new SequenceSlot[]
        {
            new IntSlot("/input/Jump",  1, 500, true, TransitionMode.None),
            new IntSlot("/input/Voice", 1, 500, true, TransitionMode.None),
        };
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data")).Returns(new ProfileExportData("", [.. importedSlots], false));
        _dialog.Setup(d => d.ConfirmOverwrite()).Returns(true);

        _sut.ImportCommand.Execute(null);

        _sut.Slots.Should().HaveCount(2);
    }

    [Fact]
    public void Import_NoExistingSlots_NoConfirmRequired()
    {
        var importedSlots = new SequenceSlot[] { new IntSlot("/input/Jump", 1, 500, true, TransitionMode.None) };
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data")).Returns(new ProfileExportData("", [.. importedSlots], false));

        _sut.ImportCommand.Execute(null);

        _dialog.Verify(d => d.ConfirmOverwrite(), Times.Never);
        _sut.Slots.Should().ContainSingle();
    }

    // ─── ToModel / LoadFromModel ──────────────────────────────────────────

    [Fact]
    public void ToModel_ReturnsProfileWithCorrectNameAndSlots()
    {
        _sut.Name = "MyProfile";
        _sut.IsLoopMode = true;
        _sut.AddSlotCommand.Execute(null);

        Profile model = _sut.ToModel();

        model.Name.Should().Be("MyProfile");
        model.IsLoopMode.Should().BeTrue();
        model.Slots.Should().ContainSingle();
    }

    [Fact]
    public void LoadFromModel_ReplacesNameAndSlots()
    {
        _sut.AddSlotCommand.Execute(null);

        var profile = new Profile
        {
            Name = "Loaded",
            IsLoopMode = true,
            Slots =
            [
                new IntSlot("/input/Jump", 1, 500, true, TransitionMode.None),
                new WaitSlot(500),
            ],
        };

        _sut.LoadFromModel(profile);

        _sut.Name.Should().Be("Loaded");
        _sut.IsLoopMode.Should().BeTrue();
        _sut.Slots.Should().HaveCount(2);
    }

    [Fact]
    public void LoadFromModel_ThenToModel_RoundTrip()
    {
        var profile = new Profile
        {
            Name = "RT",
            IsLoopMode = true,
            Slots =
            [
                new LoopBeginSlot(3),
                new IntSlot("/input/Jump", 1, 200, true, TransitionMode.None),
                new LoopEndSlot(),
            ],
        };

        _sut.LoadFromModel(profile);
        Profile restored = _sut.ToModel();

        restored.Should().BeEquivalentTo(profile, o => o.RespectingRuntimeTypes());
    }

    // ─── Rename ───────────────────────────────────────────────────────────

    [Fact]
    public void BeginRename_SetsIsRenamingTrue()
    {
        _sut.Name = "Before";

        _sut.BeginRename();

        _sut.IsRenaming.Should().BeTrue();
    }

    [Fact]
    public void CommitRename_SetsIsRenamingFalse()
    {
        _sut.Name = "Before";
        _sut.BeginRename();
        _sut.Name = "After";

        _sut.CommitRename();

        _sut.IsRenaming.Should().BeFalse();
    }

    [Fact]
    public void CommitRename_PreservesNewName()
    {
        _sut.Name = "Before";
        _sut.BeginRename();
        _sut.Name = "After";

        _sut.CommitRename();

        _sut.Name.Should().Be("After");
    }

    [Fact]
    public void CommitRename_TrimsWhitespace()
    {
        _sut.Name = "Before";
        _sut.BeginRename();
        _sut.Name = "  Trimmed  ";

        _sut.CommitRename();

        _sut.Name.Should().Be("Trimmed");
    }

    [Fact]
    public void CommitRename_EmptyName_RevertsToOriginal()
    {
        _sut.Name = "Original";
        _sut.BeginRename();
        _sut.Name = "   ";

        _sut.CommitRename();

        _sut.Name.Should().Be("Original");
    }

    [Fact]
    public void CancelRename_RevertsName()
    {
        _sut.Name = "Before";
        _sut.BeginRename();
        _sut.Name = "Changed";

        _sut.CancelRename();

        _sut.Name.Should().Be("Before");
    }

    [Fact]
    public void CancelRename_SetsIsRenamingFalse()
    {
        _sut.Name = "Before";
        _sut.BeginRename();

        _sut.CancelRename();

        _sut.IsRenaming.Should().BeFalse();
    }

    [Fact]
    public void BeginRename_RaisesPropertyChanged_ForIsRenaming()
    {
        var changed = new List<string?>();
        _sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _sut.BeginRename();

        changed.Should().Contain(nameof(ProfileViewModel.IsRenaming));
    }

    // ─── Import: 名前の反映 ────────────────────────────────────────────────

    [Fact]
    public void Import_WithNonEmptyName_SetsProfileName()
    {
        _sut.Name = "OldName";
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data"))
            .Returns(new ProfileExportData("NewName", [new IntSlot("/x", 1, 500, true, TransitionMode.None)], false));

        _sut.ImportCommand.Execute(null);

        _sut.Name.Should().Be("NewName");
    }

    [Fact]
    public void Import_WithEmptyName_KeepsExistingProfileName()
    {
        _sut.Name = "KeepMe";
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data"))
            .Returns(new ProfileExportData("", [new IntSlot("/x", 1, 500, true, TransitionMode.None)], false));

        _sut.ImportCommand.Execute(null);

        _sut.Name.Should().Be("KeepMe");
    }

    [Fact]
    public void Import_SetsIsLoopModeFromData()
    {
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data"))
            .Returns(new ProfileExportData("", [new IntSlot("/x", 1, 500, true, TransitionMode.None)], true));

        _sut.ImportCommand.Execute(null);

        _sut.IsLoopMode.Should().BeTrue();
    }
}
