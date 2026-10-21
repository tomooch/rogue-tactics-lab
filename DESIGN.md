# Native mobile の設計方向

正本は [Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。本書は契約の要点であり、実装完了の記録ではない。Web v0 は retired historical evidence。[当時の設計](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/DESIGN.md) は immutable snapshot `0d8a2a68c37902256f5123ef86017ce83a60a64e`（tag `archive/web-v0-final`）に保持し、active tree に Web implementation を持たない。Pages は current product / runtime ではなく、Human が Unpublish 済み。PR #4 は merge 済みで、Web v0 は retired 済み。accepted implementation は常に actual GitHub `main` HEAD。[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) のUnity Stage 0はmerge済み。以下はmerge前の実測revisionの記録。実行した HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af` で Unity 6000.3.25f1、import / compile PASS、EditMode 5/5 PASS、PlayMode 1/1 PASS、390×844 portrait smoke PASS。証拠は [VERIFICATION.md](VERIFICATION.md)。実機未検証、CI なし、human play 未実施。Stage 0 の成立を gameplay 仮説や frozen direction の採用証拠にはしない。

## Core hypothesis

> 自分の予想を仲間に託し、その戦いから理解を深め、次の可能性を試すRPG。

自発的変更案 → 実際の変更 → 事前予想 → 戦闘 → 行動に使える理解の更新 → 次の自発的変更案、を観察する。演出の豪華さだけを最適化しない。

## Major invariants

- iOS / Android native、Unity 6.3 LTS / C# / URP。portrait only、390×844 reference。landscape UI は作らない。
- 探索と戦闘は同じフィールド。別戦闘画面に切り替えない。小さな2.5D / diorama、orthographic または near-orthographic。
- A / B / C / D は on-board。軍師 E は off-board のコンパクト UI のみ。正解を示す oracle にしない。
- deterministic な離散 turn / step。ATB、実時間 countdown、時間圧の入力、各キャラの Attack / Heal メニューは採用しない。
- plain C# Core がルール・合法手・意図・結果・イベントを所有する。MonoBehaviour / GameObject / Animator / scene state に依存させない。
- Presentation は Core の状態・イベント・計画を描く。同じ初期状態と入力列から同じ状態・イベント列を得る。animation timing が結果を変えない。
- 意図は actor → target/destination の line、semantic icon、移動先 ghost で表す。通常プレイを4人同時の説明吹き出しに依存させない。表示 horizon は未決。

## 意図修正の実験

Issue #1 の高レベル party policy 最大3つ（バランス・慎重・攻める）は experiment parameter / prototype candidate。Human の最終採用や frozen product decision ではない。隠れた毎ターン micromanagement にしないという実験上の制約とともに評価する。

encounter ごとに味方1体の意図修正を **1回** 使用可能。残数を表示し、次の distinct encounter で reset。広告や報酬で追加しない。修正後も Core が次の行動を決め、Presentation が偽装しない。

USER_DECISION を確定した後の experiment build では、合法な場面で target / destination / hold-cancel / action alternative の correction superset を試し、play evidence に基づき削減する。Core の明示的 command と合法性判定を使い、永続 gambit / rule editor に変えない。

strategist-dependent intervention hypothesis: E の experiment configuration によって利用できる correction family を変える。隠れた戦闘結果の改変は行わず、最終 class system や正解提示として固定しない。

## 未解決 USER_DECISION

以下は gameplay 実装前にユーザーが決める。候補を承認済み仕様として扱わない。

1. 探索gestureと目的の表現等。盤外のparty監督役BはHumanが#1で承認済み。Aを直接操作する方向にはしない。
2. Resolution chunk: 1回の委譲でどこまで自律行動が解決され、どこで次の判断に戻るか。
3. Intent horizon: 次の1行動か、複数手先か。

PR #5 の Stage 0 は Unity 基盤のみ。上記 USER_DECISION を固定する gameplay、署名・実機検証は含まない。


## Design Freeze G1 — 2026-10-13 / Issue #14

最新の[実装契約 #14](https://github.com/tomooch/rogue-tactics-lab/issues/14)が今回の範囲を定める。Stage 0はPR #5でmainへmerge済み（baseline `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。Humanは盤外の監督役Bを承認済み（#1）。探索入力の正確なgesture、resolution chunk、intent horizonは未決。PR #6の広いdocs同期は取り込まず、Stage 0 mergeと盤外Bに限りstale記述を訂正した。

- 問い：同じ初期戦況からBの戦術選好だけを変えたとき、実行行動・HP・生存・行動数・回復資源に読み取れる差が出るか。Humanが次の変更を自発的に考えるかは人試遊でのみ判断する。
- 二条件：`ProtectAnchor`（生存Aに隣接して守る／近づく）と`DirectAttack`（共通の近接攻撃規則）。Bの数値や他役の規則、敵、初期配置、行動順は同一。どちらも推薦しない。
- fixture：5×5、A(2,2) HP18/攻撃5/生存被弾後counter2、B(1,2) HP16/攻撃4/guard軽減3、C(3,3) HP12/攻撃4/Aと同じ敵へ異なる隣接位置から攻撃時flank+2、D(1,1) HP10/攻撃2/射程2でHPが4以上欠けた最低HP味方を4回復（2charge）、X(2,3) HP18/攻撃6、Y(3,2) HP14/攻撃5。匿名実験役のみ。シロ魔法／ミャル弓／ガル大剣／モモ盾の最終能力とは対応させない。
- 行動順B→X→A→C→Y→D、死亡actorはskip。敵は生存Aを優先、その他は距離→ID。移動は空いた隣接マスで距離が縮むN/E/S/W順。fixture用であり探索routeの仕様ではない。guardはBの次行動で失効、死亡／離隔で無効。致死被弾後のcounter・死者回復なし。
- 予想は変更後・実行前に任意自由入力。Core規則を読む導線を設け、結論や正解は示さない。戦闘終了後は同一baselineへ戻して別条件を試し、独立の結果を併記する。
- 検証adapter：Core APIは1 actionの純粋transition。UIでは1 actionまたはencounter終端までの明示的検証操作と、現在planの可視化を用いる。これらはG1実験用adapterで、正式resolution chunk／intent horizonの既定ではない。
- 期待：同一入力でsnapshot・event・SHA-256が同一、合法行動のみ、単一選好による実行trace差、HP／行動数等の少なくとも1指標差、実390×844操作で開始・観察・比較・再試行できる。仮説や面白さ成立の観測とは区別する。
- stop条件：勝利／全味方死亡／120 actions上限。差が実規則で生じなければNOT ESTABLISHEDとしてfixtureを記録付きで修正し、偽の結果は作らない。
- 対象外：floor探索、宝箱／relic／第二戦、1 correction UI、E提案・受諾、正式class／skill、アニメーション、LLM、network、課金、telemetry、merge。G2とHuman試遊はNotion Todoに残す。

## Mobile-first Visual G1 — Issue #16

PR #15 exact baseline `aa1fc05b154abcfa0e1a5c90574bf91adc97d881`へstackする専用branch `codex/mobile-visual-g1`。Core・初期戦況・数値・plan・event順序・両trace SHAは変更しない。表示・日本語UI・safe area・静止2D skins・3D dungeon・iOS build/署名/本人限定Internal TestFlightが今回の範囲。2026-10-13のHuman GOはゲーム専用Apple登録・署名・upload・本人限定配布を許可済み。一般公開・外部テスター・SizeSnap変更・原画の外部AI送信・main mergeは含まない。

Human指定の4枚をローカルで確認：シロ（0FBC…）、ミャル（2F1A…）、モモ（65C7…）、ガル（FD16…）。顔/瞳/衣装のRGBを再描画しない。背景alphaだけをローカルの既存Apache-2.0 segmentation modelで準備し、元SHA不変・source RGB一致を検査。原画/派生texture/captureはprivate local出力に保持し、公開Gitへ画像を入れない。ゲーム専用内部配布のbuildへだけ含める。

仮のvisual対応はAシロ/Bモモ/Cガル/Dミャル。匿名のfixture能力を正式class/skillに昇格しないことを詳細UIへ明記。動作は既存Coreが確定したeventだけを順に投影する。Coreのtransitionと演出queueを分離し、未表示eventから先のHPや意図を表示しない。守護・軽減・反撃・回復・戦闘不能を位置/線/短い日本語/HPで示す。

画面はsafe area内に日本語HUD、約60%の3D field、戦術選択→任意予想→実行→比較→同一baseline再試行を置く。歩行を待たず静止skinを使う。resolution chunk/horizonはG1 adapterのままで正式採用しない。

I0 export/Xcode build、I1本人限定TestFlightの提供状態、I2物理iPhone操作、V0承認skinと3D実描画、V1イベント/表示可読性、HUMAN本人評価を別ゲートとして記録する。未接続実機やApple認証待ちをPASSにしない。

## HQ-REF-1 — Issue #20 / 2026-10-15

Source contractは[Issue #20](https://github.com/tomooch/rogue-tactics-lab/issues/20)と[監査コメント](https://github.com/tomooch/rogue-tactics-lab/issues/20#issuecomment-6184262380)。baselineはPR #17 `ce4ffac8d2216973c391d801cf5f6d41fbde66b5`。Core G1を維持してR1 HUD、R2遺跡・camera、R3 event描画だけを比較する。G2・階層・minimap・箱・正式能力・8 skill buttonsは追加しない。通常の自動観察は今回のG1 adapter候補で、正式chunk/horizon採用ではない。

Humanは今回のLocal実行で `_userwork/image/天穹の遺跡、12階の冒険.png` を比較用に指定。941×1672、実SHA `99a76f33c60e74d7fd890e9cf188531280e030e58a5893bafd90899ea2b965b8`。Issue PRIMARYのSHA `9b4ee65b…`とは異なる。指定ファイルを最新のHuman指定ローカル比較基準として扱うが、Issue原本とのbyte一致・同一原本によるfidelityは未証明。元画像/skin/実captureはpublic Git外、外部AIへ送信しない。provenanceは `evidence/hq-ref/asset-manifest.json`。

構図：主役は光の当たるcream stoneと青空の浮遊遺跡、立体的な縁・水流・蔦・小花・blue/gold banner。盤面はCoreの5×5のまま、地面と装飾は独立3D geometry、4人は既存承認skin。HUDは上の小さなタイトルと離散actor順、下のシロ/ミャル/ガル/モモportraitと実HP、方針/任意予想/開始→停止再開→結果比較。匿名A/B/C/D mappingや数値は維持する。portrait順が戦闘順や能力を再定義しない。

観測契約：R1は旧camera/environmentでHUD比較、R2は同HUD/fieldで75°と63°の固定yaw比較、R3はその後にintent/effectを比較。画像は実390×844 Unity render、setup/guard/counter/heal/death/endを記録。重要な失敗は人物・足元HP・矢印の被り、未表示のHP/死亡/結果の露出、pause中のcursor進行、queue脱落、Core trace差、generic flat terrain感。V0/Human評価は自動テストからPASSにしない。

端末予算は暫定30fps以上、最大60fps、780×1040 field render target、mesh/material共有、定常フレームでgeometry再生成しない。実測は環境とrevisionを付け、Editor/SimulatorからiPhone実機性能を推定しない。描画負荷に問題があれば装飾の低品質設定で軽減する。新package/有料asset/network/analyticsは追加しない。

R2の最初の75°/63° fixed-yaw比較では、63°の奥行きの差は小さく、双方とも参考画像へのV0はFAIL。単なる色替えを続けず、表示空間の奥行き間隔、足元ラベルの可読性、視野内の実3D植生・column・water、skinのmipmap samplingを修正。その後の55°/yaw0と55°/yaw−18°を別snapshotで比較する。55°/−18°候補は全戦闘frame回帰でX/Cの輪郭重複を検出したため、55°/yaw0°へ戻して検証する。Humanのcamera preference採用ではない。表示用セルのZ間隔はXとは別倍率だが、Coreの全座標・距離・行動・数値は変更しない。

R3は実行中eventのpresentation phaseと完了したdisplay cursorを分離する。HPはimpact時、死亡skipはDeath表示時、結果・比較は全queue表示後に有効化。pauseは動きの位相もcursorも保持する。新Actionの待機では旧intentを消し、previewの青と実行の金・開始ring/伸びるline/arrowheadを区別する。局所tweenとflashは既存静止skinへのpresentationで、#13の歩行spriteアニメ完了ではない。

接地表現は既存URP同梱のScreen Space Ambient OcclusionをMobile rendererへEditor APIで追加し、half resolution / low samplesで使う。新packageやhosted dependencyではない。軽量描画ではSSAO・post effects・装飾の動きと影を止める。原画RGBの再描画はせず、texture mipmap/trilinear samplingと表示用の短いevent tintだけを使う。cast接地は生成soft-alpha、石のgrainは局所生成textureで、元のreference PNGをfieldへ貼らない。

### HQ-REF-1 Pass2 — 2026-10-15 packet 6192468412

開始HEADはPR21 `72552b609d5b544cde5edb5233b5baa95e1a69b4`、既存branch `codex/hq-ref-vertical-slice`。新しいpackage/外部素材/課金/通信を追加せず、private原画/skinのRGB、Core/2敵/14行動の両traceを維持する。

Pass A仮説：反復9×9舗装をruntimeで増やす手法から、固有の輪郭・破断面・石組み・側面・前面滝を持つoffline mesh assetへ変更すると、同じ55°/yaw0/同Core位置でも浮島遺跡の形が画面で読める。期待は場面の同定とpartyの主役性。比較ではbuild4と同じ390×844、同camera/actor/HUDを初回controlとして保持。最初のasset実描画後に必要な構図修正だけを分離し、画像に基づき判断する。期待を観測結果とは扱わない。

Pass BはA画像レビュー後の別比較：Moveだけに軌跡を残し、非移動は実actor/targetの局所アクセントと名前/効果で区別する。Core eventのimpactでのみHP/Deathを更新し、pause pose/cursorとpreview/execution区別を維持。比較sheetをrosterより前へ置く。true sprite gait/G2/能力追加は範囲外。

承認済みprivateローカルフォルダの再探索でもPRIMARY SHAは見つからず、指定原画と旧approved-combined画像は共に`99a76f33…`。このpassでPRIMARY fidelity PASSは主張しない。パケット内Protect hashの転記は一部省略があるため、実Coreと既存固定gateが使う完全値`e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d`を保持する。

### 2026-10-16 PRIMARY RGB同一性の更新

Issue #20 [最新照合](https://github.com/tomooch/rogue-tactics-lab/issues/20#issuecomment-6203653575)により、PRIMARY 9b4ee65b… とMac指定PNG 99a76f33… はPNG bytesは異なるが941×1672の復号RGB全1,573,352画素が一致。LocalでもRGB SHA `fd52b3654adc91c3e086b21cc83a4d9cb1d2057a5cd527b0fbc5cbb3a15a3ce4` を再現した。以前のPRIMARY fidelity未照合という記載のうち、比較原画の同一性ブロッカーは解消。実描画の原画fidelity・R2 V0 FAIL・Human未評価・Notion未同期は別であり維持する。

### Pass3 bounded environment prototype

前景左端だけに独自の欠けた石積み、複数の割れた崖、丸みのある葉と花、垂れた蔦、曲面の滝を作成。`tools/author_hero_ledge.py` と実Editor生成Meshで再現する。旧front-left滝だけを置換し、盤面のCore座標・床・camera45°yaw0°・人物source RGB・既存HUDを保持。新しい角のpaletteだけsRGBを明示し、他の環境材質には適用しない。独立実画像レビューでcorner Aの立体感は改善と観測したが、部品の寄せ集め感/水の横帯を検出したため同じ角で自己修正。島全体への複製や美術方向の採用は行わない。全画面V0/Human合格を意味しない。比較と残欠陥は `evidence/hq-ref/pass3-review.md`。

C-fixedの同camera/skin/geometry比較で、HQ world cameraのpostprocess ONは人物・空の色/輪郭を弱め、OFFは視認性を改善した。独立実画像reviewも確認。DoF単独では原因を特定できず、より広いpostprocess経路として切り分ける。HQだけこの経路を回避し、SSAO/接地影/水流は保持する。非HQの処理は変更しない。処理の除去を原画RGBの補正や全体V0合格とは扱わない。

### HQ-REF-1 Pass4 composition candidate (2026-10-17, pending PR21)

Runtime `fc005d4` preserves G1/5x5/initial placement/two enemies, approved four RGB assets and camera. Product shelf is compacted with prediction beside Start and terminal comparison beside retry. Authored courtyard now has worn relief, restrained joints and selected connected moss; A/B grid/flat candidates were rejected from actual Unity images before full-floor adoption. Source and comparison evidence: `evidence/hq-ref/pass4-review.md`. This is a visible whole-screen step beyond Pass3, not Human V0/V1/fun acceptance; R2 V0 remains FAIL. No new gameplay, abilities, gait, service, merge or release.

### HQ-REF-1 Pass5 representative environment experiment (2026-10-17)

Following [review5554614607](https://github.com/tomooch/rogue-tactics-lab/pull/21#pullrequestreview-5554614607), camera/HUD/floor/heroes/Core/tests were held fixed while one front-left ledge was authored, rendered at390×844, reviewed, repaired and rerendered. A's disconnected shelves/tiny blooms and B's smooth extruded cliff failed the representative art gate despite partial water/flower gains. Neither candidate is retained or expanded. Runtime remainsfc005d4, R2 V0 FAIL. The missing step is intentional phone-scale cliff/masonry modeling, not more procedural density or a floor tweak. [Failure evidence and bounded production option](evidence/hq-ref/pass5-review.md); no art purchase, external upload or production choice approved.

### ART-CAPABILITY gate (2026-10-17)

[Review5555468771](https://github.com/tomooch/rogue-tactics-lab/pull/21#pullrequestreview-5555468771) triggered a distinct production-capability investigation at retainedfc005d4. Existing Unity sample sculpted FBX plus editable local mesh/control source was actually imported, authored, repaired and rendered at390×844; no slab/extrusion generation repeated. A/B failed recognizable stratified limestone and B weakened ivy attachment. Programmatic edit/save/reload works; actual GUI editing/Undo/reopen is unverified. **BLOCKED_ART_CAPABILITY**, no candidate adopted; runtime unchanged. [Concrete missing capability, source/license and cost/privacy options](evidence/hq-ref/art-capability-review.md). No installation/commission/purchase/reference upload approved or performed.

## Local Blender / Astra R2 production gate — 2026-10-18

[長時間実行パケット](https://github.com/tomooch/rogue-tactics-lab/pull/21#issuecomment-6236909453)の無料ローカルBlender pilotを実行。Blender5.2.2 LTS ARM64の公式SHA/署名/公証、実GUI編集・Undo/Redo・保存・close/reopenを確認した。以前の未導入/GUI未観測は解消したが、原画の美術品質を成立させた証拠ではない。

保持runtime `fc005d4` の前左cliffだけを変える3案（破断Boolean、個別patch cage、実GUI大面sculpt）をそれぞれUnity390×844の7場面で比較・独立local画像review・修正した。全案が層状石灰岩/既存cap・水・蔦との接続/全画面改善を示せず棄却、採用0。**BLOCKED_ART_PRODUCTION、R2 V0 FAIL**。Astra実native `gpt-6-astra` / highは4回のsanitized text-only助言であり、private画素を見たレビューではない。Human可愛い/見やすい/楽しいと実iPhoneは未検証。G1/4skin/5×5/2敵/HUD/床/カメラ/表示event契約は維持。

次の不足はソフトではなく、代表領域の見える輪郭・大きな破断層・植生支持面・水との前後関係を一体で設計し、電話サイズで成立させる制作。単一mesh固定は今回の実験条件で、cap変更をHumanが永久禁止したものではない。失敗案を他領域に展開しない。private source/画像はMac内、公開は[source-free実行記録](evidence/hq-ref/longrun-review.md)/hash/テスト証拠。新規課金/外部制作/原画送信/製品採用/merge/Readyの承認を推定しない。

## Integrated rim visibility gate — 2026-10-19

[Human支持のreview5566270401](https://github.com/tomooch/rogue-tactics-lab/pull/21#pullrequestreview-5566270401)に従い、cap/崖/水源-lip/蔦支持を一体で扱うGate0を実行。fixed camera/床/HUD/4人/Coreのまま、編集可能なoriginal Blender blockout A→Unity390×844→local独立画素review→重複する旧rim/壁を許可外周内だけ除くB→再描画を行った。Aの遮蔽はBで減ったが、一体棚/側方開口/水・蔦の接触が電話で読めず、両FAIL・採用0。詳細sculptや他領域へ展開せず保持runtimefc005d4へ完全復元。BLOCKED_ART_PRODUCTION/R2 V0 FAIL。Astra実native2相談はsanitized text-only・画素未閲覧で、別local reviewerがA/B各7場面を実閲覧した。

次の制作入力は、同じcamera条件でcapから支持塊・水路・植生根元を統合したartist sourceを優先する提案。無料CC0/有料編集sourceの具体的な費用・権利・適合限界と、別Human判断を要する非interactive環境hybrid案を[source-free記録](evidence/hq-ref/integrated-rim-review.md)に記載。購入/download/外部依頼/原画送信/表現方式変更は未実施・未採用。Human品質や物理iPhoneの合格を代行しない。

## Free local production boundary and source gate — 2026-10-21

最新Human指示で**有料素材NG・外部委託NO**。無料CC0 source＋Codex自身のlocal制作に限定し、Astraは助言。Poly Havenの編集scanをA→実Unity→独立画像review→配置修正B→片面/両面/単独診断→構造保持Cへ進めたが、狭い外周でcap/奥壁/支持/水/蔦を一体に読めず全案棄却、採用0・R2 V0 FAIL。小幅な石面/lipの改善を全体の美術合格にしない。同じ切出しの倍率/法線調整は終了し、無料の編集可能な閉じた形状sourceを調査中。製品方向/原画/4人/Coreは維持。[実行・棄却・復旧証拠](evidence/hq-ref/artist-source-pilot.md)。
