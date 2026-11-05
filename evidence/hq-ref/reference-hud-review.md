# 原画のパーツと意味を実画面へ — 2026-10-25

最新状態（2026-10-26）：実行source **`5f67629b9952defece734a6bdab32eaeaf833932`**、同Draft PR21のtask-owned実装。右上現在階・文字整列・自動中心UI・開始部屋の南西実床/2室接続を保持。Codexが実装/観測/採否、3人のAstraが実画像/sourceの限定助言。最終Unity6000.3.25f1は **Edit21/21・Play12/13・探索4/4**。元の全sprite quad重複検査だけstep0 X/C FAIL、assertion不変。両full固定hash、G1/承認4PNG/51凍結ファイル一致、63新390×844画像。最終Simulator export/build各0、設定4件復元・unexpected0。最終native dd892e63の実操作で長文メモ→方針変更→自動Attack勝利3人生存/fullhash→保存記録→下へ/上へ→再試行まで確認。旧native403でメモ消失を実検出してから修正した。全検査greenとは報告しない。R2 V0 FAIL / Human品質・実iPhone未検証。

以下は各試行時点の履歴。途中のpending記述を現在の成功/失敗へ読み替えない。

Humanは原画全要素の読解を承認し「ではしっかり作れ」と指示。床の単調さは今回の優先外。根本仮説に沿う提案・実験をCodex/Astraへ許可したが、提案をfrozen gameplay仕様とは扱わない。

| 原画の部品 | 制作した表示と事実の出所 |
|---|---|
| 紋章と場所名 | locally authored vector紋章・小さな場所plate。天空遺跡scene名 |
| 地図と虫眼鏡 | 発見した床/通路・表示済み生存者と実際に開く拡大地図。Core knownと表示位置/死亡 |
| 右の階層進行 | 右上縦軸と現在階一節点。準備/戦闘/探索は別表示、発見済数は地図。架空の階番号/未知階は表示しない |
| 四人の顔カード | unchanged approved RGBA、本人名、表示済HP/最大HP、Core仮役、戦闘不能 |
| 役割記号 | シロ反撃・ミャル回復・ガル連携・モモ方針。原画装備から能力を発明しない |
| 能力らしい下部図柄 | 方針/開始/停止再開/記録/比較/再試行/探索にvector図柄。8能力menuや直接commandなし |
| 前景の暗化 | fullCanvas景観を残す透明gradientと独立したframe |
| 予想から理解へ | 記録冒頭に今回方針・本人の予想、既に見た直近出来事。自動採点/未来結果なし |

原画の12F/15F/Lv12/MP/5敵/宝箱報酬は対応Coreがないので捏造しない。既存の旗/灯り/花蔦/箱/石壁・arch/水/遠景を保持。今回の環境部品追加や美術完成の主張ではない。

## 実画像から修復

Aはfiltered Edit7/7・Play4/4でもHP数値/役割が高さ不足で消失し、白い案内が空へ埋もれた。rootは実4画像で採用を保留し、文字高さ/contrast/地図button/前景gradientを修正。BはEdit7/7・Play3/4で、新入力間隔testの単位がworld値だった誤りを保存。実画像ではHP/役割が復帰したがNormal fontのタイトル欄が不足して消失。screen pixel間隔・preferredHeight回帰・タイトル高さを修正した。assertion削除ではなく意図した単位へ修復。

AstraはA4枚・C6枚を実閲覧。rootが実装/修正/採否を所有する。方針の選択を文字/frameで示し、開始を明示、人物固有色を維持して行動状態と混同しないよう修復。Cのroot6画像でもタイトル/HP/役割/方針復帰と同じfield/人物倍率を確認。原画の場所→地図→仲間→操作の対応が明確になった局所改善として保持推奨。ただし地図modal下の開始操作と、選択後の守る対象名消失を次のDで修復する。

## 検証の範囲

Core全体・固定2hash・元5×5/初期6体/2敵・6室探索・approved4PNG・camera/cast寸法は変更しない。policy/予想/IME/実EventSystemhit/停止再開/比較/再試行、既知限定地図/全6室移動を回帰。C全Edit21/21・Play12/13、新探索4/4。元strict全quad矩形分離1件はFAILのまま。C Simulatorはexport/build0、生成設定復元/guard不変までで、実install/操作証拠にはしない。Dの最終結果は検証後に追記する。

