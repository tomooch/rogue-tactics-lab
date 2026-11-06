#!/usr/bin/env python3
"""Verify actual S0 capability artifacts; an exit0 never means Shiro art PASS."""
import hashlib, io, json, subprocess, zipfile
from pathlib import Path
from xml.etree import ElementTree as ET
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[1]; OUT=ROOT/'continuity/S0'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    source=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    manifest=json.loads((OUT/'authoring_manifest.json').read_text()); checks=[]
    assert manifest['source_pixels_in_parts'] is False
    for attempt in ['attempt1','attempt2']:
        target=OUT/attempt
        doc=ET.parse(target/'editable_pilot.svg');tags=[e.tag.split('}')[-1] for e in doc.iter()]
        assert 'image' not in tags # no embedded cutout raster in any source layer
        for p in (target/'layers').glob('*.svg'):
            assert '<image' not in p.read_text(); ET.parse(p)
        with zipfile.ZipFile(target/'editable_pilot.ora') as z:
            assert z.infolist()[0].filename=='mimetype';assert z.read('mimetype')==b'image/openraster'
            stack=ET.fromstring(z.read('stack.xml'));layers=list(stack.find('stack'));assert len(layers)==9
            composite=Image.new('RGBA',(512,512))
            for layer in reversed(layers):
                im=Image.open(io.BytesIO(z.read(layer.attrib['src']))).convert('RGBA');assert im.size==(512,512)
                composite.alpha_composite(im)
            merged=Image.open(io.BytesIO(z.read('mergedimage.png'))).convert('RGBA')
            delta=np.abs(np.asarray(composite,dtype=int)-np.asarray(merged,dtype=int))
            # Require the ORA merged image to exactly represent its stored layers.
            maximum=int(delta.max());assert maximum==0,maximum
        frames=[]
        for i in range(3):
            im=Image.open(target/f'range_{i}.png');assert im.mode=='RGBA' and im.size==(512,512)
            bb=im.getchannel('A').getbbox();margins=[bb[0],bb[1],512-bb[2],512-bb[3]];assert min(margins)>=40
            frames.append({'file':f'range_{i}.png','RGBA':True,'margins_ltrb':margins,'sha256':sha(target/f'range_{i}.png')})
        checks.append({'attempt':attempt,'editable_SVG_XML':True,'ORA_named_layers':len(layers),'ORA_composite_max_channel_delta':maximum,'source_raster_embedded':False,'frames':frames})
    review=json.loads((OUT/'visual_review.json').read_text())
    assert review['final_art_verdict']=='FAIL' and review['S1_S2_S3']=='NOT_EXECUTED'
    # A labelled three-range hold diagnostic, NOT a character or walk preview.
    dst=OUT/'S0_failed_authoring_diagnostic_iPhone.mp4'
    proc=subprocess.Popen(['ffmpeg','-y','-loglevel','error','-f','rawvideo','-pix_fmt','rgb24','-s','390x844','-r','24','-i','pipe:0','-an','-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(dst)],stdin=subprocess.PIPE)
    for i in range(3):
        for background in [(239,242,247),(35,39,56)]:
            im=Image.new('RGB',(390,844),background);d=ImageDraw.Draw(im);fg=(30,34,49) if background[0]>100 else (245,245,250)
            d.text((15,35),'S0 FAILED ART-AUTHORING PILOT',fill=fg)
            d.text((15,55),'Not Shiro / not walking / not Unity',fill=fg)
            part=Image.open(OUT/'attempt2'/f'range_{i}.png');part.thumbnail((370,370));im.paste(part,(10,145),part)
            d.text((15,550),f'Inferred part-range sample {i+1}/3',fill=fg)
            d.text((15,575),'New hidden paint exists; style/joints FAIL.',fill=fg)
            d.text((15,595),'S1-S3 not executed. No art adoption.',fill=fg)
            for _ in range(24):proc.stdin.write(im.tobytes())
    proc.stdin.close();assert proc.wait()==0
    probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=codec_name,width,height,pix_fmt','-show_entries','format=duration','-of','json',str(dst)],text=True))
    subprocess.run(['ffmpeg','-v','error','-i',str(dst),'-f','null','-'],check=True)
    assert probe['streams'][0]=={'codec_name':'h264','width':390,'height':844,'pix_fmt':'yuv420p'}
    prior=['unity','experiments/shiro-front-walk/source','experiments/shiro-front-walk/input','experiments/shiro-front-walk/output','experiments/shiro-front-walk/alignment','experiments/shiro-front-walk/continuity/C0','experiments/shiro-front-walk/continuity/gates','experiments/shiro-front-walk/continuity/motion-reference']
    diff=subprocess.check_output(['git','diff','--name-only','fdd02510f90faeb7dc89fd1d523b5c58f10aaf4c','--',*prior],cwd=ROOT,text=True);assert not diff.strip()
    report={'executed_source_commit':source,'artifact_checks':checks,'video':probe,'full_decode':'PASS','prior_evidence_and_production_tree_diff':diff,'tool_artifact_validation':'PASS','S0_source_art_gate':'FAIL; canonical master unresolved and new paint style/joints fail','S1':'NOT_EXECUTED','S2':'NOT_EXECUTED','S3':'NOT_EXECUTED','Human_GO':'NOT_OBTAINED','video_SHA256':sha(dst)}
    (OUT/'qa.json').write_text(json.dumps(report,indent=2)+'\n')
    print('tests_completed: actual editable pilot artifacts PASS; S0 ART FAIL; S1-S3 NOT EXECUTED',flush=True)

if __name__=='__main__':main()
