using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RogueTactics.Tests
{
    public class HqPresentationTests
    {
        private static void Press(MobileG1View view,string id)
        {
            var button=view.Button(id);Assert.That(button.gameObject.activeInHierarchy && button.interactable,Is.True,id+" is available");
            var rect=button.GetComponent<RectTransform>();var canvas=button.GetComponentInParent<Canvas>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),id+" is hit-testable");Assert.That(hits[0].gameObject,Is.EqualTo(button.gameObject),id+" is unobstructed");
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private static void InsideSafeArea(MobileG1View view,params string[] ids)
        {
            Canvas.ForceUpdateCanvases();var safe=new Vector3[4];view.SafeRoot.GetWorldCorners(safe);
            foreach(var id in ids){var r=view.Button(id).GetComponent<RectTransform>();var c=new Vector3[4];r.GetWorldCorners(c);Assert.That(r.rect.height,Is.GreaterThanOrEqualTo(44),id);foreach(var p in c){Assert.That(p.x,Is.InRange(safe[0].x-.01f,safe[2].x+.01f),id);Assert.That(p.y,Is.InRange(safe[0].y-.01f,safe[2].y+.01f),id);}}
        }
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return null;
            foreach(var accent in Object.FindObjectsByType<RelationAccent>(FindObjectsSortMode.None).Where(a=>a.gameObject.activeInHierarchy))
            {var mesh=accent.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThanOrEqualTo(16),"Local actor/target accents must actually render");}
            var rt=Camera.main.targetTexture;var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(390,844,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,390,844),0,0);image.Apply();
            var path=Path.GetFullPath("Logs/hq/private");Directory.CreateDirectory(path);File.WriteAllBytes(Path.Combine(path,name+".png"),image.EncodeToPNG());
            RenderTexture.active=previous;Object.Destroy(image);
            CheckVisibleHealth(Object.FindFirstObjectByType<MobileG1View>());
        }
        private static void CheckVisibleHealth(MobileG1View view)
        {
            foreach(var unit in Fixture.Baseline().Units)
            {
                var track=view.Field.Find("HP "+unit.Id).GetComponent<RectTransform>();
                var died=view.DisplayedEvents.Any(e=>e.Kind==EventKind.Death && e.Target==unit.Id);
                Assert.That(track.gameObject.activeSelf,Is.EqualTo(!died),"HP visibility follows displayed death, including retry");
                if(died)continue;
                Assert.That(track.GetComponentsInChildren<Text>(true),Is.Empty,"On-board health must not expose fixture IDs or numeric black labels");
                var latest=view.DisplayedEvents.LastOrDefault(e=>e.Target==unit.Id && (e.Kind==EventKind.Damage || e.Kind==EventKind.Counter || e.Kind==EventKind.Heal));
                var expected=latest==null?unit.Hp:latest.HpAfter;
                var fill=track.Find("HP bar").GetComponent<RectTransform>();
                Assert.That(fill.rect.width/(track.rect.width-2),Is.EqualTo((float)expected/unit.MaxHp).Within(.001f),"HP shows displayed impact, not Core's queued future state");
                var owner=GameObject.Find("Fixture "+unit.Id).GetComponentsInChildren<Renderer>().First(r=>r.name=="Private approved static skin" || r.name=="Fixture slime");
                var points=owner.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>view.FieldCamera.WorldToViewportPoint(owner.transform.TransformPoint(v))).ToArray();
                var center=(points.Min(p=>p.x)+points.Max(p=>p.x))*.5f*view.Field.rect.width;
                var foot=(points.Min(p=>p.y)-1)*view.Field.rect.height;
                Assert.That(track.anchoredPosition.x+track.rect.width/2,Is.EqualTo(center).Within(.5f),"Health stays horizontally attached to its visible owner");
                Assert.That(foot-track.anchoredPosition.y,Is.InRange(3f,10f),"Health stays close below the visible foot");
            }
        }
        [UnityTest]
        public IEnumerator PauseDuringMovementPreservesVisiblePoseAndCursor()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
            view.Button("Attack").onClick.Invoke();var pawn=GameObject.Find("Fixture B").transform;var origin=pawn.position;
            view.Button("Start").onClick.Invoke();var deadline=Time.realtimeSinceStartup+5;
            while(Vector3.Distance(origin,pawn.position)<.3f && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Vector3.Distance(origin,pawn.position),Is.GreaterThan(.3f),"Observe actual movement before pausing");
            var pose=pawn.position;view.Button("Play").onClick.Invoke();
            Assert.That(Vector3.Distance(pose,pawn.position),Is.LessThan(.001f),"Pause must not snap actor to the old cell");
            var cursor=view.DisplayedEvents.Count;yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Vector3.Distance(pose,pawn.position),Is.LessThan(.001f));Assert.That(view.DisplayedEvents.Count,Is.EqualTo(cursor));
            yield return Capture("r3-paused-mid-move");view.Button("Play").onClick.Invoke();
            while(!view.DisplayedEvents.Any(e=>e.Kind==EventKind.Move))yield return null;
            Assert.That(view.DisplayedEvents.Count(e=>e.Kind==EventKind.Move),Is.EqualTo(1));
            view.Button("Play").onClick.Invoke();Camera.main.targetTexture=null;Object.Destroy(rt);
        }
        [UnityTest]
        public IEnumerator FixedYawCameraComparisonPreservesState()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);var initial=view.Session.Replay.State.Serialize();
            var ambient=Object.FindFirstObjectByType<HqAmbient>();
            var shadows=ambient.GetComponentsInChildren<MeshRenderer>().ToDictionary(r=>r,r=>r.shadowCastingMode);
            ambient.SetReduced(true);ambient.SetReduced(false);
            foreach(var pair in shadows)Assert.That(pair.Key.shadowCastingMode,Is.EqualTo(pair.Value),"Effect round-trip must restore distant and near shadow settings");
            view.SetComparisonYaw(0);
            foreach(var pitch in new[]{75f,63f,55f,45f}){view.SetComparisonCamera(pitch);yield return Capture("camera-"+pitch+"-setup");Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(initial));}
            view.SetComparisonCamera(55);view.SetComparisonYaw(-18);yield return Capture("camera-55-yaw-minus18-setup");Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(initial));
            view.SetDeveloperMode(true);view.Button("Start").onClick.Invoke();while(view.Busy)yield return null;
            foreach(var pitch in new[]{75f,63f}){view.SetComparisonCamera(pitch);yield return Capture("camera-"+pitch+"-guard");}
            Camera.main.targetTexture=null;Object.Destroy(rt);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator ProductFlowPreservesDisplayedCursorAndBothFrozenOutcomes()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;
            InsideSafeArea(view,"Policy","Start");Assert.That(view.DeveloperMode,Is.False);
            Assert.That(view.Field.rect.height,Is.EqualTo(view.SafeRoot.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.height).Within(1),"Scenery reaches screen edges behind safe-area HUD");
            Assert.That(view.Button("Start").GetComponentInChildren<Text>().text,Does.Contain("戦闘開始"),"Primary action explains its effect");
            // Compact product shelves must retain visible title, companion names and actual HP.
            foreach(var label in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                if(label.gameObject.activeInHierarchy && (label.name=="Title" || label.name=="Name" || label.name=="Actual HP"))
                    Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.1f),label.name+" fits without vertical clipping");
            yield return Capture("r1-setup");
            Press(view,"Policy");yield return null;
            Assert.That(view.Button("Protect").gameObject.activeInHierarchy,Is.True,"Momo's card opens her policy choices");
            InsideSafeArea(view,"Protect","Attack","ClosePolicy");yield return Capture("r1-policy");
            Press(view,"ClosePolicy");yield return null;
            foreach(var preference in new[]{Preference.ProtectAnchor,Preference.DirectAttack})
            {
                Press(view,"Policy");yield return null;
                Press(view,preference==Preference.ProtectAnchor?"Protect":"Attack");var prediction=preference==Preference.ProtectAnchor?"守る方針で、シロのHPに注目する":"攻める方針で、モモの行動に注目する";view.Prediction.text=prediction;
                Press(view,"ClosePolicy");yield return null;
                Press(view,"Start");yield return null;
                Assert.That(view.Auto,Is.True);Assert.That(view.Button("Next").gameObject.activeInHierarchy,Is.False);
                // Scenic headers must not hide the active initiative strip (observed quarter-A regression).
                var order=view.Field.Find("Discrete actor order").GetComponent<RectTransform>();
                var orderCorners=new Vector3[4];order.GetWorldCorners(orderCorners);
                var titleCorners=new Vector3[4];view.SafeRoot.Find("Boundary").GetComponent<RectTransform>().GetWorldCorners(titleCorners);
                Assert.That(orderCorners[1].y,Is.LessThan(titleCorners[0].y),"Initiative stays below the title/subtitle");
                yield return Capture(preference+"-start");
                Press(view,"Play");var count=view.DisplayedEvents.Count;var state=view.Session.Replay.State.Serialize();
                yield return new WaitForSecondsRealtime(.4f);
                Assert.That(view.DisplayedEvents.Count,Is.EqualTo(count),"Pause freezes displayed event cursor, including queued events");Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));
                yield return Capture(preference+"-paused");Press(view,"Play");
                var capturedGuard=false;var capturedCounter=false;var capturedHeal=false;var capturedDeath=false;
                while(view.Busy || view.Session.Replay.State.Outcome==Outcome.Active)
                {
                    CheckVisibleHealth(view);
                    var events=view.DisplayedEvents;
                    var action=events.LastOrDefault(e=>e.Kind==EventKind.Action);
                    if(action!=null && !action.Detail.StartsWith("Move"))
                        Assert.That(Object.FindObjectsByType<MobileRouteGraphic>(FindObjectsSortMode.None).Any(g=>g.gameObject.activeInHierarchy),Is.False,"Only Move may draw a trajectory; protect/attack/counter/heal must stay local");
                    if(!capturedGuard && events.Any(e=>e.Kind==EventKind.Protect)){capturedGuard=true;yield return Capture(preference+"-guard");}
                    if(!capturedCounter && events.Any(e=>e.Kind==EventKind.Counter)){capturedCounter=true;yield return Capture(preference+"-counter");}
                    if(!capturedHeal && events.Any(e=>e.Kind==EventKind.Heal)){capturedHeal=true;yield return Capture(preference+"-heal");}
                    if(!capturedDeath && events.Any(e=>e.Kind==EventKind.Death)){capturedDeath=true;yield return Capture(preference+"-death");}
                    yield return null;
                }
                Assert.That(view.Session.Replay.Hash(),Is.EqualTo(preference==Preference.ProtectAnchor?"e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d":"85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e"));
                Assert.That(view.DisplayedEvents.Select(e=>e.Serialize()),Is.EqualTo(view.Session.Replay.Events.Select(e=>e.Serialize())));
                InsideSafeArea(view,"Reset","Compare");yield return Capture(preference+"-end");Assert.That(view.Button("Compare").interactable,Is.True);Press(view,"Compare");
                var sheet=GameObject.Find("Details panel").transform;var roster=GameObject.Find("Companion portraits").transform;
                Assert.That(sheet.GetSiblingIndex(),Is.GreaterThan(roster.GetSiblingIndex()),"Comparison sheet must render in front of the roster");
                Assert.That(sheet.Find("Details viewport/Body").GetComponent<Text>().text,Does.Contain(prediction),"Entered prediction survives into its trial comparison");
                Assert.That(view.Button("Diagnostics").gameObject.activeInHierarchy,Is.False,"Normal results must not expose development controls");
                yield return Capture(preference+"-comparison");Press(view,"Close");Press(view,"Reset");yield return null;
                Assert.That(view.Button("Compare").gameObject.activeInHierarchy,Is.False,"After reset the result button must not cover prediction/start");
                InsideSafeArea(view,"Policy","Start");yield return Capture(preference+"-reset");
            }
            Camera.main.targetTexture=null;Object.Destroy(rt);
        }
    }
}
