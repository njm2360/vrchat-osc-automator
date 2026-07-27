# 新スロット型の追加手順

新しいスロット型を追加するときは、以下の 5 ファイルを順番に変更する必要がある。  
漏れが起きやすい箇所を中心に、実際のコードパターンを示す。

## 変更が必要なファイル一覧

| #   | ファイル                              | 作業内容                                                                |
| --- | ------------------------------------- | ----------------------------------------------------------------------- |
| 1   | `Models/SequenceSlot.cs`              | record 型の追加 + JSON discriminator 登録                               |
| 2   | `ViewModels/SlotPreset.cs`            | Preset record の追加 + All リストへの追加 + Category プロパティ拡張     |
| 3   | `ViewModels/SequenceSlotViewModel.cs` | プロパティ追加・ToModel/FromModel・ParameterSummary・表示切替プロパティ |
| 4   | `Views/ProfileView.xaml`              | アコーディオン展開パネルへの UI セクション追加                          |
| 5   | `Services/SequencePlayerService.cs`   | スロット実行ロジックの追加                                              |

---

## Step 1: Models/SequenceSlot.cs

### 1-1. record 型を追加する

ファイル末尾に追加。`SequenceSlot` を継承する。実行時間を持つスロットは `GetDurationMs()` をオーバーライドする（基底の既定値は 0）。`SequencePlayerService` はこの戻り値で待機するので、オーバーライドし忘れると実行時間が常に 0 になる。

```csharp
// 例: サウンドを実行するスロット（仮想）
public record PlaySoundSlot(string FilePath, float Volume, int DurationMs = 0) : SequenceSlot
{
    public override int GetDurationMs() => DurationMs;
}
```

既存の参考パターン:

- パラメーターなし → `BreakpointSlot()` を参照
- DurationMs あり → `KeySingleSlot` / `MouseButtonSlot` を参照
- 実行時間を動的に決める → `RandomWaitSlot` を参照
- OSC 送信も兼ねる場合 → `OscSlot` を継承（`FloatSlot` を参照）

### 1-2. JsonDerivedType 属性を追加する

`abstract record SequenceSlot` の属性群に 1 行追加する。

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FloatSlot),         "float")]
// ... 既存 ...
[JsonDerivedType(typeof(PlaySoundSlot),     "play_sound")]  // ← 追加
public abstract record SequenceSlot;
```

> **注意:** discriminator 文字列はスネークケースにする（既存の命名に合わせる）。  
> 一度 settings.json に保存されると変更できないため慎重に決める。

---

## Step 2: ViewModels/SlotPreset.cs

### 2-1. Preset record を追加する

ファイル末尾に sealed record を追加する。

```csharp
public sealed record PlaySoundPreset(string Name) : SlotPreset(Name);
```

### 2-2. All リストに追加する

`SlotPreset.All` の適切なカテゴリのグループ内に追加する。

```csharp
public static readonly IReadOnlyList<SlotPreset> All =
[
    // シーケンス制御
    new WaitPreset("待機"),
    // ...

    // ← カテゴリが新規の場合はここに新グループを追加
    // サウンド
    new PlaySoundPreset("サウンド実行"),   // ← 追加

    // OSC
    // ...
];
```

### 2-3. Category プロパティを拡張する

`SlotPreset.Category` の switch 式に新しい型を追加する。

```csharp
public string Category => this switch
{
    WaitPreset or RandomWaitPreset or LoopBeginPreset or LoopEndPreset or BreakpointPreset => "シーケンス制御",
    KeySinglePreset or KeyTypeStringPreset => "キーボード",
    MouseButtonPreset or MouseWheelPreset or MouseMovePreset => "マウス",
    PlaySoundPreset => "サウンド",   // ← 追加（新カテゴリの場合）
    _ => "OSC",
};
```

既存カテゴリに収まる場合は既存の行に `or PlaySoundPreset` を追加するだけでよい。

### 2-4. XAML バインディング用プロパティを追加する

`SlotPreset` の XAML バインディング用プロパティブロックに追加する。

```csharp
public bool IsPlaySound => this is PlaySoundPreset;
```

---

## Step 3: ViewModels/SequenceSlotViewModel.cs

### 3-1. パラメーター用プロパティを追加する

パラメーターを保持する `[ObservableProperty]` を追加する。  
`ParameterSummary` に影響するものは `[NotifyPropertyChangedFor(nameof(ParameterSummary))]` を付ける。

```csharp
// ── サウンド ──────────────────────────────────────────────────────────

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(ParameterSummary))]
private string _soundFilePath = string.Empty;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(ParameterSummary))]
private float _soundVolume = 1.0f;
```

### 3-2. SelectedPreset 変更通知に新プロパティを追加する

`SelectedPreset` の `[ObservableProperty]` 属性ブロックに追加する。

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(ShowResetOption))]
[NotifyPropertyChangedFor(nameof(IsDurationEditable))]
// ... 既存 ...
[NotifyPropertyChangedFor(nameof(IsPlaySoundMode))]  // ← 追加
[NotifyPropertyChangedFor(nameof(ParameterSummary))]
[NotifyPropertyChangedFor(nameof(IsValid))]
public partial SlotPreset SelectedPreset { get; set; } = SlotPreset.All[0];
```

