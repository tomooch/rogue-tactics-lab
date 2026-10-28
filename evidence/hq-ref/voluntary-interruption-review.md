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
