#!/usr/bin/env python3
"""Decode genuine frontal point-light source; 12 joint markers, no head invented."""
import argparse,subprocess,json,hashlib,math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
ap=argparse.ArgumentParser();ap.add_argument('source');a=ap.parse_args();p=Path(__file__).resolve().parents[1]/'continuity/gates/M0'
probe=json.loads(subprocess.check_output(['/opt/homebrew/bin/ffprobe','-v','error','-show_entries','frame=best_effort_timestamp_time','-select_streams','v:0','-of','json',a.source],text=True));pts=[float(x['best_effort_timestamp_time']) for x in probe['frames']]
proc=subprocess.Popen(['/opt/homebrew/bin/ffmpeg','-v','error','-i',a.source,'-an','-fps_mode','passthrough','-f','rawvideo','-pix_fmt','rgb24','pipe:1'],stdout=subprocess.PIPE)
names=['imageLeft_shoulder','imageRight_shoulder','imageLeft_elbow','imageRight_elbow','imageLeft_wrist','imageRight_wrist','imageLeft_hip','imageRight_hip','imageLeft_knee','imageRight_knee','imageLeft_ankle','imageRight_ankle'];prev=np.array([[847,116],[1099,100],[818,337],[1137,321],[837,530],[1115,523],[888,422],[1054,432],[905,759],[1047,772],[943,1066],[1025,1066]],float);rows=[];frames=[];nbytes=1920*1080*3
while True:
 b=proc.stdout.read(nbytes)
 if not b:break
 assert len(b)==nbytes
 im=np.frombuffer(b,np.uint8).reshape(1080,1920,3);mask=im.max(2)>140;mask[:,:600]=False;mask[:,1300:]=False;mask[980:1030,980:]=False
 yy,xx=np.where(mask);pool=set(zip(xx.tolist(),yy.tolist()));centres=[]
 while pool:
  seed=pool.pop();stack=[seed];group=[seed]
  while stack:
   x,y=stack.pop()
   for e in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
    if e in pool:pool.remove(e);stack.append(e);group.append(e)
  if len(group)<70:continue
  ar=np.array(group);lo=ar.min(0);hi=ar.max(0);wh=hi-lo+1
  if 8<=wh[0]<=25 and 8<=wh[1]<=25:centres.append(ar.mean(0))
 if not 10<=len(centres)<=12:raise RuntimeError(f'Unexpected marker count {len(centres)}')
 cand=np.array(centres);used=set();cur=np.full((12,2),np.nan)
 # continuity nearest assignment; raw geometric tracks, not certified anatomical L/R.
 for dist,j,k in sorted((float(np.linalg.norm(prev[j]-cand[k])),j,k) for j in range(12) for k in range(len(cand))):
  if j in used or k+100 in used:continue
  cur[j]=cand[k];used.add(j);used.add(k+100)
 rows.append({'frame':len(rows),'pts_s':pts[len(rows)],'markers':{name:(xy.tolist() if np.isfinite(xy).all() else None) for name,xy in zip(names,cur)},'head':None,'detected_count':len(centres),'pelvis_midpoint_proxy':((cur[6]+cur[7])/2).tolist() if np.isfinite(cur[6:8]).all() else None});prev=np.where(np.isfinite(cur),cur,prev)
 if len(rows)-1 in [0,23,46]:frames.append(Image.fromarray(im.copy()))
