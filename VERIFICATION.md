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

## Mobile Visual G1 / Issue #16 — implementation in progress

baseline：PR #15 HEAD `aa1fc05b154abcfa0e1a5c90574bf91adc97d881`。専用branch `codex/mobile-visual-g1`、worktree `/private/tmp/rogue-mobile-visual-g1`。最新origin/main取得済み（`ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。PR #15や他のPRのbranchは変更しない。Core treeはbaselineとbyte同一を検査する。

環境：Unity6000.3.25f1 / URP、iOS Build Support installed、Xcode27.0 (27A266a)、iphoneos SDK27.0。game専用Bundle ID `com.tomooch.roguetacticslab`をApple Developerへ登録。本人ログイン済み。game専用証明書/profileは作成・検証済み。ASC appはHumanのアクセス設定承認後に登録済み。upload開始、本人限定Internal TestFlightの提供は未確認。physical iPhoneはpairedだがtunnel unavailable、実機touch/IME/起動およびHUMAN評価は未観測。

初回working-tree検証（2026-10-14 JST）：Configure/import/compile PASS、EditMode **14/14 PASS**、PlayMode **3/3 PASS**。新Mobile操作テストは390×844を維持し、ノッチsafe area、raycast top hit、戦術2条件、予想、start/next/auto/pause/reset/details/comparison、全表示event列一致・Core state projection・両frozen hashを確認。runner `/private/tmp/rogue-mobile-jobs/unity1/completion.json`、XMLとraw logs `/private/tmp/rogue-mobile-evidence/` は一時領域。private skin入り実Unity PNGは`unity/Logs/mobile-g1/private/`に保存し、公開Gitへ含めない。

初回画像でHPラベルの重なりを観測。label配置のみ修正し、Core位置は変更しない。独立reviewの指摘から、左右safe幅対応・詳細scroll・守護線の移動失効・Death eventまでpawnを残す修正も追加。回帰検査を追加して再検証中。正式resolution chunk/horizon、正式キャラの能力、HUMAN見た目評価を固定しない。

修正後working-tree検証：2026-10-14、job unity5、EditMode **14/14 PASS**、PlayMode **3/3 PASS**。狭い左右safe area、非重複HPラベル、HP全文字幅、詳細/比較の本文contrast pixelを追加検査。rootが最新setup/rules/comparison実画像も視認。初回MaskはImage alpha0により本文が非表示だったためalpha1＋showMaskGraphic falseへ修正。白に近い画素の絶対数は短い比較本文を誤検出したため、中輝度以上の字形coverageを検査する。空白の旧比較画像は同範囲coverage0、修正後詳細本文は12,722px（RGB>115）を実測。

ゲーム用の新しいDistribution証明書を発行し、生成した秘密鍵と証明書のmodulus一致を検査。ゲーム専用App Store profile `Rogue Tactics Lab Internal G1` はexplicit `9HXM297A8U.com.tomooch.roguetacticslab`／新規certificate1件／get-task-allow falseを検査しXcodeへ登録。既存SizeSnapや既存証明書は変更しない。ASC「アクセス制限あり」はUI上無効。現ユーザ本人1名でも「アクセス制限なし」は将来ユーザへ閲覧を広げるため自動承認reviewが拒否。限定された追加設定のHuman回答待ちで、アプリ登録・TestFlightはまだ未完了。証明書/profile/秘密鍵/Apple実画面はpublic Git外。

Unity 6000.3公式[SetBuildProperty](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/iOS.Xcode.PBXProject.SetBuildProperty.html)を使い、export時に明示したgame署名fingerprint/profileだけをmain targetへ設定する。UnityFrameworkにはapp profileを付けない。新しいiOS専用条件コードのcompile/exportとXcode buildは次のgateで実測する。

### Mobile source cdecba9 fresh run / signing and device-input follow-up

source **`cdecba9abe2ebc163e27227aa0afb45892e6edca`** を新Editor processでfresh検証。import PASS、EditMode14/14、PlayMode3/3、Mac preview build PASS、Unity iOS export PASS、Xcode27.0 / iphoneos27.0未署名build PASS。署名archiveはexit65でFAIL。生成projectのgeneric `CODE_SIGN_IDENTITY`は新game Distribution fingerprintだが、`CODE_SIGN_IDENTITY[sdk=iphoneos*] = iPhone Developer` が優先され、既存Development identityとgame profileが不一致になった。export helperでSDK別にも新game identityを指定する最小修正を追加し、再検証待ち。既存identity/profile自体を編集・流用しない。

