# 広い同一フィールドの探索試作 — 2026-10-25

Humanは「部屋と通路を実際に探索する広いfield」を選択した。縮小した5×5や背景だけの拡張では満たさないため、旧G1 fixtureを保った独立した純C#探索Coreと、同一Unityシーンでの受け渡しを制作した。6部屋・31×27・既知床タップ・到達/新部屋発見での停止は試験設定で、最終入力方式/解決chunk/意図horizonの採用判断ではない。

## 実装と観測

- 戦闘後の実位置/HP/生死から探索開始。Protect4人、Attack3人を確認。復活/回復/配置リセットなし。
- Coreが床合法性・BFS・隊列・既知状態を所有。表示は実際の移動を消費してから地形/地図を公開し、全未知経路や未来の人物位置を描かない。
- 実EventSystem入力で6部屋centerを巡回して帰還。cameraは移動だけで人物の表示倍率を変えない。
- 地図の空白、移動直後のUI hit、retry時の探索済み地図残りを初回実行で検出し修復。診断ログの失敗2回も履歴として残し、NoUnexpectedReceivedは維持した。
- 選択した既知床に金枠を描き、Core内部の未知の通路終端は表示しない。新部屋のコピー元に含まれた高所庭園/上部柱は支持構造がないため除去。

Dの新Edit7/7・Play2/2はPASS。原G1scene/G1Battle/GridPosition/privatePNGのbyte guardとsource固定を確認。その後の最終全回帰はEdit21/21・Play11/12（新探索3/3）、元strict全quadだけFAIL。専用nativeでも入力と地図の優先順位修正・Protect探索接続を実観測した。過去版の成功とは分けて同JSONに記録した。

## 敵対的評価

RootはDのmoving/room3/room5/Attackdiscovery/retryを実閲覧。AstraアドバイザーもC1枚・D3枚を実閲覧し、空中植生/孤立柱の除去と既知選択地点表示を限定改善と認めた。確認したstillに新しい顔/HP/UI遮蔽は目立たないが、全frameや全合法セルでの無遮蔽保証ではない。

部屋は広がったが、均一な大石床と単純な側面、同じ意匠の反復が強い。通路の未探索端と装飾張り出しの区別、青い足元枠と金選択枠の二重表示にも弱さが残る。これは探索接続の進歩であり、美術全体・原画水準・遊びたさの達成ではない。**R2 V0 FAIL**、Human品質/実iPhone未検証。

## 証拠と再現

画像を含まないSHA/失敗履歴は[field-exploration-evidence.json](field-exploration-evidence.json)。private原画・4人・Unity画像/動画は公開GitHubへ含めない。owner-local `_userwork/hq-evidence/2026-10-25-field-parts/` にA〜Dのfresh画像/patch/source/log/XMLを保持し、ローカル所在を別セッション共有成功と扱わない。

実Unityで `RogueTactics.Editor.HqFloorSetup.Configure` を実行し、`HqExploration`シーンをPlayする。旧`MobileG1`の回帰シーンは変更しない。専用Simulator exportは `HqFloorSetup.ExportIosSimulator`。この試作は初期遭遇後の探索で、新遭遇/報酬/階層を備えた完成ループではない。新TestFlight archive/upload/配布や実機確認は未実施。


## 製品ビルドでのみ発見した問題と修復

全回帰は2回ともEdit21/21・Play10/11で元のstrict全quadだけFAIL。新探索はPASSでも、最初のnativeではShader.Find-only素材が欠落しUI初期化が中断した。sceneから参照済のshaderをcloneする修復後、新nativeで初期画面/両戦闘/4人と3人の探索/停止再開/比較/再試行/かな候補確定を確認した。これはEditorの成功をnativeへ転用せず検出した不良である。

続いてnativeIME中のmapと予想欄の重なりを実画像で発見。方針/記録modal中のmap非表示・復帰へ修復し、新しい実UI回帰を追加して最終Edit21/21・Play11/12（新探索3/3）を確認。修正後の新binary fcd44ee9…でPolicy/IME/記録の地図非表示と復帰、Protect探索を実観測。修正前native39c8adcc…のAttack/pause/retry/録画とは分けて記録した。

最終新EditorのPolicy/room3実画像もrootが再閲覧。地図が譲ることで入力幅を確保し、人物/カメラ/床造形を変えずにUI競合を除去した。改善範囲は明記し、R2/Human合格を代行しない。

最終実行source: `d521d32389c42f0987558af667084dc26c0eb8bb`。commit後に最終frozen48ファイルのSHA一致を再確認。証拠専用追記はruntime差分を含まない。
