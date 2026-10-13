#!/usr/bin/env python3
"""C0 evidence validator. Emits a FAIL gate honestly; geometry is only one part."""
import hashlib,json,math,subprocess,sys
from pathlib import Path
import numpy as np
from PIL import Image,ImageFilter
from clean_contours import mask_colors
R=Path(__file__).resolve().parents[1];P=R/'continuity/C0'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
base=json.loads((P.parent/'baseline_inventory.json').read_text());preserved=[]
for rel,h in base['files'].items():
 assert sha(R/rel)==h,f'Preserved A/B file changed: {rel}'
 preserved.append(rel)
source=Image.open(R/'input/shiro_walk_front_01.png');bind=Image.open(P/'bind_reconstruction.png')
np.testing.assert_array_equal(np.array(source),np.array(bind))
head=Image.open(P/'parts/head_face_hat.png')
np.testing.assert_array_equal(np.array(source)[340:490,535:800],np.array(head)[340:490,535:800])
manifest=json.loads((P/'pose_manifest.json').read_text());parts=json.loads((P/'parts_manifest.json').read_text());rows=[];bone_lengths=[]
for e in manifest['poses']:
 name=e['pose'];im=Image.open(P/'poses'/f'{name}.png');assert im.mode=='RGBA' and im.size==(512,512)
 a=np.array(im);alpha=a[:,:,3];assert not np.any(a[alpha==0,:3]);bb=im.getchannel('A').getbbox();margin=[bb[0],bb[1],512-bb[2],512-bb[3]];assert min(margin)>=40
 protect=np.array(Image.fromarray((alpha>=32).astype('uint8')*255).filter(ImageFilter.MinFilter(3)))>0
 fringe=int((mask_colors(a)&(alpha>0)&~protect).sum())
 for M in e['transforms_source_space'].values():np.testing.assert_allclose(np.array(M)[:2,:2].T@np.array(M)[:2,:2],np.eye(2),atol=1e-10)
 j=e['joints'];lengths={side:{'hip_knee':math.dist(j['hip'+side],j['knee'+side+'_upper']),'knee_ankle':math.dist(j['knee'+side+'_lower'],j['ankle'+side])} for side in ['L','R']}
 bone_lengths.append(lengths)
 soles={}
 for side,part,point in [('L','left_shin_boot',[724,1242]),('R','trial_right_shin_boot',[586,1242])]:
  src=(np.array(e['transforms_source_space'][part])@np.array([*point,1]))[:2];cell=(np.array(manifest['normalization'])@np.array([*src,1]))[:2];soles[side]=list(cell)
 rows.append({'pose':name,'sha256':sha(P/'poses'/f'{name}.png'),'RGBA_size':'PASS','margins_ltrb_px':margin,'boundary':'PASS','known_exterior_mask_pixels':fringe,'fringe':'PASS' if fringe==0 else 'FAIL','numeric_hinge_error_source_px':e['hinge_point_error'],'joint_lengths_source_px':lengths,'sole_points_measured_cell_px':soles,'semantic_phase':'NOT_VALIDATED; names are intended trial poses, not gait acceptance'})
for side in ['L','R']:
 for bone in ['hip_knee','knee_ankle']:assert max(e[side][bone] for e in bone_lengths)-min(e[side][bone] for e in bone_lengths)<1e-8
review=json.loads((P/'visual_review.json').read_text())
result={'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,text=True).strip(),'C0_gate':'FAIL','C1':'NOT_RUN_BY_GATE','baseline_A_B_inventory_hashes_preserved':True,'preserved_file_count':len(preserved),'bind_reconstruction_bitwise_equal':True,'source_face_ROI_bitwise_equal_in_common_head_part':True,'common_head_part_sha256':sha(P/'parts/head_face_hat.png'),'face_render_note':'Common source texture reused; fractional source transforms and resampling may alter export pixels. No claim rendered face pixels identical or natural gait proven.','scale':manifest['common_scale'],'same_scale_all_poses':True,'numeric_joint_length_constancy':'PASS; trial points are inferred, not measured hidden anatomy','frames':rows,'visual_review':review,'Unity_C':'NOT_RUN; C0 failed before C1','Human_C':'NOT_ASSESSED','no_art_adoption':True,'artifact_parts':[{ 'file':str(p.relative_to(R)),'sha256':sha(p)} for p in sorted((P/'parts').glob('*.png'))]}
(P/'qa.json').write_text(json.dumps(result,indent=2)+'\n')
print('C0_GATE_FAIL recorded; source/pixel/geometry guards PASS, fringe and visual articulation FAIL. C1 not run.',flush=True)
