using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
#endif
[Serializable] public class R0SkinRow { public string name; public string sprite; public int order; public int vertices; public double geometrySum; }
[Serializable] public class R0Frame { public int frame; public float gameTime; public float phase; public float[] boneEulerZ; public Vector3 root; public R0SkinRow[] skins; }
[Serializable] public class R0Report { public string editor; public string package="13.0.0"; public string psdImporter="12.0.0"; public string scope="OFFICIAL FEI SAMPLE / NOT SHIRO / THREE AUTHORED RIG POSES"; public string[] boneNames; public R0Frame[] frames; public string[] errors; }
[DefaultExecutionOrder(10000)]
public class R0Capture : MonoBehaviour {
 int frameCounter; int warmup;
 static readonly string[] trackedBones={"bone_16","bone_17","bone_19","bone_20","bone_10","bone_13"};
 public Camera cameraToRender; public GameObject actor; public string output;
 readonly List<R0Frame> frames=new List<R0Frame>(); readonly List<string> errors=new List<string>();
 void OnEnable(){ Application.logMessageReceived+=Log; }
 void OnDisable(){Application.logMessageReceived-=Log;}
 void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert) errors.Add(m);}
 void Start(){Time.captureFramerate=30;Directory.CreateDirectory(output+"/frames");}
 void LateUpdate(){
   if(warmup++<5)return;
   try {
   int n=frameCounter;
   cameraToRender.backgroundColor=n<90?new Color(.94f,.91f,.85f):new Color(.08f,.1f,.15f);
   var sr=new List<R0SkinRow>();
   foreach(var skin in actor.GetComponentsInChildren<SpriteSkin>()){
    var rend=skin.GetComponent<SpriteRenderer>();var v=skin.GetDeformedVertexPositionData().ToArray();
    double sum=0;for(int i=0;i<v.Length;i++)sum+=(i+1)*(v[i].x+v[i].y*7+v[i].z*17);
    sr.Add(new R0SkinRow{name=skin.name,sprite=rend.sprite.name,order=rend.sortingOrder,vertices=v.Length,geometrySum=sum});
   }
   var anim=actor.GetComponent<Animator>();frames.Add(new R0Frame{frame=n,gameTime=Time.time,phase=anim.GetCurrentAnimatorStateInfo(0).normalizedTime%1,root=actor.transform.position,boneEulerZ=trackedBones.Select(k=>actor.GetComponentsInChildren<Transform>().First(x=>x.name==k).localEulerAngles.z).ToArray(),skins=sr.ToArray()});
   cameraToRender.Render();var old=RenderTexture.active;RenderTexture.active=cameraToRender.targetTexture;
   var tex=new Texture2D(390,844,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,390,844),0,0);tex.Apply();File.WriteAllBytes(output+"/frames/"+n.ToString("D3")+".png",tex.EncodeToPNG());Destroy(tex);RenderTexture.active=old;
  frameCounter++;if(frameCounter<180)return;enabled=false;
  var report=new R0Report{editor=Application.unityVersion,boneNames=actor.GetComponentsInChildren<Transform>().Select(x=>x.name).ToArray(),frames=frames.ToArray(),errors=errors.ToArray()};File.WriteAllText(output+"/runtime.json",JsonUtility.ToJson(report,true));
#if UNITY_EDITOR
  EditorApplication.Exit(errors.Count==0?0:1);
#endif
   } catch(Exception ex){File.WriteAllText(output+"/failure.txt",ex.ToString());enabled=false;
#if UNITY_EDITOR
    EditorApplication.Exit(1);
#endif
   }
 }
}