R2 V0 FAIL、Humanの可愛い/見やすい/楽しいと実iPhone未検証。private原画/skin/PNG/動画はowner-local `_userwork/hq-evidence/2026-10-25-reference-hud/`。publicはコード/text/hash/XML。local pathだけでWebGPT別sessionへ画素を共有したとはしない。課金/委託/private新外部送信/TestFlight upload/merge/Readyなし。

Dは地図の背後入力遮断・対象名・予想記録の新検査を通過したが、既存の同frame Close→Reset操作で幕が残る新回帰を検出（Edit21/21・Play11/13）。待機追加やassertion緩和ではなく、Closeで幕も同期解除する修復を行いEで全再検証する。Dのnative buildは新回帰gateで停止し未実施。

EはEdit21/21・Play12/13でClose→即再試行を修復、残りは元quad検査のみ。52実画像のうちrootは4枚、Astraも最終差分4枚を実閲覧し対象名と本人予想の表示を保持可とした。新native7e96bddcをQA iOS27.0へinstall/launchし、初期4顔/HP/役割、地図と背後Start遮断、Close→実Start、戦闘の時間経過を観測。ただし地図で停止後、Playが一時停止のまま残る表示不良を実画像で検出。Fでmap停止時Refreshと実入力回帰を追加。記録の開発情報を別設定へ移し、本人名と実ルール・方針/予想/表示済captionを通常記録にまとめる。役割表示はCore仮役のまま、queued planによる先行動作名は使わない。E nativeの途中観測をFへ転用しない。

FはEdit21/21・Play11/13。nativeで見つけたmap停止後の再開label回帰は通過。新settings操作testはJournalを開く同frameの描画前に押してFieldへ落ちた。実journal画像で設定buttonは表示済みなので、Gでは実描画frame後の同じ厳密raycastへ修復し、旧Close→即Resetには待機を足さない。新journalをAstraが実1枚閲覧し、予想→仲間へ託す→結果比較→別方針が読み取れると評価。探索説明末尾のscrollとnative操作は未確認のまま。

GはEdit21/21・Play12/13で新map停止label/settings実hitを通過。新native78b4c4d2を実install/launch、通常記録の本人名ルールと設定分離は表示されたが、CUA drag/scroll・native swipe後も本文末尾へ移動せず閲覧不良を保存。Hで本文をraycast targetにしviewport mask内のoverflowを維持、実raycastからScrollRectへdragが届きcontent位置が変わる回帰を追加。探索中の記録もpause/保持と再開を検査し、Playの図柄を停止状態では三角へ同期する。nativeの未観測事項やGの成功をHへ転用しない。

Hは全Edit21/21・Play12/13（元A/B quad重なりのみ）。記録のevent-handler経由drag・探索記録の停止/保持・再開図柄はEditorで通過したが、新native d97a17bcでも実drag/wheel/native gestureで本文が動かなかった。手動ExecuteEventsの通過を実native入力の証拠へ読み替えない。Iではrecord/resultのみに上へ/下への実hit操作を加え、全文への代替到達を再検証する。地図/設定では不要なpage操作を表示しない。

## Human追加指示：階層表示と文字余白を再設計

原画の右上階層表示を取り入れ、文字上下余白を全画面で洗い直すよう明示指示。root/Astraが原画・同room3のbefore/after・overviewを実閲覧。以下は未検証の修正対象で、達成評価ではない。

- タイトル/案内/到着captionの上寄せと、ボタン/カードの中央寄せが混在。特に探索到着captionの下が空き、隣の再試行と重心が不揃い。見出し・通常情報・補足の3段階、共通の行高/垂直整列へ揃える。
- 右上の状態箱では原画の縦軸/現在地強調を失っている。現在階の節点一つと金の強調を制作し、階番号の正本がないため「現在階」と表示する。架空の上下階/総階数/階移動は作らない。準備/戦闘/探索は別表示、発見済室数は地図へ移す。
- カード名前/役割/HPが下側に詰まり、特にモモの長い方針が窮屈。文字とバーの行を分け、カード内の余白と状態/操作の間隔を揃える。長文だけ縮小してごまかさない。
- 世界とカード間の広い空白・盤上人物の小ささ・環境造形と原画部品の描写差は残るR2課題。今回HUD整列をこれらの達成へ換算しない。

Astraの独立助言も同じ3優先。検証中Iのsourceは変更せず、完了後の次実験で対応する。

