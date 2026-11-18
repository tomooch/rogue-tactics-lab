# 引き継ぎ — Claude（Fable）がいなくなった後に

- 版: 2026-11-17 / 作成: Claude Code（Fable 5.2）
- 前提: Human から 2026-11-17 に共有。このモデルは期間限定で、近く使えなくなる。以後は Executor（Codex）が、ここに残したものだけを頼りに完成まで進める。**この会話は参照できない前提**で書いています。
- 読む人: Executor。最初にこれを読み、次に [ROADMAP.md](ROADMAP.md) へ。

## 1. 残したものと、その使い方

| 知りたいこと | 読む場所 |
|---|---|
| 次に何を作るか、完了の条件 | [ROADMAP.md](ROADMAP.md) |
| いまのコードのどこを先に直すか | [CODE_BRIDGE.md](CODE_BRIDGE.md) |
| 何が決まっていて、何が決まっていないか | [DECISIONS.md](DECISIONS.md) |
| なぜこのゲームはこう作るのか（判断の理由） | [overall/](overall/) の 01〜07 |
| 作る単位ごとの型・数値・テスト | [specs/](specs/) の 01〜07 |
| キャラ・エフェクトの作り方、数値の根拠、失敗と直し方 | [CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) |
| 数値を変えたとき、成立するかを確かめる道具 | [sim/](sim/README.md) |

文書どうしが食い違ったら、次の順で新しい方・具体的な方を採ってください。

1. 最新の Human の明示的な指示、root の `AGENTS.md`
2. [DECISIONS.md](DECISIONS.md)
3. [overall/05_AGENCY.md](overall/05_AGENCY.md)（2026-11-17 の見直し。それ以前の specs より新しい）
4. [specs/](specs/)
5. [GAME_DESIGN.md](GAME_DESIGN.md)、[IMPLEMENTATION_SPECS.md](IMPLEMENTATION_SPECS.md)（概要。古い記述が残る）

そして**実コードと文書が食い違ったら、正しいのは実コードです。** 文書を直してください。

## 2. 迷ったときの決め方

設計書に書いていない場面は必ず出ます。そのとき、次を上から順に当ててください。上の問いで決まったら、下は見ない。

1. **コンセプトに反しないか。** 「自分の予想を仲間に託し、その戦いから理解を深め、次の可能性を試す」。プレイヤーが一手ずつ指示する形、ゲームが正解を言う形になるなら、やらない。
2. **見守っていて楽しくなるか。** 5つの楽しさ（[overall/01](overall/01_EXPERIENCE.md) §2）のどれを強くするか言えるか。言えないなら、作らない。
3. **読めるか。** 矢印1本と全角8字で説明できない行動は入れない。結果に効くものは、効く前に画面に出ているか。
4. **決定論を壊さないか。** 同じ入力から同じ結果。既存の hash が変わらないか。
5. **規則が Core にあるか。** 表示が数値を計算していないか。
6. **数字が大きくなる成長になっていないか。**
7. **小さく作れるか。** 同じ効果なら、仕組みの少ない方。

それでも決まらないものは、**製品の判断**です。作らずに [DECISIONS.md](DECISIONS.md) §3 に書き足し、Human に聞いてください。聞いている間は、それに依らない票を進める。

Human は 2026-11-17 に「面白くなる方向には変更有り。コンセプトはキープ」と言っています。設計書の数値や細部は、上の 1〜6 を守る限り、面白くなる方向へ変えてよい。変えたら、理由と根拠（計算か、試遊か）を文書に残す。

## 3. 作って確かめる輪

1つの票は、次の輪を回して終わらせます。途中で止めて「できたはず」と報告しない。

```
設計を読む → （数値を変えるなら）sim で確かめる → 実装 → EditMode → PlayMode
 → コマを撮る（RT_GFX_CAPTURE）→ 目で見る（下の基準）→ 直す → もう一度
 → Simulator で通して見る → PR 本文に、対象 commit・実行した command・結果・未確認を記録
```

- コマンドと撮り方は [CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) §7。
- **見た目は、テストが通っても合格ではありません。** 必ずコマを見る。見るときは PNG の一覧ではなく JPEG のタイルで（PNG の縮小一覧は色が潰れて判断を誤る。実際に誤りました）。
- 人の試遊と実機は、この輪の外です（[overall/07](overall/07_PLAYTEST.md)、[ROADMAP.md](ROADMAP.md) §5）。Simulator の結果を実機の結果として書かない。

### 3.1 見た目の合否（感覚に頼らないために）

コマを見て、次のどれか1つでも当てはまったら不合格です。

