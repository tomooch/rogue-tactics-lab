# 詳細設計 01 — Core の規則

- 版: 2026-11-17 / 作成: Claude Code / 状態: **未実装の設計**
- 対象コード: `unity/Assets/RogueTactics/Core/G1Battle.cs`（`RogueTactics.Core.G1`）、`Core/DungeonFloor.cs`（`RogueTactics.Core.Exploration`）
- 決定の出典: [DECISIONS.md](../DECISIONS.md) の B3・B4・B8
- 数値の根拠: [sim/](../sim/README.md) の計算。Unity では未実行。実装後に同じ結果が出ることを EditMode で固定してください。

## 0. いまの Core（読んだ事実）

設計はここから出発します。変えるときはこの表との差で考えてください。

| 項目 | 現在の実装 | 場所 |
|---|---|---|
| 盤 | 5×5。障害物なし（空きの判定は駒だけ） | `EncounterState.Empty` |
| 駒 | `Unit(Id, Role, MaxHp, Attack, Position)`。味方 A/B/C/D、敵 X/Y | `Fixture` |
| 味方の値 | A: HP 18・攻 5 / B: HP 16・攻 4 / C: HP 12・攻 4 / D: HP 10・攻 2 | `Fixture.Baseline` |
| 行動順 | 接敵ごとに固定の列。倒れた者は飛ばす | `EncounterState.Initiative` |
| 方針 | `Preference { ProtectAnchor, DirectAttack }`。意味を持つのは B だけ | `Rules.PlanFor` |
| 攻撃 | **隣接（距離1）のみ**。弓も魔法も同じ | `Rules.IsLegal` |
| 守り | B が A の隣で `Protect` → A への被害 −3。B が次に動くまで。B が隣にいる間だけ有効 | `Resolve` |
| 回復 | D が距離2以内の味方を +4。接敵ごとに2回。対象は「最大 HP より4以上減った者のうち HP 最小」 | `PlanFor` |
| 挟撃 | C が、A の隣にいる敵を殴ると +2 | `Flank` |
| 反撃 | A が殴られて生き残り、相手も生きていて、被害が1以上なら 2 を返す | `Resolve` |
| 敵の狙い | A が生きていれば必ず A。A が倒れたら最も近い者 | `PlanFor` |
| 移動 | 1歩。北→東→南→西の順に試し、距離が縮む空きマスへ。無ければ待つ | `MoveOrMaintain` |
| 修正 | 接敵ごとに1回。方針の変更（`Correct`）か、1人に狙いを指定（`TargetOnce`） | `Replay` |
| 危機 | 敵の今の狙いを合計して HP が尽きる味方 | `Rules.Threatened` |
| 打ち切り | 120 手で `Limit` | `Rules.ActionLimit` |
| 記録 | 入力列とイベント列の SHA-256。`Detail` の文字列も含む | `Replay.Hash` |

**ID の直書きに注意**: 守りと反撃と敵の狙いは `"A"`、守り手は `"B"` と ID で書かれています。一般化するまで、味方の ID と役割の対応を変えないでください。

## 1. 守ること

- pure C#。`UnityEngine` に依存しない。
- 同じ初期状態と入力列からは、同じ状態とイベント列。乱数を使うなら seed から。時刻とフレームを入力にしない。
- **既存の hash を壊さない**。`GauntletCoreTests.EncounterZeroIsTheFrozenG1FixtureAndHashes` の2つの hash は、この設計を全部入れた後も一致すること。
- `Plan.Reason` と `BattleEvent.Detail` の英文は hash に入っています。文言を変えない。表示用の日本語は Presentation で引き当てる（§4）。

## 2. 実装の順番

小さく切って、毎回 EditMode を通してください。

