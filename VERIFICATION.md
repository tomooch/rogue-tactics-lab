# 現在の実装・検証状況

accepted main の実装は **historical Web v0 のみ**。Unity native implementation はまだ存在しない。

## Historical Web v0 evidence

当時の詳細は [legacy/web-v0/VERIFICATION.md](legacy/web-v0/VERIFICATION.md) に内容を保持した。2026-09-22 の Pages / browser 機能確認を今回の native play evidence として扱わない。

- Web v0 の既存自動テストは11件。4条件 × 集中 / 分割の8 configurations、deterministic replay、HP会計、死亡後効果の禁止等を検証する。
- [historical Pages URL](https://tomooch.github.io/rogue-tactics-lab/) と [初回公開 Actions](https://github.com/tomooch/rogue-tactics-lab/actions/runs/35730342366) の evidence がある。
- 当時の機能確認は人の試遊、初見の因果理解、面白さ、native 実機成功の証明ではない。

## Unity audit evidence

[PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は Draft、environment audit only / blocked。2026-10-07 の監査で標準配置と Spotlight に Unity Hub / 6000.3.x Editor が見つからなかった。カスタム配置を完全に否定する監査ではない。

- exact Editor patch / iOS・Android Build Support は未確認。
- Unity project / Core C# / Scene / EditMode tests は未作成。
- import / compile / Play Mode / batchmode は未実行。
- human play evidence と iOS / Android device play evidence はまだない。
- gameplay の未決事項は [DESIGN.md](DESIGN.md) と Issue #1 に保持する。

## Repository cleanup verification（2026-10-07）

baseline: `0d8a2a68c37902256f5123ef86017ce83a60a64e`。cleanup の実行結果は検証後に追記する。移動だけで Web v0 の戦闘ロジック・数値・runtime・tests・historical docs は変更しない。
