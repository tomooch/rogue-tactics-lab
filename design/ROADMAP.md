# 完成までの作業票

- 版: 2026-11-17 改訂2 / 作成: Claude Code
- 対象: `claude/hq-graphics-release`（Draft PR #23）時点の状態から、[GAME_DESIGN.md §3.3](GAME_DESIGN.md) の S3（リリース候補）まで
- 改訂2: Human が 2026-11-17 に判断待ち15件を「推奨通り」で決定（[DECISIONS.md](DECISIONS.md)）。判断待ちだった仕事を、詳細設計（[specs/](specs/)）つきの作業票に書き換えました。
- 位置づけ: 作業の順番と完了条件です。どの票に着手するか、本 branch をどう取り込むかは Executor が決めます。merge、Draft-to-Ready、公開は別途 Human の明示許可が要ります。

## 0. 先に結論

- **S1（1階を製品の質に）と S2（3階まで）は、Executor が誰にも聞かずに走れます。** 票ごとに、読む設計・完了条件・確かめ方があります。
- **S3 の一部は、作る前に計算での検証が要ります**（茸の番人、影こうもり、守護者、装備）。やり方は各詳細設計に書いてあります。
- **止まるのは次の3つだけ**です。
  1. [DECISIONS.md](DECISIONS.md) §3 の未決（射程 N1、音楽 N2、価格 N3 ほか）。該当する票に印を付けてあります。
  2. 人か実機でしか終わらない仕事（§5）。
  3. merge・公開などの許可。

「完成」は [GAME_DESIGN.md §11](GAME_DESIGN.md) の基準を、実機と人の試遊で満たした S3 を指します。

## 1. 票の読み方

各票は「範囲 → 読む設計 → 完了条件 → 確かめ方」です。設計に無いことは作らない。完了条件を満たせないときは範囲を黙って削らず、PR 本文に未達として書く。

共通の決まり（毎回守る）:

- 画面で起きることは必ず Core のイベントが原因。Presentation に規則を書かない。
- 測るもの（`"Private approved static skin"` / `"Fixture slime"` の mesh・transform）を演出で動かさない。カメラは一手の間は動かさない。
- 既存の hash（`GauntletCoreTests.EncounterZeroIsTheFrozenG1FixtureAndHashes`）を壊さない。
- 変更のたびに EditMode と PlayMode を回し、`RT_GFX_CAPTURE` のコマを目で見る（[CRAFT_HANDBOOK.md §7](CRAFT_HANDBOOK.md)）。
- 数値は初期値。計算（[sim/](sim/README.md)）と Core の結果が食い違ったら、原因を調べてから進む。
- Simulator の結果を実機の結果として書かない。人の試遊の結果を推定しない。

## 2. S1 — 1階を製品の質にする

いまある3戦を、人に見せられる状態にします。上から順に。

| 票 | 範囲 | 読む設計 | 完了条件 | 確かめ方 |
|---|---|---|---|---|
| S1-1 | 表示の重なりの欠陥。PlayMode `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults` が落ちる（落ちる組は実行ごとに変わる） | 下の注 | PlayMode 19/19 が連続3回。テストを緩めて通さない | PlayMode |
| S1-2 | 命中の白い光を弱め、4つの戦い方を見分けられるようにする | [specs/07](specs/07_UI_AND_CAMERA.md) §5 | 着弾コマで対象の輪郭と顔が見える。数字を隠して4種を言い当てられる | コマ、Simulator |
| S1-3 | 味方の戦闘不能と勝利の演出を拡大して確かめ、崩れを直す | [GRAPHICS.md §3〜4](GRAPHICS.md) | 三拍（予備動作・着弾・余韻）で読め、0.9 秒以内 | 味方が倒れる方針で撮ったコマ |
| S1-4 | 表示へ渡す情報を型にする（守り・致命・挟撃） | [specs/01](specs/01_CORE_RULES.md) §6 | `HqPose` と `HqJuice` から `Detail` の文字列比較が無くなる。hash 不変 | EditMode、PlayMode |
| S1-5 | 「なぜその手か」を出す | [specs/01](specs/01_CORE_RULES.md) §4、[specs/07](specs/07_UI_AND_CAMERA.md) §3 | 全 `Reason` に一言が対応。カードの一言と矢印が `Replay.Intents()` と一致 | EditMode、PlayMode |
| S1-6 | 画面の割り付けを直す。カードを詰め、行動順を横の帯に | [specs/07](specs/07_UI_AND_CAMERA.md) §1 | 3つの画面サイズで重なりなし。盤が 55% 以上 | PlayMode、Simulator |
| S1-7 | 戦闘中にカメラを寄せる | [specs/07](specs/07_UI_AND_CAMERA.md) §4 | 横幅 18.75・15.0・13.5 で `CheckVisibleHealth` が通る。カメラが動く間に一手が始まらない | PlayMode、コマ |
| S1-8 | 効果音と環境音 | [specs/05](specs/05_AUDIO.md) | 無音の Core イベントが無い。波形の検査が通る | EditMode、**人が聴く**（§5 の C7） |
| S1-9 | 設定（音量、演出を減らす、早送り2倍）と保存 | [specs/06](specs/06_SAVE_AND_SETTINGS.md) §1、§4 | どの設定でも表示したイベント列と最終状態が同じ。設定が次回起動で残る | PlayMode |