### 3-3. 表示切替用プロパティを追加する

既存の `IsKeyboardSingleMode` などと並べて追加する。

```csharp
public bool IsPlaySoundMode => SelectedPreset is PlaySoundPreset;
```

### 3-4. IsDurationEditable を必要に応じて変更する

DurationMs を使わないスロット型の場合、`IsDurationEditable` の条件に追加する。

```csharp
// DurationMs を持たないスロットなら以下に追加
public bool IsDurationEditable => SelectedPreset is not (LoopBeginPreset or LoopEndPreset or BreakpointPreset or RandomWaitPreset);
// 例: PlaySoundSlot が DurationMs を持たない場合
// public bool IsDurationEditable => SelectedPreset is not (LoopBeginPreset or LoopEndPreset or BreakpointPreset or RandomWaitPreset or PlaySoundPreset);
```

### 3-5. ParameterSummary に case を追加する

switch 式の末尾（`_ =>` の直前）に追加する。

```csharp
public string ParameterSummary => SelectedPreset switch
{
    // ... 既存 ...
    PlaySoundPreset => SoundFilePath.Length == 0
        ? "(ファイル未選択)"
        : $"{System.IO.Path.GetFileName(SoundFilePath)} vol:{SoundVolume:F1}",
    _ => $"{FloatValue:F2}",
};
```

### 3-6. ToModel() に case を追加する

```csharp
public SequenceSlot ToModel() => SelectedPreset switch
{
    // ... 既存 ...
    PlaySoundPreset => new PlaySoundSlot(SoundFilePath, SoundVolume, DurationMs),
    _ => throw new UnreachableException(),
};
```

### 3-7. FromModel() に case を追加する

```csharp
public static SequenceSlotViewModel FromModel(SequenceSlot slot) => slot switch
{
    // ... 既存 ...
    PlaySoundSlot ps => new()
    {
        SelectedPreset = SlotPreset.All.OfType<PlaySoundPreset>().First(),
        SoundFilePath = ps.FilePath,
        SoundVolume = ps.Volume,
        DurationMs = ps.DurationMs,
    },
    _ => throw new ArgumentOutOfRangeException(nameof(slot)),
};
```

> **注意:** `FromModel` の `_ =>` 行（例外スロー）の**直前**に追加すること。  
> OSC スロット（FloatSlot 等）は `BuildOscVm` に委譲されているため、  
> 新スロットが OSC 系でない限り `BuildOscVm` は変更不要。

---

## Step 4: Views/ProfileView.xaml

`DataGrid.RowDetailsTemplate` 内の `StackPanel` に UI セクションを追加する。

### 4-1. セクションを追加する位置

`<!-- マウス移動 -->` ブロックの直後、`<!-- 値の書き戻し -->` の前に追加する。

```xml
<!-- サウンド実行 -->
<StackPanel Visibility="{Binding SelectedPreset.IsPlaySound, Converter={StaticResource BoolToVis}}">
    <StackPanel Orientation="Horizontal" Margin="0,0,0,6">
        <TextBlock Text="ファイル:" Width="96"
                   VerticalAlignment="Center" Foreground="#444" />
        <TextBox Text="{Binding SoundFilePath, UpdateSourceTrigger=PropertyChanged}"
                 Width="260" VerticalAlignment="Center" />
    </StackPanel>
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="音量:" Width="96"
                   VerticalAlignment="Center" Foreground="#444" />
        <TextBox Text="{Binding SoundVolume, UpdateSourceTrigger=LostFocus}"
                 Width="60" VerticalAlignment="Center"
                 b:NumericTextBoxBehavior.RefreshOnLostFocus="True"
                 b:NumericTextBoxBehavior.AllowDecimal="True" />
        <!-- MinValue/MaxValue のクランプは整数モード専用。小数の範囲制限は VM 側で行う -->
    </StackPanel>
</StackPanel>
```

### UI パターン早見表

