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
  var probe=new GameObject("R0Capture").AddComponent<R0Capture>();probe.cameraToRender=cam;probe.actor=actor;probe.output="/private/tmp/shiro-R0-output/run3";
  EditorSceneManager.SaveScene(scene,"Assets/R0.unity");AssetDatabase.SaveAssets();Debug.Log("R0_SETUP_COMPLETE: sample layers="+renders.Length+" bones="+bones.Length);EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
 }
}
#endif
