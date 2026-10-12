# M0 / R0 actual execution gates

Contract: [Design Office 6125123428](https://github.com/tomooch/rogue-tactics-lab/pull/12#issuecomment-6125123428), latest Human explicitly requests actual M0/R0 independently. Baseline PR `eef0b0eb15241ff349dd14d3926fe5d127943faa`. No A/B/C0 re-investigation, no new Shiro art, no production Core/Unity/main changes, no merge.

## M0 — actual frontal marker video (executed / PARTIAL)

Downloaded exact scientific asset `Action-and-Emotion-Recognition-from-Point-Light-Displays-An-Investigation-of-Gender-Differences-pone.0020989.s004.ogv`, 93,271bytes, SHA256 `f0db2c533988fae5a7f473a9d49ef77028cc79b0d2b73cdc768f6fe34238aed3`. ffprobe 1920×1080, duration3.083s. Source [Commons](https://commons.wikimedia.org/wiki/File:Action-and-Emotion-Recognition-from-Point-Light-Displays-An-Investigation-of-Gender-Differences-pone.0020989.s004.ogv), Alaerts et al.2011 [PLOS ONE](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0020989). License displayed BY2.5, file metadata BY3.0; credit both URLs, annotate modifications. Full original footage remains temp, not rehosted.

91 video frames at source PTS were actually decoded/viewed. Several overlapping markers remain null in the trajectory; all gaps are recorded. The preview is a CFR derivative at median PTS interval; source timestamps remain in JSON. [Methods / Figure1](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0020989#s2) confirms **12 markers: shoulders, elbows, wrists, hips, knees, ankles; NO HEAD MARKER**. Shoulder midpoint is not a measured head. It is a deliberately sad female actor, not neutral/cute motion direction. A second specified asset `pone.0044473.s007.ogv` was downloaded/decode-viewed (SHA256 `85a7742dd14f61320120c4bad1f4dd020c90154133a850e2bf06fcfa21c0c9d6`) but does not resolve absent head.

`run_M0.py` tracks visible centroids in original1920×1080 pixel coordinates at ffprobe actual PTS. Initial image-left/right labels followed by temporal nearest tracking are not certified anatomical L/R. Merged/hidden points staynull, never asserted observed. Decode uses passthrough frames to avoid silently duplicating into a nominal fps. No sole/floor/force data: three source-linked snapshots must not be named certified Contact/Passing merely from labels. Derived marker chart/movie are attribution-labelled educational reference. Gate remains **PARTIAL** for upper limbs/pelvis/legs; head and reliable sole/contact/neutral gait are not established.

## R0 — isolated official sample (actual run3 PASS, limited tooling scope)

Actual project creation exit0, Editor6000.3.25f1(e1dba0a9aba4), job4c744011-36fe-4417-b6d3-dec94cb9bfed; project `/private/tmp/shiro-R0-unity` then configured by `configure_R0.py`. Production `unity/` unchanged. Official package registry tgz Animation13.0.0 SHA256 `3e9906498ffb3c16b4b666b74f87a84408560f2f80e89fa958b76dd0110b81ca`; package targets6000.3, dependencies including2D Common12.0.0. PSDImporter12.0.0 is isolated-only. Official Samples license is Unity Companion License; source/sample art stays isolated/temp, only our harness and verification captures belong in PR. Preserve sample LICENSE in isolated imported folder; never make Fei final Shiro art.

R0Probe loads official `3 Character/Sprites/Fei.psb` through real PSD import, instantiates rig, authors three constant quaternion poses on sample bones, uses actual Animator/controller+loop, CPU SpriteSkin deformed vertex observations, root coordinate and layer/sorting records, actual390×844 camera RenderTexture PNG at Play Mode. Captures six seconds / light-dark. It does not prove Shiro hidden artwork, front walk, natural gait or human acceptance. After execution check skeleton semantics, vertex change, clip/phase transition, layer continuity and images; do not claim visual joint/occlusion PASS before inspection.

Commands / source:
```text
python scripts/run_M0.py /private/tmp/M0-front.ogv
python scripts/configure_R0.py /private/tmp/shiro-R0-unity /private/tmp/R0-animation13.tgz
Unity -batchmode -projectPath /private/tmp/shiro-R0-unity -executeMethod R0Probe.Run -logFile /private/tmp/shiro-R0-play.log
```
Long actual Unity jobs use detached verification runner with exact command/log/completion records. Final actual runtime: run3 exit0, 180frames, 8loop wraps, 9 nonempty deformed SpriteSkin meshes / 10renderers / 32transforms, root fixed0. Three pose maximum bone deltas≈40°, mesh vertex moments change. Six light/dark390×844captures and H264/yuv420p6sec30fps movie. Official Animation13.0.0 / PSDImporter12.0.0 / resolved2D Common12.0.4. Visible cuffs/boot joins and layering look coherent in three poses; tunic conceals hips and staff is independent, so those relationships remain unverified. No Shiro art/Human gait acceptance. NUnit/Stage0 runtime suite not rerun; production Unitytree unchanged.

R0 actual PlayMode job `3079bb85-be14-467f-9543-0e190bfea692` targets source `188eedf3e815341082295118be410f81c6f9a574`, importing Animation13.0.0/PSDImporter12.0.0 in isolated project. Results pending; no success inferred from source code.

## Final execution binding / bounded repairs

R0 source `127bb5441772f8f8e7176f2ef5b78cd5bbb6be65`, job `efb98587-5848-4c03-a141-e004d7da6a34`. First run rendered180frames but QuickSearch startup exception→FAIL. Second timed out and old runtime was explicitly rejected. Deferring Play initialization, matching capture class/file, fresh run3 output and direct exit after JSON flush resolved observed failure on run3. No exception suppression or Unity source patch. Post-encode drawtext was unavailable; annotated frames with Pillow and full decode checked. Metadata/command/freshness/hashes in `R0/commit_verification.json`, raw measurements in `runtime.json`, actual package lock/manifest retained. Full temporary Editor logs are not public proof by themselves.

## Gate semantics

M0 HYPOTHESIS: explicit frontal source can expose body coordination missing from prior side-only research. EXPECTED: actual decode with useful upper/body/lower landmarks including head. OBSERVED:91frames/12body markers, no source head/sole markers, 4merged-point frames staynull; source is sad, not neutral. DECISION:access/body observation succeeds, full M0 coverage remains PARTIAL.

R0 HYPOTHESIS: official complete sample rig can establish tooling before Shiro artwork. EXPECTED:actual import/compile/Animator/3distinct bone+deformed-mesh poses, stable root, portrait render. OBSERVED:run3 exit0/180frames/8wraps/3signatures/6captures; limited actual-image review supports visible joins/layers. DECISION:R0 tooling PASS in the exercised sample range, never Shiro art or gait adoption. Remaining:head/sole/neutral motion reference, authorable Shiro source-art capability, hidden hip/prop tests and eventual real human gait review.
