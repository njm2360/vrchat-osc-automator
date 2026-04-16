using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class ProfileViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ISequenceImportExportService _importExport;

    public ProfileViewModel(IDialogService dialogService, ISequenceImportExportService importExport)
    {
        _dialogService = dialogService;
        _importExport = importExport;
        Slots = [];
        Slots.CollectionChanged += OnSlotsCollectionChanged;
    }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSlotCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopySlotCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    public partial SequenceSlotViewModel? SelectedSlot { get; set; }

    public ObservableCollection<SequenceSlotViewModel> Slots { get; }

    /// <summary>全スロットが有効であれば true。</summary>
    public bool AllSlotsValid => Slots.All(s => s.IsValid);

    private void OnSlotsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (SequenceSlotViewModel slot in e.NewItems)
                slot.PropertyChanged += OnSlotPropertyChanged;

        if (e.OldItems is not null)
            foreach (SequenceSlotViewModel slot in e.OldItems)
                slot.PropertyChanged -= OnSlotPropertyChanged;

        OnPropertyChanged(nameof(AllSlotsValid));
        ExportCommand.NotifyCanExecuteChanged();
    }

    private void OnSlotPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SequenceSlotViewModel.IsValid))
            OnPropertyChanged(nameof(AllSlotsValid));
    }

    [RelayCommand]
    private void AddSlot()
    {
        var newSlot = new SequenceSlotViewModel();
        int insertAt = SelectedSlot is not null
            ? Slots.IndexOf(SelectedSlot) + 1
            : Slots.Count;
        Slots.Insert(insertAt, newSlot);
        SelectedSlot = newSlot;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSlot))]
    private void RemoveSlot()
    {
        if (SelectedSlot is not null)
            Slots.Remove(SelectedSlot);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSlot))]
    private void CopySlot()
    {
        if (SelectedSlot is null) return;
        var copy = SequenceSlotViewModel.FromModel(SelectedSlot.ToModel());
        int insertAt = Slots.IndexOf(SelectedSlot) + 1;
        Slots.Insert(insertAt, copy);
        SelectedSlot = copy;
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        int i = Slots.IndexOf(SelectedSlot!);
        Slots.Move(i, i - 1);
        NotifyMoveCommandsCanExecute();
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        int i = Slots.IndexOf(SelectedSlot!);
        Slots.Move(i, i + 1);
        NotifyMoveCommandsCanExecute();
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        string base64 = _importExport.Export(Slots.Select(s => s.ToModel()));
        _dialogService.ShowExportDialog(base64);
    }

    private bool CanExport() => Slots.Count > 0;

    [RelayCommand]
    private void Import()
    {
        string? input = _dialogService.ShowImportDialog();
        if (input is null or { Length: 0 }) return;

        IReadOnlyList<SequenceSlot>? slots;
        try
        {
            slots = _importExport.Import(input);
        }
        catch (Exception ex) when (ex is JsonException)
        {
            _dialogService.ShowError("インポートデータの形式が正しくありません。");
            return;
        }

        if (slots is null or { Count: 0 })
        {
            _dialogService.ShowError("スロットが含まれていないデータです。");
            return;
        }

        if (Slots.Count > 0 && !_dialogService.ConfirmOverwrite())
            return;

        Slots.Clear();
        foreach (SequenceSlot slot in slots)
            Slots.Add(SequenceSlotViewModel.FromModel(slot));
    }

    private bool HasSelectedSlot => SelectedSlot is not null;

    private bool CanMoveUp =>
        SelectedSlot is not null && Slots.IndexOf(SelectedSlot) > 0;

    private bool CanMoveDown =>
        SelectedSlot is not null && Slots.IndexOf(SelectedSlot) < Slots.Count - 1;

    private void NotifyMoveCommandsCanExecute()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    public Profile ToModel() => new()
    {
        Name = Name,
        Slots = [.. Slots.Select(s => s.ToModel())],
    };

    public void LoadFromModel(Profile profile)
    {
        Name = profile.Name;
        Slots.Clear();
        foreach (SequenceSlot slot in profile.Slots)
            Slots.Add(SequenceSlotViewModel.FromModel(slot));
    }
}
