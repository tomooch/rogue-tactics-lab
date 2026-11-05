# Transport UI — 2026-10-27

実行source `e12bbb013e5af88aeec28901f62eac01e558f0db`、開始PR HEAD `fbaf318b5b79e664e3a42217ef70bc9af2eb241c`。Humanの指示でフィールド造形は後回し。Terraが操作UIを実装し、rootが統合・実行・判定した。AstraはA/Bの実画像を閲覧した限定助言者。

前回Simulatorで停止を押そうとした時点で勝利後の全幅「遺跡を探索する」に切り替わっており、探索へ進んだ。押下の瞬間そのものは撮影していない。対策は、開始/探索の記号と文言を一つの中央揃えのまとまりとし、勝利後の探索を左に配置し、旧停止領域を操作ではない勝利・生還人数と透明な入力受けにする。盤面へのタップも通さない。新しい確認画面や待ち時間は設けない。

Aの390×844実描画は記号と文言のまとまり、探索/結果の分離を示したが、右側文字の背景との明暗差が弱かった。Bで結果を12pxと暗い縁取りに修正。rootとAstraは実画像を比較し、文字が読める局所改善と判断した。暗い縁取りの硬さは残る。静止画だけではタップ動作は証明しない。全画面の美術品質は未完成、R2 V0 FAILのまま。Humanの「可愛い・見やすい・楽しい」は未判定。

Unity 6000.3.25f1の入力テストでは実Input Systemの合成mouseを停止ボタン上で押したまま勝利を跨いで離し、探索に入らないことを確認。勝利後、旧停止範囲の左右端・中央で最前面の入力受けがButtonではなく、盤面/探索に渡らず状態とhashを変えない。結果文字の収まり、モーダル/再試行での表示解除も確認。これはEditor検証で、実機touchとは区別する。

全回帰はEditMode 21/21、PlayMode 12/13、探索4/4、exit 2。既知の `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults` のstep0全sprite矩形重複が失敗。前回はX/C、今回はB/Aが先に検出された。同一失敗座標とは扱わず、assertionは緩めていない。EditModeで両固定hash一致、Protect `e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d`、Attack `85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e`。全検査中232ファイル不変、63ソースの検証時SHAと実行commitの一致を確認。

Simulator専用iOS 27 QAに新binary SHA256 `3a315ae4dc0572c2b9cce26bdbf310a978425ceee9128487c28eca81dd97cf3f` をinstall/launch。Protectで一回開始→一時停止→HP/表示保持→再開→4人生還→旧停止位置の左/中央/右tapで勝利画面維持→探索buttonで探索へ進むことを実画面で観測。native Protect hash一致。勝利を跨ぐ押しっぱなしはEditor合成入力のみ。native Attack/IME/物理iPhone/Human試遊、新TestFlightは未検証。local build番号5はApple上の旧build5を更新しない。

private原画/skin/画像は公開Gitに含めない。実画像とlogはowner-local `_userwork/hq-evidence/2026-10-27-transport-ui/`、別sessionへ画像共有可能とは主張しない。公開するのは本記録・source SHA・個人homeパスを置換したテストXMLのみ。無関係なcanonical `.gitignore` は保持。
