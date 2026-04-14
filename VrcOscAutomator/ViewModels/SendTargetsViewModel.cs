using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SendTargetsViewModel : ObservableObject
{
    public ObservableCollection<OscTargetViewModel> Targets { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveTargetCommand))]
    private OscTargetViewModel? _selectedTarget;

    [RelayCommand]
    private void AddTarget() => Targets.Add(new OscTargetViewModel());

    [RelayCommand(CanExecute = nameof(HasSelectedTarget))]
    private void RemoveTarget()
    {
        if (SelectedTarget is not null)
            Targets.Remove(SelectedTarget);
    }

    private bool HasSelectedTarget => SelectedTarget is not null;

    public void LoadFromModels(IEnumerable<OscTarget> targets)
    {
        Targets.Clear();
        foreach (OscTarget t in targets)
            Targets.Add(OscTargetViewModel.FromModel(t));
    }

    public List<OscTarget> ToModels() => [.. Targets.Select(t => t.ToModel())];

    public string? GetDuplicateError()
    {
        var duplicates = Targets
            .GroupBy(t => (t.IpAddress.Trim(), t.Port))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Item1}:{g.Key.Port}")
            .ToList();

        if (duplicates.Count == 0) return null;

        return "送信先が重複しています:\n" + string.Join("\n", duplicates);
    }
}