| 順 | 変更 | 壊してはいけないもの |
|---|---|---|
| 1 | `Unit` に現在 HP を渡せる生成口を足す（§5） | 既存の生成子はそのまま |
| 2 | `Preference.Balanced` を足す（§3） | 値 0・1 の意味と直列化 |
| 3 | `IntentReason` を足す（§4） | `Reason` の文字列 |
| 4 | 敵の頭脳を駒ごとに持たせる（[02](02_ENEMIES.md)） | 既定は現行の「A を狙う」 |
| 5 | 遠征の状態と部屋の定義（§5、[03](03_DUNGEON_AND_ROOMS.md)） | `Fixture.Encounter(0..2)` |
| 6 | 遺物の修飾（[04](04_RELICS_AND_GEAR.md)） | 遺物なしの結果 |

## 3. 方針を3つにする（B3）

| 表示名 | 値 | B（守り手）のふるまい |
|---|---|---|
| 慎重 | `ProtectAnchor = 0`（既存） | いつも A を守る。隣にいなければ近づく |
| 攻める | `DirectAttack = 1`（既存） | 守らない。最も近い敵へ |
| バランス | `Balanced = 2`（新規） | A の HP が最大の半分より多い間は「攻める」と同じ。**半分以下になったら「慎重」と同じ** |

```csharp
// PlanFor の先頭の条件を置き換える
var protect = command.Preference == Preference.ProtectAnchor ||
    (command.Preference == Preference.Balanced && a != null && a.Alive && a.Hp * 2 <= a.MaxHp);
if (actor.Role == Role.Protector && protect && a != null && a.Alive) { /* 既存の Protect / 近づく */ }
```

- B 以外の味方と敵は、方針で変わりません（現行どおり）。
- `Threatened` の計算は方針に依らないので変更不要です。
- 表示名は Presentation の対応表に置き、Core に日本語を入れない。

**なぜこの定義か**: 3案を既存3戦で比べました（`sim/screen.py`）。

| 案 | 中身 | 結果 |
|---|---|---|
| 隣接 | 敵が A の隣にいれば守る | 3戦とも「慎重」と同じ結果になり、選ぶ意味が無い |
| **半分**（採用） | A が半分以下なら守る | 「慎重」とも「攻める」とも違う結果が出る。一言で説明できる |
| 危機 | A が倒れそうなら守る | 「半分」とほぼ同じ結果。条件が読みにくい |

**副作用**: 既存の3戦目は「正しい修正を1回使わないと全員は生き残れない」盤ですが、バランスだと修正なしで4人生存します（計算: 生存 4、HP 合計 39）。そのため **1階は2方針のまま**にし、バランスは2階から出すことを推奨します（[DECISIONS.md](../DECISIONS.md) N5）。

**対象を選べるようにする**（S2 で試す）: いまは方針が B にしか効かないので、対象を選ぶ意味がありません。試すときは「方針を持つ者」を1人選ぶ形にし、各役割の読み替えを次で始めてください（未検証）。

| 役割 | 慎重 | 攻める |
|---|---|---|
| C（切り込み） | A の隣にいる敵だけを狙う | 最も近い敵（現行） |
| D（支援） | 回復の条件を「2以上減った者」に緩める | 回復せず攻撃に回る |

## 4. 「なぜその手か」を出す（B8）

`Plan.Reason`（英文）は既にあります。これを列挙に写し、Presentation が日本語の一言に引き当てます。

```csharp
public enum IntentReason { None, Protect, ApproachToProtect, HealLowest, ApproachWounded,
                           FlankAttack, AttackAdjacent, ApproachEnemy, Blocked, NoTarget, Ordered,
                           // 敵の頭脳（02）用
                           HuntAnchor, HuntNearest, HuntWeakest, GuardNeighbour, Leap, Telegraph, Strike }
public sealed class Plan { /* 既存 */ public IntentReason Why { get; } }   // Serialize には入れない
```

