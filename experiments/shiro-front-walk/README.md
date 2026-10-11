# Shiro front walk — Issue #9 spike

Scope: one illustrated character, front walk in place, six independently generated poses. No gameplay changes. Art and gait remain provisional until Human judges the actual loop.

Baseline: `ab9fa5dc729dcea07f8680e7b22544627c5e5bed`. Exact Editor: `6000.3.25f1`.
Packet SHA256: `e66a730a2aa5e349ec3b5c573421e137ce5730241e18c8e9c4af54aa4c493f42`.

The Human explicitly approved the packet's displayed face/full-body candidates for this task (「よい」). Private original references remain outside Git. This establishes the spike's reference identity, not acceptance of generated animation or the product direction.

Production method: built-in ImageGen, one actual pose per call, canonical face/full-body plus selected first pose as grounding. Independent RGBA sources; single uniform scale and explicit sole anchors; deterministic 3×2 atlas and QA. No prior rejected leg-cutout asset, no fabricated in-between pose, no paid/new external service.

Order: L contact → L support → R swing → R contact → R support → L swing (character's left/right, not viewer's).

Reproduce with Python, Pillow, NumPy and ffmpeg already available locally:

```sh
python3 -m unittest discover -s tests -v
python3 scripts/clean_contours.py --source source --input input --qa qa
python3 scripts/build_walk.py --input-dir input --output-dir output
python3 scripts/validate_final.py
```

Only the isolated `unity/Assets/ShiroSpike` scene is used for the import experiment. The default Stage0 scene and build settings stay intact. Texture: RGBA, 1536×1024, no rescale/compression/mips, bilinear/clamp, 100 pixels per unit. Six native Sprite.Create assets slice the atlas using exact 512×512 rects and shared pivot (256,42) in Unity's bottom-up coordinates. An 8fps legacy animation clip loops six sprites; an Editor coroutine entering actual Play Mode observes actual advancing sprites and renders light/dark 390×844 frames. No new Sprite Editor package dependency.

The current explicit task requests Unity import/playback evidence as part of the spike, superseding the packet's earlier sequence that placed optional Unity proof after Human gait acceptance. Unity proof must not be called Human gait acceptance.

Machine QA can establish geometry/transparency invariants and rendering, but cannot certify identity, natural gait, anatomical contact or a fringe-free painted contour. Record those separately in the final report. No iOS/Android device or Human play result is inferred from previews.

Human AUTO-GO authorized contour-only cleanup and larger placement. Originals in `source/` are preserved by SHA256; per-frame face ROI and protected interiors are unchanged. `qa/*_difference.png` shows before/after/changed pixels. Final character height is approximately 414px; machine geometry/transparency/anchor/codec QA passes. Known exterior mask colors are removed, including low-alpha Lanczos ringing after normalization. This establishes technical candidate readiness only.

`diagnostic/` and `qa/diagnostic-result.json` retain the early undersized, fringe-contaminated FAIL for comparison. They are not the final output. Rejected generation hashes and reasons remain recorded; rejected files stay local and are excluded from Git.
