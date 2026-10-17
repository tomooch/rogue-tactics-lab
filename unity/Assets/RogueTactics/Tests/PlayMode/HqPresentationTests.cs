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

namespace RogueTactics.Tests
{
    public class HqPresentationTests
    {
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
            InsideSafeArea(view,"Protect","Attack","Start");Assert.That(view.DeveloperMode,Is.False);
            // Compact product shelves must retain visible title, companion names and actual HP.
            foreach(var label in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                if(label.gameObject.activeInHierarchy && (label.name=="Title" || label.name=="Name" || label.name=="Actual HP"))
                    Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.1f),label.name+" fits without vertical clipping");
            yield return Capture("r1-setup");
            foreach(var preference in new[]{Preference.ProtectAnchor,Preference.DirectAttack})
            {
                view.Button(preference==Preference.ProtectAnchor?"Protect":"Attack").onClick.Invoke();view.Prediction.text="守るか攻めるか、結果を見る";
                view.Button("Start").onClick.Invoke();yield return null;
                Assert.That(view.Auto,Is.True);Assert.That(view.Button("Next").gameObject.activeInHierarchy,Is.False);
                yield return Capture(preference+"-start");
                view.Button("Play").onClick.Invoke();var count=view.DisplayedEvents.Count;var state=view.Session.Replay.State.Serialize();
                yield return new WaitForSecondsRealtime(.4f);
                Assert.That(view.DisplayedEvents.Count,Is.EqualTo(count),"Pause freezes displayed event cursor, including queued events");Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));
                yield return Capture(preference+"-paused");view.Button("Play").onClick.Invoke();
                var capturedGuard=false;var capturedCounter=false;var capturedHeal=false;var capturedDeath=false;
                while(view.Busy || view.Session.Replay.State.Outcome==Outcome.Active)
                {
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
                InsideSafeArea(view,"Reset","Compare");yield return Capture(preference+"-end");Assert.That(view.Button("Compare").interactable,Is.True);view.Button("Compare").onClick.Invoke();
                var sheet=GameObject.Find("Details panel").transform;var roster=GameObject.Find("Companion portraits").transform;
                Assert.That(sheet.GetSiblingIndex(),Is.GreaterThan(roster.GetSiblingIndex()),"Comparison sheet must render in front of the roster");
                yield return Capture(preference+"-comparison");view.Button("Close").onClick.Invoke();view.Button("Reset").onClick.Invoke();yield return null;
                Assert.That(view.Button("Compare").gameObject.activeInHierarchy,Is.False,"After reset the result button must not cover prediction/start");
                InsideSafeArea(view,"Protect","Attack","Start");yield return Capture(preference+"-reset");
            }
            Camera.main.targetTexture=null;Object.Destroy(rt);
        }
    }
}
