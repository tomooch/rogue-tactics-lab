"""Verify private local raster trial evidence; technical checks never imply art PASS."""
import argparse
import hashlib
import io
import json
import subprocess
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--source',required=True); p.add_argument('--artifacts',required=True)
    p.add_argument('--report',required=True)
    a=p.parse_args(); root=Path(a.artifacts)
    sha=hashlib.sha256(Path(a.source).read_bytes()).hexdigest()
    assert sha=='59e6186b126d1c7c5ac05e577340af79bb5eddb04d0aaff270398686c465d317'
    src=np.array(Image.open(a.source).convert('RGB'))
    extract=np.array(Image.open(root/'source_rgb_raw_alpha.png'))
    assert extract.shape==(1448,1086,4)
    assert np.array_equal(extract[:,:,:3],src)
    runs=[]
    for folder in ['hidden','hidden2']:
        mask=np.array(Image.open(root/folder/'paint_mask.png'))>0
        for method in ['telea','navier_stokes']:
            with zipfile.ZipFile(root/folder/f'{method}_editable.ora') as z:
                assert z.read('mimetype')==b'image/openraster'
                tree=ET.fromstring(z.read('stack.xml'))
                layers=list(tree.find('stack'))
                assert len(layers)==2
                merged=Image.new('RGBA',(1086,1448))
                for layer in reversed(layers):
                    merged.alpha_composite(Image.open(io.BytesIO(z.read(layer.attrib['src']))).convert('RGBA'))
                stored=Image.open(io.BytesIO(z.read('mergedimage.png'))).convert('RGBA')
                assert np.array_equal(np.array(merged),np.array(stored))
                assert np.array_equal(np.array(Image.open(io.BytesIO(z.read('data/source.png'))))[:,:,:3],src)
                output=np.array(merged)[:,:,:3]
                assert np.array_equal(output[~mask],src[~mask])
                assert np.any(output[mask]!=src[mask])
                runs.append({'attempt':folder,'method':method,'mask_pixels':int(mask.sum()),
                             'outside_mask_changes':0,'stored_layer_composite_max_difference':0})
    files={str(f.relative_to(root)):hashlib.sha256(f.read_bytes()).hexdigest()
           for f in sorted(root.rglob('*')) if f.is_file()}
    report={'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),
            'canonical_source_sha256':sha,'rgba_source_rgb_exact':True,'ora_trials':runs,
            'artifact_sha256':files,'technical_preservation_checks':'PASS',
            'art_gate':'FAIL per actual-image review; not overridden by these checks',
            'three_poses':'NOT_EXECUTED','walk':'NOT_EXECUTED','unity':'NOT_EXECUTED'}
    Path(a.report).write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report),flush=True)


if __name__=='__main__': main()
