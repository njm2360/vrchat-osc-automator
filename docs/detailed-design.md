# VrcOscAutomator 詳細設計書

## 目次

1. [システム概要](#1-システム概要)
2. [アーキテクチャ](#2-アーキテクチャ)
3. [データモデル](#3-データモデル)
4. [サービス層](#4-サービス層)
5. [ViewModel 層](#5-viewmodel-層)
6. [UI 構造](#6-ui-構造)
7. [設定永続化とマイグレーション](#7-設定永続化とマイグレーション)
8. [シーケンス実行エンジン詳細](#8-シーケンス実行エンジン詳細)
9. [OSC パケット仕様](#9-osc-パケット仕様)
10. [Windows API 利用](#10-windows-api-利用)
11. [依存関係とテスト](#11-依存関係とテスト)

---

## 1. システム概要

### 目的

VrcOscAutomator は、VRChat の OSC (Open Sound Control) コントロールインターフェースに対してコマンドシーケンスを自動実行するツールである。キーボード・マウス入力の自動化、ループ制御、グローバルホットキーによる操作を組み合わせた複合シーケンスを GUI で構築・実行できる。

### 技術スタック

| 要素            | 内容                                                 |
| --------------- | ---------------------------------------------------- |
| フレームワーク  | .NET 10.0 / WPF                                      |
| 言語            | C# 13                                                |
| アーキテクチャ  | MVVM + DI (Microsoft.Extensions.DependencyInjection) |
| MVVM ライブラリ | CommunityToolkit.Mvvm 8.4.2（source generator 方式） |
| テスト          | xUnit 2.9.3 / Moq 4.20.72 / FluentAssertions 7.2.2   |
| 対応 OS         | Windows 10/11 (win-x64)                              |

---

## 2. アーキテクチャ

### レイヤー構成

```
┌─────────────────────────────────────────┐
│               Views (XAML)              │  UI描画・イベント受信
├─────────────────────────────────────────┤
│             ViewModels                  │  状態管理・コマンド処理
├─────────────────────────────────────────┤
│    Interfaces  ←→  Services             │  ビジネスロジック
├─────────────────────────────────────────┤
│               Models                    │  データ構造
└─────────────────────────────────────────┘
```

### 依存注入 (App.xaml.cs)

全サービスは Singleton スコープで登録される。アプリケーション起動時に `ServiceCollection` を構築し、`MainWindow` の DataContext に `MainWindowViewModel` を注入する。

```csharp
services.AddSingleton<IOscSender, OscSenderService>();
services.AddSingleton<IKeyboardSender, KeyboardSenderService>();
services.AddSingleton<IMouseSender, MouseSenderService>();
services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
services.AddSingleton<ISequencePlayer, SequencePlayerService>();
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<ISequenceImportExportService, SequenceImportExportService>();
services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
services.AddSingleton<MainWindowViewModel>();
```

Singleton を選んだ理由: 各サービスはアプリ全体でひとつの状態（送信先リスト・実行状態・ホットキー登録）を保持する必要があるため。

---

## 3. データモデル

### 3.1 SequenceSlot 型階層

`Models/SequenceSlot.cs` に定義。`abstract record SequenceSlot` を基底とし、JSON シリアライズ時には `[JsonPolymorphic]` + `[JsonDerivedType]` による discriminator ("type" プロパティ) で型を識別する。

#### OSC 系スロット（`OscSlot` 中間抽象から派生）

| 型           | discriminator | パラメーター                                                 |
| ------------ | ------------- | ------------------------------------------------------------ |
| `FloatSlot`  | `"float"`     | `Address`, `Value` (float), `DurationMs`, `ResetOnComplete`  |
| `IntSlot`    | `"int"`       | `Address`, `Value` (int), `DurationMs`, `ResetOnComplete`    |
| `BoolSlot`   | `"bool"`      | `Address`, `Value` (bool), `DurationMs`, `ResetOnComplete`   |
| `StringSlot` | `"string"`    | `Address`, `Value` (string), `DurationMs`, `ResetOnComplete` |

`ResetOnComplete` が `true` の場合、スロット実行後に OSC 値を既定値（float→0、int→0、bool→false、string→""）にリセットする。

#### 制御フロースロット

| 型               | discriminator   | パラメーター           | 動作                                  |
| ---------------- | --------------- | ---------------------- | ------------------------------------- |
| `WaitSlot`       | `"wait"`        | `DurationMs`           | 指定時間待機のみ                      |
| `RandomWaitSlot` | `"random_wait"` | `MinMs`, `MaxMs`       | MinMs〜MaxMs のランダム時間待機       |
| `LoopBeginSlot`  | `"loop_begin"`  | `RepeatCount`          | ループ開始マーカー。0 = 無限ループ    |
| `LoopEndSlot`    | `"loop_end"`    | なし                   | ループ終了マーカー                    |
| `BreakpointSlot` | `"breakpoint"`  | なし                   | 実行を一時停止                        |

#### キーボードスロット

| 型                  | discriminator       | パラメーター                                           |
| ------------------- | ------------------- | ------------------------------------------------------ |
| `KeySingleSlot`     | `"key_single"`      | `VirtualKey` (int), `Action` (KeyAction), `DurationMs` |
| `KeyTypeStringSlot` | `"key_type_string"` | `Text` (string), `AppendNewline` (bool), `DurationMs`  |

#### マウススロット

| 型                | discriminator    | パラメーター                                               |
| ----------------- | ---------------- | ---------------------------------------------------------- |
| `MouseButtonSlot` | `"mouse_button"` | `Button` (MouseButton), `Action` (KeyAction), `DurationMs` |
| `MouseWheelSlot`  | `"mouse_wheel"`  | `Clicks` (int, 正=上/負=下), `DurationMs`                  |
| `MouseMoveSlot`   | `"mouse_move"`   | `X`, `Y` (int), `Mode` (MouseMoveMode), `DurationMs`       |

#### 関連 Enum

```csharp
enum KeyAction     { Press, Release, PressAndRelease }
enum MouseButton   { Left, Right, Middle }
enum MouseMoveMode { Relative, Absolute }
enum OscValueType  { Float, Int, Bool, String }
```

---

### 3.2 AppSettings（設定ルートオブジェクト）

```csharp
class AppSettings {
    int Version;                    // 現在は 2
    List<OscTarget> Targets;
    List<Profile> Profiles;
    HotkeySettings Hotkeys;
    KeyRepeatSettings KeyRepeat;
    InputSettings Input;
}
```

### 3.3 OscTarget

```csharp
record OscTarget {
    string IpAddress;   // 例: "127.0.0.1"
    int Port;           // 例: 9000
    bool IsEnabled;
}
```

### 3.4 HotkeySettings / HotkeyInfo

```csharp
class HotkeySettings {
    HotkeyInfo Start;
    HotkeyInfo PauseResume;
    HotkeyInfo Stop;
}

record HotkeyInfo {
    Key Key;                    // WPF Key enum
    ModifierKeys ModifierKeys;  // WPF ModifierKeys (Alt/Ctrl/Shift/Win)
    string GetDisplayText();    // 例: "Ctrl+Alt+F1"
}
```

`Key == Key.None` の場合、そのホットキーは無効（登録しない）。

### 3.5 KeyRepeatSettings

```csharp
class KeyRepeatSettings {
    bool IsEnabled;
    int InitialDelayMs;  // 最初のリピートまでの遅延 (0=即時)
    int IntervalMs;      // リピート間隔 (最小 1ms)
}
```

### 3.6 InputSettings

```csharp
class InputSettings {
    KeyboardInputMode KeyboardMode;  // VirtualKey or ScanCode
    MouseInputMode MouseMode;        // Standard or VirtualDesktop
}
```

### 3.7 ProfileExportData（インポート/エクスポート用）

```csharp
record ProfileExportData(string Name, List<SequenceSlot> Slots, bool IsLoopMode);
```

---

## 4. サービス層

### 4.1 OscSenderService

**役割:** UDP ソケットで OSC 1.0 パケットを送信する。

**主要メソッド:**

```csharp
void SetTargets(IEnumerable<OscTarget> targets)
void SendFloat(string address, float value)
void SendInt(string address, int value)
void SendBool(string address, bool value)
void SendString(string address, string value)
```

**実装詳細:**  
`UdpClient` を使用。`SetTargets` で有効な送信先リストを更新し、送信時は全有効ターゲットに同一パケットをブロードキャストする。パケット構造の詳細は [セクション 9](#9-osc-パケット仕様) を参照。

---

### 4.2 SequencePlayerService

**役割:** スロットリストを非同期で逐次実行し、ポーズ/再開/停止/ループを制御する。

```csharp
Task PlayAsync(IReadOnlyList<SequenceSlot> slots, bool loop,
               IProgress<int>? slotProgress, CancellationToken cancellationToken)
Task PauseAsync()
Task ResumeAsync()
Task StopAsync()
void SetKeyRepeatSettings(KeyRepeatSettings settings)

bool IsPlaying { get; }
bool IsPaused { get; set; }
int CurrentSlotIndex { get; set; }
```

実行の詳細は [セクション 8](#8-シーケンス実行エンジン詳細) を参照。

---

### 4.3 KeyboardSenderService

**役割:** Windows SendInput API でキーボード入力を送信する。

```csharp
KeyboardInputMode Mode { get; set; }  // デフォルト: ScanCode
void SendKey(int virtualKey, KeyAction action)
void TypeString(string text)
```

**ScanCode モード（デフォルト）:**  
`MapVirtualKey(vk, MAPVK_VK_TO_VSC)` で VK コードをスキャンコードに変換し、`KEYEVENTF_SCANCODE` フラグで送信。ゲームやエミュレータとの互換性が高い。

**VirtualKey モード:**  
`wVk` フィールドに VK コードを直接セット。`wScan = 0`。

**拡張キーフラグ:**  
以下の VK コードは `KEYEVENTF_EXTENDEDKEY` フラグが必要：

| VK        | キー     | VK        | キー         |
| --------- | -------- | --------- | ------------ |
| 0x21      | Page Up  | 0x22      | Page Down    |
| 0x23      | End      | 0x24      | Home         |
| 0x25-0x28 | 矢印キー | 0x2C      | Print Screen |
| 0x2D      | Insert   | 0x2E      | Delete       |
| 0x5B      | LWin     | 0x5C      | RWin         |
| 0x5D      | Apps     | 0x6F      | Num/         |
| 0x90      | NumLock  | 0xA3      | RCtrl        |
| 0xA5      | RAlt     | 0xAD-0xB3 | メディアキー |

**文字入力 (TypeString):**  
`\n` / `\r` → Return キーのプレス/リリース。それ以外の文字は `KEYEVENTF_UNICODE` フラグで `wScan` フィールドに文字コードをセットして送信。

---

### 4.4 MouseSenderService

**役割:** Windows SendInput API でマウス操作を送信する。

```csharp
MouseInputMode Mode { get; set; }  // デフォルト: VirtualDesktop
void SendMouseButton(MouseButton button, KeyAction action)
void SendMouseWheel(int clicks)   // 1クリック = WHEEL_DELTA(120)
void SendMouseMove(int x, int y, MouseMoveMode mode)
```

**座標変換（絶対座標時）:**

*VirtualDesktop モード（マルチモニター対応）:*

```
vx = GetSystemMetrics(SM_XVIRTUALSCREEN)   // 仮想スクリーン左端
vy = GetSystemMetrics(SM_YVIRTUALSCREEN)   // 仮想スクリーン上端
vw = GetSystemMetrics(SM_CXVIRTUALSCREEN)  // 仮想スクリーン幅
vh = GetSystemMetrics(SM_CYVIRTUALSCREEN)  // 仮想スクリーン高さ

normalized_x = (long)(x - vx) * 65535 / vw
normalized_y = (long)(y - vy) * 65535 / vh
flags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK
```

`(long)` キャストは乗算オーバーフロー防止のため。

*Standard モード（プライマリモニターのみ）:*

```
sw = GetSystemMetrics(SM_CXSCREEN)
sh = GetSystemMetrics(SM_CYSCREEN)

normalized_x = (long)x * 65535 / sw
normalized_y = (long)y * 65535 / sh
flags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
```

*相対モード:*  
`flags = MOUSEEVENTF_MOVE`（dx, dy がピクセル単位の移動量）

---

### 4.5 GlobalHotkeyService

**役割:** Windows グローバルホットキーの登録・解除・イベント通知。

```csharp
void Initialize(Window window)
void UpdateSettings(HotkeySettings settings)
event Action? StartPressed
event Action? PauseResumePressed
event Action? StopPressed
```

**初期化フロー:**
1. `WindowInteropHelper` からウィンドウハンドル取得
2. `HwndSource.AddHook(WndProc)` でウィンドウプロシージャをフック
3. 3 つのホットキーを `RegisterHotKey` で登録

**ホットキー ID:**

| ID   | 用途        |
| ---- | ----------- |
| 9001 | Start       |
| 9002 | PauseResume |
| 9003 | Stop        |

**WM_HOTKEY (0x0312) 受信時:**  
`wParam` の値 (9001/9002/9003) に応じて対応するイベントを発火。

**ModifierKeys 変換:**

| WPF ModifierKeys | Win32 定数  | 値     |
| ---------------- | ----------- | ------ |
| Alt              | MOD_ALT     | 0x0001 |
| Control          | MOD_CONTROL | 0x0002 |
| Shift            | MOD_SHIFT   | 0x0004 |
| Windows          | MOD_WIN     | 0x0008 |

---

### 4.6 JsonSettingsRepository

**役割:** `%APPDATA%\VrcOscAutomator\settings.json` への非同期読み書き。バージョンマイグレーション付き。

```csharp
Task<AppSettings> LoadAsync()
Task SaveAsync(AppSettings settings)
```

詳細は [セクション 7](#7-設定永続化とマイグレーション) を参照。

---

### 4.7 SequenceImportExportService

**役割:** プロファイルを JSON 文字列としてエクスポート/インポートする。

```csharp
string Export(string name, IEnumerable<SequenceSlot> slots, bool isLoopMode)
ProfileExportData? Import(string input)
```

- `Export`: `WriteIndented = true`、`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`（Unicode をエスケープしない）
- `Import`: デシリアライズ失敗または Slots が null/空の場合は `null` を返す

---

### 4.8 DialogService

**役割:** モーダルウィンドウ・メッセージボックスを一元管理し、ViewModel からの直接 View 参照を排除する。

```csharp
void ShowExportDialog(string json)
string? ShowImportDialog()
bool ConfirmOverwrite()
void ShowError(string message)
IEnumerable<OscTarget>? ShowSendTargetsWindow(IEnumerable<OscTarget> targets)
HotkeySettings? ShowHotkeySettingsWindow(HotkeySettings current)
KeyRepeatSettings? ShowKeyRepeatSettingsWindow(KeyRepeatSettings current)
bool ConfirmDeleteProfile(string profileName)
```

---

## 5. ViewModel 層

### 5.1 MainWindowViewModel

全体のオーケストレーターとして機能する。

**主要プロパティ（ObservableProperty）:**

| プロパティ             | 型                   | 説明                                 |
| ---------------------- | -------------------- | ------------------------------------ |
| `IsPlaying`            | bool                 | 実行中フラグ                         |
| `IsPaused`             | bool                 | 一時停止フラグ                       |
| `SelectedProfileIndex` | int                  | アクティブプロファイルのインデックス |
| `KeyboardMode`         | KeyboardInputMode    | VirtualKey / ScanCode                |
| `MouseMode`            | MouseInputMode       | Standard / VirtualDesktop            |
| `Profiles`             | ObservableCollection | プロファイル一覧                     |

**派生プロパティ:**

| プロパティ      | 計算ロジック                                               |
| --------------- | ---------------------------------------------------------- |
| `IsNotPlaying`  | `!IsPlaying`                                               |
| `CanStart`      | プロファイル選択済み、スロットが存在する、全スロットが有効 |
| `StatusMessage` | 実行状態に応じた表示文字列                                 |
| `IsLoopMode`    | 選択中プロファイルの `IsLoopMode` への転送                 |

**コマンド:**  
`StartCommand`、`PauseResumeCommand`、`StopCommand` は `CanExecute` 条件付き。起動時に `LoadedCommand` でファイルから設定を読み込み、`GlobalHotkeyService.Initialize` を呼ぶ。

**ホットキーイベント処理:**  
`GlobalHotkeyService` の各イベントに対し、UIスレッドへのディスパッチを経由して対応コマンドを実行。

---

### 5.2 ProfileViewModel

プロファイル単体のスロット一覧を管理する。

**主要プロパティ:**

| プロパティ      | 型                     | 説明                               |
| --------------- | ---------------------- | ---------------------------------- |
| `Name`          | string                 | プロファイル名                     |
| `IsLoopMode`    | bool                   | ループ実行モード                   |
| `IsRenaming`    | bool                   | リネームモード（TextBox 表示切替） |
| `SelectedSlot`  | SequenceSlotViewModel? | 選択中スロット                     |
| `Slots`         | ObservableCollection   | スロット一覧                       |
| `AllSlotsValid` | bool                   | 全スロットが有効か（computed）     |

**コマンド:**  
`AddSlot`、`RemoveSlot`（選択時のみ有効）、`CopySlot`（選択時のみ有効）、`MoveUp` / `MoveDown`（境界チェック付き）、`Export`、`Import`

---

### 5.3 SequenceSlotViewModel

12 種類以上のスロット型を単一の ViewModel で表現する。表示すべき UI 要素はすべてプリセット選択に連動した `bool` プロパティで制御される。

**核心メソッド:**

```csharp
SequenceSlot ToModel()        // ViewModel → Model 変換
static SequenceSlotViewModel FromModel(SequenceSlot slot)  // Model → ViewModel 変換
```

**プリセット選択と UI 可視性:**  
`SelectedPreset` が変わると `IsFloatMode`・`IsIntMode`・`IsBoolMode`・`IsStringMode`・`IsKeyboardSingleMode`・`IsMouseButtonMode`…等のプロパティが再計算され、DataGrid の行展開ディテール内の各 UI セクションの `Visibility` が切り替わる。

**バリデーション:**

```csharp
bool IsValid =>
    SelectedPreset is not CustomPreset ||
    OscAddressRegex().IsMatch(CustomAddress);
```

OSC アドレスは `^/[^\s]*$` に一致する必要がある。`CustomPreset` 以外では常に `true`。

**ParameterSummary:**  
スロット型とパラメーターから人間が読みやすい要約文字列を生成する computed プロパティ。DataGrid の「パラメータ」列に表示される。

---

### 5.4 SlotPreset 型階層

```
SlotPreset (abstract record, Name プロパティ)
├── WaitPreset
├── RandomWaitPreset
├── LoopBeginPreset
├── LoopEndPreset
├── BreakpointPreset
├── KeySinglePreset
├── KeyTypeStringPreset
├── MouseButtonPreset
├── MouseWheelPreset
├── MouseMovePreset
├── BuiltinPreset (Address, ValueType を追加)
└── CustomPreset
```

`SlotPreset.All` に登録された全 30+ プリセットは UI の ComboBox でグループ表示される（カテゴリ: シーケンス制御 / キーボード / マウス / OSC）。

**VRChat 向けビルトインプリセット一覧:**

| 表示名              | OSC アドレス                  | 型    |
| ------------------- | ----------------------------- | ----- |
| 前後(軸)            | `/input/Vertical`             | Float |
| 左右(軸)            | `/input/Horizontal`           | Float |
| 水平視点回転        | `/input/LookHorizontal`       | Float |
| 右手使用(軸)        | `/input/UseAxisRight`         | Float |
| 右手グラブ(軸)      | `/input/GrabAxisRight`        | Float |
| 保持:前後           | `/input/MoveHoldFB`           | Float |
| 保持:回転(時計回り) | `/input/SpinHoldCwCcw`        | Float |
| 保持:回転(上下)     | `/input/SpinHoldUD`           | Float |
| 保持:回転(左右)     | `/input/SpinHoldLR`           | Float |
| 前進                | `/input/MoveForward`          | Int   |
| 後退                | `/input/MoveBackward`         | Int   |
| 左移動              | `/input/MoveLeft`             | Int   |
| 右移動              | `/input/MoveRight`            | Int   |
| 左旋回              | `/input/LookLeft`             | Int   |
| 右旋回              | `/input/LookRight`            | Int   |
| ジャンプ            | `/input/Jump`                 | Int   |
| 走る                | `/input/Run`                  | Int   |
| スナップターン左    | `/input/ComfortLeft`          | Int   |
| スナップターン右    | `/input/ComfortRight`         | Int   |
| 右手グラブ          | `/input/GrabRight`            | Int   |
| 右手使用            | `/input/UseRight`             | Int   |
| 右手ドロップ        | `/input/DropRight`            | Int   |
| 左手グラブ          | `/input/GrabLeft`             | Int   |
| 左手使用            | `/input/UseLeft`              | Int   |
| 左手ドロップ        | `/input/DropLeft`             | Int   |
| パニックボタン      | `/input/PanicButton`          | Int   |
| クイックメニュー左  | `/input/QuickMenuToggleLeft`  | Int   |
| クイックメニュー右  | `/input/QuickMenuToggleRight` | Int   |
| ボイス              | `/input/Voice`                | Int   |

---

### 5.5 HotkeySettingsViewModel

ホットキー設定ウィンドウの VM。「キーを押してください...」リスニング状態を `ListeningTarget` enum（None / Start / PauseResume / Stop）で管理する。

```csharp
enum ListeningTarget { None, Start, PauseResume, Stop }
```

`CurrentListening != None` の間はキーイベントをキャプチャし、`HotkeyInfo` を更新。`Key.None` を設定するとそのホットキーが無効化される。

---

### 5.6 KeyRepeatSettingsViewModel

```csharp
bool IsEnabled
int InitialDelayMs   // スライダー値
int IntervalMs       // スライダー値
int IntervalRatePerSecond  // = 1000 / IntervalMs、双方向変換
```

`IntervalRatePerSecond` は `IntervalMs` と相互に変換可能なプロパティ。setter は `IntervalMs = 1000 / value` を設定し、getter は `1000 / IntervalMs` を返す。表示は「○ 回/秒」形式。

---

## 6. UI 構造

### 6.1 MainWindow

**レイアウト:**

```
Window (600×400, NoResize)
├── Menu
│   ├── ファイル
│   ├── 操作 (開始 / 一時停止・再開 / 停止)
│   └── オプション (送信先 / ホットキー / キーリピート / 入力モード)
├── StatusBar (下部): StatusMessage 表示
├── 操作パネル (下部): ループチェックボックス + 開始/一時停止/停止ボタン
└── TabControl (スクロール対応カスタムスタイル)
    ├── タブヘッダー: プロファイル名 + "×" 閉じるボタン + "+" 追加ボタン
    └── コンテンツ: ProfileView
```

**カスタム TabControl スタイル (`ScrollableTabControlStyle`):**
- タブが多い場合に横スクロール可能
- スクロールバー高さ 6px（スリム表示）
- "+" ボタンはヘッダー右端に固定

---

### 6.2 ProfileView

**レイアウト:**

```
Grid (2カラム)
├── DataGrid (左カラム、スロット一覧)
│   ├── 列: 実行中インジケーター（緑丸）
│   ├── 列: コマンド（プリセット ComboBox）
│   ├── 列: 時間(ms)（数値入力、LoopBegin/End/Breakpoint で非表示）
│   └── 列: パラメータ（ParameterSummary、読み取り専用）
│
│   RowDetailsTemplate（選択行で展開）
│   └── Border (青い左ボーダー #7B9FD4)
│       ├── Float プリセット用: スライダー (-1.0〜+1.0、0.05 刻み)
│       ├── Int プリセット用: RadioButton (1=ON / 0=OFF)
│       ├── カスタム OSC 用: アドレス入力 + 型選択 + 値入力 + ResetOnComplete
│       ├── LoopBegin 用: 繰り返し回数入力
│       ├── Wait/LoopEnd/Breakpoint: "(パラメータなし)" テキスト
│       ├── RandomWait 用: 最小・最大 ms 入力
│       ├── KeySingle 用: アクション RadioButton + キー ComboBox
│       ├── KeyTypeString 用: テキスト入力 + 改行送信チェックボックス
│       ├── MouseButton 用: ボタン RadioButton + アクション RadioButton
│       ├── MouseWheel 用: スクロール量 TextBox
│       └── MouseMove 用: 相対/絶対 RadioButton + X/Y TextBox
│
└── StackPanel (右カラム、操作ボタン)
    ├── 追加 / コピー / 削除
    ├── 区切り
    ├── 上へ / 下へ
    ├── 区切り
    └── エクスポート / インポート
```

**バリデーション表示:**  
`IsValid = false` の行は背景色が `#FFDDDD`（薄赤）になる。

**ComboBox グループ表示:**  
プリセット ComboBox はカテゴリ名を太字グレーのヘッダーで区切った GroupStyle で表示。`ListCollectionView` でグルーピング実装。

---

### 6.3 Value Converters

| クラス                             | 変換内容                                                                |
| ---------------------------------- | ----------------------------------------------------------------------- |
| `BoolToVisibilityConverter`        | `true` → `Visible`、`false` → `Collapsed`                               |
| `InverseBoolConverter`             | bool の反転（双方向）                                                   |
| `InverseBoolToVisibilityConverter` | `true` → `Collapsed`、`false` → `Visible`                               |
| `FloatToIntBoolConverter`          | `float == ConverterParameter(int)` → RadioButton の IsChecked（双方向） |

**FloatToIntBoolConverter の用途:**  
Int プリセットの ON/OFF RadioButton を `IntValue` (0 または 1) にバインドするために使用。`ConverterParameter="1"` で ON ボタン、`ConverterParameter="0"` で OFF ボタン。

---

### 6.4 NumericTextBoxBehavior（Attached Behavior）

TextBox に数値入力制約を付与するアタッチドビヘイビア。

| プロパティ           | 型     | 動作                                        |
| -------------------- | ------ | ------------------------------------------- |
| `RefreshOnLostFocus` | bool   | フォーカスアウト時にソース値からUI を再描画 |
| `AllowDecimal`       | bool   | 小数入力を許可するか                        |
| `MinValue`           | double | 最小値クランプ                              |
| `MaxValue`           | double | 最大値クランプ                              |

フォーカスアウト時の処理:
- 小数モード: `UpdateSource()` → `UpdateTarget()`（"0." → "0" などの正規化）
- 整数モード: パース後に `[MinValue, MaxValue]` にクランプして `UpdateSource()`
- パース失敗時: `UpdateTarget()` で旧値に戻す

---

## 7. 設定永続化とマイグレーション

### ファイル場所とフォーマット

- パス: `%APPDATA%\VrcOscAutomator\settings.json`
- フォーマット: JSON、camelCase、インデント付き

### V1→V2 マイグレーション

バージョン 1 では `IsLoopMode` がグローバル設定だった。バージョン 2 でプロファイルごとの設定に変更。

**旧 V1 の LegacySlot 構造:**

```json
{
  "address": "/input/Jump",
  "value": 1.0,
  "stringValue": "",
  "valueType": 0,
  "durationMs": 100,
  "resetOnComplete": false,
  "slotType": 0,
  "repeatCount": 0
}
```

**変換ルール（MigrateLegacySlot）:**

| LegacySlot.slotType | LegacySlot の状態 | 変換先                                  |
| ------------------- | ----------------- | --------------------------------------- |
| 1                   | —                 | `LoopBeginSlot(RepeatCount)`            |
| 2                   | —                 | `LoopEndSlot()`                         |
| 0                   | `address == null` | `WaitSlot(DurationMs)`                  |
| 0                   | `valueType == 1`  | `IntSlot(address, (int)value, ...)`     |
| 0                   | `valueType == 2`  | `BoolSlot(address, value != 0f, ...)`   |
| 0                   | `valueType == 3`  | `StringSlot(address, stringValue, ...)` |
| 0                   | それ以外          | `FloatSlot(address, value, ...)`        |

**V2 の settings.json 例:**

```json
{
  "version": 2,
  "targets": [
    { "ipAddress": "127.0.0.1", "port": 9000, "isEnabled": true }
  ],
  "profiles": [
    {
      "name": "Profile 1",
      "isLoopMode": false,
      "slots": [
        {
          "type": "float",
          "address": "/input/Vertical",
          "value": 0.5,
          "durationMs": 500,
          "resetOnComplete": true
        }
      ]
    }
  ],
  "hotkeys": {
    "start": { "key": 70, "modifierKeys": 2 },
    "pauseResume": { "key": 19, "modifierKeys": 2 },
    "stop": { "key": 83, "modifierKeys": 2 }
  },
  "keyRepeat": { "isEnabled": true, "initialDelayMs": 0, "intervalMs": 33 },
  "input": { "keyboardMode": 1, "mouseMode": 1 }
}
```

---

## 8. シーケンス実行エンジン詳細

### 8.1 実行ループ全体像

`SequencePlayerService.ExecuteAsync` は以下の構造で動作する。

```
do {
    loopStack をリセット
    i = 0
    while (i < slots.Count) {
        if (IsPaused) → 入力全解放 → 再開待機 → 入力再押下
        スロット型に応じた処理
    }
} while (loop モードかつ外側ループ継続)
```

### 8.2 ループスタックの動作

`Stack<(int startIndex, int remaining)>` を使用。

- **LoopBeginSlot 到達時:** `(i, RepeatCount)` をプッシュし、次スロットへ
- **LoopEndSlot 到達時:** スタックからポップし、`remaining` を評価:
  - `remaining == 0`（無限ループ）または `remaining > 1`（まだ残あり）→ `(startIndex, remaining == 0 ? 0 : remaining - 1)` を再プッシュし、`i = startIndex + 1`
  - `remaining == 1`（最後の繰り返し完了）→ 次スロットへ

**ネストの例:**

```
スロット 0: LoopBegin(2)     → push (0, 2)
スロット 1:   LoopBegin(3)   → push (1, 3)
スロット 2:     Wait(100ms)
スロット 3:   LoopEnd        → pop (1,3) → 残り2: push(1,2), i=2
              ...3回繰り返し後 pop(0,2) → 残り1: push(0,1), i=1
スロット 4: LoopEnd          → pop(0,2) ...
```

### 8.3 ポーズ/再開の状態遷移

```
[実行中]
    ↓ PauseAsync()
[ポーズ中] ─→ IsPaused=true
    │ ・押下中キー/ボタンを全解放 (ReleaseAllInputs)
    │ ・OSC 値は最後の送信値を保持
    ↓ ResumeAsync()
[実行再開] ─→ IsPaused=false
    │ ・_resumeSignal.Release() でブロックを解除
    │ ・押下中だったキー/ボタンを再押下 (RepressAllInputs)
    │ ・OSC 値を再送信
    ↓
[実行中]
```

**SlotDelayAsync の中断処理:**  
`_pauseCts`（stopCt にリンクした CancellationTokenSource）を用いて `Task.Delay` を中断。中断後は経過時間を計算し、再開後に残り時間だけ再待機する。stopCt によるキャンセルの場合は例外を再スローして終了。

### 8.4 キーリピートループ

```
StartKeyRepeat(vk, stopCt) が呼ばれる
    ↓
別タスクで RunRepeatLoopAsync 実行:
    IsEnabled == false → return
    InitialDelayMs > 0 → await Delay(InitialDelayMs, ct)
    loop:
        await Delay(max(1, IntervalMs), ct)
        !IsPaused && vk ∈ _pressedKeys → SendKey(vk, Press)
```

停止時は `StopKeyRepeat(vk)` で当該キーの CancellationTokenSource をキャンセル。`StopAllKeyRepeats()` は全キーのリピートを一括停止。

### 8.5 入力状態追跡

| 変数                   | 型                     | 内容                                        |
| ---------------------- | ---------------------- | ------------------------------------------- |
| `_pressedKeys`         | `HashSet<int>`         | 現在押下中の仮想キーコード                  |
| `_pressedMouseButtons` | `HashSet<MouseButton>` | 現在押下中のマウスボタン                    |
| `_pendingKeyRelease`   | `int?`                 | PressAndRelease の Press 後、解放待ちのキー |

`PressAndRelease` アクション実行時:
1. Press を送信
2. `_pendingKeyRelease = vk` をセット
3. `SlotDelayAsync` 終了後に Release を送信し、`_pendingKeyRelease = null`

---

## 9. OSC パケット仕様

### 9.1 OSC 1.0 パケット構造

```
[Address Block] [Type Tag Block] [Value Block(s)]
```

全ブロックは 4 バイト境界にパディング（`PadTo4(len) = (len + 3) & ~3`）。

### 9.2 Address Block

1. アドレス文字列を UTF-8 エンコード
2. ヌル終端を追加
3. 4 バイト境界まで `\0` でパディング

例: `/input/Jump` (11文字) → 11 + 1 = 12バイト → パディング不要

### 9.3 Type Tag Block

- 最初のバイト: `0x2C` (`,`)
- 型コード:
  - Float: `0x66` (`f`)
  - Int: `0x69` (`i`)
  - Bool true: `0x54` (`T`)
  - Bool false: `0x46` (`F`)
  - String: `0x73` (`s`)
- 4 バイト境界まで `\0` でパディング（通常 `[0x2C, type, 0x00, 0x00]` の 4 バイト）

### 9.4 Value Block

| 型     | バイト数 | エンコード                                                              |
| ------ | -------- | ----------------------------------------------------------------------- |
| Float  | 4        | Big-endian IEEE 754 単精度（`BinaryPrimitives.WriteSingleBigEndian`）   |
| Int    | 4        | Big-endian 符号付き 32 ビット（`BinaryPrimitives.WriteInt32BigEndian`） |
| Bool   | 0        | 型タグのみ（T/F）で表現                                                 |
| String | 可変     | UTF-8 + ヌル終端 + 4 バイト境界パディング                               |

### 9.5 パケット例

**FloatSlot: `/input/Vertical` = 0.5**

```
Addr:  2F 69 6E 70 75 74 2F 56 65 72 74 69 63 61 6C 00  (/input/Vertical\0)
Type:  2C 66 00 00                                        (,f\0\0)
Value: 3F 00 00 00                                        (IEEE754: 0.5)
合計: 24 バイト
```

**IntSlot: `/input/Jump` = 1**

```
Addr:  2F 69 6E 70 75 74 2F 4A 75 6D 70 00              (/input/Jump\0)
Type:  2C 69 00 00                                        (,i\0\0)
Value: 00 00 00 01                                        (Big-endian: 1)
合計: 20 バイト
```

**BoolSlot: `/input/Voice` = true**

```
Addr:  2F 69 6E 70 75 74 2F 56 6F 69 63 65 00 00 00 00  (/input/Voice\0\0\0\0)
Type:  2C 54 00 00                                        (,T\0\0)
(value block なし)
合計: 20 バイト
```

---

## 10. Windows API 利用

### 10.1 P/Invoke 一覧

| API                | DLL        | 用途                           |
| ------------------ | ---------- | ------------------------------ |
| `SendInput`        | user32.dll | キーボード・マウス入力送信     |
| `MapVirtualKey`    | user32.dll | VK コード → スキャンコード変換 |
| `RegisterHotKey`   | user32.dll | グローバルホットキー登録       |
| `UnregisterHotKey` | user32.dll | グローバルホットキー解除       |
| `GetSystemMetrics` | user32.dll | モニターサイズ取得             |

### 10.2 KEYBDINPUT 構造体

```csharp
[StructLayout(LayoutKind.Sequential)]
struct KEYBDINPUT {
    ushort wVk;           // Virtual Key コード（ScanCode モード時は 0）
    ushort wScan;         // スキャンコード or Unicodeコード
    uint dwFlags;         // KEYEVENTF_* フラグ
    uint time;            // 通常 0
    IntPtr dwExtraInfo;   // 通常 IntPtr.Zero
}
```

**主要フラグ:**

| フラグ                  | 値     | 意味                 |
| ----------------------- | ------ | -------------------- |
| `KEYEVENTF_SCANCODE`    | 0x0008 | スキャンコードを使用 |
| `KEYEVENTF_EXTENDEDKEY` | 0x0001 | 拡張キー             |
| `KEYEVENTF_KEYUP`       | 0x0002 | キーアップイベント   |
| `KEYEVENTF_UNICODE`     | 0x0004 | Unicode 文字入力     |

### 10.3 MOUSEINPUT 構造体

```csharp
[StructLayout(LayoutKind.Sequential)]
struct MOUSEINPUT {
    int dx;           // X 方向（絶対: 0-65535、相対: ピクセル）
    int dy;           // Y 方向（同上）
    uint mouseData;   // ホイール量（WHEEL_DELTA=120単位）
    uint dwFlags;     // MOUSEEVENTF_* フラグ
    uint time;        // 通常 0
    IntPtr dwExtraInfo;
}
```

**主要フラグ:**

| フラグ                    | 値     | 意味                 |
| ------------------------- | ------ | -------------------- |
| `MOUSEEVENTF_MOVE`        | 0x0001 | マウス移動           |
| `MOUSEEVENTF_LEFTDOWN`    | 0x0002 | 左ボタン押下         |
| `MOUSEEVENTF_LEFTUP`      | 0x0004 | 左ボタン解放         |
| `MOUSEEVENTF_RIGHTDOWN`   | 0x0008 | 右ボタン押下         |
| `MOUSEEVENTF_RIGHTUP`     | 0x0010 | 右ボタン解放         |
| `MOUSEEVENTF_MIDDLEDOWN`  | 0x0020 | 中ボタン押下         |
| `MOUSEEVENTF_MIDDLEUP`    | 0x0040 | 中ボタン解放         |
| `MOUSEEVENTF_WHEEL`       | 0x0800 | ホイール             |
| `MOUSEEVENTF_ABSOLUTE`    | 0x8000 | 絶対座標             |
| `MOUSEEVENTF_VIRTUALDESK` | 0x4000 | 仮想デスクトップ全体 |

---

## 11. 依存関係とテスト

### 11.1 NuGet パッケージ

| パッケージ                               | バージョン | 用途                                                      |
| ---------------------------------------- | ---------- | --------------------------------------------------------- |
| CommunityToolkit.Mvvm                    | 8.4.2      | MVVM source generator（ObservableProperty, RelayCommand） |
| Microsoft.Extensions.DependencyInjection | 10.0.5     | DI コンテナ                                               |
| xUnit                                    | 2.9.3      | テストフレームワーク                                      |
| Moq                                      | 4.20.72    | モックライブラリ                                          |
| FluentAssertions                         | 7.2.2      | アサーションライブラリ                                    |
| coverlet.collector                       | 8.0.1      | コードカバレッジ計測                                      |

### 11.2 テスト構成

| テストクラス                       | テスト対象                  | 主な検証内容                           |
| ---------------------------------- | --------------------------- | -------------------------------------- |
| `SequencePlayerServiceTests`       | SequencePlayerService       | 実行・ループ・ポーズ・ResetOnComplete  |
| `SequenceImportExportServiceTests` | SequenceImportExportService | シリアライズ往復・バリデーション       |
| `KeyboardSlotTests`                | KeyboardSenderService       | SendInput 呼び出し内容                 |
| `MouseSlotTests`                   | MouseSenderService          | ボタン・ホイール・移動                 |
| `MainWindowViewModelTests`         | MainWindowViewModel         | コマンド有効状態・状態遷移             |
| `ProfileViewModelTests`            | ProfileViewModel            | スロット CRUD・import/export           |
| `SequenceSlotViewModelTests`       | SequenceSlotViewModel       | ToModel/FromModel 往復・バリデーション |

全サービスは Interface 経由で注入されるため、テスト時は Moq で差し替え可能。
