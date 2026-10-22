# B direction / quarter-view implementation experiment — 2026-10-23

## Authorization and preserved conditions

Human selected static direction B on 2026-10-22, then explicitly resumed implementation on 2026-10-23 and supplied a quarter-view diorama reference. This supersedes the previous bounded preproduction stop. It does not approve pixel-art restyling, key-art background pasting, new gameplay, paid assets, outsourcing, external private-media upload, merge or Ready.

Start PR21 HEAD: `36d5a288a49bdc67d89cbfe96d3f479da48f6bd7`; branch `codex/hq-ref-vertical-slice`, base `codex/mobile-visual-g1`, Draft/open verified from GitHub. Canonical main has an unrelated modified `.gitignore`, left untouched.

All trials keep G1 Core, 5×5, initial grid coordinates, two enemies, event order, approved four source PNGs and environment meshes. Presentation camera, shared billboard size, HUD layout and foot labels are the experiment variables. No Blender/additional geometry was produced.

## Actual Unity observations

Each A–D trial used Unity 6000.3.25f1 and a filtered ProductFlow test (EditMode selects 0, PlayMode 1/1, both fixed outcome hashes asserted). These are **not full-suite passes**. Seven actual 390×844 stages were retained per trial: setup, Protect guard/heal, Attack counter/death, and both comparisons.

| Trial | Camera pitch / yaw / orthographic size | Shared actor height | Observation / disposition |
| --- | --- | --- | --- |
| A | 35.264 / −45 / 15 | 5.4 | Side/thickness reads as diorama, but Garu substantially hides X and initiative is behind the header. Reject. |
| B | same A camera | 3.8 | Header/order separated; X partly visible; cast loses emphasis and HP labels remain displaced. Repair. |
| C | 45 / −30 / 15 | 3.8 | Six faces distinguishable; more side/depth. B/X labels remain far from owners. Not accepted. |
| D | same C camera | 3.8 | 32×18 HP labels, lateral offset limited to 12 points, improve ownership. Counter still crowded. Not accepted. |
| E full | same D projection | 3.8 | Edit14/14, Play8/9: initial B/A full sprite rectangles overlap. Existing readability assertion unchanged. Reject. |
| F full | 55 / −20 / 13 | 3.0 | Edit14/14, Play8/9: silhouette assertion passes initially but B's HP covers A's rectangle. Existing assertion unchanged. Reject. |
| G full | same F camera | 2.7 | Edit14/14, Play8/9: at step3 X's HP overlaps A by 0.04 points. Regular400 improves Japanese text; cast is too small. Reject camera. |
| H full | same G projection | 2.7 | Edit14/14, Play8/9: removing the distance penalty moves the failure to X/C at step4. Reject; source/patch retained privately. |

Root directly viewed A–F actual setup/action images. A bounded independent local pixel reviewer viewed A/C/D; reported improved diorama depth and closer D labels, but crowded cast/HP relationships. Astra advised from sanitized **text only**, with no image inspection; its advice is not independent pixel evidence.

## What remains unresolved

The selected B static image has larger cast and a composed environment montage. Actual trial images have a dominant repetitive floor, simple water ribbons and a sparse environment. Camera/HUD progress does not close that art gap. R2 V0 remains FAIL; Human cute/readable/fun and real-iPhone acceptance remain unverified.

Shallow projection, large fixed billboards and strict full-rectangle non-overlap conflict in this close G1 fixture. We have not weakened the existing silhouette/HP/path tests. A technical pass alone cannot justify adopting a smaller, floor-dominated screen.

The existing Noto variable font has `wght` default 100 and OS/2 weight100. Synthetic Bold visibly corrupts/thickens CTA strokes; changing to Normal removes that defect but remains too thin. Static Regular400 was generated locally from the existing OFL source using development-only FontTools4.60.1. Actual G/H images have clear CTA/input/event text without synthetic-bold corruption. The existing OFL file is retained; no paid dependency, private-media upload or production service. See [official instancer API](https://fonttools.readthedocs.io/en/latest/varLib/instancer.html) and `tools/prepare_hud_font.py`. Original variable font remains in Git at the start HEAD; the output SHA is in the evidence JSON.

All camera/actor/HUD candidates were rejected, and the four task-owned Presentation source files were restored byte-for-byte to start HEAD. The source-linked font-only full suite passed EditMode14/14 and PlayMode9/9 on the retained camera/layout. Both frozen outcomes, exact displayed event order, pause/resume, prediction/compare/retry, silhouette/HP/path checks pass. Root viewed all seven actual images; independent pixel review confirmed clearer Japanese/HP and no new clipping/glyph corruption. A new header/order regression assertion and expanded failure coordinates preserve every existing check. No whole-screen quality improvement is claimed from font repair.

The concrete Human question is pending: prioritize face/eyes/clothing/action-target clarity with limited foot/transparent-margin overlap, or retain strict full billboard rectangle separation. `direction-tradeoff.png` is a private same-size D/G actual-Unity pair. No answer has been inferred.

## Private evidence / limits

Owner-local durable root: `_userwork/hq-evidence/2026-10-23-b-quarter-view/` in the canonical workspace. Trial folders contain source/results JSON, actual PNGs, logs/XML, capture hashes and root review. Failed E/F source patches are retained privately. `jobs/` records exact argv, completion/exit and queue delivery. The selected static B and reference original were directly compared again after F.

No private original, skin, PNG, video or comparison is included in Git. Local paths do not establish cross-session media sharing. No new Simulator, TestFlight archive/upload/distribution or physical-device result is claimed for these candidates. Old build5 is unrelated source `d5e8e08…`.

Both frozen hashes remain the required outcomes: Protect `e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d`; Attack `85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e`.


## Retained improvement and source identity

Only the static Regular400 font is a runtime change. All Presentation C#, Core, meshes, skin bytes and restored Scene match start HEAD. Header/order occlusion coverage was added to ProductFlow; existing failure diagnostics now include step/rectangle coordinates. Neither change relaxes a check. Font-only execution source is start HEAD `36d5a288a49bdc67d89cbfe96d3f479da48f6bd7` plus Unity diff SHA256 `5a989b2a0745e0c25e90e83c97128ba922924ae8174c7dbfb8d035fec7a25e59`; the frozen manifest maps it to the committed files. Public evidence JSON and font-only XML contain no media. Fresh source8c71051 Simulator export/build/install/launch and actual both-condition/input/pause/comparison observations succeeded; see `quarter-font-native-observation.json`. This is not real iPhone/TestFlight evidence.


## Fresh native observation

Dedicated iOS27.0 QA Simulator, source8c71051: Unity export exit0; initial guard stopped on automatic icon metadata, then five generated settings were audited/preserved/restored before building the successful export. Xcode build exit0, install/launch observed. Both native full hashes match, Protect pause/hold/resume and retry, Japanese composing「か」→candidate「かつ」→keyboard return→comparison retention observed. Visible roster/caption/transport ROI is identical over20.891seconds while paused. Both comparison outcomes show14actions, alive4/3 and expected HP/heal counts.

Private Attack recording77.44s,1206×2622,h264,4649frames, SHA256 `e6dd3492524d23ba01eb0ae895ec9bb581a583bbe561b61c435b0ae444febe25`. Bounded normal QuickTime playback with actual clock advancement and changing battle states was observed; no claim of visually inspecting every frame. The recording and raw PNG/logs remain local. A slow Finder UI call delayed file-location observation; filesystem discovery found the exact recording and SHA-verified it into the durable private evidence folder. No external upload/TF distribution/real-iPhone/Human approval.
