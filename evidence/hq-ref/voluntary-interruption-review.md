# Voluntary target instruction — 2026-10-28

Human rejected mandatory crisis stops and approved: voluntarily stop, choose an ally and enemy, confirm one instruction, resume auto battle; one confirmed instruction per encounter, cancel free. This supersedes crisis-stop UI for this experiment. Baseline PR21 a86679ead3bbda8b3525444ef606640ce7785fa6. Notion Decision Log not yet synced.

## Implemented scope

Presentation offers 指示する during active battle while the correction budget remains. A request completes the already resolving action and its visible events before opening the selection sheet. Cancel resumes without consumption. Confirmation calls pure Core Replay.TargetOnce; the selected living ally's next action attacks an adjacent selected enemy or takes an existing legal approach step (waits when blocked), then normal policy resumes. Target/actor death invalidates pending instructions. This is one action, not a persistent pursuit order. Existing no-intervention G1 traces remain unchanged. Enemy/ally selection currently uses named buttons, not direct board taps. No move/protect/heal command menu added.

## Closed-loop evidence

First Edit run: 29/31, two incorrect new test expectations (blocked approach and extra Advance) repaired without changing movement rules. Second run Edit31/31, Play13/15: cancel/reopen hit testing failure plus pre-existing silhouette overlap. Third run still failed next immediate actor click; canvas refresh and real rendering frames before next click repaired. Fourth run Edit31/31, Play14/15. Only unchanged whole-sprite overlap assertion remains failed at step0 A/B. No tolerance weakened. New real EventSystem raycast flow passes through encounters 2 and 3: request while busy, boundary stop, frozen state, free cancel, reopen, actor/enemy selection, confirm consumes one, automatic finish, no crisis stop. Frozen Protect/Attack hashes pass. Test success is not human fun evidence.

Root viewed retained 390x844 instruction-1-confirm and instruction-2-choose. Heading previously clipped, repaired and now visible. Selected ally/enemy highlighted; confirmation text visible. Sheet is functional but text-heavy and covers lower field/cards; this is an interaction prototype, not completed visual quality. Existing character overlaps, arrows and edge clipping remain. No new Simulator, physical-device or TestFlight verification. No claim that human finds it fun.

Exact executed source file hashes and Unity diff hash, XML results, private PNG hashes: voluntary-interruption-evidence.json. Private media retained owner-local under _userwork/hq-evidence/2026-10-28-interruption/retained-images; not uploaded. Raw failed runs retained alongside fourth. No paid service, external image transmission, merge or Ready change.

## Portrait selection refinement

Base 00f22b8. Approved existing portraits (aspect-correct UV, no new pixels), actual HP, selected tint, framed sheet. Panel 340→304 reference pixels: top moves down 36px, reveals more field. Root compared previous retained instruction-1-confirm with new Unity instruction-1-confirm: heading, names, HP and action buttons visible; selected ally identifiable by face. Enemies remain text/HP; board-to-enemy correspondence is not solved. No gameplay changes. Fresh Edit31/31, Play14/15: voluntary encounter input flow passes; existing silhouette assertion C/X fails, not relaxed. Source hashes checked against executed manifest. Private captures in owner-local 2026-10-28-interruption/portrait-retained. No Simulator or human play claim.

## Selected instruction preview

Base caac16f. While choosing, round-intent routes are limited to the selected instruction. Core provides current-state attack/approach/maintain description; explicit notice that circumstances can change before the actor's turn. No future simulation or damage promise. Root inspected instruction-1-confirm: selected Shiro's approach arrow and current one-cell approach description visible, unrelated round-intent arcs removed. Existing relation marker and right-edge character clipping remain; not a complete readability/fun acceptance. Fresh Edit31/31, Play14/15, only existing initial sprite overlap D/B fails. Selection text and at-most-one round route regressed with real input flow through both later encounters. Exact source manifest matched. Private screenshots retained in owner-local intent-retained, not published. No native/device play this iteration.

## Instruction feedback and later-encounter framing

Base a9a4ff5. Later encounters use width18.75 instead of16; initial encounter remains16. Camera changes resize sky backdrop too (initial attempt exposed backdrop edge, repaired). Root inspected instruction-1-outcome: all four allies fit, sky covers frame, receipt says Shiro moved one cell. Waiting receipt changes only after presented resolved events; reports damage/approach/blocked action or actor/target death. Budget stays consumed. Independent Luna read-only review identified missing outcome/invalidation feedback, now addressed. Full Unity Edit31/31 Play14/15; existing initial whole-sprite overlap remains. New outcome receipt test passes through both later encounters. No new native or human play; this is not fun acceptance.

