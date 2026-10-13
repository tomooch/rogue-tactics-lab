#!/usr/bin/env python3
"""Local source-art capability pilot; NOT a Shiro rig or a gait.

Authors new closed vector paint, including hidden joints. No input pixels are
copied into parts. Two bounded attempts demonstrate what this local authoring
method can and cannot deliver before investing in a whole character.
"""
import argparse, hashlib, io, json, math, subprocess, zipfile, shutil
from pathlib import Path
from xml.etree import ElementTree as ET
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'continuity/S0'
DEFS = '''<defs>
<linearGradient id="skin"><stop stop-color="#ffe7d4"/><stop offset=".55" stop-color="#fff0df"/><stop offset="1" stop-color="#efbbac"/></linearGradient>
<linearGradient id="blue"><stop stop-color="#42416e"/><stop offset=".45" stop-color="#6375b7"/><stop offset="1" stop-color="#353762"/></linearGradient>
<linearGradient id="white"><stop stop-color="#c6bfdf"/><stop offset=".5" stop-color="#fff7e9"/><stop offset="1" stop-color="#dfd8ed"/></linearGradient>
<linearGradient id="gold"><stop stop-color="#b98250"/><stop offset=".5" stop-color="#ffe0a4"/><stop offset="1" stop-color="#cf985d"/></linearGradient>
</defs>'''

def svg(body):
    return '<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" width="512" height="512" viewBox="0 0 512 512">'+DEFS+body+'</svg>'

def path(d, fill, stroke='#715969', width=1.4):
    return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round"/>'

def star(x,y,r):
    pts=[]
    for i in range(8):
        a=i*math.pi/4-math.pi/2;rr=r if i%2==0 else r*.3
        pts.append(f'{x+math.cos(a)*rr:.3f},{y+math.sin(a)*rr:.3f}')
    return '<polygon points="'+' '.join(pts)+'" fill="url(#gold)"/>'

def flower(x,y,r):
    return ''.join(f'<ellipse cx="{x}" cy="{y-r*.8}" rx="{r*.5}" ry="{r}" fill="url(#white)" stroke="#cfb8bb" stroke-width=".8" transform="rotate({a} {x} {y})"/>' for a in range(0,360,72))+f'<circle cx="{x}" cy="{y}" r="{r*.42}" fill="url(#gold)"/>'