assert proc.wait()==0 and len(rows)==len(pts)
arr=np.array([[r['markers'][k] if r['markers'][k] is not None else [np.nan,np.nan] for k in names] for r in rows],dtype=float);ranges={k:(np.nanmax(arr[:,j],axis=0)-np.nanmin(arr[:,j],axis=0)).tolist() for j,k in enumerate(names)}
q={'source_url':'https://commons.wikimedia.org/wiki/File:Action-and-Emotion-Recognition-from-Point-Light-Displays-An-Investigation-of-Gender-Differences-pone.0020989.s004.ogv','source_sha256':hashlib.sha256(Path(a.source).read_bytes()).hexdigest(),'credit':'Alaerts K, Nackaerts E, Meyns P, Swinnen S, Wenderoth N (2011), PLOS ONE Video S3, doi:10.1371/journal.pone.0020989','license_display':'CC BY 2.5','license_url':'https://creativecommons.org/licenses/by/2.5/','metadata_license':'CC BY 3.0 (also linked for provenance https://creativecommons.org/licenses/by/3.0/)','modification':'Decoded centroid tracks and resized/reannotated diagnostic. No raw motion capture coordinates or private Shiro art.','M0':'PARTIAL: frontal 12-marker decode/observation successful; head and sole/contact force absent','emotion':'sad actor; not neutral/cute Shiro specification','tracking':'initial image-left/right then nearest temporal matching; anatomical L/R and occlusion not certified','decoded_frames':len(rows),'missing_marker_frames':[r['frame'] for r in rows if r['detected_count']<12],'gap_policy':'null; no interpolated/imputed observation; prior point only assists next assignment','rows':rows,'range_xy_px':ranges,'missing':['head','heel','toe','force','true floor contact'],'three_pose_frames':[0,int(np.nanargmax(arr[:,10,1]-arr[:,11,1])),int(np.nanargmin(arr[:,10,1]-arr[:,11,1]))],'three_pose_note':'observed snapshots, NOT named leftContact/Passing/rightContact without contact evidence'}
(p/'observations.json').write_text(json.dumps(q,indent=2)+'\n')
# Three source-linked marker charts; rendered derived from centroids rather than unlicensed photos.
chart=Image.new('RGB',(1170,844),'#111111');d=ImageDraw.Draw(chart)
for i,f in enumerate(q['three_pose_frames']):
 r=rows[f];off=i*390;d.text((off+10,15),f"SOURCE frame {f} / {r['pts_s']:.3f}s",fill='white')
 for j,k in enumerate(names):
  if r['markers'][k] is None:continue
  x,y=r['markers'][k];x=off+195+(x-970)*.58;y=60+y*.65;d.ellipse((x-5,y-5,x+5,y+5),fill='red' if j%2==0 else 'cyan')
 d.text((off+10,785),'12 markers / head NOT measured',fill='white')
d.text((10,825),'Alaerts et al. 2011 / PLOS ONE / CC BY2.5; annotated derivative',fill='white');chart.save(p/'three_observed_poses.jpg')
# Actual decoded trajectory playback at original frame timestamps; 390x844 MP4.
delta=np.diff(pts);q['pts_interval_min_max_s']=[float(delta.min()),float(delta.max())];q['movie_timing']='CFR derivative using median PTS delta; original timestamps retained in JSON, not original VFR muxing';(p/'observations.json').write_text(json.dumps(q,indent=2)+'\n')
fps=1/np.median(delta);out=subprocess.Popen(['/opt/homebrew/bin/ffmpeg','-v','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','390x844','-r',str(fps),'-i','pipe:0','-an','-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(p/'frontal_markers_iPhone.mp4')],stdin=subprocess.PIPE)
for r in rows:
 im=Image.new('RGB',(390,844),'#111111');d=ImageDraw.Draw(im);d.text((10,15),f"ACTUAL SOURCE TRACKS / {r['pts_s']:.3f}s",fill='white')
 for j,k in enumerate(names):
  if r['markers'][k] is None:continue
  x,y=r['markers'][k];x=195+(x-970)*.58;y=60+y*.65;d.ellipse((x-5,y-5,x+5,y+5),fill='red' if j%2==0 else 'cyan')
 d.text((10,770),'No head / no sole / sad walk',fill='white');d.text((10,802),'Alaerts et al.2011 PLOS ONE CC BY2.5',fill='white');out.stdin.write(im.tobytes())
out.stdin.close();assert out.wait()==0
print(json.dumps({'decoded_frames':len(rows),'range_xy_px':ranges,'M0':q['M0']}))