Additional separately executed Core-only measurement (1/1 pass): 937 legal single target instructions across all baseline action boundaries/three encounters/two preferences. 306 change survivors/totalHP/actioncount/outcome; 631 leave those metrics unchanged, though traces may differ. 76 increase survivor count relative to that condition's no-intervention baseline. This measures mechanical consequence, not intuitive choices or fun. Full raw CSV and XML retained publicly without private pixels. No gameplay parameters altered to manufacture success. Only measurement test added after full runtime run, marked separately in evidence.

## Current autonomous action versus instruction

Base29d898d. Selected ally's current autonomous action/target is shown above portraits. Equal immediate action (same destination for Move, same target for Attack) is marked explicitly and confirmation says 同じ一手を指定; does not forbid use or change budget semantics. Root viewed instruction-1-confirm: autonomous Shiro approaches flower, selected moss produces same immediate move; equality notice fits with timing caveat. This compares present plans, not eventual outcomes. Fresh full Edit32/32 Play14/15, known initial B/A silhouette failure only. Source manifest checked. Raw captures local comparison-retained. No new human/native evidence, no fun acceptance.


## Native-observed result repair (2026-10-28)

Native Simulator source `bb0ced0d84e60e5a5ce1571dd03458c31a16ec6a` exported and built successfully; generated settings restored and source assets unchanged. Installed binary SHA-256 `91fb20d8c633b0e2528e37df878f60d76a4dc67f3a16b9aa6b2c01160d8e9f6b`. CUA screen taps exercised start, voluntary interrupt, Shiro→flower spirit selection, confirm, auto-resume, and four-survivor victory. No new TestFlight upload or physical-device claim.

Observed defects: victory heading had no visible glyphs; instruction outcome disappeared at victory. Reduced heading font to fit its line box and retained the observed one-action receipt in the result banner. It is an action receipt, not a claim of causal victory contribution.

Full repair run: Edit 32/32, Play 13/15. Failures were the existing initial silhouette-overlap gate and a new result assertion reading before HUD Update. Corrected the test to observe the subsequent frame. Focused consecutive-encounter Play test then passed; it checks retained receipt and generated heading vertices. Viewed fresh 390×844 second/third encounter result captures: heading, survivor count and receipt visible; card area unobstructed. Full suite remains non-green due to existing overlap; no gate was weakened. Post-repair native rebuild has not yet run. Human fun remains unverified.

Private local evidence: `_userwork/hq-evidence/2026-10-28-interruption/native-retained/manual-observation.json`, `native-retained/build-result.json`, `result-retained/gauntlet-2-end.png`, `result-retained/gauntlet-3-end.png`, `result-focused/`. Images are local only. Public machine evidence is `instruction-result-*`; source manifest identifies the tested patch on bb0ced0.


## Target identity portraits (2026-10-29)

Added locally drawn green/violet spirit faces beside enemy name and HP in the voluntary instruction selector, using the existing garden-spirit visual vocabulary. No imported assets, private-source upload, Core/position/camera/actor-size changes. Compared same encounter/state `instruction-1-confirm.png` against `2026-10-28-interruption/comparison-retained/`: portraits add a visible board-to-button identity cue, with both name and HP fitting. This is an executor visual judgment, not measured human recognition or fun.

Focused consecutive-encounter Play test passed (1/1); filtered Edit run contains 0 tests, so is not a new Core suite claim. Previous full Edit 32/32 evidence remains scoped to the previous change. Fresh native verification not run for these faces. Private captures remain under `_userwork/hq-evidence/2026-10-29-target-portraits/private/`.

Independent read-only review confirmed that the existing silhouette gate compares whole projected mesh AABBs, including private-sprite transparency. It does not measure opaque coverage or depth occlusion. The failing gate is preserved unchanged; no claim of resolved overlap. A future diagnostic must compare isolated visible actor masks with actual combined rendering before distinguishing harmless transparent overlap from occlusion.


## Same-encounter retry and instruction lifecycle (2026-10-29)

Added terminal “同じ戦い” returning to the current encounter initial state and preserving preference, beside “先へ進む”. Existing former-Pause input sink remains on the right: first candidate reused that space and was rejected by regression test. A subsequent broad replacement caused a compiler error; repaired before this run. New flow check actually clicks retry after third encounter, verifies initial serialized state, correction budget 1, cleared receipt, preserved encounter/preference, then completes another battle.

