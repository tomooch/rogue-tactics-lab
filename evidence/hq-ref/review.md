# HQ-REF-1 adversarial findings

Source: Issue #20 / audit comment 6184262380. Independent bounded source/image review, followed by root diagnosis and repair. This is agent review, not Human visual/fun acceptance.

| Finding | Observation and response |
| --- | --- |
| R1 contextual controls overflow safe area | Reviewer calculated Start/Compare extending beyond 763pt safe height. Repositioned shelf, added explicit button bounds and >=44pt checks. |
| Old pause test waited for a frozen queue to drain | New pause freezes the cursor immediately. Updated device test to inspect frozen state/cursor and resume before reset. Product test checks pause/resume without future-state exposure. |
| Developer Reset label wraps | Normal result CTA remained in narrow developer Reset. Restored short developer wording; focused safe-area battle regression passed. |
| R2 generic flat paving / tiny cast / missing vista | Actual 75/63 renders and independent image review failed V0. Revised authored geometry, framing, rectangular worn slabs, cast/portrait scale, filtering and actual scenery placement. No quality PASS inferred from colour pixels. |
| Water occluded by island | Water initially lay inside opaque island body. Moved visible cascade outside edge and changed camera composition as a separate comparison. |
| Mid-tween pause snaps actor to old cell | RefreshActors restored Core-cell pose on pause. Added active tween guard and visible geometry/HP/route synchronization; regression observes actual movement, freezes pose/cursor and resumes once. |
| Maintain can retain previous intent arrow | Clear old route/ghost at each new Action before drawing this Core plan. |
| Runtime VolumeProfile lifetime | Explicitly release the generated profile on scene teardown. Generated mesh/material/texture lifetimes are owned by scenery/creature components. |
| Foot HP touches adjacent foreground actor | Reproduced by full rendered-battle check. Place labels below their owner and search nearby horizontal positions against actual projected silhouettes and other labels; keep IDs and actual HP. |

