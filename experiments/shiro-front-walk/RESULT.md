# Shiro front walk spike result — Issue #9

技術工程はPASS。正面6コマの自然さ・可愛さ・正式採用はHuman判定待ち。既存の不合格歩行素材は正式採用していない。

## FACT / 観測結果

- branch: `codex/issue-9-shiro-front-walk-spike`
- baseline main: `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`
- 実Unity検証対象source commit: `6ed401b866d7a6c77cea8ad8a12ed067e3468740`
- ZIP SHA256: `e66a730a2aa5e349ec3b5c573421e137ce5730241e18c8e9c4af54aa4c493f42`。READY_FOR_SOL / DESIGN_FREEZE / README、参照画像、QA / atlas / previewスクリプトとIssue #9を確認。実mainとroot README / DESIGN / VERIFICATIONも確認。root文書のStage0 pending記述は現在mainと相違するが、このspikeで無関係な文書を変更しない。
- Humanが表示したface/full-body候補に「よい」と回答。ZIP内の非公開原参照はGitに追加せず、hashで識別。4方向参照を今回の生成素材に使用していない。
- built-in ImageGenで各コマを独立制作。初期frame6は支持足が違い棄却。frame2輪郭再生成、frame6背景付き再生成、その背景除去案も棄却。frame4の姿勢編集によるframe6を選択。正面以外・別キャラ・ゲームCore変更はない。
- Human AUTO-GO後、外周の既知mask色と不要な孤立low-alphaをコード補正。元PNG6枚のhash不変。各frameのface ROIと保護対象内部の変更0。before/after/difference画像6枚を保存。
- 不要画素除去後、共通倍率でキャラ高約154pxから約414pxへ再配置。明暗背景の390×844 / H.264 / yuv420p / 8fps / 6秒MP4を生成し全decode成功。

## QA result

`qa/commit_machine_validation.json` / `output/qa/final_validation.json`: PASS。
RGBA、atlas1536×1024、3×2、セル512×512、全alpha基準の四辺40px以上完全透明、境界非越境、atlas各セルと独立frameの完全一致、alpha0のRGB0、6枚の異なるhash、原画像保存、face ROI不変、既知外周maskフリンジ0を検査。足元目標(256,470)、raster誤差≤0.5px。実際の靴底を手動指定した座標と当該alpha可視性を検査し、bbox角を接地座標に代用していない。

`qa/harness-tests.txt`: 6/6 PASS。透過・余白失敗、足元の空白座標拒否、低alphaによる暗黙crop防止、輪郭補正時の内部・AA保護などを検証。

既知mask色の検査は任意の色フリンジ全般の数学的証明ではない。明暗の差分画像と実Unity描画を視認した範囲で大きな残留背景面は見られない。自然な歩行や顔同一性は機械QAの合格対象へすり替えない。

## Unity result

実Editor `6000.3.25f1`（project pin一致）でimport / compile PASS。Editor生成metadataを保存。atlasは100PPU、bilinear/clamp、mipmapなし、非圧縮、サイズ維持。native `Sprite.Create`で6枚を厳密sliceし、共通pivot(256,42)。標準Animator/controllerと8fps・0.75秒loop clipを専用sceneへ生成。

対象source commit上でEditMode 6/6 PASS。そのうちEditor coroutineが実PlayModeへ入り、2秒間に6種類のsprite切り替えを観測、normalizedTimeが1より進行、12枚の390×844明暗RenderTexture画像を取得した。既存Stage0 PlayMode 1/1 PASS。描画画像を実際に視認。Core、既存Stage0、Packages、ProjectSettings、default build scenesに変更なし。

初回はEditModeのままPlayMode APIを呼んで失敗。修正後はLegacy Animationがsprite referenceを更新せず1枚のみで失敗。標準Animatorへ修正し実Editorで再生成、commit後に再検証してPASS。失敗も`qa/unity-first-attempt.json` / `qa/unity-second-attempt.json`へ保存。全ログにはlicense情報がありGitに含めず、結果XMLとcommand/revisionを保存した。

## 成果物一覧 / 相対path

| 成果物 | path |
|---|---|
| 保存した元PNG6枚 | `source/shiro_walk_front_01.png`〜`06.png` |
| 輪郭補正したauthoring PNG6枚 | `input/shiro_walk_front_01.png`〜`06.png` |
| 最終独立PNG6枚（512角） | `output/frames/shiro_walk_front_01.png`〜`06.png` |
| 最終atlas | `output/shiro_walk_front_atlas.png` |
| 配置・hash・pivot | `output/manifest.json` |
| 実靴底の指定座標 | `input/foot_anchors.json` |
| 補正前後差分6枚 | `qa/shiro_walk_front_01_difference.png`〜`06_difference.png` |
| 補正測定 | `qa/contour_cleanup.json` |
| commit付き機械QA | `qa/commit_machine_validation.json` |
| iPhone向け評価MP4 / inline GIF | `output/qa/shiro_walk_front_iPhone.mp4` / `.gif` |
| 6コマ一覧 | `output/qa/contact_sheet.jpg` |
| Unity command / revision / 観測結果 | `qa/unity-verification.json` |
| Unity実描画12PNG・結果XML | `qa/unity/` |
| Unity実描画pose列のMP4 | `qa/unity/shiro_Unity_captured_poses.mp4` |
| 生成・棄却記録 | `generation.json`, `repair-prompts.json`, `qa/raw-generation-record.json` |
| 再現script・検査 | `scripts/`, `tests/` |

Unity scene / atlas / slice / clip / controller / import設定はrepo rootから `unity/Assets/ShiroSpike/`。Unity描画pose列MP4は取得した12画像から組み立てたpreviewで、画面録画ではない。実時間中の自動切り替えは別途testが観測している。`diagnostic/`は小さすぎた旧FAILを保持する比較証拠のみ。棄却画像はlocal一時worktree内の`rejected/`に保持しGitから除外、hashと理由を記録。

## INTERPRETATION / Art・motion risk

基準絵を参照した独立生成と、輪郭補正・配置・検査の分離でUnityへ持ち込める候補は成立した。ただし支持足を共通点へ合わせると半周期の切り替えで胴体が横へ動く。顔・口の微差、上半身の動きの少なさ、6→1の切り替えが滑り/跳ねに見える可能性は残る。顔ROI補正0は「6枚の顔が同一」の証明ではない。

## UNRESOLVED / 未解決

自然さ・素朴な可愛さ・顔一貫性の最終Human合否は未取得。物理iPhone / iOS build / human playは未観測。CIは設定がなく未実施。技術PASSは正式art採用や製品方向の承認ではない。

## NEXT / Humanが判断すること

iPhone向けMP4/GIFで、左右交互の接地→支持→振り出し、足が開きっぱなしでないか、胴体の横移動や6→1が自然か、顔が同じシロに見えるか、可愛さを判断する。合否と問題frameを記録してから、この6コマ内で再制作するか採用するか決める。他モーション・他キャラ・Coreへ拡張しない。mergeは禁止のまま。

証拠追加の後続commitは画像・Unity sourceを変更せず、上記verified source commitへのリンクを保持する。