| 入力の種類       | 使う要素                                          | 参考セクション                   |
| ---------------- | ------------------------------------------------- | -------------------------------- |
| テキスト入力     | `TextBox` + `UpdateSourceTrigger=PropertyChanged` | `<!-- 文字入力 -->`              |
| 整数入力         | `TextBox` + `NumericTextBoxBehavior`              | `<!-- マウスホイール -->`        |
| 小数入力         | `TextBox` + `AllowDecimal="True"`                 | カスタム Float モード            |
| ON/OFF 選択      | `RadioButton` ×2（IsValueOn / IsValueOff）        | `<!-- Int定義済みプリセット -->` |
| 複数択一         | `RadioButton` ×n + bool プロパティ                | `<!-- キー送信(単押し) -->`      |
| フラグ           | `CheckBox`                                        | `<!-- 改行を送信する -->`        |
| パラメーターなし | `TextBlock Text="(パラメータなし)"`               | `<!-- 待機 -->`                  |

### "完了後に 0 に戻す" の表示制御

OSC 送信のないスロットにはこのチェックボックスは不要。  
`ShowResetOption` は `SelectedPreset is BuiltinPreset` のときのみ `true` を返すので、  
OSC 以外のスロットでは自動的に非表示になる（`SequenceSlotViewModel.cs` 側の変更不要）。

---

## Step 5: Services/SequencePlayerService.cs

実行時間は `slot.GetDurationMs()`（Step 1 でオーバーライド済み）から取得されるため、SequencePlayerService 側に時間関連の追加は不要。実行ロジックだけ追加する。

### 5-1. 実行ロジックを追加する

`switch (slot)` ブロック（KeySingleSlot / MouseButtonSlot などが並んでいる箇所）に追加する。

```csharp
switch (slot)
{
    case KeySingleSlot ks:
        // ... 既存 ...
        break;
    // ... 既存 ...
    case PlaySoundSlot ps:
        // ここに実際の処理（サービス呼び出し等）を書く
        // _soundPlayer.Play(ps.FilePath, ps.Volume);
        break;
}
```

### 5-2. 入力状態追跡が必要な場合

押下状態の管理（`_pressedKeys` / `_pressedMouseButtons` のような追跡）が必要なスロットは、  
`ReleaseAllInputs()` / `RepressAllInputs()` / `ClearInputState()` にも追加が必要になる。  
キーボード・マウス以外のスロットであれば通常不要。

---

## 完了チェックリスト

実装後、以下をすべて確認する。

### コード

- [ ] `SequenceSlot.cs` — record 型を追加した
- [ ] `SequenceSlot.cs` — 実行時間を持つ場合は `GetDurationMs()` をオーバーライドした
- [ ] `SequenceSlot.cs` — `[JsonDerivedType]` 属性を追加した（discriminator は変更不可）
- [ ] `SlotPreset.cs` — sealed record を追加した
- [ ] `SlotPreset.cs` — `All` リストに追加した（適切な位置・カテゴリ内）
- [ ] `SlotPreset.cs` — `Category` switch 式に追加した
- [ ] `SlotPreset.cs` — `IsXxx` プロパティを追加した
- [ ] `SequenceSlotViewModel.cs` — `[ObservableProperty]` フィールドを追加した
- [ ] `SequenceSlotViewModel.cs` — `SelectedPreset` の `[NotifyPropertyChangedFor]` に追加した
- [ ] `SequenceSlotViewModel.cs` — `IsXxxMode` プロパティを追加した
- [ ] `SequenceSlotViewModel.cs` — `IsDurationEditable` を必要に応じて変更した
- [ ] `SequenceSlotViewModel.cs` — `ParameterSummary` に case を追加した
- [ ] `SequenceSlotViewModel.cs` — `ToModel()` に case を追加した
- [ ] `SequenceSlotViewModel.cs` — `FromModel()` に case を追加した
- [ ] `ProfileView.xaml` — RowDetailsTemplate に UI セクションを追加した
- [ ] `SequencePlayerService.cs` — 実行 `switch (slot)` ブロックに追加した

### 動作確認

- [ ] 新スロットを追加して保存 → アプリ再起動後に正しく復元される（JSON 往復）
- [ ] エクスポート → インポートが正常に動作する
- [ ] 実行時に意図した動作をする
- [ ] 一時停止 → 再開後に正常に動作する
- [ ] ループ内に配置しても正常に動作する

---

## よくある漏れとエラー

| 症状                                                  | 原因箇所                                                                             |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------ |
| 新スロット選択後に行詳細が何も表示されない            | `ProfileView.xaml` の `IsXxx` バインディング名が `SlotPreset` のプロパティ名と不一致 |
| `ToModel()` / `FromModel()` で `UnreachableException` | switch に case を追加し忘れた                                                        |
| JSON 保存後に読み込むと型が消える                     | `[JsonDerivedType]` 属性の追加漏れ                                                   |
| DataGrid の「パラメータ」列が空白                     | `ParameterSummary` の switch に case を追加し忘れた                                  |
| 実行時間が常に 0 になる                               | `GetDurationMs()` のオーバーライド漏れ                                               |
| `SelectedPreset` 変更時に UI が更新されない           | `[NotifyPropertyChangedFor(nameof(IsXxxMode))]` の追加漏れ                           |
