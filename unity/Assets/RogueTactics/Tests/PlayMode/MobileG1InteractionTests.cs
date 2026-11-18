using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RogueTactics.Tests
{
    public class MobileG1InteractionTests
    {
        private static readonly string Evidence=Path.GetFullPath("Logs/mobile-g1/private");
        private static void Click(MobileG1View view,string id)
        {
            var button=view.Button(id);Assert.That(button.interactable,Is.True,id);
            var r=button.GetComponent<RectTransform>();Assert.That(r.rect.height,Is.GreaterThanOrEqualTo(44),id);
            var canvas=button.GetComponentInParent<Canvas>();var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,r.TransformPoint(r.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0),id);
            Assert.That(hits[0].gameObject,Is.EqualTo(button.gameObject),id+" top hit");ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private static void CheckLayout(MobileG1View view)
        {
            Canvas.ForceUpdateCanvases();
            CheckActorReadability(view);
            var labels=Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("HP ") && c.gameObject.activeInHierarchy).ToArray();
            for(var i=0;i<labels.Length;i++)for(var j=i+1;j<labels.Length;j++)
            {var a=labels[i].GetComponent<RectTransform>();var b=labels[j].GetComponent<RectTransform>();Assert.That(new Rect(a.anchoredPosition.x,-a.anchoredPosition.y,a.rect.width,a.rect.height).Overlaps(new Rect(b.anchoredPosition.x,-b.anchoredPosition.y,b.rect.width,b.rect.height)),Is.False,"HP labels must not overlap");}
            Assert.That(view.Field.rect.height,Is.GreaterThan(440));
            foreach(var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if(!text.gameObject.activeInHierarchy || text.name=="Event feedback" || text.name=="Body")continue;
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+2),text.name+" : "+text.text);
                if(text.name=="Value" && text.transform.parent.name.StartsWith("HP "))Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width),text.transform.parent.name+" fits HP text");
                if(text.name=="Placeholder" || text.name=="Value")continue;
                var c=new Vector3[4];text.rectTransform.GetWorldCorners(c);
                foreach(var corner in c){var p=Camera.main.WorldToViewportPoint(corner);Assert.That(p.x,Is.InRange(-.005f,1.005f),text.transform.parent.name+"/"+text.name+" : "+text.text);Assert.That(p.y,Is.InRange(-.005f,1.005f),text.transform.parent.name+"/"+text.name+" : "+text.text);}
            }
        }
        private static void CheckActorReadability(MobileG1View view)
        {
            var bodies=new Dictionary<string,Rect>();
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.activeInHierarchy && (r.name=="Private approved static skin" || r.name=="Fixture slime")))
            {
                var points=new List<Vector2>();
                // Project actual mesh vertices. World-axis bounds exaggerate billboard height at 63 degrees.
                var vertices=renderer.GetComponent<MeshFilter>().sharedMesh.vertices;
                foreach(var vertex in vertices)
                {var p=view.FieldCamera.WorldToViewportPoint(renderer.transform.TransformPoint(vertex));points.Add(new Vector2(p.x*view.Field.rect.width,(p.y-1)*view.Field.rect.height));}

                var rect=Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
                bodies[renderer.transform.parent.name.Substring(8)]=rect;
                Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(2),renderer.name+" inside field");Assert.That(rect.xMax,Is.LessThanOrEqualTo(view.Field.rect.width-2));
                Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(-view.Field.rect.height+2));Assert.That(rect.yMax,Is.LessThanOrEqualTo(-2));
            }
            // Readability is about what the player sees, not about rectangles. In the diorama view neighbouring cells
            // are 37-43 px apart on screen while each figure's board is 55 px wide, so boards of neighbours always
            // overlap (design/ROADMAP.md S1-1). What must never happen is a figure standing in front covering the face
            // of one behind it with its own painted pixels, or a foot HP badge covering any face.
            var actors=bodies.Keys.ToDictionary(id=>id,id=>Actor.Of(view,id,bodies[id]));
            // A face part stays readable when its centre is uncovered and at least half of it shows: a blade or a sleeve
            // may clip a neighbour's eye, but never hide it. (Tightest case seen, 2026-11-18: Garu's crossguard over the right
            // eye of the warden behind him at the start of encounter 1, 34/100 covered with the pupil clear; design/ROADMAP.md S1-1.)
            foreach(var back in actors.Values)
            {
                var parts=back.FaceParts();
                Assert.That(parts.Count,Is.GreaterThan(0),back.Id+" has a face to read");
                foreach(var part in parts)Assert.That(part.Points.Count,Is.GreaterThan(4),back.Id+" "+part.Name+" sampling sees the figure's own pixels");
                foreach(var front in actors.Values)
                {
                    if(front==back || front.Depth>=back.Depth)continue;
                    foreach(var part in parts)
                    {
                        var covered=part.Points.Count(front.Opaque);
                        Assert.That(front.Opaque(part.Centre) || covered*2>part.Points.Count,Is.False,front.Id+" hides the "+part.Name+" of "+back.Id+" ("+covered+"/"+part.Points.Count+" covered, centre "+(front.Opaque(part.Centre)?"covered":"clear")+") at step "+view.Session.Replay.State.Step+" rects "+front.Rect+" / "+back.Rect);
                    }
                }
            }
            var hpRects=new List<Rect>();
            foreach(var badge in Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("HP ") && c.gameObject.activeInHierarchy))
            {
                var t=badge.GetComponent<RectTransform>();var r=new Rect(t.anchoredPosition.x,t.anchoredPosition.y-t.rect.height,t.rect.width,t.rect.height);var own=bodies[badge.name.Substring(3)];
                Assert.That(r.yMax,Is.LessThanOrEqualTo(own.yMin-3),badge.name+" beneath body, never across head");
                Assert.That(r.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(r.xMax,Is.LessThanOrEqualTo(view.Field.rect.width));Assert.That(r.yMin,Is.GreaterThanOrEqualTo(-view.Field.rect.height));
                // A badge the view has hidden (alpha 0, HqFootBadges: it would sit over another figure's upper body) covers nothing.
                if(badge.alpha>.01f)foreach(var a in actors.Values)Assert.That(a.FaceParts().Sum(f=>f.Points.Count(r.Contains)),Is.Zero,badge.name+" must not cover the face of "+a.Id+" at step "+view.Session.Replay.State.Step+" rects "+r+" / "+a.Rect);
                hpRects.Add(r);
            }
            foreach(var route in Object.FindObjectsByType<MobileRouteGraphic>(FindObjectsSortMode.None).Where(g=>g.gameObject.activeInHierarchy))
            {
                Assert.That(route.raycastTarget,Is.False);
                for(var i=1;i<route.Points.Count;i++)for(var step=0;step<=30;step++)
                {
                    var p=Vector2.Lerp(route.Points[i-1],route.Points[i],step/30f);
                    foreach(var b in bodies)Assert.That(new Rect(b.Value.x-4,b.Value.y-4,b.Value.width+8,b.Value.height+8).Contains(p),Is.False,route.name+" avoids "+b.Key+" at step "+view.Session.Replay.State.Step);
                    foreach(var r in hpRects)Assert.That(r.Contains(p),Is.False,route.name+" must not cross foot HP at step "+view.Session.Replay.State.Step);
                }
            }
        }
        // One figure as the player sees it: its projected board, its distance from the camera, and whether a point in
        // Field pixels (x right, y down from 0 to -height) lands on its painted pixels.
        private sealed class Actor
        {
            public string Id;public Rect Rect;public float Depth;
            private Renderer renderer;private Camera camera;private Vector2 field;private Texture2D art;private bool mirrored;private List<Rect> spiritFace;private Rect companionFace;
            private static readonly Dictionary<Texture,Texture2D> readable=new Dictionary<Texture,Texture2D>();
            public static Actor Of(MobileG1View view,string id,Rect rect)
            {
                var r=GameObject.Find("Fixture "+id).GetComponentsInChildren<Renderer>().First(x=>x.name=="Private approved static skin" || x.name=="Fixture slime");
                var cam=view.FieldCamera;var m=r.sharedMaterial;
                return new Actor{Id=id,Rect=rect,renderer=r,camera=cam,field=new Vector2(view.Field.rect.width,view.Field.rect.height),
                    Depth=Vector3.Dot(r.bounds.center-cam.transform.position,cam.transform.forward),
                    art=r.name=="Private approved static skin" && m.mainTexture!=null?Readable(m.mainTexture):null,
                    mirrored=m.HasProperty("_Mirror") && m.GetFloat("_Mirror")>.5f,spiritFace=SpiritFace(view,r.transform.parent),companionFace=CompanionFace(id)};
            }
            // A companion's face in image space (u right, v up), from the head bone the puppet rig authors for that image:
            // from just below the bone's lower end to its middle, 60% of the head radius to each side (checked against Shiro's
            // eyes at v .65 under her hat on 2026-11-18). Image x in the rig is (u-.5)*.75.
            private static Rect CompanionFace(string id)
            {
                var index=id=="A"?0:id=="B"?1:id=="C"?2:id=="D"?3:-1;if(index<0)return Rect.zero;
                var head=RogueTactics.Presentation.Gfx.HqPuppet.Authored(index).Bones[RogueTactics.Presentation.Gfx.HqPuppet.Head];
                var u=head.A.x/.75f+.5f;var half=head.Radius*.6f/.75f;
                return Rect.MinMaxRect(u-half,head.A.y-.05f,u+half,(head.A.y+head.B.y)/2);
            }
            // A spirit's face is built from parts under "Spirit face": each eye and the mouth, as projected (not the cheeks).
            private static List<Rect> SpiritFace(MobileG1View view,Transform root)
            {
                var parts=new List<Rect>();var face=root.Find("Spirit face");if(face==null)return parts;
                foreach(var part in face.GetComponentsInChildren<Renderer>().Where(x=>x.name=="Bright spirit eye" || x.name=="Smile"))
                {
                    var b=part.bounds;var points=new List<Vector2>();
                    for(var k=0;k<8;k++)
                    {
                        var corner=new Vector3((k&1)==0?b.min.x:b.max.x,(k&2)==0?b.min.y:b.max.y,(k&4)==0?b.min.z:b.max.z);
                        var v=view.FieldCamera.WorldToViewportPoint(corner);points.Add(new Vector2(v.x*view.Field.rect.width,(v.y-1)*view.Field.rect.height));
                    }
                    parts.Add(Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y)));
                }
                return parts;
            }
            // The approved originals are imported unreadable; copy each once through the GPU instead of touching its import settings.
            private static Texture2D Readable(Texture texture)
            {
                if(readable.TryGetValue(texture,out var copy) && copy!=null)return copy;
                var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);Graphics.Blit(texture,rt);
                var previous=RenderTexture.active;RenderTexture.active=rt;copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
                copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
                return readable[texture]=copy;
            }
            public bool Opaque(Vector2 p)
            {
                if(!Rect.Contains(p))return false;
                // A spirit is a rounded solid body: its inscribed ellipse.
                if(art==null){var c=(p-Rect.center);var e=new Vector2(c.x/(Rect.width*.5f),c.y/(Rect.height*.5f));return e.sqrMagnitude<=1;}
                var ray=camera.ViewportPointToRay(new Vector3(p.x/field.x,p.y/field.y+1,0));var t=renderer.transform;
                if(!new Plane(t.forward,t.position).Raycast(ray,out var distance))return false;
                var local=t.InverseTransformPoint(ray.GetPoint(distance));if(Mathf.Abs(local.x)>.5f || Mathf.Abs(local.y)>.5f)return false;
                return art.GetPixelBilinear((mirrored?-local.x:local.x)+.5f,local.y+.5f).a>.5f;
            }
            // What the player reads a face by. A companion: its face from the rig's head bone, placed on the projected board
            // (mirrored when the figure faces the other way). A spirit: each eye and the mouth. Each part is sampled on a grid;
            // only the figure's own painted pixels count.
            public List<(string Name,Vector2 Centre,List<Vector2> Points)> FaceParts()
            {
                var rects=spiritFace;
                if(art!=null)
                {
                    var f=companionFace;var u0=mirrored?1-f.xMax:f.xMin;var u1=mirrored?1-f.xMin:f.xMax;
                    rects=new List<Rect>{Rect.MinMaxRect(Rect.xMin+Rect.width*u0,Rect.yMin+Rect.height*f.yMin,Rect.xMin+Rect.width*u1,Rect.yMin+Rect.height*f.yMax)};
                }
                var parts=new List<(string,Vector2,List<Vector2>)>();var n=0;
                foreach(var face in rects)
                {
                    var points=new List<Vector2>();
                    for(var i=0;i<10;i++)for(var j=0;j<10;j++){var p=new Vector2(Mathf.Lerp(face.xMin,face.xMax,(i+.5f)/10),Mathf.Lerp(face.yMin,face.yMax,(j+.5f)/10));if(Opaque(p))points.Add(p);}
                    parts.Add((art!=null?"face":n<2?"eye "+(n+1):"mouth",face.center,points));n++;
                }
                return parts;
            }
        }
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return null;
            foreach(var route in Object.FindObjectsByType<MobileRouteGraphic>(FindObjectsSortMode.None).Where(g=>g.gameObject.activeInHierarchy))
            {
                Assert.That(route.GetComponent<CanvasRenderer>(),Is.Not.Null,route.name+" must have an actual UI renderer");
                var mesh=route.canvasRenderer.GetMesh();
                Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThanOrEqualTo(6),route.name+" actual arrow mesh");Assert.That(route.canvasRenderer.cull,Is.False);
            }
            var rt=Camera.main.targetTexture;var old=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(390,844,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,390,844),0,0);texture.Apply();
            foreach(var route in Object.FindObjectsByType<MobileRouteGraphic>(FindObjectsSortMode.None).Where(g=>g.gameObject.activeInHierarchy))
            {
                var visible=0;var camera=route.canvas.worldCamera;
                for(var i=1;i<route.Points.Count;i++)for(var sample=2;sample<29;sample++)
                {
                    var point=Vector2.Lerp(route.Points[i-1],route.Points[i],sample/30f);var pixel=camera.WorldToViewportPoint(route.rectTransform.TransformPoint(point));
                    var c=texture.GetPixel(Mathf.Clamp(Mathf.RoundToInt(pixel.x*390),0,389),Mathf.Clamp(Mathf.RoundToInt(pixel.y*844),0,843));
                    if(Mathf.Abs(c.r-route.color.r)<.25f && Mathf.Abs(c.g-route.color.g)<.25f && Mathf.Abs(c.b-route.color.b)<.25f)visible++;
                }
                Assert.That(visible,Is.GreaterThanOrEqualTo(3),route.name+" must be visibly coloured in actual render");
            }
            Assert.That(texture.GetPixels32().Count(p=>p.r>150 && p.g>150 && p.b>150),Is.GreaterThan(300),"Visible Japanese HUD and approved art");
            if(name=="02-mobile-rules" || name=="07-mobile-comparison")
                Assert.That(texture.GetPixels(25,250,335,400).Count(p=>p.r>.45f && p.g>.45f && p.b>.45f),Is.GreaterThan(1000),"Actual modal body must be visible through stencil mask");
            Directory.CreateDirectory(Evidence);File.WriteAllBytes(Path.Combine(Evidence,name+".png"),texture.EncodeToPNG());RenderTexture.active=old;Object.Destroy(texture);
        }
        [UnityTest]
        public IEnumerator PredictionStaysAboveKeyboardAndReturnsWithoutChangingCore()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();view.SetDeveloperMode(true);var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;yield return null;
            var state=view.Session.Replay.State.Serialize();var input=view.Prediction.GetComponent<RectTransform>();var origin=input.anchoredPosition;
            view.Prediction.text="かつ";
            foreach(var height in new[]{300,420})
            {
                view.ApplyKeyboardArea(new Rect(0,0,390,height));Canvas.ForceUpdateCanvases();
                var corners=new Vector3[4];input.GetWorldCorners(corners);
                Assert.That(RectTransformUtility.WorldToScreenPoint(Camera.main,corners[0]).y,Is.GreaterThanOrEqualTo(height+11));
                Assert.That(RectTransformUtility.WorldToScreenPoint(Camera.main,corners[1]).y,Is.LessThan(797));
                Assert.That(view.Prediction.text,Is.EqualTo("かつ"));
                Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));
            }
            view.ApplyKeyboardArea(Rect.zero);Assert.That(input.anchoredPosition,Is.EqualTo(origin));
            foreach(var developer in new[]{false,true})
            {
                view.SetDeveloperMode(developer);yield return null;yield return null;
                if(!developer)
                {
                    yield return Capture("keyboard-product-before-policy");
                    Click(view,"Policy");yield return null;yield return null;
                    Assert.That(view.Prediction.gameObject.activeInHierarchy,Is.True,"Open Momo's policy panel before editing its prediction");
                }
                var resting=input.anchoredPosition;
                view.ApplyKeyboardArea(Rect.zero,true);Canvas.ForceUpdateCanvases();
                var docked=new Vector3[4];input.GetWorldCorners(docked);
                Assert.That(RectTransformUtility.WorldToScreenPoint(Camera.main,docked[0]).y,Is.GreaterThan(600),"Editing stays above tall keyboard even before area is reported");
                view.ApplyKeyboardArea(Rect.zero);
                Assert.That(input.anchoredPosition,Is.EqualTo(resting),"Editing returns to the actual layout in each mode");
                if(!developer)
                {
                    var a=new Vector3[4];var b=new Vector3[4];input.GetWorldCorners(a);view.Button("Start").GetComponent<RectTransform>().GetWorldCorners(b);
                    Assert.That(new Rect(a[0],a[2]-a[0]).Overlaps(new Rect(b[0],b[2]-b[0])),Is.False,"Returned prediction must not cover Start");
                    Assert.That(input.rect.height,Is.GreaterThanOrEqualTo(44));
                }
            }
            Camera.main.targetTexture=null;Object.Destroy(rt);
        }
        [UnityTest]
        public IEnumerator QueuedMouseReachesButtonsThroughInputSystem() => DevicePipeline(false);
        [UnityTest]
        public IEnumerator QueuedTouchReachesButtonsThroughInputSystem() => DevicePipeline(true);
        private static string lastDeviceSample;
        private static IEnumerator DevicePipeline(bool touch)
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();view.SetDeveloperMode(true);
            Assert.That(EventSystem.current.currentInputModule,Is.TypeOf<InputSystemUIInputModule>());
            var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            var previousFocus=InputSystem.settings.backgroundBehavior;
            var previousEditorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            // A batch Editor has no foreground window: simulate an active game for device routing only.
            // This is not evidence of OS native delivery, an iPhone touch, or an IME.
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputDevice device=touch?(InputDevice)InputSystem.AddDevice<Touchscreen>():InputSystem.AddDevice<Mouse>();
            try
            {
                yield return null;yield return null;
                view.SetTestSafeArea(new Rect(0,0,390,844),390,844);yield return null;yield return null;
                var baseline=view.Session.Replay.State.Serialize();
                yield return DeviceClick(view,"Attack",device);Assert.That(view.Session.Preference,Is.EqualTo(Preference.DirectAttack),lastDeviceSample);
                yield return DeviceClick(view,"Protect",device);Assert.That(view.Session.Preference,Is.EqualTo(Preference.ProtectAnchor),lastDeviceSample);
                Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(baseline));
                yield return DeviceClick(view,"Details",device);Assert.That(view.Button("Close").gameObject.activeInHierarchy,Is.True);
                yield return DeviceClick(view,"Close",device);Assert.That(view.Button("Close").gameObject.activeInHierarchy,Is.False);
                yield return DeviceClick(view,"Start",device);Assert.That(view.Session.Started,Is.True);
                while(view.Busy)yield return null;
                Assert.That(view.Session.Replay.State.Step,Is.EqualTo(1));
                yield return DeviceClick(view,"Next",device);while(view.Busy)yield return null;
                Assert.That(view.Session.Replay.State.Step,Is.EqualTo(2));
                yield return DeviceClick(view,"Play",device);Assert.That(view.Auto,Is.True);
                yield return DeviceClick(view,"Play",device);Assert.That(view.Auto,Is.False);
                var displayCursor=view.DisplayedEvents.Count;
                var paused=view.Session.Replay.State.Serialize();yield return new WaitForSecondsRealtime(.3f);
                Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(paused));Assert.That(view.DisplayedEvents.Count,Is.EqualTo(displayCursor));
                yield return DeviceClick(view,"Play",device);while(view.Busy || view.Session.Replay.State.Outcome==Outcome.Active)yield return null;
                yield return DeviceClick(view,"Reset",device);Assert.That(view.Session.Started,Is.False);
                Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(baseline));

            }
            finally
            {
                InputSystem.RemoveDevice(device);InputSystem.settings.backgroundBehavior=previousFocus;InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorRouting;
                Camera.main.targetTexture=null;Object.Destroy(rt);
            }
        }
        private static IEnumerator DeviceClick(MobileG1View view,string id,InputDevice device)
        {
            var button=view.Button(id);Assert.That(button.interactable,Is.True,id);
            var r=button.GetComponent<RectTransform>();var camera=button.GetComponentInParent<Canvas>().worldCamera;
            var point=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center));
            var module=(InputSystemUIInputModule)EventSystem.current.currentInputModule;
            Assert.That(module.point.action.enabled,Is.True,"UI point action enabled");
            Assert.That(module.leftClick.action.enabled,Is.True,"UI click action enabled");
            Assert.That(module.point.action.controls.Any(c=>c.device==device),Is.True,"Synthetic pointer resolves to point action");
            if(device is Mouse mouse)InputSystem.QueueStateEvent(mouse,new MouseState{position=point});
            yield return null;yield return null;
            if(device is Mouse m)InputSystem.QueueStateEvent(m,new MouseState{position=point}.WithButton(MouseButton.Left));
            else InputSystem.QueueStateEvent(device,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});
            yield return null;yield return null;
            lastDeviceSample="device="+device.name+" enabled="+device.enabled+" screen="+Screen.width+"x"+Screen.height+" camera="+camera.pixelRect+" queued="+point+" actionPoint="+module.point.action.ReadValue<Vector2>()+" pointControl="+module.point.action.activeControl?.path+" click="+module.leftClick.action.ReadValue<float>()+" clickControl="+module.leftClick.action.activeControl?.path+" lastHit="+module.GetLastRaycastResult(device.deviceId).gameObject?.name+" focused="+Application.isFocused+" esFocus="+EventSystem.current.isFocused+" moduleActive="+module.IsActive();
            Assert.That(module.GetLastRaycastResult(device.deviceId).gameObject,Is.EqualTo(button.gameObject),lastDeviceSample);
            if(device is Mouse up)InputSystem.QueueStateEvent(up,new MouseState{position=point});
            else InputSystem.QueueStateEvent(device,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});
            yield return null;yield return null;
        }
        [UnityTest]
        public IEnumerator JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();view.SetDeveloperMode(true);Assert.That(view.HasAllArt,Is.True,"Approved private originals are required for V0");
            var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;yield return null;yield return null;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;yield return null;
            yield return Capture("01-mobile-setup-safearea");CheckLayout(view);
            view.SetTestSafeArea(new Rect(10,34,355,763),390,844);yield return null;yield return null;CheckLayout(view);
            foreach(var id in new[]{"Protect","Attack","Start","Details","Compare"})
            {var r=view.Button(id).GetComponent<RectTransform>();var c=new Vector3[4];r.GetWorldCorners(c);foreach(var corner in c){var v=Camera.main.WorldToViewportPoint(corner);Assert.That(v.x,Is.InRange(10f/390-.005f,365f/390+.005f),id+" inside safe width");}}
            yield return Capture("00-narrow-safearea");view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;yield return null;
            var baseline=view.Session.Replay.State.Serialize();Click(view,"Details");yield return null;yield return null;CheckLayout(view);yield return Capture("02-mobile-rules");
            Click(view,"Close");yield return null;yield return null;
            foreach(var preference in new[]{Preference.ProtectAnchor,Preference.DirectAttack})
            {
                Click(view,preference==Preference.ProtectAnchor?"Protect":"Attack");yield return null;
                Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(baseline));view.Prediction.text="自分で結果を予想する";
                Click(view,"Start");while(view.Busy)yield return null;
                CheckLayout(view);yield return Capture(preference==Preference.ProtectAnchor?"03-protect":"05-attack");
                Click(view,"Next");while(view.Busy)yield return null;
                yield return Capture(preference==Preference.ProtectAnchor?"04-guard-damage-counter":"06-damage-counter");
                Click(view,"Play");yield return null;yield return null;Click(view,"Play");yield return null;
                Assert.That(view.Paused,Is.True);var stopped=view.Session.Replay.State.Serialize();yield return new WaitForSecondsRealtime(.6f);
                Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(stopped));
                Click(view,"Play");while(view.Busy || view.Session.Replay.State.Outcome==Outcome.Active){CheckActorReadability(view);yield return null;}
                Assert.That(view.Session.Replay.Hash(),Is.EqualTo(preference==Preference.ProtectAnchor?"e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d":"85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e"));
                Assert.That(view.DisplayedEvents.Select(e=>e.Serialize()),Is.EqualTo(view.Session.Replay.Events.Select(e=>e.Serialize())));
                CheckLayout(view);Click(view,"Reset");yield return null;yield return null;
            }
            Assert.That(view.Session.Trials.Count,Is.EqualTo(2));Click(view,"Compare");yield return null;yield return null;CheckLayout(view);yield return Capture("07-mobile-comparison");
            Click(view,"Close");yield return null;yield return null;CheckLayout(view);
            Camera.main.targetTexture=null;Object.Destroy(rt);
        }
    }
}
