#!/usr/bin/env python3
"""Independent registration QA: prove exact pixel translation and reject old B sole lock."""
import hashlib,json,subprocess
from pathlib import Path
import numpy as np
from PIL import Image,ImageFilter
from clean_contours import mask_colors
r=Path(__file__).resolve().parents[1];p=r/'alignment'
a=json.loads((r/'output/manifest.json').read_text());b=json.loads((p/'B/manifest.json').read_text());l=json.loads((p/'landmarks.json').read_text())
assert b['fps']==a['fps']==8 and b['pivot_bottom_left_px']==[256,256]
assert b['uniform_scale']==json.loads((r/'output/qa/technical_validation.json').read_text())['uniform_scale']
assert b['alignment']=='head_torso_horizontal_composite'
atlas=Image.open(p/'B/shiro_walk_front_atlas.png');assert atlas.mode=='RGBA' and atlas.size==(1536,1024)
measure=[]
for i,(ae,be,lm) in enumerate(zip(a['frames'],b['frames'],l['frames'])):
 assert ae['file']==be['file']==lm['file']==f'shiro_walk_front_{i+1:02}.png'
 for folder,key in [('source','source_png_sha256'),('input','input_png_sha256')]:assert hashlib.sha256((r/folder/lm['file']).read_bytes()).hexdigest()==lm[key]
 original=Image.open(r/'output/frames'/ae['file']);frame=Image.open(p/'B/frames'/be['file']);assert frame.mode=='RGBA' and frame.size==(512,512)
 assert hashlib.sha256((p/'B/frames'/be['file']).read_bytes()).hexdigest()==be['sha256']
 abb=original.getchannel('A').getbbox();bb=frame.getchannel('A').getbbox();dx=bb[0]-abb[0]
 assert [bb[j]-abb[j] for j in range(4)]==[dx,0,dx,0]
 np.testing.assert_array_equal(np.array(frame.crop(bb)),np.array(original.crop(abb)))
 assert min(bb[0],bb[1],512-bb[2],512-bb[3])>=40
 np.testing.assert_array_equal(np.array(atlas.crop(((i%3)*512,(i//3)*512,(i%3+1)*512,(i//3+1)*512))),np.array(frame))
 arr=np.array(frame);alpha=arr[:,:,3];assert not np.any(arr[alpha==0,:3])
 protected=np.array(Image.fromarray((alpha>=32).astype('uint8')*255).filter(ImageFilter.MinFilter(3)))>0
 assert not np.any(mask_colors(arr)&(alpha>0)&~protected)
 # Independently recover body X from source landmark and A placement, not B's claimed QA status.
 vals=[]
 for k in ['face_center','torso_center']:
  src=lm[k];box=ae['bbox'];placed=ae['placed_bbox']
  x=placed[0]+(src[0]-box[0])*(placed[2]-placed[0])/(box[2]-box[0])+dx
  y=placed[1]+(src[1]-box[1])*(placed[3]-placed[1])/(box[3]-box[1])
  np.testing.assert_allclose([x,y],be['anatomical_landmarks'][k],atol=1e-9);vals.append(x)
 assert abs(sum(vals)/2-256)<=0.501
 assert be['translation_px']==[dx,0]
 measure.append({'file':ae['file'],'dx':dx,'composite_x':sum(vals)/2,'painted_pixels_identical':True})
assert len(measure)==6
# B cannot silently keep A's sole X lock; all six support soles are measured from the same artwork.
soles=[e['anatomical_landmarks'][e['anatomical_landmarks']['support_foot']+'_sole'][0] for e in b['frames']]
assert max(soles)-min(soles)>5
result={'status':'PASS','checks':'independent actual pixels/bbox/atlas/hash/source/semantic landmark transform and old sole-lock rejection','frames':measure,'support_sole_x_measured':soles,'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip(),'Human_B':'PENDING'}
(p/'independent_qa.json').write_text(json.dumps(result,indent=2)+'\n')
print('INDEPENDENT_ALIGNMENT_QA_PASS')
