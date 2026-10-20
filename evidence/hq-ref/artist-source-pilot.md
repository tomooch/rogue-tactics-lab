# 次の制作入力：統合外周1領域のartist-source pilot

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

## 公開一次情報の候補調査（未取得・未採用）

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

この更新は制作条件とread-only調査/証拠保全のみでruntime変更なし。前日のEdit14/14・Play9/9を新規実行に読み替えない。新Unity/Simulator/TestFlight/実iPhone/Human品質PASSなし、採用0・R2 V0 FAIL。次の実制作入力を選定してから同じ閉ループへ戻る。
