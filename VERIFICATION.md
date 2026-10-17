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

### Final G1 source / fresh-run evidence

検証済みsource commit: **`2d200ffb7769b1a286f7d0f3b8f60a4c0ecd2ee6`**。このcommitを固定して新しいEditor processでimport → EditMode → PlayMode → Mac preview buildを順次実行。exit0、Unity 6000.3.25f1。証拠追加commitはdocs/evidenceのみでUnity treeを変えない。

| 検査 | 実測 |
| --- | --- |
| import / compile | PASS / exit0 |
| EditMode | **12/12 PASS**（G1 Core 7件＋scaffold 5件） |
| PlayMode | **2/2 PASS**（G1実入力経路＋Stage0描画） |
| 390×844描画 | 7枚、空白検出・文字bounds・生存label重なり・44px以上のbutton・raycast top hitを検査。agentが実画像も確認 |
| Core/UI projection | 全stepのHP/位置がCore eventからだけ更新され、Core stateと一致。全表示eventがCore event列と一致 |
| replay / baseline | 2条件の同一初期snapshot、同条件再実行のbyte/hash一致、停止中のCore不変、予想のtrial別保持 |
| macOS preview build | PASS。既存EditorのStandaloneOSX build、Development / windowed 390×844。iOS/Android buildではない |

[evidence/g1](evidence/g1/)にfresh XML / summary、PNG、完全traceと結果、修正前XMLを保存。raw Editor logはGit外の一時領域 `/private/tmp/rogue-g1-evidence/fresh/`。runner recordは `/private/tmp/rogue-g1-jobs/fresh1/completion.json`。Mac buildはUnityが生成するshader prefilter/runtime settingsとStandalone window設定だけを書き換えたため、build完了後に4つのtask-owned生成差分を検証済みsourceへ戻した。preview binaryはbuild時のwindowed設定を持つ。Unity source treeのevidence追加前後の同一性をGit diffで確認。

再実行（Editor引数）:

```text
-batchmode -quit -projectPath <worktree>/unity -executeMethod RogueTactics.Editor.G1Setup.Configure -logFile <configure.log>
-batchmode -quit -projectPath <worktree>/unity -logFile <import.log>
-batchmode -projectPath <worktree>/unity -runTests -testPlatform EditMode -testResults <edit.xml> -logFile <edit.log>
-batchmode -projectPath <worktree>/unity -runTests -testPlatform PlayMode -testResults <play.xml> -logFile <play.log>
-batchmode -quit -projectPath <worktree>/unity -executeMethod RogueTactics.Editor.G1Setup.BuildMac -logFile <build.log>
```

通常再検証はscene再生成不要。batchmodeでもgraphics有効。PlayMode testは操作中390×844 RenderTextureを維持して実描画し、raycastとpointer handlerでstart/next/play/pause/reset/setup/rules/trialsを操作する。

### Counterfactual results / input and provenance

各trialを `new Replay(Fixture.Baseline(), preference)` から開始し、terminalまで `Advance()` を14回。command sequenceはprotect=`0`を14個、attack=`1`を14個。予想はCore入力・結果に影響せず、Presentationのtrial記録にだけ保持する。

| 条件（B選好のみ変更） | 結末 | actions | 味方HP A/B/C/D | 生存 | heal残数 | SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| ProtectAnchor | Victory | 14 | 16 / 16 / 12 / 10 | 4 | 0 | `e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d` |
| DirectAttack | Victory | 14 | 0 / 16 / 12 / 10 | 3 | 1 | `85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e` |

双方のHP/生存/資源を独立指標として表示。単一scoreや推奨は作らない。実action差は最初のBのProtect対Moveから生じ、次のXの被弾はguard条件で3、direct条件で6。counterは生存Aだけ。完全なevent orderとterminal stateは`protect.trace` / `attack.trace`に残る。今回のfixtureで技術的な対照差は成立したが、普遍的な戦術価値や面白さ成立は未証明。

### Adversarial review / deferred work

読み取り専用の独立レビュー＋root自己レビューで、Core/Presentation境界、single preference fork、合法性と致死順、キャラ役混同、未決UX固定、main/既存PR保護を確認。予想喪失・空白capture・label重なりを修正し、回帰検査を追加。human playは未実施。G1 TECHはこの固定実験の範囲、G1 HUMANとG2は未完了。

### Native previewの実操作観測

source `2d200ffb7769b1a286f7d0f3b8f60a4c0ecd2ee6`からbuildした `/private/tmp/rogue-gameplay-g1/build/G1Battle.app` を起動。Computer Useの実クリック（200ms press）と実keyboard入力で、Attack選択 → 自由予想 `I will compare remaining HP.` → Start → Next actionで被弾6/counter2 → Play to end → Victory/A0/3人生存/回復残1を観測。Baseline → Protect選択 → 自由予想 `I will observe protection.` → Start/Nextでguard軽減後の被弾3/counter2 → Play to end → Victory/A16/4人生存/回復残0を観測。Rules/Trialsで両予想と結果・trace hashが保持されることを実画面で確認し、最後にBaselineへ戻した。画面のgame領域は390×844（window chromeを含むcaptureは390×900）。これはagentによるnative Unity player操作であり、Human試遊や実機touchではない。

native操作の前に、短すぎる自動クリックがframe pollingに届かなかった。press時間を200msにして実入力到達を確認した。通常のpointer経路はraycast PlayMode testでも確認済み。入力や戦闘のルールを変更した結果ではない。

## Mobile Visual G1 / Issue #16 — build 2

専用branch `codex/mobile-visual-g1`、worktree `/private/tmp/rogue-mobile-visual-g1`。最新origin/main取得時のaccepted mainは`ab9fa5dc729dcea07f8680e7b22544627c5e5bed`。未mergeのPR #15 HEAD `aa1fc05b154abcfa0e1a5c90574bf91adc97d881`を明示的baselineにして、[Draft PR #17](https://github.com/tomooch/rogue-tactics-lab/pull/17)をそのbranchへstack。既存PRのbranch、main、Issue #13のanimationは変更しない。

検証済みUnity source **`32cba28058ae8069f065e0ac018a0572a4d91930`**。Core tree `50274c84cb56344d1394b4283833728c72cd7577`はPR #15 baselineと同一。初期状態、行動順、数値、event列、上記2つの完全trace hashは維持する。Aシロ/Bモモ/Cガル/Dミャルは今回の静止skin対応で、fixtureの能力を正式キャラ能力へ採用しない。resolution chunk / intent horizonは未決。

