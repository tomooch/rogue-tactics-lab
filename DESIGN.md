# Native mobile の設計方向

正本は [Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。本書は契約の要点であり、実装完了の記録ではない。Web v0 は retired historical evidence。[当時の設計](https://github.com/tomooch/rogue-tactics-lab/blob/0d8a2a68c37902256f5123ef86017ce83a60a64e/DESIGN.md) は immutable snapshot `0d8a2a68c37902256f5123ef86017ce83a60a64e`（tag `archive/web-v0-final`）に保持し、active tree に Web implementation を持たない。Pages は current product / runtime ではなく、Human が Unpublish 済み。PR #4 は merge 済みで、Web v0 は retired 済み。accepted implementation は常に actual GitHub `main` HEAD。[PR #5](https://github.com/tomooch/rogue-tactics-lab/pull/5) の Unity Stage 0 は Draft / pending implementation。実行した HEAD `6a0879e8877cf0717d2ac5019a923ea11fbbd0af` で Unity 6000.3.25f1、import / compile PASS、EditMode 5/5 PASS、PlayMode 1/1 PASS、390×844 portrait smoke PASS。証拠は [VERIFICATION.md](VERIFICATION.md)。実機未検証、CI なし、human play 未実施。Stage 0 の成立を gameplay 仮説や frozen direction の採用証拠にはしない。

## Core hypothesis

> 自分の予想を仲間に託し、その戦いから理解を深め、次の可能性を試すRPG。

自発的変更案 → 実際の変更 → 事前予想 → 戦闘 → 行動に使える理解の更新 → 次の自発的変更案、を観察する。演出の豪華さだけを最適化しない。

## アート方向：ポップで可愛らしい「ちび冒険者」（Human-approved reference, 2026-10-08）

**Humanが承認したのは、4人の顔立ち・目・耳・装備の方向性を含むキャラクタービジュアルの基準。** 2026-10-08に共有した4人のステータスカード画像を比較し、「顔がぜんぜん違う。目がぜんぜん違う」と修正を要求した後、丸く素直な目に寄せた版に「さいこうだああああ」、ガルに狼耳と小さな牙を足した最終比較画像に「ok！」と応答した。**以前のUnity PR #10で使用した別テイストの顔や目を、承認済みスタイルとみなさない。**

### 顔と描画の共通ルール

- **目が最重要の識別要素。** 参考カードと同じ、単純で丸みのある縦長の大きな虹彩・少数の控えめな光・柔らかい輪郭を基本にする。過剰なまつ毛、複雑な宝石状の虹彩、強い写実的なハイライト、きつい目元へ勝手に寄せない。
- 大きめの頭、丸い頬、ほんのり赤い頬、小さな鼻と口、短い胴・手足。**あどけなく、親しみやすく、表情豊かな「ポップで可愛い」ちびキャラ**。大人っぽさ・ギラギラした重厚ファンタジーに戻さない。
- 明快なシルエット、やさしいパステル寄りの配色、薄く繊細な輪郭、柔らかい塗り。縮小したスマホ実寸でも**目・表情・種族差が分かる**ことを優先する。キャラと環境の画風・陰影・光の方向も揃える。
- 4人は同じ絵柄で、それぞれ**目の色・髪型・耳・装備・衣装**で一目で区別できる。カプセル/箱を完成キャラと呼ばず、別の高精細アニメ顔に無断置換しない。
- **敵も「ちょっと毒がある、不思議・怪しい」個性を残しつつ可愛い。** 怖さや毒々しさを理由に写実ホラー、威圧的な巨大頭身に寄せない。

### 4人の基準デザイン

| 仲間 | 顔・耳・外観の基準 | 職能を示す視覚要素（スキル効果の仕様ではない） |
| --- | --- | --- |
| **シロ** | 白銀の柔らかい短〜中髪、大きくシンプルな青〜青紫の目、白花を添えた青い魔法帽、童顔 | 青と金の魔法服、杖・青く光る宝珠 |
| **ミャル** | 明るい黄〜茶の髪、緑の目、**大きめの猫／狐を思わせる立ち耳**、葉や白花の飾り。ガルの狼耳と区別 | 緑のマント、弓、森・植物を連想する装備 |
| **ガル** | 淡い灰茶〜銀灰のふわふわした髪、優しい茶系の丸い目、**ミャルとは異なる小ぶりの狼らしい三角耳**（灰茶色の外側と明るい内毛）、笑った口元に**見て分かる小さな牙**。狼っぽさを足すが怖くしない。耳をミャルの大きな猫系の耳と同形にしない | 青・白・金の軽装騎士、防具、剣と青い盾 |
| **モモ** | 桃色の髪、ピンク〜紫の丸い目、白と桃色の帽子と花飾り、優しい頬と笑顔 | 桃色・白の回復役風衣装、ハート形の杖 |

### 参考資料と制作・検証の境界

- **見本は実物の画像を優先：** 2026-10-08のHuman提示「シロ／ミャル／ガル／モモ」の4枚の横並びカードの切り抜き、顔と目を合わせ直した再生成版、ガルの狼耳・牙を反映した最終比較画像。画像や切り抜きの存在を確認せず、文面だけから勝手に「同じ目」を再解釈しない。
- 旧ビジュアル参考資料のローカル保存先は `_userwork/rogue-tactics-visual-references-2026-10-08/`（**Git除外の人間所有フォルダ、読み取り専用**）。**後から確定したガル狼耳版がその中に保存されているかは未確認**。実装に使う前に該当画像を照合して保存先を明示する。Gitに参考原本をコミットしない。
- **画像生成物・Unity素材・画面表示は別の成果物。** 人のイラストへの好意を、Unityの実描画品質・ゲームの楽しさの実証と取り違えない。Unityで使う際は元画像のスプライト分離、縮小時の目の見え方、輪郭/背景との一体感を**実際の390×844レンダー**で検証し、Humanが評価する。
- 既存の[Issue #9](https://github.com/tomooch/rogue-tactics-lab/issues/9) / [Draft PR #10](https://github.com/tomooch/rogue-tactics-lab/pull/10)は旧ビジュアル候補のUnity検証として保持する。**キャラクター顔・目のHuman visual acceptanceは未達**。本項はアート方向の決定であり、キャラの確定スプライト、アニメーション、ゲームスキル・入力ボタンや戦闘AIの採用決定ではない。
- 探索と戦闘は同じ縦画面の場を共有し、可愛いちびキャラが主役。UIに関する既知の好みは**6枚目の縦階層表示＋マップ、7枚目の各人2つの技能風アイコン、2枚目の弧状矢印**。スキル風アイコンの操作意味は未確定。アート実装で未決の `resolution chunk` / `intent horizon` を固定しない。

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
