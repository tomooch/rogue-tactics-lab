#!/usr/bin/env python3
"""One-variable registration experiment: integer X translation of exact A pixels.
A is immutable sole-registration baseline; B never hard-locks sole X.
"""
import hashlib,json,subprocess
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
from clean_contours import mask_colors
ROOT=Path(__file__).resolve().parents[1]
BASELINE='0572960aaf21855dff0b05467e8c64339a4480c1'

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def landmark_transform(point,entry):
    bb=entry['bbox'];x,y,x1,y1=entry['placed_bbox']
    return [x+(point[0]-bb[0])*(x1-x)/(bb[2]-bb[0]),y+(point[1]-bb[1])*(y1-y)/(bb[3]-bb[1])]
def translate(frame,dx):
    bb=frame.getchannel('A').getbbox()
    if bb[0]+dx<40 or bb[2]+dx>472:raise ValueError('B translation violates 40px gutter; do not shrink')
    out=Image.new('RGBA',(512,512));out.paste(frame,(dx,0))
    np.testing.assert_array_equal(np.array(frame.crop(bb)),np.array(out.crop(out.getchannel('A').getbbox())))
    return out

def validate_frame(a,b,dx,face,torso):
    assert a.mode==b.mode=='RGBA' and a.size==b.size==(512,512)
    bb=b.getchannel('A').getbbox();m=[bb[0],bb[1],512-bb[2],512-bb[3]];assert min(m)>=40
    np.testing.assert_array_equal(np.array(translate(a,dx)),np.array(b))
    ap=np.array(b);alpha=ap[:,:,3];assert not np.any(ap[alpha==0,:3])
    interior=np.array(Image.fromarray((alpha>=32).astype('uint8')*255).filter(ImageFilter.MinFilter(3)))>0
    assert not np.any(mask_colors(ap)&(alpha>0)&~interior)
    assert abs((face[0]+torso[0])/2-256)<=0.501,'B must use anatomical composite, not sole'
    return m

def summary(rows):
    out={}
    for key in ['face_center','torso_center','composite_center','left_sole','right_sole']:
        x=[r[key][0] for r in rows];y=[r[key][1] for r in rows]
        out[key]={'x':x,'y':y,'x_range':max(x)-min(x),'y_range':max(y)-min(y),
                  'delta_3_to_4':[x[3]-x[2],y[3]-y[2]],'delta_6_to_1':[x[0]-x[5],y[0]-y[5]],
                  'adjacent_delta_xy_including_wrap':[[x[(i+1)%6]-x[i],y[(i+1)%6]-y[i]] for i in range(6)]}
    out['support_sole']=[{'foot':r['support_foot'],'point':r[r['support_foot']+'_sole']} for r in rows]
    return out

