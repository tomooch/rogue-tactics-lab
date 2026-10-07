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

baseline: `0d8a2a68c37902256f5123ef86017ce83a60a64e`。以下は cleanup worktree での実測結果。移動だけで Web v0 の戦闘ロジック・数値・runtime・tests・historical docs は変更しない。


- root `npm test`: 11/11 PASS、fail 0、exit 0。8 configurations の既存 fixture と deterministic replay を再現。
- root `npm run check`: PASS、8 configurations の生死・時刻・残HP・回復回数を出力。
- root `npm run build`: PASS。`legacy/web-v0/_site/` の runtime 5 files + `revision.json` + `.nojekyll` を確認。runtime は移動前とバイト一致。
- runtime / assets / tests / compare script / historical docs を baseline Git blob と照合し、バイト一致を確認。
- Ruby YAML parser で Pages workflow の構文、root npm test/build、upload path を確認。revision の SHA と `legacy/web-v0/VERIFICATION.md` URL を確認。
- root `npm start` と built Pages artifact をそれぞれローカル HTTP 配信。runtime の HTTP 200 / 内容一致、相対 imports、JavaScript MIME を確認。今回の smoke は HTTP 検証であり、ブラウザ描画・人の試遊の再検証ではない。
- canonical root の旧 `_site/` は untracked / ignored、runtime 5 files が source と一致する7ファイルの generated artifact と再確認し、ローカルだけ削除。新生成物も commit しない。
- PR #3 は Draft / HEAD `1a62aa6b7052bc3ad193587ddd73a97fae0c763b` を保持。Godot・Unity implementation は追加なし。

Local raw evidence: `/private/tmp/rogue-tactics-cleanup-verification-20261007/completion.json`、`stdout.log`、`stderr.log`（一時領域、Git 管理外）。

未検証: cleanup 後の live Pages deploy（merge / production publication は未実施）、ブラウザ描画の再確認、人の試遊、Unity / native device。既存 workflow は main push / workflow_dispatch のみで Draft PR では実行されない。今回の配置変更は Issue #1 の旧 root 配置記述に対する明示的なユーザー指示に従った。
