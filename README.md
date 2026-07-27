# VrcOscAutomator

[![CI](https://github.com/njm2360/vrchat-osc-automator/actions/workflows/ci.yml/badge.svg)](https://github.com/njm2360/vrchat-osc-automator/actions/workflows/ci.yml)
[![Release](https://github.com/njm2360/vrchat-osc-automator/actions/workflows/release.yml/badge.svg)](https://github.com/njm2360/vrchat-osc-automator/actions/workflows/release.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4?logo=windows)
![License](https://img.shields.io/badge/license-BSD%202--Clause-blue)

---

## 概要

VRChatの [OSC機能](https://docs.vrchat.com/docs/osc-overview) を使った自動化ツールです。GUI上でスロットを並べてシーケンスを組み、自動実行できます。

OSCコマンドのほか、キーボード・マウス入力の自動化にも対応しています。

## 注意事項

- 本ツールの使用によって生じたいかなる損害についても、作者は一切の責任を負いません。
- ゲームワールド内での自動化に使用する場合は、そのワールドの作者やコミュニティが定めるルールに必ず従ってください。

## 機能

### シーケンス制御

- 待機・ランダム待機・ループ（有限/無限/ネスト対応）・ブレークポイント

### OSC送信

- Float / Int / Bool / String の各型に対応
- VRChat用プリセット29種収録（移動・ジャンプ・グラブ等）
- カスタムアドレスによる任意OSC送信
- 実行完了後に値を自動リセット（ResetOnComplete）
- 複数送信先（IP:Port）の同時指定

### キーボード入力

- 単一キーの押下・解放・押して離す
- 任意文字列の入力（Unicode対応）
- キーリピート（初期ディレイ・間隔を設定可能）
- ScanCode / VirtualKeyモード切替

### マウス操作

- 左・右・中ボタンのクリック操作
- スクロールホイール
- 相対・絶対座標でのカーソル移動
- マルチモニター対応（仮想モード）

### その他

- グローバルホットキーによる開始・一時停止・停止
- 送信先を複数セットすることで2つ以上のVRChatウィンドウを自動化可能

## 動作要件

- Windows 10 / 11 (x64)
- [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## インストール

[Releases](../../releases) から最新の `VrcOscAutomator.exe` をダウンロードして任意の場所に配置してください。

## VRChat側の設定

OSC を有効化するには、VRChatのアクションメニュー → **Options → OSC → Enable** をオンにしてください。デフォルトのポート番号は9000です。

アプリ側の送信先は **オプション → 送信先** から変更できます。

## ドキュメント

| ドキュメント                                               | 内容                                       |
| ---------------------------------------------------------- | ------------------------------------------ |
| [操作マニュアル](docs/user-manual.md)                      | 基本的な使い方・各機能の操作手順           |
| [Import/Export JSONスキーマ](docs/import-export-schema.md) | エクスポートファイルの形式・スロット型定義 |

## ライセンス

[BSD 2-Clause License](LICENSE)