### Fresh run / I0

環境はUnity **6000.3.25f1 (e1dba0a9aba4)** / URP、iOS Build Support、Xcode **27.0 (27A266a)** / iphoneos27.0、Apple Silicon。新Editor processを順次起動し、全command exit0を確認。

| 対象 | 結果 |
| --- | --- |
| import / compile | PASS |
| EditMode | **14/14 PASS**（deterministic Coreを含む） |
| PlayMode | **6/6 PASS** |
| Unity iOS IL2CPP export | PASS。実exportの1024×1024 Marketing icon存在も検査 |
| game署名設定 | mainとUnityFrameworkがgame専用identity、profileはmainだけ |
| Xcode Release / generic iOS archive | exit0 |
| archive codesign / profile | deep/strict PASS、explicit game ID、新証明書1件、get-task-allow false |
| Unity Simulator export / Xcode ARM64 Simulator build | 両方exit0 |
| Internal-only upload | **exit0 / Upload succeeded**。ASC処理完了・本人提供は別gate |

PlayModeは390×844実描画、ノッチ/狭い左右safe area、48px操作面、HP文字の幅と非重複、詳細/比較本文coverage、各条件の予想保持、pause不変、各stepのevent投影とCore state一致、frozen hashを確認。callbackテストだけでなく、実InputSystemUIInputModuleへMouse/Touch stateをqueueする経路も確認する。batch EditorだけIgnoreFocus / AllDeviceInputAlwaysGoesToGameViewを設定しfinallyで復元し、product設定は変えない。keyboard高さ300/420pxで予想欄が上に出て、閉じると元位置へ戻り、Coreを変えない回帰検査を含む。

public XML/summaryは[evidence/mobile-g1](evidence/mobile-g1/)の`32cba28-*`。rawログは一時領域 `/private/tmp/rogue-mobile-evidence/build2-final/`、`upload2/`。runnerは`/private/tmp/rogue-mobile-jobs/build2-final/completion.json`と`upload2/completion.json`。buildによるURP prefilter/runtime settings・iOS backend/batchingの4つの生成差分だけを完了後にsourceへ戻し、検証後Unity treeを変更しない。

再実行のEditor引数（全てexact Editor、graphics有効）:

```text
-batchmode -quit -projectPath <worktree>/unity -logFile <import.log>
-batchmode -projectPath <worktree>/unity -runTests -testPlatform EditMode -testResults <edit.xml> -logFile <edit.log>
-batchmode -projectPath <worktree>/unity -runTests -testPlatform PlayMode -testResults <play.xml> -logFile <play.log>
-batchmode -quit -projectPath <worktree>/unity -buildTarget iOS -executeMethod RogueTactics.Editor.MobileG1Setup.ExportIos -logFile <ios-export.log>
-batchmode -quit -projectPath <worktree>/unity -buildTarget iOS -executeMethod RogueTactics.Editor.MobileG1Setup.ExportIosSimulator -logFile <sim-export.log>
```

device export時だけowner-local環境変数`ROGUE_IOS_SIGNING_SHA`と`ROGUE_IOS_PROFILE`でgame専用署名を指定。Xcodeは`-scheme Unity-iPhone -configuration Release -sdk iphoneos -destination generic/platform=iOS archive`。uploadは`xcodebuild -exportArchive`、manual signing、`method=app-store-connect`、`destination=upload`、`manageAppVersionAndBuildNumber=false`、**`testFlightInternalTestingOnly=true`**。署名鍵/profile/IPA/raw credential logsはpublic Git外。

### Actual Simulator interaction / V0・V1技術観測

source32cba28、version **0.1.0 / build 2**、Bundle ID `com.tomooch.roguetacticslab`。専用iPhone17 / iOS27.0 Simulator **Rogue Tactics G1 QA**へXcodeBuildMCPでinstall/launch。semantic snapshotはremote automation timeoutのため、Device HubのCUA native pointerと日本語software keyboardで実操作した。

1. 日本語「かつ」を入力中、keyboard上の入力面で文字が見えること、確定後に元の予想欄へ戻ることを実画面で確認。
2. Protect開始→次行動で守護3 / Damage3 / Counter2→連続再生→Victory14 / A16 / 生存4 / 回復残0。
3. 戻すで全初期HP/位置へ復元→Attackに変更→BがMove→連続再生→Victory14 / A0 / 生存3 / 回復残1。
4. 比較画面で両結果・hash prefix・Protectの予想を保持。完全hashは同source PlayModeでも一致。

private動画 `/private/tmp/rogue-mobile-evidence/build2-keyboard.mp4`、比較画像`build2-comparison.jpg`、keyboard開閉画像`build2-keyboard-{visible,restored}.png`はowner-local一時領域。publicには観測JSONだけを保存。独立reviewは最新keyboard開閉2画像でBLOCKER/MAJORなし。先行source7d3ec53の4実画像reviewもBLOCKER/MAJORなし、中央label/線の密度はMINOR。これはagentによるSimulator操作であり、実iPhoneやHuman試遊ではない。

### 修正と再検証の記録

- 初期実描画でHP重なり、詳細Mask alpha0による本文消失を検出。safe幅/label配置、Mask alpha1＋showMaskGraphic false、本文coverage検査を追加して再検証。
- event順序から早くpawnを消す箇所と移動後のguard線を修正。Death event時にだけ消し、Coreと表示の一致を検査。
- callback直呼びだけではnative入力の証明にならなかった。Mouse/Touch queue回帰を追加。device1/device3はbatch Editorの非focus routingで失敗し、test-only routing修正後device4はPlay5/5 PASS。
- source cdecba9の署名archiveはSDK別`iPhone Developer`優先によりexit65。game専用identityを`CODE_SIGN_IDENTITY[sdk=iphoneos*]`にも設定し、7d3ec53で署名archive/export成功。既存certificateを変更しない。
- build1のInternal uploadは必須1024px app icon不足でexit70。opaque RGB24のテスト用盤面emblemをUnityで生成し、全iOS icon slotsを設定。正式原画やbrandingの採用ではない。build2の実export gateとApple upload成功で修正を確認。
- Simulator実操作でkeyboardが予想欄を隠すことを発見。表示位置を補正し、型名とRect helperの衝突CS0119も明示型で修正。prepare2 Edit14/Play6の後、固定source32cba28で全fresh runと実keyboard操作を再実施。

