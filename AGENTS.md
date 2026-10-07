# Agent Operating Contract

## Source of Truth

事実の種類ごとに、以下の正本を確認する。

- **latest explicit Human judgment** = user intent / UX / priority。最新の明示的な Human 判断が今回の範囲と許可を定める。候補・提案・未決事項を承認済み仕様に読み替えず、上位の安全・権限境界を維持する。
- **accepted implementation** = GitHub main。実際の main HEAD と内容を確認する。
- **pending implementation** = exact PR / branch HEAD。PR 番号・branch・commit SHA を特定し、未 merge の実装を accepted main と混同しない。
- **current design / experiment contract** = root [README.md](README.md) / [DESIGN.md](DESIGN.md) / [VERIFICATION.md](VERIFICATION.md) / applicable Issue。現在は [GitHub Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1) が適用される。着手時に最新の本文と Human 指示を読む。設計方向と実測済み evidence・未検証事項を区別する。
- **runtime behavior** = verified execution / preview evidence。観測した revision・環境・操作・結果の範囲で判断する。
- **automated result** = target commit CI / reproducible local evidence。対象 commit と実行条件を特定し、別 revision の成功を転用しない。
- **human play result** = 実際に記録された人試遊のみ。自動テスト・agent の preview 操作から人の試遊結果を推定しない。
- **Todo** = Notion Todo。
- **important WHY** = Notion Decision Log。
- **reusable lesson** = Notion Tried & Learned。

Historical conversation / old docs だけから current state を断定しない。現在の正本や evidence にアクセスできない場合は未確認と明示する。

本書は agent の運用契約であり、実装完了や追加 scope の承認ではない。docs、Issue、実装の食い違いは証拠で確認し、material な未解決の矛盾は Human に示して依存する作業だけ止める。許可済みの独立作業は進める。

Issue #1 の旧 Web 維持 / npm regression 記述は、最新の明示的 Human 判断による retirement に置き換える。historical evidence は immutable commit `0d8a2a68c37902256f5123ef86017ce83a60a64e` / tag `archive/web-v0-final` で参照し、current tree に保持しない。PR #4 は merge 済み。accepted implementation は常に actual GitHub `main` HEAD。PR #5 は merge 済み（merge commit `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`）で、Stage 0 の基盤は main に取り込み済み。実行済みテストは PR #5 の旧検証 HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af` に紐付け、merge commit や以後の HEAD で再実行した結果と混同しない。

## Design Office / Executor / Human

- **Design Office**: 仮説、制約、候補、tradeoff、受入条件、実験の問いを整理する。USER_DECISION と提案を区別し、Human の製品判断や実測 evidence を代行しない。
- **Executor**: 承認された contract と scope を実装・検証し、adversarial self-review、修正、commit / push、Draft PR と evidence packet を担当する。仕様・数値・受入条件を黙って変えない。
- **Human**: 製品判断、未決 USER_DECISION、scope の重大な変更、実験から frozen direction への採用、外部公開・merge 等の承認を担う。人の試遊と実機でしか判断できない結果を agent の自動検証で代替しない。
- 役割は責任の区分。別チャット作成、他チャットへの送信、外部への連絡を自動的に許可するものではない。

## HUMAN-GO boundary

- HUMAN-GO は、Human が対象・範囲・効果を明示して許可したこと。agent の提案、テスト成功、自己評価、Design Office の推奨、Human の沈黙は承認ではない。
- 未決 USER_DECISION を固定する gameplay 実装や frozen direction の変更には Human の決定が必要。決定待ちの箇所を実装で埋めない。
- merge、default branch への直接 push、force push、共有履歴の書換え、branch 削除、Draft-to-Ready、auto-merge / merge queue、deployment / release / 外部公開、production-state write、権限変更、課金・有料サービス・新しい hosted dependency、secret / credential の導入、破壊的操作には明示的な許可が必要。
- 承認済みの範囲内の編集・必要な検証・専用 branch への task-owned commit / push は進める。同じ対象と効果への有効な許可を再度求めない。検証成功だけで追加の権限を得ない。
- 2026-10-08 の明示的 Human 指示で Unity Stage 0 を再開。PR #3 は closed / unmerged / historical audit evidence、branch 保持。PR #5 は main へ merge 済みで基盤のみ成立。Unity 6000.3.25f1、import / compile PASS、EditMode 5/5 PASS、PlayMode 1/1 PASS、390×844 portrait smoke PASS の対象 HEAD は `6a0879e8877cf0717d2ac5019a923ea11fbbd0af`。実機未検証、CI なし、human play 未実施。[Design Office comment](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298) と最新 Human 指示で4文書の事実同期を承認済み。gameplay scope 拡大・merge・Draft-to-Ready の承認ではない。

