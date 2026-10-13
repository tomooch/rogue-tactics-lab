# S0 raster pilot — identity resolved / bounded hidden-paint FAIL

## FACT / canonical source

Human's 2026-10-13 attachment `shiro_approved_reference.jpeg` is 1086×1448 RGB.
Measured SHA-256 is `59e6186b126d1c7c5ac05e577340af79bb5eddb04d0aaff270398686c465d317`, matching the supplied digest.
The ZIP's same-size `CANDIDATE_full_body.png` is **not identical**: SHA-256
`8d1323045422d60774adda005eb04f23dcfcbfe8ae9b6a728d8675da33f979d9`.
Decoded RGB also differs (mean absolute channel difference 1.745772, maximum55).
Use the attached JPEG; the older S0 identity blocker is resolved, while its vector-art FAIL remains valid historical evidence.
See [measured identity](reference_identity.json) and [Human registration](https://github.com/tomooch/rogue-tactics-lab/pull/12#issuecomment-6135237549).

## HYPOTHESIS / EXPECTED RESULT

Source-raster layers plus narrowly bounded hidden-region paint may preserve the approved character better than the failed vector redraw. This is not proved by RGB identity alone: separation, joint coverage, occlusion and actual pose appearance still need review.

## Local-only execution boundary

The original and derived raster artwork stay local. Do not commit/upload these pixels.
The original file is never overwritten. Only reproducible code, non-raster measurements and documentation are intended for GitHub.
Public model download is inbound only; inference must use CPU locally, without transmitting the reference image.

Masking tool: [SkyTNT anime-segmentation](https://github.com/SkyTNT/anime-segmentation),
[Apache-2.0 ONNX model](https://huggingface.co/skytnt/anime-seg), pinned revision
`493cb60893f47441b26ec4fb9a306bce9e342982`. This model extracts foreground; it does **not** draw missing joints or prove editable art feasibility.

## Gates

1. Inspect light/dark extraction at source resolution, preserving original RGB.
2. Define semantic layers, protected visible pixels, pivots/overlap and newly required hidden regions.
3. Test local completion only in those regions; record exact paint masks and before/after evidence.
4. Review three actual poses before any loop production. Stop if source style or joint/garment continuity fails after bounded repairs.
5. Only an established artwork gate permits new Unity animation verification. Historical R0 sample replay is not this gate's evidence.

OBSERVED RESULT: identity comparison completed. Local CPU extraction completed (3.38s command wall time including imports/model load/output, not a pure inference benchmark). Model SHA-256 `f15622d853e8260172812b657053460e20806f04b9e05147d49af7bed31a6e99`. Extracted RGBA retains all decoded source RGB values exactly. The light/dark comparison shows residual background tint and soft halos around hair gaps and staff: **transparency art gate not passed**. Nonzero alpha extends to y=0; raw mask is not atlas-ready. [Metrics](extraction_metrics.json).

No complete anatomical layers, poses or new Unity replay. Bounded local occlusion paint was executed using OpenCV Telea and Navier–Stokes (radius5). Initial mask left some gold tip outside the fill; the mask was corrected once and both methods freshly rerun. Final mask5330px; outside-mask RGB changes0 in both variants. Both still produce broad blurred skin/cloth transitions and fail source-style/structure review. These are two editable diagnostic layers (untouched original + local fill), **not a genuine articulated Shiro source**. [Attempt1](hidden_paint_attempt1.json), [attempt2](hidden_paint_attempt2.json), [independent review and clarification](visual_review.json).
DECISION: canonical JPEG selected by explicit Human instruction; no art or production-method adoption.


## DECISION / gate status

- Identity PASS; original byte hash unchanged.
- RGB-preservation and bounded-edit checks measured separately; final ORA verification recorded in `verification.json` when complete.
- S0 art FAIL for the tested automatic interpolation workflow. Full layer feasibility remains unestablished; do not promote cutouts to finished joint art.
- S1 three poses, S2 walk, S3 new Unity replay: NOT EXECUTED because prerequisite source art failed. No new walk MP4 exists.
- Old A/B/C0/M0/R0 and vector-S0 evidence retained. No Core/Stage0 edits; no regression rerun required for these local diagnostic scripts.
- This is not Human rejection of new completed artwork: no completed artwork exists. Human naturalness GO remains absent.

## Local review artifacts (not public URLs)

Private delivery directory on the user's Mac:
`/Users/tomoki/.codex/visualizations/2026/10/11/01a12b8b-cc88-7592-9846-85a0ade0eb48/shiro-raster-pilot/`

- `extraction_review.png`: source/light/dark comparison; raw mask, not final transparency.
- `hidden/hidden_paint_comparison.png`: first attempt, preserved.
- `hidden2/hidden_paint_comparison.png`: corrected-mask attempt, actual failure evidence.
- `hidden2/mask_scope.png`: modified region.
- `hidden2/telea_editable.ora`, `hidden2/navier_stokes_editable.ora`: two-layer diagnostic sources.
- `source_rgb_raw_alpha.png`, `raw_alpha.png`: local extraction data.

These contain approved source pixels and were not uploaded to GitHub or Notion. They are local desktop review artifacts, **not iPhone-accessible public evidence**. Git contains code/measurements only. Original remains in Downloads unchanged.

## Reproduction

Dependencies actually used: Python3.12, Pillow11.3.0, OpenCV4.12.0, ONNXRuntime1.22.1 CPU. `rembg` was installed but not invoked. Installation runner resolved the venv Python symlink to the bundled runtime; this was not an isolated venv install. No claim of isolation is made for Python packages. Unity remained untouched.

Download public Apache-2.0 `isnetis.onnx` from HF revision `493cb60893f47441b26ec4fb9a306bce9e342982`, check model digest above. No reference upload is involved.

```sh
python experiments/shiro-front-walk/scripts/probe_s0_raster.py --source /path/to/shiro_approved_reference.jpeg --model /local/isnetis.onnx --output /private/local-pilot
python experiments/shiro-front-walk/scripts/probe_s0_hidden_paint.py --source /path/to/shiro_approved_reference.jpeg --output /private/local-pilot/hidden2
```

First-attempt polygon is recorded in `hidden_paint_attempt1.json`; final script uses the corrected mask. The final verifier inspects both preserved attempts and exact stored-layer compositing; it cannot decide illustration quality.

## Remaining blocker / next executable choices

The tested local interpolation does not reconstruct semantic skin/cloth contours, star patterns or frill geometry. There is no demonstrated local, style-matching hidden-region authoring capability yet. Segmentation cannot recover occluded anatomy, and more frames/Unity rigging cannot repair this source defect.

Next small test must use either skilled local raster painting (clone/paint with explicit contour reconstruction and visual approval) or a supplied layered source with completed joints. A local generative inpainting model would require separately verified license/runtime and a bounded identity-preservation test; it was **not** executed or validated here. No cloud upload, paid service or external commission is authorized by this recommendation. Do not repeat Telea/NS on larger body regions or return to vector redraw/independent full-body generation.
