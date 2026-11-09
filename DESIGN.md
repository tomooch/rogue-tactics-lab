# Native mobile の設計方向

正本は [Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。本書は契約の要点であり、実装完了の記録ではない。Web v0 は retired historical evidence。[当時の設計](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/DESIGN.md) は immutable snapshot `0d8a2a68c37902256f5123ef86017ce83a60a64e`（tag `archive/web-v0-final`）に保持し、active tree に Web implementation を持たない。Pages は current product / runtime ではなく、Human が Unpublish 済み。PR #4 は merge 済みで、Web v0 は retired 済み。accepted implementation は常に actual GitHub `main` HEAD。[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) のUnity Stage 0はmerge済み。以下はmerge前の実測revisionの記録。実行した HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af` で Unity 6000.3.25f1、import / compile PASS、EditMode 5/5 PASS、PlayMode 1/1 PASS、390×844 portrait smoke PASS。証拠は [VERIFICATION.md](VERIFICATION.md)。実機未検証、CI なし、human play 未実施。Stage 0 の成立を gameplay 仮説や frozen direction の採用証拠にはしない。

## Core hypothesis

> 自分の予想を仲間に託し、その戦いから理解を深め、次の可能性を試すRPG。

自発的変更案 → 実際の変更 → 事前予想 → 戦闘 → 行動に使える理解の更新 → 次の自発的変更案、を観察する。演出の豪華さだけを最適化しない。

## Major invariants

- iOS / Android native、Unity 6.3 LTS / C# / URP。portrait only、390×844 reference。landscape UI は作らない。
- 探索と戦闘は同じフィールド。別戦闘画面に切り替えない。小さな2.5D / diorama、orthographic または near-orthographic。
- A / B / C / D は on-board。軍師 E は off-board のコンパクト UI のみ。正解を示す oracle にしない。
- deterministic な離散 turn / step。ATB、実時間 countdown、時間圧の入力、各キャラの Attack / Heal メニューは採用しない。
- plain C# Core がルール・合法手・意図・結果・イベントを所有する。MonoBehaviour / GameObject / Animator / scene state に依存させない。
- Presentation は Core の状態・イベント・計画を描く。同じ初期状態と入力列から同じ状態・イベント列を得る。animation timing が結果を変えない。
- 意図は actor → target/destination の line、semantic icon、移動先 ghost で表す。通常プレイを4人同時の説明吹き出しに依存させない。表示 horizon は未決。

## 意図修正の実験

Issue #1 の高レベル party policy 最大3つ（バランス・慎重・攻める）は experiment parameter / prototype candidate。Human の最終採用や frozen product decision ではない。隠れた毎ターン micromanagement にしないという実験上の制約とともに評価する。

encounter ごとに味方1体の意図修正を **1回** 使用可能。残数を表示し、次の distinct encounter で reset。広告や報酬で追加しない。修正後も Core が次の行動を決め、Presentation が偽装しない。

USER_DECISION を確定した後の experiment build では、合法な場面で target / destination / hold-cancel / action alternative の correction superset を試し、play evidence に基づき削減する。Core の明示的 command と合法性判定を使い、永続 gambit / rule editor に変えない。

strategist-dependent intervention hypothesis: E の experiment configuration によって利用できる correction family を変える。隠れた戦闘結果の改変は行わず、最終 class system や正解提示として固定しない。

## 未解決 USER_DECISION

以下は gameplay 実装前にユーザーが決める。候補を承認済み仕様として扱わない。

1. 探索gestureと目的の表現等。盤外のparty監督役BはHumanが#1で承認済み。Aを直接操作する方向にはしない。
2. Resolution chunk: 1回の委譲でどこまで自律行動が解決され、どこで次の判断に戻るか。
3. Intent horizon: 次の1行動か、複数手先か。

PR #5 の Stage 0 は Unity 基盤のみ。上記 USER_DECISION を固定する gameplay、署名・実機検証は含まない。


## Design Freeze G1 — 2026-10-13 / Issue #14

最新の[実装契約 #14](https://github.com/tomooch/rogue-tactics-lab/issues/14)が今回の範囲を定める。Stage 0はPR #5でmainへmerge済み（baseline `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。Humanは盤外の監督役Bを承認済み（#1）。探索入力の正確なgesture、resolution chunk、intent horizonは未決。PR #6の広いdocs同期は取り込まず、Stage 0 mergeと盤外Bに限りstale記述を訂正した。

- 問い：同じ初期戦況からBの戦術選好だけを変えたとき、実行行動・HP・生存・行動数・回復資源に読み取れる差が出るか。Humanが次の変更を自発的に考えるかは人試遊でのみ判断する。
- 二条件：`ProtectAnchor`（生存Aに隣接して守る／近づく）と`DirectAttack`（共通の近接攻撃規則）。Bの数値や他役の規則、敵、初期配置、行動順は同一。どちらも推薦しない。
- fixture：5×5、A(2,2) HP18/攻撃5/生存被弾後counter2、B(1,2) HP16/攻撃4/guard軽減3、C(3,3) HP12/攻撃4/Aと同じ敵へ異なる隣接位置から攻撃時flank+2、D(1,1) HP10/攻撃2/射程2でHPが4以上欠けた最低HP味方を4回復（2charge）、X(2,3) HP18/攻撃6、Y(3,2) HP14/攻撃5。匿名実験役のみ。シロ魔法／ミャル弓／ガル大剣／モモ盾の最終能力とは対応させない。
- 行動順B→X→A→C→Y→D、死亡actorはskip。敵は生存Aを優先、その他は距離→ID。移動は空いた隣接マスで距離が縮むN/E/S/W順。fixture用であり探索routeの仕様ではない。guardはBの次行動で失効、死亡／離隔で無効。致死被弾後のcounter・死者回復なし。
- 予想は変更後・実行前に任意自由入力。Core規則を読む導線を設け、結論や正解は示さない。戦闘終了後は同一baselineへ戻して別条件を試し、独立の結果を併記する。
- 検証adapter：Core APIは1 actionの純粋transition。UIでは1 actionまたはencounter終端までの明示的検証操作と、現在planの可視化を用いる。これらはG1実験用adapterで、正式resolution chunk／intent horizonの既定ではない。
- 期待：同一入力でsnapshot・event・SHA-256が同一、合法行動のみ、単一選好による実行trace差、HP／行動数等の少なくとも1指標差、実390×844操作で開始・観察・比較・再試行できる。仮説や面白さ成立の観測とは区別する。
- stop条件：勝利／全味方死亡／120 actions上限。差が実規則で生じなければNOT ESTABLISHEDとしてfixtureを記録付きで修正し、偽の結果は作らない。
- 対象外：floor探索、宝箱／relic／第二戦、1 correction UI、E提案・受諾、正式class／skill、アニメーション、LLM、network、課金、telemetry、merge。G2とHuman試遊はNotion Todoに残す。
