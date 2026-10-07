# 現在の実装・検証状況

accepted implementation は常に actual GitHub `main` HEAD。[PR #4](https://github.com/tomooch/rogue-tactics-lab/pull/4) は merge 済みで、historical Web v0 は retired 済み。main に Web runtime / npm harness / Pages workflow はない。Unity Stage 0 は PR #5 により main へ merge 済み。

[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) は **2026-10-08 JST に merge 済み**（merge commit `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。Stage 0 の実行済み evidence は **PR #5 の旧検証 HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af`** に紐付く。以下はその revision の Local Mac evidence であり、merge commit や以後の HEAD での新しい実行結果、実機、人の試遊の成功へ読み替えない。docs status sync は [Design Office comment](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298) と最新 Human 指示により承認済み。

## Unity Stage 0 evidence（2026-10-08 JST）

- 実行時 branch: `codex/issue-1-unity-stage0`
- PR #5 baseline main: `daa00c50e0d43a1911b6df6d1f59eee36b797145`
- Unity 実行済み HEAD: `6a0879e8877cf0717d2ac5019a923ea11fbbd0af`
- environment: Local Mac / Apple Silicon arm64
- Unity Hub: 3.22.2
- Unity Editor: **6000.3.25f1 (e1dba0a9aba4)**。実 Editor による生成と `ProjectVersion.txt` の pin を確認。
- installed modules: iOS Build Support、Android Build Support、Android SDK & NDK Tools、NDK r27c、OpenJDK 17.0.18+8、SDK Build / Platform Tools 36.0.0、SDK platforms 34/35/36/37、command-line tools 16.0、CMake 3.22.1。
- Human 操作: Hub 利用規約の確認・同意と account sign-in。Unity Personal license 有効を確認。credential / secret は repo や command 引数に保存していない。

| 確認 | 実測結果と範囲 |
| --- | --- |
| project generation / import | PASS、実 Editor の同梱 URP template を使用、exit 0 |
| compile / asmdef | PASS、C# compile error 0。Core は `noEngineReferences: true`、Unity / Presentation 依存なし |
| EditMode | **5/5 PASS**、exit 0。整数座標の同一入力列による同一座標列、overflow、Core 依存境界、portrait 設定、URP 設定 |
| PlayMode | **1/1 PASS**、exit 0。Stage0 Scene を load し、Play Mode の frames と描画を実行。`LogAssert.NoUnexpectedReceived` PASS |
| 390×844 portrait smoke | **PASS**。実 render target で text bounds / preferred size を検証し PNG 保存。agent が画像を確認し、横方向 clipping なし |
| final worktree | clean。初回 PlayMode による URP schema 12→13 migration を保存後、上記 HEAD で両 test mode を再実行 |

Core の deterministic 検証は座標演算 scaffold の範囲。legal movement、戦闘、意図、event sequence や one-floor gameplay の検証ではない。portrait Scene は静的な基盤表示のみ。exploration input / player embodiment、resolution chunk、intent horizon は USER_DECISION のまま。party policy 最大3つは experiment parameter / prototype candidate のまま。

Editor log に非阻害の license access-token refresh と usbmuxd diagnostics が残る。license を使用した Editor 実行・test・render は成功し、C# compile error や予期しない PlayMode error は検出されていない。すべての log が無警告とは主張しない。

### 再実行と evidence の場所

Editor executable: `/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity`。`<worktree>` は PR #5 の専用 checkout。下記を Editor の引数として使う。Scene 再生成は必要な場合だけ実施する。

```text
-batchmode -quit -createProject <worktree>/unity -cloneFromTemplate <editor>/Contents/Resources/PackageManager/ProjectTemplates/com.unity.template.3d-cross-platform-17.0.14.tgz -logFile <log>
-batchmode -quit -projectPath <worktree>/unity -executeMethod RogueTactics.Editor.Stage0Setup.Configure -logFile <log>
-batchmode -projectPath <worktree>/unity -runTests -testPlatform EditMode -testResults <results.xml> -logFile <log>
-batchmode -projectPath <worktree>/unity -runTests -testPlatform PlayMode -testResults <results.xml> -logFile <log>
```

生成 command は未生成のパスでのみ使用する。既存 checkout での通常の検証は最後の2 command を順に実行する。

Local evidence は Git 管理外の一時領域:

- `/private/tmp/rogue-stage0-jobs/tests-final/completion.json`、`stdout.log`（実行対象 HEAD と結果）
- `/private/tmp/rogue-stage0-EditMode.xml` / `.log`
- `/private/tmp/rogue-stage0-PlayMode.xml` / `.log`
- `<worktree>/unity/Logs/stage0-portrait.png`（ignored local output）

要約 evidence は PR #5 本文にも記録済み。PR #5 の docs sync と今回の merge 後 docs 整合は4文書のみの変更であり、旧検証 HEAD から Unity source / assets / packages / settings / tests を変えない。merge commit と今回の docs HEAD の Unity tree の一致を Git diff で確認する。Unity tests は再実行しておらず、旧検証 HEAD の結果を新 HEAD の実行結果とは扱わない。

## Merge 後の main 同期・docs 整合（2026-10-08 JST）

- canonical main は clean を確認し、origin fetch 後に `git merge --ff-only origin/main` で `ab9fa5dc729dcea07f8680e7b22544627c5e5bed` へ同期した。この SHA は同期時の観測値であり、accepted implementation の定義は常に actual GitHub main HEAD。
- 既存 worktree / branch は保持し、新規専用 worktree / `codex/stage0-merged-docs` branch で README / DESIGN / VERIFICATION / AGENTS の status 記述のみを修正する。
- self-review と `git diff --check` を実施。Unity / .gitignore、gameplay scope、3つの USER_DECISION、party-policy experiment parameter は変更しない。

## 未実施 / 管理記録

- iOS / Android 実機、署名・export / build は未検証。
- CI workflow / PR checks はなし。CI 成功は主張しない。
- human play 未実施。面白さや gameplay 仮説の成立は未検証。
- one-floor gameplay と後続 pure Core gate は未実装・未検証。
- PR #5 関連の Notion Todo / review tracking は Design Office 側で同期済み（[final review comment](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6047665493) と最新 Human 指示）。新しい gameplay Decision は発生していない。新しい Tried & Learned が必要とは現時点で判断していない。Issue / PR の evidence を Notion 正本に昇格させない。

## Historical Unity audit

[PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は **closed / unmerged / historical audit evidence**。旧 main と Web/npm 前提のため implementation branch に再利用せず、branch は保持した。2026-10-07 の標準配置 / Spotlight 監査で Hub / 6000.3.x 未検出だった事実は historical evidence として保持する。現在の環境不足や bootstrap 停止を意味しない。

## Retired historical Web evidence

Web v0 最終 accepted snapshot は [commit `0d8a2a68c37902256f5123ef86017ce83a60a64e`](https://github.com/tomooch/rogue-tactics-lab/tree/0d8a2a68c37902256f5123ef86017ce83a60a64e)。annotated tag `archive/web-v0-final` も同 commit を指すことを確認済み。

- [当時の VERIFICATION.md](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/VERIFICATION.md) に11 tests、8 configurations、2026-09-22 の Pages / browser evidence を保持する。
- Web source / assets / tests / npm harness / build scripts は archive のみ。current requirement に戻さず、native runtime・実機・human play evidence に変換しない。
- PR #4 の retirement は Git diff / tree と archive 参照で検証し、merge 済み。Stage 0 branch でも Web runtime / root package.json / Pages workflow の不在を確認した。

## Pages status

Pages は **Human が Unpublish 済み**（[Human の報告](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298) と今回の明示指示）。agent が公開停止操作や現在の外部設定の live 検証を行ったという意味ではない。Pages は current product / runtime ではない。

2026-10-07 の cleanup 時点で Pages API が `build_type: workflow`、source `main:/` と公開 URL を返した記録は過去の観測であり、現在の公開状態ではない。PR #4 の workflow 削除と Human の Unpublish は別操作として区別する。
