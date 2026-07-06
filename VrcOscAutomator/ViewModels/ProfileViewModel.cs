using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcOscAutomator.Exceptions;
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
        // スロットの追加・削除時にAllSlotsValidとExportCommandを再評価
        Slots.CollectionChanged += OnSlotsCollectionChanged;
    }

    // ── プロファイル ──────────────────────────────────────────────────────

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoopMode { get; set; }

    // ── リネーム処理 ──────────────────────────────────────────────────────

    [ObservableProperty]
    public partial bool IsRenaming { get; set; }

    private string _nameBeforeRename = string.Empty;

    public void BeginRename() { _nameBeforeRename = Name; IsRenaming = true; }
    public void CommitRename() { string t = Name.Trim(); Name = t.Length > 0 ? t : _nameBeforeRename; IsRenaming = false; }
    public void CancelRename() { Name = _nameBeforeRename; IsRenaming = false; }

    // ── スロット一覧 ──────────────────────────────────────────────────────

    // DataGrid で選択中のスロット（null = 未選択）
    [ObservableProperty]
    public partial SequenceSlotViewModel? SelectedSlot { get; set; }

    // SelectedSlot変更時に_selectedSlotsを単一選択として同期
    partial void OnSelectedSlotChanged(SequenceSlotViewModel? value)
    {
        _selectedSlots = value is null ? [] : [value];
        NotifySelectionCommandsCanExecute();
    }

    // DataGridの複数選択（code-behindからSetSelectedSlotsで更新）
    private IReadOnlyList<SequenceSlotViewModel> _selectedSlots = [];

    // コピー後などにプログラムから複数選択を要求するイベント
    public event EventHandler<IList<SequenceSlotViewModel>>? SelectionRequested;

    public void SetSelectedSlots(IReadOnlyList<SequenceSlotViewModel> slots)
    {
        _selectedSlots = slots;
        NotifySelectionCommandsCanExecute();
    }

    public ObservableCollection<SequenceSlotViewModel> Slots { get; }

    // 全スロットのアドレスが有効であれば true
    public bool AllSlotsValid => Slots.All(s => s.IsValid);

    // LoopBegin と LoopEnd のスロット数が一致していれば true
    public bool LoopSlotsBalanced =>
        Slots.Count(s => s.SelectedPreset.IsLoopBegin) == Slots.Count(s => s.SelectedPreset.IsLoopEnd);

    // スロットの追加・削除時にAllSlotsValidを再通知し、各スロットの変更監視を付け外し
    private void OnSlotsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (SequenceSlotViewModel slot in e.NewItems)
                slot.PropertyChanged += OnSlotPropertyChanged;

        if (e.OldItems is not null)
            foreach (SequenceSlotViewModel slot in e.OldItems)
                slot.PropertyChanged -= OnSlotPropertyChanged;

        OnPropertyChanged(nameof(AllSlotsValid));
        OnPropertyChanged(nameof(LoopSlotsBalanced));
        ExportCommand.NotifyCanExecuteChanged();
    }

    // 個々のスロットのIsValidが変わったときAllSlotsValidを再通知
    private void OnSlotPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SequenceSlotViewModel.IsValid))
            OnPropertyChanged(nameof(AllSlotsValid));
        if (e.PropertyName == nameof(SequenceSlotViewModel.SelectedPreset))
            OnPropertyChanged(nameof(LoopSlotsBalanced));
    }

    // ── スロット操作コマンド ──────────────────────────────────────────────

    // 選択行の直後にスロットを挿入（未選択時は末尾）
    [RelayCommand]
    private void AddSlot()
    {
        var newSlot = new SequenceSlotViewModel();
        int insertAt = _selectedSlots.Count > 0
            ? Slots.IndexOf(_selectedSlots.MaxBy(Slots.IndexOf)!) + 1
            : Slots.Count;
        Slots.Insert(insertAt, newSlot);
        SelectedSlot = newSlot;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSlot))]
    private void RemoveSlot()
    {
        var toRemove = _selectedSlots.ToList();
        int maxIdx = toRemove.Max(Slots.IndexOf);
        int minIdx = toRemove.Min(Slots.IndexOf);

        // 削除後の選択: 最大インデックスの次 → なければ最小インデックスの前 → なければ null
        SequenceSlotViewModel? nextSelection =
            maxIdx + 1 < Slots.Count ? Slots[maxIdx + 1] :
            minIdx - 1 >= 0 ? Slots[minIdx - 1] :
            null;

        foreach (var slot in toRemove)
            Slots.Remove(slot);

        SelectedSlot = nextSelection;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSlot))]
    private void CopySlot()
    {
        if (_selectedSlots.Count == 0) return;
        var ordered = _selectedSlots.OrderBy(Slots.IndexOf).ToList();
        int insertAt = Slots.IndexOf(ordered.Last()) + 1;
        var copies = ordered.Select(s => SequenceSlotViewModel.FromModel(s.ToModel())).ToList();
        foreach (var copy in ((IEnumerable<SequenceSlotViewModel>)copies).Reverse())
            Slots.Insert(insertAt, copy);
        SelectedSlot = copies[0];
        SelectionRequested?.Invoke(this, copies);
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        var ordered = _selectedSlots.OrderBy(Slots.IndexOf).ToList();
        foreach (var slot in ordered)
        {
            int i = Slots.IndexOf(slot);
            Slots.Move(i, i - 1);
        }
        NotifyMoveCommandsCanExecute();
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        var ordered = _selectedSlots.OrderByDescending(Slots.IndexOf).ToList();
        foreach (var slot in ordered)
        {
            int i = Slots.IndexOf(slot);
            Slots.Move(i, i + 1);
        }
        NotifyMoveCommandsCanExecute();
    }

    // ── インポート / エクスポート ─────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        string json = _importExport.Export(Name, Slots.Select(s => s.ToModel()), IsLoopMode);
        _dialogService.ShowExportDialog(json);
    }

    private bool CanExport() => Slots.Count > 0;

    [RelayCommand]
    private void Import()
    {
        string? input = _dialogService.ShowImportDialog();
        if (input is null or { Length: 0 }) return;

        ProfileExportData? data = null;
        try { data = _importExport.Import(input); }
        catch (UnsupportedSchemaVersionException)
        {
            _dialogService.ShowError("このデータはより新しいバージョンで作成されています。アプリを更新してください。");
            return;
        }
        catch (JsonException) { }

        if (data is null)
        {
            _dialogService.ShowError("インポートデータの形式が正しくありません。データが破損しているか、異なるバージョンのアプリで作成された可能性があります。");
            return;
        }

        if (Slots.Count > 0 && !_dialogService.ConfirmOverwrite())
            return;

        if (!string.IsNullOrEmpty(data.Name)) Name = data.Name;
        IsLoopMode = data.IsLoopMode;

        Slots.Clear();
        foreach (SequenceSlot slot in data.Slots)
            Slots.Add(SequenceSlotViewModel.FromModel(slot));
    }

    // ── CanExecute ────────────────────────────────────────────────────────

    private bool HasSelectedSlot => _selectedSlots.Count > 0;
    private bool CanMoveUp => _selectedSlots.Count > 0
        && Slots.IndexOf(_selectedSlots.MinBy(s => Slots.IndexOf(s))!) > 0;
    private bool CanMoveDown => _selectedSlots.Count > 0
        && Slots.IndexOf(_selectedSlots.MaxBy(s => Slots.IndexOf(s))!) < Slots.Count - 1;

    private void NotifySelectionCommandsCanExecute()
    {
        RemoveSlotCommand.NotifyCanExecuteChanged();
        CopySlotCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    private void NotifyMoveCommandsCanExecute()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    // ── モデル変換 ────────────────────────────────────────────────────────

    // VM => Profile
    public Profile ToModel() => new()
    {
        Name = Name,
        IsLoopMode = IsLoopMode,
        Slots = [.. Slots.Select(s => s.ToModel())],
    };

    // Profile => VM
    public void LoadFromModel(Profile profile)
    {
        Name = profile.Name;
        IsLoopMode = profile.IsLoopMode;
        Slots.Clear();
        foreach (SequenceSlot slot in profile.Slots)
            Slots.Add(SequenceSlotViewModel.FromModel(slot));
    }
}
