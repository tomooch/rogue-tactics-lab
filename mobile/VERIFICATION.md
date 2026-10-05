# Mobile vertical slice verification

## Scope

This file applies only to the Godot project under `mobile/`. It does not modify or supersede the historical web prototype verification at repository root.

## Current automated gate

The GitHub Actions workflow `.github/workflows/godot-mobile.yml`:

1. runs the existing root `npm test` suite to catch regressions in the historical prototype;
2. downloads the pinned Godot 4.7.2 stable editor;
3. parses/imports the `mobile/` project headlessly;
4. runs `mobile/tests/run_tests.gd`.

The Godot test script checks deterministic state progression, A movement intent consistency, B guard behavior, C flank behavior, D heal behavior, relic effects, lethal no-post-death-counter behavior, and a canonical reachable stairs path.

## Required human/device verification before claiming the slice is validated

- iPhone portrait launch and touch selection
- Android portrait launch and touch selection
- no landscape product UI
- text legibility at narrow phone widths
- one complete real-device run through first encounter → relic → second encounter → stairs
- intent lines remain readable with four party members + multiple enemies
- strategist E never appears as a board pawn
- no ATB/countdown or per-character command requirement
- player can explain at least one observed cause/effect without being given the answer

No human playtest result is recorded here yet.
