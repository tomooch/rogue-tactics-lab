# Current C-0 feasibility gate: FAIL, C-1 not run

Latest Human: A/B naturalness NG; B foot sliding not bothersome; separate pictures/no continuity. This revision tests shared-part feasibility first, not a finished six-frame animation. See RESULT.md and continuity/C0/qa.json. A/B/source/input/output/alignment files stay unchanged.

C-0 has 3 failed trial poses, common parts, hidden-paint candidates, manifests and seam diagnostics. One pelvis/upper-leg local completion and one bounded clothing repair still leave connected-paint/occlusion failures. C-1 six-frame generation/Unity is intentionally not run. No art adoption.

Reproduce failed trials (from this directory):

```sh
python3 scripts/build_c0_feasibility.py                  # first trial; original samples retained in C0/attempt1
python3 scripts/build_c0_feasibility.py --clothing-repair # bounded repair trial
python3 scripts/validate_c0_feasibility.py               # writes C0_gate FAIL honestly
python3 scripts/make_c0_preview.py                      # holds 3 failed poses, not a walk
```

The original candidate crops, built-in ImageGen prompts/hashes and final parts are retained. Source anatomy/pivots are inferred test values, not recovered invisible art. Numeric hinges and source face equality cannot qualify C0. The validator's successful process exit means evidence creation, not artistic or C0 gate PASS.

The earlier B alignment experiment remains in alignment/; B actual Unity source is f0776dd. Its 6frame/8fps/0.75s loop and evidence remain available unchanged. B Human naturalness is NG. No new C assets are imported into Unity.