| `Reason`（既存の文字列。変えない） | `Why` | カードの一言（13pt、全角8字まで） |
|---|---|---|
| `Protect preference; adjacent living A` | `Protect` | シロを守る |
| `Approach living A to protect` | `ApproachToProtect` | シロのそばへ |
| `Lowest HP ally in heal range; charge available` | `HealLowest` | {名前}を癒やす |
| `Approach wounded ally` | `ApproachWounded` | {名前}のもとへ |
| `Adjacent attack; complementary flank +2` | `FlankAttack` | 挟み撃ち |
| `Adjacent living opponent` | `AttackAdjacent` | 目の前を討つ |
| `Approach opponent by legal unoccupied step` | `ApproachEnemy` | 敵へ向かう |
| `No empty step reduces distance` | `Blocked` | 道がない |
| `No living opponent` | `NoTarget` | 様子を見る |
| `Intervention: …` | `Ordered` | 指示に従う |

- `Why` は `Plan.Serialize()` に**入れない**（hash を変えないため）。`Reason` から決まる派生の値として持つ。
- 敵の一言は出さない。敵は矢印と印だけ（[07](07_UI_AND_CAMERA.md) §3）。
- 完了条件: 全 `Reason` に `Why` が対応していることを EditMode で列挙して確かめる。対応の無い `Reason` が出たらテストが落ちる。

## 5. HP の持ち越しと回復（B4）

### 5.1 規則

| 時機 | 起きること | 初期値 |
|---|---|---|
| 戦闘に入る | 味方の HP は前の戦闘の終わりの値。回復の回数は毎回2に戻る | — |
| 戦闘に勝つ | 倒れた者が戻る | 最大 HP の **25%**（切り上げ） |
| 戦闘に勝つ（続き） | 全員がひと息つく | 最大 HP の **25%**（切り上げ）を加える。上限は最大 HP |
| 泉の部屋 | 全員が回復 | 最大 HP の **50%**。階に1つ、2戦目と3戦目の間 |
| 階を降りる | 全員が回復 | 最大 HP の **50%** |
| 全滅 | 遠征の終わり。その階の最初からやり直せる（S3） | — |

切り上げ後の実数: A は 25% で 5、50% で 9 / B は 4・8 / C は 3・6 / D は 3・5。

### 5.2 この数値の根拠

5階・14戦（既存3戦＋新しい11部屋、守護者は除く）を、選び方の違う4人の遊び手で通した計算です（`sim/sweep.py`）。

| 回復の設定（戦闘後 / 階移動 / 復帰 / 泉） | よく考える人 | 修正を使わない人 | いつも慎重 | いつも攻める |
|---|---|---|---|---|
| 0% / 50% / 25% / 50% | 8戦目で全滅 | 8戦目で全滅 | 8戦目で全滅 | 8戦目で全滅 |
| **25% / 50% / 25% / 50%**（採用） | **踏破。倒れた延べ5人** | 8戦目で全滅 | 11戦目で全滅 | 14戦目で全滅 |
| 25% / 50% / 50% / 50% | 踏破。延べ5人 | 8戦目で全滅 | 踏破。延べ13人 | 踏破。延べ15人 |
| 34% / 50% / 25% / 50% | 踏破。延べ3人 | 踏破。延べ8人 | 11戦目で全滅 | 14戦目で全滅 |
| 50% / 50% / 25% / 50% | 踏破。延べ1人 | 踏破。延べ6人 | 踏破。延べ13人 | 踏破。延べ12人 |

読み取れること:

- 戦闘後の回復が無いと、誰がやっても3階（8戦目）で終わる。既存の盤は満タン前提で作られているため。
- 採用値は「考えれば踏破できる。同じ方針を押し続けると途中で終わる」境目。
- 復帰を 50% にすると、何も考えなくても踏破できてしまう（N4）。

**この計算の限界**: 「よく考える人」は各戦闘でその場の最善を選ぶ近似で、遠征全体の最善ではありません（実際の最善はこれより良い）。守護者戦、茸の番人、影こうもり、遺物は入っていません。人が遊んだ結果ではありません。

### 5.3 型