## Core hypothesis

> 自分の予想を仲間に託し、その戦いから理解を深め、次の可能性を試すRPG。

自発的変更案 → 実際の変更 → 事前予想 → 戦闘 → 行動に使える理解の更新 → 次の自発的変更案、を検証する。見栄え、content volume、自動テスト成功だけで仮説が成立したと判断しない。

## Current frozen direction

以下は承認済みの方向であり、実装済みという意味ではない。

- 最終製品は iOS / Android native game。Unity 6.3 LTS (6000.3.x)、C#、URP。実装先は `unity/`。
- portrait only、390×844 reference。landscape UI は作らない。小さな2.5D / diorama、orthographic または near-orthographic。
- 探索と戦闘は同じ field。別 battle screen にしない。A / B / C / D は on-board、E は compact off-board UI のみ。E を pawn や正解を示す oracle にしない。
- deterministic な離散 turn / step。ATB、real-time countdown、時間圧入力、各キャラの Attack / Heal command menu は採用しない。
- Core が意図を生成し、Presentation が line / semantic icon / movement destination ghost を描く。表示手数は未決。
- encounter ごとに味方1体の意図修正を1回使用可能。残数を明示し、次の distinct encounter で reset。広告・報酬で修正回数を増やさない。
- Godot PR #2 は closed / unmerged。`mobile/` のコード・履歴を流用しない。

## Unresolved USER_DECISION

以下は候補の比較段階であり、gameplay 実装前に Human が決める。

1. **探索入力 / player embodiment**: 盤面外から目的地を指示するか、A 自身を操作するか、別方式か。destination-tap や direct-A-control を確定仕様にしない。
2. **Resolution chunk**: 1回の委譲でどこまで自律行動が解決され、どこで次の判断に戻るか。
3. **Intent horizon**: 次の1行動か、複数手先か。次の1行動という候補も未承認。

Stage 0 の環境監査・bootstrap はこれらを固定しない範囲に限る。Stage 0 の基盤 bootstrap は PR #5 で成立し、main へ merge 済み。決定を得た際は根拠となる Human 指示と contract 更新を記録してから依存する実装へ進む。

## Experiment governance

- Issue #1 の高レベル party policy「最初の slice で最大3つ（バランス・慎重・攻める）」は **experiment parameter / prototype candidate**。隠れた毎ターン micromanagement にしないという実験上の制約とともに評価する。frozen product decision ではなく、Human が最終採用したとは扱わない。
- 実験では問い、比較する変更、保持する条件、観察対象、採用・棄却判断の根拠を明示する。仮説、実測、推論、未検証を分ける。
- interaction USER_DECISION の確定後、承認された experiment build で target / destination / hold-cancel / legal action alternative の correction superset を試す。Core の明示的 command と合法性判定に従い、play evidence から削減・簡略化する。
- correction を永続 gambit / rule editor に変えない。E によって利用可能な intervention family を変える strategist-dependent intervention hypothesis は実験設定として扱い、隠れた戦闘結果の変更や最終 class system として固定しない。
- 実験の成功を frozen direction への自動採用にしない。Human の判断を得る。実験を口実に scope、課金、telemetry、analytics、外部サービスを追加しない。
- 試遊では推薦や正解を押し付けず、自発的変更と事前予想、その後の行動に使える理解を観察する。human play evidence のない状態で面白さや因果理解を証明したと書かない。

## Unity architecture invariants

- pure C# Core は MonoBehaviour / GameObject / Animator / Unity scene state に依存しない。Core / Presentation の境界を asmdef で分離する。
- Core は合法移動、turn order、target selection、意図、damage / guard / flank / heal / counter / death、reward effects、win/lose、event output を所有する。
- Presentation は Core の state / events / plan を描画する。独立したルール判定、偽の意図修正、致死後の不可能な効果を作らない。animation timing が Core 結果を変えない。
- 同じ initial state + input sequence から同じ state / event sequence を得る。rendering なしで simulation を実行でき、seed / replay / state serialization を将来阻害しない。
- 実 Unity Editor でプロジェクトを生成する。検出した exact 6000.3.x patch を pin し、別 version に代替しない。Unity 生成 metadata を手書きで偽造しない。
- signing keys、provisioning profiles、Apple certificates、Android keystores、store credentials を commit / log に含めない。

## Local vs Cloud routing

- **Local Mac**: Unity Hub / exact Editor patch / build modules の監査、実 Editor での生成、import / compile、EditMode / Play Mode、portrait UI、device / signing 関連の観測を担当する。利用可能な環境で実測する。Human の license / credential 操作が必要なら必要事項を明示する。
- **Cloud**: repository / docs review、環境で実行可能な deterministic tests、pure C# Core の検証など、Unity GUI や Local credential を要しない承認済み作業に使える。Cloud の成功を Local Unity / 実機成功に読み替えない。
- routing は環境適性の判断であり、別チャット作成や外部 I/O の許可ではない。環境不足は未検証として記録し、別 version・有料 CI・secret 追加で迂回しない。

