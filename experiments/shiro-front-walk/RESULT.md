# Current C-0 shared-parts feasibility — FAIL / C-1 not executed

## Latest sources / Human result

main `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`、PR #12 latest baseline `8524ddb4a389deb878a5487611e02ecd196bd70c` をfetch/APIで照合。root README/DESIGN/VERIFICATION/AGENTS、Issue #9、最新[staged C handoff](https://github.com/tomooch/rogue-tactics-lab/pull/12#issuecomment-6121644343)を確認。専用worktree/branchを継続しcanonical mainの既存`.gitignore`変更には触れない。rootの旧Stage0 pending記述は現在mainと相違するが、無関係な修正はしない。

Human確定: **A NG、B NG、Bの足滑りは気にならない、「バラバラの絵」「連続感がない」**。6コマ不足は疑問/仮説であり確定原因ではない。旧A/B証拠と全source/input/output/alignment hashを保存。旧Human GO待ち記述は歴史的状態で、現評価の正本ではない。

## C-0 HYPOTHESIS

同じ原画・共通部品から、骨盤→股関節→太もも→膝→すね/靴を接続し、胸/肩/髪/服を連動させるための隠れ画素を局所補描できるか。顔を共通化しても、関節と衣装が成立しなければ進めない。

## C-0 EXPECTED RESULT

左Contact、Passing、右Contactの3主要ポーズが、同じシロ、閉じた膝/股関節、離れない手/杖、自然なスカート遮蔽を保つ。3ポーズで成立した場合のみC-1を6コマ/8fps/0.75秒で制作する。

## C-0 OBSERVED RESULT

`continuity/C0/`に部品inventory、欠け領域、原画crop、local paint候補2枚、共通部品PNG、3試作ポーズ、source-space pose、part/pose manifest、onion skin、拡大診断、初回試作保存を作成。

- masterは既存cleaned `input/shiro_walk_front_01.png`。承認参照は元ZIPのhashで識別、私有参照をGitへ追加しない。
- 元の顔/帽子・衣装部品を共通sourceから管理。元部品をbind配置に戻すと元絵とbitwise一致。source顔ROIはhead部品内でbitwise一致。ただしexportは変換/再samplingを含むので全poseの顔pixel同一と主張しない。
- 統合PNGの骨盤、股関節、膝背面、肩/袖の付け根、手/杖の後ろ、cape/hairの裏は未描画。マスク分離で回収できない。
- Built-in ImageGenで下半身の隠れ領域を局所補描1案。靴まで再生成されたためcandidateの靴は不使用。骨盤/上腿のみ試用し、元front bootと明示的に反転した試験用companionを保持（元rear bootは保存）。この反転/再構成はCの変更でありB pixel一致ではない。
- 左右の股関節・膝のparent transformを設計。胸をcounter-rotateし、共通頭部を小移動、腕と握り手/杖をgroup管理し、cape/hairに追従を与えた。脚全体の平行移動だけ、全身一括bobだけの試作ではない。
- 初回3poseで膝/靴に切り抜き線、衣装の欠け・断片が見えた。脚後ろの隠れ衣装だけを局所補描する限定修正を1回追加。元paint・初回poseは`attempt1/`に保存。
- 修正後も全3poseに太もも/靴下周辺のhard/jagged seam、Passingの股下に青いwedge、staff/腕/靴部品に隣接hair/cape/clothing fragmentが残る。新しいfrill構造の整合性も確立しない。
- Executor視認と限定した独立read-only敵対レビューはともに**C-0 FAIL**。HumanによるC art評価ではない。

## C-0 QA result

Exact C-0 source `1ccc436894e56cd6d8a0389a048b2d23bf3f378c`でvalidatorを実行し、C-0 FAILを記録。既存Python回帰9/9 PASS（exit0）。command・時刻・QA hash・ログは`continuity/C0/commit_verification.json`。この後の証拠commitは画像/配置script/Unityを変更しない。

`continuity/C0/qa.json`。RGBA512、四辺40px以上（最小44px）、境界非越境、alpha0 RGB0、旧A/B全hash不変、元bind復元、source face ROI、共通頭部source、rigid transform/数値上の関節長 constancyはPASS。膝hinge誤差は約1e-13 source px。

しかし関節長は推定2D試験点の数値検査であり、隠れた実解剖学的形状の証明ではない。元画像への接続paint/遮蔽/歩行位相はFAILまたは未成立。既知外周mask色は最終各pose22/23/22画素でFAIL。alpha255も含むため顔・衣装の輪郭を無差別削除してPASSにしない。顔一致やgeometry PASSでC-0合格にしない。

## C-0 DECISION / stop boundary

**C-0不成立。C-1へ進行しない。** 原画から存在しない関節・背面衣装を少数の局所補描で整合させる方法は今回の限定試作では成立しなかった。これは真正のlayered rig一般が不可能という結論でも、6コマ不足の証明でもない。無制限な補描/全身生成を続けず停止する。

