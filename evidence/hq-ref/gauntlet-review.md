# Gauntlet experiment: consecutive encounters, intent round, crisis stop (2026-10-27)

Author: Claude Code (sub agent), pushed to PR #21 on the explicit Human instruction to push there directly.
Base: `b33eb5b` (visual polish), itself on Codex HEAD `7ec0552`.

## Human decisions in this session (2026-10-27, Claude Code chat)

- "フィールドはそれなりでいいです。メインは楽しさを考える　作る　です。"
- First fun core to build: **連戦で学びを試す** (chosen over "意図修正の一手" and "予想当て").
- Intent horizon (USER_DECISION 3): **一巡（全員の次の手）**.
- Resolution chunk (USER_DECISION 2): **危機で自動停止** — auto play continues, stops briefly only when a correction is still available and an ally is newly threatened; manual pause stays available.

These answer two open USER_DECISIONs for this experiment build. They are Human choices, not play evidence.
Notion Decision Log is **not synced** (Notion is not authenticated in the Claude session).

## What was built

Core (pure C#, `G1Battle.cs`, `DungeonFloor.cs`):

- `EncounterState` takes an explicit per-encounter initiative. The implied G1 order is not serialized, so G1 states, replays and both frozen hashes stay byte-identical (`e6f35225…`, `85770bb4…`).
- `Fixture.Encounter(0..2)`: 0 is the frozen G1 fixture; 1 and 2 reuse the same four allies with unchanged stats and place two guardians differently.
- `Rules.Intents`: every living actor's present plan in acting order, each read from the current state (what they mean to do now, not a simulated future).
- `Rules.Threatened`: allies whose HP would be exhausted by attacks enemies currently intend (guard reduction applied).
- `Replay.Correct`: the encounter's single correction of B's preference, recorded in the command stream.
- `GauntletRooms`: encounter 1 sits in floor room 1, encounter 2 in room 2 (5x5 board centred in the 7x7 room). `FloorTraveller` accepts the board origin for the battle-to-exploration handoff.

Presentation (`HqGauntlet.cs` plus small origin plumbing):

- Exploration scene only. After a victory and exploration, reaching the next guardian room offers **守り手に挑む（n / 3戦目）**. Nothing starts a fight automatically.
- Allies walk into the room's formation; guardians rise on their Core cells; the camera eases to the board. Same field, no separate battle screen.
- From encounter 2: dashed straight arrows show every living actor's present intent (next actor at full strength, later ones fainter); auto play stops at a new threat while the correction remains; a red action offers the one correction; **再開** continues without it.
- Encounter 1 keeps the frozen G1 flow exactly: no crisis stop, no round arrows. New mechanics are introduced one encounter later.
- Allies start each encounter at full HP (experiment parameter, not a product decision).

## Encounter design evidence (Core, outside Unity via bundled Mono)

| Encounter | B: protect only | B: attack only | Correction at first crisis |
| --- | --- | --- | --- |
| 1 (G1) | Victory, 4 alive | Victory, Shiro falls (step 11) | Attack→Protect at step 5: Shiro still falls |
| 2 | Victory, Shiro falls (step 27) | Victory, 4 alive | Protect→Attack at step 16: 4 alive |
| 3 | Victory, Shiro falls (step 29) | Victory, Shiro falls (step 28) | Attack→Protect at step 13: 4 alive, Shiro HP 1. Protect→Attack: Shiro and Garu fall |

Intended learning arc (hypothesis, not observed): 1 "protecting Shiro matters", 2 "protecting alone gets ground down", 3 "press, then switch to protect when Shiro is threatened".
Layouts 2 and 3 came from a seeded search over legal layouts with no ally/enemy adjacency at start; they are experiment fixtures, not balanced content.

## Verification (Unity 6000.3.25f1, Local Mac)

- New EditMode `GauntletCoreTests` (6): frozen G1 identity and hashes, encounter 2/3 outcome table above, single recorded deterministic correction, read-only intent/threat queries, boards inside their rooms.
- New PlayMode `GauntletFlowTests.ConsecutiveEncountersOnTheSameFloorWithCrisisCorrection`: G1 win with frozen hash and no crisis stop → explore → walk to room 1 through visible touch targets → challenge → each unit on its room cell → crisis stop with ≥3 intent arrows → correction → 4 alive → explore → room 2 → same with Attack start → "3つの戦いを越えた！".
- Full suite on the committed source: **EditMode 27/27, PlayMode 13/14**. The single failure is the pre-existing Y/C whole-sprite overlap in `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults`, identical to baseline `7ec0552` (Edit 21/21, Play 12/13).

## Known limits and open issues

- No human play. Whether the arc is fun or learnable is unverified.
- No physical iPhone or Simulator run of this build.
- Encounter 3: the existing order strip overlaps the cluster near the top of the board at the crisis frame.
- After a later encounter the exploration camera eases to the party (about 0.6 s); encounter 1 keeps the exact no-cut handoff.
- Dead allies from an earlier encounter return at the next encounter (full-HP parameter). HP carry-over is a natural next lever.
- The pre-existing `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults` Y/C whole-sprite overlap failure is unchanged.

## UI parts pass (2026-10-27, Human: "全部")

Scope chosen by the Human: companion cards, top HUD with the initiative strip, and the icon set, compared against `_userwork/image/天穹の遺跡、12階の冒険.png` (private, not committed).

- Icons: `HudGlyphGraphic.Gem` draws a white glyph with a soft halo on a lit, rimmed plate of the accent colour, with an upper gloss. Filled shield, play, star and descend glyphs. Used on every action button, the card role badges, the title emblem, the map search and the current-floor marker. Plates grow outward, so placement and touch targets are unchanged.
- Cards: taller portrait (94 pt) with the role gem in the top-right corner and the name right-aligned on a soft shade; then actual HP, a 7 pt bar (warm when actual HP is at or below 35%) and the role or Momo's policy. No level, skill or second gauge is shown: none exists in Core.
- Top HUD: pill title plate with a round star gem; dotted floor axis with a gold descend gem on the single current-floor marker. Still no invented floor number, total or locked floors.
- Initiative strip: moved to the sky gap beside the map (clear of the board at every encounter), compact tiles with ally/enemy rims and the next actor in gold.
- Fix: the strip previously listed the frozen G1 order in every encounter. It now reads `Session.Replay.State.Initiative`, so encounters 2 and 3 show their own Core order. G1 is unchanged (same order).

Not done: the reference's skill-icon row (no skills exist), Lv plates, the second gauge, the cat floor icon and floor numbers. The title remains "天空の遺跡" (the reference reads 天穹; the existing test pins the current name).

Verification of this pass (Unity 6000.3.25f1, Local Mac, uncommitted tree on `d152061`, Presentation source identical to the commit):

- Full suite: **EditMode 27/27, PlayMode 13/14**. The single failure is the pre-existing `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults` whole-sprite overlap. It now reports A/B instead of Y/C only because of object iteration order: a logged probe showed Y and C at the same projected rects as before (`237.05,-203.65` / `271.42,-174.11`), so the board geometry is unchanged.
- `GauntletFlowTests` gains a regression check: at each crisis stop, the visible order tiles equal a rotation of the encounter's living Core initiative. Rerun after adding it: 1/1 passed.
- Captures were reviewed for encounters 1 to 3, the policy choice, the dead-companion card and the map overlay. No physical device, Simulator or human play.

## Board readability pass (2026-10-27, Human: "ガンガンハイクオリティにしていってください")

- Round intents are glowing arcs from the actor's upper body to the target (or destination cell). The next actor's arc is solid and bright with its action kind as a gem on the apex; later intents are thinner dashed arcs. Geometry still comes only from `Rules.Intents`.
- The initiative strip starts at, and golds, the same next actor while round intents are shown, so the strip and the solid arc agree.
- Crisis stop: a pulsing red alert gem above each ally in `Rules.Threatened`.
- Encounter result: a gold banner between board and cards ("勝利！", survivors, and "3つの戦いを越えた" on the last). Display only; the existing terminal result label is kept.
- Foot HP bars: brighter fills with a gloss, same geometry.
- Paving shader: broad warm/cool stone families and sparse clover specks inside moss.
- Verification (Unity 6000.3.25f1, Local Mac): full suite **EditMode 27/27, PlayMode 13/14**; the one failure is the same pre-existing A/B whole-sprite overlap with identical rects. Gauntlet and reference captures reviewed. No Simulator, device or human play.