- **S1-2 の現状**（2026-11-17）: 白い光を弱める分は実装済み。4種の見分けは未完了。
- **S1-1 の注**: 2026-11-17 の3回の実行では、どれも「step 0」（まだ誰も動いていない時点）で A/B、C/X、D/B の組が重なりと判定された。演出ではなく、初期配置の時点で起きている。本 branch の演出を入れる前から落ちていた。静止メッシュ同士の重なりが原因という見立ては推論で、未確認。まず落ちる組と座標を記録して原因を特定する。S1-7（寄せる）は悪化させる可能性があるので、必ず S1-1 の後に。
- **担当**: S1-2・S1-3 は Claude（Human 指示のグラフィック担当）。Executor が進めてもよい。
- S1 が終わったら、§5 の C1〜C3（実機、性能、人の試遊）に進める状態です。**S2 に入る前に C3 の試遊を1回行うことを推奨します。** 見た目が先に進んで芯が未検証のまま機能が増えるのを防ぐためです。

## 3. S2 — 3階まで

「次を試したくなる」が階をまたいで続くかを確かめる段階です。Core の変更は [specs/01](specs/01_CORE_RULES.md) §2 の順番を守ってください。

| 票 | 範囲 | 読む設計 | 完了条件 | 確かめ方 |
|---|---|---|---|---|
| S2-1 | 方針「バランス」を足す | [specs/01](specs/01_CORE_RULES.md) §3 | 既存3戦でバランスの結果が計算と一致（3人・4人・4人生存）。hash 不変 | EditMode |
| S2-2 | 敵の頭脳を駒ごとに。苔の精と花の精 | [specs/02](specs/02_ENEMIES.md) §1〜3 | 敵が `Hunter` だけの戦闘は hash 不変。狙いの選び方を単体で固定 | EditMode |
| S2-3 | 部屋の定義と一覧（R01〜R11） | [specs/03](specs/03_DUNGEON_AND_ROOMS.md) §2〜3 | 全部屋で、3方針の生存者数と手数が一覧のとおり | EditMode |
| S2-4 | HP の持ち越し、戦闘後の回復、泉、階の移動 | [specs/01](specs/01_CORE_RULES.md) §5 | `sim/floors.py` の「よく考える人」の選択列で、同じ HP の推移になる | EditMode |
| S2-5 | 遠征の進行（2階・3階）と、成立する並び順 | [specs/03](specs/03_DUNGEON_AND_ROOMS.md) §1、§5〜6 | 既定の並びで3階まで通る。`RoomCatalog.Orders` の検査が通る | EditMode、PlayMode |
| S2-6 | 方針を選ぶ画面（3択）、2階で3つ目が増える | [specs/07](specs/07_UI_AND_CAMERA.md) §2 | 説明文が Core の定義と対応。選ぶと矢印が描き変わる | PlayMode |
| S2-7 | 遺物（規則の集約 `RuleSet`、6種、2階の終わりの報酬） | [specs/04](specs/04_RELICS_AND_GEAR.md) §1〜5 | 遺物なしは hash 不変。危機の判定と実際の被害が全遺物・全部屋で一致 | EditMode、PlayMode |
| S2-8 | 保存（途中再開）と記録 | [specs/06](specs/06_SAVE_AND_SETTINGS.md) §2〜3、§5 | 途中再開しても最終の hash が一致。壊れた保存で落ちない | EditMode |
| S2-9 | 画面のつながり（勝利後の回復を数字で見せる、泉、報酬） | [specs/07](specs/07_UI_AND_CAMERA.md) §7 | 回復の数字が Core の結果と一致。画面全体を覆う切り替えが無い | PlayMode、コマ |
| S2-10 | 茸の番人と影こうもり。**先に計算で検証** | [specs/02](specs/02_ENEMIES.md) §4〜5 | 設計の「検証のやり方」の条件を満たす部屋が見つかり、Core の結果が計算と一致 | 計算、EditMode |

- S2-10 は S2 の中で最後に。数値が未検証なので、部屋が見つからなければ設計の数値を動かす（動かす範囲は設計に書いてある）。
- S2 が終わったら、人の試遊 S2（§5 の C4）。

## 4. S3 — リリース候補