def parts(repair):
    p={}
    p['thigh_hidden_hip']=path('M132 82 C143 71 170 74 179 91 C184 113 171 154 172 193 C169 214 132 215 130 194 C129 158 121 116 132 82 Z','url(#skin)')
    p['shin_hidden_knee']=path('M132 179 C140 164 165 165 172 180 C180 204 168 231 171 273 C167 291 140 294 131 277 C130 250 124 204 132 179 Z','url(#skin)')
    p['boot_complete']=path('M126 257 C139 247 163 254 177 261 L177 313 C183 324 183 350 174 360 C155 371 121 370 113 356 C112 344 116 326 124 313 Z','url(#blue)')+path('M113 349 C126 356 155 360 178 349 L180 365 C153 379 120 372 112 364 Z','url(#gold)')
    p['upper_arm_hidden_shoulder']=path('M350 82 C365 71 386 75 392 90 C397 110 382 147 375 176 C372 190 352 191 343 180 C337 152 343 112 350 82 Z','url(#white)')
    p['forearm_hidden_elbow']=path('M345 160 C355 149 376 154 379 171 C376 198 370 221 359 242 C349 250 334 239 332 230 C334 207 339 178 345 160 Z','url(#white)')
    p['sleeve_inner_cuff']=path('M330 218 C339 209 360 217 370 224 L365 243 C352 254 327 246 325 235 Z','url(#blue)')+path('M328 224 C339 232 356 235 369 225 C366 236 361 240 351 241 C340 241 332 239 328 224 Z','#c2a9c3')
    p['hand_closed_wrist']=path('M338 234 C345 229 355 232 358 239 L356 259 C361 263 358 270 352 268 L350 253 C349 270 342 270 342 255 C341 267 333 264 337 252 C325 259 324 251 334 246 Z','url(#skin)')
    p['skirt_back_complete']=path('M285 323 C321 294 370 296 408 323 L446 407 C436 425 421 421 411 409 C398 430 380 429 371 414 C357 438 338 430 332 417 C319 431 301 426 294 408 C283 423 263 421 255 405 Z','url(#blue)')
    p['skirt_inside_frill']=path('M282 351 C321 338 379 337 412 355 L427 393 C419 411 406 407 398 398 C385 418 369 412 362 401 C348 420 332 413 325 400 C313 414 295 408 291 396 C279 408 264 401 265 387 Z','url(#white)')
    if repair:
        # Bounded repair after actual review: remove outline across the hidden
        # knee/elbow overlap, retaining external contour. Does not recover style.
        for key in ['shin_hidden_knee','forearm_hidden_elbow']:
            p[key]=p[key].replace('stroke="#715969"', 'stroke="none"')
        p['shin_hidden_knee']+=path('M132 201 C130 222 130 250 131 277 M173 200 C170 223 168 248 171 273','none')
        p['forearm_hidden_elbow']+=path('M342 186 C339 203 334 220 332 230 M376 190 C372 211 365 230 359 242','none')
        p['boot_complete']+=path('M125 261 Q140 251 151 259 Q163 251 178 261 L173 273 Q158 265 151 275 Q140 267 127 274 Z','url(#white)')
        p['boot_complete']+=path('M126 276 Q109 274 119 294 L148 286 L168 307 Q185 288 173 279 L149 283 Z','url(#blue)')+flower(149,287,9)
        p['boot_complete']+=''.join(star(x,y,r) for x,y,r in [(141,316,8),(163,334,6),(125,337,4)])
        p['skirt_back_complete']+=''.join(star(x,y,r) for x,y,r in [(300,355,10),(389,363,9),(420,391,8)])
        p['sleeve_inner_cuff']+=flower(347,229,6)
        # Explicitly authored paint patches, not copied texture or hidden source.
        for key,shape in [('boot_complete','M128 305 Q148 295 173 308 L169 314 Q147 305 130 312 Z'),('skirt_back_complete','M285 377 Q336 343 416 379 L420 395 Q360 365 284 396 Z')]:
            p[key]+=f'<path d="{shape}" fill="#ad9ab8" opacity=".24"/>'
    return p

def render(node,sharp,body,dest):
    script='const s=require(process.argv[1]);s(Buffer.from(process.argv[2])).png().toFile(process.argv[3]).catch(e=>{console.error(e);process.exit(1)});'
    subprocess.run([node,'-e',script,sharp,svg(body),str(dest)],check=True,capture_output=True)

