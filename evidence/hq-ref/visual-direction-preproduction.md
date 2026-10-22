# Visual Direction Preproduction — B SELECTED / 実装未再開

## VISUAL_DIRECTION_FREEZE — B / 2026-10-22

Humanは本チャットで選択質問へ **「B」** と回答した。B「天空遺跡と仲間」の390×844静止frame（SHA `9a318b8a2f1e1c6d4060b8f1caa4b7de124fc86fc73349ea79413764e6b09f76`）を全画面の視覚方向の基準とする。空・遠景・縦の遺跡・滝のcanvasと4人の視認性を両立する。Humanが理由を別途述べたとはしない。[Notion Decision Log](https://app.notion.com/p/3e3ad3888915816caa0bcca7c5128c4b)へ選択を記録。

選択画像のworld584px/stage308px/4人89px/roster80pxは再現用のreference値であり、今後の実装で可読性を犠牲にして固定する新gameplay仕様ではない。G1初期state/5×5/承認4人/2敵/実HP/既存操作とprivate境界を維持し、密集セルと動的遮蔽は未検証のriskとして残す。

これは構図方向の選択。背景合成/2D hybridの本番方式、Unity実装・R2美術品質・実機/Human funの合格ではない。今回の応答でUnity/Blender制作を再開しない。Draftを維持し、原画/元画像・watermark・SHAは書き換えない。以下は選択前の制作/検証記録として保持する。

[Human-GO packet](https://github.com/tomooch/rogue-tactics-lab/pull/21#issuecomment-6316622356)に従い、追加造形・Unity実装を止め、Mac内の承認原画・4skin・実Unity CONTROLで390×844静止全画面2案を制作した。開始PR HEAD62e2580f041e4b0c65bf71563b22b5844563e1cc/Draft、既存runtime f64e04cを保持。A/Bを承認済みdirectionとせず、R2全体V0 FAIL・Humanの可愛い/見やすい/楽しい未検証を維持する。

## 同じ状態と、使った正本

両方ともStart前の同一G1初期状態、ProtectAnchorを選択表示。5×5、Aシロ(2,2)HP18、Bモモ(1,2)HP16、Cガル(3,3)HP12、Dミャル(1,1)HP10、X苔の精(2,3)HP18、Y花の精(3,2)HP14。Core上のセル/HP/敵数を変更していない。未来の行動/Guard/Heal/攻撃範囲を描いていない。4人は同じ承認RGBAから余白だけを除き、全身に均一倍率でresample/alpha合成。顔・目・衣装を補描/色変更しない。敵2体は実retained Unity setupから輪郭を切り抜いたもの。

原画ローカルPNGは941×1672、PNG SHA99a76f33…・復号RGB SHAfd52b365…を確認。PRIMARYと同一RGBという判断を維持し、別画像へすり替えていない。保持版の実Unity setupとGuardも実閲覧。元画像のミニマップ/12F/スキル8個/余分な敵/宝箱をmockへ入れず、人物/UI/宝箱のない環境部分だけを監査して合成した。床の石面に原画由来のpaint断片を用いるが、新しい3D/production assetではない。

## Layout spec / 390×844

| 項目 | A 仲間主役 | B 天空遺跡と仲間 |
| --- | --- | --- |
| 最初に見るもの | 4人の表情・全身・戦況 | 高さと空の遺跡＋4人 |
| header | y0–98（上17pxはSTATIC表示） | y24–85を景色に重ねる（上17pxはSTATIC表示） |
| world canvas | y98–566、468px | y0–584、584px（headerと重なる） |
| 5×5 stage | y150–536、386px | y244–552、308px |
| 4人の全身alpha高さ | 全員104px、全身均一倍率 | 全員89px、全身均一倍率 |
| roster | y568–660、92px | y586–666、80px |
| policy | y668–712、44px | y674–718、44px |
| optional prediction | y720–764、44px | y726–770、44px |
| Start | y772–820、48px | y778–822、44px |

正確なfoot point/alpha bounds/元RGBA SHA/scale/論理セルは[数値specと証拠](visual-direction-preproduction.json)。4人のHPは同一素材の顔を使うrosterへ、敵18/14HPはそれぞれ自分の頭上へ配置。空の占有cell markが敵の論理セルを示し、表示上だけ23px(A)/32px(B)浮かせて仲間の背後に隠れるのを避けた。これはmock-onlyのpresentation提案で、Core位置の変更/新能力ではない。両案とも既存Protect/Attack、任意予想、Start、記録だけ。pause/resume/compare/retryは既存flowとして維持する必要があるが、今回の初期frameに未出現の操作を捏造しない。

Aは世界のpanoramaを減らして仲間とセルを大きくする。Bは空・遠景・縦の遺跡・滝のcanvasを増やし、人物とセルは小さくなる。UIを省略した美しいkey artとの比較ではなく、同じ操作枠/同じ状態でのhierarchy選択。

## 実画像レビューと限定修正

rootはPRIMARY vs 実Unityのprivate contact、実setup/Guard、A/B各全画面と780×844の並列画像を直接閲覧。最初の案は敵が仲間に隠れ、HPが髪へ掛かったため修正。固定論理セルをproposed cameraの表示座標へ写し、全25セルを画面内に収め、敵表示/HP位置を整理した。

中立local reviewer（gpt-6-luna/medium）はrootの好みを渡されず、原画・A/B・並列・修正後の最終2枚を実閲覧。最終版は6体/4人の顔/25セル/敵HP18・14を確認でき、架空floor/minimap/skillと実Unity誤認を招く重大違反は見当たらないと判断。Aはセル/人物を読みやすい一方で場所性が薄い、Bは環境の場所性を伝える一方で密集したセル/人物認識の負担が増す、と指摘。これはmock制約の点検であり品質合格やHuman選択ではない。

## DESIGN_RISK / 実装へ進む条件

- 背景/側面は原画部分の静止合成、石床はgraphic collage。画像の豊かさをUnity3D制作能力の成立としない。両画像へ **STATIC DESIGN STUDY — NOT UNITY RUNTIME** と「原画背景の静止合成・方向性未承認」を常時明示。
- Aは既存Sprite/Camera/roster/操作UIを近似配置する方向だが、動的なMove・Guard・Counter等の遮蔽/target読取と安全領域は再設計/実測が必要。Bはさらに遠近/空/浮遊遺跡/滝/植生の品質ある環境素材が必要で、その制作能力は未成立。既存画面でこの構図をそのまま作れるとは未証明。
- proposed stageの投影/絵のscale/浮かせ方はmockの視覚仮説。現行カメラの実挙動とは異なり、将来一つのUnity cameraで近似できる範囲も要検証。合成背景/2D hybridを本番採用する承認はない。
- active frame/動画/新Unity/Blender/Simulator/TestFlight/実iPhoneは今回実行していない。optional active案も作らず、headline2枚と並列比較1枚で停止。

## private成果物・再現性・表示の限界

owner-local durable ignore配下 `_userwork/hq-evidence/2026-10-21-visual-direction/` に保存。

- `A-cast-first.png` / `B-scenic-diorama.png`：各390×844、最終案はこの2枚のみ。
- `A-vs-B.png`：780×844、両案を原寸のまま並列、伸長/cropなし。
- `A-cast-first.ora` / `B-scenic-diorama.ora`：1170×2532、各11編集layer。layerを再合成したpixelsがmergedimageと完全一致し、その390×844縮小結果が最終PNG全画素と一致することをassert。
- `compose_studies.py` / `inputs.json` / `layout-spec.json` / `qa.json`：入力SHA/構図座標/alpha uniform scale/出力SHA/再現手順。`primary-vs-runtime.png`とsource contactは入力観察用で、追加提案ではない。

画像パネルへA/B/並列をopen要求したが、結果はすべて **queued**。MacのCodexで当タスクを開けば表示予約が反映されるが、既にwindowへ表示されたとは未確認。直接PNGのpathも保持する。これはowner-localでありWebGPT別sessionへの画素共有ではない。public GitHub/Notionへ原画・skin・mock・派生画像・ORA・scriptをアップロードしない。PRへはこのtextとSHA/数値だけを記録する。

**選択後のgate:** Humanが2026-10-22にBを選択済み。静止方向の選択とDecision Log同期は完了。追加制作/Unity/Blenderは今回再開していない。R2 V0 FAIL、Draft/no merge/no Ready、ユーザー未commit作業を保持する。
