# Shiro front walk: sole vs head/torso alignment A/B — current experiment

**AはHumanが動画を見て自然さNG。Bは技術QA PASS、自然さ・可愛さHuman GO待ち。** 同じ6枚を整数X移動するだけで比較し、画像生成・脚の描き直し・倍率/速度変更は行わない。

## Human observation / HYPOTHESIS / EXPECTED / DECISION

HumanはPR #12の動画を実際に視聴し、「接地足を軸にしているため、足の切り替えで頭と体が反対側へ寄る。頭と体を軸にすると良くなると思う」と指摘。これは実観察であり、Aの自然さはNG。仮説は「足裏X固定による全身の補償移動が主因」。期待は「頭/胴体を登録すれば大きな左右揺れが減り、自然さが改善する可能性」。自然さ/可愛さの採否はHuman、長期制作方針を今回だけで固定しない。

## Current contract / alignment

baseline PR HEAD `0572960aaf21855dff0b05467e8c64339a4480c1`、前回Unity source `6ed401b866d7a6c77cea8ad8a12ed067e3468740`、fetch時main `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`、Draft #12最新コメント/Issue #9を照合。専用既存worktree/branchを継続。

A=`output/`を全ファイルinventory/hashまでbaseline Git objectと照合し、そのまま保存。B=`alignment/B/`。入力source/input PNGも変更なし。顔・衣装・髪・杖・靴等の全painted RGBAはAのexact integer translationとbitwise一致。再拡大/再補正/再生成なし。共通scale `0.33452830188679245`、6枚順序、8fps、0.75秒loopを維持。

顔=両目irisの中点、胴体=青い襟元brooch中心。全6枚で同じ意味の点を選び、source座標で測定・overlayを視認。irisは手動指定した解剖学的ROI内の青色pixel centroid、brooch/両靴底は手動座標。推定誤差±4 source px（約±1.34 cell px）。alpha bbox中心や帽子上端は身体基準に使わない。検査は注釈の意味を独立認識しているわけではない。

BはfaceX/torsoXの等重み複合点をセルX256へ整数pxで移動する。deltaXは `[10,6,3,-36,-45,-36]`、全deltaY=0。顔をpixel描き直しや完全静止にする方式ではなく、顔と胸の相対leanとAの上下動を完全保存する。source靴底点はprovenance、Bの配置基準ではない。B manifestでは`source_contact_sole_point` / `measured_support_sole_point`へ明示的に名前を分ける。

## OBSERVED RESULT / coordinate comparison (cell px)

| 観測 | A Sole | B Head/Torso |
|---|---:|---:|
| face X (01..06) | 243.29,247.00,249.59,289.30,297.22,288.85 | 253.29,253.00,252.59,253.30,252.22,252.85 |
| torso X (01..06) | 248.60,253.23,256.23,294.55,304.07,295.07 | 258.60,259.23,259.23,258.55,259.07,259.07 |
| face X max-min | 53.93 | 1.08 |
| torso X max-min | 55.47 | 0.68 |
| face X 03→04 | +39.71 | +0.71 |
| torso X 03→04 | +38.32 | -0.68 |
| face X 06→01 | -45.56 | +0.44 |
| torso X 06→01 | -46.48 | -0.48 |
| face Y max-min | 4.44 | 4.44（不変） |
| torso Y max-min | 10.22 | 10.22（不変） |
| 頭頂のalpha bbox Y max-min | 2 | 2（不変） |

`alignment/comparison_metrics.json`に6枚ごとの顔/胸/左右靴底XY、全隣接差分とwrapを保存。ランドマーク誤差より小さいBの残差を高精度の身体変化として解釈しない。数値上、大きな左右移動は配置だけで解消した。歩行の自然さが成立したという観測ではない。

## Sole/contact/sliding risks

両靴底を測定し、supportはcharacter左01–03、右04–06。支持点YはA/Bで不変、支持足切り替えの脚ポーズ/接地タイミング候補も同一。Aの支持足X(256,470)条件はAにだけ検査を保持し、Bではbody複合X/bitwise translation/透明余白を検査する。足測定を削除したり固定を別の場所へ隠していない。

