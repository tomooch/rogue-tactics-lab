#!/usr/bin/env python3
"""Bounded three-pose C0 trial. Not an accepted rig, not a six-frame walk.
Original appearance parts + local hidden-pelvis paint + articulated hip/knee hierarchy.
Never makes geometric QA mean seam/anatomical/visual PASS.
"""
import argparse,hashlib,json,math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];OUT=R/'continuity/C0';N=1254

def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def polygon(points):
 m=Image.new('L',(N,N));ImageDraw.Draw(m).polygon(points,fill=255);return np.array(m)>0
def translate(x,y):return np.array([[1.,0,x],[0,1.,y],[0,0,1.]])
def rotate(deg,p):
 a=math.radians(deg);c=math.cos(a);s=math.sin(a)
 return translate(*p)@np.array([[c,-s,0],[s,c,0],[0,0,1]])@translate(-p[0],-p[1])
def warp(im,M):
 inv=np.linalg.inv(M);return im.transform((N,N),Image.Transform.AFFINE,tuple(inv[:2].ravel()),Image.Resampling.BICUBIC)
def point(M,p):return list((M@np.array([*p,1]))[:2])
def masked(image,mask):
 a=np.array(image);a[~mask]=0;a[a[:,:,3]==0,:3]=0;return Image.fromarray(a)