Iはsandbox起動時のreadonly DBで設定処理前に停止しテスト未実行、owned batch Unity終了後exit241。通知も3回失敗していた。rootが待機扱いを続け、環境不良の診断が遅れた。再設計を統合したlayout Aは通常Mac権限で起動したが、ライセンスIPCで180秒timeout、新画像/試験0。LicenseClientログはglobal mutex取得失敗を示し、既知I由来の補助プロセスが残存していた。PIDとcommandを確認しその補助プロセスだけを終了、認証/ライセンスファイルを変更せず同sourceをlayout Bで再試験。環境失敗を造形/UIの採否やテスト成功へ変換しない。

layout BではConfigure0・Edit21/21・Play11/13、40新描画。rootはoverview/同room3/方針/page後記録/移動中記録の5画像を実閲覧し、右の現在階一節点・発見済数の地図移動・カード行分離・到着captionの垂直整列を確認。承認顔のviewport寸法/UVは旧比率を維持。既存quad FAILに加え、旧HQ sceneの行動順が拡大した案内行の下へ約2px不足する新回帰を原検査が検出。次に下げて修復しassertionは維持する。短いresultにも無効なpage操作が出る欠陥も実画像で確認し、overflow時だけ出す修復を行う。native実操作や最終採用はまだ未確認。

Astraはbefore2枚/after3枚を実閲覧し、行分離と到着文の整列は保持可としたが、金の現在階節点がアーチの上を隠し、探索のガル頭上と競合すると指摘。rootも同画像で根拠を確認。Cではmarker/axisを右端側へ移し上げ、captionを左に置く。元initiative assertionを維持して案内行から下げ、長い記録のpage到達を残し短いresultでは隠す。結果未確認のまま美術達成とは書かない。

## Humanの自動進行/南西拡張指示と3人のAstra助言

Humanは少しずつの改善を認めたうえで、下/左にも実mapを広げて角地だけで評価しないこと、自動がゲームの売りであること、結果/事前予測の認知負荷低減を明示。3人の独立Astraをadviserとして実使用し、rootが統合・実装を担当する。各自は実画像/該当sourceを閲覧、編集や外部画像送信はしない。

- 自動RPG動機担当：overview/choice/room3を閲覧。準備1tapで任せる、戦闘中は任意pause、終了後は探索が主操作。方針/メモ/比較を各turnの課題にしない。
- 認知負荷担当：overview/choice/page後journal/探索中recordを閲覧。未入力の予想を表示しない、通常recordと比較の入口を安定させ、実測の勝敗/生還を短く見せる。自動で未知の行先や次戦闘を決める変更は含めない。
- 美術構図担当：overview/room3とFloorMap/Scenery/Explorationを閲覧。開始部屋(0,0)と全6室が正座標側で東北出口だけという実際のcorner biasを特定。負座標の実部屋/通路、南西door apron、負値剰余によるmesh indexへの対処が必要。

rootはCの検証sourceを保ち、南西2室/接続/loopと4方向の実出入口を追加する変更を別実験として準備。G1 fixtureは固定し、未知室を初期地図に出さず、負座標まで実往復してから自動中心UIを比較する。助言のfun仮説をHuman試遊や品質合格へ読み替えない。

CはEdit21/21・Play12/13、元quad B/A以外の新回帰を解消し57新描画、native export/build0。rootはoverview/同room3/短いresultを実閲覧し、現在階節点がアーチ/ガルから離れたことと、短いresultの不要なpage非表示を確認。C nativeのinstall/実操作へ読み替えない。Human新指示の南西拡張・自動中心UIはこれからの別sourceとして扱う。

南西Aは対象Edit7/7・Play4/4、32描画、source/保護bytes不変。rootは開始overview、西room6、南room7、room0への帰還を実閲覧。8室往復・負座標mesh/known-only・倍率/HP/hashは通過したが、開始画面は新通路先端の露出に留まり、角地印象を十分変えていないと判断。探索Room0だけを負座標側に延長し実床/支持体を描く次候補を準備する。G1戦闘の5x5合法域/fixtureは変更しない。これを地形試験の成功から全画面品質合格に読み替えない。


南西BはEdit7/7・Play4/4、32新描画、source/保護bytes不変。開始Room0を(-3,-3)〜(4,4)へ広げ、rootは開始/西room6/南room7を、Astra美術adviserは旧overviewとの計4枚を実閲覧。同じ人物倍率で床が左下へ画面外へ続く改善を確認した。一方、旧縁石/滝/崖と新床が段差や黒い隙間に見える接続不良を双方が検出。広さの範囲は次候補として保つが美術接合は採用しない。移動先room6では地形側面が透過カードの下に見え、文字背景も不均一。地形を縮めず接合/カード背景を次段で修復する。