| # | 不合格の条件 | 直し方の入口 |
|---|---|---|
| 1 | 着弾の瞬間に、当たった側の顔か輪郭が見えない | `HqVfx.Impact` の `Glow`・`Ring`（[specs/07](specs/07_UI_AND_CAMERA.md) §5 に経緯） |
| 2 | 数字が読めない（切れる、体に被る、背景に溶ける） | `HqJuice`。枠を `Overflow` に。縁取りと影（[CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) §6） |
| 3 | 承認済みの4人が別人に見える（顔が歪む、目の位置が動く。顔の中の変形） | 頭の骨の回転は待機・歩きで ±0.06rad、被弾・倒れ・起き上がりで ±0.2rad まで（いまの値）。超えたら Head の Strength を上げる |
| 4 | 脚や腕がゴムのように伸び縮みする | 脚は変形させない。体全体の傾きと弾みで見せる |
| 5 | 誰が誰に何をしたかが、1コマずつ追っても分からない | 予備動作が短い、または光が多すぎる |
| 6 | 光が白い石の上で見えない | 先に暗い暈（`Veil`）を敷く |
| 7 | 足元の HP や顔カードが、エフェクトに隠れる | エフェクトの大きさと位置 |
| 8 | 測るもの（`"Private approved static skin"`、`"Fixture slime"`）の位置や大きさが動いた | 動きはシェーダーの頂点オフセットだけに |

#1 は自動で見張っています（票 S1-13、PlayMode `GfxReadabilityWatchdogTests`）: 着弾ごとに当たった側の顔の「白に近い画素」の割合を毎フレーム数え、しきい値を超えたら落とす。しきい値と実測は [CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) §5.4.1。テストが通っても、コマを見る手順は省かない。ほかの項目の自動の見張りは、まだありません。

## 4. 素材の扱い

| もの | 場所 | 決まり |
|---|---|---|
| 承認済みの原画（4人） | `unity/Assets/RogueTactics/Art/MobileG1/Private/`、`_userwork/` | **Git に入れない。外部のサービス・別の AI・クラウドに送らない。編集しない。** 公開する文書には名前・寸法・SHA だけ |
| 撮ったコマ、録画 | `unity/Logs/hq/private/`、作業用の一時領域 | Git に入れない。PR やコメントに貼らない（原画が写っている） |
| シェーダー、メッシュの生成、エフェクト | `Presentation/Gfx/`、`Resources/Gfx/` | すべて計算で作っている。テクスチャ素材なし。追加もこの方針で |
| 音 | まだ無い | 合成で作る設計（[specs/05](specs/05_AUDIO.md)）。素材を使うなら無料で権利の明確なものだけ、出典を記録 |
| 有料素材、外部委託、生成 AI サービス | — | Human の明示の許可が要る |

キャラの絵を**描き足さない**でください。新しい絵が要る仕事（表情差分、背面、装備、8コマ歩行）は Human の判断と原画待ちです。いまある1枚絵を骨で動かす範囲で作る。

## 5. グラフィックの現在地

| もの | 状態 | 残り |
|---|---|---|
| 味方4人の骨と型（詠唱・盾・剣・弓） | 実装済み | — |
| 踏み込み、のけぞり | 実装済み | — |
| 敵（丸いトゥーン） | 番兵（1階）・苔・花・茸の番人・影こうもり・守護者の6種の見た目を実装済み（後の3種は 2026-11-18〜19、Core 未接続。CRAFT §4.x） | 後の3種を Core の敵種に接続（S2-10・S3-2） |
| エフェクト 14 形・レシピ 17 | 実装済み | 退く |
| 遺物の絵 6 種、泉の絵 | 表示だけ実装済み（2026-11-19、PR #24）。`HqRelicArt`（`Icon/Sprite/Accent`、id は specs/04 §3 の表と同じ。未知 id は例外）、`HqSpringProp`（`Build`・`Drink`・`SetSpent`）。**Core には未接続** | 遺物の効いた瞬間の光（`BattleEvent.Relic`、S2-7）、泉の回復数字（S2-9）、`SetSpent` は保存の復元（S2-8）から |
| 命中の白い光 | 弱めた（2026-11-17）。戦い方ごとに `Impact` を絞った（2026-11-18、CRAFT §5.3.1） | 自動の見張り（S1-13） |
| 剣・鈍器・魔法・矢の見分け | 形で作り分け、2026-11-18 のコマで4種とも見分けを確認（矢は `RT_GFX_ENCOUNTER=2`） | 人が数字を隠して言い当てる確認（S1-2 の完了条件）は未実施 |
| 味方の戦闘不能 | 空いている側へ倒れ、土煙と星、横たわって残る。戦闘だけの build では勝つと起き上がる（2026-11-18、コマで確認） | 製品の流れでの復帰（25%、S2-4）を Core に入れたら、起き上がりを製品の流れでも使う（CRAFT §3.3.1） |
| 勝利の演出 | 実装済み。紙吹雪は控えめ。倒れる三拍は 0.72 秒で着地（コマ） | 紙吹雪を強めるかは試遊で |
| ダメージ数字 | 実装済み | — |
| 仲間が「気づく」演出 | 未実装 | [overall/05](overall/05_AGENCY.md) §3.6 |
| 音 | 未実装 | [specs/05](specs/05_AUDIO.md) |

足し方は、どれも同じ手順です（[CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) §9）: レシピを1つ足す → `HqPose` から呼ぶ → コマを撮って §3.1 で確かめる。

## 6. 分かっている落とし穴

実際に踏んだものです。