Residual visual fidelity and Human preference must be assessed on the final source-linked actual Unity/native captures. True authored sprite gait (#13), full exploration/relic/second battle (G2), physical iPhone QA and Human fun remain separate gates.

## Final bounded visual judgment (2026-10-15)

Root observed the actual 390×844 candidate after the contact-lighting and island-composition pivots. The candidate still reads as repetitive paving, simplified vegetation and a shallow court; waterfall/vista presence and sprite/world light integration are materially below the supplied local reference. **V0 FAIL / R2 quality BLOCKED**, not HQ completion. Multiple equivalent material/framing fixes failed to close this art gap. Further equivalent parameter polishing is stopped. The actionable next step is a separately reviewed local authored environment asset pass (sculpted silhouette/trim, designed floral clusters, visible island side/cascade, coordinated sprite lighting), using owner-approved assets and the same fixture; external art upload or paid purchases remain unauthorized. Existing local geometry is retained as an executable review candidate.

The Human-provided path resolves to SHA `99a76f33c60e74d7fd890e9cf188531280e030e58a5893bafd90899ea2b965b8`, not the Issue PRIMARY SHA. Comparison to this explicitly supplied local image is useful but does not verify PRIMARY byte fidelity. No numerical fidelity score or PRIMARY acceptance is claimed. Human R5 judgment remains pending.

Known residual: automatic foot-label avoidance can move a label far laterally from its owner (notably X/B in setup). IDs and true HP are retained, but association remains less direct than the target. Do not treat non-overlap as complete readability acceptance.

Independent final image/source review confirmed V0 FAIL: repetitive cream paving dominates; island-edge/cascade/distant silhouettes and dense flowers remain weak; the fixed fixture yields a clustered cast with small silhouettes. R1 contextual choice/prediction/start and single pause/review improve structure, but tiny actor tiles still rely on IDs. No additional event-loss/future-HP/death defect was identified in the bounded source review; final motion/impact legibility must be assessed from the event captures/video. Reviewed files: `r1-setup.png`, `ProtectAnchor-start.png`, `DirectAttack-start.png` and supplied owner-local reference. This is agent evidence, not Human preference or acceptance.

Final integrated run: Edit14/14, Play8/9. The rendered full-battle regression found guard-link overlap with B at step1 after a protect hop. `Display(Protect)` routed using the previous tween geometry, then `RefreshActors` restored the final pose without refreshing the persistent guard line. Refresh now rebuilds that line after final projected actor/HP placement. The regression remains enabled; a fresh full PlayMode run determines the repair result.

Reroute-after-refresh alone did not resolve the same step1 guard overlap (Play8/9). The three original direct/side-lane candidates all intersect projected obstacles in the new composition. Pivot: a bounded visibility-graph fallback routes the same known actor/target around expanded projected body/HP rectangles; no Core pathfinding or targeting is involved. The rendered-battle regression remains the acceptance check.

Native build4/c51aab1: Japanese on-screen kana input was stored, but the editable prediction stayed under the visible keyboard. The existing mock-area test did not model keyboard focus/opening with area=0. Fix docks the focused input beneath the title for the native edit session, then restores its contextual position. A regression now checks both normal/developer layouts with zero reported area. c51aab1 was not uploaded; a new frozen-source native run is required.

Final independent event-capture review (d782437): inspected actual Guard/Counter/Heal/Death captures and native pause/comparison screenshots. R3 visual FAIL confirmed: non-movement effects use long orthogonal arrows and can be mistaken for walking paths. HP/event values are readable, but small low-contrast order captions and layered HP labels weaken instant attribution. Comparison modal text distinguishes both outcomes, while roster cards paint across its lower region and compete with the sheet. No new event ordering/hash or modal-data corruption was observed. These are explicit residual UI quality failures, not Human or physical-device acceptance. Candidate remains Draft; no G2 advance.

## Pass2 A / packet 6192468412

First imported-OBJ render (55°/yaw0, same fixture/HUD as build4): product-flow regression1/1 PASS, but image review FAIL. Independent review found the replacement lost the old stone colour/shading variation, broad cream floor dominated, flowers appeared as dark spikes, island sides/front water remained hidden by framing/roster. Root did not infer the cause from pixels alone: standard Lit ignores vertex colours, and the old imported mesh colour stream was unverified. Corrective pivot imports the owned authoring dialect into real Editor-created Mesh assets with explicit normals/UV/RGB and a vertex-colour URP material; same-camera renders isolate this repair before reframing. Original failed images are retained privately in `/private/tmp/rogue-hq-pass2/pass-a-images/`. No HQ PASS from generated polygon counts or Core tests.

Vertex-payload repair run: real Editor log reports `vertices == colours` on every owned mesh (e.g. Limestone10629/10629), and product flow1/1 passes with exact outcomes. The same-camera image still fails sky-island composition: the world-space front edge falls beneath the portrait shelf. Next controlled composition removes the shelf/field overlap, fits the island side in the field, and captures55°/45° under the same yaw. This is a layout/framing repair justified by images, not a change to gameplay coordinates or obstacles. Source character RGB remains untouched.

Pass A repaired composition: all3 HQ PlayMode cases pass; independent actual-image review confirms removal of9×9 lattice and visible island side/front cascades. V0 remains FAIL. Missing production-grade environment art is specific: worn/beveled stone surface variation, layered cliff/island mass, substantial flowing waterfalls, dense flower/vine clusters and distant sky-ruin silhouettes. The current owned mesh authoring pass cannot supply that quality; more camera/material-number polishing is stopped.55°/45° does not prove a Human preference;45° remains reversible candidate framing only. Proceed to the independently actionable semantic/modeled-sheet bugs.

最終B独立画像review: setup/guard/counter/heal/death/comparison。巨大floating消失、headerコントラスト、sheet不透明・rosterより前面を確認。局所frame/receipt/HP整合に新規重大欠陥なし。R2 V0 FAIL: authored meshへ移行しても、石材の造形/崖の層/植物密度/滝の量感/遠景と4人の主役性が原画水準に届かない。数値微調整の反復を止め、具体的なproduction環境asset制作能力・素材の不足として保持。Humanの可愛さ/見やすさ/面白さは未判定。
