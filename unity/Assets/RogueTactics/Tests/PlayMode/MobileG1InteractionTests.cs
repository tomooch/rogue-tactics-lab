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
                foreach(var corner in c){var p=Camera.main.WorldToViewportPoint(corner);Assert.That(p.x,Is.InRange(-.005f,1.005f),text.name);Assert.That(p.y,Is.InRange(-.005f,1.005f),text.name);}
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
            var all=bodies.ToArray();for(var i=0;i<all.Length;i++)for(var j=i+1;j<all.Length;j++)
                Assert.That(all[i].Value.Overlaps(all[j].Value),Is.False,"Silhouette separation "+all[i].Key+" / "+all[j].Key+" at step "+view.Session.Replay.State.Step+" rects "+all[i].Value+" / "+all[j].Value);
            var hpRects=new List<Rect>();
            foreach(var badge in Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("HP ") && c.gameObject.activeInHierarchy))
            {
                var t=badge.GetComponent<RectTransform>();var r=new Rect(t.anchoredPosition.x,t.anchoredPosition.y-t.rect.height,t.rect.width,t.rect.height);var own=bodies[badge.name.Substring(3)];
                Assert.That(r.yMax,Is.LessThanOrEqualTo(own.yMin-3),badge.name+" beneath body, never across head");
                Assert.That(r.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(r.xMax,Is.LessThanOrEqualTo(view.Field.rect.width));Assert.That(r.yMin,Is.GreaterThanOrEqualTo(-view.Field.rect.height));
                foreach(var b in bodies)Assert.That(r.Overlaps(b.Value),Is.False,badge.name+" must not cover "+b.Key);
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
                view.SetDeveloperMode(developer);view.ApplyKeyboardArea(Rect.zero,true);Canvas.ForceUpdateCanvases();
                var docked=new Vector3[4];input.GetWorldCorners(docked);
                Assert.That(RectTransformUtility.WorldToScreenPoint(Camera.main,docked[0]).y,Is.GreaterThan(600),"Editing stays above tall keyboard even before area is reported");
                view.ApplyKeyboardArea(Rect.zero);
                Assert.That(input.anchoredPosition.y,Is.EqualTo(developer?-54:-51));
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
