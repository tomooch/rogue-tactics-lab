# Rogue Tactics Lab

最終製品は **iOS / Android 向け native Unity game**。開発方向は **Unity 6.3 LTS (6000.3.x) / C# / URP / portrait mobile**。

Current design / experiment contract は [DESIGN.md](DESIGN.md)、[VERIFICATION.md](VERIFICATION.md)、[GitHub Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。agent 運用は [AGENTS.md](AGENTS.md)。

## 現在地

accepted implementation は常に actual GitHub `main` HEAD。PR #4 は merge 済みで、historical Web v0 は retired 済み。main に Web implementation / npm harness / Pages workflow はなく、Unity Stage 0 はまだ main に取り込まれていない。

[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) は Draft / pending。`unity/` の URP・portrait 基盤と pure C# / asmdef / test scaffold が検証済み。Unity **6000.3.25f1**、import / compile PASS、EditMode **5/5 PASS**、PlayMode **1/1 PASS**、390×844 portrait smoke PASS。実行した pending HEAD は `6a0879e8877cf0717d2ac5019a923ea11fbbd0af`。証拠と限界は [VERIFICATION.md](VERIFICATION.md) に記録する。gameplay は未実装、実機未検証、CI なし、human play 未実施。

- [Godot PR #2](https://github.com/tomooch/rogue-tactics-lab/pull/2) は closed / unmerged。流用しない。
- [旧 Unity audit PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は closed / unmerged / historical audit evidence。旧 branch は保持し、新しい Stage 0 では再利用していない。
- 探索入力 / player embodiment、resolution chunk、intent horizon は USER_DECISION 未決。実装で固定しない。

## Retired historical Web evidence

Web v0 は retired historical evidence。active tree に保持せず、[最終 accepted snapshot](https://github.com/tomooch/rogue-tactics-lab/tree/0d8a2a68c37902256f5123ef86017ce83a60a64e) **`0d8a2a68c37902256f5123ef86017ce83a60a64e`** と annotated tag `archive/web-v0-final` で参照する。再現が必要なら archive を別 checkout で使用し、current main に runtime や旧 tooling を戻さない。npm test / check / build / start は archive のコマンドであり、current repo requirement ではない。

[当時の検証記録](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/VERIFICATION.md) は historical browser の証拠であり、native 実装や人の試遊成功の証拠ではない。Pages は current product / runtime ではない。

PR #4 は merge 済み。Pages は Human が Unpublish 済み（[Human の報告と docs sync 承認](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298)）。workflow 削除と Unpublish は別操作であり、agent が今回外部設定を変更したという意味ではない。
