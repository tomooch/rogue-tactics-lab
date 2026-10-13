#!/usr/bin/env python3
"""Deterministic 6-frame front-walk sprite finalizer (not an image generator).

Requires six independent transparent PNG frames and explicit foot contact points.
Never decides that motion looks natural: that remains a human acceptance gate.
"""
import argparse, hashlib, json, math, subprocess, sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from clean_contours import mask_colors

CELL=512; PAD=40; COLS=3; ROWS=2; FOOT=(256,470)
NAMES=[f'shiro_walk_front_{i:02d}.png' for i in range(1,7)]
LABELS=['L contact','L compression','R passing','R contact','R compression','L passing']

class QAError(Exception): pass

def load_sources(indir):
    template_path=Path(indir)/'foot_anchors.json'
    if not template_path.exists():
        raise QAError(f'Missing {template_path} (explicit contact anchors required)')
    anchors=json.loads(template_path.read_text(encoding='utf-8'))
    results=[]
    for i,name in enumerate(NAMES):
        path=Path(indir)/name
        if not path.is_file(): raise QAError(f'Missing frame: {path}')
        src=Image.open(path)
        if src.mode!='RGBA': raise QAError(f'{name} must be RGBA PNG with real alpha; found {src.mode}')
        arr=np.asarray(src)
        if np.min(arr[:,:,3])==255: raise QAError(f'{name} is fully opaque. Remove background, do NOT label as transparency.')
        if any(int(arr[y,x,3])>8 for x,y in ((0,0),(src.width-1,0),(0,src.height-1),(src.width-1,src.height-1))):
            raise QAError(f'{name} has nontransparent corners / background bleed')
        # Do not silently crop away low-alpha haze or stray edge pixels.
        alpha=arr[:,:,3]>0
        ys,xs=np.nonzero(alpha)
        if len(xs)<200: raise QAError(f'{name} has too few opaque pixels')
        bb=(int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1)
        anchor=anchors.get(name,{}).get('foot_point')
        if not isinstance(anchor,list) or len(anchor)!=2 or not all(isinstance(v,(int,float)) for v in anchor):
            raise QAError(f'{name}: foot_point must be two explicit numbers, got {anchor}')
        fx,fy=map(float,anchor)
        if not (0<=fx<src.width and 0<=fy<src.height): raise QAError(f'{name}: foot_point out of image')
        if not math.isfinite(fx) or not math.isfinite(fy):
            raise QAError(f'{name}: nonfinite foot_point')
        ax,ay=round(fx),round(fy)
        if arr[ay,ax,3]<128:
            raise QAError(f'{name}: marked foot_point is not on a visible sole')
        if fy<bb[3]-max(8,int(src.height*0.08)):
            raise QAError(f'{name}: foot_point too high above lowest pixels (bbox y_max={bb[3]}). Verify sole contact, not skirt.')
        results.append((name,src,arr,bb,(fx,fy)))
    return results

def global_scale(items):
    max_left=max(fx-bb[0] for _,_,_,bb,(fx,fy) in items)
    max_right=max(bb[2]-fx for _,_,_,bb,(fx,fy) in items)
    max_top=max(fy-bb[1] for _,_,_,bb,(fx,fy) in items)
    max_bottom=max(bb[3]-fy for _,_,_,bb,(fx,fy) in items)
    limits=[(FOOT[0]-PAD)/max(1,max_left), (CELL-PAD-FOOT[0])/max(1,max_right),
            (FOOT[1]-PAD)/max(1,max_top), (CELL-PAD-FOOT[1])/max(1,max_bottom)]
    return min(1.0,*limits)*0.985