- 左支持01→02→03：Aの靴底X range約0.73px、Bは7.73px（266.30→262.23→258.57）。
- 右支持04→05→06：Aの靴底X range約0.21px、Bは9.21px（220.44→211.23→220.26）。
- 左靴底全6枚X range A51.36→B12.85、右 A60.42→B14.42。
- 接地足間のXは動く。in-place画像の靴底変位からworld-spaceの滑りを証明できない。Bの支持中の約8–9px移動が足滑りに見える可能性を残す。
- 03→04/06→01の大きな胴体X段差は減ったが、Y差は不変（胸のwrap +7.21px）、脚の振り出し量や顔/口差、01→02の小さいpose差も不変。動きが止まる/足だけ動く印象などをHumanが動画で判断する。

## Machine QA / adversarial review

`alignment/machine_qa.json`と`independent_qa.json`: PASS。全A inventory/source/input hash、RGBA、1536×1024 / 3×2 /512セル、全alphaで40px透明余白（B最小41px）、境界非越境、既知外周mask色0、atlasセルと独立PNG一致、同倍率/順序/速度、全画素exact integer X translationとdeltaY0、body複合点X誤差≤0.5px、Bの旧soleX固定拒否、左右sole記録。縮小して余白を通していない。回帰検査9/9 PASS、追加検査は旧sole lockのB混入、移動時のAA不変、余白不足時の停止を対象。

限定的なread-only adversarial reviewでpixel/scale破壊は見つからなかった。指摘されたB manifestの曖昧なfoot anchor名と、旧inventoryの削除検出漏れを修正。注釈の意味は視認依存、靴滑り/ループ/止まり感は未解決として保持。

## Videos / diagnostic assets

全動画390×844 H.264 yuv420p、8fps、48frame/6秒、8loop、前半明背景/後半暗背景、全decode PASS。A/B/ABすべてcell表示384pxで倍率/viewport/背景/再生タイミングを一致。上下比較を390×844へ収めるため、旧448px表示のA動画は保存したまま、今回の比較動画3本では共通384px表示を使う。asset倍率は一切変更しない。ラベル以外の描画条件は同じ。静止画比較は左A/右Bで同倍率。

- `alignment/A_iPhone.mp4`: 今回の共通表示条件でA単独
- `alignment/B_iPhone.mp4`: B単独
- `alignment/AB_iPhone.mp4` / `.gif`: 上A・下B、同期比較
- `alignment/paired_frames.jpg`: 全6枚のA/B静止比較
- `alignment/landmark_crops.png`: source解剖学的ランドマーク確認
- `alignment/diagnostic_landmarks.jpg`: 顔(赤)/胸(青)/左sole(緑)/右sole(橙)
- `alignment/trajectories.png`: 同一座標軸のface/torso X軌跡、6→1含む
- `alignment/B/frames/`, `shiro_walk_front_atlas.png`, `manifest.json`, `unity_landmarks.json`: B成果物

## Unity B — current verification pending

実Unity6000.3.25f1でBを再importする。Bは全spriteに共通canvas-center pivot(256,256)。rootは固定(0,0,0)、per-frame root/sole補正を禁止。共通camera Yを2.6→0.46に変え、旧foot pivotからcanvas pivotへの共通座標原点差だけを相殺し、同倍率・同じ縦表示範囲を保持する。これはposeごとの補正ではない。8fps/0.75秒clipは同一。

B atlasのfile SHAと実slice.texture参照、clipの6key順序/時刻、実PlayModeのphaseとsprite切替/wrap、root固定、body world座標、明暗12枚の390×844実描画、既存Stage0の非回帰を新commitで検証する。旧Unity PASSをBへ転用しない。検証完了後、この節とexact revision証拠を更新する。

## UNRESOLVED / NEXT

Bの自然さ・可愛さHuman GOは未取得。数値で胴体揺れが小さくても正式採用しない。物理iPhone/iOS build・CIは未観測。HumanはA/Bを再生し、左右揺れが改善したか、足滑りや頭/胴体の静止感が増えていないか、03→04/06→01と顔一貫性が自然かを判断する。Draft維持、merge禁止。ゲームCore/他キャラ/他motion/有料サービス変更なし。

---

## Historical original A technical packet (before Human NG)

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
