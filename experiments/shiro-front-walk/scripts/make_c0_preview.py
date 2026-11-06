#!/usr/bin/env python3
"""Diagnostic holds of 3 failed C0 poses, explicitly not a walking loop."""
import hashlib,json,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];P=R/'continuity/C0'
names=['01_left_contact','02_passing','03_right_contact'];frames=[]
for t in range(48):
 i=t//16;dark=t>=24;bg=Image.new('RGB',(390,844),(14,17,23) if dark else (235,242,252));ink='white' if dark else 'black';d=ImageDraw.Draw(bg)
 d.text((10,12),'C0 FEASIBILITY FAIL / NOT A WALK LOOP',fill=ink)
 d.text((10,36),f'Trial {i+1}/3 / held 2 seconds / {names[i]}',fill=ink)
 im=Image.open(P/'poses'/f'{names[i]}.png');whole=im.resize((384,384),Image.Resampling.LANCZOS);bg.paste(whole,(3,86),whole)
 detail=im.crop((170,300,330,474)).resize((300,326),Image.Resampling.NEAREST);bg.paste(detail,(45,508),detail)
 d.text((10,484),'FAIL: clothing fragments / joint cut seams',fill=ink)
 frames.append(bg.tobytes())
path=P/'C0_failed_three_pose_diagnostic_iPhone.mp4'
subprocess.run(['ffmpeg','-v','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','390x844','-r','8','-i','-','-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(path)],input=b''.join(frames),check=True)
subprocess.run(['ffmpeg','-v','error','-i',str(path),'-f','null','-'],check=True)
probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=width,height,codec_name,pix_fmt,r_frame_rate','-show_entries','format=duration','-of','json',str(path)]))
s=probe['streams'][0];assert [s['width'],s['height']]==[390,844] and s['codec_name']=='h264' and s['pix_fmt']=='yuv420p' and s['r_frame_rate']=='8/1'
(P/'diagnostic_video.json').write_text(json.dumps({'file':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'probe':probe,'meaning':'3 static failed feasibility poses held 2 seconds each. 8fps container is NOT a six-frame walk, NOT 0.75s animation.','full_decode':'PASS','Human_naturalness':'not assessed'},indent=2)+'\n')
print('C0_DIAGNOSTIC_VIDEO_PASS codec/decode only; C0 still FAIL',flush=True)
