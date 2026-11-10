# Native mobile の設計方向

正本は [Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。本書は契約の要点であり、実装完了の記録ではない。Web v0 は retired historical evidence。[当時の設計](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/DESIGN.md) は immutable snapshot `0d8a2a68c37902256f5123ef86017ce83a60a64e`（tag `archive/web-v0-final`）に保持し、active tree に Web implementation を持たない。Pages は current product / runtime ではなく、Human が Unpublish 済み。PR #4 は merge 済みで、Web v0 は retired 済み。accepted implementation は常に actual GitHub `main` HEAD。[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) の Unity Stage 0 は main へ merge 済み（merge commit `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）。実行済みテストは PR #5 の旧検証 HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af` に紐付き、その revision で Unity 6000.3.25f1、import / compile PASS、EditMode 5/5 PASS、PlayMode 1/1 PASS、390×844 portrait smoke PASS。証拠は [VERIFICATION.md](VERIFICATION.md)。merge commit や今回の docs HEAD の再実行結果ではない。実機未検証、CI なし、human play 未実施。Stage 0 の成立を gameplay 仮説や frozen direction の採用証拠にはしない。

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

1. 探索操作とプレイヤーの立場: 盤面外から目的地を指示するか、A 自身を操作するか、別方式か。
2. Resolution chunk: 1回の委譲でどこまで自律行動が解決され、どこで次の判断に戻るか。
3. Intent horizon: 次の1行動か、複数手先か。

PR #5 の Stage 0 は Unity 基盤のみ。上記 USER_DECISION を固定する gameplay、署名・実機検証は含まない。
