using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SendTargetsViewModel : ObservableObject
{
    // DataGridにバインドする送信先一覧
    public ObservableCollection<OscTargetViewModel> Targets { get; } = [];

    // DataGridで選択中の行
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveTargetCommand))]
    public partial OscTargetViewModel? SelectedTarget { get; set; }

    // ── コマンド ──────────────────────────────────────────────────────────

    // 追加ボタン: 新規行を末尾に挿入
    [RelayCommand]
    private void AddTarget() => Targets.Add(new OscTargetViewModel());

    // 削除ボタン: 選択行を削除（未選択時は無効）
    [RelayCommand(CanExecute = nameof(HasSelectedTarget))]
    private void RemoveTarget()
    {
        if (SelectedTarget is not null)
            Targets.Remove(SelectedTarget);
    }

    private bool HasSelectedTarget => SelectedTarget is not null;

    // ── モデル変換 ────────────────────────────────────────────────────────

    public void LoadFromModels(IEnumerable<OscTarget> targets)
    {
        Targets.Clear();
        foreach (OscTarget t in targets)
            Targets.Add(OscTargetViewModel.FromModel(t));
    }

    public List<OscTarget> ToModels() => [.. Targets.Select(t => t.ToModel())];

    // ── バリデーション ────────────────────────────────────────────────────

    // IP:Portが重複する行がある場合はエラー
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
