# S0 source-art authoring capability pilot — FAIL / source identity BLOCKED

This is an **executed local authoring test**, not another motion-reference report,
not a full Shiro source artwork, not S1 Contact/Passing poses, and not a walk.
Contract: latest Human S0→S3 AUTO-GO and [Design Office handoff](https://github.com/tomooch/rogue-tactics-lab/pull/12#issuecomment-6133846792).
Baseline PR `fdd02510f90faeb7dc89fd1d523b5c58f10aaf4c`; fetched main
`ab9fa5dc729dcea07f8680e7b22544627c5e5bed`. Existing PR worktree/branch reused.
Root documents still describe historical Stage0; their historical results are not
new S0 results. Latest Human permits isolated Unity tests without final art
adoption, superseding the old ZIP's Human-before-Unity experiment order.

## HYPOTHESIS / EXPECTED / OBSERVED / DECISION

- HYPOTHESIS: closed, newly painted, editable anatomical/costume layers could
  support articulation without C0's absent hidden pixels and foreign fragments.
- EXPECTED: local tools can author hidden hip/knee/shoulder/elbow/cuff/cloth
  pixels while preserving the approved Shiro model and material rendering;
  prove this narrowly before making a whole character.
- OBSERVED: authored nine new vector paint groups and raster layers, two
  variants and one bounded joint-outline repair. Real SVG rendering and ORA
  packaging executed. Three **part-range tests**, not three Shiro poses, expose
  knee/elbow motion at inferred angles. No old C0 parts, generated images or
  old capture frames enter these new source layers.
- DECISION: limited editable-new-paint capability works, but **S0 art FAIL**.
  Boot volume/folds/flowers/material shading remain simplified; knee/elbow
  overlap shading/contour breaks remain after repair. There is no complete
  Shiro model or validated identity source. Do not propagate failing swatches
  into S1 or six/eight-frame production. True layered artwork in general is
  not disproven, and six-frame insufficiency is not established.

## Source and actual tool audit

`source_inventory.json` records read-only paths/dimensions/hashes, not private
image data. ZIP full-body reference is 1086×1448 RGB; face is1254×1254 RGB;
four-direction candidate is1254×1254 RGBA. Its manifest marks all three
`approved_as_final:false`. `_userwork/` contains the broader game visual
reference pack but not matching Shiro master filenames. No canonical authored
PSD/PSB/KRA/ORA/SVG was located in these relevant inputs. The exact latest
approved Shiro master therefore remains unresolved; a path question was sent
while independent capability work continued. Candidate names/old reported
approval are not silently promoted to confirmed source identity.

The comparison inset uses already-public `input/shiro_walk_front_01.png` only
as a style warning. This rejected-motion baseline is neither accepted neutral
art nor the source of any pilot layer. Private reference originals are not
committed, uploaded or embedded. No original is changed.

- Actual local authoring: Python3.12/Pillow, bundled Node.js,
  sharp0.35.5 (librsvg2.63.2/cairo1.18.6) renders explicit Bézier paths/gradients.
  No extra tool installation or account/cost was needed.
- Photoshop2024 is installed, but `cua.getApp` returned **Computer Use was not
  approved**. It was not operated; no alternate automation bypass was used.
  This is an access limitation, not proof Photoshop cannot author the art.
- Built-in reference image editing would transfer a reference to generation;
  private-art upload is prohibited in this task, so no such call was made.
- torch/diffusers/local model weights and Krita/Blender were not detected.
  Installing a paint editor alone supplies no matching hidden anatomy or
  illustration. No quality claim is made about untested local model workflows.
- [sharp SVG input](https://sharp.pixelplumbing.com/api-constructor/) and
  [OpenRaster layout](https://www.openraster.org/baseline/file-layout-spec.html)
  informed the actual format implementation. ORA XML/layers are read back by
  the validator; reopening them in a paint-editor UI remains unobserved.

## Source structure / reproducible artifacts

`authoring_manifest.json` records pivots, ordering, hidden paint and missing
whole-character components. Each attempt has:

- `editable_pilot.svg`: nine separately named editable paint groups;
- `editable_pilot.ora`: nine separate RGBA layers with stack order/merged image;
- `layers/*.svg` and `*.png`: actual independently authored components;
- `neutral.png`, `range_0..2.png`, `range_diagnostic.jpg`,
  `style_comparison.jpg`: rendered swatch/range evidence.

Parts: thigh with hidden hip cap, shin with hidden knee, complete trial boot,
upper arm with hidden shoulder, forearm with hidden elbow, sleeve inner cuff,
closed wrist/hand, rear skirt and inner frill. These are provisional new paint,
not recovered anatomy. Knee0/+25/−20°, elbow0/−18/+15° are **design test ranges**,
not M0 measurements or measured Contact/Passing phases. Back→front part order
and root-space pivots are recorded. PNG texture is generated from the same
editable paths; attempt2 ornament/edge changes are difference paint.

Absent: complete face/head/hat/hair, torso/pelvis, distinct bilateral legs and
arms, front/back skirt/cape interaction and hand-held staff. An editable
archive of swatches is not a genuine complete Shiro neutral source.

## Adversarial review / bounded repair

Executor and independent Luna Medium source/actual-image review agree that
new paint exists, but flat vector styling does not preserve the illustrated
baseline. Initial smooth gradients/plain boot were insufficient. Ornament
variant added bow/flowers/gold stars/cuff/frill shading but still lacked volume,
folds and motif detail. Actual review found hard knee/elbow outline rings.
One reactive repair removed those inner outlines and retained outer contours;
fresh renders show less dark ring but visible shading/contour separation.
Pre-repair files are preserved in `attempt2/before_joint_repair/`.

`visual_review.json` records the art verdict separately from artifact QA. A
successful process/format/margin check cannot erase these failures. Do not
claim every alternative method is impossible or a whole Shiro test failed:
this limited local method did not satisfy the source-art gate.

## Gate status and next executable choices

| Gate | Status | Evidence / reason |
|---|---|---|
| local editable new paint | limited capability established | actual SVG/ORA/raster outputs |
| S0 Shiro neutral source | FAIL; canonical identity also BLOCKED | visual mismatch, remaining seams, incomplete model and unresolved master |
| S1 three Shiro poses | NOT EXECUTED | S0 prerequisite not met |
| S2 six/eight-frame walk | NOT EXECUTED | no S1; no evidence sampling is the problem |
| S3 Shiro Unity execution | NOT EXECUTED | no acceptable source; old Fei R0 is not new Shiro proof |
| HUMAN GO | NOT OBTAINED | no finished Shiro candidate video |

Resolve exact canonical artwork path/version first. Then use a genuinely
available local paint workflow to author matching complete model artwork;
the nine new swatches are **failed capability evidence**, not production rig
parts. Photoshop access may enable actual local painting, but neither access
nor a new free editor establishes matching artwork automatically. If an
editable original exists, inspect hidden anatomy and use three-pose S1 first.
No external commission/cloud art upload or paid model/service has been used.

## Reproduction / verification

From repository root, with bundled tools or equivalent installed versions:

```text
python experiments/shiro-front-walk/scripts/build_s0_authoring_pilot.py --node <node> --sharp <sharp module path>
python experiments/shiro-front-walk/scripts/verify_s0_authoring_pilot.py
```

Builder completes actual art rendering. Validator checks XML/layer sources,
ORA structure/recomposition, RGBA/range margins, prior evidence preservation,
and makes/decode-checks a390×844 H264/yuv420p diagnostic. It labels its output
**S0 FAILED / NOT SHIRO / NOT WALKING / NOT UNITY**. Exit0 means artifact
checks executed, never art acceptance. Target source SHA/actual job result
belong in `qa.json` and `commit_verification.json` after completion.

Production `unity/`, old source/input/output/alignment/C0/M0/R0/motion-reference
are unchanged. No relevant production tests are affected. No new Unity test,
Stage0 regression, physical iPhone playback or Human gait observation is
claimed. Draft PR12 continues; no main change, merge or art adoption.