fresh job record `/private/tmp/rogue-mobile-jobs/fresh1/completion.json`、XML/log/summary `/private/tmp/rogue-mobile-evidence/fresh/`。これらは一時領域。Mac build/iOS exportが生成したURP prefilter・window/batching/backendの4設定だけを、完了後にsource commitへ戻した。public Draft [PR #17](https://github.com/tomooch/rogue-tactics-lab/pull/17) はPR15 branchにstack、対象HEAD cdecba9、Draft、checksなし。

Mac previewのCUA実クリックは条件変更の到達を確認できていない。同Bundle IDの旧/新preview同時起動を確認して両方を閉じ、new単独起動・window raise・press duration/座標を切り分けたが未確認。Dock取得は操作ツールtimeout。これを実操作PASSにしない。旧PlayModeのraycast→`ExecuteEvents`はcallback直呼びで、InputDevice→InputAction→UIInputModule経路を検証していなかった。

新たにMouse/Touch stateを実InputSystemUIInputModuleへqueueし、条件選択・開始/次行動/停止/再試行を確認する2回帰テストを追加。batch EditorでのみIgnoreFocusを設定しfinallyで復元する。これは実機touch/native OS delivery/IMEの証拠ではない。初回job device1はEdit14/14、Play3/5（Mouse/Touchの選択assert FAIL）。device3の観測ではactionはenabled、device controlも解決済みだったが、queueしたpointer/button値はactionへ届かなかった。installed InputSystem 1.20のEditor routing既定値が、非focus Game Viewへのpointer入力を送らないことを確認。

最新devicectl JSONにはsimulatedとphysical双方が含まれる。physical iPhoneはpairedだがtunnel unavailableで、実機操作は未観測。ASC追加アクセス設定の回答待ちは継続。TestFlight upload/processing/本人限定提供/HUMANは未完了。上記の修正・新テストはcdecba9以降のworking treeであり、旧PASSを新sourceへ転用しない。

追加回帰job **device4** はPlayMode **5/5 PASS**。test内のみ `AllDeviceInputAlwaysGoesToGameView` と `IgnoreFocus` を設定しfinallyで復元すると、Mouse/Touch両方で実UIInputModuleのraycastが各buttonを指し、選択・開始・次行動・auto/pause・resetが成功した。product入力設定は変更しない。これはbatch Editorの仮想device経路を証明し、native OS／Simulator／物理iPhone／IMEの実操作を代替しない。修正前後のXMLを`evidence/mobile-g1/`へ保存。SDK別署名修正とSimulator exportを含む次の固定commitで、fresh import/Edit14/Play5/署名archive/Simulator buildを検証する。

### Mobile source 7d3ec53 / I0 verified, I1 in progress

検証source **`7d3ec532b3c47b10775a741a4572fe2aa9bd3046`**。fresh import PASS、EditMode **14/14**、PlayMode **5/5**、Unity iOS IL2CPP export PASS。最初の検証scriptはschemeの `-showBuildSettings -json` にUnityFrameworkが含まれると仮定してStopIteration。アプリtargetの署名は正しく、frameworkを `-target UnityFramework` で個別取得するようscriptのみ修正し、成功済み同source exportを使って続行した。app/framework両方が新game署名を選ぶこと、mainだけがgame profileを持つことを確認後、Xcode Release / generic iOS **署名archive exit0**。既存credential資産の変更なし。

archive実体のBundle ID `com.tomooch.roguetacticslab`、version **0.1.0**、build **1** を検査。`codesign --verify --deep --strict` PASS、embedded profileのgame application-identifier／get-task-allow false／新game certificate1件一致を検査。`xcodebuild -exportArchive` は **exit0、IPA1件**。exportは `method=app-store-connect`、`destination=export`、manual game signing、`manageAppVersionAndBuildNumber=false`、**`testFlightInternalTestingOnly=true`**。公開TestFlight／App Storeへ配布できないInternal専用設定。秘密鍵・profile・IPA・原画・private captureはpublic Git外。

public証拠は `evidence/mobile-g1/7d3ec53-*`。raw jobs/logは一時領域 `/private/tmp/rogue-mobile-jobs/final1/`（script取得失敗）、`final2/`（archive成功・Simulator継続）、`export1/`（署名検証とexport成功）。実際のappは`build/RogueTacticsG1.xcarchive`／`build/internal-export/`。Simulator成功・実機起動・IMEの代替にはしない。

Humanが2026-10-14にASC「アクセス制限なし」を明示承認し、ゲーム専用ASCアプリ **Rogue Tactics Lab / Apple ID6822532815** を登録。内部グループ **本人限定 G1** は自動配信OFFで作成し、本人Account Holder1名のみ追加、ビルド0件を実画面で確認。game source7d3ec53 / 0.1.0(1)のInternal-only uploadを開始した。**I0 PASS**、I1はupload/processing/対象buildの本人提供確認待ち、I2/HUMAN未検証。App Store審査・正式公開・外部テスター・公開リンクなし。
