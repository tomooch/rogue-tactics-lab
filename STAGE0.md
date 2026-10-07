# Stage 0 — Local Unity 環境監査（2026-10-07 JST）

契約: [GitHub Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。最初に本文を読み、Stage 0 のみを対象にした。これは環境監査の記録であり、Unity bootstrap の完了報告ではない。

## Git baseline

- canonical: `/Users/tomoki/Desktop/WorkSpaces/rogue-tactics-lab`
- origin: `https://github.com/tomooch/rogue-tactics-lab.git`
- `git fetch origin main` 成功。
- canonical `main` / 最新 `origin/main`: `0d8a2a68c37902256f5123ef86017ce83a60a64e`
- 開始時 `git status --porcelain=v1` は空。canonical のファイルは編集しない。
- worktree: `/Users/tomoki/.codex/worktrees/issue-1-unity-bootstrap/rogue-tactics-lab`
- branch: `codex/issue-1-unity-bootstrap`（上記 origin/main から作成）

## Unity 環境と停止理由

標準配置と Spotlight 検索で Unity Hub / Unity Editor を検出できなかった。

調査先:
- `/Applications`、`~/Applications`、`/Users/Shared`、`/opt`（名前に unity を含む項目、深さ5まで）
- `/Applications/Unity Hub.app`、`~/Applications/Unity Hub.app`
- `/Applications/Unity/Hub/Editor`、`~/Applications/Unity/Hub/Editor`
- `/Users/Shared/Unity`、`/Library/Unity`、`/opt/Unity`
- `~/Library/Application Support/UnityHub`、`~/Library/Application Support/Unity`
- Spotlight bundle ID: `com.unity3d.UnityEditor`、`com.unity3d.unityhub`

上記の個別配置先は存在せず、検索にも該当結果なし。任意のカスタム配置を完全に否定する監査ではない。

- exact Unity Editor version: 未検出（6000.3.x の patch は未確定）。
- iOS Build Support: 未検出、利用可能性を確認できない。
- Android Build Support: 未検出、利用可能性を確認できない。

別バージョンへの代替、インストール、ライセンス操作は実施していない。Editor で生成する条件を満たせないため、`unity/`、`.meta`、`ProjectVersion.txt`、Scene、URP 設定、asmdef を手書きで仮作成していない。

## 再開条件と残作業

ユーザーが Unity Hub と Unity **6.3 LTS (6000.3.x)** を導入し、必要なサインイン／ライセンス操作を完了する。Hub の Editor モジュールで iOS Build Support と Android Build Support（SDK & NDK Tools / OpenJDK）を導入する。既にカスタム配置している場合は Editor の絶対パスを提示する。

その後、この worktree で以下を継続する:

1. 実 Editor の exact patch / モジュールを確認し、Editor で `unity/` に URP プロジェクトを生成する。
2. 指定の Assets/RogueTactics 配下構造、UnityEngine に依存しない Core asmdef と Presentation 境界、EditMode test scaffolding を作成する。
3. portrait / 390×844 reference、iOS / Android 前提、orthographic の空 Scene を設定する。landscape UI と A/B/C/D/E の具体挙動は実装しない。
4. import / compile / EditMode tests / Play Mode smoke を実行し、可能な batchmode evidence を保存する。
5. Stage 0 の完成差分を self-review、commit / push する。merge はしない。

## 検証

- 既存 root `npm test`: 11/11 成功、fail 0、exit 0。Node v22.18.0。専用 worktree で既存テストを変更せず実行。
  - Local evidence: `/private/tmp/rogue-tactics-issue1-npm-test-20261007/completion.json` と `stdout.log` / `stderr.log`（一時領域、Git 管理外）。
- Unity import / compile / EditMode / Play Mode / batchmode: 実 Editor 未検出のため未実行。
- real-device iOS / Android: 未実行。署名・export・実機検証は今回の対象外。
- GitHub CI: 既存 workflow は main push / workflow_dispatch のみ。Draft PR 作成だけでは実行されない。CI 設定、secret、有料サービスは変更しない。

## Self-review / USER_DECISION

今回の差分は本監査記録のみ。既存 Web v0 とテストを変更せず、Godot PR #2 のコード・履歴・`mobile/` を流用していない。Core 自体を未作成のため Unity 依存の混入もないが、asmdef 境界の実検証は残作業。

以下を未決のまま保持し、実装しない:
- 探索操作とプレイヤーの立場（A 自身か盤面外かを含む）
- 1回の委譲で進める解決範囲
- 意図を何手先まで見せるか

これらは gameplay 実装前の USER_DECISION。Stage 0 再開に必要なのは Unity 環境であり、gameplay の決定ではない。
