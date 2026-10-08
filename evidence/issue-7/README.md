# Issue #7 — 縦持ち理由表示の比較モック

このディレクトリのPNGはUnity Editor **6000.3.25f1**のPlayModeで実際に描画した390×844の画像。架空の戦況を作成者が記述したUX fixtureであり、Coreの観測、AIの推論、戦闘結果、人試遊の証拠ではない。

- baseline main: `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`
- branch: `codex/issue-7-portrait-ux`
- scope: [Issue #7](https://github.com/tomooch/rogue-tactics-lab/issues/7)
- A: 全員の理由を常時表示。
- B: 全員の行動は常時表示、選んだ仲間の理由のみ表示。
- PNGは双方とも仲間Aを選択。同じfixture、文章、盤上配置、色の役割、介入導線を使用。

## 実行結果（2026-10-08 JST）

実行対象source commit: `73f69023c41f77cadf1bed9ae1f6f23afe325361`。import/compile成功、EditMode **5/5 PASS**、PlayMode **2/2 PASS**（Stage0 regressionを含む）、両Editor実行exit 0。UI raycastとpointer-click handlerによるA/B切替、4人の行選択・理由閲覧、説明パネルの開閉、残数が変わらないこと、文字boundsを検証。

- [A: 全員の理由](variant-a.png) / [B: 選んだ仲間の理由](variant-b.png)
- [検証要約・PNG SHA256](verification.json)
- [EditMode XML](editmode.xml) / [PlayMode XML](playmode.xml)

画像生成revisionも同source commit。後続のevidence commitはこのディレクトリだけを追加し、Unity treeは実行対象と同一。最終PR HEADとこの区別をPR本文に記録。raw Editor logsはlicense等のdiagnosticを含み得るためcommitせず、verification.jsonの一時領域pathに保存。テストのXMLは公開用の実測結果。

## Editorで比較する

`unity/Assets/RogueTactics/Scenes/PortraitComparison.unity`を開いてPlay。Game Viewを390×844 portraitにする。A/Bボタン、仲間の行または盤上のA/B/C/Dラベルをクリックして比較する。「仲間に託す」「選んだ仲間を修正」は説明だけを開く。比較画面に戻ると同じ戦況と選択が保持される。残数は表示例の1回のまま。

日本語フォントはLocal Macの`Hiragino Sans`をOSから使用する。font assetや新しいpackageは追加していない。iOS/Androidでのフォント、touch、safe area、実機表示は未検証。

## 開発・検証の進め方

2026-10-08のHuman指示に従い、Unity開発もCLI主体。コード編集、import/compile、EditMode/PlayMode、実描画PNG生成をCLIで実施し、GUIはCLIでは観測できない表示・操作に限る。実行待ちはdetached deterministic runnerへ任せる。コストを抑えるために受入条件や必要な検証は減らさない。今回のGUI用Computer Use呼出しはtimeoutで観測できず、GUI操作は行っていない。ボタン操作はUnityのUI raycastとpointer-click handlerで検証する。

## 再実行

Editor実行ファイル: `/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity`。

```text
-batchmode -quit -projectPath <worktree>/unity -executeMethod RogueTactics.Editor.PortraitComparisonSetup.Configure -logFile <import.log>
-batchmode -projectPath <worktree>/unity -runTests -testPlatform EditMode -testResults <edit.xml> -logFile <edit.log> -screen-width 390 -screen-height 844
-batchmode -projectPath <worktree>/unity -runTests -testPlatform PlayMode -testResults <play.xml> -logFile <play.log> -screen-width 390 -screen-height 844
```

通常は最後の2 commandのみ。Scene再生成は必要な場合だけ実行する。Stage0 Sceneとそのbuild設定は保持。比較SceneはEditor用fixtureで、production buildのentry pointには追加していない。

## Self-review

Eはcompact off-board UIのみ。プレイヤーの方針はfixtureの明示された目的として表示し、内心を判定しない。EとAの相違に正誤、警告、評価色をつけない。託す／修正は説明のみで、fixtureと残数は変わらない。比較で変わるのは理由表示と選択モードの表示。Core / Stage0 / Packages / ProjectSettingsはbaselineから不変。

独立レビューでUI cameraの描画対象を指摘され、UI layerとfield描画を分離して修正。強化したraycast testで、新たに開いた説明パネルのgraphicsが登録される前の同フレーム検査が失敗。インストール済みuGUI GraphicRaycasterの未描画graphics除外を確認し、開いた後にrender frameを待つテストへ修正した。UI制約やテストのassertionは緩めていない。

AGENTS.mdには後続Human指示のCLI運用を追記。PR #6の状態同期と重ならない箇所に追加し、`git merge-tree`でconflict markerなしを確認。PR #6はDraftの元HEADを保持。

## Humanに比較してほしい問い

- Eの提案、Aの異なる意図、その理由を行動前に読み分けられるか。
- 仲間全員の理由が見えるAと、選んで読めるBでは、判断しやすさや負担がどう違うか。
- 「託す／1回だけ修正する」の導線を発見できるか。

比較結果はまだない。採用する案、resolution chunk、intent horizonはこの実装で決めない。Core戦闘、合法性、軍師AI、信頼度、実際の意図修正・行動解決は含めない。外部サービス・telemetryは追加していない。
