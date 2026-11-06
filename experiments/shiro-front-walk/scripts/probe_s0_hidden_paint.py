"""Bounded local raster inpainting capability test, NOT a complete Shiro rig.

The lower staff occludes skin/cloth. Probe whether local continuation can recover
that small area before allowing expensive whole-character reconstruction.
All source-containing outputs must stay outside the repository.
"""
import argparse
import hashlib
import io
import json
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

import cv2
import numpy as np
from PIL import Image, ImageDraw

HASH = '59e6186b126d1c7c5ac05e577340af79bb5eddb04d0aaff270398686c465d317'


def png(im):
    b = io.BytesIO(); im.save(b, format='PNG'); return b.getvalue()


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--source', required=True)
    p.add_argument('--output', required=True)
    a = p.parse_args()
    source = Path(a.source)
    assert hashlib.sha256(source.read_bytes()).hexdigest() == HASH
    out = Path(a.output); out.mkdir(parents=True, exist_ok=True)
    original = Image.open(source).convert('RGB'); rgb = np.array(original)
    # Hand-authored source-space occluder mask, not an anatomy measurement.
    # Attempt 2: actual image review found attempt 1 left part of the gold tip
    # outside the mask. Cover the complete lower tip before judging inpainting.
    polygon = [(687,942),(713,947),(711,975),(699,983),
               (688,1007),(689,1033),(648,1057),(636,1008),
               (650,1001),(665,976),(674,952)]
    mask = np.zeros(rgb.shape[:2], np.uint8)
    cv2.fillPoly(mask, [np.array(polygon, np.int32)], 255)
    mask = cv2.dilate(mask, np.ones((5,5), np.uint8))
    Image.fromarray(mask).save(out/'paint_mask.png')
    crop = (580,920,755,1080)
    review = Image.new('RGB', (700*3, 680), (235,235,240))
    review.paste(original.crop(crop).resize((700,640)), (0,40))
    d = ImageDraw.Draw(review); d.text((10,10), 'SOURCE / STAFF OCCLUSION', fill='black')
    metrics = []
    for i, (name, method) in enumerate([('telea',cv2.INPAINT_TELEA),('navier_stokes',cv2.INPAINT_NS)], 1):
        painted = cv2.inpaint(rgb, mask, 5, method)
        assert np.array_equal(painted[mask==0], rgb[mask==0])
        patch = Image.fromarray(np.dstack((painted,mask)))
        patch.save(out/f'{name}_paint_layer.png')
        merged = original.convert('RGBA'); merged.alpha_composite(patch)
        merged.save(out/f'{name}_composite.png')
        review.paste(merged.convert('RGB').crop(crop).resize((700,640)), (700*i,40))
        d.text((700*i+10,10), f'{name.upper()} / NOT ART PASS', fill='black')
        root = ET.Element('image', {'w':str(original.width),'h':str(original.height),'name':'Hidden paint feasibility only'})
        stack = ET.SubElement(root,'stack')
        for label, src in [('local_hidden_paint','data/paint.png'),('original_untouched','data/source.png')]:
            ET.SubElement(stack,'layer',{'name':label,'src':src,'opacity':'1.0','visibility':'visible','composite-op':'svg:src-over','x':'0','y':'0'})
        with zipfile.ZipFile(out/f'{name}_editable.ora','w') as z:
            z.writestr('mimetype','image/openraster',compress_type=zipfile.ZIP_STORED)
            z.writestr('stack.xml',ET.tostring(root)); z.writestr('data/source.png',png(original.convert('RGBA')))
            z.writestr('data/paint.png',png(patch)); z.writestr('mergedimage.png',png(merged))
        metrics.append({'method':name,'changed_pixels':int(np.any(painted!=rgb,axis=2).sum()),
                        'outside_mask_changed_pixels':int(np.any(painted[mask==0]!=rgb[mask==0],axis=1).sum()),
                        'editable_layers':2,'art_result':'REQUIRES_ACTUAL_IMAGE_REVIEW'})
    review.save(out/'hidden_paint_comparison.png')
    overlay = original.copy(); ov = Image.new('RGBA',original.size)
    ov.putalpha(Image.fromarray((mask.astype(np.float32)*0.45).astype(np.uint8)))
    # Use visible magenta for scope diagnosis, without altering trial inputs.
    ov.paste((255,0,150), (0,0,original.width,original.height), Image.fromarray(mask))
    overlay=Image.alpha_composite(original.convert('RGBA'),ov)
    overlay.crop(crop).resize((700,640)).save(out/'mask_scope.png')
    report={'source_sha256':HASH,'scope':'bounded staff occlusion fill, not whole-character separation',
            'polygon_source_pixels':polygon,'dilation_px':2,'mask_pixels':int((mask>0).sum()),
            'crop':crop,'methods':metrics,'original_unchanged':hashlib.sha256(source.read_bytes()).hexdigest()==HASH,
            'new_hidden_rgb_generated':True,'complete_anatomy_or_layers':False,'poses_created':False,
            'external_upload':False,'art_gate':'PENDING'}
    (out/'hidden_paint_metrics.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report),flush=True)


if __name__=='__main__': main()
