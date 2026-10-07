# 現在の実装・検証状況

最終製品は iOS / Android native Unity game。この cleanup の pending tree に Web implementation はなく、Unity implementation もまだ未作成。PR #4 未 merge の間、accepted GitHub main は historical snapshot `0d8a2a68c37902256f5123ef86017ce83a60a64e` のまま。merge 後の main 用の tree と現在の accepted main を混同しない。

## Retired historical evidence

Web v0 最終 accepted snapshot は [commit `0d8a2a68c37902256f5123ef86017ce83a60a64e`](https://github.com/tomooch/rogue-tactics-lab/tree/0d8a2a68c37902256f5123ef86017ce83a60a64e)。annotated tag `archive/web-v0-final` も同 commit を指す。

- [当時の VERIFICATION.md](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/VERIFICATION.md) に11 tests、8 configurations、2026-09-22 の Pages / browser evidence を保持する。
- 旧 npm harness / runtime / source / assets / tests / build scripts は archive 内に残るが、current tree の実装・検証 requirement ではない。
- 過去の cleanup revision に対する npm / build / HTTP 成功は Git history / PR 記録に残る。今回の removal 後の tree に対する runtime 成功とは主張しない。
- human play evidence と native device play evidence はまだない。historical browser 機能確認を人試遊や Unity evidence に変換しない。

## Unity audit evidence

[PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は Draft、environment audit only / blocked。2026-10-07 の標準配置 / Spotlight 監査では Unity Hub / 6000.3.x Editor 未検出。カスタム配置を完全に否定する監査ではない。Human 指示で Unity 作業は停止中。

exact patch / build modules は未確認。Unity project / Core C# / Scene / EditMode tests は未作成、import / compile / Play / batchmode / device 検証は未実行。USER_DECISION は [DESIGN.md](DESIGN.md) と Issue #1 に保持する。

## Web retirement verification（2026-10-07）

今回の受入条件は Git diff / tree と immutable archive の確認。npm コマンドは廃止済みで実行対象外。Web runtime / source / assets / tests / build scripts、root package.json、Pages workflow を削除し、Unity implementation は追加しない。検証の実測結果は PR #4 に記録する。

Issue #1 は Design Office 側で current retirement 方針へ更新済み。Web v0 は `archive/web-v0-final` / `0d8a2a68c37902256f5123ef86017ce83a60a64e` に保持し、current main の運用として Web runtime / npm harness / Pages workflow を維持しない。Stage 0 の native Unity 作業も retired Web harness を前提にしない。PR #4 未 merge の間の accepted main と、この retirement contract に沿った pending tree は区別する。

GitHub Pages API は `build_type: workflow`、URL `https://tomooch.github.io/rogue-tactics-lab/`、source `main:/` を返した。workflow 削除後の tree には main push による Pages deployment 定義がない。ただし現在は未 merge で GitHub main の旧 workflow は残る。Pages 設定 / 公開済み site の停止・削除は未実施。Pages は current product / runtime ではない。
