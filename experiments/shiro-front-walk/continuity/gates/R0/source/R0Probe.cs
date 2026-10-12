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
  EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(errors.Count==0?0:1);
#endif
   } catch(Exception ex){File.WriteAllText(output+"/failure.txt",ex.ToString());enabled=false;
#if UNITY_EDITOR
    EditorApplication.Exit(1);
#endif
   }
 }
}
#if UNITY_EDITOR
public static class R0Probe {
 public static void Run(){
  var path="Assets/Samples/2D Animation/13.0.0/Samples/3 Character/Sprites/Fei.psb";
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new Exception("Official Fei PSB prefab not imported");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab);actor.name="R0_OfficialFei";actor.transform.position=Vector3.zero;
  var bones=actor.GetComponentsInChildren<Transform>();
  AssetDatabase.DeleteAsset("Assets/R0_ThreePoses.anim");AssetDatabase.DeleteAsset("Assets/R0.controller");
  var clip=new AnimationClip{name="R0_ThreePoses",frameRate=4};
  var requested=new Dictionary<string,float[]>{{"bone_16",new[]{20f,0f,-20f,20f}},{"bone_17",new[]{-25f,15f,5f,-25f}},{"bone_19",new[]{-20f,0f,20f,-20f}},{"bone_20",new[]{5f,15f,-25f,5f}},{"bone_10",new[]{-12f,0f,12f,-12f}},{"bone_13",new[]{12f,0f,-12f,12f}}};
  foreach(var kv in requested){
   var b=bones.FirstOrDefault(x=>x.name==kv.Key);if(b==null)throw new Exception("Missing sample bone "+kv.Key);
   var bind=b.localRotation;var qs=kv.Value.Select(z=>bind*Quaternion.Euler(0,0,z)).ToArray();var bp=AnimationUtility.CalculateTransformPath(b,actor.transform);
   for(int c=0;c<4;c++){
    var ks=new Keyframe[4];for(int k=0;k<4;k++)ks[k]=new Keyframe(k*.25f,qs[k][c],float.PositiveInfinity,float.PositiveInfinity);
    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bp,typeof(Transform),"m_LocalRotation."+"xyzw"[c]),new AnimationCurve(ks));
   }
  }
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,"Assets/R0_ThreePoses.anim");
  var ctl=AnimatorController.CreateAnimatorControllerAtPath("Assets/R0.controller");var st=ctl.layers[0].stateMachine.AddState("ThreePoses");st.motion=clip;ctl.layers[0].stateMachine.defaultState=st;
  var an=actor.GetComponent<Animator>();if(an==null)an=actor.AddComponent<Animator>();an.runtimeAnimatorController=ctl;an.applyRootMotion=false;
  foreach(var skin in actor.GetComponentsInChildren<SpriteSkin>())skin.alwaysUpdate=true;
  var renders=actor.GetComponentsInChildren<SpriteRenderer>();if(renders.Length<2)throw new Exception("No layered sample renderers");var bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
  var cam=new GameObject("R0Camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=Mathf.Max(bounds.extents.y*1.2f,bounds.extents.x*844f/390f*1.2f);cam.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.targetTexture=new RenderTexture(390,844,24);cam.targetTexture.Create();
  var probe=new GameObject("R0Capture").AddComponent<R0Capture>();probe.cameraToRender=cam;probe.actor=actor;probe.output="/private/tmp/shiro-R0-output";
  EditorSceneManager.SaveScene(scene,"Assets/R0.unity");AssetDatabase.SaveAssets();Debug.Log("R0_SETUP_COMPLETE: sample layers="+renders.Length+" bones="+bones.Length);EditorApplication.isPlaying=true;
 }
}
#endif
