# 設計資料の入口

天穹の遺跡（Rogue Tactics Lab）の設計資料はすべてこのディレクトリにあります。作成は Claude Code（総合設計・グラフィック担当、Human 指示 2026-11-16）。

root の [DESIGN.md](../DESIGN.md) / [VERIFICATION.md](../VERIFICATION.md) / [AGENTS.md](../AGENTS.md) は repo の契約文書で、ここの資料はそれを置き換えません。食い違ったら契約文書と最新の Human 指示が優先です。

## 読む順番

| 順 | 文書 | 何が分かるか | 読む人 |
|---|---|---|---|
| 1 | [ROADMAP.md](ROADMAP.md) | 完成までの作業票。順番、完了条件、どの設計を読めばよいか | 全員。まずここ |
| 2 | [DECISIONS.md](DECISIONS.md) | Human が決めたこと（2026-11-17 の15件）と、まだ決まっていないこと | 全員 |
| 3 | [GAME_DESIGN.md](GAME_DESIGN.md) | 何を作るか。製品の芯、遊びの構造、規則、品質の基準、未決事項 | 全員 |
| 4 | [CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) | どう作るか。考え方、アーキテクチャ、手法、数値の根拠、確かめ方、失敗と直し方 | 実装する人 |
| 5 | [specs/](specs/) | **詳細設計**。作る単位ごとに、型・数値・手順・テストまで（下の表） | 実装する人 |
| 6 | [IMPLEMENTATION_SPECS.md](IMPLEMENTATION_SPECS.md) | グラフィック以外の実装設計の概要（Core / フィールド / カメラ / UI / 音 / 保存 / 性能 / 公開）。specs/ と食い違ったら specs/ が新しい | 実装する人 |
| 7 | [GRAPHICS.md](GRAPHICS.md) | キャラ・アニメーション・エフェクトの実装済み仕様の一覧 | 演出を触る人 |

## 詳細設計（specs/）

| 文書 | 中身 | 数値の根拠 |
|---|---|---|
| [01_CORE_RULES.md](specs/01_CORE_RULES.md) | いまの Core の事実、方針3つ、「なぜその手か」、HP の持ち越しと回復、表示へ渡す型 | 計算で確認 |
| [02_ENEMIES.md](specs/02_ENEMIES.md) | 敵5種のふるまいと数値、頭脳の仕組み | 苔・花は計算で確認。番人・こうもり・守護者は未検証 |
| [03_DUNGEON_AND_ROOMS.md](specs/03_DUNGEON_AND_ROOMS.md) | 5階の構成、部屋11個の配置図と結果、並び順、泉と報酬 | 計算で確認 |
| [04_RELICS_AND_GEAR.md](specs/04_RELICS_AND_GEAR.md) | 遺物6種、報酬の出し方、装備の方向 | 遺物は計算で確認。装備は未検証 |
| [05_AUDIO.md](specs/05_AUDIO.md) | 効果音26種と環境音の合成の仕方、鳴らす場所、重なりの決まり | 未検証（耳で確かめていない出発点） |
| [06_SAVE_AND_SETTINGS.md](specs/06_SAVE_AND_SETTINGS.md) | 保存の形、途中再開、記録、設定、演出を減らす、早送り | 設計のみ |
| [07_UI_AND_CAMERA.md](specs/07_UI_AND_CAMERA.md) | 画面の割り付け、カード、主操作、方針の選択、カメラの寄り、命中の光 | 画面の観察からの見立て |

「計算で確認」は [sim/](sim/README.md) の Python の計算です。Unity での実行でも、人の試遊でもありません。

## 印の意味

| 印 | 意味 | Executor の扱い |
|---|---|---|
| **[確定]** | AGENTS.md の frozen direction、または Human が明示的に決めたもの | 実装してよい |
| **[実験中]** | Human が方向を選び、実装はあるが人の試遊で確かめていないもの | 実装してよい |
| **[採用 日付]** | Human がこの資料の提案を採ったもの。出典は DECISIONS.md | 実装してよい。数値は初期値で、試遊で見直す |
| **[提案]** | この資料の提案。Human の判断がまだ無い | 実装で先に埋めない。DECISIONS.md §3 を見る |

## 実装の場所

| 領域 | 場所 |
|---|---|
| 規則（pure C#） | `unity/Assets/RogueTactics/Core/` |
| 表示 | `unity/Assets/RogueTactics/Presentation/`（`MobileG1View` の partial: `HqMotion` / `HqPose` / `HqJuice`） |
| キャラ・敵・エフェクト | `unity/Assets/RogueTactics/Presentation/Gfx/`、シェーダーは `unity/Assets/RogueTactics/Resources/Gfx/` |
| 非公開の原画 | `unity/Assets/RogueTactics/Art/MobileG1/Private/`（Git に入れない） |