Instruction receipt now changes from waiting to executing when its actor's presented Action begins; final result still derives from completed events. Selector explicitly says confirmation consumes the opportunity, cancellation does not. Astra provided text-only advice, not visual acceptance.

Full verification: Edit 32/32, Play 14/15. Only existing initial X/C projected-bounds overlap fails; no test weakened. Viewed fresh 390×844 result and retry-ready captures: button labels fit, result receipt persists, all four HP reset and encounter remains 3. Post-change native/physical/Human play not yet verified. Source manifest and XML are `play-loop-*`. Private images remain `_userwork/hq-evidence/2026-10-29-play-loop/retained/`. Five playability criteria remain unaccepted; these checks establish behavior, not fun.


## Current presented action (2026-10-29)

Existing event shelf shows actor, target and action kind while the already-presented Action event is active, then returns to observed results. It does not preview future queued effects. Full Edit 32/32 and Play 14/15; sole remaining failure is existing initial projected actor-bounds overlap. Fresh `gauntlet-current-action.png` viewed at 390×844: モモ→シロ / 守護 fits the shelf while the matching cast is highlighted. Consecutive-encounter test asserts action caption on start and completes retry flow. Private capture retained at `_userwork/hq-evidence/2026-10-29-play-loop/retained/current-action.png`. No human-fun claim.


Native follow-through at source `52c071fb5ee26192651b3c844ce6ea847ace80dd`: export/build succeeded; actual QA Simulator CUA taps verified cancel/reopen/confirm/auto-victory/same-encounter retry. Victory receipt visibly retained flower-spirit 4 damage. Selected instruction matched autonomous action; this is flow evidence, not proof of strategic benefit. Exact binary/source/observed limits: `play-loop-native-observation.json`. No new TestFlight upload.


## Current enemy intent and meaningful retry comparison (2026-10-30)

Enemy selector shows current paused Core action/target beside face and HP. Sheet grows 16 reference pixels so two-line enemy labels fit. Root viewed 390×844 baseline, selection and directed result. Same encounter and DirectAttack: autonomous battle leaves 3 survivors; first-boundary Garu→flower instruction leaves 4 (Shiro HP9). This is an actual raycast UI flow, not solely a Core simulation. The result receipt reports flower 6 damage, without claiming that damage alone explains victory. No optimal-answer prompt or future-state preview added.

Full Edit32/32, Play15/16: new same-encounter comparison passes; sole failure is existing initial A/B whole-mesh AABB overlap. No gate weakened. Exact source manifest, XML and local private image hashes are threat-choice-*; images not uploaded. No native run of this change yet. Executor judgment: outcome consequence is evidenced, but watching interest, intuitive reasons to intervene and desire to retry are not yet strong.


## Rendered actor contribution diagnostic

Read-only Luna review identified transparent billboard corners in the AABB gate. Added a separate report-only test; original gate remains unchanged. First differential run was rejected because unscaled breathing changed pixels. Second run froze the actual pose by disabling the view/ambient updates temporarily, restored them afterward, used unchanged camera/materials and actual field render target. Each body was rendered alone and with other bodies; RGB difference >3/255 measured contribution against the same background. This is a color-difference estimate, not an exact alpha/visibility mask or face-legibility acceptance. Source shader, sorting and Core remain untouched. Raw field render target has a different aspect than the phone UI and is not a finished phone screenshot.

HQ initial-state body contribution lost in composite: A25/22344, B354/22432, C25/22342, D39/23067; moss X2936/11367 (~26%). Repeated-render instability was 2–20 pixels per body. The green enemy is visibly behind Garu in the actual render; allies are mostly visible despite overlapping quads. This narrows the issue; it does not clear the old gate or establish battle-time readability. Enemy eyes/leaves, shadows and markers were not isolated as body masks. Private raw renders retained owner-local in 2026-10-30-threat-choice/occlusion-{MobileG1,HqExploration}; public CSV/XML/source manifest actor-contribution-*.


## Remembering the immediately previous attempt

Added compact actual history to the existing survivor line: 4人が生還（前回3人）. Stored only when retrying this encounter; ignored in other encounters. No extra panel, required comparison/input, or causal success claim. Text-only Astra advice supported this narrow addition (no visual review claimed). Focused real-input comparison passed1/1; viewed fresh result at390×844 with receipt and previous survivors fitting. Full suite above predates this final line change; native confirmation pending. Private result retained at retained/retry-with-previous-result.png.
