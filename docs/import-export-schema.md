# Import / Export JSON スキーマ仕様

シーケンスプロファイルを外部JSONとして書き出し・読み込みする際のフォーマット定義。
スクリプトやツールでシーケンスをプログラム的に生成・編集したい場合に参照する。

---

## ルート構造

```json
{
  "schemaVersion": 2,
  "appVersion": "1.3.1",
  "name": "プロファイル名",
  "isLoopMode": false,
  "slots": [ ... ]
}
```

| フィールド      | 型      | 必須 | 説明                                                       |
| --------------- | ------- | ---- | ----------------------------------------------------------- |
| `schemaVersion` | integer |      | このJSONのスキーマバージョン。省略時はレガシー形式として扱う |
| `appVersion`    | string  |      | 書き出したアプリのバージョン（例: `"1.3.1"`）。参考情報      |
| `name`          | string  | ✓    | プロファイルの表示名                                        |
| `isLoopMode`    | boolean | ✓    | シーケンス終端で先頭に戻るか                                |
| `slots`         | array   | ✓    | スロットの配列（空不可）                                     |

---

## スロット共通ルール

- 全スロットは `"type"` フィールド（discriminator）を持つ
- JSON プロパティ名は **camelCase**
- 文字列は UTF-8、Unicode エスケープなし
- 数値は JSON number 型（整数・浮動小数）
- 列挙型フィールド（`transitionMode` / `action` / `button` / `mode`）は 文字列名（例: `"Linear"`、`"PressAndRelease"`）で表現する

> **バージョン互換に関する注意（v1.3.0）**
> v1.3.0で列挙型フィールドの書き出し形式が整数から文字列名（例: `"PressAndRelease"`）に変更された。
>
> - v1.3.0は旧形式（整数）・新形式（文字列）どちらのJSONも読み込める（後方互換あり）。
> - v1.3.0が書き出したJSONはv1.2.0以前では読み込めない（前方互換なし）。旧バージョンで開くファイルには整数値を使うこと。

---

> **バージョン互換に関する注意（v1.3.1）**
> v1.3.1で `schemaVersion` / `appVersion` フィールドが導入された。v1.2.0・v1.3.0はどちらもこのフィールドを書き出さない。
>
> 読み込み時の判定ルール:
>
> - `schemaVersion` が **無い** 場合: レガシー形式（v1.2.0 または v1.3.0）として読み込む。enum が整数・文字列のどちらで書かれていても読める。
> - `schemaVersion` が **現在のスキーマバージョン（2）以下** の場合: 通常どおり読み込む。
> - `schemaVersion` が **現在のスキーマバージョンより大きい** 場合: このアプリより新しいバージョンで作成されたファイルとみなし、読み込みを拒否してアプリの更新を促すエラーを表示する。

---

## スロット型一覧

### OSC 系

#### `float` — Float 値送信

```json
{
  "type": "float",
  "address": "/input/Vertical",
  "value": 0.5,
  "durationMs": 500,
  "resetOnComplete": true,
  "transitionMode": "None",
  "transitionFromValue": 0.0,
  "transitionToValue": 1.0
}
```

| フィールド            | 型      | 説明                                             |
| --------------------- | ------- | ------------------------------------------------ |
| `address`             | string  | OSC アドレス（`/` 始まり）                       |
| `value`               | number  | 送信する float 値（`transitionMode: "None"` 時） |
| `durationMs`          | integer | 待機時間（ms）。補間時は補間継続時間             |
| `resetOnComplete`     | boolean | 完了後に 0.0 に戻す                              |
| `transitionMode`      | string  | 値の補間モード（下記参照）。省略時は `"None"`    |
| `transitionFromValue` | number  | トランジション開始値                             |
| `transitionToValue`   | number  | トランジション終了値                             |

**transitionMode 値:**

| 値            | 意味                   |
| ------------- | ---------------------- |
| `"None"`      | 補間なし（固定値送信） |
| `"Linear"`    | 線形補間               |
| `"EaseIn"`    | イーズイン             |
| `"EaseOut"`   | イーズアウト           |
| `"EaseInOut"` | イーズイン・アウト     |

---

#### `int` — Int 値送信

```json
{
  "type": "int",
  "address": "/input/Jump",
  "value": 1,
  "durationMs": 100,
  "resetOnComplete": true,
  "transitionMode": "None",
  "transitionFromValue": 0,
  "transitionToValue": 1
}
```