def transforms(knee,elbow):
    return {k:f'rotate({knee} 151 191)' for k in ['shin_hidden_knee','boot_complete']} | {k:f'rotate({elbow} 360 172)' for k in ['forearm_hidden_elbow','sleeve_inner_cuff','hand_closed_wrist']}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--node',required=True);parser.add_argument('--sharp',required=True);args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    records=[]
    for repair in [False,True]:
        target=OUT/('attempt2' if repair else 'attempt1');target.mkdir(exist_ok=True);(target/'layers').mkdir(exist_ok=True)
        if repair and (target/'editable_pilot.svg').exists() and not (target/'before_joint_repair').exists():
            backup=target/'before_joint_repair';backup.mkdir()
            for f in list(target.iterdir()):
                if f == backup:continue
                if f.is_dir():shutil.copytree(f,backup/f.name)
                else:shutil.copy2(f,backup/f.name)
        p=parts(repair);stack=ET.Element('image',{'w':'512','h':'512','name':'S0 authoring swatches - NOT complete Shiro'});xmlstack=ET.SubElement(stack,'stack')
        for key,body in p.items():
            (target/'layers'/f'{key}.svg').write_text(svg(body));render(args.node,args.sharp,body,target/'layers'/f'{key}.png')
        neutral=''.join(f'<g id="{k}" inkscape:groupmode="layer" inkscape:label="{k}">{body}</g>' for k,body in p.items())
        (target/'editable_pilot.svg').write_text(svg(neutral));render(args.node,args.sharp,neutral,target/'neutral.png')
        for key in reversed(p):ET.SubElement(xmlstack,'layer',{'name':key,'src':f'data/{key}.png','opacity':'1.0','visibility':'visible','composite-op':'svg:src-over','x':'0','y':'0'})
        # OpenRaster's merged image must represent its actual raster layers,
        # not a separately rasterized whole SVG with different AA rounding.
        composite=Image.new('RGBA',(512,512))
        for key in p:composite.alpha_composite(Image.open(target/'layers'/f'{key}.png').convert('RGBA'))
        mergedbuf=io.BytesIO();composite.save(mergedbuf,format='PNG')
        with zipfile.ZipFile(target/'editable_pilot.ora','w') as z:
            z.writestr('mimetype','image/openraster',compress_type=zipfile.ZIP_STORED)
            z.writestr('stack.xml',ET.tostring(stack));z.writestr('mergedimage.png',mergedbuf.getvalue())
            for key in p:z.write(target/'layers'/f'{key}.png',f'data/{key}.png')
            thumb=Image.open(target/'neutral.png');thumb.thumbnail((256,256));buf=io.BytesIO();thumb.save(buf,format='PNG');z.writestr('Thumbnails/thumbnail.png',buf.getvalue())
        poseinfo=[]
        grid=Image.new('RGB',(1536,550),(234,237,243));draw=ImageDraw.Draw(grid)
        for i,(knee,elbow) in enumerate([(0,0),(25,-18),(-20,15)]):
            tr=transforms(knee,elbow);body=''.join(f'<g id="{k}" transform="{tr.get(k,"")}">{v}</g>' for k,v in p.items());name=f'range_{i}.png';render(args.node,args.sharp,body,target/name)
            im=Image.open(target/name).convert('RGBA');grid.paste(im,(i*512,35),im);draw.text((i*512+12,10),f'NEW PAINT RANGE TEST knee={knee} / elbow={elbow}',fill='black')
            poseinfo.append({'file':name,'knee_deg':knee,'elbow_deg':elbow,'role':'inferred articulation range, NOT measured gait or S1 Contact/Passing'})
        grid.save(target/'range_diagnostic.jpg',quality=94)
        # Public baseline is only a comparison inset. Never inserted into pilot layers.
        src=Image.open(ROOT/'input/shiro_walk_front_01.png');crop=src.crop((650,880,835,1254));crop.thumbnail((250,380))
        compare=Image.new('RGB',(1024,512),(234,237,243));d=ImageDraw.Draw(compare);compare.paste(crop,(45,90),crop)
        trial=Image.open(target/'neutral.png').crop((100,65,190,385));trial.thumbnail((180,380));compare.paste(trial,(335,90),trial)
        d.text((20,20),'Existing PUBLIC B source detail (not approved master proof)',fill='black');d.text((335,60),'New vector paint / source-art feasibility only',fill='black')
        sleeve=Image.open(target/'neutral.png').crop((315,65,405,280));sleeve.thumbnail((180,300));compare.paste(sleeve,(615,90),sleeve);compare.save(target/'style_comparison.jpg',quality=94)
        records.append({'attempt':target.name,'paint':'all pilot paths newly authored; no copied texture pixels','parts':list(p),'pose_tests':poseinfo,'repair':repair,'source_art_gate':'PENDING_VISUAL_REVIEW'})
    manifest={'schema_version':1,'scope':'S0 LOCAL ART-AUTHORING CAPABILITY PILOT, not full neutral Shiro; not S1 or gait','tools':{'node':args.node,'sharp_module':args.sharp,'python':'Pillow/stdlib'},'source_pixels_in_parts':False,'dimensions':[512,512],'part_order_back_to_front':list(p),'pivots':{'hip':[151,88],'knee':[151,191],'ankle':[151,271],'shoulder':[371,90],'elbow':[360,172],'wrist':[348,236],'skirt':[350,320]},'pivot_status':'proposed anatomy/range, NOT M0 measured landmarks','hidden_paint':{'thigh_hidden_hip':'closed upper cap and complete thigh','shin_hidden_knee':'closed knee behind thigh','upper_arm_hidden_shoulder':'closed shoulder behind sleeve','forearm_hidden_elbow':'closed elbow behind upper sleeve','sleeve_inner_cuff':'painted inner cuff; hidden wrist unproven','skirt_back_complete':'closed rear skirt, provisional shape','skirt_inside_frill':'new scalloped underskirt'},'missing_complete_character':['face/head/hat/hair','chest/shoulder/pelvis','anatomically distinct left/right limbs','hand-held staff','cape and complete skirt front/back interaction','matching approved texture/model details'],'attempts':records}
    (OUT/'authoring_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('S0_NEW_PAINT_SWATCHES_RENDERED; source-art gate requires visual review',flush=True)

if __name__=='__main__':main()
