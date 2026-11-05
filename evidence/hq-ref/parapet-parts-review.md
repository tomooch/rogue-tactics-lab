# 原画の石造部材 — 2026-10-26

Humanは広域自律移動の拡張を後回しにし、「パーツしっかり作ろう」と制作優先を指示した。同Draft PR21 baseline `ae3507e588df7dc689fe686ab6626bc4993d1ca9`から、右手前の装飾部材一個を制作。G1/探索/Core/カメラ/4人/UIの挙動は変更しない。Codexが制作/観測/採否、Astraが実画像の限定助言。Claudeへの依頼、課金/有料素材/委託/private新外部送信/公開媒体/merge/Readyなし。

## 実制作 → 実描画 → 棄却と修復

- **A：花蔦の壁面帯**。編集可能SVG輪郭が原画の元RGBを直接サンプルするprivate Meshを制作。単体は白花/蔦/石を読めたが、390×844では既存不透明崖が大半を隠し棄却。
- **B：同輪郭の埋まり修復**。倍率と画面上の位置を維持し、通常depth-testの装飾面を支持壁前へ調整。花は見えたが、細い斜め帯と大きな灰色板が別部品に見える。root/Astraの実before/初期/勝利レビューで棄却。数や彩度で救済しない。
- **元画像の精査**。previewに輪郭単体と同じUV矩形の元絵を個別出力する機能を追加。最初は変数名衝突CS0136でexit1/新画像なし。修復後のfresh Unity exit0で、前輪郭が笠石と壁の主塊を落としていたことを実確認。
- **C：笠石＋石塊へ取り直す**。白花帯を廃し、上面・正面・側面・金具を持つ完結した主塊を選択。初期/勝利で石造の量感が読めた。一方、source下端の白/左辺の水色混入、床から独立した杭のような接触をroot/Astraが検出し保留。
- **D：輪郭と接触を仕上げる**。白/水色部分を石側へ詰め、主塊/倍率を保って笠石の奥側を床縁へ数px差し込む。rootの単体/初期/勝利、AstraのC/D計6実画像で、混入解消・量感維持・明確な空隙なしを確認。この位置の装飾石一個として保持。最終全回帰とfresh nativeでも下記の範囲を確認した。

## 実装の意味と限界

既存の原画UV輪郭Mesh工程を継続し、原画PNG/承認4PNGのbyte/RGBAを再描画しない。探索sceneだけ原画part定義を更新してloadし、凍結MobileG1 sceneは保護。装飾面の小さなcamera方向offsetはopaque支持体へ埋まる対処であり、深度検査を無効化しない。元の2部品はoffset0のまま。Collider/合法床/攻撃/HP/状態/結果は生成しない。

方向と陰影が焼き込まれた2.5D部材で、自由回転できる汎用3D石材の完成ではない。既存壁との材質差、広い灰色面、原画水準の全体品質、既存whole-sprite重複は未解決。**R2 V0 FAIL / Humanの可愛い・見やすい・楽しい未評価**を維持する。

## 検証・証拠

A/B/C/Dは各Unity6000.3.25f1のfiltered Edit0・Play1/1。実記録入力/方針/自動Protect/固定hash/保存比較/ページ/再試行の既存経路を通過。全EditやAttack/nativeの成功へ転用しない。各run中のsourceとG1/探索Core・MobileG1 scene・private PNGの保護9ファイル不変。実390×844をrootとAstraが上記範囲で閲覧した。

全回帰job `f67b2067-d0bc-47c8-9949-59f54bbc8d07`はEdit21/21・Play12/13・探索4/4。元の `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults` のstep0 C/X全sprite矩形重複のみFAIL。assertion/テスト不変、all-greenとはしない。baselineのreference-hud記録にも同じ失敗がある。両full固定hash・全8室往復・方針/任意120文字メモ/停止再開/比較/page/retryを対象試験で確認。source/保護不変、71fresh実画像。rootは最終初期/全8室帰還/Attack探索開始を実閲覧し、この範囲で新人物・HP・UI遮蔽を認めなかった。

fresh native job `2fbbdd85-ade4-4d21-b8c9-be65680b93b3` はUnity export/Xcode build各exit0、生成4設定復元・unexpected0。QA iOS27.0にbinary SHA256 `498d461fb47ff90bfb9012e5e2432c11d306d89a1b2398cc4ec2f209fe0cef62` をinstall/launch。実CUAで初期6体/HP → 一回Startの自動Protect → 勝利4人生存 → HPを保った探索 → 南西floor選択 → 移動 → 目的地到着を観測。初期/勝利/移動中/到着の4枚とruntime logをprivate保持。新nativeのProtect fullhash一致、runtime logにException行なし。この角度の石の上面/正面/側面と床縁の接触を保ち、観測画面で人物/UIへの新しい遮蔽なし。MCP stillは368×800の最適化JPEGであり、Editor390×844 PNGと同じ寸法とはしない。

最終nativeではAttack/全8室/pause/IME/メモ/比較/retryを再実施していない。新TestFlight upload/ASC配布確認/物理iPhone/Human試遊なし。local0.1.0(5)は旧Apple build5の更新ではない。最終sourceと全検査62ファイル/native232ファイルのSHAを照合し、不一致0。再現には公開codeに加え、owner-localのignored原画参照SVG/Mesh/Unity metadataが必要で、Gitだけでprivate美術まで復元できるとはしない。

private証拠はowner-local `_userwork/hq-evidence/2026-10-26-parapet-parts/`。各A〜DのSVG/単体と元領域/初期と勝利PNG/raw XML/log/source patch・guard、編集可能な原画参照/Unity Mesh/metadataのprivate archiveを保持。画像・SVG・Mesh・archiveは公開Gitへ入れない。別WebGPT sessionへの画素共有は未成立、公開はcode/text/hash/XMLに限定する。

実行source commit: `aded5bb95d88b038b5372762d24c17d4f7bfa77c`。commit内容と凍結manifestのtracked sourceも不一致0。後続commitは検証記録のみ。