自動中心UIを統合しautonomy A pilotを開始。一つの「仲間に任せて戦う」から既存連続戦闘へ進み、方針はモモcard、任意メモは事前の記録内、勝敗/生還は短く、終了後は探索を主操作とする。比較/再試行は記録内へ残す。120文字メモの保存/実pageボタン/再試行と、入力なしの自動戦闘を実raycastで確認する対象。現在は未検証で、成功やfunを先取りしない。

autonomy AはEdit7/7・Play2/4、31新画像・保護/source不変。rootが開始/勝利/任意メモの3実画像を確認し、単一の開始操作から14行動4人生還と固定Protect hash、探索を主操作にする表示を観測。新比較/再試行testは記録を開いた同frameの次操作がFieldへ落ちて失敗。未描画のbuttonを機械的に連続tapする新testに限り描画後の実raycastへ変更し、旧Close→即Reset回帰は不変。比較/paging/retry到達をまだ成功とは書かない。

認知負荷のAstra adviserも旧overview/新開始/勝利/任意メモの実4枚を閲覧。並列の方針/開始から一度任せる導線への改善と、未入力や比較を課題にしない方向を支持した。ただしモモcardの操作可能性と「今の方針」が行動保証に読める文言は改善余地。停止/比較/retryの到達性・native・fun合格は未判定。

autonomy Bでは、探索版だけ元島の南西低所縁石/崖/水をruntime meshで整理し、元Courtyardの境界を実通行tileへつなぐ。床支持体の上端も石床の継ぎ目より下へ修正。輸入OBJ/承認画像/単独G1 sceneは書換えない。初期6体/戦闘Core/カメラ倍率を固定し、同じ開始画面と8室往復の検証を再実行中。接合が改善したかは実画像でこれから判断する。

BはEdit7/7・Play4/4、35新画像/source保護不変で完了。rootは開始、通常記録、page後比較、西room6、開始部屋帰還の5枚を実閲覧し、初期の左下接続上にあった縁石/滝の障害物状輪郭の解消と実8室往復を確認した。右外側の崖端面と反復床の造形不足は残る。記録比較/実page/再試行のraycastも通過したが、120文字の保存メモ本文がpageボタンの後ろまで伸びる欠陥を実画像で発見。本文viewportがpage領域へ入らない上下関係を回帰に追加し修復。背景の地形濃淡がカード/記録pane内へ強く見えるため、この局所paneだけ不透明にし全画面を覆う帯は作らない。

Astraの文言助言を受け、「今の方針：モモが」を「モモの方針：」へ変更し行動保証との混同を減らす。戦闘前のモモ名欄に「方針」を添え実操作入口を示し、開始後は名前だけに戻して方針操作を無効にする。全suiteとfresh native buildの結果は未確認。fun/自動の面白さ/R2の品質合格をテストから推定しない。

最初の全suiteはEdit21/21・Play11/13、55新画像・source保護不変。元quad FAILに加え、新しい「モモ・方針」の表示に対してrootが旧「モモ」の期待値を残した検査が失敗し、native buildをgateで停止した。ラベルの横幅/高さは実overviewでも保持されるが、検査期待値の更新漏れはrootの不備として記録する。他の本人名を含む厳密一致検査、元quad検査は維持し、戦闘前の新入口ラベルだけを仕様へ揃えて全再実行する。

rootが新overview/同room6を実比較し、以前はカードの役割/名前の後ろに見えた斜めの地形模様が消失したことを確認。不透明化はカード/記録pane内だけで、field全体へ帯は追加しない。B/新fullの同5背景座標について初期と西部屋のRGB差も保存し、文章だけの見やすさ判断を補助する。これは全画面品質や人の遊びやすさの証明ではない。

再実行autonomy finalはEdit21/21・Play12/13、新探索4/4。元quad矩形重なりだけstep0 X/CでFAIL、assertion緩和なし。62新画像、source/保護bytes不変。rootは長文保存メモのpage後比較、通常記録、方針画面を実閲覧し、本文/pageボタンの領域分離・背景地形の透け解消・任意メモを要求しない方針選択を確認した。保存120文字/厳密raycast page/記録内Retry/停止再開/IME/全8室往復/固定G1 hashesもこのsourceで通過。新native export/buildへ進行中で、旧binaryの操作結果を転用しない。

