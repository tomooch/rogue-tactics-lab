# 設計資料の入口

天穹の遺跡（Rogue Tactics Lab）の設計資料はすべてこのディレクトリにあります。作成は Claude Code（総合設計・グラフィック担当、Human 指示 2026-11-16）。

root の [DESIGN.md](../DESIGN.md) / [VERIFICATION.md](../VERIFICATION.md) / [AGENTS.md](../AGENTS.md) は repo の契約文書で、ここの資料はそれを置き換えません。食い違ったら契約文書と最新の Human 指示が優先です。

## 読む順番

| 順 | 文書 | 何が分かるか | 読む人 |
|---|---|---|---|
| 1 | [ROADMAP.md](ROADMAP.md) | 完成までの作業票。どれが今すぐ走れて、どれが Human の判断待ちか | 全員。まずここ |
| 2 | [GAME_DESIGN.md](GAME_DESIGN.md) | 何を作るか。製品の芯、遊びの構造、規則、品質の基準、未決事項 | 全員 |
| 3 | [CRAFT_HANDBOOK.md](CRAFT_HANDBOOK.md) | どう作るか。考え方、アーキテクチャ、手法、数値の根拠、確かめ方、失敗と直し方 | 実装する人 |
| 4 | [IMPLEMENTATION_SPECS.md](IMPLEMENTATION_SPECS.md) | グラフィック以外の実装設計（Core / フィールド / カメラ / UI / 音 / 保存 / 性能 / 公開） | 実装する人 |
| 5 | [GRAPHICS.md](GRAPHICS.md) | キャラ・アニメーション・エフェクトの実装済み仕様の一覧 | 演出を触る人 |

## 印の意味

| 印 | 意味 | Executor の扱い |
|---|---|---|
| **[確定]** | AGENTS.md の frozen direction、または Human が明示的に決めたもの | 実装してよい |
| **[実験中]** | Human が方向を選び、実装はあるが人の試遊で確かめていないもの | 実装してよい |
| **[提案]** | この資料の提案。Human の判断がまだ無い | 実装で先に埋めない。ROADMAP の「判断待ち」を見る |

## 実装の場所

| 領域 | 場所 |
|---|---|
| 規則（pure C#） | `unity/Assets/RogueTactics/Core/` |
| 表示 | `unity/Assets/RogueTactics/Presentation/`（`MobileG1View` の partial: `HqMotion` / `HqPose` / `HqJuice`） |
| キャラ・敵・エフェクト | `unity/Assets/RogueTactics/Presentation/Gfx/`、シェーダーは `unity/Assets/RogueTactics/Resources/Gfx/` |
| 非公開の原画 | `unity/Assets/RogueTactics/Art/MobileG1/Private/`（Git に入れない） |
