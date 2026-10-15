# Rogue Tactics Lab

最終製品は **iOS / Android 向け native Unity game**。開発方向は **Unity 6.3 LTS (6000.3.x) / C# / URP / portrait mobile**。

Current design / experiment contract は [DESIGN.md](DESIGN.md)、[VERIFICATION.md](VERIFICATION.md)、[GitHub Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。agent 運用は [AGENTS.md](AGENTS.md)。

## 現在地

accepted implementation は常に actual GitHub `main` HEAD。PR #4 は merge 済みで、historical Web v0 は retired 済み。main に Web implementation / npm harness / Pages workflow はなく、Unity Stage 0 はPR #5でmerge済み（今回のbaseline `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。

[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) はmerge済み。以下のStage 0実測はmerge前の対象revisionの記録。`unity/` の URP・portrait 基盤と pure C# / asmdef / test scaffold が検証済み。Unity **6000.3.25f1**、import / compile PASS、EditMode **5/5 PASS**、PlayMode **1/1 PASS**、390×844 portrait smoke PASS。実行した pending HEAD は `6a0879e8877cf0717d2ac5019a923ea11fbbd0af`。証拠と限界は [VERIFICATION.md](VERIFICATION.md) に記録する。Stage 0にgameplayは含まない。今回のG1は下記の未merge実装。実機未検証、CI なし、human play 未実施。

- [Godot PR #2](https://github.com/tomooch/rogue-tactics-lab/pull/2) は closed / unmerged。流用しない。
- [旧 Unity audit PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は closed / unmerged / historical audit evidence。旧 branch は保持し、新しい Stage 0 では再利用していない。
- Humanは盤外からpartyを監督するB方向を承認済み（#1）。探索gesture、resolution chunk、intent horizonは未決。G1で正式仕様として固定しない。

## Retired historical Web evidence

Web v0 は retired historical evidence。active tree に保持せず、[最終 accepted snapshot](https://github.com/tomooch/rogue-tactics-lab/tree/0d8a2a68c37902256f5123ef86017ce83a60a64e) **`0d8a2a68c37902256f5123ef86017ce83a60a64e`** と annotated tag `archive/web-v0-final` で参照する。再現が必要なら archive を別 checkout で使用し、current main に runtime や旧 tooling を戻さない。npm test / check / build / start は archive のコマンドであり、current repo requirement ではない。

[当時の検証記録](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/VERIFICATION.md) は historical browser の証拠であり、native 実装や人の試遊成功の証拠ではない。Pages は current product / runtime ではない。

PR #4 は merge 済み。Pages は Human が Unpublish 済み（[Human の報告と docs sync 承認](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298)）。workflow 削除と Unpublish は別操作であり、agent が今回外部設定を変更したという意味ではない。

## G1 gameplay experiment / Issue #14

現在のaccepted mainにはPR #5のStage 0がmerge済み（`ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。今回の未merge実装は[Issue #14](https://github.com/tomooch/rogue-tactics-lab/issues/14)に従う固定1戦闘であり、one-floor完成ではない。

Unity **6000.3.25f1**でこのbranchの`unity/`を開き、`Assets/RogueTactics/Scenes/G1Battle.unity`をPlay。Game viewは390×844 portraitにする。

1. `Rules`で匿名役A/B/C/D・敵X/Yの数値と発動条件を読む。
2. `B: Protect A`または`B: Direct attack`を選ぶ。変更点はBの選好だけ。
3. 任意の自分の予想を入力し、`Start this setup`を押す。
4. `Next action`で1行動ずつ、または`Play to end`で順に観察する。`Pause`で止められる。線・記号・移動先ghostはCoreの現在plan、HPと行動ログは確定eventを表示する。
5. 終了後`Baseline`へ戻り、もう一方の条件で再試行する。結果は独立に保持され、HP／生存数／行動数／残り回復を比較する。`Rules`→`Trials / predictions`で条件ごとの事前予想とtrace hashも確認できる。

A/B/C/Dは匿名の実験役。正式キャラのシロ／ミャル／ガル／モモとは対応しない。1 action／encounter playbackや現在plan表示は検証adapterであり、正式resolution chunk／intent horizonの採用ではない。探索・relic・第二戦・意図修正・E提案はG2以降。[Design Freeze G1](DESIGN.md#design-freeze-g1--2026-10-13--issue-14)、実測・限界は[VERIFICATION.md](VERIFICATION.md)。

## Mobile Visual G1 / Issue #16（未merge）

PR #15のCoreを維持した専用scene `Assets/RogueTactics/Scenes/MobileG1.unity` をUnity **6000.3.25f1**でPlay。日本語UIでBの条件を選び、任意の予想を入力し、「この条件で戦闘開始」→「次の行動」または「続けて見る」で観察。「戻す」で初期状態へ戻り、もう一方を試して「結果・予想を比較」で照合する。「詳細」は匿名fixtureの数値・発動条件を表示する。

外見はAシロ/Bモモ/Cガル/Dミャルの**一時的なskin対応**。匿名fixtureの能力を正式キャラの能力に採用していない。Coreの数値・event列・2条件のhashはPR #15と同じ。

原画と派生skinは非公開のローカル入力であり、public Gitには含めない。source checkoutだけではskinは未設定になる。所有者の承認原画フォルダと下記offline toolを使い、`Tools/Rogue Tactics/Create Mobile G1`でscene参照を再生成する。原画のRGBは変更せずalphaだけ抽出する。モデルは開発時だけ使い、ゲームにAI runtimeを組み込まない。

```text
python tools/prepare_mobile_art.py --originals <approved-originals> --model <pinned-isnetis.onnx> --output unity/Assets/RogueTactics/Art/MobileG1/Private
```

依存：Pillow、numpy、opencv-python-headless、onnxruntime。モデル：[SkyTNT anime-seg](https://huggingface.co/skytnt/anime-seg/tree/493cb60893f47441b26ec4fb9a306bce9e342982)、Apache-2.0、`isnetis.onnx` SHA-256 `f15622d853e8260172812b657053460e20806f04b9e05147d49af7bed31a6e99`。原画を外部AIへ送信しない。

iOS exportは`RogueTactics.Editor.MobileG1Setup.ExportIos`、game専用IDは`com.tomooch.roguetacticslab`、version0.1.0/build4 candidate。署名・TestFlight・実機はそれぞれ独立したgateであり、Editorテスト成功を配布成功とは扱わない。本人限定TestFlightの初回利用は、登録済み本人メールへの招待をiPhoneで開いて承諾する。ASCの「テスト中」だけでは端末到達を確認できないため、招待メール受信→承諾→アプリ表示→install/起動を別に確認する。private rasterを含むcapture/buildはGit外へ保存する。最新の実測と未検証は[VERIFICATION.md](VERIFICATION.md)に記録する。

2026-10-14の試遊feedbackを受け、build3ではHPを頭上から足元へ移し、画角・表示間隔・人物を避ける矢印を調整。Core/両traceは不変。animation不足とHumanの見た目評価は別課題で、固定source d19a135でfresh Edit14/14・Play6/6、署名archive/Simulator実操作を確認し、本人限定TestFlightの0.1.0(3)はテスト中。iPhoneではTestFlightのRogue Tactics Labを更新する。修正後のHuman評価は未取得。詳細はVERIFICATION.mdを確認。

## HQ Reference G1 candidate — Issue #20

[Issue #20](https://github.com/tomooch/rogue-tactics-lab/issues/20) と監査コメントを基に、PR #17 exact HEADへstackするHQ-REF-1候補です。上のbuild3操作説明に代わり、通常画面は「シロを守る / 敵へ攻める」→任意予想→「仲間に託す」。仲間が自動行動し、「一時停止 / 再開」で観察します。終了後「結果・予想を比較」と「方針を変えて、もう一度」で両条件を試せます。「記録」はルール・開発用単発操作・軽量描画への導線です。正式chunk/horizonの採用ではありません。

実Unity 3D遺跡、既存承認skin、portrait/実HP、Core由来actor order、表示カーソルを保持するpause、Core eventだけの動きとfeedbackを比較します。最終画質・面白さのHuman GO、実機QA、true authored gait #13、G2探索は別ゲートです。

同じprivate素材を用意したLocal checkoutでの再検証：

```text
python3 tools/verify_hq.py --editor /Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity --output <owner-local-evidence> --configure
```

実captureはignored `unity/Logs/hq/private/`。公開証拠は `evidence/hq-ref/`、source/限界はVERIFICATION.md。元画像・派生skin・capture・署名資産はpublic Gitに含めません。floor/minimap/chest/正式skillは追加していません。


PR #21の[Pass2実装パケット](https://github.com/tomooch/rogue-tactics-lab/pull/21#issuecomment-6192468412)に沿い、環境は`tools/author_sky_ruins.py`で作る自作OBJとUnity Editor生成Mesh assetへ移行。再生成は同script→上記`--configure`の順です。非移動行動は局所actor/target表示と名前付きevent文、Moveだけ経路表示を使い、比較シートをrosterより前へ描画します。Coreの5×5・2敵・両traceと承認skinは維持します。

Pass2の造形比較では浮島側面と滝の露出は改善した一方、石材の厚み・侵食、崖の重層感、植物の密度、滝の量感、遠景が原画の品質に届いていません。R2はV0 FAILを保持し、高品質完成とは扱いません。PRIMARY bytes未取得、Human評価・実機QA・Notion同期も未完了です。新しい実行結果はVERIFICATION.mdのPass2記録を参照してください。

Pass2 final runtime source d5e8e08の0.1.0(5)はEdit14/14・Play9/9、署名archive・Simulator両戦闘/IME/再試行/比較を確認し、本人限定upload exit0。Apple transportはprocessingを返したが、ASC認証切れにより処理完了と本人限定グループ配布は未確認です。美術R2未達とHuman/実機評価待ちは継続します。
