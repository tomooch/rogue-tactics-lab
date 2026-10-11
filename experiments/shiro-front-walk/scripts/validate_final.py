#!/usr/bin/env python3
"""Independent final file validation, not artistic or Human acceptance."""
import hashlib,json,subprocess
from pathlib import Path
import numpy as np
from PIL import Image,ImageFilter
from clean_contours import mask_colors

root=Path(__file__).resolve().parents[1]
out=root/'output';atlas_image=Image.open(out/'shiro_walk_front_atlas.png')
assert atlas_image.mode=='RGBA' and atlas_image.size==(1536,1024)
atlas=np.array(atlas_image)
manifest=json.loads((out/'manifest.json').read_text())
cleanup=json.loads((root/'qa/contour_cleanup.json').read_text())
hashes=[];records=[]
for i,entry in enumerate(manifest['frames']):
    path=out/'frames'/entry['file'];im=Image.open(path)
    assert im.mode=='RGBA' and im.size==(512,512)
    arr=np.array(im);a=arr[:,:,3];bb=im.getchannel('A').getbbox()
    margins=[bb[0],bb[1],512-bb[2],512-bb[3]]
    assert min(margins)>=40
    assert not (np.any(a[:40]) or np.any(a[-40:]) or np.any(a[:,:40]) or np.any(a[:,-40:]))
    assert not np.any(arr[a==0,:3])
    x=(i%3)*512;y=(i//3)*512
    np.testing.assert_array_equal(atlas[y:y+512,x:x+512],arr)
    fp=entry['placed_foot_point'];error=max(abs(fp[0]-256),abs(fp[1]-470))
    assert error<=0.501
    assert a[round(fp[1]),round(fp[0])]>0
    protect=np.array(Image.fromarray((a>=32).astype('uint8')*255).filter(ImageFilter.MinFilter(3)))>0
    fringe=int((mask_colors(arr)&(a>0)&~protect).sum());assert fringe==0
    h=hashlib.sha256(path.read_bytes()).hexdigest();assert h==entry['sha256'];hashes.append(h)
    original=root/'source'/entry['file'];clean=root/'input'/entry['file']
    report=next(r for r in cleanup['frames'] if r['file']==entry['file'])
    assert hashlib.sha256(original.read_bytes()).hexdigest()==report['source_sha256']
    before=np.array(Image.open(original));after=np.array(Image.open(clean))
    np.testing.assert_array_equal(before[340:490,535:800],after[340:490,535:800])
    assert report['protected_design_changes']==0
    records.append({'file':entry['file'],'margins_ltrb_px':margins,
        'foot_error_px':error,'known_contour_fringe_pixels':fringe,'sha256':h,
        'original_preserved':True,'face_roi_bitwise_unchanged':True})
assert len(hashes)==6 and len(set(hashes))==6
probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries',
    'stream=width,height,codec_name,pix_fmt,r_frame_rate','-show_entries','format=duration',
    '-of','json',str(out/'qa/shiro_walk_front_iPhone.mp4')]))
stream=probe['streams'][0]
assert [stream['width'],stream['height']]==[390,844] and stream['codec_name']=='h264'
assert stream['pix_fmt']=='yuv420p' and stream['r_frame_rate']=='8/1'
assert abs(float(probe['format']['duration'])-6)<0.01
# Decode the whole file; a metadata-only ffprobe pass is insufficient.
subprocess.run(['ffmpeg','-v','error','-i',str(out/'qa/shiro_walk_front_iPhone.mp4'),'-f','null','-'],check=True)
result={'machine_qa':'PASS','human_art_and_gait_acceptance':'PENDING','unity':'SEPARATE_EVIDENCE',
        'atlas_size':[1536,1024],'cell_size':[512,512],'grid':[3,2],
        'foot_target_px':[256,470],'rasterization_tolerance_px':0.5,
        'all_sources_preserved':True,'all_protected_design_regions_unchanged':True,
        'preview':probe,'frames':records}
(out/'qa/final_validation.json').write_text(json.dumps(result,indent=2)+'\n')
print('FINAL_MACHINE_QA_PASS six distinct frames; alpha/margins/atlas/anchors/fringe/originals/face/MP4 decode',flush=True)
