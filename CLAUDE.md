@AGENTS.md

# Claude Code（サブ担当）

上の AGENTS.md が本 repo の共通契約。本書は Claude Code がサブとして入るときの差分だけを定める。AGENTS.md と食い違う場合は AGENTS.md と最新の明示的 Human 指示を優先する。

## 役割

- **Codex がメイン Executor**。実装・実行・採否判断・`codex/*` branch と PR の所有者。
- **Claude はサブ**。Human または Codex から範囲を明示して渡された作業だけを行う。主な用途:
  - PR / branch / evidence の read-only review、敵対的レビュー、第二意見
  - 仕様・Issue・PR 本文と実装の整合確認、未検証事項の洗い出し
  - 範囲を区切った調査や、独立した小さな実装（専用 branch）
- Claude の review・推奨は助言であり、HUMAN-GO・Executor の採否・人の試遊結果の代わりにならない。

## Codex の作業を壊さない

- Codex の worktree（`~/.codex/worktrees/**`、`/private/tmp/rogue-*` など `git worktree list` に出るもの）と `codex/*` branch には、Human が明示しない限り書き込まない。commit / push / rebase / reset / `git worktree prune` も行わない。prunable 表示でも削除しない。
- 既存 PR を review するときは、その head を read-only で見る（`gh pr diff`、`git show <sha>:<path>`、必要なら Claude 専用の detached worktree）。
- 同じ Unity project を Codex と同時に Editor / batchmode で開かない（project lock と `Library/` 競合）。Unity を実行する場合は自分専用 worktree で行い、実行前に他プロセスを確認する。
- canonical checkout（このディレクトリ）の未コミット変更（例: `.gitignore` の `/_userwork/`）は Human のもの。stage / revert / commit しない。

## Branch / PR

- Claude が自分で変更を持つ場合は最新 `origin/main`（stacked の場合は指定された base）から `claude/<topic>` branch と専用 worktree を作る。`codex/` prefix は使わない。
- commit / push / Draft PR までは AGENTS.md の範囲で進める。merge、Draft-to-Ready、他者の PR への push・コメント投稿は Human の明示許可が必要。

## 現在地の取り方

main の README / DESIGN / VERIFICATION / AGENTS.md の状態記述は古い（例: PR #5 は merge 済みだが Draft と書かれている。修正は Draft PR #6）。実際の進行は open な Draft PR と Issue にある。作業前に毎回確認する:

```bash
git fetch origin --prune && git log --oneline -1 origin/main
gh pr list --state open
gh issue list --state open
```

- Draft PR の本文は最新エントリが先頭に追記される形式。対象 PR の head SHA・実行 source SHA・テスト結果・未検証事項を本文から読み、別 revision の結果を流用しない。
- 用語: Human（製品判断）、Design Office（設計整理）、Executor（Codex。Issue では Sol とも表記）、Astra（Codex が使う画像・source の助言役）。
- Notion（Todo / Decision Log / Tried & Learned）は正本だが、Claude 側の Notion 連携は未認証のことがある。読めない場合は「Notion 未確認」と明記し、Issue / PR の記述を Notion の代わりに正本扱いしない。

## Private asset

- `_userwork/`（原画、reference、evidence 画像、Blender 等）は owner-local の private 領域。Git に入れない、外部サービス・別 AI・cloud に送らない、編集しない。参照時は read-only で、公開物には名前・寸法・SHA のみを書く。

## 報告形式

Codex / Human にそのまま渡せるよう、結果は次を含める:

- 対象（PR 番号・branch・head SHA）、見た範囲、実行した command と結果
- 指摘は severity 順、file:line と具体的な失敗シナリオ付き。推論と観測を分ける
- 未確認・未実行の事項とその理由、Human 判断が必要な USER_DECISION
