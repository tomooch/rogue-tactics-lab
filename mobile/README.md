# Rogue Tactics Lab — Godot mobile vertical slice

This is the **native mobile game prototype** for the project. The repository root still contains the historical browser visibility experiment; this `mobile/` project does not replace its evidence.

## Engine

- Godot 4.7.2 stable
- GDScript
- Mobile renderer
- Portrait orientation (390×844 design viewport)
- Target products: iOS and Android

## Current play loop

The player is the party supervisor, not a fifth pawn on the board.

1. Tap a revealed dungeon tile to set the party's next exploration destination.
2. Review the four allies' visible next intents on the field.
3. Choose one high-level policy: Balance / Safe / Attack.
4. Advance one deterministic turn.
5. A/B/C/D act autonomously, then enemies act.
6. Open the chest and choose one of two deterministic relics.
7. Observe how the relic changes the second encounter.
8. Defeat the enemies and reach the stairs.

Combat and exploration use the same field. There is no battle-screen transition, ATB gauge, countdown, or per-character combat command menu.

## Party

- **A / Front** — advances toward the selected destination, attacks adjacent enemies, counters after a hit threshold, self-heals after counter damage.
- **B / Guard** — stays near A and reduces A's incoming damage when adjacent.
- **C / Skirmisher** — seeks flanking positions; gains a clear damage bonus when another ally is also adjacent to the target.
- **D / Support** — stays behind the group and heals allies below its policy-dependent threshold.
- **E / Strategist** — never appears as a board pawn; gives short risk/context notes only.

## Intent visual grammar

The renderer reads `GameCore.intent_plan()` and draws only Core-authored next-plan information:

- line = actor → target/destination
- compact icon = attack / guard / reposition / heal
- faint destination ghost = movement intent

The renderer never decides attacks, healing, counters, deaths, targets, or legal movement.

## Relics

- **Thorn Talisman** — A counters after 2 hits instead of 3 and counter damage becomes 7 instead of 5.
- **Pilgrim Bell** — D heal becomes 10 instead of 7 and heal range becomes 4 instead of 3.

## Run locally

Install Godot 4.7.2 and open `mobile/project.godot`, then Run Project.

Headless deterministic core tests:

```sh
Godot_v4.7.2-stable_linux.x86_64 --headless --path mobile --script res://tests/run_tests.gd
```

## iOS / Android

The prototype is engine-level and cross-platform. iOS export requires macOS, Xcode, Godot export templates, an Apple Team ID, and a bundle identifier. Android export requires the normal Godot Android SDK/export setup. Signing / TestFlight / Play release credentials are intentionally **not** stored in this repository.

## Not yet validated

- real iPhone / Android touch feel
- production art direction
- fun / readability with human players
- final party movement model
- procedural generation
- long-term progression
- AI planner integration

The current slice exists to answer a narrower question: **is it enjoyable to read autonomous ally intentions, choose a small high-level change, and watch the result on a seamless roguelike field?**