## C-1 HYPOTHESIS / EXPECTED / OBSERVED / DECISION

- HYPOTHESIS: 共通原画から一周期として6コマを制作すれば、独立した絵の切り替え感が減る可能性。
- EXPECTED: 6frame/8fps/0.75秒の連続した歩行。
- OBSERVED: **未実施**。C-0 gateがFAILのため6コマ、final atlas、B/C walking comparisonを作成しない。
- DECISION: C-1の仮説は未検証。8/12コマにも増やさない。正式art/長期制作方式採用なし。

## Unity / iPhone evidence

C-0で停止したため、新CのUnity import/slice/Animator/PlayModeは未実施。Unity source/assetsは変更なし。旧Bのsource `f0776ddfc7569edd381781b4ecae91bb736112ad` に対するPASSは保存し、新Cへ転用しない。Core/Stage0/default scene/Packages/ProjectSettings変更なし。

- C-0通常速度のwalk動画は存在しない。`C0_failed_three_pose_diagnostic_iPhone.mp4`は失敗した3静止poseを各2秒保持する診断表示。390×844 H264/yuv420p、6秒、全decode PASS。8fpsはcontainerの値で、6コマ歩行/0.75秒loopを装わない。
- `three_pose_contact.jpg`, `joint_clothing_closeups.jpg`, `parts_contact.jpg`, `three_pose_onion.jpg`, `occlusion_inventory.jpg` をGitHubから閲覧できるようPRへ掲載する。
- Bの通常歩行MP4は既存`alignment/B_iPhone.mp4`のまま保存。C単独/B-C同期/6→1 walking loopはC-1未実施のため提供しない。

## Recommended next production proposal (not adopted)

真正のeditable neutral/rest原画を先に準備する。必要なのは閉じた骨盤、各脚の完全な太もも/膝/すね/靴、きれいな袖/腕/握り手+杖、相補的なスカート/cape/髪の裏。可動範囲で必要なpaintが揃ったら、この3pose gateだけを再実行する。全身独立sprite追加ではなく、部位対応が一貫したsource rig artworkを制作する。別案の一周期動画から抽出はproposalのみ、外部有料サービス/追加uploadは実行しない。どちらも長期採用・正式Decisionではない。

## Human / unresolved

今回のC0失敗はExecutorの成立性判断。Humanに完成歩行として合否を求めない。次に確認すべきは、必要なlayer原画準備へ制作工程を移すか、別の一周期動画方式を比較対象として検討するか。次の有効なcandidateができた時の最重要評価は「同じシロが一連の動作として自然に歩いて見えるか」。物理iPhone/CI/新C Unity未観測。Draft維持、merge禁止。

---

## Preserved historical A/B technical evidence (latest Human rejection annotated)

# Shiro front walk: sole vs head/torso alignment A/B — current experiment

**最新Human評価: A/Bとも自然さNG。Bの足滑りは気にならず、「バラバラの絵」「連続感がない」。可愛さ/identityの独立採否は未評価。Bの技術QA PASSは維持する。** 同じ6枚を整数X移動するだけで比較し、画像生成・脚の描き直し・倍率/速度変更は行わない。

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

## Unity B — current verified result

実Unity6000.3.25f1でBを再import・compile PASS。Bは全spriteに共通canvas-center pivot(256,256)。rootは固定(0,0,0)、per-frame root/sole補正を禁止。共通camera Yを2.6→0.46に変え、旧foot pivotからcanvas pivotへの共通座標原点差だけを相殺し、同倍率・同じ縦表示範囲を保持する。これはposeごとの補正ではない。8fps/0.75秒clipは同一。

新検証source commit **`f0776ddfc7569edd381781b4ecae91bb736112ad`**。EditMode6/6 PASS（実PlayModeへ入るB test含む）、既存Stage0 PlayMode1/1 PASS。B atlas SHA=`8be8019ea74d4bcdb376f2bf6b6fd6d9e24174a185aa7b166dd0d096a5964810` と実slice.texture参照を確認。clip6key順序/時刻、実PlayMode phase一致、17回のsprite観測と順序/wrap、root固定、body world座標を確認。完全frame区間のgame-time差は平均0.125040秒（8fps）。明暗12枚の390×844実RenderTexture画像を視認。今回の新結果であり旧PASSを転用していない。`alignment/unity_verification.json`, `commit_verification.json`, `unity/playback-observation.json`, XML/PNGに保存。後続commitは証拠・記録のみ、画像/配置/Unity sourceに変更なし。

## UNRESOLVED / NEXT

Bの自然さはHuman NG。足滑りはHumanが「気にならない」と観測。可愛さの独立評価は未取得。数値で胴体揺れが小さくても正式採用しない。物理iPhone/iOS build・CIは未観測。HumanはA/Bを再生し、左右揺れが改善したか、足滑りや頭/胴体の静止感が増えていないか、03→04/06→01と顔一貫性が自然かを判断する。Draft維持、merge禁止。ゲームCore/他キャラ/他motion/有料サービス変更なし。

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
