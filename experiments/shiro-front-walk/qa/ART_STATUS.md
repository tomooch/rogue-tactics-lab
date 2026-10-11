# Art status — final candidates, Human acceptance pending

## FACT
Six independently authored pose candidates exist in the requested order. First-pass frame 6 had the wrong support foot and was rejected. A fringe repair and two background-contaminated alternatives were also rejected; hashes/reasons are recorded. A pose-only edit of frame 4 supplies the selected sixth source.

Human AUTO-GO authorized contour correction and rescaling. Source SHA256 values are unchanged. Protected interiors and each frame’s face ROI have zero changes. Exterior masking colors and detached low-alpha contamination were removed; adjacent antialiasing remains. Six before/after/difference sheets are retained. The final atlas passes independent geometry, transparency, anchors, known-fringe and full MP4 decode checks. Character height is approximately 414px. Six synthetic regression checks pass. Unity 6000.3.25f1 imported and generated the six native slice assets, loop clip and isolated scene successfully; actual Animator playback on commit `6ed401b866d7a6c77cea8ad8a12ed067e3468740` reached all six sprites and rendered 12 portrait images.

## INTERPRETATION
Reference grounding retains recognizable Shiro, but independent generation does not reliably enforce support phase or clean cutouts. Each frame’s unchanged face does not mean all six faces are identical. Support-foot anchoring causes lateral torso displacement between the two half-cycles; mouth/eye variations and the loop transition can affect perceived naturalness.

## UNRESOLVED
Human has not accepted naturalness or cuteness. Actual Unity playback and rendering PASS; see `unity-verification.json`. No physical iPhone/device or human play has been observed. The early 154px diagnostic is historical FAIL, superseded by the approximately 414px candidate.

## NEXT
Review the observed Unity evidence and captured light/dark frames. Publish the candidate evidence in a Draft PR for Human assessment; do not promote rejected assets or technical PASS to formal art acceptance.
