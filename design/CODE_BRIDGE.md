# 既存コードから完成形への橋

- 版: 2026-11-17 / 作成: Claude Code
- 対象: `claude/hq-graphics-release` の 2026-11-18 の head（S1-1 の解決と倒れる演出の手直しの後）の `unity/Assets/RogueTactics/`
- 目的: 詳細設計（[specs/](specs/)）は「完成形」を書いています。この文書は、**いまのコードのどこが、その完成形を妨げているか**と、**どの順番で直せば途中で壊れないか**を書きます。行番号は上の commit のものです。ずれていたら同じ検索をやり直してください（§6）。

## 1. 結論

いまの表示のコードは、**味方4人（A/B/C/D）・敵2体（X/Y）・方針2つ・既存の盤**を前提に書かれています。Core は一般的な形（任意の駒数、任意の行動順）を受け取れますが、表示がそれを受け取れません。

敵を3体にする、部屋ごとに HP を変える、方針を3つにする、のどれを入れても、表示のどこかが黙って間違えます（例外にならず、名前や色や最大 HP が違って出る）。**S2 に入る前に §4 の順で直してください。**

## 2. 前提が埋まっている場所

`G1View.cs` と `Stage0View.cs` は旧い検証用の画面で、製品の画面（`MobileG1View` と `Hq*.cs`）ではありません。下の一覧から除いています。触らないでください。

### 2.1 既存の盤の値を直接読んでいる（最大 HP、駒の一覧、行動順）

| 場所 | 何をしているか | 何が壊れるか |
|---|---|---|
| `HqProductHud.cs:37` | カードを `Fixture.Baseline().Initiative` の順に作る | 行動順が違う部屋で並びが変わる |
| `HqProductHud.cs:149` | カードの「HP x / y」の y を `Fixture.Baseline()` から読む | 最大 HP が部屋で違うと嘘の数字が出る |
| `HqReferenceHud.cs:122`、`:212` | 敵味方の色分けと、カードの駒を既存の盤から引く | 3体目の敵で `Find` が null |
| `HqReferenceTypography.cs:85` | HP の棒の長さの分母 | 同上 |
| `MobileG1View.cs:100`、`:269` | 駒の見た目を既存の盤の駒の数だけ作る | 敵が増えても見た目が作られない |
| `HqExploration.cs:31`、`:66` | 探索の一行を既存の盤の駒から作る | 持ち越した HP が探索に反映されない |

### 2.2 ID を文字で直書きしている

| 場所 | 何をしているか |
|---|---|
| `HqProductHud.cs:23` | カードの並び `{"A","D","C","B"}` |
| `HqProductHud.cs:24` | 名前。味方4人は直書き、敵は `HqCreature.NameOf(id)`（敵の名前・手番の一文字 `GlyphOf`・色・顔は `HqCreature` の1か所。ただし種類の決め方 `KindOf` は ID 起点で、敵の定義データを持つ段で置き換える） |
| `HqProductHud.cs:46`、`:49`、`:53`、`:150`、`HqAutonomousHud.cs:121`、`HqReferenceHud.cs:111`、`:214`、`:218`、`HqReferenceTypography.cs:77`、`:91`、`:92` | 方針を持つのは B、という前提でカードの文言と大きさを変える |
| `HqProductHud.cs:158`、`:161`、`HqExploration.cs:216` | 敵かどうかを `id=="X" || id=="Y"` で判定 |
| `MobileG1View.cs:290`、`:293` | 敵の見た目を `HqCreature.KindOf`/`AltOf`（ID 起点。`AltOf` は `id=="Y"`）で決める。旧い球は `u.Id=="X"` |
| `MobileG1View.cs:347`、`:356`、`:359`、`:389`、`HqMotion.cs:47` | 守りの線と盾を `"B"` から `"A"` へ固定で引く |
| `HqGauntlet.cs:171`〜`:190` | 狙いの指定の画面が X と Y の2択 |
| `HqPose.cs:58` | エフェクトの色を ID から引く（グラフィック担当の書いたコード。同じ問題） |
| `HqReferenceTypography.cs:73` | 顔の中心の位置をガルだけ変える |

### 2.3 数が固定

| 場所 | 固定されている数 |
|---|---|
| `HqReferenceHud.cs:94`、`:170`、`:210`、`HqReferenceTypography.cs:62` | カード 4 枚 |
| `HqGauntlet.cs:42` | 意図の矢印 6 本（味方4＋敵2） |
| `HqGauntlet.cs:46` | 危機の印 4 つ |

