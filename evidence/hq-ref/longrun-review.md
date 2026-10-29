# Local Blender / Astra R2 closed loop — 2026-10-18

**美術採用0、`BLOCKED_ART_PRODUCTION`、R2 V0 FAIL。** 保持runtimeは `fc005d4ddee3edc5ac5b035ecfefb5e132f5e25a`。ソフト導入・編集能力と、美術品質を分けて判定した。PR21はDraftのまま。

## 対象・許可

[Human長時間実行パケット](https://github.com/tomooch/rogue-tactics-lab/pull/21#issuecomment-6236909453) と [Astra追加指示](https://github.com/tomooch/rogue-tactics-lab/pull/21#issuecomment-6236942637)。実行開始・再照合時のPR HEAD `78bbaa7c44c9c7ceeedad22974205890bae130e2`、head `codex/hq-ref-vertical-slice`、base `codex/mobile-visual-g1` / `ce4ffac8d2216973c391d801cf5f6d41fbde66b5`、main `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`。既存専用worktreeを使用。canonical main側の無関係な `.gitignore` 編集には触れていない。

主変数は前左の既存 `HeroLedgeCliff` だけ。cap、水、蔦、床、カメラ、照明、postprocess、4人、HUD、Core、event順序は固定した。各案は同じMesh GUIDのOBJ/native Meshを実Editorで更新し、Sceneは変更していない。単一meshに限定したことはこの実験の制御条件であり、Humanがcapを永久に変更禁止にしたという意味ではない。

## G0 / G1：実環境と実編集

macOS26.6.2 / Apple Silicon、Unity6000.3.25f1。公式Blender5.2.2 LTS ARM64、build `d13f752e3b9c` をMac内に導入した。追加支出0、アカウント・外部制作サービスなし。

- 初回の公式ダウンロードlanding URLはHTMLを返し、SHA不一致で拒否。実行しなかった。
- 公式mirrorの [DMG](https://mirror.blender.org/release/Blender5.2/blender-5.2.2-macos-arm64.dmg) と [公式checksum](https://download.blender.org/release/Blender5.2/blender-5.2.2.sha256) のSHA256 `dc4125399b8bfefe283cc1624d6cfc7809d1cac20ace51072127eb371f31f210` が一致。
- `codesign --verify --deep --strict` exit0、Developer IDはStichting Blender Foundation、`spctl --assess --type execute` exit0 / Notarized Developer ID。OS保護の解除なし。
- 実Blender GUIでAの破断面を選択しZ−0.18編集、Undo画像で戻り、Redo画像で再適用を確認。保存toast→新規ファイルで元を閉じる→保存したファイルを再open→Edit Modeで再編集可能を実観測した。アプリ再起動を試したという主張ではない。
- Bは独自の疎な面構成を作成後、GUIで下端2頂点のvertex bevelを操作して保存。CはBを局所sculpt用にremesh後、GUIのScrape Multiplaneで3本の大きな面削りを実行し保存した。初期の小さいブラシでは有効な面差が出ず、広いブラシ・固定plane angleにして実変形を観測した。

下地はローカルBlender Pythonによる独自形状＋実GUI編集であり、「空のファイルから全形状を手彫りした」とはしない。独自meshにUnity sample FBX、外部モデル、原画/skinの画素は入力していない。色は保持cliffの既存vertex RGBを位置で移した。Blenderのソフトウェアライセンスと生成作品の権利は別である。[公式ライセンス説明](https://www.blender.org/about/license/)。この未採用sourceの公開・再配布ライセンスは設定しておらず、`.blend`/OBJ/native Meshはprivate保持。

G1の実編集・Undo/Redo・保存・再openは確認できた。前回の「GUI未観測」「Blender未導入」という能力不足は解消したが、美術ゲートの通過ではない。

## 実制作 → Unity → 実画像 → 修正 → 再描画

CONTROLと各案を実Unityで390×844の7場面に描画した。setup / Protect Guard / Heal / Attack Counter / Death / Protect比較 / 両条件比較。原画は縦横比を保って幅390px、候補は生の390×844で比較し、2倍cropは診断補助だけにした。原画のPRIMARY対Mac比較PNGのRGB完全一致は[既存独立検証](https://github.com/tomooch/rogue-tactics-lab/issues/20#issuecomment-6203653575)で解決済み。ambient animationの位相は固定しておらず、同一event段階は同一pixel時刻を意味しない。差分pixel面積を美術scoreにしなかった。

| 案 | 主要な制作操作 | 電話サイズの実観測と採否 |
| --- | --- | --- |
| A | 不規則な斜め破断面と6つの負形状をBooleanで切り込む独自原型。GUIの面編集後に保存したsourceをexport | 幅広い褐色の平板に見え、切り込みが層を分けない。下部の滝と蔦を岩が隠す。独立local画像reviewも全体退行と判定。**棄却**。切り込みを増やす方法は続けなかった。 |
| B | 個々に向きを設計した37頂点27面の閉じたpatch cage。開いた下端の欠けと大小2つの垂下形状。GUIで先端を破断 | Aより滝が全長見え、負の空間が読める。ただし滑らかな短い歯状で、下の既存蔦が支持を失う。CONTROLに対する画面全体の魅力・4人の主役性は増していない。独立local画像reviewも不採用。**棄却**。 |
| C | Bに実GUIの大面Scrape Multiplane sculpt。細線を追加せず凸面を削り替える | Blender近接では面があるが、Unity390pxでは上の厚いcapに隠れ、見える面は浅い凹凸に留まる。CONTROLより短く単純な崖に見える。水の全長は保つが蔦の接続は未解決。独立local画像reviewも層状石灰岩・全体改善を認めず。**棄却**。 |

各案で4人、HP、HUD、Guard/Heal/Counter/Deathのreceiptや両結果に目立つ新規遮蔽・切れは観測しなかった。これは可読性や楽しさのHuman合格ではない。Death名のcaptureは敵の戦闘不能を含む中間stageで、最終的なAの死亡画像と取り違えない。結果はProtect A16/B16/C12/D10・4人生存・回復残0、Attack A0/B16/C12/D10・3人生存・回復残1。

ローカル独立画像reviewerは各案の7組と原画/CONTROL/候補を実際に閲覧した。Bについて「緑を追加」と表現した点はrootが訂正した。緑は変更しておらず、崖の短縮で既存片が露出した見え方である。観察者の言葉も実装事実と照合した。

Cのexportではremesh後の三角形ごとの頂点重複が既存UInt16 importer上限を超え得るため、位置・面normalが同じ頂点を共有し54,863頂点に収め、上限assertで切り捨てを拒否した。法線の違う面を溶かすsimplifyではなく同じsurfaceのindex共有で、三角形27,904は保持。三角形数の増加を品質根拠にしない。これらのprivate export/helperも製品に採用していない。

## Astra：実際の呼出しと入力の限界

実native collaborationの `gpt-6-astra` / highを初期診断1回、A後、B後、C後の計4回使用。**すべてsanitized text-only**。原画・skin・画像・動画・mesh・private pathは渡していない。Astraは一度も画素を見ていないため、独立pixelレビューや視覚PASSを主張しない。

1. 初期：閉じた容器状の外形と方向のない層が疑わしい。大きな張り出しと欠けで検証。
2. A後：切れ目が外形・遮蔽量を変えていない。疎な面で開口と非対称な垂下形状を設計する仮説。
3. B後：凸面を細い傷で飾らず大きなScrape面に置換。水の遮蔽、浮遊蔦、歯状が残れば採用拒否。
4. C後：新しい加工コマンドを足す前に、電話画像の露出範囲と水/蔦の接触位置を確認。現在の方法は `BLOCKED_ART_PRODUCTION` が妥当という助言。

rootはCONTROLの実画像に手動の露出範囲・接触域注釈を作り確認した。これは近似的な観測注釈で、pixel-perfectなobject maskや3D接触測定ではない。露出面は存在するので物理的不可能とは言えない。現状は、厚いcapの下で見える大きさ、滝の前後関係、蔦の支持を同時に満たす石灰岩の大形状を**この制作が示せていない**。助言を根拠に未承認の永久scope制限やHuman合格を作らなかった。

## 判定と不足

**`BLOCKED_ART_PRODUCTION`**。ソフト・GUI操作・保存・importが使えない状態ではない。欠けているのは、固定されたゲーム構図内で、大きな破断層と深い影、張り出し、植生の支持面を統合して設計し、実際に電話で読めるsourceへ仕上げる制作能力/形状案である。A→B→Cの異なる操作はその品質を証明できなかった。全面の床、他の崖、水、植生、遠景へ失敗形状を複製していない。4人が最初の焦点になる全体構図も未達。

次に有効な制作は「ブラシ値/細部/色/三角形数をもう一度変える」ではない。代表領域内のcapから下側支持面までを一体にした、**電話画像上で見える輪郭と接触位置を先に説明できる形状設計**が必要。G2の範囲で成立するなら追加の通常制作許可を求める必要はないが、今回それを完成したとはしない。

| 再開の選択肢 | 費用・権利・privacy |
| --- | --- |
| 既存の無料ローカルBlenderで、この代表領域を作れる作者が形状設計/制作 | ソフト追加0、制作時間は未見積。独自source・商用利用/改変・source公開権を確認。原画はMac内に保持可能。インストールだけで品質が解決するとはしない。 |
| 所有者が既に権利を持つ、同じ接触条件を満たせる編集source | 新規購入は実施0。権利/作者/商用mobile改変/source再配布条件と対応形式を事前確認。privateローカルだけで検証可能。 |
| 外部の限定制作依頼/有料source | 未見積・支出0・連絡0。予算、deliverable、権利、守秘、private原画へのアクセス、再利用/保持をHumanが明示承認してから。現指示では実施しない。 |

## 回帰・rollback・証拠

各CONTROL/A/B/Cのfiltered ProductFlowはPlay1/1（Edit0）で両full frozen hashをassertした。全14/9とは区別する。C保存sourceからのimport・実描画exit0を確認後、失敗したOBJ/native MeshとEditor helper/meta/patchをprivate保存し、task-ownedの2tracked assetだけを保持runtimeと完全一致に戻し、2temporary helperだけを除去した。Scene SHAは全案で `4e6b58ed0b3e80938d2d1b25b2e801013c0b0fe6d6ff2ea908657184d3fc3704` のまま。

**Fresh full gate: EditMode14/14、PlayMode9/9 PASS、各exit0。** Unity6000.3.25f1で2026-10-17 23:37–23:39 UTCに保持sourceを新プロセスで実行。両full frozen hash、表示cursor/停止再開/予想/比較/再試行、safe area/keyboard、death suppressの既存全suiteを再検証。Sceneと承認4skinの全SHA一致、Unity/tools tracked差分0・untracked source0をassert。公開XMLは `longrun-editmode.xml` / `longrun-playmode.xml`、完全なhash/source条件は `longrun-evidence.json`。保持sourceの新Simulator export/buildも各exit0、5生成設定は差分保全後に元へ戻しUnity/tools byte一致を確認。専用QA iOS27.0にinstall/launchし、両条件の14行動と上記HP/生存/回復残数を実画面で確認。この起動sessionのログで両full frozen hash一致。ProtectのA17/X5/Y12で約18秒間の停止保持→再開、比較/close/再試行/初期配置への復帰、画面内かなキーボードで「かつ」のcomposition→確定→比較保持を確認した。未実行CI、物理iPhone、Humanの可愛い/見やすい/楽しいをPASSにしない。Apple旧build5は `d5e8e08...` のまま。今回の未熟な美術案をTestFlightへuploadせず、ASCのcompliance/groupや認証設定にも触れていない。

公開証拠は本書と `longrun-evidence.json`、fresh XML（検証完了後）。画像・動画・`.blend`/OBJ/native Mesh・rawログは公開しない。owner-local **一時領域** `/private/tmp/rogue-hq-longrun/` の `control/`、`limestone-a/`、`limestone-b/`、`limestone-c/`、各 `*-comparison/`、`gui/` に実画像・source・logs/hashを保持。`exposure-and-contact-diagnostic.png` はrootが実際に確認した注釈付き診断。`private-comparison.html` に原画/CONTROL/A/B/Cと7場面を外部通信なしのHTMLとして保存した。

**ローカルpathはWebGPTの画素アクセス経路ではない。** 既承認の別session向けprivateメディア共有経路は確立していない。WebGPTは公開の本文・source/画像hash・テストXMLを確認できるが、今回のprivate画像を独立視覚評価済みとはできない。新規外部サービス/原画送信/公開/merge/Readyはなし。

新しいprivate native録画 `simulator/retained-live-ime-both.mp4` は184.635秒・11,259frames・1206×2622/H264（操作/停止/読み取り時間を含む）。QuickTimeの標準速度を明示選択した有限区間134.317→144.462→159.070秒でも、局所攻撃/反撃とA7/X5からA0/勝利への進行を観測した。全184秒を毎frame視認したとはしない。Simulator録画の平均fpsは実iPhone性能証明ではない。ASTC6x6非対応のdecompression2件とshadowmap depth16-bit fallback警告あり、今回操作でcrash/exception/新規の描画破損は観測なし。Simulator開発番号5はAppleにuploadされた旧d5e8e08/build5とは別物で、新archive/upload/ASC変更0。公開のbuild/source/実操作/動画SHAは `longrun-evidence.json`。