| フィールド            | 型      | 説明                                           |
| --------------------- | ------- | ---------------------------------------------- |
| `address`             | string  | OSC アドレス                                   |
| `value`               | integer | 送信する int 値（`transitionMode: "None"` 時） |
| `durationMs`          | integer | 待機時間（ms）。補間時は補間継続時間           |
| `resetOnComplete`     | boolean | 完了後に 0 に戻す                              |
| `transitionMode`      | string  | 補間モード（`float` セクション参照）           |
| `transitionFromValue` | integer | トランジション開始値                           |
| `transitionToValue`   | integer | トランジション終了値                           |

---

#### `bool` — Bool 値送信

```json
{
  "type": "bool",
  "address": "/avatar/parameters/SomeToggle",
  "value": true,
  "durationMs": 0,
  "resetOnComplete": false
}
```

| フィールド        | 型      | 説明                  |
| ----------------- | ------- | --------------------- |
| `address`         | string  | OSC アドレス          |
| `value`           | boolean | 送信する bool 値      |
| `durationMs`      | integer | 待機時間（ms）        |
| `resetOnComplete` | boolean | 完了後に false に戻す |

---

#### `string` — String 値送信

```json
{
  "type": "string",
  "address": "/chatbox/input",
  "value": "Hello VRChat!",
  "durationMs": 0,
  "resetOnComplete": false
}
```

| フィールド        | 型      | 説明                 |
| ----------------- | ------- | -------------------- |
| `address`         | string  | OSC アドレス         |
| `value`           | string  | 送信する文字列       |
| `durationMs`      | integer | 待機時間（ms）       |
| `resetOnComplete` | boolean | 完了後に `""` に戻す |

---

### 制御フロー系

#### `wait` — 待機

```json
{
  "type": "wait",
  "durationMs": 1000
}
```

| フィールド   | 型      | 説明           |
| ------------ | ------- | -------------- |
| `durationMs` | integer | 待機時間（ms） |

OSC 送信は行わず、指定時間だけ待機する。

---

#### `random_wait` — ランダム待機

```json
{
  "type": "random_wait",
  "minMs": 300,
  "maxMs": 1000
}
```

| フィールド | 型      | 説明                          |
| ---------- | ------- | ----------------------------- |
| `minMs`    | integer | 待機時間の下限（ms）。≥ 0     |
| `maxMs`    | integer | 待機時間の上限（ms）。≥ minMs |

実行時に `minMs` 〜 `maxMs` の範囲でランダムな時間だけ待機する。OSC 送信は行わない。

---

#### `loop_begin` — ループ開始

```json
{
  "type": "loop_begin",
  "repeatCount": 3
}
```

| フィールド    | 型      | 説明                             |
| ------------- | ------- | -------------------------------- |
| `repeatCount` | integer | 繰り返し回数。**0 = 無限ループ** |

必ず対応する `loop_end` とペアで使う。ネスト可能。

---

#### `loop_end` — ループ終了

```json
{
  "type": "loop_end"
}
```

フィールドなし。直近の `loop_begin` に対応する。

---

#### `breakpoint` — ブレークポイント

```json
{
  "type": "breakpoint"
}
```

フィールドなし。実行がこのスロットに到達すると一時停止する。再開操作（UI ボタンまたはホットキー）で続行。

---

### キーボード系

#### `key_single` — キー単体操作

```json
{
  "type": "key_single",
  "virtualKey": 65,
  "action": "PressAndRelease",
  "durationMs": 50
}
```

