# 次の制作入力：統合外周1領域のartist-source pilot

**2026-10-20 最新Human境界：有料素材NG・外部委託NO。** これ以降は無料で権利が明確なsourceとCodex自身のローカル制作に限定する。以下の有料素材/artist発注案は調査時の履歴であり、承認待ちの現行候補でも実行対象でもない。モデルをOpusへ切り替えたり新しい推論サービスへprivate素材を送ったりせず、公開された制作/検証方法を調査して再現可能な部分を使う。Astraは引き続きアドバイザー、Codexが実行責任者。

**最新実測 2026-10-21：無料scanのA/B/Cは採用0・R2 V0 FAIL。** Cは石の量感/滝lipがわずかに明瞭になったが、張り出し・奥壁・不均等支持と植生接触を一体で読めず、全体の品質改善を示せないため棄却した。局所改善を合格へ水増ししない。6つの試作OBJ/native meshファイルをtask-owned検査後にHEADへ復元した。保持美術/挙動はfc005d4、今回の実装変更は失われた承認4PNGへのScene参照復旧と実Unity生成4metadataのみ。4PNG自体はprivateのまま、publicにはmetadataを明示的に追跡する。

復元後の新規全回帰はUnity6000.3.25f1 EditMode14/14・PlayMode9/9各exit0、両full fixed hash・承認4skin SHA・全凍結sourceを維持。実行sourceはHEAD `a86b6c29ce09395391360f80eedd0200c45bfa34`＋4参照GUID修復、Unity diff SHA `4e1e2823ee5ac5050518a4dc71c9ff47ea0f267ec20c72d21490743020b7f014`。新7場面と復元setupを実閲覧した。[source-free検証JSON](free-source-evidence.json)にrun別のsource/delta・各test名/結果・raw XML SHA・画像SHA・metadata SHAを記録した。raw XML/ログと媒体はowner-local。新Simulator/TestFlight/実iPhone/Human品質PASSはない。

## 無料source pilotとOpus周辺の公開手法からの具体的な変更