修正前XML、旧sourceのsummaryも同evidence directoryに対象revisionを付けて保持する。旧PASSを最新sourceの証明として使わない。Mac previewのMobile sceneクリックは未確認だったため、現在の操作成功は上記Simulator実測だけに基づく。

### Distribution / 残るgate

Human承認の範囲でgame専用Bundle ID、Distribution certificate、explicit App Store profile、ASC **Rogue Tactics Lab / Apple ID6822532815**を作成。ASC「アクセス制限なし」は自動reviewで拒否後、2026-10-14の明示Human承認を受けて設定。内部グループ **本人限定 G1**は自動配信OFF、本人Account Holder1名のみ、作成時はビルド0件を確認。新game資産だけを用い、SizeSnapと既存credential資産を変更しない。

**I0 PASS / I1のASC配布設定PASS**。2026-10-14、Human再ログイン後にbuild2（0.1.0/2）の処理完了を確認。実装はローカルreplayのSHA-256照合のみで通信/暗号化機能を追加しておらず、Appleの[暗号化回答手順](https://developer.apple.com/help/app-store-connect/manage-app-information/overview-of-export-compliance/)と実sourceを確認して「上記のアルゴリズムのどれでもない」を保存。buildは「テスト準備完了」になり、「本人限定 G1」へ追加後「テスト中」、build詳細の内部グループ1件/テスター1名を確認。グループ画面でも内部テスター1名・ビルド1件、本人「招待済み（2026年10月14日）」とbuild0.1.0(2)「テスト中」を確認。日本語の操作説明も保存。owner-local証拠`/private/tmp/rogue-mobile-evidence/asc-build2-testing.jpg`と`asc-build2-self-invited.jpg`（一時領域）。本人の端末での招待受領・install/起動成功は別の実測として未確認。I2はphysical iPhoneのtunnel unavailableのため起動/touch/IME未検証、HUMAN見た目・理解・面白さ・次の自発的変更は未評価。private原画を持たないpublic checkoutのV0は未設定。一般公開、外部tester、公開リンク、App Store審査/リリース、main merge、Draft-to-Readyなし。CI workflow/checksは未設定でCI PASSを主張しない。

#### 招待到達の再確認

2026-10-14、Humanから「TestFlightに来ていない、SizeSnapしかない」と報告。ASCの本人1名/ビルド1件と「招待済み」を再確認し、同じ本人だけを選択して再招待→再送信、処理dialog終了を観測。受信箱への配達・招待承諾・端末表示は未確認。したがってI1のASC設定PASSとend-to-end到達は分け、後者は未達のまま。新規招待はメールからTestFlightで承諾する[Apple公式手順](https://developer.apple.com/help/app-store-connect/test-a-beta-version/add-internal-testers/)をhandoffへ明示し、端末表示確認前にインストール可能と断定しない。SizeSnapや別アカウント/外部testerは変更しない。再送後のowner-local画像は`/private/tmp/rogue-mobile-evidence/asc-build2-reinvited.jpg`（一時領域）。

### Human試遊feedbackと視認性修正（2026-10-14、build3技術検証済み）

Humanは招待メールを確認しiPhoneで開くと回答、その後「できた」と報告。さらに実際の見た目として、キャラが見づらい・キャラからの線が見づらい・斜め視点が見づらい・詰まって見える・キャラが大きすぎるかもしれない・アニメーションなしでは面白くなさそう、と報告。追加で「頭にバーを被せることが認識阻害。下もしくは分離」と指摘。V0/HUMANの視認性・期待感は未合格。端末model/iOS/実機capture/全操作QAは未取得であり、招待成功や感想から未観測の全I1をPASSにしない。

旧build2のUnity実画像と独立reviewで、orthographic cameraのyawと浅い俯角による駒群の重なり、spriteに対するセル間隔不足、キャラ上を横切る細い線、頭上HPの問題を確認。Core座標・trace・初期条件を変えず、Presentationだけのセル倍率/俯角/余白、足元HP、縁取り方向矢印を修正中。頭上HPは採用せず、各シルエットの下かつ隣キャラとも重ならないことを回帰検査する。animationへの反応はレイアウトで解決したと扱わず、別Issue #13の未完課題として保持し、今回の静止キャラ契約を黙って拡大しない。

job readability1でcompile/PlayMode/390×844実描画を再検証。修正後のHuman GOはまだない。旧画像はowner-local `/private/tmp/rogue-mobile-evidence/readability/build2-before-*.png`（一時領域）、private原画入り画像はpublic Git/Notionへ含めない。

readability1はcompile PASS、PlayMode5/6。追加回帰が「HP X must not cover A」を検出。新画角でも中心pivotの球敵の前縁がcellより下へ伸び、足元HPが次行の味方へ入っていた。敵の表示本体/眼/影だけをcontact edgeがcellに揃うよう補正し、Core root位置やstateは維持。readability2で再描画・再検証中。失敗XMLは`evidence/mobile-g1/readability1-failed-play.xml`。

readability2もcompile PASS、Play5/6。初期/守護/被弾の実画像で足元HP・人物非重複は確認したが、新矢印が描かれていないため未合格。testの事前LogAssert.Expectが後続assertの原因を「terminal log不在」で覆ったため、terminal/hash/event比較のassertは維持し不要な事前log期待だけ除去し、actual Canvas mesh/色/cull/pathを診断するreadability3を開始。画面は今回のowner-local private実Unity renderで、Humanの修正後GOではない。

readability3の診断追加でUnity6のCanvasRenderer.GetMesh APIを旧1引数形式と誤記しcompile FAIL。実EditorのUIModule XMLを確認した0引数GetMeshへ修正し、readability4で診断を再実行。製品のCore/数値には変更なし。

readability4はcompile PASS、Play5/6。actual Canvas mesh取得でMissingComponentExceptionとなり、新custom MaskableGraphicにCanvasRendererが無いことを特定。経路値があっても描画不能だった。RequireComponent(CanvasRenderer)を追加し、readability5でactual mesh頂点/cullに加えて画面上の経路から色付きpixelをsampleする回帰を追加。HP/人物の非重複と両trace gateは維持。

readability5はcompile PASS、Play5/6。矢印actual mesh/cull/pixel検査と実画像確認は成功。戦闘途中の「Core intention avoids B」で、Move後もpreview用の古い矢印が残り、移動先Bに被ることを検出。Core Move/Death eventでaction arrow/ghostを消す表示修正を追加し、readability6で全frameの人物回避＋足元HP回避を再検証。Coreのevent生成や時刻は変えない。

readability6はcompile PASS、全戦闘frameのsilhouette/足元HP/矢印回避、両terminal/hash/event比較まで通過。最後のNoUnexpectedReceivedが、診断時に期待登録を外した正常MOBILE_G1_RESULTログを未処理として検出しsuiteは5/6。元のLogAssert.Expectを復元しbuild3設定とともにreadability7でEdit14/Play6を再実行。独立reviewは最新初期/守護画像の範囲でBLOCKER/MAJORなし。MINORは矢印終点/上部event receiptの標的との距離、画角変更で盤面が平面grid寄りに見える点。Humanの再評価は必要で、animation不足は未解決。

readability7はUnity6000.3.25f1でConfigure/import PASS、EditMode14/14、PlayMode6/6 PASS。全戦闘frameの人物間/足元HP/矢印非交差、actual arrow mesh/色pixel、両trace hashとCore event投影、狭いsafe幅、keyboard/Mouse/Touchを確認。実画像で頭上HP被りを解消。Editor再生成はsceneのobject IDだけ変えていたためsceneを元へ戻し、auto生成のiOS rendering flagだけも復元。buildNumber3はEditorが生成した差分を保持。sourceを固定してfresh import/tests・署名archive/Simulator再buildへ進む。

ASCをfresh reloadし、本人の状態が**インストール済み0.1.0(2)**、device列が**iPhone17 / iOS27.0**になったことを観測。招待メール到達はHuman回答、承諾/インストールはASC実測とHuman「できた」報告、起動後の見た目はHuman feedbackとして記録する。専用Simulatorとは別のASC TestFlight実機配布証拠であり、全touch/IME/safe areaの独立QAや修正build3成功へ転用しない。private screenshot `/private/tmp/rogue-mobile-evidence/asc-build2-installed.jpg`（一時領域、public Git外）。

固定source d19a135のbuild3-finalは全工程完了：fresh import、Edit14/14、Play6/6、iOS export、署名archive、Simulator export/build PASS。upload3もgame専用codesign/profile/Internal-only検査後exit0。両runner completion/artifactを読んで確認済み。build時に生成されたrendering/cacheの4設定差分だけを復元し、Unity sourceはd19a135と同一。

専用SimulatorのCFBundleVersion3を確認してinstall/launch後、CUA native pointerでProtectの開始→次行動（Guard3/Damage3/Counter2）→連続再生（Victory14、A16/B16/C12/D10、生存4、回復残0）→戻す→Attack開始（B Move）→連続再生（Victory14、A0/B16/C12/D10、生存3、回復残1）→両結果/hash比較まで実画面で観測。頭にHPを重ねず足元へ置き、方向矢印が人物を迂回して描画されることを確認。build3 native試行の予想は空欄、日本語native keyboardの実操作はbuild2で観測した範囲。最新public観測JSONは`evidence/mobile-g1/d19a135-native-observation.json`。owner-local動画`/private/tmp/rogue-mobile-evidence/build3-readability.mp4`、画像`build3-native-guard.png`/`build3-comparison.jpg`（一時領域）を保存。build3のHuman/実機評価とアニメーション不足は未解決。

2026-10-14、Apple処理後のbuild0.1.0(3)にbuild2と同じ暗号化回答を保存して「テスト準備完了」を確認。本人限定G1へ追加し「テスト中」、build詳細の内部グループ1件/テスター1名、日本語の変更点と操作説明の保存・reload後保持を確認。iOS一覧でもbuild3「テスト中」/本人限定G1を実画面で再確認。既存build2を期限切れにせず、外部グループ/公開リンクを追加しない。private証拠は`/private/tmp/rogue-mobile-evidence/asc-build3-testing.jpg`と`asc-build3-self-notes.jpg`（一時領域）。public記録は`evidence/mobile-g1/d19a135-asc-distribution.json`。build3の本人端末install/起動・Human再評価は未確認。Notion Todo/Tried & Learned同期成功。

## HQ-REF-1 実行中 — Issue #20

基点PR #17 `ce4ffac8d2216973c391d801cf5f6d41fbde66b5`の独立control worktreeでUnity6000.3.25f1のMobileG1入力回帰4/4と390×844実captureを再取得。R1候補はEdit14/14、通常player flow/両frozen hash/全event投影/途中pauseのcursor固定/Mouse/Touch等6/7を確認し、残ったdeveloper Resetラベルの折返しを修正後、その戦闘・safe-area回帰1/1 PASS。これは最終統合sourceの全回帰を代替しない。

R2初回の実Unity画像で75°/63°を固定yawで比較。Core状態不変、camera/product tests2/2 PASS。しかし規則的な舗装が広すぎ、原画の浮遊遺跡・花・滝・立体感が読めずV0 FAIL。独立画像reviewも同じ主因と小さなcast/portraitを指摘。テスト成功をHQ成功とは扱わず、camera framing、実3D奥行き、舗装形状、foot HPとcast scaleを修正中。63°の奥行きの差を観測したがHuman preferenceは未取得。

Private実画像はcontrolの `unity/Logs/mobile-g1/private/`、R1記録 `/private/tmp/rogue-hq-evidence/r1-captures/`、R2初回 `/private/tmp/rogue-hq-evidence/r2-initial/`（owner-local一時領域）。公開Gitへ画像を入れない。最終source-linkedの画像hash・XML・段階別ledgerは完了後追記する。現在のTestFlightは既存build3までをfresh ASCで確認。HQ新buildの配布・実機評価は未実施。

R3の途中停止regressionは実際に駒が移動し始めてからpauseを押し、押下瞬間・停止中のpose/cursor不変と再開時Moveが1件だけになることを確認。source修正後のPlay9件中8件が通過し、残ったfield内HP B/D重複は自動checkが検出した。labelをownerの下へ保ちつつ近傍で横避けする処理に変更して再描画中。HP/生存/2本のtraceは変更しない。

フレーム間隔のowner-local計測は取得可能。先行R3のEditor/Metal・captureを含むsample1329でp50 16.692ms/p95 16.740ms等を記録。ただし最終sourceの値ではなく、Simulator・iPhone実機値でもない。物理device探索は0件で、実機QAは未実行。値をiPhoneの30fps/thermal/memory PASSへ読み替えない。

### HQ candidate の品質判定と残るgate

接地影/SSAO/skyと浮島構図のpivot後も、actual 390×844 renderは舗装の反復、植生・石材の簡素さ、滝/島側面の弱さ、2D人物と3D照明の差が残る。**V0 FAIL、R2品質BLOCKED**。同型の色/カメラ微調整を続けず、ローカルの造形asset passが必要と判断。実行可能な比較候補は保持するが、HQ完了やHuman GOとは報告しない。足元HPの非重複回避はX/Bなどを横へ離す場合があり、所有者との関係の読みやすさも残課題。

User指定パスの941×1672画像は実測SHA `99a76f33c60e74d7fd890e9cf188531280e030e58a5893bafd90899ea2b965b8`。Issue PRIMARY `9b4ee65b5fe3dfeaa0938cb09aa928eb67bf99cfcb06b9ea1423208fe6aa6621`とは異なる。同一bytesと主張せず、supplied local reference比較とPRIMARY照合未完を区別する。

R0はasset/provenance/control監査済みだがPRIMARY照合未完でPARTIAL。R1/R3の最終統合判定は固定sourceの全回帰結果へ紐付ける。R4はnative build/Simulator/ASCと実機を分けて記録する。R5はPENDING HUMAN、G2はNOT STARTED。真のsprite gaitは#13のまま。Notion正本の取得/更新能力がこのexecutorで得られず、今回の同期は未実施。Issue/PRの実行記録をNotion正本に昇格させない。

最終統合candidate-finalはConfigure PASS、Edit14/14、Play8/9。守護hop後に最終poseへ戻した際、guard lineが旧投影のままでB輪郭へかかる失敗を検出。RefreshActors後にも守護線を再投影する修正を追加し、全Play9件で再検証する。失敗をテスト除去や回避assertで処理しない。

修正後yaw-finalはConfigure/import PASS、Edit14/14、Play9/9 PASS。guard lineの最終投影と障害物迂回、yaw0°での全戦闘frame輪郭/HP/線回避、両hash/event投影、移動途中pause保持を確認。これをcommitで固定し、fresh runとnative再buildへ進む。品質V0 FAILとHuman未評価は維持。

固定source c51aab1のnative4はfresh import、Edit14/14、Play9/9、iOS export/署名archive、Simulator export/buildすべてPASS。build0.1.0(4)の実install/launchを確認。しかしnative日本語keyboardを開くと予想欄が隠れ、on-screen kana入力は閉じた後だけ確認できた。旧mock-areaテストだけでは検出できなかった不具合。uploadを保留し、入力focus中はtitle直下へdockする修正をd782437にcommit、通常/developer両layout・reported area0の回帰を追加。新sourceで全検証・archive/Simulatorを再実行中。c51aab1の成功を新sourceへ転用しない。

### HQ final native evidence — source d782437

Source `d7824378755d52cffbdb6add558bef0f50baefe8`。Unity6000.3.25f1 / Xcode27.0(27A266a) / SDK27。fresh import、Edit14/14、Play9/9、iOS export/ゲーム専用署名archive、Simulator export/build PASS。ビルド生成のcache/rendering差分のみ復元し、保持するUnity sourceは固定commitと同一。公開結果は `evidence/hq-ref/{native-build.json,edit.xml,play.xml}`。

専用iPhone17/iOS27 Simulatorへ0.1.0(4)をinstall/launchし、CUA native pointerで日本語kana「あ」の入力・確定・欄の復帰、Protect開始→途中pause→表示保持→再開→勝利14行動、生存4/HP A16 B16 C12 D10/回復残0、reset→Attack開始→勝利14行動、生存3/HP A0 B16 C12 D10/回復残1、両結果と保存予想・hash比較を実画面で確認。実ログでも両full SHAが固定値と一致。`native-observation.json`は操作/結果/限界を記録。

Simulator native Metalのframe intervalはProtect sample1328 p50 16.666ms/p95 17.268ms、Attack sample1281 p50 16.666ms/p95 16.697ms。録画/host負荷を含む。実iPhoneのfps/thermal/memoryは未観測。Editorの最終source frame intervalは別JSONとして保持し、混同しない。

Private証拠（一時領域、public Git外）：`/private/tmp/rogue-hq-evidence/build4-final-native.mp4`、30秒抜粋`build4-action-clip.mp4`、`build4-paused.jpg`、`build4-comparison.jpg`。比較用`private-comparison.html`はHuman指定画像と実Unity guard画像を並べる。Unity 390×844イベント画像のSHA/所在は`capture-manifest.json`。原画/skin本体・動画・署名資産・rawログは公開Gitへ含めない。所有者はローカルファイルを直接開いて確認できる。

| Stage | 判定 | 範囲と未達 |
| --- | --- | --- |
| R0 | BLOCKED（監査は実施） | control/provenance/許可境界を確認。PRIMARY bytesは指定ローカル画像と不一致、Notion同期不可。 |
| R1 | PASS（技術検証範囲） | 通常導線、actor order、safe area/44pt、入力/停止/比較を自動/nativeで確認。Human UX評価は未実施。 |
| R2 | BLOCKED / V0 FAIL | 実3D環境を実装・複数比較/修正したが原画の造形・密度・浮島感へ未達。ローカル造形asset passが必要。 |
| R3 | FAIL（視認品質） | event順/HP/死亡/両hashとmotion/pause整合はPASS。長い迂回矢印が移動経路に見えやすく、actor/targetの即読性は未達。true gait #13は別。 |
| R4 | BLOCKED（実機） | archive/Simulator/IME/両戦闘はPASS。本人限定build4 upload実行中。I2実iPhone・thermal/memoryはPENDING HUMAN。 |
| R5 | BLOCKED / PENDING HUMAN | HUMAN_VISUAL_GOとHUMAN_FUN_HYPOTHESISは未評価。 |
| G2 | NOT STARTED | HQ gateの先に進めていない。 |

仮説H-HQ1の期待は魅力・因果理解・視聴継続意欲。観測は上記agent実操作と独立reviewに限定され、Humanの理解/面白さを証明しない。判断：比較可能な実行候補をDraft PR #21として保存し、V0 FAILを保持。同型の平面scene微調整は止める。次の有効な変更はローカル造形assetと長い意図線の視認性を対象にした別の可逆pass。既存Core・原画RGB・承認境界は維持する。

2026-10-15 16:05 JST、source d782437の0.1.0(4)はゲーム専用archive/codesign/profile/internal-only検査後にupload exit0。ASCをfresh表示し、同gameのbuild4「処理中」を観測。旧build3/2の本人限定G1配布を維持。新build4のグループ追加/本人の端末installは、この時点では未確認。public `internal-upload.json` とprivate `/private/tmp/rogue-hq-evidence/asc-build4-processing.png` を保持。

最終独立reviewはd782437のGuard/Counter/Heal/Death実画像とnative停止/比較画像を確認。長い折れ線による非移動effectの誤読、上部表示の小ささ/低コントラスト、足元ラベルの密度、比較modal下部にportraitが重なる階層競合を残課題として認定。比較文・両結果は読め、event/hash/HPの新たなデータ不整合は観測なし。これらを品質PASSに読み替えず、現candidateをDraftに保持。

### build4 本人限定TestFlight配布の実測

2026-10-15、Apple処理後の0.1.0(4)へ、以前の同gameと同じ暗号化区分（上記アルゴリズムのどれでもない）を保存。今回はPresentation追加のみで通信/暗号化実装・依存を追加していない。ASCの「テスト準備完了」を確認後、既存「本人限定 G1」だけへ追加して「テスト中」を観測。build詳細はグループ1件・内部テスター1名。source d782437/PR21、通常操作、原画との差・長い線・比較画面の重なり・実機未検証を明記した日本語テスト説明を保存し、reload後の保持を確認。新app/外部tester/公開リンクなし、旧build2/3を期限切れにしていない。

**R4のarchive/Simulator/ASC本人限定配布はPASS、I2実機はPENDING HUMAN**。本人端末でのbuild4 install/起動・実機性能・見た目/面白さは未確認。R2 V0 FAIL・R3視認品質FAIL・R5未評価を配布成功で上書きしない。private証拠 `/private/tmp/rogue-hq-evidence/asc-build4-self-notes.png` と `asc-build4-testing.png`（owner-local一時領域）。Notion同期は未実施。CI checksなし。PR21はDraftのまま、main/#15/#17 HEADを維持。

## HQ Pass2 開始 — packet 6192468412

PR21の既存HEAD72552b6・同branchで再開。Pass Aはruntime反復舗装生成を廃止し、オフラインauthor scriptと10 layer別OBJ（不規則な破断石面、周縁層、前面滝、遠景等）をimportする方式へ変更中。中央のCore floorは描画meshを持たないため、actor下の非障害物surfaceもassetで用意する。初回はbuild4と同じ55°/yaw0°・Core位置・HUDで比較。OBJの生成成功を美術品質の成功とは扱わない。

Build4録画をffprobeで再確認したところ、設定値30fpsに対して実ファイルは1052 frames/156.743s、平均約6.71fpsだった。前回のSimulator frame interval計測とは異なる値であり、録画の設定値を実capture cadenceと誤解しない。新passの10–20秒active clipはvendor simctl recordVideoで取得し、実frame数/時間/平均cadenceも記録する。

Pass B初回はConfigure PASS、Edit14/14・Play9/9 PASS。既存の全戦闘frame/HP/route/pause/hash/event投影gateを維持し、非Move時のpath不在、実局所Graphic mesh、比較シートがrosterより前に描かれる回帰を追加した。実画像と独立reviewで守護/反撃/回復/死亡の名前receipt・HP結果は整合。旧floating labelを名前にした副作用で巨大文字がfield左上へ出ることを発見し、EventMotion中は局所frame/HP/receiptへ整理。order header暗色背景、sheet不透明化とともに再検証する。

Pass2最終B: Configure PASS、EditMode14/14・PlayMode9/9 PASS（pass2-edit.xml / pass2-play.xml）。巨大floating非表示・暗色order header・不透明modalを再描画し、独立画像reviewでも旧指摘の解消と新規重大欠陥なしを確認。局所actor/target表示と名前receiptは実eventに整合。R2 V0 FAILは継続。capture SHAとowner-local一時パスはpass2-capture-manifest.json。build5向けsourceをこの記録とともにfreezeし、native検証は後続記録へ分離する。

Native build5 source f305135: fresh Edit14/14・Play9/9、署名archive・Simulator build/install/launch PASS。日本語kana入力確定、Protect pause/姿勢保持/resume/14行動/HP A16 B16 C12 D10/予想保存/full SHAを実測。ただしnativeでreset後にCompareが予想欄/開始へ重なる未検出regressionを発見。配布前に修正し、Compareを終了画面の結果導線へ限定、reset後非表示の回帰assertionを既存全trace testへ追加。f305135は最終配布sourceにしない。

Native vendor録画(f305135、再試行修正前): 19.483333秒/1169 frames、ffprobe実平均60fps。Pauseを含めず開始直後からの戦闘segmentを保存し、8秒/15秒の実frameも確認。旧7fps captureとの差を実測。これはSimulator録画cadenceであり実機fpsではない。最終修正sourceで再capture予定。private動画 `/private/tmp/rogue-hq-pass2/build5-protect-active.mp4`、公開metadata pass2-capture-cadence.json。

修正source d5e8e08の最初のnative再検証はpreflightで停止。前exportによるInternalIcon importer/URP生成設定/iOS PlayerSettings差分を検出したため、テストは未実行。直前のtask生成差分5件を監査してHEADへ復元後、別jobで再実行。旧Simulator bundleを誤って再installしたが、修正後native証拠には使わない。

### Pass2 最終source d5e8e08 — build5 local verification

Unity6000.3.25f1 / Xcode27.0(27A266a) / iOS SDK27。fresh import/compile PASS、Edit14/14・Play9/9 PASS、ゲーム専用手動署名archive/codesign設定・1024icon・Simulator build/install/launch PASS。公開証拠pass2-*.xml / pass2-native-build.jsonはこのsourceへ更新。exportが更新する5件のUnity生成設定は監査後にHEADへ戻し、Coreやpackage差分へ混ぜない。Unity生成asset/metaの空欄末尾spaceは実Editor由来として保持し、手書きsourceのdiff-checkはPASS。

専用iPhone17/iOS27 SimulatorでCUA実操作。Protect勝利14行動/4人生存/HP A16 B16 C12 D10/回復残0、reset後Attack勝利14行動/3人生存/HP A0 B16 C12 D10/回復残1、両full SHA固定一致。Protect後・Attack後ともresetで二択/予想欄/Startを確認し、Compareの遮蔽は解消。再試行後の日本語kana「あ」確定・通常位置復帰・比較への保存、不透明modal本文/操作の非遮蔽を実測。独立reviewの「DirectAttack-resetで入力が見えない」候補は、同じPNGのRoot再読で欄/Start描画を確認し、nativeでもAttack後resetを追加観察して再現しなかった。sheet外下端に終了画面のCompareが見える装飾上の残りはあるが、本文/操作を遮らない。

native frame intervalはProtect sample1329 p50 16.6666ms/p95 16.6919ms、Attack sample1281 p50 16.6667ms/p95 16.6838ms。host/録画負荷を含むSimulator観測で実iPhone性能ではない。CoreDeviceService初期化失敗により実機接続状態を今回は取得できず、実機/thermal/memoryは未検証。build5は未upload。ASC最新reloadは認証切れで、本人の再ログイン回答待ち。既存本人限定build4の状態を新build5成功へ転用しない。Notion未同期。

R0 PRIMARY/Notion BLOCKED、R1 technical PASS、R2 V0 FAIL/BLOCKED、R3 event/hash/局所表示/停止・比較の技術gate PASS（Human即読性評価は未実施）、R4 archive/Simulator PASS・新TestFlight認証待ち・I2未検証、R5 Human visual/fun PENDING、G2 NOT STARTED。高品質完成や原画一致を主張しない。

最終source最初のProtect録画は19.67秒/1182framesだが、5秒frameで既に終盤Death、16秒frameで終了画面。10–20秒active証拠として不採用。録画先行→native pointer Startへ手順を修正して再取得する。録画ファイル成立だけで契約達成としない。

Protect再取得の4–19秒を採用: 実14.9983秒/900frames（約60fps）。先頭Guardから後半C攻撃までactive、長いpause/終了画面を含まない15秒clipを確認。private `/private/tmp/rogue-hq-pass2/build5-protect-15s.mp4`。最終source d5e8e08の署名/profile/bundle/build/internal-onlyを再検査し、既存資格情報による本人限定uploadを開始。Appleブラウザ再ログインは回答待ちで、processing/グループ配布は未確認。

最終native動画確定: Protect14.9983秒/900frames、Attack12.9963秒/781frames（各約60fps、詳細ffprobeはpass2-capture-cadence.json）。Attackはsource6–19秒でX攻撃から後半A攻撃まで、setup/pause/終了画面を除外。実active segmentを各10–20秒確保。owner-local `build5-protect-15s.mp4` / `build5-attack-13s.mp4` をprivate比較HTMLへ追加。capture cadenceとframe interval/実機性能を混同しない。

### build5 upload result

2026-10-15 23:57 JST、final runtime source d5e8e08の0.1.0(5)を既存ゲーム専用profile/署名・internal-only・管理version変更なしで再検査し、upload exit0 / EXPORT SUCCEEDED。Apple transportは「Uploaded package is processing」を返した。ASC画面は同アプリへ再接続してもauthResult=FAILEDで、処理完了・暗号化区分・既存「本人限定 G1」1 testerへの追加は未確認・未変更。本人Chrome再ログイン待ちでここだけ停止。旧build2/3/4をexpireせず、public link・外部tester・別app・新credentialなし。公開証拠pass2-internal-upload.json。署名archive/原画/動画/rawログはprivateのまま。

今回の変更・修正・検証証拠は同PR21 branchへpushし、Draftを保持。R2 V0 FAILとPRIMARY fidelity未確認・Human visual/fun/実機未検証・Notion未同期を維持。CI checksなし。最終残作業はApple再ログイン後のbuild5処理/本人限定配布確認、および次の環境美術制作入力の具体化であり、高品質完成とは扱わない。

### 2026-10-16 PRIMARY RGB同一性の更新

Issue #20 [最新照合](https://github.com/tomooch/rogue-tactics-lab/issues/20#issuecomment-6203653575)により、PRIMARY 9b4ee65b… とMac指定PNG 99a76f33… はPNG bytesは異なるが941×1672の復号RGB全1,573,352画素が一致。LocalでもRGB SHA `fd52b3654adc91c3e086b21cc83a4d9cb1d2057a5cd527b0fbc5cbb3a15a3ce4` を再現した。以前のPRIMARY fidelity未照合という記載のうち、比較原画の同一性ブロッカーは解消。実描画の原画fidelity・R2 V0 FAIL・Human未評価・Notion未同期は別であり維持する。

### Pass3 representative corner closed loop (2026-10-16)

基準PR21 HEAD `cc3f81aa383d1bea931304b43fcbe22a7346ed4b`。private比較HTMLの実参照、26PNG、Protect15秒/Attack13秒のSHAとsourceを監査し、原画/build4/build5を同じ390px幅で直接比較。QuickTimeの通常再生と経過時刻/姿勢/HP/receiptの変化を観測。全frameを個別検査したとは主張しない。前縁の造形・材質・植物・滝を一角で試作→実Unity描画→独立敵対レビュー→同じ角の横帯/材質を修正→旧滝との重複除去。A/Bは各Configure PASS/filtered Play1/1 PASS（Edit0、全回帰とは別）。全回帰/最終実画像は後続の固定source証拠へ記録する。DoF Off単独診断は改善を示さずrevert。原画RGBは変更なし。

ASC認証は今回利用可能。build5内部は「コンプライアンスがありません」、グループ未設定と実画面で確認した。build4は本人限定 G1でテスト中。build5 upload成功と配布完了を区別する。今回は状態のreadのみでApple回答/グループ/資格情報は未変更。ローカル証拠・具体的比較は `evidence/hq-ref/pass3-review.md`。メディアはowner-local一時領域のみ。WebGPTからの直接参照可能な安全な共有経路は未確認であり、共有成功とは報告しない。

Pass3 final: Unity6000.3.25f1 Configure/import PASS、Edit14/14、Play9/9 PASS。既存の両固定hash、event/visible cursor、移動中pause、予想・結果比較・再試行、camera/足元HP/intent投影に加え、描画設定往復で遠景を含む元のshadow modeへ戻る回帰を確認。HQ world postprocess回避後のsetup/guard/heal実画像で人物の色・輪郭改善とcornerの層を確認。公開テストXML `pass3-editmode.xml` / `pass3-playmode.xml`、画像のSHAとowner-local所在 `pass3-capture-manifest.json`。Core tree `50274c84cb56344d1394b4283833728c72cd7577`・package・private4skin SHAは基準と一致。R2全体V0 FAIL、Human/I2/Notion未評価・未同期は維持。Simulatorはこの固定sourceで後続実行する。

Pass3 runtime commitは `9e5fd06eda2c1fe11cf883335c316b988a3f1845`。最終テストの全sourceファイルSHAとconfigured scene SHAがcommit内容と一致（`pass3-source-verification.json`）。公開メディアは追加せず、同branchへ通常push、Draft維持。[実行パケット](https://github.com/tomooch/rogue-tactics-lab/pull/21#issuecomment-6205066312)。

Notion接続が今回利用可能となり、既存[Todo](https://app.notion.com/p/3e3ad38889158105a09df12a35b633a7)と[Tried & Learned](https://app.notion.com/p/3e3ad388891581aba10ddf3e65a70047)へPass3実測・棄却したDoF仮説・局所改善/V0未達・ASC build5状態を同期しreadback確認した。以前のNotion未同期記録は当時の状態として保持。新しいHuman製品DecisionやHuman GOは作成していない。private画像/動画のNotionアップロードなし。

### Pass3 current-source Simulator result

runtime `9e5fd06eda2c1fe11cf883335c316b988a3f1845` のUnity export / Xcode Simulator build exit0、専用QA iOS27.0へinstall/launch PASS。実画面で新corner・人物/空の鮮明化を確認。ProtectはA13/X5/Y12の途中停止を保持→再開→14行動/生存4/HP16・16・12・10/回復残0。Attackは14行動/生存3/HP0・16・12・10/回復残1。比較modal/再試行/初期配置/IME確定文字の保存を実観測し、この起動sessionのnativeログで両full SHA一致。IMEテストでは入力操作から確定したliteral `aattackcheck` の保持のみを確認、指定英語の完全再現や日本語IME全ケース合格とは主張しない。

新DeviceHub録画はProtect55.333333秒/3322frames（意図的pauseを含む）、Attack30.956667秒/1873frames、1206×2622。Live観測とQuickTime通常再生で時間経過・姿勢/HP/receiptを確認。詳細・SHA・frame intervalsは `pass3-native-observation.json`。一時所在 `/private/tmp/rogue-hq-pass3/simulator/`。旧pass2動画とsourceを混同せず、今回のローカル開発binary番号5をAppleへupload済のd5e8e08/build5と区別する。新upload/配布は行っていない。ASTC decompression2件とURP depth fallback警告を記録、crash/exception/描画破損は観測せず。実iPhone性能・Human合格は未検証。export生成5設定は監査/差分保存後にruntime commitへ復元、Core/package/skin不変。追加commitは文書/検証メタデータのみ。

NotionはPass3の初回Todo/Learned同期とreadback済み。native最終結果はTodo更新のtool成功を確認したが、Tried & Learnedへの最終追記は自動承認レビューの利用上限により**未実行**（unsafe判定ではなくreview実行不可）。その追記は未同期として残す。許可回避・別経路での再送は行っていない。

## HQ-REF-1 Pass4 / fc005d4 (2026-10-17 Local Mac)

Unity6000.3.25f1 Configure PASS, EditMode14/14 and PlayMode9/9 PASS, actual390x844 product/camera captures. `evidence/hq-ref/pass4-source-verification.json` maps tested source-file/scene hashes to frozen runtime fc005d4; `pass4-editmode.xml`, `pass4-playmode.xml`, `pass4-capture-manifest.json`, `pass4-review.md`. Both G1 fixed hashes and all approved skin SHA preserved; Core tree unchanged. Keyboard return compares actual resting layout and verifies input/Start do not overlap; title/companion-name/HP vertical fit regression catches the observed rejected layout-A text loss.

Whole-screen comparison supports party/ground separation and stone-relief progress, not R2 V0/Human GO. Final26 PNG/contact/HTML remain private temporary local artifacts under `/private/tmp/rogue-hq-pass4/`; no cross-session media delivery. Current-source Simulator run pending. ASC refresh reached login; current compliance/group state unverified, no upload or new owner distribution. Apple old build5 is not fc005d4.

Pass4 native completion: frozenfc005d4 export/build/install/launch PASS, both14-action outcomes and full hashes match. Protect paused/held/resumed; both results/close/retry/initial state checked. Native on-screen Japanese composition「かつ」confirmed, input restored and exact value retained in result. Actual recordings82.22s Protect (pause/terminal included),47.90s Attack (terminal/comparison included),1206x2622; normal-speed Attack playback observed movement, local attack/HP11→5/receipt transition and terminal. Detailed limits/source/SHA in `evidence/hq-ref/pass4-native-observation.json`; this is Simulator evidence, not actual iPhone or TestFlight.5 export-generated settings diffs audited/restored, private patch retained. Current ASC login required; no new archive/upload/distribution.


## HQ-REF-1 Pass5 rejected art experiment / retained runtime fc005d4 (2026-10-17)

[WebGPT review5554614607](https://github.com/tomooch/rogue-tactics-lab/pull/21#pullrequestreview-5554614607) was executed from ca550012. CONTROL/A/B each ran actual Unity6000.3.25f1 Configure + filtered ProductFlow1/1, with seven fresh390×844 stages per variant. A improved water opacity but left disconnected shelves/tiny blooms. Repair B exposed the buried flower crown and improved grouped flowers/source connection, but its cliff reads as a uniform extruded wall. Root and bounded independent local image review rejected both whole vignettes; all19 candidate files restored, no art/code adopted or expanded. R2 V0 FAIL/Human pending remain.

Fresh retained-source full regression: Configure exit0, EditMode14/14, PlayMode9/9 PASS, both full frozen hashes and approved4skin SHA preserved. All tested C#/shader/OBJ/provenance bytes match retained HEAD; Core/tests unchanged. Generated Scene only differed in local object/fileIDs; normalized full serialized graph equivalence was asserted, tested raw Scene SHA retained in evidence, and task-generated ID churn restored. This is not a raw Scene byte-identity claim. `evidence/hq-ref/pass5-source-verification.json`, `pass5-editmode.xml`, `pass5-playmode.xml`, `pass5-experiment.json`, `pass5-review.md` record precise scope, source/hash, failure and production-capability gap.

Owner-local temporary `/private/tmp/rogue-hq-pass5/` holds21 CONTROL/A/B actualPNG, same-stage pairs/crops/contact/HTML, rejected-source patch and26 fresh retained images. No candidate motion/Simulator PASS; retained fc005d4 has previous Pass4 source-linked Simulator evidence. Historical Pass4 Attack video replay0.422→8.441→14.139s confirmed the opaque strip silhouette persists during movement; this is not new native execution. Current ASC DOM again shows sign-in: live build/compliance/group unverified, no new upload/archive/writes, real iPhone/Human pending. Public GitHub/Notion receive text/hash only; cross-session private-media sharing remains unestablished. Existing Notion Todo/Learned updated, no duplicate task/new product Decision. PR21 stays Draft.
