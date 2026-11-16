# 完成までの作業票

- 版: 2026-11-17 / 作成: Claude Code
- 対象: `claude/hq-graphics-release`（Draft PR #23）時点の状態から、[GAME_DESIGN.md §3.3](GAME_DESIGN.md) の S3（リリース候補）まで
- 位置づけ: 作業の順番と完了条件の**提案**です。HUMAN-GO ではありません。採否は Executor と Human が決めます。

## 0. 先に結論

Executor が**誰にも聞かずに走れるのは A 群だけ**です。そこから先は次の2つで止まります。

- **B 群**: Human の一言（判断）が要る。推奨案は書いてあるので、Human が「推奨どおり」と答えれば走れます。答えが無いうちは実装で埋めないでください。
- **C 群**: 人か実機でしか終わらない（試遊、実機計測、原画、公開）。agent の自動検証で代用しないでください。

「完成」は [GAME_DESIGN.md §11](GAME_DESIGN.md) の基準を、実機と人の試遊で満たした S3 を指します。基準の数値は実機未計測の提案値です。

## 1. 票の読み方

各票は「範囲 → 出典 → 完了条件 → 確かめ方」の順です。出典に無いことは作らない。完了条件を満たせないときは範囲を黙って削らず、PR 本文に未達として書く。

共通の決まり（毎回守る）:

- 画面で起きることは必ず Core のイベントが原因。Presentation に規則を書かない。
- 測るもの（`"Private approved static skin"` / `"Fixture slime"` の mesh・transform・カメラ）を演出で動かさない。
- 変更のたびに EditMode と PlayMode を回し、`RT_GFX_CAPTURE` のコマを目で見る（[CRAFT_HANDBOOK.md §7](CRAFT_HANDBOOK.md)）。
- Simulator の結果を実機の結果として書かない。人の試遊の結果を推定しない。

## 2. A 群 — 今すぐ走れる（判断不要）

[確定] と [実験中] の範囲だけです。上から順に。

### A1. 表示の重なりの欠陥を直す

- 範囲: PlayMode `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults` が落ちる件。シルエットの重なりで、落ちる組は実行ごとに変わる（Y/C、D/B、X/C、C/Y、B/A を観測）。
- 出典: [GAME_DESIGN.md §12](GAME_DESIGN.md) の順2。
- 分かっていること: 本 branch の演出を入れる前から落ちていた。静止メッシュ同士の重なりが原因という見立ては推論で、未確認。まず落ちる組と座標を記録して原因を特定する。
- 完了条件: PlayMode 19/19 が連続3回。テストを緩めて通さない。
- 確かめ方: PlayMode。

### A2. HUD の重なりを解く

- 範囲: 左上の「階層」の帯と行動順の重なり。通常画面から開発用ボタンを外す。
- 出典: [IMPLEMENTATION_SPECS.md §4.1](IMPLEMENTATION_SPECS.md)、[GAME_DESIGN.md §8](GAME_DESIGN.md)。
- 完了条件: 390×844 と Simulator 実寸で、文字と部品が互いに重ならない。タップ領域 44pt 以上。
- 確かめ方: PlayMode、Simulator の画面保存。
- 注意: カードの高さを 20% 詰める案は B5（判断待ち）。ここでは触らない。

### A3. 意図・行動順・危機を盤上で読めるようにする

- 範囲: 次に動く者の実線の矢印と以降の点線、行動順の帯、倒れる恐れのある味方の「！」。
- 出典: [IMPLEMENTATION_SPECS.md §4.2](IMPLEMENTATION_SPECS.md)（部品ごとに出典の Core API が決まっている。それ以外から作らない）。意図を一巡見せるのは D3 [実験中]。
- 完了条件: 矢印と帯が Core の Plan / initiative と一致することをテストで固定。顔と HP を隠さない。
- 確かめ方: PlayMode、コマの目視。

### A4. 命中の見え方を詰める（グラフィック）

- 範囲: 着弾の白い光が一瞬対象を覆う点、剣・鈍器・魔法・矢の差が見分けにくい点。
- 出典: [CRAFT_HANDBOOK.md §5、§9](CRAFT_HANDBOOK.md)。`HqPose.LandPoses` と `HqVfx.Impact`。
- 完了条件: 着弾コマで対象の輪郭が残る。4つの戦い方を、数字を隠した状態のコマで言い当てられる。
- 確かめ方: `RT_GFX_CAPTURE` のコマ、Simulator。
- 担当: Claude（Human 指示のグラフィック担当）。Executor が進めてもよい。

### A5. 味方の戦闘不能と勝利の演出を確かめる

- 範囲: 実装済みだが拡大して見ていない。崩れていれば直す。
- 出典: [GRAPHICS.md §3、§4](GRAPHICS.md)。
- 完了条件: 両方の演出が三拍（予備動作・着弾・余韻）で読め、0.9 秒以内。
- 確かめ方: 味方が倒れる方針の組み合わせで撮ったコマ。

### A6. Core が演出へ渡す契約をテストで固定する

- 範囲: イベントの順序・`HpAfter`・行動者と対象を Presentation が推測せずに済む形。
- 出典: [IMPLEMENTATION_SPECS.md §1.1〜1.2、§1.7](IMPLEMENTATION_SPECS.md)。
- 完了条件: EditMode で不変条件と replay hash が固定されている。規則の追加はしない（`IntentReason` は B8）。

## 3. B 群 — Human の一言で走れる（判断待ち）

