"""Local-only approved art -> static skin alpha. No image upload, no paint changes.
Requires supplied approved originals; never accepts PR10/generated replacements.
SkyTNT anime-seg Apache-2.0 pinned ONNX is an offline build tool, not game dependency.
"""
import argparse,hashlib,json
from pathlib import Path
import numpy as np
import cv2
import onnxruntime as ort
from PIL import Image

FILES={'shiro':'0FBCFEF0-C983-48B1-827B-0D0FD6FBF45A.png','myaru':'2F1AAC49-C1E3-48BF-8BA1-AB7707D371AF.png','momo':'65C78C53-3B13-4A93-B04C-3E15AAFF9FD9.png','garu':'FD16E637-CAD0-472A-A024-9B05D07BC049.png'}
MODEL_SHA='f15622d853e8260172812b657053460e20806f04b9e05147d49af7bed31a6e99'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--originals',required=True);p.add_argument('--model',required=True);p.add_argument('--output',required=True);a=p.parse_args()
 assert sha(a.model)==MODEL_SHA
 out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
 opt=ort.SessionOptions();opt.intra_op_num_threads=4;s=ort.InferenceSession(a.model,sess_options=opt,providers=['CPUExecutionProvider']);name=s.get_inputs()[0].name
 results=[]
 for character,file in FILES.items():
  source=Path(a.originals)/file;before=sha(source);rgb=np.array(Image.open(source).convert('RGB'));hh,ww=rgb.shape[:2];size=1024
  h,w=(size,int(size*ww/hh)) if hh>ww else (int(size*hh/ww),size);ph,pw=size-h,size-w
  inp=np.zeros((size,size,3),np.float32);inp[ph//2:ph//2+h,pw//2:pw//2+w]=cv2.resize(rgb.astype(np.float32)/255,(w,h))
  mask=s.run(None,{name:inp.transpose(2,0,1)[None]})[0].squeeze();mask=cv2.resize(mask[ph//2:ph//2+h,pw//2:pw//2+w],(ww,hh))
  # Remove detached garden detections; keep the largest connected foreground plus its soft edge.
  high=(mask>=.65).astype(np.uint8);n,labels,stats,_=cv2.connectedComponentsWithStats(high,8);main_id=1+np.argmax(stats[1:,cv2.CC_STAT_AREA]);core=(labels==main_id).astype(np.uint8)
  support=cv2.dilate(core,np.ones((7,7),np.uint8));alpha=np.clip((mask-.20)/.70,0,1);alpha*=support
  alpha=np.round(alpha*255).astype(np.uint8);rgba=np.dstack([rgb,alpha]);assert np.array_equal(rgba[:,:,:3],rgb)
  Image.fromarray(rgba).save(out/(character+'.png'))
  assert sha(source)==before
  results.append({'character':character,'original_filename':file,'source_sha256':before,'source_rgb_preserved':True,'original_unchanged':True,'skin_sha256':sha(out/(character+'.png')),'alpha_foreground_pixels':int((alpha>0).sum())})
  print('STATIC_SKIN_PREPARED',character,flush=True)
 (out/'provenance.json').write_text(json.dumps({'model_sha256':MODEL_SHA,'model_revision':'493cb60893f47441b26ec4fb9a306bce9e342982','model_license':'Apache-2.0','external_image_upload':False,'hidden_paint':False,'artifacts_private':True,'sources':results},indent=2)+'\n')
if __name__=='__main__':main()
