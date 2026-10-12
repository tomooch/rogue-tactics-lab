#!/usr/bin/env python3
"""Configure an already Editor-created ISOLATED sample-only Unity project."""
import json,shutil,tarfile,sys
from pathlib import Path
project=Path(sys.argv[1]).resolve();archive=Path(sys.argv[2]).resolve();root=Path(__file__).resolve().parents[1]
assert project==Path('/private/tmp/shiro-R0-unity'), 'Never configure production unity/'
assert '6000.3.25f1' in (project/'ProjectSettings/ProjectVersion.txt').read_text()
m=project/'Packages/manifest.json';j=json.loads(m.read_text());j['dependencies']['com.unity.2d.animation']='13.0.0';j['dependencies']['com.unity.2d.psdimporter']='12.0.0';m.write_text(json.dumps(j,indent=2)+'\n')
# Extract data safely to scratch and copy exact official Samples~ (license included).
tmp=Path('/private/tmp/R0-sample-source');tmp.mkdir(exist_ok=True)
with tarfile.open(archive) as t:t.extractall(tmp,filter='data')
src=tmp/'package/Samples~/AnimationSamples';dest=project/'Assets/Samples/2D Animation/13.0.0/Samples';dest.parent.mkdir(parents=True,exist_ok=True);shutil.copytree(src,dest,dirs_exist_ok=True)
shutil.copy2(root/'continuity/gates/R0/source/R0Probe.cs',project/'Assets/R0Probe.cs')
print('Isolated R0 configured: official samples+license, 2D Animation13.0.0 / PSDImporter12.0.0; production unity untouched')