| フィールド   | 型      | 説明                                                                |
| ------------ | ------- | ------------------------------------------------------------------- |
| `virtualKey` | integer | Windows 仮想キーコード（[付録 A](#付録-a-仮想キーコード一覧) 参照） |
| `action`     | string  | キーアクション（下表）                                              |
| `durationMs` | integer | Press 後・Release 前の待機時間（ms）                                |

**action 値:**

| 値                  | 意味       |
| ------------------- | ---------- |
| `"Press"`           | 押下のみ   |
| `"Release"`         | 解放のみ   |
| `"PressAndRelease"` | 押して離す |

---

#### `key_type_string` — 文字列入力

```json
{
  "type": "key_type_string",
  "text": "Hello, World!",
  "appendNewline": true,
  "durationMs": 0
}
```

| フィールド      | 型      | 説明                           |
| --------------- | ------- | ------------------------------ |
| `text`          | string  | 入力する文字列（Unicode 対応） |
| `appendNewline` | boolean | 末尾に Enter キーを送信するか  |
| `durationMs`    | integer | 待機時間（ms）                 |

`\n` / `\r` を含む場合は Enter キーとして送信される。

---

### マウス系

#### `mouse_button` — マウスボタン操作

```json
{
  "type": "mouse_button",
  "button": "Left",
  "action": "PressAndRelease",
  "durationMs": 50
}
```

| フィールド   | 型      | 説明                                            |
| ------------ | ------- | ----------------------------------------------- |
| `button`     | string  | ボタン種別（下表）                              |
| `action`     | string  | キーアクション（key_single の action と同じ値） |
| `durationMs` | integer | 待機時間（ms）                                  |

**button 値:**

| 値         | 意味     |
| ---------- | -------- |
| `"Left"`   | 左ボタン |
| `"Right"`  | 右ボタン |
| `"Middle"` | 中ボタン |

---

#### `mouse_wheel` — マウスホイール

```json
{
  "type": "mouse_wheel",
  "clicks": 3,
  "durationMs": 0
}
```

| フィールド   | 型      | 説明                                   |
| ------------ | ------- | -------------------------------------- |
| `clicks`     | integer | スクロール量。正 = 上方向、負 = 下方向 |
| `durationMs` | integer | 待機時間（ms）                         |

内部では `clicks × 120`（WHEEL_DELTA）を SendInput に渡す。

---

#### `mouse_move` — マウス移動

```json
{
  "type": "mouse_move",
  "x": 960,
  "y": 540,
  "mode": "Absolute",
  "durationMs": 0
}
```

| フィールド   | 型      | 説明                                   |
| ------------ | ------- | -------------------------------------- |
| `x`          | integer | X 座標（絶対）またはピクセル数（相対） |
| `y`          | integer | Y 座標（絶対）またはピクセル数（相対） |
| `mode`       | string  | 移動モード（下表）                     |
| `durationMs` | integer | 待機時間（ms）                         |

**mode 値:**

| 値           | 意味     | 座標系                                 |
| ------------ | -------- | -------------------------------------- |
| `"Relative"` | 相対移動 | ピクセル単位の差分                     |
| `"Absolute"` | 絶対移動 | InputSettings.MouseMode に依存（後述） |

絶対座標の実際の変換は `InputSettings.MouseMode` による：

- **VirtualDesktop（デフォルト）:** 仮想デスクトップ全体に対する座標（マルチモニター対応）
- **Standard:** プライマリモニター上の座標

---

## 完全なサンプル

```json
{
  "schemaVersion": 2,
  "appVersion": "1.3.1",
  "name": "ジャンプ3回",
  "isLoopMode": false,
  "slots": [
    {
      "type": "loop_begin",
      "repeatCount": 3
    },
    {
      "type": "int",
      "address": "/input/Jump",
      "value": 1,
      "durationMs": 100,
      "resetOnComplete": true
    },
    {
      "type": "wait",
      "durationMs": 500
    },
    {
      "type": "loop_end"
    }
  ]
}
```

---

## 付録 A 仮想キーコード一覧

`key_single` の `virtualKey` に指定する値。

| VK（10進） | VK（16進） | キー名       |
| ---------- | ---------- | ------------ |
| 8          | 0x08       | Backspace    |
| 9          | 0x09       | Tab          |
| 13         | 0x0D       | Enter        |
| 19         | 0x13       | Pause/Break  |
| 20         | 0x14       | Caps Lock    |
| 27         | 0x1B       | Escape       |
| 32         | 0x20       | Space        |
| 33         | 0x21       | Page Up      |
| 34         | 0x22       | Page Down    |
| 35         | 0x23       | End          |
| 36         | 0x24       | Home         |
| 37         | 0x25       | ←            |
| 38         | 0x26       | ↑            |
| 39         | 0x27       | →            |
| 40         | 0x28       | ↓            |
| 44         | 0x2C       | Print Screen |
| 45         | 0x2D       | Insert       |
| 46         | 0x2E       | Delete       |
| 48–57      | 0x30–0x39  | 0 〜 9       |
| 65–90      | 0x41–0x5A  | A 〜 Z       |
| 91         | 0x5B       | LWin         |
| 92         | 0x5C       | RWin         |
| 93         | 0x5D       | Apps         |
| 112–123    | 0x70–0x7B  | F1 〜 F12    |
| 144        | 0x90       | Num Lock     |
| 145        | 0x91       | Scroll Lock  |
| 160        | 0xA0       | Shift（左）  |
| 161        | 0xA1       | Shift（右）  |
| 162        | 0xA2       | Ctrl（左）   |
| 163        | 0xA3       | Ctrl（右）   |
| 164        | 0xA4       | Alt（左）    |
| 165        | 0xA5       | Alt（右）    |
| 186        | 0xBA       | ; :          |
| 187        | 0xBB       | = +          |
| 188        | 0xBC       | , <          |
| 189        | 0xBD       | - _          |
| 190        | 0xBE       | . >          |
| 191        | 0xBF       | / ?          |
| 192        | 0xC0       | ` ~          |
| 219        | 0xDB       | [ {          |
| 220        | 0xDC       | \ \|         |
| 221        | 0xDD       | ] }          |
| 222        | 0xDE       | ' "          |