def encode(frames,path):
    subprocess.run(['ffmpeg','-v','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','390x844','-r','8','-i','-',
        '-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(path)],input=b''.join(f.tobytes() for f in frames),check=True)
    probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=width,height,codec_name,pix_fmt,r_frame_rate','-show_entries','format=duration','-of','json',str(path)]))
    s=probe['streams'][0];assert (s['width'],s['height'],s['codec_name'],s['pix_fmt'],s['r_frame_rate'])==(390,844,'h264','yuv420p','8/1')
    assert abs(float(probe['format']['duration'])-6)<0.01
    subprocess.run(['ffmpeg','-v','error','-i',str(path),'-f','null','-'],check=True)
    return {'file':path.name,'sha256':sha(path),'probe':probe,'full_decode':'PASS'}

def preview(frame,bg,y):
    # All three previews use the same 384px cell display, including stacked A/B.
    mini=frame.resize((384,384),Image.Resampling.LANCZOS);bg.paste(mini,(3,y),mini)
    ImageDraw.Draw(bg).line((10,y+round(470*384/512),380,y+round(470*384/512)),fill=(110,135,160),width=1)

def main():
    work=ROOT/'alignment';out=work/'B';(out/'frames').mkdir(parents=True,exist_ok=True)
    manifest=json.loads((ROOT/'output/manifest.json').read_text());landmarks=json.loads((work/'landmarks.json').read_text())
    # Baseline artifacts must be exact originals, not a regenerated A.
    preserve=[]
    expected_paths=subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE,'--','experiments/shiro-front-walk/source','experiments/shiro-front-walk/input','experiments/shiro-front-walk/output'],cwd=ROOT.parent.parent,text=True).splitlines()
    paths=[ROOT.parent.parent/path for path in expected_paths]
    current={str(p.relative_to(ROOT.parent.parent)) for folder in ['source','input','output'] for p in (ROOT/folder).rglob('*') if p.is_file()}
    assert current==set(expected_paths),'Baseline file inventory missing/extra; preserve A in full'
    for path in sorted(p for p in paths if p.is_file()):
        rel=path.relative_to(ROOT.parent.parent)
        original=subprocess.check_output(['git','show',f'{BASELINE}:{rel}'],cwd=ROOT)
        assert hashlib.sha256(original).hexdigest()==sha(path),f'Baseline changed: {rel}'
        preserve.append({'file':str(path.relative_to(ROOT)),'sha256':sha(path)})
    (work/'baseline_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    aframes=[];bframes=[];arows=[];brows=[];entries=[];checks=[]
    for i,(e,l) in enumerate(zip(manifest['frames'],landmarks['frames'])):
        assert e['file']==l['file']
        assert sha(ROOT/'source'/l['file'])==l['source_png_sha256']
        assert sha(ROOT/'input'/l['file'])==l['input_png_sha256']
        inp=Image.open(ROOT/'input'/l['file'])
        for point in l['eye_centers']+[l['torso_center']]+list(l['sole_points'].values()):
            assert inp.getpixel((round(point[0]),round(point[1])))[3]>=128
        face=landmark_transform(l['face_center'],e);torso=landmark_transform(l['torso_center'],e)
        composite=[(face[j]+torso[j])/2 for j in range(2)];dx=round(256-composite[0])
        a=Image.open(ROOT/'output/frames'/e['file']);assert sha(ROOT/'output/frames'/e['file'])==e['sha256']
        b=translate(a,dx);b.save(out/'frames'/e['file'],optimize=True)
        row={'file':e['file'],'face_center':face,'torso_center':torso,'composite_center':composite,
             'left_sole':landmark_transform(l['sole_points']['left'],e),'right_sole':landmark_transform(l['sole_points']['right'],e),
             'support_foot':l['support_foot'],'phase':l['phase'],'body_alpha_bbox':a.getchannel('A').getbbox()}
        rb={k:([v[0]+dx,v[1]] if k in ['face_center','torso_center','composite_center','left_sole','right_sole'] else v) for k,v in row.items()}
        rb['body_alpha_bbox']=b.getchannel('A').getbbox()
        margins=validate_frame(a,b,dx,rb['face_center'],rb['torso_center'])
        # Preserve the old sole contract for A and measure (rather than enforce) it for B.
        support=row[row['support_foot']+'_sole'];assert max(abs(support[0]-256),abs(support[1]-470))<=0.501
        entry=dict(e);entry.update(sha256=sha(out/'frames'/e['file']),alignment='head_torso_horizontal_composite',translation_px=[dx,0],
            anatomical_landmarks=rb,alpha_bbox=rb['body_alpha_bbox'],placed_bbox=[e['placed_bbox'][0]+dx,e['placed_bbox'][1],e['placed_bbox'][2]+dx,e['placed_bbox'][3]],
            placed_foot_point=[e['placed_foot_point'][0]+dx,e['placed_foot_point'][1]])
        entry['source_contact_sole_point']=entry.pop('foot_point')
        entry['measured_support_sole_point']=entry.pop('placed_foot_point')
        entries.append(entry);aframes.append(a);bframes.append(b);arows.append(row);brows.append(rb)
        checks.append({'file':e['file'],'delta_xy':[dx,0],'margins_ltrb_px':margins,'exact_integer_translation':True,'painted_pixels_bitwise_unchanged':True,'anatomical_composite_error_x_px':rb['composite_center'][0]-256,'support_sole_measured_not_fixed':rb[rb['support_foot']+'_sole']})
    atlas=Image.new('RGBA',(1536,1024))
    for i,f in enumerate(bframes):atlas.paste(f,((i%3)*512,(i//3)*512))
    atlas.save(out/'shiro_walk_front_atlas.png',optimize=True)
    manifest.update(schema_version=2,alignment='head_torso_horizontal_composite',baseline_commit=BASELINE,uniform_scale=json.loads((ROOT/'output/qa/technical_validation.json').read_text())['uniform_scale'],
        pivot_bottom_left_px=[256,256],root_position_fixed=True,frames=entries)
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    unity={'atlasSha256':sha(out/'shiro_walk_front_atlas.png'),'baselineAtlasSha256':sha(ROOT/'output/shiro_walk_front_atlas.png'),
        'frames':[{'file':e['file'],**{prefix+axis:row[key][j] for prefix,key in [('face','face_center'),('torso','torso_center'),('leftSole','left_sole'),('rightSole','right_sole')] for j,axis in enumerate(['X','Y'])}} for e,row in zip(entries,brows)]}
    (out/'unity_landmarks.json').write_text(json.dumps(unity,indent=2)+'\n')
    metrics={'baseline_commit':BASELINE,'A':summary(arows),'B':summary(brows),'per_frame':{'A':arows,'B':brows},
        'units':'cell pixels; source annotation +/-4px uncertainty (~1.34 cellpx)','hypothesis':'Fixed support-sole X induces opposite whole-body translation.',
        'human_A_observation':'Human actually watched A and judged lateral head/torso motion unnatural; A Human NG.',
        'expected_result':'Body registration may improve perceived gait; not yet Human tested.',
        'observed_result':'Measured coordinate change only; natural gait is NOT certified.',
        'sole_measurement_limits':'In-place X displacement is not world-space sliding proof. Supporting side L1-3/R4-6; actual contact timing and visual slide require Human video judgment.',
        'decision':'B Human GO pending; not long-term production decision'}
    (work/'comparison_metrics.json').write_text(json.dumps(metrics,indent=2)+'\n')
    # Paired stills: A left, B right, same scale; annotation points kept off deliverable PNGs.
    sheet=Image.new('RGB',(1024,6*512),(235,242,252));draw=ImageDraw.Draw(sheet)
    for i,(a,b) in enumerate(zip(aframes,bframes)):
        for j,f in enumerate([a,b]):sheet.paste(f,(j*512,i*512),f)
        draw.text((12,i*512+10),f'A SOLE / {i+1}',fill='black');draw.text((524,i*512+10),f'B HEAD/TORSO / {i+1}',fill='black')
    sheet.save(work/'paired_frames.jpg',quality=90)
    overlay=sheet.copy();d=ImageDraw.Draw(overlay)
    for i in range(6):
        for col,rows in enumerate([arows,brows]):
            for k,c in [('face_center','red'),('torso_center','blue'),('left_sole','green'),('right_sole','orange')]:
                x,y=rows[i][k];x+=col*512;y+=i*512;d.ellipse((x-5,y-5,x+5,y+5),outline=c,width=2)
    overlay.save(work/'diagnostic_landmarks.jpg',quality=90)
    # Trajectory values use the same fixed coordinate axes for A/B and show wrap.
    plot=Image.new('RGB',(1000,480),'white');d=ImageDraw.Draw(plot)
    for j,key in enumerate(['face_center','torso_center']):
        yy=j*240;d.text((10,yy+8),key+' X trajectory / red A, blue B; 6->1 wrap',fill='black')
        for value in [210,230,250,270,290]:
            y=yy+205-(value-210)*2;d.line((65,y,970,y),fill='#ddd');d.text((12,y),str(value),fill='black')
        for label,color in [('A','red'),('B','blue')]:
            values=metrics[label][key]['x'];pts=[(80+i*145,yy+205-(v-210)*2) for i,v in enumerate(values+[values[0]])];d.line(pts,fill=color,width=3)
            for i,(x,y) in enumerate(pts):d.ellipse((x-3,y-3,x+3,y+3),fill=color);d.text((x,yy+214),str(i+1 if i<6 else 1),fill='black')
    plot.save(work/'trajectories.png')
    movies={'A':[],'B':[],'AB':[]}
    for t in range(48):
        i=t%6;dark=t>=24;background=(14,17,23) if dark else (235,242,252);ink='white' if dark else 'black'
        for label,frames in [('A',aframes),('B',bframes)]:
            bg=Image.new('RGB',(390,844),background);d=ImageDraw.Draw(bg);d.text((10,15),f'{label} / {"SOLE ANCHOR" if label=="A" else "HEAD + TORSO ANCHOR"}',fill=ink)
            d.text((10,40),f'Frame {i+1}/6 / 8fps / same scale / Human judgment',fill=ink);preview(frames[i],bg,100)
            feet=frames[i].crop((150,355,365,475)).resize((340,190),Image.Resampling.LANCZOS);bg.paste(feet,(25,620),feet);d.text((10,585),'FEET / assess sliding and contact',fill=ink);movies[label].append(bg)
        bg=Image.new('RGB',(390,844),background);d=ImageDraw.Draw(bg)
        d.text((10,6),f'A SOLE / frame {i+1} / same 384px display',fill=ink);preview(aframes[i],bg,26)
        d.text((10,420),f'B HEAD + TORSO / frame {i+1} / 8fps',fill=ink);preview(bframes[i],bg,440);movies['AB'].append(bg)
    videos=[encode(v,work/f'{k}_iPhone.mp4') for k,v in movies.items()]
    subprocess.run(['ffmpeg','-v','error','-y','-i',str(work/'AB_iPhone.mp4'),'-vf','fps=8,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse',str(work/'AB_iPhone.gif')],check=True)
    result={'machine_qa':'PASS','baseline_commit':BASELINE,'baseline_files_unchanged':preserve,'B_atlas_sha256':sha(out/'shiro_walk_front_atlas.png'),
        'grid':[3,2],'cell_size':[512,512],'atlas_size':[1536,1024],'frames':checks,'uniform_scale_A_B':manifest['uniform_scale'],
        'no_resampling_or_art_edit':True,'vertical_A_B_bitwise_identical':True,'B_contract':'composite X=256 +/-0.5px, exact integer translation, >=40px margins, both soles measured, no fixed sole X assertion',
        'B_face_x_range':metrics['B']['face_center']['x_range'],'B_torso_x_range':metrics['B']['torso_center']['x_range'],'videos':videos,'Human_B':'PENDING','Unity_B':'PENDING'}
    (work/'machine_qa.json').write_text(json.dumps(result,indent=2)+'\n')
    print('ALIGNMENT_AB_QA_PASS',json.dumps({k:metrics[k]['face_center']['x_range'] for k in ['A','B']}),flush=True)
if __name__=='__main__':main()
