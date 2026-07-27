# VrcOscAutomator 詳細設計書

実装の構造と設計判断を記述する。メソッドシグネチャ・定数・ライブラリバージョンなど、コードや csproj を見れば分かる情報は書かない（コードが正）。

関連ドキュメント:

- エクスポート JSON の形式: [import-export-schema.md](import-export-schema.md)
- スロット型の追加手順: [how-to-add-slot-type.md](how-to-add-slot-type.md)

## 目次

- [VrcOscAutomator 詳細設計書](#vrcoscautomator-詳細設計書)
  - [目次](#目次)
  - [1. システム概要](#1-システム概要)
  - [2. アーキテクチャ](#2-アーキテクチャ)
  - [3. データモデル](#3-データモデル)
    - [3.1 SequenceSlot 型階層](#31-sequenceslot-型階層)
    - [3.2 設定モデル](#32-設定モデル)
    - [3.3 ProfileExportData](#33-profileexportdata)
  - [4. サービス層](#4-サービス層)
  - [5. ViewModel 層](#5-viewmodel-層)
  - [6. UI 構造](#6-ui-構造)
  - [7. 設定永続化とマイグレーション](#7-設定永続化とマイグレーション)
  - [8. シーケンス実行エンジン](#8-シーケンス実行エンジン)
    - [ループ制御](#ループ制御)
    - [ポーズ / 再開](#ポーズ--再開)
    - [停止](#停止)
    - [キーリピート](#キーリピート)
    - [PressAndRelease](#pressandrelease)
  - [9. OSC パケット](#9-osc-パケット)
  - [10. テスト](#10-テスト)

---

## 1. システム概要

VrcOscAutomator は、VRChat の OSC インターフェースに対してコマンドシーケンスを自動実行する Windows 用 GUI ツール。キーボード・マウス入力の自動化、ループ制御、グローバルホットキーを組み合わせたシーケンスを GUI で構築・実行できる。

技術スタック: .NET / WPF、MVVM + DI（Microsoft.Extensions.DependencyInjection）。MVVM は CommunityToolkit.Mvvm の source generator 方式。テストは xUnit + Moq + FluentAssertions。

---

## 2. アーキテクチャ

```text
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

全サービスは `App.xaml.cs` で interface に対して Singleton 登録され、`MainWindow` の DataContext に `MainWindowViewModel` を注入する。Singleton なのは、各サービスがアプリ全体でひとつの状態（送信先リスト・実行状態・ホットキー登録）を保持するため。

---

## 3. データモデル

### 3.1 SequenceSlot 型階層

`Models/SequenceSlot.cs`。`abstract record SequenceSlot` を基底に、`[JsonPolymorphic]` + `[JsonDerivedType]` の discriminator（`"type"` プロパティ）で型を識別する。スロット型と各フィールドの一覧は [import-export-schema.md](import-export-schema.md) を参照。

設計上の制約:

- discriminator 文字列は settings.json とエクスポート JSON に永続化されるため、一度決めたら変更できない。
- 実行時間は基底の virtual `GetDurationMs()`（既定 0）で表現する。`RandomWaitSlot` はここでランダム値を生成する。
- `FloatSlot` / `IntSlot` のトランジション開始・終了値は nullable。`TransitionMode` が `None` のときは null とし、JSON 出力時は省略する。

### 3.2 設定モデル

| クラス                          | 内容                                                                          |
| ------------------------------- | ----------------------------------------------------------------------------- |
| `AppSettings`                   | 設定ルート。`Version`（マイグレーション判定用）と以下の各設定を持つ           |
| `OscTarget`                     | 送信先（IP アドレス・ポート・有効フラグ）                                     |
| `Profile`                       | プロファイル名・ループモード・スロット列                                      |
| `HotkeySettings` / `HotkeyInfo` | 開始 / 一時停止・再開 / 停止 の3ホットキー。`Key.None` は未設定（登録しない） |
| `KeyRepeatSettings`             | キーリピートの有効・初回遅延・間隔                                            |
| `InputSettings`                 | キーボード送信方式・マウス座標系                                              |

### 3.3 ProfileExportData

エクスポート JSON のルート。`SchemaVersion` / `AppVersion` は旧バージョンが書き出したファイルに存在しないため必須にしない。インポート時にファイルの `schemaVersion` が現在のスキーマバージョンより大きい場合は `UnsupportedSchemaVersionException` を投げ、上位層がアプリの更新を促すエラーを表示する。

---

## 4. サービス層

| サービス                      | 役割                                                                                           |
| ----------------------------- | ---------------------------------------------------------------------------------------------- |
| `OscSenderService`            | UDP で OSC 1.0 パケットを送信。全有効ターゲットに同一パケットを送る                            |
| `SequencePlayerService`       | スロット列の逐次実行とポーズ/再開/停止/ループ制御（[セクション 8](#8-シーケンス実行エンジン)） |
| `KeyboardSenderService`       | SendInput によるキー入力送信                                                                   |
| `MouseSenderService`          | SendInput によるマウス操作送信                                                                 |
| `GlobalHotkeyService`         | RegisterHotKey によるグローバルホットキー登録とイベント通知                                    |
| `JsonSettingsRepository`      | settings.json の読み書きとマイグレーション（[セクション 7](#7-設定永続化とマイグレーション)）  |
| `SequenceImportExportService` | プロファイルの JSON エクスポート/インポートと検証                                              |
| `DialogService`               | ダイアログ表示の集約。ViewModel から View への直接参照を排除する                               |

設計ポイント:

- **KeyboardSenderService**: 既定は ScanCode モード（ゲームやエミュレータとの互換性が高い）。VirtualKey モードにも切り替え可能。矢印キーなどの拡張キーには拡張キーフラグを付ける（対象キーはコードの `ExtendedKeys` を参照）。文字列入力は Unicode フラグで1文字ずつ送り、改行だけ Return キーとして送る。
- **MouseSenderService**: 既定は VirtualDesktop モード（マルチモニター対応）。絶対座標は仮想スクリーン（Standard モードではプライマリモニター）を基準に 0〜65535 へ正規化して送信する。
- **GlobalHotkeyService**: MainWindow のウィンドウプロシージャをフックし、WM_HOTKEY を受けて開始 / 一時停止・再開 / 停止のイベントを発火する。ウィンドウハンドルが必要なため、初期化は `Window.Loaded` 後。
- **SequenceImportExportService**: エクスポートはインデント付き・Unicode 非エスケープ・enum は文字列名で書き出す。インポートはスキーマバージョン確認 → デシリアライズ → スロット検証の順で、不正なデータは例外として上位層に伝える。

---

## 5. ViewModel 層

- **MainWindowViewModel**: 実行状態・プロファイル一覧・各種設定を保持する。開始可否（`CanStart`）は「未実行・有効な送信先あり・スロットあり・全スロット有効・ループ開始/終了が均衡」で判定し、満たさない理由をステータスバーに表示する。ホットキーイベントも同じ条件でガードする。
- **ProfileViewModel**: スロット一覧の追加・削除・コピー・並べ替え（複数選択対応）、リネーム、インポート/エクスポート。
- **SequenceSlotViewModel**: 全スロット型を単一の VM で表現し、プリセット選択に連動した bool プロパティで各入力パネルの表示を切り替える。`ToModel` / `FromModel` で Model と相互変換する。バリデーション対象はカスタム OSC のアドレス形式とランダム待機の範囲のみで、それ以外のスロットは常に有効。
- **SlotPreset**: コマンド選択 ComboBox に出すプリセット定義。シーケンス制御 / キーボード / マウス / OSC のカテゴリでグループ表示する。VRChat 向けビルトインプリセットの一覧は `SlotPreset.All` を参照。
- **SendTargetsViewModel / HotkeySettingsViewModel / KeyRepeatSettingsViewModel**: 各設定ダイアログ用。ホットキー設定はキー入力の待ち受け状態を持ち、押されたキーをそのまま設定値にする。

---

## 6. UI 構造

- **MainWindow**: メニュー + プロファイルタブ（横スクロール対応のカスタム TabControl）+ 実行コントロール + ステータスバー。Ctrl+1〜0 でプロファイルを切り替えられる。実行中はウィンドウを閉じられない。
- **ProfileView**: スロット一覧の DataGrid（選択行の詳細展開でパラメーターを編集）+ 操作ボタン列。無効なスロットは行の背景色で示す。
- **NumericTextBoxBehavior**: 数値入力 TextBox のフォーカスアウト時に、値の正規化・整数の範囲クランプ・不正入力の巻き戻しを行うアタッチドビヘイビア。
- **RenameBehavior**: タブヘッダーのダブルクリックでリネームモードに入る。

---

## 7. 設定永続化とマイグレーション

- パス: `%APPDATA%\VrcOscAutomator\settings.json`（JSON、camelCase、インデント付き）
- 保存は一時ファイルに書いてから `File.Move` で置き換える（書き込み途中のクラッシュでファイルが壊れないように）。
- 読み込みに失敗した場合は元ファイルを `backup/` に退避してデフォルト設定で起動し、起動時にその旨を通知する。
- ルートの `version` フィールドでマイグレーションを判定する。V1（`IsLoopMode` がグローバル設定・スロットが単一クラス）は読み込み時に V2（プロファイルごとの `IsLoopMode`・型別スロット）へ変換し、変換前のファイルをバックアップしてから保存し直す。変換規則は `JsonSettingsRepository.MigrateLegacySlot` を参照。

---

## 8. シーケンス実行エンジン

`SequencePlayerService` の中核仕様。

### ループ制御

（開始インデックス, 残り回数, 現在の反復回数）のスタックで管理する。LoopBegin でプッシュし、LoopEnd で残り回数を評価してループ先頭に戻るか抜ける。残り回数 0 は無限ループ。ネスト可。反復回数は進捗表示用に `SequenceProgress` で ViewModel へ通知する。

### ポーズ / 再開

一時停止時は押下中のキー・マウスボタンをすべて解放し、実行中スロットの OSC 値をリセット値（0 / false / ""）に戻す。再開時は OSC 値を再送信し、キー・ボタンを再押下して、スロットの残り待機時間だけ待つ。ブレークポイントスロットは到達時に自動でこの一時停止状態に入る。

### 停止

停止時・実行終了時は、実行中スロットの OSC 値リセットと全入力の解放を必ず行う（finally で保証）。キーを押しっぱなしのまま終了しないための安全策。

### キーリピート

「押す」アクションでキーを押下すると、キーリピート設定に従って別タスクが Press の再送を続ける。解放・一時停止・停止でリピートは止まる。

### PressAndRelease

Press 送信 → スロットの待機時間 → Release 送信。待機中に停止された場合も Release は必ず送る。

---

## 9. OSC パケット

OSC 1.0 準拠。アドレスブロック + 型タグブロック + 値ブロックで構成し、各ブロックは 4 バイト境界にパディングする。Float / Int はビッグエンディアン 4 バイト、Bool は型タグ（T / F）のみで値ブロックなし、String は UTF-8 + ヌル終端 + パディング。

---

## 10. テスト

サービスはすべて interface 経由で注入されるため、Moq で差し替えてテストする。テストの一覧と内容は `VrcOscAutomator.Tests` を参照。
