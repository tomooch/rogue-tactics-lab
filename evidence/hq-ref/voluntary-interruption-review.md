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