def assemble(items,outdir,make_video):
    outdir=Path(outdir); outdir.mkdir(parents=True,exist_ok=True)
    frameout=outdir/'frames';frameout.mkdir(exist_ok=True)
    qa=outdir/'qa';qa.mkdir(exist_ok=True)
    scale=global_scale(items)
    if not 0.1<scale<=1.0: raise QAError('Global scale impractical; inspect anchors and pose extents')
    atlas=Image.new('RGBA',(COLS*CELL,ROWS*CELL),(0,0,0,0))
    sheet=Image.new('RGB',(COLS*CELL,ROWS*CELL),(238,241,247))
    draw=ImageDraw.Draw(sheet)
    metrics=[];frames=[]
    for i,(name,src,arr,bb,(fx,fy)) in enumerate(items):
        cropped=src.crop(bb)
        w=max(1,round(cropped.width*scale));h=max(1,round(cropped.height*scale))
        im=cropped.resize((w,h),Image.Resampling.LANCZOS)
        ap=np.array(im)
        # Lanczos can create saturated low-alpha ringing in otherwise clean edges.
        # Remove only those mask-like pixels; no opaque design pixels are touched.
        ringing=mask_colors(ap)&(ap[:,:,3]>0)&(ap[:,:,3]<=32)
        ap[ringing]=0
        ap[ap[:,:,3]==0,:3]=0
        im=Image.fromarray(ap,'RGBA')
        # original anchor relative to bounding box, scaled along with image
        x=round(FOOT[0]-(fx-bb[0])*w/cropped.width)
        y=round(FOOT[1]-(fy-bb[1])*h/cropped.height)
        if x<PAD or y<PAD or x+w>CELL-PAD or y+h>CELL-PAD:
            raise QAError(f'{name} violates {PAD}px padding after alignment: {(x,y,x+w,y+h)}')
        frame=Image.new('RGBA',(CELL,CELL),(0,0,0,0));frame.alpha_composite(im,(x,y))
        # Strict guard on content touching outer PAD pixels
        a=np.asarray(frame)[:,:,3]
        if np.any(a[:PAD,:]) or np.any(a[-PAD:,:]) or np.any(a[:,:PAD]) or np.any(a[:,-PAD:]):
            raise QAError(f'{name}: actual alpha violates padding')
        if np.any((np.asarray(frame)[:,:,3]==0) & np.any(np.asarray(frame)[:,:,:3]!=0, axis=2)):
            raise QAError(f'{name}: transparent pixels have residual RGB')
        frame.save(frameout/name,optimize=True)
        frames.append(frame)
        ox=(i%COLS)*CELL;oy=(i//COLS)*CELL
        atlas.alpha_composite(frame,(ox,oy));sheet.paste(frame,(ox,oy),frame)
        draw.rectangle((ox,oy,ox+CELL-1,oy+CELL-1),outline=(160,178,199),width=3)
        draw.rectangle((ox+PAD,oy+PAD,ox+CELL-PAD,oy+CELL-PAD),outline=(54,147,108),width=2)
        draw.text((ox+12,oy+12),f'{i+1:02d} {LABELS[i]}',fill=(31,52,76))
        yy,xx=np.nonzero(a)
        metrics.append({'file':name,'source_size':src.size,'bbox':bb,'foot_point':[fx,fy],
                        'source_sha256':hashlib.sha256(src.tobytes()).hexdigest(),
                        'removed_resampling_mask_pixels':int(ringing.sum()),
                        'placed_bbox':[x,y,x+w,y+h],
                        'alpha_bbox':[int(xx.min()),int(yy.min()),int(xx.max())+1,int(yy.max())+1],
                        'cell':[i%COLS,i//COLS], 'atlas_rect_top_left':[ox,oy,CELL,CELL],
                        'unity_rect_bottom_left':[ox,(ROWS-1-i//COLS)*CELL,CELL,CELL],
                        'placed_foot_point':[x+(fx-bb[0])*w/cropped.width,y+(fy-bb[1])*h/cropped.height],
                        'sha256':hashlib.sha256((frameout/name).read_bytes()).hexdigest()})
    atlas.save(outdir/'shiro_walk_front_atlas.png',optimize=True)
    sheet.save(qa/'contact_sheet.jpg',quality=91)
    result={'status':'TECHNICAL_QA_ONLY','pass':True,'frame_count':6,'frames_size':[CELL,CELL],
            'atlas_size':[COLS*CELL,ROWS*CELL],'padding_px':PAD,'foot_pixel':FOOT,'uniform_scale':scale,
            'natural_gait_human_approved':False,'unity_tested':False,'frames':metrics}
    (qa/'technical_validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (outdir/'manifest.json').write_text(json.dumps({'schema_version':1,'atlas':'shiro_walk_front_atlas.png',
        'atlas_size':[COLS*CELL,ROWS*CELL],'grid':[COLS,ROWS],'cell_size':[CELL,CELL],
        'fps':8,'pivot_bottom_left_px':[256,42],'art_approved':False,'frames':metrics},indent=2)+'\n')
    if make_video:
        # 6 frames at 8 fps -> 0.75 sec cycle, loop 8 times = 6 sec. transparent assets remain separate.
        w,h=390,844
        seq=qa/'video_sequence';seq.mkdir(exist_ok=True)
        for i in range(48):
            f=frames[i%6]
            dark=i>=24
            bg=Image.new('RGB',(w,h),(14,17,23) if dark else (235,242,252))
            d=ImageDraw.Draw(bg)
            ink=(215,225,239) if dark else (25,45,75)
            sz=448; mini=f.resize((sz,sz),Image.Resampling.LANCZOS);ty=110
            gy=ty+round(FOOT[1]*sz/CELL)
            d.line((15,gy,w-15,gy),fill=(110,135,160),width=1)
            bg.paste(mini,((w-sz)//2,ty),mini)
            feet=f.crop((150,355,365,475)).resize((340,190),Image.Resampling.LANCZOS)
            bg.paste(feet,(25,620),feet)
            d.text((12,16),'SHIRO FRONT WALK / HUMAN REVIEW PENDING',fill=ink)
            d.text((12,40),f'Frame {i%6+1}/6 / {"DARK" if dark else "LIGHT"} / 8 fps',fill=ink)
            d.text((12,588),'FEET DETAIL / check contact and sliding',fill=ink)
            bg.save(seq/f'{i:03d}.png')
        cmd=['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','8','-i',str(seq/'%03d.png'),'-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(qa/'shiro_walk_front_iPhone.mp4')]
        try: subprocess.run(cmd,check=True,timeout=90)
        except Exception as ex:
            (qa/'VIDEO_BLOCKER.txt').write_text(str(ex))
            raise QAError('Preview encoding failed; see VIDEO_BLOCKER.txt') from ex
        finally:
            for f in seq.glob('*.png'):f.unlink()
            seq.rmdir()
    return result

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input-dir',type=Path,required=True)
    parser.add_argument('--output-dir',type=Path,required=True)
    parser.add_argument('--no-video',action='store_true')
    a=parser.parse_args()
    try:
        r=assemble(load_sources(a.input_dir),a.output_dir,not a.no_video)
        print(f'TECHNICAL_QA_PASS: {r["frame_count"]} frames with {r["padding_px"]}px padding; human gait acceptance PENDING')
    except QAError as ex:
        print('TECHNICAL_QA_FAIL:',ex,file=sys.stderr);sys.exit(2)

if __name__=='__main__':main()
