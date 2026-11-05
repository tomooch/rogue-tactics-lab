# 統合外周 Gate0 — 2026-10-19

**Gate0 A/B FAIL、採用0、BLOCKED_ART_PRODUCTION、R2 V0 FAIL。** [Humanが支持した独立レビュー5566270401](https://github.com/tomooch/rogue-tactics-lab/pull/21#pullrequestreview-5566270401)の次実験を実行した。今回は崖だけを変える方法を止め、cap・崖・水源/lip・蔦の支持を同時に扱った。しかし実Unity390×844で、一体の岩盤として読める輪郭と接触関係を示せなかった。細部sculptへ進めず、このblockoutから詳細化する経路を棄却した。これは全3D制作の不可能性やHumanの品質判断ではない。

開始/復元HEAD `7cb7f0c6804881e3446f57f2311eb5609555c8e8`、branch `codex/hq-ref-vertical-slice`、base `codex/mobile-visual-g1`、Draft維持を最新GitHubで確認。保持runtime `fc005d4ddee3edc5ac5b035ecfefb5e132f5e25a`。accepted mainとpending PRを混同しない。

## 実験と実観測

主変数は、前左の非プレイ領域における**統合した外周の可視シルエットと接続構造**。変更範囲はlocal x[-6.7,-0.5]、y[-4.2,0.15]、z[-11.4,-9.3]。Core/5×5/初期配置/2敵/承認4skin/床/Courtyard/カメラ/照明/HUD/表示eventは固定。

Blender5.2.2 LTS、hash d13f752e3b9cでoriginalな簡易sourceを制作・保存・再読込し7層OBJへexportした。今回はcode-directedなGate0簡易形状であり、新規GUI sculptや完成石灰岩の制作とはしない。前回のGUI edit/Undo/保存/reopenの証拠はhistoricalで、今回の視覚PASSに転用しない。原画/skinの画素や外部モデルは制作入力に使っていない。

CONTROL/A/BのそれぞれについてUnity6000.3.25f1でsetup、Protect Guard、Heal、Attack Counter、Death、Protect比較、両条件比較の7場面を新規描画した。各画像は390×844の生表示、原画は縦横比を保って幅390px。PRIMARY/Mac原画のRGB一致は[既存独立検証](https://github.com/tomooch/rogue-tactics-lab/issues/20#issuecomment-6203653575)に従う。ambient animationの位相はpixel時刻まで固定していないため差分pixel数を品質scoreにしていない。今回の新規媒体は静止画であり、候補のmotion/native動画を観測したとはしない。

| 段階 | 実変更 | 実画像での観察と判定 |
| --- | --- | --- |
| A | HeroLedgeのcapを後退し、非対称の中段と支持塊、側方の欠け、水路→lip→fall、capから下がる蔦支持マーカーを7層の編集可能meshで構成 | 既存Limestoneの前縁が残り、新cap・水源・崖の前を横断。二重の縁として読み取れ、意図した中段とノッチは見えない。蔦は緑の細線。**FAIL**。詳細を増やさず遮蔽の統合処理を修正。 |
| B | Aの7層形状を維持し、重複する旧Limestone/Cliffを許可AABB内だけで差し引く。境界を横切る面はhalfspace clippingし外側の元XYZ/RGB/UV/normalを保持 | Aより遮蔽が減り崖面の一部は露出。しかし横長の灰色帯と吊り下がる角板、空色の歯状スロットに見える。下面・後退支持面・側方開口が一体の侵食形状として読めない。水の上端はlipの後ろで、水路→落下を追えない。蔦は支持された量感でなく縦マーカー。**FAIL**。この原型の詳細sculptは停止。 |

BのAABB内差引きはLimestone145面・Cliff31面が交差し、外側の未交差triangleは元payloadを保持した。床・キャラ位置を削ったり移したりして露出を作っていない。Bでもrootと独立local画像reviewerが全7場面を確認し、一体棚/開口ノッチ/水・蔦の支持接触を認めなかった。4人・HP・event receipt・下部操作UIに大きな新規遮蔽や切れは観測していないが、主役性や全画面の魅力の進歩も示せなかった。比較overlayで環境が隠れる2場面を石材品質の証拠として数えない。Death captureは敵の戦闘不能を含む中間stageで、最終A死亡と同一画像ではない。

前回のbuild4/build5/pass3/pass4/longrun比較は既存記録を参照する。今回の直接差分は同source・同条件のfresh CONTROL→A→Bであり、古い動画を新候補の実行証拠に転用していない。

## 独立レビューと制作不足

Astraは実native `gpt-6-astra` / highへ2回相談した。**すべてsanitized text-onlyで、private画像/動画/mesh/pathは渡していない。Astraは画素を見ていない。** 初期は後退cap・非対称棚・側方欠け・水/蔦の連続支持を提案。A後は「遮蔽除去だけではGate0に足りず、支持された一体の輪郭、下面と後退面、連続する水/蔦が電話で読めなければ詳細化しない」と指摘した。

別のlocal reviewerはA/Bそれぞれ7場面と比較画像を実際に閲覧し、両案をFAILとした。rootはAのsetup/Guard/Counterと原画、Bの全7組を実際に閲覧して照合した。Astraの助言をpixelレビューに偽装しない。初回の露出/contact sketchもcapとchannel bedを省いた投影図であり、Unity候補や正確なobject maskではない。

不足はBlender未導入やimport不能ではない。既存構図内で、capから張り出し・支持塊への大きな形を設計し、空の抜けと水/植物の接触を同時に読める**美術sourceへ落とす制作能力/原型**がまだ不足している。A→Bの遮蔽修復でこの不足を切り分けた。色、細線、密度、三角形数、ブラシ値の追加へ戻らない。

## 次の制作入力と境界

以下は調査した選択肢であり、購入・download・外部連絡・新方式の採用は行っていない。調査日2026-10-19。価格は表示時点、税・手数料・適用条件は取得時に再確認する。

| 選択肢 | 費用・編集性・権利・privacy・適合の限界 |
| --- | --- |
| 権利を持つartistの**一体の外周モジュールsource**を使用/限定制作 | 推奨する入力。単なる単石より、cap/張り出し/後退支持塊/水路/lip/植生根元を別objectで持つ`.blend`、同じ固定cameraの390px preview、UV/normalとUnity向けexportが必要。既所有sourceなら新規購入不要、制作依頼は未見積・連絡0。商用mobile改変、作者/provenance、チーム利用、source公開可否、守秘を確認し、発注/原画アクセスには別Human GO。匿名化した幾何条件のみのbriefから始める選択も可能。 |
| [Poly Haven Rock Moss Set 01](https://polyhaven.com/a/rock_moss_set_01) | 公式にBlend/glTF/FBX等、約63K trisの石6種。無料・[CC0](https://polyhaven.com/license)、[signup/paywall不要](https://polyhaven.com/about-contact)。取得しても原画のuploadは不要。ただしリアルな苔石で、石灰岩の遺跡外周・原画styleへの適合は未検証。**これを入れれば解決するとはしない**。素材評価の候補であり未取得。 |
| [Gonzalo Tapia: Stylized Rock High & Low Poly](https://www.artstation.com/marketplace/p/8Nnyy/stylized-rock-high-low-poly) | 表示USD1、`.blend`とhigh/low mesh・UV/normalあり、full textureは含まれない旨。小額でも購入は未承認・支出0。[Standard契約](https://www.artstation.com/marketplace-product-eula)の商用1作品・sales/views制限やraw assetの公開制限があり、public GitHubへsourceを置く前提に使えない。石1種で統合外周は未解決。Quixel追加textureの取得/使用を許可済みと扱わない。private原画の送信は不要。 |
| 非interactive環境のみhybrid 2.5D/3D | ローカルBlenderで固定camera用の奥行きmesh/描画layerと3Dの水・接触影を組み合わせる案。Core/人物/床/HUDを変えず、前後遮蔽とイベント可読性のgateは維持する。追加ソフト費0でもart制作費/工数は未確定。原画をそのままゲーム背景に貼る承認ではなく、新しい視覚表現方式なので**Humanの選択が必要**。未実装。 |

次の提案はartistによる統合sourceを先に確保し、同じ1領域で再評価すること。正当に使用できる適合sourceがなければ、hybrid方式の狭いpilotをHumanが選ぶ。現時点で次の候補を勝手にdownload/購入/発注したり、採用方向を凍結したりしない。

## 復元・検証・証拠

A/BのOBJ、`.blend`、native Mesh、helper/meta、画像・比較HTML・raw logsはowner-local一時領域 `/private/tmp/rogue-hq-rim-integrated/` に保存した。公開は本文/数値/hash/XMLのみ。18個のtask-owned OBJ/native Meshと2個の一時helperを復元/撤去し、Unity/toolsは保持runtimeとbyte差分0、worktree差分0をassertした。canonical mainのユーザー`.gitignore`差分には触れていない。

CONTROL/A/B各filtered ProductFlowはPlay1/1、Edit0選択、両full frozen hashをassert。全14/9と区別する。Scene SHAと承認4skinの全SHAを照合、固定コード・Courtyard・camera/HUD/event sourceの一致もassertした。初回Blenderは結合後のremoved-object参照で失敗し修正。次のsandbox実行はMetal初期化SIGSEGV、通知はローカルDBアクセス拒否で失敗したためHuman再開時に保存ログから回収した。必要な権限で再実行し保存/export完了と通知を確認。検証準備ではGit lock制限によってharnessが作成されなかったが、最初のfull jobを誤って起動し、file未存在・exit2で停止した。準備失敗の依存順序を修正し、権限を解消→復元/byte一致assert→harness作成後に別jobで全テストを実行した。失敗を成功runに混ぜない。

**Fresh retained full gate: EditMode14/14、PlayMode9/9 PASS、両exit0。** Unity6000.3.25f1、2026-10-19 12:35:59–12:38:41 UTCの新プロセスで実行し、両full fixed hashをログと既存assertionsで確認した。表示cursor、pause/resume、prediction、比較/retry、death suppress、safe area/keyboardの既存全suiteを再検証。復元後も7場面を新規取得し、rootはsetup実画像で保持画面へ戻ったことを確認。Scene/承認4skin全SHA一致、Unity/tools tracked byte差分0・untracked source0をassert。公開XMLは`integrated-rim-editmode.xml`と`integrated-rim-playmode.xml`、実行条件/command/source/image SHAは`integrated-rim-evidence.json`。

Simulator/TestFlight/物理iPhone/新候補動画の実行は今回行っていない。保持runtimeの前回Simulator証拠は[longrun](longrun-review.md)にあるが、今回の新規native成功にはしない。Apple旧build5(d5e8e08)と保持runtimeを混同しない。実iPhone・Humanの可愛い/見やすい/楽しいはOPEN。

WebGPTが次回別sessionから確認できるのは本書、`integrated-rim-evidence.json`とfullテストXML。private画像・動画への既承認共有経路は確立していない。`/private/tmp/.../private-comparison.html`はMac内で開く比較資料で、外部通信なしに原画/CONTROL/A/B/7場面を埋め込む。ローカルpathをWebGPTが受領済みの画素共有と偽らない。新たな外部画像upload、課金、モデル公開、merge/Ready、G2/#13は行わない。
