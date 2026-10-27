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
