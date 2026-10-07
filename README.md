# Rogue Tactics Lab

最終製品は **iOS / Android 向け native Unity game**。開発方向は **Unity 6.3 LTS (6000.3.x) / C# / URP / portrait mobile**。

Current design / experiment contract は [DESIGN.md](DESIGN.md)、[VERIFICATION.md](VERIFICATION.md)、[GitHub Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。agent 運用は [AGENTS.md](AGENTS.md)。

## 現在地

この cleanup の tree は Unity native 開発用の入口であり、Web implementation / npm harness / Pages workflow を持たない。Unity implementation もまだ未作成。将来の配置先は `unity/`。

- [Godot PR #2](https://github.com/tomooch/rogue-tactics-lab/pull/2) は closed / unmerged。流用しない。
- [Unity bootstrap PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は Draft、environment audit only / blocked。Unity Hub / 6000.3.x 未検出という監査結果であり、実 Editor による生成は未実施。Human 指示で作業停止中。
- 探索入力 / player embodiment、resolution chunk、intent horizon は USER_DECISION 未決。実装で固定しない。

## Retired historical Web evidence

Web v0 は retired historical evidence。active tree に保持せず、[最終 accepted snapshot](https://github.com/tomooch/rogue-tactics-lab/tree/0d8a2a68c37902256f5123ef86017ce83a60a64e) **`0d8a2a68c37902256f5123ef86017ce83a60a64e`** と annotated tag `archive/web-v0-final` で参照する。再現が必要なら archive を別 checkout で使用し、current main に runtime や旧 tooling を戻さない。npm test / check / build / start は archive のコマンドであり、current repo requirement ではない。

[当時の検証記録](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/VERIFICATION.md) は historical browser の証拠であり、native 実装や人の試遊成功の証拠ではない。Pages は current product / runtime ではない。

PR #4 が未 merge の間は GitHub main に旧 Web v0 が残る。この PR の merge 後の current main は Web implementation と Pages deployment workflow を持たない。既存 Pages 設定や公開済み site の停止は workflow 削除とは別操作であり、本 cleanup では変更しない。