def main():
 parser=argparse.ArgumentParser();parser.add_argument("--clothing-repair",action="store_true");args=parser.parse_args()
 (OUT/'parts').mkdir(parents=True,exist_ok=True);(OUT/'poses').mkdir(exist_ok=True)
 base=Image.open(R/'input/shiro_walk_front_01.png').convert('RGBA');arr=np.array(base);visible=arr[:,:,3]>0
 # A disjoint assignment makes bind-pose reconstruction exact. It does not recover occluded paint.
 definitions=[
 ('grip_staff',[(910,240),(1194,225),(1194,475),(1108,579),(987,749),(876,1000),(755,1009),(784,963),(853,812),(897,730),(897,619),(937,491),(903,450)]),
 ('left_shin_boot',[(655,1005),(790,1008),(832,1060),(831,1253),(620,1253),(622,1120)]),
 ('right_shin_boot',[(502,952),(629,950),(650,992),(626,1148),(484,1155),(474,1036)]),
 ('left_thigh',[(654,884),(783,864),(799,946),(788,1038),(674,1042),(650,938)]),
 ('right_thigh',[(508,907),(557,873),(654,884),(652,953),(609,991),(523,982),(498,946)]),
 ('free_arm',[(575,549),(648,583),(576,697),(463,724),(364,678),(338,588),(399,553),(474,562)]),
 ('head_face_hat',[(200,0),(1050,0),(1050,468),(932,565),(846,542),(766,511),(667,508),(558,512),(479,569),(354,552),(239,494)]),
 ('skirt_front',[(563,704),(787,700),(951,829),(919,911),(798,942),(689,924),(653,951),(551,938),(423,888),(359,842),(470,769)]),
 ('cape_left',[(100,710),(393,665),(548,796),(607,963),(507,1010),(361,1012),(299,964),(196,875),(100,827)]),
 ('cape_right',[(791,757),(1168,781),(1168,1088),(956,1090),(837,941),(803,834)]),
 ('back_hair_left',[(180,447),(551,483),(550,720),(371,799),(217,736)]),
 ('back_hair_right',[(802,481),(1012,477),(1012,824),(850,822),(818,655)])]
 used=np.zeros((N,N),bool);parts={};metadata=[]
 for name,points in definitions:
  mask=polygon(points)&visible&~used;used|=mask;parts[name]=masked(base,mask)
  metadata.append({'id':name,'source':'input/shiro_walk_front_01.png','mask_polygon':points,'ownership_order':'first matching region owns source pixel','painting':'none','missing_hidden_art':True})
 parts['torso_front']=masked(base,visible&~used);metadata.append({'id':'torso_front','source':'input/shiro_walk_front_01.png','mask':'remaining visible pixels after named part ownership','painting':'none','missing_hidden_art':True})
 restored=Image.new('RGBA',(N,N))
 for im in parts.values():restored.alpha_composite(im)
 np.testing.assert_array_equal(np.array(restored),arr)
 restored.save(OUT/'bind_reconstruction.png',optimize=True)
 # Local completion is used only behind hip/skirt. Repainted candidate boots are excluded.
 candidate=Image.open(OUT/'lower_body_completion_candidate.png').convert('RGBA')
 completed=Image.new('RGBA',(N,N));completed.alpha_composite(candidate.resize((420,464),Image.Resampling.LANCZOS),(420,790))
 hipmask=polygon([(531,798),(786,798),(794,917),(543,934),(515,880)])
 parts['pelvis_underpaint']=masked(completed,hipmask)
 metadata.append({'id':'pelvis_underpaint','source':'continuity/C0/lower_body_completion_candidate.png','source_resize':[420,464],'source_translation':[420,790],'mask_polygon':[(531,798),(786,798),(794,917),(543,934),(515,880)],'painting':'built-in ImageGen local occlusion completion; inferred blue undergarment, hidden only','excluded':'all regenerated boots/lower legs','missing_hidden_art':'side/back hip and neutral knee not authored'})
 # A normalizer trial uses the master front boot for both feet, preserving one texture;
 # a reflected companion is a documented NEW part, never B pixel-identical.
 hipL=(718,868);kneeL=(720,1014);ankleL=(724,1144)
 hipR=(592,868);kneeR=(590,1014);ankleR=(586,1144)
 skin=masked(completed,polygon([(650,852),(805,852),(805,1040),(650,1040)]))
 parts['trial_left_thigh']=skin
 reflection=np.array([[-1.,0,1310],[0,1.,0],[0,0,1.]])
 parts['trial_right_thigh']=warp(skin,reflection)
 parts['trial_right_shin_boot']=warp(parts['left_shin_boot'],reflection)
 metadata.extend([{'id':'trial_left_thigh','source':'continuity/C0/lower_body_completion_candidate.png','painting':'completed skin/blue shorts behind skirt; canonical visible boot retained','status':'candidate, incomplete back knee contour'},
 {'id':'trial_right_thigh','source_part':'trial_left_thigh','transform':reflection.tolist(),'status':'reflected leg-normalization experiment; not approved original right-leg artwork'},
 {'id':'trial_right_shin_boot','source_part':'left_shin_boot','transform':reflection.tolist(),'status':'reflected front-boot texture; original rear-view boot retained separately, not overwritten'}])
 if args.clothing_repair:
  cloth=Image.open(OUT/'occluded_clothing_completion_candidate.png').convert('RGBA')
  source=Image.new('RGBA',(N,N));source.alpha_composite(cloth.resize((700,440),Image.Resampling.LANCZOS),(320,650))
  parts['clothing_underpaint']=masked(source,polygon([(486,853),(805,853),(806,1024),(490,1024)]))
  metadata.append({'id':'clothing_underpaint','source':'continuity/C0/occluded_clothing_completion_candidate.png','source_resize':[700,440],'source_translation':[320,650],'mask_polygon':[(486,853),(805,853),(806,1024),(490,1024)],'painting':'one bounded hidden-cloth repair; not existing canonical paint','status':'candidate: extra frill geometry must be reviewed'})
 for k,im in parts.items():im.save(OUT/'parts'/f'{k}.png',optimize=True)
 (OUT/'parts_manifest.json').write_text(json.dumps({'schema_version':1,'baseline':'8524ddb4a389deb878a5487611e02ecd196bd70c','master_png_sha256':sha(R/'input/shiro_walk_front_01.png'),'canvas':[N,N],'face_and_hat_source':'head_face_hat','original_parts_bind_reconstruction':'bitwise exact; this is not articulation PASS','parts':metadata,'hierarchy':'pelvis -> left/right hip -> thigh -> knee -> shin/boot; torso counter-rotates; head common unit; arm/staff share torso; cape/hair follow torso','joint_points':{'hipL':hipL,'kneeL':kneeL,'ankleL':ankleL,'hipR':hipR,'kneeR':kneeR,'ankleR':ankleR},'joint_basis':'inferred 2D trial points, not observed neutral anatomy; body-hidden hip points lack original paint','art_status':'PROVISIONAL_C0_ONLY'},indent=2)+'\n')
 poses=[('01_left_contact',0,-2,0,0,-20,25,1.0),('02_passing',0,-7,-5,5,-24,17,0.0),('03_right_contact',0,-2,20,-25,0,0,-1.0)]
 results=[];painted=[]
 for name,tx,ty,Lu,Lk,Ru,Rk,sway in poses:
  pelvis=translate(tx,ty)@rotate(sway,(655,830));body=pelvis@rotate(-sway*.5,(665,748))
  Lupper=pelvis@rotate(Lu,hipL);Llower=Lupper@rotate(Lk,kneeL)
  Rupper=pelvis@rotate(Ru,hipR);Rlower=Rupper@rotate(Rk,kneeR)
  head=translate(tx,ty*.35);arm=body@rotate(sway*1.3,(577,580));staff=body@rotate(-sway*.8,(940,652))
  transforms={'pelvis_underpaint':pelvis,'cape_left':body@rotate(-sway*.6,(525,735)),'cape_right':body@rotate(-sway*.6,(833,796)),
    'back_hair_left':head@rotate(sway*.3,(506,493)),'back_hair_right':head@rotate(sway*.3,(842,505)),
    'trial_left_thigh':Lupper,'trial_right_thigh':Rupper,'left_shin_boot':Llower,'trial_right_shin_boot':Rlower,
    'torso_front':body,'skirt_front':pelvis@rotate(sway*.7,(655,800)),'free_arm':arm,'head_face_hat':head,'grip_staff':staff}
  if args.clothing_repair:transforms['clothing_underpaint']=body
  # Skirt must occlude leg roots; face remains in front of body, staff stays a connected group.
  order=['cape_left','cape_right','back_hair_left','back_hair_right','pelvis_underpaint','trial_right_thigh','trial_right_shin_boot','trial_left_thigh','left_shin_boot','torso_front','skirt_front','free_arm','head_face_hat','grip_staff']
  if args.clothing_repair:order.insert(4,'clothing_underpaint')
  full=Image.new('RGBA',(N,N))
  for part in order:full.alpha_composite(warp(parts[part],transforms[part]))
  full.save(OUT/'poses'/f'{name}_source.png',optimize=True)
  scale=0.33452830188679245;S=np.array([[scale,0,24.145],[0,scale,51.66],[0,0,1.]])
  inv=np.linalg.inv(S);frame=full.transform((512,512),Image.Transform.AFFINE,tuple(inv[:2].ravel()),Image.Resampling.BICUBIC)
  aa=np.array(frame);aa[aa[:,:,3]==0,:3]=0;frame=Image.fromarray(aa);frame.save(OUT/'poses'/f'{name}.png',optimize=True);painted.append(frame)
  bb=frame.getchannel('A').getbbox();margins=[bb[0],bb[1],512-bb[2],512-bb[3]]
  joints={k:point(M,p) for k,M,p in [('hipL',Lupper,hipL),('kneeL_upper',Lupper,kneeL),('kneeL_lower',Llower,kneeL),('ankleL',Llower,ankleL),('hipR',Rupper,hipR),('kneeR_upper',Rupper,kneeR),('kneeR_lower',Rlower,kneeR),('ankleR',Rlower,ankleR)]}
  results.append({'pose':name,'expected_phase':name,'transforms_source_space':{k:v.tolist() for k,v in transforms.items()},'render_order':order,'joints':joints,'hinge_point_error':max(float(np.linalg.norm(np.array(joints[k+'_upper'])-joints[k+'_lower'])) for k in ['kneeL','kneeR']),'margins_ltrb_px':margins,'geometry_40px':min(margins)>=40,'painted_joint_contour_valid':'REQUIRES_VISUAL_REVIEW','body_completion_valid':'NOT_ESTABLISHED'})
 sheet=Image.new('RGB',(1536,544),(232,238,247));d=ImageDraw.Draw(sheet)
 for i,f in enumerate(painted):sheet.paste(f,(i*512,32),f);d.text((i*512+10,10),poses[i][0]+' / PROVISIONAL C0',fill='black')
 sheet.save(OUT/'three_pose_contact.jpg',quality=92)
 onion=Image.new('RGBA',(512,512),(232,238,247,255))
 for i,f in enumerate(painted):
  a=np.array(f);a[:,:,3]=(a[:,:,3].astype('float32')*.30).astype('uint8');a[:,:,:3]=[(220,50,60),(35,140,70),(45,80,230)][i];onion.alpha_composite(Image.fromarray(a))
 onion.convert('RGB').save(OUT/'three_pose_onion.jpg',quality=92)
 grid=Image.new('RGB',(4*320,math.ceil(len(parts)/4)*340),(232,238,247));d=ImageDraw.Draw(grid)
 for i,(k,im) in enumerate(parts.items()):
  bb=im.getchannel('A').getbbox();thumb=im.crop(bb) if bb else Image.new('RGBA',(1,1));thumb.thumbnail((300,300));x=(i%4)*320;y=(i//4)*340;grid.paste(thumb,(x,y+28),thumb);d.text((x+4,y+8),k,fill='black')
 grid.save(OUT/'parts_contact.jpg',quality=90)
 (OUT/'pose_manifest.json').write_text(json.dumps({'common_scale':scale,'normalization':S.tolist(),'render_bone_lengths':'same part transforms, no per-pose scale','poses':results,'clothing_repair':args.clothing_repair,'gate':'not passed by numeric hinge coincidence; hidden paint and visual joints remain decisive'},indent=2)+'\n')
 print('C0_THREE_POSE_TRIAL_RENDERED',[(x['pose'],x['margins_ltrb_px'],x['hinge_point_error']) for x in results],flush=True)
if __name__=='__main__':main()
