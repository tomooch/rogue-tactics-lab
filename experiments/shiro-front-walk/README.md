# Current PR #12 continuation: head/torso alignment A/B

Human watched A and judged the lateral head/torso shift unnatural. Current B is a reversible registration experiment, not accepted art. `RESULT.md` starts with current measurements/limits; the original sole-based result remains below as historical evidence.

A=`output/` unchanged at baseline0572960. B=`alignment/B/`. Canonical source/input files remain unchanged. `build_alignment_ab.py` performs integer X translation only, using annotated eye midpoint and blue collar brooch composite. It preserves all existing resized RGBA pixels, scale and Y positions. The old sole QA is retained for A; B checks anatomical composite X and records both soles instead of fixing them.

Reproduce from this directory (Python/Pillow/NumPy/ffmpeg already available):

```sh
python3 scripts/build_alignment_ab.py
python3 scripts/validate_alignment.py
python3 -m unittest discover -s tests -v
```

B uses explicit `alignment/landmarks.json` source-coordinate annotations; annotated crops and trajectories explain their meaning. No ImageGen invocation is needed or permitted for this comparison. A/B standalone and stacked comparison movies use identical384px display size,390×844,H264/yuv420p,8fps,6sec with light/dark halves. Old A MP4 remains unchanged.

To install generated B into the isolated Unity fixture, copy `alignment/B/shiro_walk_front_atlas.png` to `../../unity/Assets/ShiroSpike/Art/shiro_walk_front_atlas.png` and `alignment/B/unity_landmarks.json` to `../../unity/Assets/ShiroSpike/Art/shiro_walk_front_manifest.json`; then run exact6000.3.25f1 `ShiroSpikeSetup.Configure`. The Editor generates slices/controller/metadata. Common canvas pivot(256,256), stable root, no per-frame sole locking. Actual tests must use a committed B source revision; `verify_unity.py` records it.

The earlier generation/cleanup pipeline and approvals remain documented in the historical result and Git baseline0572960. The private original references and rejected candidates are preserved outside tracked final assets. Human accepts/rejects naturalness and cuteness separately from technical QA.
