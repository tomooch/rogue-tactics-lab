# 設計用の計算

詳細設計（[specs/](../specs/)）の数値の根拠にした計算です。製品には入りません。Python 3 の標準ライブラリだけで動きます。

## これは何か

`core_sim.py` は、`unity/Assets/RogueTactics/Core/G1Battle.cs` の規則を Python に写したものです。盤面や数値の候補を大量に試して、「どの方針が効くか」で選り分けるために使います。Unity を開かずに数秒で回せます。

**写しが正しいことの確かめ**: 既存の EditMode テスト（`GauntletCoreTests`）が固定している結果と一致することを確認しています。

| 既存テストの事実 | この計算の結果 |
|---|---|
| 1戦目・慎重: 勝利、4人生存 | 一致（シロ HP 16。2026-11-17 に Simulator でも同じ値を観察） |
| 2戦目・慎重のみ: 3人生存 / 攻める: 4人 / 慎重→危機で修正: 4人 | 一致 |
| 3戦目: どちらの方針でも4人未満。慎重→修正でも4人未満。攻める→修正で4人 | 一致 |

hash の一致までは確かめていません（イベントの文字列を写していないため）。**規則の解釈が Core と食い違っていたら、正しいのは Core です。** Core を変えたらこの写しも直してください。

## 入っているもの

| ファイル | 何をするか | 出力が載っている場所 |
|---|---|---|
| `core_sim.py` | 規則の写し。既存3戦の結果を表示 | — |
| `screen.py` | 危機の判定と、修正1回を使った場合の結果。バランス方針の3案の比較 | [specs/01](../specs/01_CORE_RULES.md) §3 |
| `rooms.py` | 盤面を無作為に 6 万個作り、方針ごとに選り分ける（`rooms.json` を出力。Git には入れない） | [specs/03](../specs/03_DUNGEON_AND_ROOMS.md) |
| `pick.py` | 選り分けた中から 11 部屋を選ぶ（`picked.json`） | 同上 |
| `catalog.py` | 部屋の一覧を文書の形で出力 | [specs/03](../specs/03_DUNGEON_AND_ROOMS.md) §3 |
| `floors.py` | 5階・14戦を HP 持ち越しで通す。遊び手5通り | [specs/01](../specs/01_CORE_RULES.md) §5 |
| `sweep.py` | 回復の規則を総当たり | 同上 |
| `orders.py` | 階の中の部屋の順番を全通り試す（`good_orders.json`） | [specs/03](../specs/03_DUNGEON_AND_ROOMS.md) §1.2 |
| `relics.py` | 遺物ごとに、14戦×3方針の結果がどう変わるか | [specs/04](../specs/04_RELICS_AND_GEAR.md) §2 |
| `pacing.py` | 戦闘ごとの手数、最初の一撃・最初の危機までの手数、演出の秒数の見積もり | [overall/01](../overall/01_EXPERIENCE.md) §3 |
| `answers.py` | 一行の状態を変えたとき、部屋ごとの最善の方針が変わるか | [overall/05](../overall/05_AGENCY.md) §1 |
| `routes.py` | 部屋の順番と泉の時機をプレイヤーが選ぶ場合の結果 | [overall/05](../overall/05_AGENCY.md) §3.3 |

## 回し方

```bash
cd design/sim && python3 floors.py
```

`rooms.py` → `pick.py` → `catalog.py` → `floors.py` → `orders.py` の順に依存しています。部屋や数値を変えたら、変えたところから後ろを全部回し直してください。`rooms.py` は seed を固定しているので、同じ結果が出ます。`rooms.json` は Git に無いので、`pick.py` から回すときも先に `rooms.py`。時間の目安（2026-11-18、Mac）: core_sim 0.2 秒、floors 1.3 秒、relics 1.4 秒、routes 38 秒、orders 10 分前後。

## 限界

- **人が遊んだ結果ではありません**。「よく考える人」は各戦闘でその場の最善を選ぶだけの近似です。面白いかどうか、読めるかどうかは分かりません。
- 入っていないもの: 茸の番人、影こうもり、守護者、装備、射程。これらの数値は未検証です。
- 遺物を持った状態での遠征全体は計算していません（遺物1つずつの効果だけ）。
- 「狙いを1人に指定する」修正（`TargetOnce`）は入っていません。修正は方針の変更だけを試しています。実際のプレイヤーにはもう1つ手があるので、計算より少し有利です。
