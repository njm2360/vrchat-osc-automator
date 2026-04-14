using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public static string WindowTitle { get; } =
        $"VRChat OSC Automator v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";

    private readonly ISequencePlayer _player;
    private readonly ISettingsRepository _repository;
    private readonly IOscSender _oscSender;
    private readonly IDialogService _dialogService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private List<OscTarget> _targets = [];
    private HotkeySettings _hotkeys = new();

    private CancellationTokenSource? _cts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotPlaying))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseResumeCommand))]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PauseResumeCommand))]
    private bool _isPaused;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private int _selectedProfileIndex;

    [ObservableProperty]
    private bool _isLoopMode;

    public bool IsNotPlaying => !IsPlaying;
    private bool CanStart => IsNotPlaying
        && _targets.Any(t => t.IsEnabled)
        && Profiles[SelectedProfileIndex].Slots.Count > 0
        && Profiles[SelectedProfileIndex].AllSlotsValid;

    public string StatusMessage
    {
        get
        {
            if (IsPlaying) return string.Empty;
            if (!_targets.Any(t => t.IsEnabled))
                return "送信先が設定されていません。「送信先設定」から有効な送信先を追加してください。";
            if (Profiles[SelectedProfileIndex].Slots.Count == 0)
                return "スロットが追加されていません。";
            if (!Profiles[SelectedProfileIndex].AllSlotsValid)
                return "無効な設定のスロットがあります。内容を確認してください。";
            return string.Empty;
        }
    }
    public ObservableCollection<ProfileViewModel> Profiles { get; } = [];

    public MainWindowViewModel(
        ISequencePlayer player,
        ISettingsRepository repository,
        IOscSender oscSender,
        IDialogService dialogService,
        ISequenceImportExportService importExport,
        IGlobalHotkeyService hotkeyService)
    {
        _player = player;
        _repository = repository;
        _oscSender = oscSender;
        _dialogService = dialogService;
        _hotkeyService = hotkeyService;

        _hotkeyService.StartPressed += OnHotkeyStartPressed;
        _hotkeyService.PauseResumePressed += OnHotkeyPauseResumePressed;
        _hotkeyService.StopPressed += OnHotkeyStopPressed;

        for (int i = 1; i <= 5; i++)
        {
            var profile = new ProfileViewModel(dialogService, importExport) { Name = $"Profile {i}" };
            profile.Slots.CollectionChanged += (_, _) =>
            {
                StartCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(StatusMessage));
            };
            profile.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ProfileViewModel.AllSlotsValid))
                {
                    StartCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(StatusMessage));
                }
            };
            Profiles.Add(profile);
        }
    }

    public void InitializeHotkeys(Window window)
    {
        _hotkeyService.Initialize(window);
        _hotkeyService.UpdateSettings(_hotkeys);
    }

    private void OnHotkeyStartPressed()
    {
        if (CanStart) _ = StartAsync();
    }

    private void OnHotkeyPauseResumePressed()
    {
        if (IsPlaying) _ = PauseResumeAsync();
    }

    private void OnHotkeyStopPressed()
    {
        if (IsPlaying) _ = StopAsync();
    }

    private void OnSlotChanged(int index)
    {
        ProfileViewModel profile = Profiles[SelectedProfileIndex];
        for (int i = 0; i < profile.Slots.Count; i++)
            profile.Slots[i].IsCurrentSlot = (i == index);
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        AppSettings settings = await _repository.LoadAsync();
        _targets = settings.Targets;
        _oscSender.SetTargets(_targets);
        _hotkeys = settings.Hotkeys;
        IsLoopMode = settings.IsLoopMode;

        for (int i = 0; i < Profiles.Count && i < settings.Profiles.Count; i++)
            Profiles[i].LoadFromModel(settings.Profiles[i]);

        StartCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(StatusMessage));
    }

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
            var progress = new Progress<int>(OnSlotChanged);
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
    private async Task ClosingAsync() => await SaveAsync();

    private async Task SaveAsync()
    {
        AppSettings settings = new()
        {
            Targets = _targets,
            Profiles = [.. Profiles.Select(p => p.ToModel())],
            Hotkeys = _hotkeys,
            IsLoopMode = IsLoopMode,
        };
        await _repository.SaveAsync(settings);
    }
}
