# 現在の実装・検証状況

accepted implementation は常に actual GitHub `main` HEAD。[PR #4](https://github.com/tomooch/rogue-tactics-lab/pull/4) は merge 済みで、historical Web v0 は retired 済み。main に Web runtime / npm harness / Pages workflow はない。Unity Stage 0はPR #5でmainへmerge済み（G1 baseline `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。

[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) はmerge済み。以下はmerge前の実行対象revisionのhistorical evidence。Stage 0 の実行済み evidence は **pending HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af`** に紐付く。以下はその revision の Local Mac evidence であり、accepted main、実機、人の試遊の成功へ読み替えない。docs status sync は [Design Office comment](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298) と最新 Human 指示により承認済み。

## Unity Stage 0 evidence（2026-10-08 JST）

- branch: `codex/issue-1-unity-stage0`
- baseline main: `daa00c50e0d43a1911b6df6d1f59eee36b797145`
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

要約 evidence は PR #5 本文にも記録する。docs sync は4文書のみの変更であり、この実行済み HEAD から Unity source / assets / packages / settings / tests を変えない。docs sync 後の PR HEAD 自体で新たに Unity tests を実行したとは主張せず、Unity tree の一致を Git diff で確認する。

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

## Gameplay G1 — Issue #14 / 2026-10-13

- baseline: actual `origin/main` **`ab9fa5dc729dcea07f8680e7b22544627c5e5bed`**、PR #5 merge済み。
- branch / checkout: `codex/gameplay-g1-battle` / `/private/tmp/rogue-gameplay-g1`。
- environment: Local Mac Apple Silicon、Unity **6000.3.25f1 (e1dba0a9aba4)**、URP、390×844、graphics有効のCLI。
- Scope: [Issue #14](https://github.com/tomooch/rogue-tactics-lab/issues/14)と[Design Freeze G1](DESIGN.md#design-freeze-g1--2026-10-13--issue-14)。固定1戦闘、匿名A/B/C/D。承認済みcastとは対応しない。
- Pending PR監査（変更・mergeなし）: #6 `7055394`、#8 `7767211`、#10 `7c3f2e0`、#11 `9405e1e`、#12 `47db820`、すべてOPEN/Draft。#13 animationは依存なし。

### 閉ループ中の失敗と修正

1. 初回Core importはテストの`Throws.ArgumentOutOfRangeException`がinstalled NUnitに存在せずcompile失敗。`Throws.TypeOf<ArgumentOutOfRangeException>()`へ修正。受入条件を緩めていない。
2. Core 7/7 PASS後、既存scaffoldの`EditorBuildSettings.scenes.Single()`がG1 scene追加で失敗。Stage0の保持と先頭G1を確認するテストに変更。
3. 初回実PNGは空白。Canvas rootがdefault layer0、UI cameraがlayer5のみを描画していた。rootをUI layerに統一。captureの一時RenderTexture解除で操作座標系も変わっていたため、全操作試験中390×844を維持。画像の色変化と文字pixel検査を追加し、空白を成功にしない。
4. batchmodeで進まない`WaitForEndOfFrame`は初回compile/test段階の静的レビューで見つけ、描画frameを待つ`yield return null`へ変更（この待機が実際にtimeoutしたとは主張しない）。[Unity 6000.3 API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitForEndOfFrame.html)。
5. 敵対レビューで、予想がbaseline resetで消え、条件別に結果と照合できない問題を検出。完了trialの予想・結果・SHAを条件別に保存し、Rules→Trialsで参照できるよう修正。再試行後も前trialが保持される検証を追加。
6. 実画像で役名/HPの重なりを視認。小さい二行labelとより上からのcameraへ修正。終了時の意図した`G1_RESULT`情報logはtestで明示的に期待し、予期しないerrorを無視しない。

元失敗XMLは一時領域 `/private/tmp/rogue-g1-evidence/failed-initial/`、`failed-render/`、`failed-log/`に保持。raw Editor logはlicense diagnosticsを含み得るため公開packetには入れない。

### 証拠の境界

UIのHP・位置はCore eventを順に投影し、stepごとにCore stateとの一致をassert。planのactor/target/destination/reasonはCoreが生成する。敵AIもCoreの固定target選択規則であり、Presentationで偽のダメージや回復を作らない。

テスト入力はraycastのtop hitとpointer handlerを含むagent操作。Humanの自由試遊ではない。Native Editor描画とmacOS previewの結果を実iPhone/Androidへ読み替えない。実機touch/safe area/IME、署名/store/export、Human可読性・面白さ・次の自発的変更は未検証。floor探索、relic、第二戦、correction、E提案はG2以降。
