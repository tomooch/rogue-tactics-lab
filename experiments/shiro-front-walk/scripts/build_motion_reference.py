#!/usr/bin/env python3
"""Reproducible manual-reference diagnostic, never Shiro finished artwork.
No footage/model downloads. Measured proxies and designed side rendering separate.
"""
import json,math,subprocess,hashlib
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];P=R/'continuity/motion-reference'
a=json.loads((P/'manual_landmarks.json').read_text());rows=a['samples']
raw=np.array([[r['points'][n] for n in ['A_hip_proxy','A_knee_proxy','A_ankle_proxy','B_hip_proxy','B_knee_proxy','B_ankle_proxy']] for r in rows],float)
angles=[]
for q in raw:
 angles.append([math.atan2(q[j+1,0]-q[j,0],q[j+1,1]-q[j,1]) for j in [0,1,3,4]])
angles=np.array(angles);knots=np.arange(9)/8
# Endpoint is explicitly replaced for this DESIGN curve. Actual residual reported below.
closed=angles.copy();closed[-1]=closed[0]
def angle(u):return np.array([np.interp(u,knots,closed[:,j]) for j in range(4)])
def pose(u):
 v=angle(u);hips=[];knees=[];feet=[];depth=[]
 for side in range(2):
  x=256+(-17 if side==0 else 17);h=np.array([x,355.]);t,s=v[2*side:2*side+2];k=h+[-50*math.sin(t),50*math.cos(t)];f=k+[-60*math.sin(s),60*math.cos(s)]
  hips.append(h);knees.append(k);feet.append(f);depth.append(-50*math.sin(t)-60*math.sin(s))
 # Geometric ground presentation; no contacting shoe X anchor. No anatomical force inference.
 dy=470-max(f[1] for f in feet)
 for e in hips+knees+feet:e[1]+=dy
 head=[256,210+float(np.clip(.35*dy,-4,4))];chest=[256,300+dy]
 return {'phase':u,'angles_sagittal_rad':v.tolist(),'hips':np.array(hips).tolist(),'knees':np.array(knees).tolist(),'ankles':np.array(feet).tolist(),'depth_proxy_px':depth,'head_design':head,'chest_design':chest,'pelvis_design':[256,355+dy]}
def draw(u):
 q=pose(u);im=Image.new('RGB',(512,512),'#f5f0e6');d=ImageDraw.Draw(im)
 d.line((50,480,462,480),fill='#888888',width=2)
 d.ellipse((q['head_design'][0]-65,q['head_design'][1]-65,q['head_design'][0]+65,q['head_design'][1]+65),outline='#555555',width=4)
 d.line([tuple(q['head_design']),tuple(q['chest_design']),tuple(q['pelvis_design'])],fill='#555555',width=5)
 for j,col in [(0,'#d45151'),(1,'#327fbe')]:
  pts=[q['hips'][j],q['knees'][j],q['ankles'][j]];d.line([tuple(e) for e in pts],fill=col,width=7)
  for x,y in pts:d.ellipse((x-5,y-5,x+5,y+5),fill=col)
  x,y=pts[-1];r=9+max(-3,min(3,q['depth_proxy_px'][j]/20));d.ellipse((x-r,y-4,x+r,y+4),fill=col)
 d.text((12,12),'SIDE DESIGN SKELETON / NOT SHIRO ART',fill='black');d.text((12,35),f'phase={u:.3f} / red=A->L proposed, blue=B->R',fill='black')
 return im
samples={str(n):[dict(pose(i/n),target_time_s=i*.75/n,source_time_requested_s=round(10.56+1.52*i/n,6)) for i in range(n)] for n in [6,8]}
metrics={}
for n in [6,8]:
 err=np.array([np.degrees(angle(t)-angle(math.floor(t*n)/n)) for t in np.arange(960)/960])
 delta=np.array([np.degrees(angle(((i+1)%n)/n)-angle(i/n)) for i in range(n)])
 metrics[str(n)]={'cycle_s':.75,'frame_duration_s':.75/n,'equivalent_fps':n/.75,'held_angle_error_rms_deg':float(np.sqrt(np.mean(err**2))),'held_angle_error_max_deg':float(abs(err).max()),'last_to_first_angle_delta_deg':delta[-1].tolist(),'max_adjacent_angle_delta_deg':float(abs(delta).max()),'naturalness':'NOT_VALIDATED; metric against manually estimated, periodicized design curve only'}
res={'schema_version':1,'basis':'side-view manual proxy angles retargeted to short rigid bones; lengths/head all DESIGNED; frontal motion NOT produced','design_bones_px':{'thigh':50,'shin':60,'hip_half_width':17},'head_y_cap_px':4,'source_endpoint_proxy_residual_px':np.linalg.norm(raw[-1]-raw[0],axis=1).tolist(),'source_endpoint_angle_residual_deg':np.degrees(angles[-1]-angles[0]).tolist(),'loop_repair':'design endpoint explicitly set equal start; linear interpolation C0-continuous, velocity seam not resolved','unobserved':'front-plane sway, true hip centres, head/chest/shoulder/arms; no force/foot-slip quantification','samples':samples,'comparison':metrics}
(P/'motion_samples.json').write_text(json.dumps(res,indent=2)+'\n')
for n in [6,8]:
 contact=Image.new('RGB',(512*n,512),'white')
 for i in range(n):contact.paste(draw(i/n),(512*i,0))
 contact.save(P/f'skeleton_{n}_poses.jpg')
# Raw side-view annotation chart (no source imagery reproduced).
out=Image.new('RGB',(1600,700),'white');d=ImageDraw.Draw(out)
for i,r in enumerate(rows):
 x=(i%5)*320;y=(i//5)*340
 d.text((x+8,y+8),f"requested {r['requested_time_s']:.2f}s / p={r['phase']:.3f}",fill='black')
 for side,col in [('A','red'),('B','blue')]:
  pts=[r['points'][side+'_'+j+'_proxy'] for j in ['hip','knee','ankle']];pts=[(x+(px-350),y+py-40) for px,py in pts];d.line(pts,fill=col,width=4)
  for px,py in pts:d.ellipse((px-4,py-4,px+4,py+4),fill=col)
 d.text((x+8,y+280),'Estimated 8..15+px; L/R not verified',fill='black')
d.text((8,680),'Macleod et al. 2014 Video S2 / CC BY 4.0 / estimated proxies; no source frames reproduced',fill='black');out.save(P/'manual_side_proxy_chart.jpg')
# 96fps carrier: six holds=12ticks, eight holds=9ticks, cycle=72ticks=0.75s.
args=['/opt/homebrew/bin/ffmpeg','-v','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','390x844','-r','96','-i','pipe:0','-an','-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(P/'skeleton_6_vs_8_iPhone.mp4')]
p=subprocess.Popen(args,stdin=subprocess.PIPE)
for t in range(576):
 u=(t%72)/72;im=Image.new('RGB',(390,844),'#f5f0e6');d=ImageDraw.Draw(im)
 for n,y in [(6,35),(8,435)]:
  im.paste(draw(math.floor(u*n)/n).resize((384,384)),(3,y));d.text((12,y-20),f'{n} samples / 0.75s / {n/.75:.4f} fps',fill='black')
 d.text((12,825),'NOT gait acceptance / no Shiro imagery',fill='black');p.stdin.write(im.tobytes())
p.stdin.close();assert p.wait()==0
subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-i',str(P/'skeleton_6_vs_8_iPhone.mp4'),'-f','null','-'],check=True)
print(json.dumps(metrics,indent=2))