### 2.4 方針が2つ

`MobileG1View.cs:115`、`:116`、`:408`、`:409`、`:425`、`:426`、`:438`、`HqProductHud.cs:150`、`HqReferenceHud.cs:214`、`:229`、`:242`、`HqAutonomousHud.cs:164`、`:165`、`:173`、`HqGauntlet.cs:289`。

どれも `ProtectAnchor ? 文言A : 文言B` の形です。3つ目を足すと、バランスが「攻める」の文言で出ます。`HqGauntlet.cs:289` は危機のときの「逆の方針」を二択の反転で作っています。

### 2.5 出来事の中身を文字列で判定している

| 場所 | 判定 |
|---|---|
| `MobileG1View.cs:355`、`:366`、`HqRelations.cs:22`、`HqPose.cs:230` | `Detail.StartsWith("Guard")` で守りを判定 |
| `MobileG1View.cs:371`、`HqPose.cs:141` | `Detail=="Victory"` で勝敗を判定 |

Core 側の文言を1文字変えると、表示が黙って守りを見落とします。[specs/01](specs/01_CORE_RULES.md) §6 の型（`EventTags`）で置き換えます。

### 2.6 Core 側の直書き

| 場所 | 中身 |
|---|---|
| `G1Battle.cs:208`、`:216`、`:252`、`:265`、`:297` | 守られる者は `"A"`、守り手は `"B"` |
| `G1Battle.cs:120` | 既定の行動順 `{"B","X","A","C","Y","D"}` |
| `G1Battle.cs:40` | 回復 2 回。`:225` 回復の条件「4 以上減った」、`:229` 距離 2、`:291` 守り −3、`:293` 回復 +4、`:296` 挟撃 +2、`:302` 反撃 2 |
| `MobileG1Session.cs:21`、`:24` | 戦闘を `Fixture.Encounter(index)` からしか作れない |
| `MobileG1Session.cs:17` | 記録（`Trials`）の鍵が方針。同じ方針で2回遊ぶと上書きされる |

Core の ID の直書きは、**役割で引く形に直さないでください**（「Anchor の役割を持つ駒」など）。hash と既存テストの前提が変わります。味方の ID と役割の対応（A=守られる者、B=守り手）は S3 まで固定とし、[specs/01](specs/01_CORE_RULES.md) §0 のとおり扱います。

## 3. 完成形で持つべき形

表示が ID や既存の盤を直接見るのをやめ、**1か所の名簿**から引きます。

```csharp
namespace RogueTactics.Presentation
{
    // 駒1つの「見せ方」。規則は持たない。
    public sealed class UnitLook {
        public string Id;            // "A" "X" …
        public string Name;          // "シロ" "苔の精"
        public bool Enemy;
        public Color Tint;           // エフェクトと矢印の色
        public PuppetStyle Style;    // 味方の演出の型（Caster / Guardian / Blade / Archer）
        public EnemyKind Kind;       // 敵の見た目とふるまい（見た目＝ふるまい。specs/02 §7）
        public string PortraitKey;   // 顔カードの絵
    }
    public sealed class Roster {
        public IReadOnlyList<UnitLook> Allies;   // カードの並び順
        public IReadOnlyList<UnitLook> Enemies;
        public UnitLook Of(string id);           // 無ければ例外。黙って別の名前を返さない
        public static Roster From(EncounterState state);   // 戦闘の状態から作る
    }
}
```

- 名前、色、型、敵の種類は、**すべて `Roster` からだけ**引く。`id=="B"` のような比較を表示から消す。
- 最大 HP、生存、位置は `Session.Replay.State`（戦闘の状態）から読む。`Fixture.Baseline()` を表示から消す。
- 「方針を持つ者」は `Roster` ではなく Core に聞く（いまは B だけ。将来は `Rules.PolicyHolders(state)`）。
- 「守りの線」は、イベントの `Guardian`（守った者）と `Target` から引く。`"B"` と `"A"` を直書きしない。
- 方針の文言は `PolicyText.Of(Preference)` の対応表1つに集める。二択の `? :` を消す。
- 数の固定（4 枚、6 本、4 つ）は、`Roster` の人数から作る。画面に収まる上限（味方 4、敵 4）を `Roster.From` で検査し、超えたら例外。

## 4. 直す順番

