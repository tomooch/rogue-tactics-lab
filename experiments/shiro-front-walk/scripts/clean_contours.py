#!/usr/bin/env python3
"""Human-authorized contour cleanup; no geometry, face or interior repainting.

Retains originals, removes unmistakable mask colors only in the outer 4px band,
and keeps low-alpha antialiasing only adjacent to the connected character.
"""
import argparse
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def expand(mask, size=3):
    return np.array(Image.fromarray(mask.astype('uint8') * 255).filter(ImageFilter.MaxFilter(size))) > 0

def mask_colors(arr):
    r,g,b=arr[:,:,:3].astype('int16').transpose(2,0,1)
    return ((r>180)&(g<70)&(b<100)&(r-np.maximum(g,b)>100)) | \
           ((b>220)&(g<65)&(r<85)) | ((r>180)&(b>180)&(g<100)) | \
           ((g>200)&(r<80)&(b<100))

def clean(image, seed, face_box=None):
    arr=np.array(image.convert('RGBA')); alpha=arr[:,:,3]
    protected=np.array(Image.fromarray((alpha>=32).astype('uint8')*255).filter(ImageFilter.MinFilter(9)))>0
    if face_box:
        x0,y0,x1,y1=face_box;protected[y0:y1,x0:x1]=True
    fringe=mask_colors(arr)&~protected
    valid=(alpha>=32)&~fringe
    # fromarray may expose a read-only shared buffer; floodfill needs a copy.
    connected=Image.fromarray(expand(valid).astype('uint8')*255).copy()
    if connected.getpixel(seed)!=255: raise ValueError('Seed is not on connected character')
    ImageDraw.floodfill(connected, seed, 128, thresh=0)
    component=np.array(connected)==128
    core=valid&component
    # Preserve fine alpha transitions immediately next to the real silhouette.
    keep=(alpha>0)&expand(core,3)&~fringe
    keep|=protected&(alpha>0)
    out=arr.copy();out[~keep]=0
    out[out[:,:,3]==0,:3]=0
    changed=np.any(out!=arr,axis=2)
    visible_changed=changed&(alpha>=32)
    if np.any(changed&protected&(alpha>0)):
        raise ValueError('Protected design pixels changed; stop')
    if visible_changed.sum()>np.count_nonzero(alpha>=32)*0.02:
        raise ValueError('Too much visible contour changed; stop for design review')
    remaining=mask_colors(out)&(out[:,:,3]>0)&~protected
    if np.any(remaining): raise ValueError('Known chroma fringe remains')
    metrics={'removed_mask_color_pixels':int((fringe&(alpha>0)).sum()),
             'removed_isolated_or_low_alpha_pixels':int(((alpha>0)&~keep&~fringe).sum()),
             'changed_visible_pixels':int(visible_changed.sum()),
             'protected_nontransparent_pixels':int((protected&(alpha>0)).sum()),
             'protected_design_changes':0,'remaining_known_mask_color_pixels':int(remaining.sum()),
             'original_alpha_bbox':image.getchannel('A').getbbox(),
             'clean_alpha_bbox':Image.fromarray(out[:,:,3]).getbbox()}
    return Image.fromarray(out),changed,metrics

def main():
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True)
    p.add_argument('--input',type=Path,required=True);p.add_argument('--qa',type=Path,required=True)
    args=p.parse_args();args.input.mkdir(parents=True,exist_ok=True);args.qa.mkdir(parents=True,exist_ok=True)
    records=[]
    for source in sorted(args.source.glob('shiro_walk_front_*.png')):
        before_hash=hashlib.sha256(source.read_bytes()).hexdigest()
        original=Image.open(source).convert('RGBA')
        result,difference,metrics=clean(original,(690,600),(535,340,800,490))
        dest=args.input/source.name;result.save(dest,optimize=True)
        if hashlib.sha256(source.read_bytes()).hexdigest()!=before_hash: raise ValueError('Original changed')
        metrics.update(file=source.name,source_sha256=before_hash,clean_sha256=hashlib.sha256(dest.read_bytes()).hexdigest())
        # Source-scale difference visualization; all three panels use the same scale.
        panels=[]
        for im in [original,result]:
            bg=Image.new('RGBA',im.size,(20,24,32,255));bg.alpha_composite(im)
            panels.append(bg.convert('RGB').resize((512,512),Image.Resampling.LANCZOS))
        heat=np.zeros_like(np.array(original));heat[difference]=[255,45,100,255]
        panels.append(Image.fromarray(heat).convert('RGB').resize((512,512),Image.Resampling.NEAREST))
        sheet=Image.new('RGB',(1536,544),(20,24,32));draw=ImageDraw.Draw(sheet)
        for i,(panel,label) in enumerate(zip(panels,['BEFORE / fringe FAIL','AFTER / contour only','CHANGED PIXELS / no pose edits'])):
            sheet.paste(panel,(i*512,32));draw.text((i*512+12,10),label,fill='white')
        sheet.save(args.qa/(source.stem+'_difference.png'))
        records.append(metrics)
    (args.qa/'contour_cleanup.json').write_text(json.dumps({'status':'CONTOUR_NORMALIZATION_ONLY',
        'human_authorization':'AUTO-GO: contour cleanup and suitable-scale placement; originals preserved',
        'method':'known saturated mask colors in exterior 4px band; isolated alpha pruning; preserve adjacent AA, face ROI and eroded interior',
        'frames':records},indent=2)+'\n')
    print('CONTOUR_CLEANUP_COMPLETE originals intact; protected design pixels unchanged',flush=True)

if __name__=='__main__':main()
