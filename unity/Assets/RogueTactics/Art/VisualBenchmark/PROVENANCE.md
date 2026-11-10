# Visual benchmark art provenance

The Skygarden scene, terrain, props, clouds, UI shapes and icon motifs are
original procedural artwork authored for this repository in
`VisualBenchmarkSetup.cs`. No raw Human reference image or crop was imported,
copied into Unity, or included in the Git tree.

`PartyAtlas.png` and `EnemyAtlas.png` are new original illustrations generated
with the built-in imagegen tool on 2026-10-08. No external character/franchise
assets were used. They are character art only, sliced into separate transparent
sprites by the Unity Editor and individually placed in the actual 3D diorama.
They do not contain scenery or a full-screen composite. Compact portrait crops
are Unity sprite sub-assets from the same party atlas. Generation prompts and
asset hashes are recorded in `GENERATION.md`.

The scene uses an honest 2.5D technique: hand-authored 3D environment, lighting
and curved geometry + painted, camera-facing character sprites. These sprites
are static single poses, not rigged 3D models or animation sets. Material/lighting
and animation integration remain prototype limitations. The generated output is
project-owned artwork under [OpenAI's output terms](https://openai.com/policies/row-terms-of-use/#content); this is not a claim of
exclusive copyright or a legal guarantee of non-infringement. No paid source
asset or new service/account was introduced.

Human-provided AI concept illustrations and crops were viewed read-only as
composition/style references. Their original private folder remains local and
ignored. The scene is a fictional, static art/composition experiment. No asset
purports to implement Core planning, combat, skills or gameplay decisions.

## Third-party font

`NotoSansJP.ttf`: unmodified official Google Fonts Noto Sans JP variable font.
Downloaded 2026-10-08 from:
https://raw.githubusercontent.com/google/fonts/main/ofl/notosansjp/NotoSansJP%5Bwght%5D.ttf

License source:
https://raw.githubusercontent.com/google/fonts/main/ofl/notosansjp/OFL.txt

SIL Open Font License 1.1 permits bundling/redistribution with its copyright and
license notice. The complete notice is included as `OFL.txt`; the font is not sold
separately or renamed. No paid asset, service, account or runtime dependency was
introduced. Unity built-in sphere/cube/cylinder meshes and URP shaders are used
under the project's existing Unity installation.