Humanの「進めて」を受け、無料CC0の[Coastal Cliff01](https://polyhaven.com/a/coastal_cliff_01)を公式file manifestから取得し、Blenderで形状/LOD/UV/依存textureを実検査するpilotを開始した。公式[license](https://polyhaven.com/license)は商用/改変を含むCC0。Rico Cilliers/Rob Tuytelのscan/cleanup sourceで、Codexの原創sculptとはしない。原画・skinは制作入力として外部に送らない。単に現実の崖を縮小すれば原画styleになるとは判断せず、Unityの固定画面で支持構造と接触が成立した時だけ採用を検討する。現時点はsource取得/検査job実行中で品質未判定、採用0。

Opus周辺の[公式harness研究](https://www.anthropic.com/engineering/harness-design-long-running-apps)は、制作と評価を分け、具体的な視覚基準、実UI操作、評価からの修正、方向転換、継続用artifactを使う。frontend/appでの報告であり、Unityの石灰岩を同じ品質に作れる証明ではない。本作でも既に実描画/独立review/固定source記録を使っているので、名称だけを新手法にしない。

今回追加する差分は、独立画素reviewerへrootの「良さそう/FAIL予想」や採用意図を渡さず、候補順を中立化すること。一体性、意図した形、仕上がり、戦闘の理解/操作を別々に記録し、技術PASSで美術不足を相殺しない。未知のsourceを購入/生成した数量や反復数で品質を代替せず、各案の実画面・event・sourceを保存して方向を修正する。[長時間agentの実行記録/継続手法](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)も参考にするが、新しい自動化サービスや常駐loopは導入しない。

旧checkoutで消えた4skinは、Mac内に残る承認原本4枚の全source SHA一致を確認し、既存の同一revision/hashのoffline segmentation工程で復旧した。生成4PNGは承認済みSHAにすべて一致。初回jobは推論成功後、存在しなかったコピー先directoryで失敗したため、そのdirectoryだけ作成して検証済みPNGを配置した。推論の再実行や似た別skinへの代用はない。

実Unity Editorが生成したtexture metadataで4参照を復旧した。Scene raw SHAは`4e6b58ed…`から`01c82355…`へ変わり、4つのCharacterArt GUID以外のbytesが一致することをassertした。raw Scene不変とは主張しない。新規CONTROLはProductFlowを実行しPlay1/1、両fixed hashと7場面を取得、setupの4人を実画像で確認した。これはfull Edit14/Play9の再実行ではない。

CC0 sourceの取得とBlender実検査は完了した。native `.blend`と3textureは公式manifestのsize/MD5一致、追加SHAを記録。新しい中立照明sceneで左右・中央のclay/textured計6画像を生成し、左・中央・右のclayと中央texturedを実閲覧した。左領域には自然な張り出し下面、深い窪み、不均等な支持部が連続する。写真textureの粒状感には既存styleと人物の主役性へのリスクがあるため、まずその領域を既存stone RGBでUnityへ入れる。Blender上の良さをゲーム内合格に置き換えない。現段階のUnity候補評価・採用は未完、R2 V0 FAIL。

今回のprivate証拠はcanonicalの既存ignore配下`_userwork/hq-evidence/2026-10-20-free-source/`に保持する。`intake.json`、`inspection/source-inspection.json`、`neutral-inspection/`、`skin-recovery-results.json`、`skin-installation-results.json`、`control/scene-reference-recovery.json`、`control/source-and-results.json`、`control/capture-manifest.json`が出典・実行条件・結果を区別する。private PNG・動画・skin・原画をpublic GitHubへ載せない。

局所source制作job `e797dc8b-e1dd-4a7c-9b01-aa94a72b6d38` はexit0。Blender5.2.2 LTSで編集可能な`artist-module-a.blend`を保存し、左領域LOD3の切り出しをuniform scale 0.42で配置した。出力6,029tri/3,639vertex、bounds x[-6.60,-0.72],y[-3.724,0.05],z[-10.476,-9.322]で許可AABB内。元source SHA `81e79b13…`、working blend SHA `d080f36a…`、HeroLedgeCliff OBJ SHA `5475f150…`。自然なscanの原topology/UVを保持し、原創sculptや閉じたwatertight模型とは主張しない。旧Limestone/Cliffの重複領域だけを切り取った計3meshがUnity試作対象。nativeモデルの制作成功は美術採用を意味しない。

### Unity A：制作成功と画面品質を分離して棄却

job `0ef1121b-54a9-44d3-84a5-385b0d9ee299` は90.77秒/exit0。Unity6000.3.25f1の実importと新規7PNG、ProductFlow Play1/1・両fixed hash、他code/shader/OBJ/4skin/復旧Sceneの保持をassertした。filter対象外のEditModeは0件でfull Edit14/Play9とはしない。実行sourceはHEAD `a86b6c29…`＋3OBJ/native mesh組と4texture参照GUID修復、Unity diff SHA `3ba5f432484edb6c7d06f026e50596379fd351cd96e418f7f41d16d7d8d9300d`。

rootはCONTROLとAのsetup全画面、AのGuard/Counter全画面、承認原画を実閲覧した。Aでは前左の崖の張り出し/窪み/支持が見えず、capと蔦が浮遊する印象。無料sourceの大形状が電話画面へ届かず、**A不採用・美術改善証明なし**。原画の厚い支持石/立体的な植生との隔たりを埋めない。

独立local reviewerはgpt-6-luna/mediumで、rootの評価・採用意図を渡さずX=A/Y=CONTROLのsetup/Guard/Counterを実閲覧。HUD・人物・戦闘可読性はほぼ同等、左下の植生以外の差は小さく、CONTROLの方が根付きの印象はわずかに良いと判断。「Xがすっきりした」を画面全体の品質向上には数えない。Astraの文章助言ともHumanの可愛い/楽しいとも区別する。詳細はprivate `unity-a/root-review.json`。

次の修正Bは局所sourceの前後配置だけ。実bboxの前面z=-10.476がcap前面-11.353より約0.877奥であるため、scan全体をz=-0.85移動して同じ許可領域内に置き直す。source topology/scale/RGB、cap/滝/蔦、カメラ/照明/床/HUD/人物は固定。これは遮蔽・接触への仮説を検証する配置修正であり、成功を先取りしない。Bの制作→Unity同7場面再描画jobを起動、結果未確認。

### Unity B：前後配置だけでは解決しない

job `3e2a179d-50c1-43de-975b-a052b5d50eb9` は160.02秒/exit0。Play1/1、Edit0（filter）、両固定結果と凍結52sourceの保持、7場面を取得。復旧Scene SHAは同じ`01c82355…`。sourceは同HEAD＋候補B、Unity diff SHA `607c45f76ae3bbd11dc9bda269f4ef4903cb5cd8583174aedc6f4c71d56aff6d`。working blend SHA `1737b1ae…`、局所OBJ `d0f78fdb…`、前面z=-11.326で許可AABB内。

rootはCONTROL/A/Bのsetup実画像を並べ、Bでも崖の輪郭/窪み/支持がほぼ読めないと確認。前進だけで解決する仮説は棄却し、Bも採用しない。cameraや色の再調整へ逃げず、実Unityで①崖単独の既存片面描画、②単独両面描画、③元の全体で崖だけ両面描画、の一時診断を起動した。これは表示欠落の原因切り分けで、両面描画を完成手法として採用するものではない。診断結果は未確認。

初回診断job `800e7bfa-2fea-490f-8c84-c1cf3e797754` はexit1。temporary testの`Debug.Log`が未処理ログとしてPlayModeを撮影前に停止し、画像0。meshは実runtimeへ3,639vertexとして存在し、world bounds center(1.34,-1.84,-0.75)・extents(2.94,1.89,.58)、materialのCull=2まで観測した。デバッグ記録をprivateファイルへ移す限定修正で再診断を起動。ゲーム側の回帰FAILとも、美術合格とも扱わない。失敗XML/logは保持し、temporary test/helperはfinallyで撤去する。

private `private-comparison.html`には承認原画とCONTROL/A/Bの各7実PNGを変更せず埋込み、全画像390 CSS px幅・縦横比維持で並べた。`comparison-manifest.json`に元PNGとHTMLのSHAを記録。ファイルはowner-localのみで、別セッション/WebGPTがアクセスできる共有の成立を主張しない。

### 診断v2：カリングではなく、選んだ形状と統合の不足

job `ab5a4805-89bc-4dad-adbd-05b55d08f429` は33.88秒/exit0。診断Play1/1・Edit0、3実PNGをrootがすべて閲覧した。①崖単独・片面でも見える、②両面化しても大形状は回復しない、③全体へ戻すと同じ棚形状がcap/葉に隠れる。**カリング主因説は棄却**。単独モデル自体が左の薄棚と右の楔に見え、元sourceで見えた大きな窪み/支持部を残していない。奥行きcutが重要部分を捨てた可能性を次に検証する。診断はCoreの両結果を検証したrunではなく、Bの既存結果と分離する。

修正Cは幅[-34,-20]だけをcropし、元scanの奥行き全体を保持する。許可AABBへ幅0.42/奥行0.18/高さ0.40で明示的にfitし、変形後の法線を再計算する。uniform scale保持とはしない。カメラ/照明/人物/床/HUDは固定し、同7場面へ再import。元形状の細部追加や単純押出しはしない。結果未判定。

Astra advisorへ今回の失敗観測と修正仮説をsanitized text-onlyで相談。画像/mesh未閲覧、品質採否を委譲していない。①幅crop内でも張り出し/奥壁/支持がつながるか、②奥行圧縮で空洞が線に潰れないか、③Cでも棚状または接触/境界違反なら、この切出しと固定範囲へのfitの経路を止め、同じ倍率/法線調整を続けない、との助言を受けた。C不合格を素材全体の利用不能へ一般化しない。

比較HTMLのChrome表示はfile URLのbrowser policyで拒否された。別経路のbrowser操作で回避せず、元PNGのローカル直接閲覧で観測を継続した。HTMLを実browserで表示確認済みとはしない。

### Unity C：局所の量感は回復、統合美術gateは不合格

job `4e977d12-c887-49fc-9bc3-6f2391df8e65` は144.94秒/exit0、filtered Play1/1・Edit0、両固定結果と7新PNG。11,346tri/6,581vertex、bounds x[-6.60,-.72],y[-3.794,.05],z[-10.993,-9.735]。working blend SHA `c286ef67…`、局所OBJ `018fea1b…`。rootはCONTROL/CのsetupとCのGuard/Counterを実閲覧し、A/Bより連続した石の面は見えるが、capから奥壁/支持部へ読める原画の立体構成には届かないと判断した。

同じ独立local reviewerへ、rootの判定を渡さずX=CONTROL/Y=Cのsetup/Guard/Counter計6実画像を提示。Cは石の量感と水源lipが少し明瞭、CONTROLは蔦の根付きが強い、人物優先度/戦闘可読性は同等、どちらも明確な支持部はない、との判断。rootは小幅な差だけでは代表領域のgateを満たさないとしてCを棄却。**このsource切出しを狭い外周へfitする経路は終了**し、同じ倍率・法線・RGBの反復をしない。CC0素材全体の利用不能やHumanの美術判断へ一般化しない。失敗patch/`.blend`/実画像はprivate保持、公開の美術meshは変更0。

次の無料sourceは作者[Quaternius Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html)の公式CC0/Blend提供を確認した。新MegaKitのBlend/engine sourceは有料版なので除外。旧パック公式download先にある`AllModels.blend`を取得し、36,730,024 bytes・SHA `6dbd16ccc5bbbf5f59306c4e49fa875552d35fd95a7bfc84f521af8fb35a60cd`・BLENDER headerを実確認。別private root `_userwork/hq-evidence/2026-10-21-free-stylized/`でautoexec無効のBlender実検査を開始した。編集可能な閉じたrockの形状/UV/material/依存fileを確認する段階で、美術採用・Unity成功は未判定。支出/外部委託/private原画送信0。

2026-10-20。最新Human指示「高品質に仕上げる為に努力を惜しまない」「Astraをメインにせずアドバイザーとして活用」を受けた具体化。**Codexが実装・実行・実画像の採否・自己修正・証拠・pushを所有し、Astraは美術方針と重大な失敗原因への助言を担当する。** rootのmodel変更、Astraへの主担当移管、判断の丸投げはしない。今回Astraは実native gpt-6-astra/highへ1回、sanitized text-onlyで仕様を敵対点検した。画像/private path/meshを渡しておらず、Astraは画素未閲覧。前日の2相談とは別。

これは制作依頼先へ送信したbriefでも、発注・購入・予算・表現方式変更の承認でもない。現時点はread-onlyの一次情報調査と制作条件の具体化。保持runtime fc005d4、R2 V0 FAIL・美術採用0を維持する。前日の[統合外周A/B](integrated-rim-review.md)を詳細化する経路は再開しない。

## 制作する単位と固定条件

対象は前左の非プレイ外周1領域。local x[-6.7,-0.5]、y[-4.2,0.15]、z[-11.4,-9.3]。**cap／侵食された崖／片側に開いた欠け／奥に後退した支持塊／水路→lip→落下／植生の根元と接触面**を同じ構造として設計する。岩単体パックの点数で代用しない。

Core・5×5・初期配置・2敵・承認4人のRGB/顔/目/衣装・床/Courtyard・カメラ・照明・HUD・event順を固定。Move経路、Attack/Guard/Counter/Heal局所表現、death suppress、未来状態リーク禁止、停止再開/予想/比較/retryを維持。環境の造形を成立させるために人物を小さくする・移動する・敵数を減らす方法は採らない。

固定cameraはortho10、pitch45、yaw0、target(5,-1.4,9.6)、distance21。Unity local envelopeは環境origin(5,0,10)に対する座標。Blenderとの軸・単位対応を納品sourceに記録する。viewport接写ではなく、実runtimeのRenderTextureとHUDを経た390×844を判断面とする。単独Blender作品画像・別の美しい照明は補助で、実ゲーム品質の証拠にはしない。

## 最初に見せる結果と棄却条件

1. **全画面で支持構造が読める**：capから張り出し、下面、奥の支持塊、側方の空の抜けを区別できる。旧rimとの二重縁、平行な積み棚、均一な歯、吊り下がる薄板、ランダムな穴は不合格。
2. **水と植生に接触の理由がある**：上面の浅い水路から一つのlipへ連続し、落下を遮らない。流れの幅や透明度だけで源を隠さない。植物は根元と岩の支持面が連続し、孤立した緑線・浮遊片・隙間を横断する蔦にならない。
3. **原画に近づく造形と材質がある**：不均一な石灰岩の大きい破断面と適度な丸み、層/窪み/厚み、まとまった植生が同時に成立する。細い傷・模様・密度・三角形数は評価の代替でない。既存の可愛い4人と競合する暗さ/コントラスト/情報密度は棄却。
4. **遊ぶ画面の主役を保つ**：4人・顔/衣装・足元/HP・event receipt・下部操作を含む全画面をCONTROLと並べ、環境が人物より先に視線を奪わず、戦闘状態を読みづらくしないことを実画像で確認する。Humanの可愛い/見やすい/楽しいは別判断。
5. **編集とUnity出力が再現する**：cap、cliff、water、vegetationが編集可能なobjectとしてあり、材質/UV/normal、必要texture、単位、export手順、元sourceのSHAが揃う。外部missing依存・宣伝画像だけ・編集元未提供では制作入力に選ばない。

Gate0は電話サイズの大形状/接触を先に検証する。通過してから材質・破断面・植生の美術仕上げを行う。仕上げても同じ全画面比較で改善を示せなければ採用しない。全画面→局所cropの順で見る。前日の簡易形状の修飾に戻らず、artistの設計したsourceを実際に評価する。

## 納品・試作から採用まで

- `.blend`と参照textureを相対pathで同梱、再open可能。Unity対応export（FBX/OBJ等）と、Unity6000.3.25f1/既存URPでの材質移行情報。新規pluginや有料shaderを暗黙に要求しない。pipeline変更が必要なら実装前に影響を示す。
- 同じsetup/Guard/Heal/Counter/Death/両比較の7場面。静止画で成立後、runtimeの流れとevent順を動画でも確認する。preview絵やrenderだけの納品をゲーム内の合格にしない。
- 主変数をこの代表領域に限定してCodexがimport→実描画→原画/CONTROL比較→独立画像review→修正→再検証する。改善した成果だけを保持候補へ進め、full Edit/Play、両fixed hash、4skin/SceneとCore境界を再照合する。
- 事前の未根拠なpoly上限やfps目標を新受入条件にしない。実際のmesh/material/texture/draw負荷を記録し、既存mobile検証で副作用を調べる。Simulatorと物理iPhoneを区別する。

## 権利・費用・privacyで確認する内容

出典/作者/制作方法、商用mobile利用、改変・組込み、Codexを含むチーム/複数環境での利用、原本と派生sourceの再配布・public GitHub掲載可否、依存textureの権利を候補ごとに確認する。購入前の記載が足りなければ保留にする。public原本不可のsourceはGitHubへのcommitを前提にしない。

artistに依頼する場合は**1領域だけ**の納品物、修正範囲、見積、権利、守秘、承認が必要な参照素材を具体化する。現在、依頼先への連絡0・見積取得0・発注0・支出0。原画/private screenshot/skinを無断で送らず、初期相談には匿名化した幾何/接触仕様を使える。private参照へのアクセスを求める場合は対象・相手・用途を確定してHuman判断を得る。

## 参考履歴：Human禁止前の有料候補調査（全件対象外）

価格は2026-10-20の公開表示、税/為替/手数料別。下表は権利やfileが最終確認済みの購入推奨ではない。publisherの「Unity対応」「game ready」は今回環境での成功証拠ではない。

| 候補 | 確認できた内容 | このpilotへの不足と現時点の判定 |
| --- | --- | --- |
| [Sky Harbor Volume01 / Mehranita](https://superhivemarket.com/products/sky-harbor--volume-01-stylized-floating-cliffs) | 表示USD6。11 cliff・6 vegetation・3 material、Blender5.0–5.2/UV/normal/texture。Chromeの実ページとhero画像を実閲覧し、大きな不揃いの塊・張り出しと側面の植物を確認した。[Creative Commonsリンク先](https://superhivemarket.com/page/creative-commons-license)はCC-BY4.0を説明。 | 公開heroは褐色の丸岩の集積で、原画の石灰岩/遺跡とのstyle適合は未検証。水路/lip、実際のnative file一覧、package内のlicense/dependencyは未確認。公開FAQ/docsでも解消しない。**構造候補として保留**、購入/原画送信なし。CC0ではない。 |
| [Stone Ruins — Stylized Modular Kit / 3D Tudor](https://3dtudor.itch.io/stylized-stone-ruins-kit) | 表示USD24.99。native Modelling.blend、FBX/textures、GLB、UEprojectの一覧あり。単一購入creatorの商用完成作品利用と原本再配布制限が記載される。AI-assistedの開示はText。 | 石造建築kitで、崖/水/蔦の一体sourceの証拠はない。native依存チェックはBlender5.1.2とのpublisher申告でUnity6.3では未確認。公開画像の実視認未実施。原本公開/チーム条件は追加確認が必要。**建築部の候補まで**。 |
| [Stylized Ruins / LapaModels](https://www.artstation.com/marketplace/p/PXoK7/stylized-ruins) | 表示USD7.99。Blender等format、painterly textureを掲げるが、Generative AI outputありとも開示。Standard商用1projectのsales/views制限が表示される。 | 宣伝上の手作業表現を作者/provenance証明と扱わない。実mesh/textureの出典、統合崖/水、public source権、Unityの固定画面は未確認。**現段階で購入を勧めない**。 |
| [STYLIZED Fantasy Ruins / PHIDEAS](https://assetstore.unity.com/packages/3d/environments/fantasy/stylized-fantasy-ruins-363224) | 閲覧地域の表示EUR64.40税別、Unity2022.3.62でURP等対応表、Standard Asset Store EULA。 | editable `.blend`提供や今回の電話画面、Unity6000.3.25f1実動の証拠なし。license type表示にもSingle Entity/Extension Assetが混在し確定できない。**今回のsource候補として保留**。 |

現時点で「買えば高品質になる」と判断できる候補は0。選定の問いは「格好よい岩が何個あるか」から「この接触位置で一体の構造を成立させる編集sourceがあるか」へ変える。予算が低くても、原本/権利/統合の不足が解決しないまま購入しない。1領域のartist sourceを優先する提案は維持するが、実際の依頼先・見積・参照権限が揃うまで発注段階ではない。

## 環境と証拠の継続

2026-10-20に旧 `/private/tmp/rogue-hq-ref/` の`.git`接続とprivate skin等の一部が失われていることを実確認。Gitのworktree一覧でも旧checkoutはprunable。旧ディレクトリを消したり上書きせず、同じhead branchを許可済みの安定workspaceへ新規checkoutした。履歴書換えやforce pushはしていない。

前日のprivate比較/Blender source等は残っていたため、167ファイルをcanonicalの既存ignore対象 `_userwork/hq-evidence/2026-10-19-integrated-rim/` へ保全し、全コピーのSHA一致をassert。旧tempも保持した。元のsource/画像SHAは変わらず、WebGPTの外部画素共有が成立したわけではない。public GitHubにprivate媒体は置かない。canonicalのユーザー`.gitignore`差分を保持し、今回の作業へ混ぜない。

上記の有料調査時点は制作条件とread-only調査/証拠保全のみでruntime変更なしだった。現在の無料pilotとScene参照復旧の新規結果は冒頭に分離した。前日のEdit14/14・Play9/9を新規実行に読み替えない。新Simulator/TestFlight/実iPhone/Human品質PASSなし、採用0・R2 V0 FAIL。
