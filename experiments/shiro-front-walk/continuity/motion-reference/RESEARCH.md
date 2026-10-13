# Existing human walking footage → motion-reference study (2026-10-12)

## Executive conclusion / status

**実歩行の下半身から時系列の関節proxyを作り、同じ0.75秒で6/8点の診断比較を作成した。ただし正面全身の実動画観察と、シロのアート制作工程は未成立。** 足の動作資料は得たが、これだけで「自然なシロ歩行」完成とはしない。AI歩行動画生成は利用せず、シロの原画も新生成/外部uploadしていない。

推奨する次の小実験は**実動画の時系列ガイドを使った、真正のneutral/restレイヤー原画からの3主要ポーズ制作**。C-0の完成PNGマスク切出しを再利用せず、関節と衣装裏を最初から描く。骨格の動きとキャラクターのpaintを別gateで検証する。現段階の不足は正面全身観察・完全な原画・その作画能力であり、8コマ追加では埋まらない。

## Current FACT / sources

main `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`、Draft #12 HEAD `e456de252d4865e7202f087c942ddda9b3642999`をfetch/gh APIで照合し専用既存worktreeで継続。PR最新[Human訂正を記録したresearch request](https://github.com/tomooch/rogue-tactics-lab/pull/12#issuecomment-6124210979)とIssue #9本文/comments、root README/DESIGN/VERIFICATION/AGENTS、RESULT、A/B/C-0 evidence、Notion Todo/Tried & Learnedを読む。rootの古いStage0 pending記述はmain実態と異なる歴史的文書の問題であり、今回変更しない。

- A自然さHuman NG。Bは顔X range 53.93→1.08px、胸55.47→0.68pxだがHuman NG。B足滑りは気にならない。「バラバラの絵」「連続感がない」が最新Human観測。
- C-0 source `1ccc436894e56cd6d8a0389a048b2d23bf3f378c` は隠れ関節paint・衣装裏・継ぎ目FAIL、C-1未実施。9/9 regressionやgeometry PASSはart成功ではない。
- B実Unity source `f0776ddfc7569edd381781b4ecae91bb736112ad` は歴史的証拠。今回Unity実行やart変更なし。旧source/input/output/alignment/continuity/C0全tracked filesを保存。

## Actual reference registry / observation limits