| 落とし穴 | 何が起きたか | 避け方 |
|---|---|---|
| 存在を検索だけで断定する | クラス名で検索して「`HqFrameMetrics` は無い」と書いた。ファイル名だった | 無いと書く前に、ファイル名と中身の両方を探す |
| 数字の枠が小さい | ダメージ数字が最初から1つも出ていなかった（枠で切れていた）。テストは通っていた | 画面を見る |
| 満タン前提の盤に持ち越しを入れる | 誰がやっても8戦目で全滅した | 規則を足したら `sim/floors.py` を回す |
| 部屋の順番を自由に入れ替える | 432 通り中、成立するのは 252 通り | `sim/orders.py`、`sim/routes.py` |
| 遺物を「強くする」方向で作る | 反撃 2→4 は、持つだけで 42 通り中 15 通りが良くなり、悪くなる場面が無かった | `sim/relics.py` で増減の両方を数える |
| Unity の書き出しが設定ファイルを書き換える | `ProjectSettings.asset` などに差分が出た | 書き出しの後に差分を戻す。commit しない |
| コマ撮りの頻度を上げると別のテストが落ちる | `HQ_FRAME_METRICS` のログが次のテストに混ざった | 合否を見る実行と、コマを撮る実行を分ける |
| 同じ Unity project を2つ同時に開く | — | 実行前に他のプロセスを確かめる。自分の worktree だけを使う |
| 修正を入れた直後に閉じる | `Replay.Correct()` は入力の列に残らない | 保存は表示側で入力を記録する（[specs/06](specs/06_SAVE_AND_SETTINGS.md) §2.1） |

## 7. GitHub と Notion に移すもの

Claude 側からは Notion を読むことも書くこともできませんでした。**次は未同期です。** Executor / Human が転記してください。転記するまで、[DECISIONS.md](DECISIONS.md) は写しであって正本ではありません。

| 移すもの | 元 | 先 |
|---|---|---|
| 2026-11-17 の決定 15 件（B1〜B15） | [DECISIONS.md](DECISIONS.md) §2 | Notion Decision Log |
| 「面白くなる方向には変更有り。コンセプトはキープ」「Fable は期間限定。Codex が完成まで進める」 | [DECISIONS.md](DECISIONS.md) §1 | Notion Decision Log |
| 未決事項 | [DECISIONS.md](DECISIONS.md) §3 | Notion Todo（Human の判断待ち） |
| 作業票 | [ROADMAP.md](ROADMAP.md) | Notion Todo、GitHub Issue（票ごと、または S1・S2・S3 ごと） |
| 学んだこと | 上の §6、[CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) §8 | Notion Tried & Learned |

root の契約文書で、古くなっている記述（直すのは Executor / Human。Claude は触っていません）:

- `AGENTS.md`「Unresolved USER_DECISION」: 探索入力（D1）は確定、Resolution chunk（D2）と Intent horizon（D3）は実験中、と共有資料で更新済み。本文は未更新。
- `AGENTS.md`「Current frozen direction」の「表示手数は未決」: D3 と同じ。
- `AGENTS.md` / `README.md` / `DESIGN.md` / `VERIFICATION.md` の PR の状態（PR #5 は merge 済みなのに Draft と書かれている、など）: Draft PR #6 が直している。
- `DESIGN.md` に、この `design/` ディレクトリへの参照が無い。

## 8. このブランチの扱い

- `claude/hq-graphics-release` は Draft PR #23。base は `codex/hq-ref-vertical-slice`（Draft PR #21 の系列）。
- 取り込むかどうか、どう取り込むかは Executor が決めます（[DECISIONS.md](DECISIONS.md) B15）。merge と Draft-to-Ready は Human の明示許可。
- 取り込むときの注意: `design/` は丸ごと独立しています。コードの変更は `Presentation/Gfx/`、`Presentation/HqPose.cs`・`HqJuice.cs`・`HqMotion.cs`・`HqCreature.cs`（敵の見た目と名前）・`HqReferenceHud.cs`（`SpiritPortraitGraphic`）・`HqProductHud.cs`・`HqGauntlet.cs`・`MobileG1View.cs`（`ResizeButton`、`ResetProjection` ほか小変更）、`Resources/Gfx/`、`Editor/HqFloorSetup.cs`（実機用の `ExportIos` を追加）、`Tests/PlayMode/`。確かめるには `git diff --stat $(git merge-base HEAD origin/codex/hq-ref-vertical-slice) HEAD -- unity`。
- PlayMode は 23 件合格・1件スキップ（撮影専用）（PR #24 取り込み後。2026-11-18 に S1-1 を解決して 19/19 を3回連続、S1-13 の白飛びの見張りを足して 20 件、後の階の敵の見た目の一覧 `GfxSpiritGalleryTests` を足して 21 件、遺物・泉の一覧 `GfxRelicSpringGalleryTests` 3件を足して 24 件。うち `CaptureSpringAndIconsOnTheBoard` は `RT_GFX_CAPTURE` 指定時だけ動き、通常は skipped で不具合ではない。2026-11-19 に 24 件を実行して 23 通過 + 1 skipped を確認）。
