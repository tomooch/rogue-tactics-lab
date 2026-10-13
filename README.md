# Rogue Tactics Lab

最終製品は **iOS / Android 向け native Unity game**。開発方向は **Unity 6.3 LTS (6000.3.x) / C# / URP / portrait mobile**。

Current design / experiment contract は [DESIGN.md](DESIGN.md)、[VERIFICATION.md](VERIFICATION.md)、[GitHub Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。agent 運用は [AGENTS.md](AGENTS.md)。

## 現在地

accepted implementation は常に actual GitHub `main` HEAD。PR #4 は merge 済みで、historical Web v0 は retired 済み。main に Web implementation / npm harness / Pages workflow はなく、Unity Stage 0 はPR #5でmerge済み（今回のbaseline `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。

[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) はmerge済み。以下のStage 0実測はmerge前の対象revisionの記録。`unity/` の URP・portrait 基盤と pure C# / asmdef / test scaffold が検証済み。Unity **6000.3.25f1**、import / compile PASS、EditMode **5/5 PASS**、PlayMode **1/1 PASS**、390×844 portrait smoke PASS。実行した pending HEAD は `6a0879e8877cf0717d2ac5019a923ea11fbbd0af`。証拠と限界は [VERIFICATION.md](VERIFICATION.md) に記録する。Stage 0にgameplayは含まない。今回のG1は下記の未merge実装。実機未検証、CI なし、human play 未実施。

- [Godot PR #2](https://github.com/tomooch/rogue-tactics-lab/pull/2) は closed / unmerged。流用しない。
- [旧 Unity audit PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は closed / unmerged / historical audit evidence。旧 branch は保持し、新しい Stage 0 では再利用していない。
- Humanは盤外からpartyを監督するB方向を承認済み（#1）。探索gesture、resolution chunk、intent horizonは未決。G1で正式仕様として固定しない。

## Retired historical Web evidence

Web v0 は retired historical evidence。active tree に保持せず、[最終 accepted snapshot](https://github.com/tomooch/rogue-tactics-lab/tree/0d8a2a68c37902256f5123ef86017ce83a60a64e) **`0d8a2a68c37902256f5123ef86017ce83a60a64e`** と annotated tag `archive/web-v0-final` で参照する。再現が必要なら archive を別 checkout で使用し、current main に runtime や旧 tooling を戻さない。npm test / check / build / start は archive のコマンドであり、current repo requirement ではない。

[当時の検証記録](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/VERIFICATION.md) は historical browser の証拠であり、native 実装や人の試遊成功の証拠ではない。Pages は current product / runtime ではない。

PR #4 は merge 済み。Pages は Human が Unpublish 済み（[Human の報告と docs sync 承認](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298)）。workflow 削除と Unpublish は別操作であり、agent が今回外部設定を変更したという意味ではない。

## G1 gameplay experiment / Issue #14

現在のaccepted mainにはPR #5のStage 0がmerge済み（`ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。今回の未merge実装は[Issue #14](https://github.com/tomooch/rogue-tactics-lab/issues/14)に従う固定1戦闘であり、one-floor完成ではない。

Unity **6000.3.25f1**でこのbranchの`unity/`を開き、`Assets/RogueTactics/Scenes/G1Battle.unity`をPlay。Game viewは390×844 portraitにする。

1. `Rules`で匿名役A/B/C/D・敵X/Yの数値と発動条件を読む。
2. `B: Protect A`または`B: Direct attack`を選ぶ。変更点はBの選好だけ。
3. 任意の自分の予想を入力し、`Start this setup`を押す。
4. `Next action`で1行動ずつ、または`Play to end`で順に観察する。`Pause`で止められる。線・記号・移動先ghostはCoreの現在plan、HPと行動ログは確定eventを表示する。
5. 終了後`Baseline`へ戻り、もう一方の条件で再試行する。結果は独立に保持され、HP／生存数／行動数／残り回復を比較する。`Rules`→`Trials / predictions`で条件ごとの事前予想とtrace hashも確認できる。

A/B/C/Dは匿名の実験役。正式キャラのシロ／ミャル／ガル／モモとは対応しない。1 action／encounter playbackや現在plan表示は検証adapterであり、正式resolution chunk／intent horizonの採用ではない。探索・relic・第二戦・意図修正・E提案はG2以降。[Design Freeze G1](DESIGN.md#design-freeze-g1--2026-10-13--issue-14)、実測・限界は[VERIFICATION.md](VERIFICATION.md)。
