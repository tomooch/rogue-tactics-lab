using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RogueTactics.Core;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RogueTactics.Tests
{
    public class FloorExplorationFlowTests
    {
        private static void Press(MobileG1View view,string id)
        {
            Canvas.ForceUpdateCanvases();
            var button=view.Button(id);Assert.That(button.gameObject.activeInHierarchy && button.interactable,Is.True,id);
            var rect=button.GetComponent<RectTransform>();var canvas=button.GetComponentInParent<Canvas>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0),id);
            Assert.That(hits[0].gameObject,Is.EqualTo(button.gameObject),id+" is not covered");Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private static void Tap(MobileG1View view,GridPosition target)
        {
            var v=view.FieldCamera.WorldToViewportPoint(new Vector3(target.X*2.5f,0,target.Y*3.125f));
            Assert.That(v.x,Is.InRange(.01f,.99f),"Requested known floor must really be on screen");Assert.That(v.y,Is.InRange(.01f,.99f));
            var local=new Vector3(v.x*view.Field.rect.width,(v.y-1)*view.Field.rect.height,0);
            var canvas=view.Field.GetComponentInParent<Canvas>();var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,view.Field.TransformPoint(local))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0));
            Assert.That(hits[0].gameObject,Is.EqualTo(view.Field.gameObject),"Touch reaches real world surface, not a hidden control");
            ExecuteEvents.Execute(view.Field.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(view.TravelAnimating,Is.True,"Known corridor tap starts autonomous party traversal");
            Assert.That(view.TravelSelection,Is.EqualTo(target),"Only the accepted known selection is marked");
            var marker=GameObject.Find("Chosen known floor").GetComponent<LineRenderer>();
            Assert.That(marker.positionCount,Is.EqualTo(4));
            var center=Enumerable.Range(0,4).Select(marker.GetPosition).Aggregate(Vector3.zero,(a,p)=>a+p)/4;
            Assert.That(Vector3.Distance(center,new Vector3(target.X*2.5f,.04f,target.Y*3.125f)),Is.LessThan(.001f));
        }
        private static IEnumerator Capture(string name,bool mapExpected=true)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return null;
            var map=Object.FindFirstObjectByType<FloorMapGraphic>();Assert.That(map!=null,Is.EqualTo(mapExpected),"Map yields to policy/keyboard/modal and returns to field");
            if(mapExpected)
            {
                var mesh=map.canvasRenderer.GetMesh();
                TestContext.WriteLine("FLOOR_MAP_CAPTURE "+name+" vertices="+(mesh==null?0:mesh.vertexCount)+" culled="+map.canvasRenderer.cull+" rect="+map.rectTransform.rect);
                Assert.That(mesh,Is.Not.Null,"Known map must produce real UI geometry");Assert.That(mesh.vertexCount,Is.GreaterThanOrEqualTo(100),"Initial25known floor tiles must render");
            }
            var previous=RenderTexture.active;RenderTexture.active=Camera.main.targetTexture;var image=new Texture2D(390,844,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,390,844),0,0);image.Apply();var directory=Path.GetFullPath("Logs/hq/private");Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());RenderTexture.active=previous;Object.Destroy(image);
        }
        private static bool Touchable(MobileG1View view,GridPosition target)
        {
            var v=view.FieldCamera.WorldToViewportPoint(new Vector3(target.X*2.5f,0,target.Y*3.125f));
            if(v.x<=.01f || v.x>=.99f || v.y<=.01f || v.y>=.99f)return false;
            var canvas=view.Field.GetComponentInParent<Canvas>();var local=new Vector3(v.x*view.Field.rect.width,(v.y-1)*view.Field.rect.height,0);
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,view.Field.TransformPoint(local))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);return hits.Count>0 && hits[0].gameObject==view.Field.gameObject;
        }
        private static IEnumerator WalkTo(MobileG1View view,GridPosition destination)
        {
            var attempts=0;
            while(!view.Exploration.Party.Values.Contains(destination) && attempts++<60)
            {
                var floor=view.Exploration;var paths=floor.Party.Values.Select(p=>floor.Map.Path(p,destination)).Where(p=>p.Count>0).OrderBy(p=>p.Count).ToArray();
                Assert.That(paths,Is.Not.Empty);var occupied=floor.Party.Values.ToArray();var known=new HashSet<GridPosition>(floor.KnownCells);
                // Itinerary knowledge belongs to the test, never the UI: each actual
                // instruction must hit an already-seen, unobscured on-screen tile.
                var targets=paths[0].Skip(1).Take(5).Where(p=>known.Contains(p) && !occupied.Contains(p) && Touchable(view,p)).Reverse().ToArray();
                Assert.That(targets,Is.Not.Empty,"Known route must have an unobscured touch target");
                Tap(view,targets[0]);var deadline=Time.realtimeSinceStartup+30;
                while(view.TravelAnimating && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(view.TravelAnimating,Is.False);yield return null;
            }
            Assert.That(view.Exploration.Party.Values.Contains(destination),Is.True,"Party reaches room through the visible field");
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator SameFieldBattleHandoffTouchTravelPauseDiscoveryAndRetry()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();Assert.That(view.ExplorationEnabled,Is.True);
            var target=new RenderTexture(390,844,24);Camera.main.targetTexture=target;view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
            yield return Capture("floor-initial");var initialMapVertices=Object.FindFirstObjectByType<FloorMapGraphic>().canvasRenderer.GetMesh().vertexCount;
            LogAssert.Expect(LogType.Log,new Regex("^HQ_FRAME_METRICS \\{"));
            LogAssert.Expect(LogType.Log,"MOBILE_G1_RESULT ProtectAnchor e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d");
            Press(view,"Start");var deadline=Time.realtimeSinceStartup+55;
            while((view.Session.Replay.State.Outcome==Outcome.Active || view.Busy) && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.Busy,Is.False);Assert.That(view.Session.Replay.Hash(),Is.EqualTo("e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d"));
            var camera=view.FieldCamera.transform.position;var zoom=view.FieldCamera.orthographicSize;
            var original=view.Session.Replay.State.Units.Where(u=>!u.Enemy && u.Alive).ToDictionary(u=>u.Id,u=>GameObject.Find("Fixture "+u.Id).transform.position);
            var hash=view.Session.Replay.Hash();yield return Capture("floor-before-exploration");Press(view,"Explore");
            Assert.That(view.IsExploring,Is.True);Assert.That(view.FieldCamera.transform.position,Is.EqualTo(camera));
            foreach(var actor in original)Assert.That(GameObject.Find("Fixture "+actor.Key).transform.position,Is.EqualTo(actor.Value));
            Assert.That(view.Exploration.Party.Count,Is.EqualTo(4));Assert.That(GameObject.Find("Exploration room 2"),Is.Null,"Unknown room must not render");
            yield return Capture("floor-entry");Tap(view,new GridPosition(5,3));deadline=Time.realtimeSinceStartup+5;
            while(original.All(p=>Vector3.Distance(GameObject.Find("Fixture "+p.Key).transform.position,p.Value)<.2f) && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.TravelAnimating,Is.True);yield return Capture("floor-moving");Press(view,"Play");
            var paused=original.Keys.ToDictionary(id=>id,id=>GameObject.Find("Fixture "+id).transform.position);var known=view.Exploration.DiscoveredRooms;
            yield return new WaitForSecondsRealtime(.25f);
            foreach(var actor in paused)Assert.That(Vector3.Distance(actor.Value,GameObject.Find("Fixture "+actor.Key).transform.position),Is.LessThan(.001f));
            Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(known));yield return Capture("floor-paused");Press(view,"Play");
            deadline=Time.realtimeSinceStartup+25;while(view.TravelAnimating && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.TravelAnimating,Is.False);Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(2));
            Assert.That(Vector3.Distance(camera,view.FieldCamera.transform.position),Is.GreaterThan(5));Assert.That(view.FieldCamera.orthographicSize,Is.EqualTo(zoom).Within(.001f));
            foreach(var unit in view.Session.Replay.State.Units.Where(u=>!u.Enemy && u.Alive))Assert.That(view.Exploration.Health[unit.Id],Is.EqualTo(unit.Hp));
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash),"Exploration must not rewrite the completed combat trace");
            yield return Capture("floor-discovery");Tap(view,new GridPosition(10,3));deadline=Time.realtimeSinceStartup+25;
            while(view.TravelAnimating && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.TravelAnimating,Is.False);yield return Capture("floor-room-one");
            foreach(var room in new[]{1,2,3,4,5,0})
            {
                yield return WalkTo(view,view.Exploration.Map.Rooms[room].Center);
                Assert.That(view.FieldCamera.orthographicSize,Is.EqualTo(zoom).Within(.001f));
                foreach(var unit in view.Session.Replay.State.Units.Where(u=>!u.Enemy && u.Alive))Assert.That(view.Exploration.Health[unit.Id],Is.EqualTo(unit.Hp));
                yield return Capture("floor-room-"+room+"-center");
            }
            Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(6));Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash));
            Press(view,"Reset");Assert.That(view.IsExploring,Is.False);Assert.That(view.Session.Started,Is.False);
            Assert.That(view.TravelSelection,Is.Null);Assert.That(GameObject.Find("Chosen known floor"),Is.Null,"Retry hides the former selection");
            Assert.That(view.Session.Replay.State.Units.Select(u=>u.Hp),Is.EqualTo(Fixture.Baseline().Units.Select(u=>u.Hp)));
            yield return Capture("floor-retry");Assert.That(Object.FindFirstObjectByType<FloorMapGraphic>().canvasRenderer.GetMesh().vertexCount,Is.EqualTo(initialMapVertices),"Retry clears explored map as well as world geometry");
            Assert.That(GameObject.Find("Exploration room 2"),Is.Null);Camera.main.targetTexture=null;Object.Destroy(target);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest,Timeout(120000)]
        public IEnumerator AttackHandoffDoesNotReviveOrMoveDeadCompanion()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);view.Select(Preference.DirectAttack);yield return null;
            LogAssert.Expect(LogType.Log,new Regex("^HQ_FRAME_METRICS \\{"));
            const string hash="85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e";
            LogAssert.Expect(LogType.Log,"MOBILE_G1_RESULT DirectAttack "+hash);Press(view,"Start");var deadline=Time.realtimeSinceStartup+55;
            while((view.Session.Replay.State.Outcome==Outcome.Active || view.Busy) && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash));Press(view,"Explore");yield return null;
            Assert.That(view.Exploration.Party.Count,Is.EqualTo(3));Assert.That(view.Exploration.Party.ContainsKey("A"),Is.False);
            Assert.That(Resources.FindObjectsOfTypeAll<Transform>().Single(t=>t.name=="Fixture A" && t.gameObject.scene.IsValid()).gameObject.activeSelf,Is.False);
            yield return Capture("floor-attack-entry");Tap(view,new GridPosition(5,3));deadline=Time.realtimeSinceStartup+25;
            while(view.TravelAnimating && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.TravelAnimating,Is.False);Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(2));
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash));yield return Capture("floor-attack-discovery");
            Camera.main.targetTexture=null;Object.Destroy(rt);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator FloorMapYieldsToPolicyPredictionAndReturnsAfterClosing()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return Capture("floor-before-policy");
            var state=view.Session.Replay.State.Serialize();Press(view,"Policy");yield return null;yield return null;
            Assert.That(Object.FindFirstObjectByType<FloorMapGraphic>(),Is.Null,"Map cannot cover policy or docked keyboard input");
            view.Prediction.text="かつ";view.ApplyKeyboardArea(Rect.zero,true);Canvas.ForceUpdateCanvases();
            var input=view.Prediction.GetComponent<RectTransform>();var corners=new Vector3[4];input.GetWorldCorners(corners);
            Assert.That(RectTransformUtility.WorldToScreenPoint(Camera.main,corners[0]).y,Is.GreaterThan(600));
            view.ApplyKeyboardArea(Rect.zero);yield return Capture("floor-policy",false);
            Press(view,"ClosePolicy");yield return null;yield return Capture("floor-after-policy");
            Press(view,"Details");yield return null;yield return Capture("floor-record",false);
            Press(view,"Close");yield return null;yield return Capture("floor-after-record");
            Assert.That(view.Prediction.text,Is.EqualTo("かつ"));Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));
            Camera.main.targetTexture=null;Object.Destroy(rt);LogAssert.NoUnexpectedReceived();
        }
    }
}
