#!/usr/bin/env python3
"""Independent observable guards for actual R0 Editor/PlayMode output; no mock render."""
from pathlib import Path
import json,hashlib,subprocess,shutil,math
import numpy as np
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];out=Path('/private/tmp/shiro-R0-output');p=R/'continuity/gates/R0';r=json.loads((out/'runtime.json').read_text());rows=r['frames'];assert r['editor']=='6000.3.25f1' and len(rows)==180 and not r['errors'];assert all(all(abs(v)<1e-7 for v in f['root'].values()) for f in rows)
assert all(f['skins'] and all(s['vertices']>=3 for s in f['skins']) for f in rows)
selected=[];sigs=[]
for bg,subset in [('light',rows[:90]),('dark',rows[90:])]:
 for k,target in enumerate([1/6,.5,5/6]):
  f=min(subset,key=lambda x:abs(x['phase']-target));src=out/'frames'/f"{f['frame']:03d}.png";im=Image.open(src);assert im.size==(390,844);a=np.array(im);bgpixel=a[0,0,:3].astype(int);nonbg=np.max(abs(a[:,:,:3].astype(int)-bgpixel),axis=2)>20;assert nonbg.sum()>1000
  assert not nonbg[0,:].any() and not nonbg[-1,:].any() and not nonbg[:,0].any() and not nonbg[:,-1].any(),'Capture clips at viewport boundary'
  name=f'{bg}_pose{k+1}.png';shutil.copy2(src,p/name);selected.append({'image':name,'frame':f['frame'],'phase':f['phase'],'boneEulerZ':f['boneEulerZ'],'skins':f['skins'],'nonbackground_pixels':int(nonbg.sum()),'sha256':hashlib.sha256(src.read_bytes()).hexdigest()})
  if bg=='light':sigs.append(f)
deltas=[]
for i,j in [(0,1),(1,2),(2,0)]:
 angles=np.array(sigs[j]['boneEulerZ'])-np.array(sigs[i]['boneEulerZ']);angles=(angles+180)%360-180;assert max(abs(angles))>15
 geom=max(abs(b['geometrySum']-a['geometrySum']) for a,b in zip(sigs[i]['skins'],sigs[j]['skins']));assert geom>1
 deltas.append({'pose_pair':[i+1,j+1],'max_bone_angle_delta_deg':float(max(abs(angles))),'max_skin_vertex_weighted_moment_delta':geom})
shutil.copy2(out/'runtime.json',p/'runtime.json')
contact=Image.new('RGB',(1170,884),'white');d=ImageDraw.Draw(contact)
for i,e in enumerate(selected[:3]):contact.paste(Image.open(p/e['image']).convert('RGB'),(390*i,40));d.text((390*i+8,10),f"Official Fei / pose{i+1} / phase={e['phase']:.3f}",fill='black')
contact.save(p/'three_actual_Unity_poses.jpg')
subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-y','-framerate','30','-i',str(out/'frames/%03d.png'),'-an','-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(p/'R0_actual_Unity_iPhone.mp4')],check=True)
subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-i',str(p/'R0_actual_Unity_iPhone.mp4'),'-an','-f','null','-'],check=True)
checks={'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,text=True).strip(),'editor':r['editor'],'capture_frames':len(rows),'root_fixed':'PASS','deformed_vertex_data_nonempty':'PASS','three_pose_bone_and_geometry_changes':'PASS','portrait_nonblank_and_not_boundary_clipped':'PASS','exception_errors':r['errors'],'pose_deltas':deltas,'captures':selected,'visual_joint_occlusion':'PENDING actual-image review; sorting data alone is not visual acceptance','Shiro_art':'NOT_EXECUTED','Human_gait':'NOT_ASSESSED','NUnit':'NOT_RUN; this is actual PlayMode capture probe, not Stage0 regression suite'}
(p/'verification.json').write_text(json.dumps(checks,indent=2)+'\n');print(json.dumps({k:v for k,v in checks.items() if k not in ['captures','pose_deltas']}))