```csharp
namespace RogueTactics.Core.Expedition
{
    public sealed class PartyState {            // 戦闘をまたいで残るもの
        public IReadOnlyDictionary<string,int> Hp { get; }
        public PartyState AfterVictory(EncounterState end, RecoveryRule rule);   // 復帰 → ひと息
        public PartyState Rest(int percent);                                      // 泉・階移動
    }
    public sealed class RecoveryRule { public int RevivePercent = 25, BreathPercent = 25, FountainPercent = 50, DescendPercent = 50; }
    public sealed class ExpeditionState {
        public int RulesVersion; public int Seed; public int Floor; public int Step;   // Step = 階の中の何番目か
        public PartyState Party; public IReadOnlyList<string> Relics; public bool BalancedUnlocked;
    }
    public static class EncounterBuilder {
        // 部屋の定義と今の一行から、戦闘の初期状態を作る。唯一の入口。
        public static EncounterState Build(RoomDef room, PartyState party, IReadOnlyList<string> relics);
    }
}
```

- `Unit` には `internal Unit(string id, Role role, int maxHp, int hp, int attack, GridPosition position)` を足す。`0 < hp <= maxHp` を検査する。
- `EncounterState.Serialize()` は既に `MaxHp` と `Hp` を別々に書いているので、形式の変更は不要です。
- `FloorTraveller.Health` は既に戦闘の終わりの HP を持っています。`PartyState` はこれを置き換えるか、ここから作ってください。
- 回復の計算は整数のみ。`(max * percent + 99) / 100`。

### 5.4 テスト

- 満タンの一行で `EncounterBuilder.Build(G1 の部屋)` を作ると、`Fixture.Encounter(0)` と `Serialize()` が一致する。
- 最大 HP を超えない、0 以下で戦闘に入らない、倒れた者が必ず1以上で戻る。
- `sim/floors.py` の「よく考える人」の選択列を入力として、同じ HP の推移になる（計算と実装の一致を固定）。

## 6. 表示へ渡す情報を型にする

いま Presentation は `Detail` の文字列の前方一致で守りを判定しています（`"Guard reduced damage by 3"` など）。壊れやすいので、派生の値を足します。

```csharp
[Flags] public enum EventTags { None=0, Guarded=1, Lethal=2, Flanked=4, Counter=8, Overkill=16 }
public sealed class BattleEvent { /* 既存 */ public EventTags Tags { get; } public int Prevented { get; } public string Guardian { get; } }
```

| 値 | 決め方 | 表示が使う場所 |
|---|---|---|
| `Guarded` / `Prevented` / `Guardian` | 守りで被害が減ったとき。減った量と守り手の ID | 盾の光を守り手から出す。数字の横に「−3」 |
| `Lethal` | この被害で HP が 0 になった | 着弾の止めを長くし、続く `Death` を間を置かずに繋ぐ |
| `Flanked` | 挟撃の +2 が乗った | 斬撃の色を金に |
| `Overkill` | 与えようとした量が残り HP を超えた | 数字を少し大きく |

- どれも `Serialize()` に入れない。hash は変わらない。
- 完了条件: `HqPose` と `HqJuice` から `Detail` の文字列比較が無くなる。

## 7. 危機で止める条件を Core に移す

いまは Presentation が `Rules.Threatened` を見て止めています。条件を増やすと表示側に規則が漏れるので、Core に問い合わせの口を作ります。

```csharp
public enum PauseReason { None, AllyThreatened, GuardianTelegraph }
public static PauseReason Rules.ShouldPause(EncounterState s, Command c, int correctionsLeft);
```

- 修正が残っていなければ止めない（止めても何もできないため）。
- `GuardianTelegraph` は守護者の予告（[02](02_ENEMIES.md) §5）が出た最初の一度だけ。
- 自動で止まるのは接敵ごとに最大2回。それ以上はテンポを壊すので止めない。

## 8. 射程（未決。N1）

決まるまで実装しません。決まったときの形だけ書いておきます。

- `Unit.Range`（既定 1）。`IsLegal` の攻撃条件を `Distance <= Range` に。
- 候補: D（弓）3、A（魔法）2。反撃は隣接からの攻撃にだけ返す。
- 入れると移動の止まる位置が変わり、**全部屋の結果が変わります**。`sim/rooms.py` に `range` を足して、[03](03_DUNGEON_AND_ROOMS.md) の部屋を全部選び直してください。