| 票 | 範囲 | 読む設計 | 完了条件 | 止まる条件 |
|---|---|---|---|---|
| S3-1 | 4階・5階（番人・こうもりを混ぜた部屋への入れ替え） | [specs/03](specs/03_DUNGEON_AND_ROOMS.md) §1、§4 | 並び順の検査が通る | S2-10 の後 |
| S3-2 | 遺跡の守護者と「退く」修正。**先に計算で検証** | [specs/02](specs/02_ENEMIES.md) §6、[specs/01](specs/01_CORE_RULES.md) §7 | 修正の使いどころで結果に差が出る数値が見つかる | — |
| S3-3 | 3階・4階の終わりの報酬（遺物の残り2組） | [specs/04](specs/04_RELICS_AND_GEAR.md) §4 | 朝露の雫と巡礼の鈴が同時に持てない | — |
| S3-4 | 記録の画面、同じ部屋に再挑戦したときの「前回」の1行 | [specs/06](specs/06_SAVE_AND_SETTINGS.md) §3 | 事実を並べるだけで、助言を出さない | — |
| S3-5 | 全滅からのやり直し、遠征の終わりの画面 | [specs/03](specs/03_DUNGEON_AND_ROOMS.md) §1.1 | その階の最初から再開できる | — |
| S3-6 | 射程 | [specs/01](specs/01_CORE_RULES.md) §8 | 全部屋を計算し直して選び直す | **N1 が未決** |
| S3-7 | 装備 | [specs/04](specs/04_RELICS_AND_GEAR.md) §6 | 「どの方針でも全員生存」になる部屋が出ない | **原画の追加が未決**。N1 の後 |
| S3-8 | 音楽 | [specs/05](specs/05_AUDIO.md) §4 | — | **N2 が未決** |
| S3-9 | 敵の種類が増えたときの描画の結合、性能の段階 | [IMPLEMENTATION_SPECS.md §7](IMPLEMENTATION_SPECS.md) | 実機で予算内 | C2 の後 |
| S3-10 | Android、ストア素材、プライバシー表示（収集なし） | [IMPLEMENTATION_SPECS.md §8](IMPLEMENTATION_SPECS.md) | — | Human の作業を含む |

## 5. 人か実機でしか終わらない仕事

agent の自動検証で代用しないでください。

| # | 仕事 | 誰が | いつ | 完了の証拠 |
|---|---|---|---|---|
| C1 | 最新 commit を実際の iPhone で動かす（署名は Human の資格情報。commit / log に入れない） | Human と Executor | S1 の後 | 対象 commit の実機記録 |
| C2 | 実機で性能を測り、[GAME_DESIGN.md §11](GAME_DESIGN.md) の数値を確定する | Executor | C1 と同時 | `HQ_FRAME_METRICS` のログ（`HqFrameMetrics.cs`）。実機で取ったもの |
| C3 | 非誘導の人の試遊 S1（3〜5人）。[GAME_DESIGN.md §1](GAME_DESIGN.md) の6つの問い | Human | S1 の後、S2 の前 | 初見の反応・発言・自発操作の記録 |
| C4 | 人の試遊 S2。遺物を選んだ理由を自分から話すか、方針を変えたくなるか | Human | S2 の後 | 同上 |
| C5 | シロの正式な8コマ歩行（Issue #13）。表情差分・背面・装備の原画を足すか | Human（原画） | 独立 | Human の合格 |
| C6 | 1時間の連続プレイで破綻がないこと | Executor と Human | S3 の後 | 記録 |
| C7 | 音を聴いて直す（スピーカーとイヤホン） | Human | S1-8 の後 | [specs/05](specs/05_AUDIO.md) §6 の5点 |
| C8 | 公開 | Human の明示許可 | 最後 | — |

## 6. 設計がまだ薄いところ

正直に書きます。次は作る前に追加の作業が要ります。

| 領域 | 何が足りないか | 誰が埋めるか |
|---|---|---|
| 茸の番人・影こうもり・守護者 | 数値が未検証。部屋がまだ無い | 票 S2-10・S3-2 の中で計算する |
| 装備 | 方向と候補だけ。数値も原画も無い | N1 と原画の判断の後 |
| 音 | 合成の数値は耳で確かめていない | 実装して聴く（C7） |
| 2階以降の地図 | 「一本道＋分岐1つ」の最小構成だけ。景色の作り込みは未設計 | 優先度が低い（[GAME_DESIGN.md §2](GAME_DESIGN.md)）。S3 の後 |
| 導入（初めての人への見せ方） | 「チュートリアル無しの導入」と書いただけ | C3 の試遊の観察から設計する。先に作らない |
| 軍師・合議・経験学習 | 長期構想（[GAME_DESIGN.md §6.5](GAME_DESIGN.md)） | S3 の範囲外 |

Notion（Todo / Decision Log / Tried & Learned）は Claude 側から未確認です。この票と [DECISIONS.md](DECISIONS.md) を Notion の代わりの正本として扱わないでください。Notion への転記と、root の AGENTS.md の未決事項の更新は Executor / Human の作業です。