同候補のnative export/buildは各0、生成4設定を復元/unexpected0/source保護不変。binary403287fe02dd2a281f0083bbd7de679fa8c56bb07b7a86b67519253d3206a993をQA iOS27.0へ実install/launch。rootの実操作で、初期6HP/現在階/名前/方針入口、地図の背後Start遮断、メモなしStart→Protect4人生還/固定hash、任意記録と短い比較（未入力表示なし）、同位置HPから探索→南西追加床の指定/追従/到着、記録からRetry→初期状態復元を確認。Attackも停止中HP12/人物位置保持→再開→3人生還/もう一つの固定hashを新runtime logで確認。短すぎるCUA clickでは入力が届かない場合があり、実touchを180ms保持して到達を再観測した。

任意メモの日本語かな「か」composition→候補「かつ」選択→確定/キーボードdismiss/欄復帰は実nativeで確認した。ただしその後にモモ方針を変えると、旧Select処理のPrediction.text消去が残っており、メモが保存trialから失われた。rootは実記録で消失を検出し、IME入力の成功をメモ保存成功へ読み替えずFAILを保存。新自動UIに限って方針変更時の任意メモを保持し、記録入力→実モモcard→Attack/Protect切替→記録へ戻る→Start→保存比較の回帰を追加した。legacy G1方針切替と明示Retryの消去動作は維持。全suite/fresh native再検証をautonomy-memo-fixで開始し、修復成功・最終pushはまだ未完。

録画live-flow.mp4は実操作を含むが、中断中も録画が継続し38821.421667秒/5925921213bytesになった。全時間を観測したとはせず、ライブで見た操作区間だけを証拠とする。原本をprivate保持し、有限の先頭/末尾excerptとhashを別生成する。今後の記録はこの長時間ファイルを「短い動作clip」と呼ばない。実iPhone/Human判断・新TestFlight uploadなし。

メモ保持修正版はEditorの全Edit21/21・Play12/13、floor4/4を通過（旧quad X/CだけFAIL）。63新画像/source保護不変。rootは方針変更後の記録入力欄と、戦闘後の保存比較/page後を実閲覧し、メモ保持の回帰を確認した。public raw XMLをこの最新sourceの結果へ更新。新native buildの完了/実操作とsource commitへの結合はまだ未完。


## 最終native修復の再検証・採否

最終binary SHA256 `dd892e63ed17811064ee47472e2cb3b16f5232bb4b8ddf9d63441dc23dea387b` をQA iOS27.0 Simulatorで実観測。入力転送offのpasteは到達せず、Capture Keyboardを一時onにして実キーを入力、長文確定後offへ復元した。メモ入力後にモモcardからAttackへ変更して記録を再度開くとメモが残り、一回の開始で勝利3人生存となった。fresh runtime.logのAttack fullhashは `85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e` と一致。Exception行なし。通常記録にも保存メモが表示され、戦いの記録の「下へ」で最後の`EN`を含む行まで到達、「上へ」で先頭へ復帰した。本文はpage操作と重ならない。上端に途中行が切れる軽微な課題は残る。実「もう一度試す」で6体/初期HP/準備へ戻った。

dd892でProtect・全8室・移動中pause・かな候補を再実施したとはしない。前の403 binaryではProtect/Attack両固定hash、pause保持/再開、南西開始部屋移動、かな候補かつ確定を実観測したが、その時点ではメモ消失があった。Editor全8室/両条件/120文字memo検査とnativeの観測範囲を分離する。

原画/before/afterの実比較とAstra助言に基づき、現在階と統一行・一回任せる主操作・任意記録・南西連続床・不透過card背景は局所採用。5同位置のカード背景RGB差は地形依存22〜28から0。右の板状崖/継ぎ目、全sprite矩形重複、反復する大床、全画面の原画水準は未解決。**R2 V0 FAIL**を維持し、Humanの楽しさ判断を代行しない。

private証拠はowner-local `_userwork/hq-evidence/2026-10-25-reference-hud/`。最新Editor `autonomy-memo-fix/images/`、native `autonomy-native-memo-fix-observation/` に初期/変更後メモ/保存記録/page前後とruntime.logを保持。旧native動画は `autonomy-native-observation/live-flow.mp4`、38821.421667秒/5925921213bytesで長い無操作区間を含み、全時間/全frameを観測したとはしない。有限抜粋2本とSHAはJSONに記録。媒体jobは終了0だが通知3回失敗、再開時recoverで読んでacknowledge済み。公開証拠はtext/hash/XMLのみ、別sessionへのprivate画素共有経路は未成立。新TestFlight/実iPhone/外部送信/課金/委託/merge/Readyなし。
