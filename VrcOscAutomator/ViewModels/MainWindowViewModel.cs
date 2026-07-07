using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    // ── 定数・依存サービス ────────────────────────────────────────────────

    private readonly ISequencePlayer _player;
    private readonly ISettingsRepository _repository;
    private readonly IOscSender _oscSender;
    private readonly IDialogService _dialogService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly IKeyboardSender _keyboardSender;
    private readonly IMouseSender _mouseSender;
    private readonly ISequenceImportExportService _importExport;

    // 設定ファイルから読み込んだ OSC 送信先・ホットキー・キーリピート設定
    private List<OscTarget> _targets = [];
    private HotkeySettings _hotkeys = new();
    private KeyRepeatSettings _keyRepeat = new();

    // 実行中断用トークン
    private CancellationTokenSource? _cts;

    // ── 実行状態 ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotPlaying))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseResumeCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PauseResumeCommand))]
    public partial bool IsPaused { get; set; }

    public bool IsNotPlaying => !IsPlaying;

    // ── プロファイル選択 ──────────────────────────────────────────────────

    // タブ選択中のプロファイルインデックス
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial int SelectedProfileIndex { get; set; }

    // プロファイル切り替え時に IsLoopMode の変更通知を出す
    partial void OnSelectedProfileIndexChanged(int value) => OnPropertyChanged("IsLoopMode");

    // IsLoopMode は選択中プロファイルへのパススルー
    public bool IsLoopMode
    {
        get => SelectedProfileIndex >= 0 && SelectedProfileIndex < Profiles.Count
               ? Profiles[SelectedProfileIndex].IsLoopMode
               : false;
        set
        {
            if (SelectedProfileIndex >= 0 && SelectedProfileIndex < Profiles.Count
                && Profiles[SelectedProfileIndex].IsLoopMode != value)
            {
                Profiles[SelectedProfileIndex].IsLoopMode = value;
                OnPropertyChanged();
            }
        }
    }

    // ── 入力モード設定 ────────────────────────────────────────────────────

    // キーボード送信方式（VirtualKey / ScanCode）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKeyboardVkMode))]
    [NotifyPropertyChangedFor(nameof(IsKeyboardScanMode))]
    public partial KeyboardInputMode KeyboardMode { get; set; } = KeyboardInputMode.ScanCode;

    // マウス座標系（Standard=物理ピクセル / VirtualDesktop=仮想デスクトップ正規化座標）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMouseStandardMode))]
    [NotifyPropertyChangedFor(nameof(IsMouseVirtualDesktopMode))]
    public partial MouseInputMode MouseMode { get; set; } = MouseInputMode.VirtualDesktop;

    // RadioButton バインディング用: 各入力モードを bool に変換
    public bool IsKeyboardVkMode => KeyboardMode == KeyboardInputMode.VirtualKey;
    public bool IsKeyboardScanMode => KeyboardMode == KeyboardInputMode.ScanCode;
    public bool IsMouseStandardMode => MouseMode == MouseInputMode.Standard;
    public bool IsMouseVirtualDesktopMode => MouseMode == MouseInputMode.VirtualDesktop;

    // ── Start CanExecute / ステータス表示 ────────────────────────────────

    // 送信先が有効・スロットが存在・全スロットが有効・ループが対応している場合のみ再生可能
    private bool CanStart => IsNotPlaying
        && Profiles.Count > 0
        && SelectedProfileIndex >= 0 && SelectedProfileIndex < Profiles.Count
        && _targets.Any(t => t.IsEnabled)
        && Profiles[SelectedProfileIndex].Slots.Count > 0
        && Profiles[SelectedProfileIndex].AllSlotsValid
        && Profiles[SelectedProfileIndex].LoopSlotsBalanced;

    // 実行できない理由をユーザーに表示するメッセージ
    public string StatusMessage
    {
        get
        {
            if (IsPlaying) return string.Empty;
            if (Profiles.Count == 0 || SelectedProfileIndex < 0) return string.Empty;
            if (!_targets.Any(t => t.IsEnabled))
                return "送信先が設定されていません。「送信先設定」から有効な送信先を追加してください。";
            if (Profiles[SelectedProfileIndex].Slots.Count == 0)
                return "スロットが追加されていません。";
            if (!Profiles[SelectedProfileIndex].AllSlotsValid)
                return "無効な設定のスロットがあります。内容を確認してください。";
            if (!Profiles[SelectedProfileIndex].LoopSlotsBalanced)
                return "繰り返し開始と繰り返し終了のスロット数が一致していません。";
            return string.Empty;
        }
    }

    // ── プロファイル一覧 ──────────────────────────────────────────────────

    public ObservableCollection<ProfileViewModel> Profiles { get; } = [];

    public MainWindowViewModel(
        ISequencePlayer player,
        ISettingsRepository repository,
        IOscSender oscSender,
        IDialogService dialogService,
        ISequenceImportExportService importExport,
        IGlobalHotkeyService hotkeyService,
        IKeyboardSender keyboardSender,
        IMouseSender mouseSender)
    {
        _player = player;
        _repository = repository;
        _oscSender = oscSender;
        _dialogService = dialogService;
        _importExport = importExport;
        _hotkeyService = hotkeyService;
        _keyboardSender = keyboardSender;
        _mouseSender = mouseSender;

        _hotkeyService.StartPressed += OnHotkeyStartPressed;
        _hotkeyService.PauseResumePressed += OnHotkeyPauseResumePressed;
        _hotkeyService.StopPressed += OnHotkeyStopPressed;

        AddProfileInternal("Profile 1");
    }

    // プロファイルを追加し、スロット変更・IsLoopMode変更の監視を設定する内部ヘルパー
    private ProfileViewModel AddProfileInternal(string name)
    {
        var profile = new ProfileViewModel(_dialogService, _importExport) { Name = name };
        profile.Slots.CollectionChanged += (_, _) =>
        {
            StartCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(StatusMessage));
        };
        profile.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ProfileViewModel.AllSlotsValid) or nameof(ProfileViewModel.LoopSlotsBalanced))
            {
                StartCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(StatusMessage));
            }
            else if (e.PropertyName == nameof(ProfileViewModel.IsLoopMode)
                     && Profiles.IndexOf(profile) == SelectedProfileIndex)
            {
                OnPropertyChanged(nameof(IsLoopMode));
            }
        };
        Profiles.Add(profile);
        return profile;
    }

    // ── ホットキー ────────────────────────────────────────────────────────

    // Windowハンドル取得後に呼ぶ（WM_HOTKEYの登録にHWNDが必要なため）
    public void InitializeHotkeys(Window window)
    {
        _hotkeyService.Initialize(window);
        _hotkeyService.UpdateSettings(_hotkeys);
    }

    private void OnHotkeyStartPressed() { if (CanStart) _ = StartAsync(); }
    private void OnHotkeyPauseResumePressed() { if (IsPlaying) _ = PauseResumeAsync(); }
    private void OnHotkeyStopPressed() { if (IsPlaying) _ = StopAsync(); }

    // ── 再生中のスロット進捗更新 ──────────────────────────────────────────

    // IProgress<SequenceProgress> コールバック: 現在スロットと反復回数を各VMに反映
    private void OnSlotChanged(SequenceProgress progress)
    {
        ProfileViewModel profile = Profiles[SelectedProfileIndex];
        for (int i = 0; i < profile.Slots.Count; i++)
        {
            profile.Slots[i].IsCurrentSlot = i == progress.SlotIndex;
            profile.Slots[i].CurrentIteration = progress.LoopIterations.TryGetValue(i, out int iter) ? iter : 0;
        }
        IsPaused = _player.IsPaused;
    }

    // ── プロファイル操作コマンド ──────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(IsNotPlaying))]
    private async Task AddProfileAsync()
    {
        int n = Profiles.Count + 1;
        string name = $"Profile {n}";
        while (Profiles.Any(p => p.Name == name)) name = $"Profile {++n}";
        SelectedProfileIndex = Profiles.IndexOf(AddProfileInternal(name));
        await SaveAsync();
    }

    [RelayCommand(CanExecute = nameof(IsNotPlaying))]
    private async Task DeleteProfileAsync(ProfileViewModel profile)
    {
        if (!_dialogService.ConfirmDeleteProfile(profile.Name)) return;
        int idx = Profiles.IndexOf(profile);
        Profiles.Remove(profile);
        SelectedProfileIndex = Profiles.Count > 0 ? Math.Min(idx, Profiles.Count - 1) : -1;
        await SaveAsync();
    }

    // ── アプリケーションライフサイクル ────────────────────────────────────

    // Window.Loaded: 設定を読み込みプロファイルと各種設定を復元
    [RelayCommand]
    private async Task LoadedAsync()
    {
        SettingsLoadResult loadResult = await _repository.LoadAsync();
        if (loadResult.WasCorrupted)
            _dialogService.ShowError(loadResult.CorruptionDetail!);
        AppSettings settings = loadResult.Settings;
        _targets = settings.Targets;
        _oscSender.SetTargets(_targets);
        _hotkeys = settings.Hotkeys;
        _keyRepeat = settings.KeyRepeat;
        _player.SetKeyRepeatSettings(_keyRepeat);
        KeyboardMode = settings.Input.KeyboardMode;
        MouseMode = settings.Input.MouseMode;
        _keyboardSender.Mode = KeyboardMode;
        _mouseSender.Mode = MouseMode;

        Profiles.Clear();
        if (settings.Profiles.Count == 0)
        {
            AddProfileInternal("Profile 1");
        }
        else
        {
            foreach (Profile p in settings.Profiles)
            {
                ProfileViewModel vm = AddProfileInternal(p.Name);
                vm.LoadFromModel(p);
            }
        }

        SelectedProfileIndex = 0;
        StartCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(StatusMessage));
    }

    // ── 設定ダイアログ系コマンド ──────────────────────────────────────────

    [RelayCommand]
    private async Task OpenTargetsWindowAsync()
    {
        IReadOnlyList<OscTarget>? result = _dialogService.ShowSendTargetsWindow(_targets);
        if (result is not null)
        {
            _targets = [.. result];
            _oscSender.SetTargets(_targets);
            StartCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(StatusMessage));
            await SaveAsync();
        }
    }

    [RelayCommand]
    private async Task OpenHotkeySettingsAsync()
    {
        HotkeySettings? result = _dialogService.ShowHotkeySettingsWindow(_hotkeys);
        if (result is not null)
        {
            _hotkeys = result;
            _hotkeyService.UpdateSettings(_hotkeys);
            await SaveAsync();
        }
    }

    [RelayCommand]
    private async Task OpenKeyRepeatSettingsAsync()
    {
        KeyRepeatSettings? result = _dialogService.ShowKeyRepeatSettingsWindow(_keyRepeat);
        if (result is not null)
        {
            _keyRepeat = result;
            _player.SetKeyRepeatSettings(_keyRepeat);
            await SaveAsync();
        }
    }

    [RelayCommand]
    private void OpenAboutWindow() => _dialogService.ShowAboutWindow();

    [RelayCommand]
    private async Task SetKeyboardModeAsync(KeyboardInputMode mode)
    {
        KeyboardMode = mode;
        _keyboardSender.Mode = mode;
        await SaveAsync();
    }

    [RelayCommand]
    private async Task SetMouseModeAsync(MouseInputMode mode)
    {
        MouseMode = mode;
        _mouseSender.Mode = mode;
        await SaveAsync();
    }

    // ── 実行コマンド ──────────────────────────────────────────────────────

    // 実行開始: スロットをModelに変換してプレイヤーに渡す
    // （完了・例外どちらでもIsPlayingをリセット）
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        _oscSender.SetTargets(_targets);
        await SaveAsync();

        List<SequenceSlot> slots = [.. Profiles[SelectedProfileIndex].Slots.Select(s => s.ToModel())];

        Profiles[SelectedProfileIndex].SelectedSlot = null;
        IsPlaying = true;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<SequenceProgress>(OnSlotChanged);
            await _player.PlayAsync(slots, IsLoopMode, progress, _cts.Token);
        }
        finally
        {
            IsPlaying = false;
            IsPaused = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsPlaying))]
    private async Task PauseResumeAsync()
    {
        if (IsPaused)
        {
            await _player.ResumeAsync();
            IsPaused = false;
        }
        else
        {
            await _player.PauseAsync();
            IsPaused = true;
        }
    }

    [RelayCommand(CanExecute = nameof(IsPlaying))]
    private async Task StopAsync()
    {
        await _player.StopAsync();
        IsPlaying = false;
        IsPaused = false;
    }

    [RelayCommand]
    private static void Close() => Application.Current.MainWindow?.Close();

    // Window.Closing: 終了前に設定を保存
    [RelayCommand]
    private async Task ClosingAsync() => await SaveAsync();

    // ── 設定永続化 ────────────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        AppSettings settings = new()
        {
            Targets = _targets,
            Profiles = [.. Profiles.Select(p => p.ToModel())],
            Hotkeys = _hotkeys,
            KeyRepeat = _keyRepeat,
            Input = new InputSettings { KeyboardMode = KeyboardMode, MouseMode = MouseMode },
        };
        await _repository.SaveAsync(settings);
    }
}