| ID / title / creator | URL / usable time | 実確認 / camera | Speed / useful movement | Rights / suitability / unknown |
|---|---|---|---|---|
| H1 EMG and foot contact recording during human treadmill walking; Macleod C, Meng L, Conway B, Porr B (2014), PLOS ONE Video S2 | [source](https://commons.wikimedia.org/wiki/File:Reflex-Control-of-Robotic-Gait-Using-Human-Walking-Data-pone.0109959.s005.ogv), [paper](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0109959); 3–42s walking, detailed 10.00–12.08s | 原配信OGVをlocal取得、ffmpegで時系列フレームをdecode/視認。640×360,25fps,42.52s。固定側面、下半身中心、左右足の解剖学的名前は映像だけで未確定。頭/肩はcrop外 | 候補一周期10.56–12.08s≈1.52s±0.12s目視推定。膝折れ→下腿追従→passing→脚伸展/踵接地候補、支持中belt後送、後脚踵上げが読める。実m/s未計測 | [CC BY4.0](https://creativecommons.org/licenses/by/4.0/)。原動画/抽出写真はrepo再収録せず、URL/hash/手動proxyのみ。下半身タイミングに適合。上半身/正面の根拠には使えない。EMG/FSR図は数値化していない |
| H2 Walking behavior with UDW; Yoon J, Park H, Damiano D (2012), Journal of NeuroEngineering and Rehabilitation | [source](https://commons.wikimedia.org/wiki/File:A-novel-walking-speed-estimation-scheme-and-its-application-to-treadmill-control-for-gait-1743-0003-9-62-S1.ogv); 0–42sを3秒間隔で確認 | 原OGV取得・時系列decode。固定側面、手すりで腕/骨盤が遮蔽、頭は上端crop | 映像中0.5/1.0/1.5m/s表示、速度変更/停止がある。これは表示値で自測値ではない。速度の混同を避ける反例、定速周期の主資料には不採用 | [CC BY2.0](https://creativecommons.org/licenses/by/2.0/)。写真/動画再収録なし。全身/正面/腕counterphase分析には不適合 |
| S1 Ranger-Front Walk; Kurt Kaiser (2021), Adobe Animate製自作game sprite | [source](https://commons.wikimedia.org/wiki/File:Ranger-Front_Walk.gif); 0–0.9s全30frame decode、0/5/10/15/20/25を視認 | 正面chibi、固定camera、顔/服の形が近い連続例。ただし下肢の接地/膝の読取は弱い。出荷ゲームの動画とは確認できない | GIF metadata 30枚×30ms=0.9s。固定部品・頭身管理の参考候補であり、優れた自然歩行と認定しない | CC0。取得hash記録、原GIF再収録なし。シロより単純な服/髪/prop、可愛さ・自然さHuman未評価 |
| U1 Walking Reference Video (FRONT); Mooven | [Vimeo](https://vimeo.com/1117830829); usable range未確認 | 実browserで「メディアを再生できません」。タイトル/作者のみ確認、動作は未観察 | 正面/全身/周期をタイトルから断定しない | license未確認、download/rehostなし |
| U2 Walk Cycle Reference; Thomas Howard | [Vimeo](https://vimeo.com/1089013288) | 公開descriptionはtreadmill撮影という作者説明。動画の再生/時系列は未確認 | camera/周期/身体動作未確認 | 権利未確認。URL紹介のみ |
| U3 Male Walk Cycle Animation Reference / Front & Side / Realtime; Endless Reference | [reference listing](https://animationreference.org/video/IwxzcU18pYRKNAn6OCf4) | ページの作者名/タイトル/Pro表示のみ確認、動画観察なし | 優先条件に合いそうという候補。時刻/速度は未確認 | downloadはPro表示、今回は購入/登録なし。再利用権未確認 |
| U4 Natural Gait; Paul D. Larsen / Suzanne S. Stensaas, Adult NeuroLogic Exam | [university page](https://neurologicexam.med.utah.edu/adult/html/gait_normal.html#02) | 実browser埋込playerはblank、別playerリンクもblank。動作は未観察 | 「normal associated upper-extremity movement」は作者説明、測定ではない | [BY-NC-SA / educational only](https://neurologicexam.med.utah.edu/adult/html/creative-commons-license.html)。商用ゲーム素材への転用を許可済みとしない。今回は未取得 |

CMU mocap official hostはweb timeout/local DNS failure。ASF/AMC/AVIを取得していないため、mocap計測値と偽らない。優先した**正面・全身・左右識別・定速**の人動画はこの環境ではまだ確認できていない。研究packetはこの欠けを明示した部分成立で、Motion gate全面PASSではない。

Source H1 SHA256 `21a1aaa44e6d93028950f9bacf22b77f740659f2ba55e83e3b789d75383d1bae`、H2 `f06a3b0d9eb258d6f47170ffc08b456e340731753f936af23e20780618767d1f`、S1 `3b069971b18b56ec18eac682d9e0064d201c158403a952734e047b708929b3a0`。local tempのみ保存。元動画公開元/著者がシロや本研究をendorsedした意味ではない。

## H1 cycle analysis / measured vs estimated

`manual_landmarks.json`に9時刻のA/B股関節proxy・膝proxy・足首proxyのimage XYを記録。640×360 source、原点左上、y下向き、足首±8px、膝±10px、隠れ股関節±15px以上の手動推定。股関節は隠れるため実中心の計測ではない。クロス時は左右追跡に不確かさがある。A/Bはphase対応を手動追跡した脚名で、実人の左/右とは断定しない。シロ左=Aは**設計上の割当**。source25fpsなので時刻解像度0.04秒、抽出requested時刻と厳密PTSの差も残る。

| source候補時刻 / 0.75sへの対応 | 観察・推定 | 設計への変換 |
|---|---|---|
| 10.52–10.64s / 約0 | A前脚の踵/足底接地候補、B後脚支持 | 左Contact。double supportから荷重移行候補。荷重そのものは未測定 |
| 10.75s /0.09375 | A支持側、B踵が上がる | Down候補。ただし骨盤minは元動画から確定できない |
| 10.94–11.06s / 約0.19–0.25 | B膝屈曲、下腿と足が支持脚近くを通る | Passing。膝が先、足先が追従するため脚全体平行移動をしない |
| 11.13s /0.28125 | Bが前方へ出て下腿伸展に移る | Up/振り出し候補。pelvis/head upは設計値で補う |
| 11.12–11.32s / 約0.28–0.375 | B接地候補。contact時刻は広い不確かさ | 右Contactを設計phase0.5へ置くにはphase整形が必要。単純等時サンプルとevent-key版を混同しない |
| 11.51s /0.46875 | B支持、A踵上げ | 右Down候補、force未測定 |
| 11.70–11.90s / 約0.56–0.66 | A膝折れ/Passing/前進 | 左Passing/Up候補 |
| 11.96–12.12s / 約0.69–0.77 | 次のA接地候補 | 次の左Contact、周期は推定 |

**OBSERVED:** 支持脚の踵が上がり、遊脚の膝が折れ、足が支持脚付近を通り、下腿が前へ伸びて接地へ向かう時系列がある。belt上の接地足は画面内を後へ動く。静止spriteの靴底X固定とは別物。

**NOT OBSERVED:** 頭/胸/肩/左右腕の軌道、骨盤の真の重心/roll、実荷重/force、正面の脚交差/遮蔽。上半身がcamera外であるH1から腕逆位相や頭bobの値を捏造しない。一般的原則は設計仮説として別記。

## Shiro motion design proposal (not accepted / not final measured motion)

1周期は左Contact→次の左Contact。target0.75s。実候補1.52sから約2.03倍速へretimeするので、実人の自然速度をそのまま保持した比較ではない。初めはsource速度版も残し、fast targetがせわしなくならないか見る。

- 共通identity: 顔輪郭/眼/口/表情/帽子装飾/衣装模様/杖paintは一つの原画モデル。同じ内部textureを再利用し頭全体のtranslation/小rotationは許す。screen上の顔固定や異なる顔生成は禁止。
- 接地/support: 左0、右0.5、左1を**設計key**とし、各Contact直後の短いdouble support→単脚支持→反対脚Passingを分離。sourceのevent時刻をphase正規化した版は新artifactとして作り、等時sample版と比較する。いまのsampleはその整形をしていない。
- 短脚: pilot thigh50px/shin60px等はrig診断用設計値（シロ実原画の採寸ではない）。実paintを得て採寸し共通骨長へ置換。膝折れ→下腿追従の順は保持し、成人の歩幅をそのまま移さない。3D/2D+depthの前後差とfront projectionでforeshorteningを管理。正面だけの横開脚で代用しない。
- 骨盤/胸: Downで小さく沈む、PassingからUpへ持ち上がる案。cellでpelvis上下peak-to-peak4–8px、胸2–5px、head2–4pxを**初期探索範囲**とし、観測数値や強制受入閾値にしない。root安定、胸・肩は骨盤に対し小counter motion。靴底Xで全身補償しない。
- 頭/帽子: head同一unit、顔局所変形なし。大頭なので成人振幅を弱める。headが上がるタイミングはhead/front footageを観察してから確定する。
- 腕/杖: free armを反対脚と逆位相にする案。持ち手・握り手・杖は同じ階層で接続、杖側は小さい振幅。H1ではこれを観察できていない。
- 裾/髪/cape: pelvis/胸に対しphase0.03–0.08（約23–60ms）遅れる小motion案。慣性方向を前の速度に対応させ、装飾/模様は保つ。物理計測ではない。裾越しに遊脚が折れ/通過する余地を最初から描く。
- loop: phase1のposeだけでなくvelocity/遮蔽/左右part IDもphase0とつなぐ。最終spriteをstartと同一に重複追加しない。現診断はendpointを明示的にstartへ置換したpiecewise linear curveで、位置C0連続でも速度のseamは未解決。実動画のendpoint残差を消したことと実測seamが0であることを混同しない。

## Same source, 6 vs 8 points

`build_motion_reference.py`は手動proxyから側面の太もも/下腿angleを計算し、同じ短骨の診断骨格へ変換する。**正面モーションは生成していない**。9個目endpointはstartへ置換した設計curve。実endpointのproxy差とangle差は`motion_samples.json`に残す。直線補間は観測していない中間姿勢を推定するモデルで、ground-truthではない。

| 条件 / この設計curveに対する値 | 6 | 8 |
|---|---:|---:|
| target周期 |0.75s|0.75s|
| 一枚保持 |0.125s|0.09375s|
| 相当fps |8|10.6666667|
| angle sample-hold RMS error |10.82°|10.00°|
| 最大hold error |36.54°|39.49°|
| 最大隣接angle jump（wrap含む） |36.77°|39.82°|

8点は平均誤差が小さくても最大jumpが大きい。測定誤差・phase samplingとの関係があり、**8枚なら自然になるという結果ではない**。6点ではDownとPassing/Upが近似混合され、遊脚屈曲のピーク/接地直前の伸展を飛ばす可能性。8点は分離する余地があるが、元の誤った関節/identity/phaseを増やせば悪化する。等時8点=4つの意味pose自動成立でもない。

診断MP4は390×844,H264/yuv420p,32fps carrier、上6/下8、24tick=0.75sを8周期。同じ表示倍率/骨格、6点を4tick、8点を3tick保持。0.75秒比較を整数tickで厳密に表すためcarrier32fpsを使用。物理iPhoneの32fps再生可否/表示dropは未観測。診断を通常8fpsのシロwalkと呼ばない。

## Art workflow options / actual availability

| Candidate | Input/output・identity/temporal mechanism | Hidden paint・Unity・automation | Effort / cost / failure risk / local status |
|---|---|---|---|
| A reference-guided sequential drawing (Krita) | 同じ原画/model sheet＋pose guide→editable .kra/PNG列。duplicate frameとonion skinで隣接絵を描き継ぐ | 隠れ関節は作者が描く。PNG→既存atlas/Animator。書出し自動化、作画は自動保証なし | 3キーでも中工数、全身別絵生成のidentity driftを減らしやすい。Krita公式は[frame-by-frame/onion skin](https://docs.krita.org/en/user_manual/animation.html)を説明。このMacにKritaは検出なし。未導入 |
| B common character 3D/2D model motion control | 一体のモデル/rig＋時系列angle→render/PNG。shapeを共有しidentity/骨長を保つ | 本物のhidden geometry/paintが必要。Unity/Blender render→sprite。motion automation高 | 初期モデル制作大。以前procedural Shiro3D Human NGを無視して同じmeshを再利用しない。Blender app未検出、モデルなし |
| **C genuine layered neutral artwork＋motion-guided 3-key pilot (recommended)** | 新たに可動用途で描いた完全な部位/表裏＋共通顔＋pose data。骨格curve/部位ID/paintを共有。PNG/PSB/manifest→rig or bounded export | closed pelvis、完全thigh/knee/shin、sleeve/arm/hand+staff、skirt/cape/hair裏を最初から準備。paint swapで角度別シルエットを補う | 準備中〜大、pose反復小。紙人形/mesh stretchingは残るので3キーでgate。C-0のmask PNGは正本rigに昇格しない。現状complete artworkなし |
| D pose-conditioned image generation | OpenPose/ControlNetのpose conditioning＋参照モデル/時系列module→画像列 | unseenpaintを推論し、identity/loop/alphaの保証なし。PNG→Unity可 | [ControlNet](https://github.com/lllyasviel/ControlNet)はposeを条件化するが単体の時間的一貫性は保証しない。[AnimateDiff](https://github.com/guoyww/AnimateDiff)はmotion module/SparseCtrlを提供、重み/依存/モデル権利/computeが別途必要。torch/model未導入、実生成未検証。今回採用・実行しない |

[Unity 2D Animation13](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/index.html) / [Skinning](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SkinningEditor.html) / [Sprite Swap](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/ex-sprite-swap.html)はbone/mesh/weightsと部位差分に対応する。ただし現在manifestには2D Animation/PSD Importerがなく、6000.3.25f1でこのrigを実行した証拠なし。入れるだけでhidden paintは生まれない。新package追加は今回未実施、後続taskでcompatibility/versionを確認する。

MediaPipe Pose Landmarkerは[official guide](https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker)でvideo landmarkを提供。ただしlocal mediapipe/cv2/torchなし、モデル未取得。今回は手動annotation、深度や遮蔽/荷重を自動推定できると扱わない。

Tool audit: Python3.12/NumPy/Pillowとffmpeg/ffprobeでdecode/手動points/数値変換/MP4を実行できた。built-in ImageGenはavailableだが、今回未使用でprecise骨格sequenceをhard constraintとして渡せるAPI/temporal保証は確認できない。Unity6000.3.25f1既存sprite Animatorは前回観測済み、今回起動していない。Krita/Blender/OpenToonz app、mediapipe/cv2/torchとtrained modelsは検出なし。新account/有料API/シロ外部uploadなし。

## READY_FOR_CODEX — bounded next task / not approved art adoption

**HYPOTHESIS:** 実測に近い一周期のphase/軌道＋可動用途の完全な共通paintを共有すれば、「別絵の切替」ではなく一体の動作に見える可能性。
**EXPECTED RESULT:** 左Contact・Passing・右Contactで接続/遮蔽/identityが成立し、後続6枚の一周期でHumanが同じシロと認識する。骨格だけでは期待結果成立としない。

1. M0資料gateを閉じる: 実再生可能な**正面全身**人動画1本を取得/閲覧し、左右脚を色服/解剖で追跡。固定cameraの定速2周期を選び、頭/胸/肩/骨盤/hips/knees/ankles/feet/armsと接地時刻を全frameまたは最低12時刻でannotation。source URL/hash/license/PTS、測定/隠れproxy/uncertaintyを記録。side H1は補助。見えない部位をnullにして勝手に補わない。権利不明ならURL/timeのみ、取得不能ならgate未完了として停止。
2. source timelineをevent-mapで左Contact0/右Contact0.5/次左1へ設計retimeし、0.75sへ変換。source速度版とtarget速度版を両方残す。6/8等時版とContact/Down/Passing/Up key版を分離。pose/sample JSONにsource_time/target_time/support/depth/qualityを持たせる。root安定、sole X再固定禁止。
3. **art prerequisite**: 完成PNGの再切抜きではないeditable neutral原画を用意。共通head/face/hat、torso/shoulders/pelvis、左右complete limb、free arm、grip+staff、前裾/裏裾/cape/backhair。source/paint差分/overlap/pivot/z-orderをmanifest化。準備できないならSolが作れると偽らず、原画作者/手動作画能力が必要と報告。外部発注/課金は別承認。
4. 3主要poseだけを制作: 左Contact、片脚Passing、右Contact。膝/骨盤/裾遮蔽/腕staff連結をmotion guideに沿って変える。headtexture共通、全身bobだけ/脚平行移動だけは禁止。onion overlays/接続拡大/骨長/part ID/変更領域を保存。Humanに同一感・関節・可愛さを確認、3静止poseで自然なloop合格とはしない。
5. 成立した場合のみ6枚: 01LContact,02LDown,03RPassing/Up,04RContact,05RDown,06LPassing/Up。8fps/0.75s、元Bと同表示倍率・明暗・390×844。3→4/6→1の部位velocity/形状/遮蔽を重点確認。8枚は比較提案のまま、勝手にC finalへ置換しない。
6. Final6 atlas1536×1024/3×2/512/40pxRGBA、QA、Unity6000.3.25f1 actual import/native slice/Animator/6/8fps/root fixed/明暗/Stage0 regressions。PNGに新paintがあればB bitwise一致を要求せず出典と変更を記録。exact source commit、実描画/transition、通常速度iPhone MP4を保存。Human「同じシロが一つの連続した動作として歩くか」が最終gate。

**STOP CONDITION:** 正面/body左右が追えない、hiddenpaint未準備、3poseでjoint/seam/服模様/顔/杖連結NG、uniform骨長でも紙人形、loopにphase飛び、2回以内の局所修正で整合しない場合は6/8量産を止める。AI providerでwalk生成して穴埋めしない。方式自体を見直し、今回の研究を長期採用にしない。

## Reproduction / evidence boundary

```text
python experiments/shiro-front-walk/scripts/build_motion_reference.py
```

Python with NumPy/Pillow, ffmpeg at `/opt/homebrew/bin/ffmpeg`。入力は公開元からdownloadするvideoではなくchecked-in手動proxy JSON。原動画取得不要で設計診断を再生成できる。`manual_side_proxy_chart.jpg`は手動proxyの静止可視化、`skeleton_6_poses.jpg`/`skeleton_8_poses.jpg`/`skeleton_6_vs_8_iPhone.mp4`は設計変換であり実測人/完成シロ画像ではない。

**DECISION:** 研究資料のみ。motion gate部分成立、art/自然さHuman未評価。新正式Decisionなし、Draft維持、Core/main/旧evidence変更・mergeなし。
