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
        _importExport.Setup(s => s.Export(It.IsAny<IEnumerable<SequenceSlot>>())).Returns("base64data");

        _sut.ExportCommand.Execute(null);

        _importExport.Verify(s => s.Export(It.IsAny<IEnumerable<SequenceSlot>>()), Times.Once);
        _dialog.Verify(d => d.ShowExportDialog("base64data"), Times.Once);
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
    public void Import_InvalidBase64_ShowsError()
    {
        _dialog.Setup(d => d.ShowImportDialog()).Returns("!!!invalid!!!");
        _importExport.Setup(s => s.Import("!!!invalid!!!")).Throws<FormatException>();

        _sut.ImportCommand.Execute(null);

        _dialog.Verify(d => d.ShowError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Import_EmptySlotList_ShowsError()
    {
        _dialog.Setup(d => d.ShowImportDialog()).Returns("validbase64");
        _importExport.Setup(s => s.Import("validbase64")).Returns([]);

        _sut.ImportCommand.Execute(null);

        _dialog.Verify(d => d.ShowError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Import_ExistingSlotsAndOverwriteDenied_NoChange()
    {
        _sut.AddSlotCommand.Execute(null);
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data")).Returns(
            [new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int }]);
        _dialog.Setup(d => d.ConfirmOverwrite()).Returns(false);

        _sut.ImportCommand.Execute(null);

        _sut.Slots.Should().HaveCount(1); // 元のスロットのまま
    }

    [Fact]
    public void Import_ExistingSlotsAndOverwriteConfirmed_ReplacesSlots()
    {
        _sut.AddSlotCommand.Execute(null);
        var importedSlots = new[]
        {
            new SequenceSlot { Address = "/input/Jump",  Value = 1f, ValueType = OscValueType.Int },
            new SequenceSlot { Address = "/input/Voice", Value = 1f, ValueType = OscValueType.Int },
        };
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data")).Returns(importedSlots);
        _dialog.Setup(d => d.ConfirmOverwrite()).Returns(true);

        _sut.ImportCommand.Execute(null);

        _sut.Slots.Should().HaveCount(2);
    }

    [Fact]
    public void Import_NoExistingSlots_NoConfirmRequired()
    {
        var importedSlots = new[]
        {
            new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int },
        };
        _dialog.Setup(d => d.ShowImportDialog()).Returns("data");
        _importExport.Setup(s => s.Import("data")).Returns(importedSlots);

        _sut.ImportCommand.Execute(null);

        _dialog.Verify(d => d.ConfirmOverwrite(), Times.Never);
        _sut.Slots.Should().ContainSingle();
    }

    // ─── ToModel / LoadFromModel ──────────────────────────────────────────

    [Fact]
    public void ToModel_ReturnsProfileWithCorrectNameAndSlots()
    {
        _sut.Name = "MyProfile";
        _sut.AddSlotCommand.Execute(null);

        Profile model = _sut.ToModel();

        model.Name.Should().Be("MyProfile");
        model.Slots.Should().ContainSingle();
    }

    [Fact]
    public void LoadFromModel_ReplacesNameAndSlots()
    {
        _sut.AddSlotCommand.Execute(null);

        var profile = new Profile
        {
            Name = "Loaded",
            Slots =
            [
                new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int },
                new SequenceSlot { Address = null, DurationMs = 500 },
            ],
        };

        _sut.LoadFromModel(profile);

        _sut.Name.Should().Be("Loaded");
        _sut.Slots.Should().HaveCount(2);
    }

    [Fact]
    public void LoadFromModel_ThenToModel_RoundTrip()
    {
        var profile = new Profile
        {
            Name = "RT",
            Slots =
            [
                new SequenceSlot { SlotType = SlotType.LoopBegin, RepeatCount = 3 },
                new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int, DurationMs = 200 },
                new SequenceSlot { SlotType = SlotType.LoopEnd },
            ],
        };

        _sut.LoadFromModel(profile);
        Profile restored = _sut.ToModel();

        restored.Should().BeEquivalentTo(profile);
    }
}