1つずつ。各段の後で EditMode と PlayMode を回し、**結果が変わらないこと**を確かめてください（いまは PlayMode 28 件、通常 27 通過 + 1 skipped。[HANDOFF.md](HANDOFF.md) §8。重なりの検査は「画素で顔が隠れないか」を見る。[ROADMAP.md](ROADMAP.md) S1-1）。見た目が変わらない段は、`RT_GFX_CAPTURE` のコマが前と同じであることも見る。

| 段 | やること | 触るファイル | 変わってはいけないもの |
|---|---|---|---|
| 1 | `Roster` と `UnitLook` を足す。中身は既存の6駒と同じ値 | 新規 1 ファイル | 何も |
| 2 | 名前・色・敵かどうか（§2.2 の `HeroName`、`TintOf`、`id=="X"`）を `Roster` から引く | `HqProductHud`、`HqPose`、`HqExploration`、`MobileG1View`、`HqGauntlet` | 画面 |
| 3 | 最大 HP と駒の一覧（§2.1）を戦闘の状態から読む | `HqProductHud`、`HqReferenceHud`、`HqReferenceTypography`、`MobileG1View`、`HqExploration` | 画面 |
| 4 | 出来事の型（§2.5）。Core に `EventTags` を足し、表示の文字列比較を消す | `G1Battle`、`MobileG1View`、`HqRelations`、`HqPose` | hash、画面 |
| 5 | 方針の文言（§2.4）を対応表に集める。まだ2つのまま | `MobileG1View`、`HqProductHud`、`HqReferenceHud`、`HqAutonomousHud`、`HqGauntlet` | 画面 |
| 6 | 数の固定（§2.3）を人数から作る。敵 3 体の盤で PlayMode を1本足す | `HqReferenceHud`、`HqReferenceTypography`、`HqGauntlet` | 既存の盤の画面 |
| 7 | 守りの線をイベントから引く | `MobileG1View`、`HqMotion` | 画面 |
| 8 | 戦闘の作り方を `EncounterBuilder` 経由に（[specs/01](specs/01_CORE_RULES.md) §5） | `MobileG1Session`、`HqGauntlet` | hash |
| 9 | 記録の鍵を「部屋＋何回目」に（[specs/06](specs/06_SAVE_AND_SETTINGS.md) §3） | `MobileG1Session`、`HqAutonomousHud`、`MobileG1View` | — |

- 段 1〜7 は**見た目も結果も変えない作り直し**です。ここまでが S1 の中に入ります（[ROADMAP.md](ROADMAP.md) S1-0）。
- 段 4 は [ROADMAP.md](ROADMAP.md) の S1-4 と同じ仕事です。
- 段 6 で足す「敵 3 体の盤」は、[specs/03](specs/03_DUNGEON_AND_ROOMS.md) の R03 を使ってください。
- 段 8 から先が S2 の仕事（方針3つ、敵の頭脳、持ち越し）の入口になります。

## 5. 確かめ方（この橋が渡れたかどうか）

次の検索が、`G1View.cs` と `Stage0View.cs` 以外で **0 件**になること。

```bash
cd unity/Assets/RogueTactics && grep -rnE 'Fixture\.Baseline\(\)|"[ABCDXY]"|Detail\.StartsWith|Detail==' Presentation --include='*.cs' | grep -v 'Presentation/\(G1View\|Stage0View\)'
```

加えて:

- 敵 3 体の盤（R03）で、名前・色・矢印・危機の印・カードがすべて正しく出る PlayMode が通る。
- 最大 HP を変えた駒で、カードと足元の棒が正しい割合になる。
- 既存の2つの hash が変わらない。

- 既知の例外: `HqCreature.cs` の `AltOf`（`id=="Y"`）は段 1（Roster）まで残る1件。段 2 で Roster の `Alt` に置き換えて 0 件にする。

## 6. この一覧の作り方

行番号がずれたら、次で作り直せます。

```bash
cd unity/Assets/RogueTactics && for pat in 'Fixture\.Baseline\(\)' '"[ABCDXY]"' 'Preference\.(ProtectAnchor|DirectAttack)' 'Detail\.(StartsWith|Contains)|Detail==' 'i<4;|i<6;'; do echo "### $pat"; grep -rnE "$pat" Presentation --include='*.cs' | grep -v 'Presentation/\(G1View\|Stage0View\)'; done
```

`i<4;|i<6;` は関係のない繰り返し（`HqVfx.cs` の粒、`HqRoute.cs` の方向）も拾います。§2.3 に挙げたものだけが対象です。