Human は「推奨」列でよければ番号だけ答えれば足ります。決まったら [GAME_DESIGN.md](GAME_DESIGN.md) の印を [確定] か [実験中] に書き換えてから実装してください。

| # | 判断すること | 推奨 | 決まると走れる作業 | 出典 |
|---|---|---|---|---|
| B1 | 1枚絵を骨で動かす方式を続けるか。揺れや伸縮が「うにょうにょ」「別人」に当たらないか | 続ける。当たる動きだけ外す | キャラ演出の残り全部 | GAME_DESIGN §14-1、HANDBOOK §3.2 |
| B2 | 命中の強さ（暗い暈・放射線）は絵柄に対して強すぎないか | いまの強さから白い光だけ弱める | A4 の仕上げ | HANDBOOK §9 |
| B3 | 方針を3つ（バランス・慎重・攻める）にするか（D4） | 3つ。対象を選べるのは S2 で試す | Core の方針追加、カード UI | GAME_DESIGN §4.3、SPECS §1.4 |
| B4 | 連戦で HP を持ち越すか（D5） | 持ち越す。階ごとに泉 | Core の連戦、HUD | SPECS §1.5 |
| B5 | カードを 20% 詰めて盤を広げ、戦闘中はカメラを寄せるか | 両方やる | HUD 割り付け、カメラ、投影矩形テストの調整 | SPECS §3、§4.1 |
| B6 | 音を入れるか。素材の出どころ | 入れる。Core イベント起点、自作または無料で権利の明確な素材 | 音の仕組みと対応表 | SPECS §5 |
| B7 | 敵を3種足すか（茸の番人・影こうもり・遺跡の守護者） | S2 で2種、S3 で守護者 | Core の敵規則、見た目 | GAME_DESIGN §5、SPECS §1.6 |
| B8 | 仲間の「なぜその手か」を一言出すか（`IntentReason`） | 出す | Core の Plan 拡張、カード | SPECS §1.3 |
| B9 | 成長と報酬の原則（戦い方が変わるを先に。数値成長の有無） | 原則を採る。数値成長は入れない | レリック、装備 | GAME_DESIGN §6 |
| B10 | 階と遠征の規模（S2: 3階、S3: 5階＋守護者）と、手置きの部屋の並べ替え | 提案どおり | フィールド、進行、保存 | GAME_DESIGN §3.3、SPECS §2、§6 |
| B11 | 保存と記録を入れるか | 入れる。端末内のみ、収集なし | 保存、振り返り | SPECS §6 |
| B12 | 演出を減らす設定と早送り2倍 | 入れる | 設定画面、演出の `dt` | SPECS §4.4 |
| B13 | 画面揺れを入れるか | 入れない（矩形計算をカメラから切り離す改修が先に要る） | — | HANDBOOK §2.2 |
| B14 | 配信形態（買い切り）と価格 | 買い切り | 公開準備 | GAME_DESIGN §10 |
| B15 | 本 branch を PR #21 の系列に取り込むか | Executor の採否 | 統合 | GAME_DESIGN §14-9 |

走る順番の目安（依存の少ない順）: B1・B2 → B5 → B3・B8 → B6 → B4 → B7・B9 → B10・B11 → B12 → B14。

## 4. C 群 — 人か実機でしか終わらない

| # | 仕事 | 誰が | 完了の証拠 |
|---|---|---|---|
| C1 | 最新 commit を実際の iPhone で動かす（署名は Human の資格情報。commit / log に入れない） | Human と Executor | 対象 commit の実機記録 |
| C2 | 実機で性能を測り、[GAME_DESIGN.md §11](GAME_DESIGN.md) の数値を確定する | Executor | フレーム時間の計測値（計測の仕組みは未実装。Unity の Profiler か、`Time.unscaledDeltaTime` を記録する小さな計測を足す） |
| C3 | 非誘導の人の試遊 S1（3〜5人）。[GAME_DESIGN.md §1](GAME_DESIGN.md) の6つの問い | Human | 初見の反応・発言・自発操作の記録 |
| C4 | シロの正式な8コマ歩行（Issue #13）。表情差分・背面の原画を足すか | Human（原画） | Human の合格 |
| C5 | Android 2機種、1時間 soak、ストア素材、プライバシー表示 | Executor と Human | [IMPLEMENTATION_SPECS.md §8](IMPLEMENTATION_SPECS.md) |
| C6 | 公開 | Human の明示許可 | — |

S1 の試遊（C3）を S2 の作業（B7・B9・B10）に入る条件にすることを推奨します。見た目が先に進んで芯が未検証のまま機能が増えるのを防ぐためです（[GAME_DESIGN.md §13](GAME_DESIGN.md)）。

## 5. この資料で足りないもの

正直に書きます。次は設計が薄く、該当の B が決まった後に詳細化が要ります。

- **敵の行動規則の数値**（B7）: ふるまいと「教えること」はあるが、HP・ダメージ・射程は未定。
- **レリックと装備の中身**（B9）: 原則と検証用の候補2つだけ。
- **部屋の形と並べ替えの規則**（B10）: 方針だけ。部屋の一覧は無い。
- **音の素材**（B6）: 対応表はあるが、音そのものは無い。
- **軍師・合議・経験学習**（[GAME_DESIGN.md §6.5](GAME_DESIGN.md)）: 長期構想。S3 の範囲に入れていない。

Notion（Todo / Decision Log / Tried & Learned）は Claude 側から未確認です。この票を Notion の代わりの正本として扱わないでください。