## Git / worktree workflow

- 変更前に git status / remote を監査し、origin/main を fetch。新規タスクは最新 origin/main から専用 worktree / `codex/` branch を作る。既存 PR 更新はその専用 worktree / head branch を使い、無断で rebase / history rewrite しない。
- canonical main を直接編集しない。unrelated user changes を上書き・stage・commit・削除しない。task-owned 差分だけ commit / push する。
- 実装 → 必要な検証 → adversarial self-review → 修正 → commit / push → Draft PR → 利用可能な CI 結果確認。既存 PR 更新ではその PR に push し、説明を最終 scope に合わせる。
- **no auto merge**。merge / main push / Draft-to-Ready は Human の明示許可が必要。PR #4 merge 後の accepted main と PR #5 に Pages deployment workflow はない。Pages は Human が Unpublish 済み。

## Verification requirements

- 承認された task の受入条件と、重要な失敗を区別できる証拠を決める。観測可能な環境では implement → run → observe → compare → repair → rerun を完了する。
- current repo に Web npm harness はない。retirement は Git diff / tree、historical commit / archive tag の参照、Web runtime / source / assets / tests / build scripts と Pages workflow の不在を確認する。旧 npm コマンドを current requirement にしない。
- Unity Stage 0 の基盤は main へ merge 済み。import / compile / EditMode scaffolding / portrait Scene Play smoke の実行済み証拠は PR #5 の旧検証 HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af` に紐付く。対象 revision・command・結果・限界は VERIFICATION.md を参照し、今後の変更に応じて必要な再検証を行う。後続 gameplay は Issue #1 の pure Core gate と one-floor acceptance を満たす。Stage 0 に後続 gameplay の実装を混ぜない。
- CI は既存設定と許可の範囲で使う。paid CI、license secrets、新しい recurring cost を追加しない。current tree に CI workflow はまだない。実行されていない CI 成功を主張しない。
- branch、baseline / HEAD SHA、PR URL、changed files、実行した command と結果、environment / exact Editor、未実行事項と理由、USER_DECISION / blocker を報告する。
- mocks / tests / HTTP smoke / Editor / simulator / real device / human play の証拠の範囲を区別する。観測していない UI・公開 deploy・実機成功を主張しない。

## Retired historical Web evidence

- Web v0 の最終 accepted evidence は immutable commit `0d8a2a68c37902256f5123ef86017ce83a60a64e` と annotated tag `archive/web-v0-final`。source / tests / assets / 当時の docs は Git history から参照する。
- current main を過去 prototype 運用のために汚さない。active tree に legacy/web-v0、Web runtime、npm wrapper、Pages artifact / deploy workflow を復活させない。再現は archive の別 checkout で行う。
- historical automated / browser evidence は native runtime・人試遊・current execution の evidence へ誤変換しない。過去の revision と現在の観測対象を特定する。
- Pages は current product / runtime ではなく、Human が Unpublish 済み（[Human 報告](https://github.com/tomooch/rogue-tactics-lab/pull/5#issuecomment-6042899298)）。workflow 削除による停止とは扱わない。外部設定の追加変更は明示的な許可の範囲で行う。

## Todo / Decision / Tried & Learned governance

Todo は Notion Todo、重要な WHY は Notion Decision Log、再利用できる lesson は Notion Tried & Learned を正本とする。Issue / PR と既存 docs には関連する contract・実装・検証 evidence と正本への参照を残す。新しい docs 階層や管理サービスを増やさず、記録を作業の承認と取り違えない。Notion にアクセスできない場合は未同期と明示し、別の記録を正本に昇格させない。

- **Todo**: scope、受入条件、担当 role、依存する USER_DECISION、status / blocker を記録する。未承認案は実行対象にしない。完了は実測 evidence と紐付ける。
- **Decision**: Human が決めた内容、理由、対象 scope、日付、元の指示 / Issue / PR を記録する。proposal / hypothesis と accepted decision を分け、既存 contract と矛盾する場合は整合させる。新しい material evidence がない限り settled decision を反復審議しない。
- **Tried & Learned**: 問い、試した条件、command / revision / environment、観測結果、限界、得た理解と次の判断を記録する。失敗・未検証・停止理由も残し、同じ試行を新しい根拠なしに繰り返さない。
- Todo / WHY / lesson は上記 Notion の各正本に、task の実行詳細と evidence は Issue / PR に残す。一時ログへのリンクは一時領域であると明示し、secret / 個人情報を含めない。
