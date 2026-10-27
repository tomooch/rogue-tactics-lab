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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
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
            var v=view.FieldCamera.WorldToViewportPoint(new Vector3(target.X*2.5f,0,target.Y*2.5f));
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
            Assert.That(Vector3.Distance(center,new Vector3(target.X*2.5f,.04f,target.Y*2.5f)),Is.LessThan(.001f));
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
        // Human placed the known map in the lower right (2026-10-27); wherever it sits it must never hide the cast:
        // over an actor it turns see-through.
        private static void AssertMapClearOfCast(MobileG1View view,string when)
        {
            var map=(RectTransform)view.SafeRoot.Find("Explored floor map");if(!map.gameObject.activeInHierarchy)return;
            var corners=new Vector3[4];map.GetWorldCorners(corners);var field=view.Field.rect;
            Vector2 Normalized(Vector3 world){var local=(Vector2)view.Field.InverseTransformPoint(world);return new Vector2((local.x-field.xMin)/field.width,(local.y-field.yMin)/field.height);}
            var a=Normalized(corners[0]);var b=Normalized(corners[2]);var mapRect=Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.activeInHierarchy && (r.name=="Private approved static skin" || r.name=="Fixture slime")))
            {
                var points=renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>(Vector2)view.FieldCamera.WorldToViewportPoint(renderer.transform.TransformPoint(v))).ToArray();
                var body=Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
                if(mapRect.Overlaps(body))Assert.That(view.KnownMapAlpha,Is.LessThanOrEqualTo(.35f),"Known map hides "+renderer.transform.parent.name+" at "+when);
            }
        }
        private static bool Touchable(MobileG1View view,GridPosition target)
        {
            var v=view.FieldCamera.WorldToViewportPoint(new Vector3(target.X*2.5f,0,target.Y*2.5f));
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
            Press(view,"Start");yield return null;Press(view,"Map");yield return null;
            Assert.That(view.Paused,Is.True);Assert.That(view.Auto,Is.False);
            Assert.That(view.Button("Play").transform.Find("Label").GetComponent<Text>().text,Does.Contain("再開"),"Opening the map updates the transport label immediately");
            yield return Capture("floor-reference-battle-map");Press(view,"Close");Press(view,"Play");
            var deadline=Time.realtimeSinceStartup+55;
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
            Press(view,"Details");Assert.That(view.Exploration.Paused,Is.True,"Exploration record holds movement like the map");
            var recordPose=original.Keys.ToDictionary(id=>id,id=>GameObject.Find("Fixture "+id).transform.position);
            yield return new WaitForSecondsRealtime(.15f);
            foreach(var actor in recordPose)Assert.That(Vector3.Distance(actor.Value,GameObject.Find("Fixture "+actor.Key).transform.position),Is.LessThan(.001f));
            Assert.That(view.SafeRoot.Find("Details panel/Heading").GetComponent<Text>().text,Is.EqualTo("旅の記録"),"Record has the same meaning while exploring");
            yield return Capture("floor-travel-journal",false);Press(view,"RecordCompare");yield return null;
            Assert.That(view.Button("RecordDown").gameObject.activeInHierarchy,Is.False,"A short result has no useless page action");
            yield return Capture("floor-reference-travel-record",false);Press(view,"Close");
            Assert.That(view.Button("Play").GetComponentInChildren<HudGlyphGraphic>().Symbol,Is.EqualTo(HudSymbol.Play));Press(view,"Play");
            deadline=Time.realtimeSinceStartup+25;while(view.TravelAnimating && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.TravelAnimating,Is.False);Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(2));
            Assert.That(Vector3.Distance(camera,view.FieldCamera.transform.position),Is.GreaterThan(5));Assert.That(view.FieldCamera.orthographicSize,Is.EqualTo(zoom).Within(.001f));
            foreach(var unit in view.Session.Replay.State.Units.Where(u=>!u.Enemy && u.Alive))Assert.That(view.Exploration.Health[unit.Id],Is.EqualTo(unit.Hp));
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash),"Exploration must not rewrite the completed combat trace");
            yield return Capture("floor-discovery");Tap(view,new GridPosition(10,3));deadline=Time.realtimeSinceStartup+25;
            while(view.TravelAnimating && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.TravelAnimating,Is.False);yield return Capture("floor-room-one");
            foreach(var room in new[]{1,2,3,4,5,6,7,0})
            {
                yield return WalkTo(view,view.Exploration.Map.Rooms[room].Center);
                Assert.That(view.FieldCamera.orthographicSize,Is.EqualTo(zoom).Within(.001f));
                foreach(var unit in view.Session.Replay.State.Units.Where(u=>!u.Enemy && u.Alive))Assert.That(view.Exploration.Health[unit.Id],Is.EqualTo(unit.Hp));
                yield return Capture("floor-room-"+room+"-center");
            }
            Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(8));Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash));
            Press(view,"Details");yield return null;yield return Capture("floor-before-record-retry",false);
            Press(view,"RecordRetry");Assert.That(view.IsExploring,Is.False);Assert.That(view.Session.Started,Is.False);
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
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash));
            var unavailable=view.SafeRoot.Find("Companion portraits/Companion A");
            var status=unavailable.Find("Role meaning").GetComponent<Text>();
            Assert.That(status.text,Is.EqualTo("戦闘不能"),"Death replaces the normal role, not just its icon");
            Assert.That(status.preferredWidth,Is.LessThanOrEqualTo(status.rectTransform.rect.width+.1f),"The complete death status must remain readable");
            Assert.That(unavailable.Find("Core role").gameObject.activeInHierarchy,Is.False,"Do not retain a live role badge on the unavailable companion");
            yield return Capture("floor-attack-result");Press(view,"Explore");yield return null;
            Assert.That(view.Exploration.Party.Count,Is.EqualTo(3));Assert.That(view.Exploration.Party.ContainsKey("A"),Is.False);
            Assert.That(Resources.FindObjectsOfTypeAll<Transform>().Single(t=>t.name=="Fixture A" && t.gameObject.scene.IsValid()).gameObject.activeSelf,Is.False);
            yield return Capture("floor-attack-entry");Tap(view,new GridPosition(5,3));deadline=Time.realtimeSinceStartup+25;
            while(view.TravelAnimating && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.TravelAnimating,Is.False);Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(2));
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash));yield return Capture("floor-attack-discovery");
            Camera.main.targetTexture=null;Object.Destroy(rt);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator AllyFacingFollowsScreenMotionWithoutMovingBounds()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var pawn=GameObject.Find("Fixture A").transform;
            var skin=pawn.Find("Private approved static skin").GetComponent<Renderer>();var start=pawn.position;var right=view.FieldCamera.transform.right;
            foreach(var side in new[]{1f,-1f,1f})
            {
                // Walk like a tween: short per-frame steps along the screen's horizontal axis.
                for(var i=0;i<8;i++){pawn.position+=right*side*.08f;yield return null;}
                Assert.That(view.PoseMirrored("A"),Is.EqualTo(side>0),"The single approved image turns toward its on-screen walk");
                var bounds=skin.bounds;yield return null;
                Assert.That(skin.bounds.center,Is.EqualTo(bounds.center));Assert.That(skin.bounds.size,Is.EqualTo(bounds.size),"Pose is a shader offset; projected body rects must not move");
            }
            pawn.position=start-right*3;yield return null;
            Assert.That(view.PoseMirrored("A"),Is.True,"A teleport (reset or retry) is not a walk and keeps the facing");
            pawn.position=start;yield return null;
        }
        [UnityTest]
        public IEnumerator ReferencePartsKeepRealOwnershipAndKnownOnlyMap()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return Capture("floor-reference-overview");
            var title=view.SafeRoot.Find("Title").GetComponent<Text>();Assert.That(title.text,Is.EqualTo("天空の遺跡"));Assert.That(title.preferredHeight,Is.LessThanOrEqualTo(title.rectTransform.rect.height+.1f),"Place name must actually fit its frame");
            var state=view.Session.Replay.State.Serialize();var known=view.SafeRoot.Find("Explored floor map");
            Assert.That(known.GetComponent<RectTransform>().anchoredPosition.x,Is.GreaterThan(view.SafeRoot.rect.width/2),"Known map sits in the lower right");
            AssertMapClearOfCast(view,"overview");
            Assert.That(known.Find("Discovered rooms").GetComponent<Text>().text,Is.EqualTo("発見 1室"),"Do not expose the six-room hidden layout");
            Assert.That(view.SafeRoot.Find("Observed journey/Current floor caption/Meaning").GetComponent<Text>().text,Is.EqualTo("現在階"),"No invented floor number or total");
            var floorMarker=view.SafeRoot.Find("Observed journey/Current floor marker");
            Assert.That(floorMarker.GetComponentInChildren<Button>(),Is.Null,"Current floor is a location marker, not an invented traversal action");
            foreach(var label in new[]{title,view.SafeRoot.Find("Boundary").GetComponent<Text>(),known.Find("Discovered rooms").GetComponent<Text>()})
                Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.1f),"Single-line navigation text must retain its full glyph height");
            foreach(var id in new[]{"A","D","C","B"})
            {
                var card=view.SafeRoot.Find("Companion portraits/Companion "+id);var portrait=card.Find("Approved portrait").GetComponent<RawImage>();
                Assert.That(portrait.texture,Is.SameAs(view.CharacterArt[id[0]-'A']),"Portrait samples approved pixels, not newly invented faces");
                Assert.That((float)portrait.texture.width/portrait.texture.height,Is.EqualTo(.75f).Within(.005f),"Approved art keeps its authored 3:4 pixels; a power-of-two square silently widens every face crop");
                var sampledAspect=portrait.texture.width*portrait.uvRect.width/(portrait.texture.height*portrait.uvRect.height);
                Assert.That(portrait.rectTransform.rect.width/portrait.rectTransform.rect.height,Is.EqualTo(sampledAspect).Within(.001f),"Approved faces must not be stretched by the portrait layout");
                Assert.That(card.Find("Name").GetComponent<Text>().text,Is.EqualTo(id=="A"?"シロ":id=="B"?"モモ・方針":id=="C"?"ガル":"ミャル"));
                Assert.That(card.Find("Actual HP").GetComponent<Text>().text,Does.StartWith("HP "));
                foreach(var labelName in new[]{"Name","Role meaning","Actual HP"})
                {
                    var label=card.Find(labelName).GetComponent<Text>();
                    Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.1f),labelName+" must render, not be vertically truncated");
                    Assert.That(label.preferredWidth,Is.LessThanOrEqualTo(label.rectTransform.rect.width+.1f),labelName+" must fit horizontally without hiding companion information");
                }
                Assert.That(card.GetComponentInChildren<HudPanelGraphic>().canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(20));
            }
            Assert.That(view.Button("PolicyShortcut").gameObject.activeInHierarchy,Is.False);
            Assert.That(view.Prediction.gameObject.activeInHierarchy,Is.False,"No mandatory prediction form in the main flow");
            Assert.That(view.Button("Start").GetComponent<RectTransform>().rect.width,Is.GreaterThan(view.SafeRoot.rect.width-30),"One primary entrust action");
            Press(view,"Map");yield return null;yield return Capture("floor-reference-map",true);
            Assert.That(view.SafeRoot.Find("Details panel/Expanded known terrain").gameObject.activeInHierarchy,Is.True);
            var behind=view.Button("Start").GetComponent<RectTransform>();var click=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(Camera.main,behind.TransformPoint(behind.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(click,hits);
            Assert.That(hits[0].gameObject.name,Is.EqualTo("Modal input curtain"),"Map blocks the real underlying Start hit, not only darkens it");
            ExecuteEvents.Execute(hits[0].gameObject,click,ExecuteEvents.pointerClickHandler);Assert.That(view.Session.Started,Is.False);
            Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));Press(view,"Close");yield return null;
            Press(view,"Policy");yield return null;yield return Capture("floor-reference-choice",false);
            Assert.That(view.Prediction.gameObject.activeInHierarchy,Is.False,"Policy changes do not require a forecast form");
            Assert.That(view.Button("Protect").transform.Find("Label").GetComponent<Text>().text,Does.StartWith("選択中"));Press(view,"Attack");yield return null;
            Assert.That(view.Button("Attack").transform.Find("Label").GetComponent<Text>().text,Does.StartWith("選択中"));
            Press(view,"Protect");Press(view,"ClosePolicy");yield return null;
            Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));Press(view,"Details");yield return null;yield return null;
            Assert.That(view.Prediction.gameObject.activeInHierarchy,Is.True,"Optional memo lives in record before combat");
            Assert.That(view.Prediction.transform.parent.name,Is.EqualTo("Details panel"));
            Assert.That(view.SafeRoot.Find("Details panel/Details viewport/Body").GetComponent<Text>().text,Does.Not.Contain("未入力"));
            view.Prediction.text=new string('あ',120);
            Assert.That(view.Prediction.text.Length,Is.EqualTo(120));
            yield return Capture("floor-reference-journal",false);
            Assert.That(view.Button("Diagnostics").gameObject.activeInHierarchy,Is.False);
            Press(view,"PresentationSettings");yield return null;
            Assert.That(view.Button("Diagnostics").gameObject.activeInHierarchy,Is.True);
            yield return Capture("floor-reference-settings",false);Press(view,"Close");yield return null;
            Assert.That(view.Prediction.gameObject.activeInHierarchy,Is.False);
            Press(view,"Policy");yield return null;Press(view,"Attack");yield return null;
            Assert.That(view.Prediction.text,Is.EqualTo(new string('あ',120)),"Changing policy must not erase the optional record memo");
            Press(view,"Protect");Press(view,"ClosePolicy");yield return null;
            Press(view,"Details");yield return null;
            Assert.That(view.Prediction.text,Is.EqualTo(new string('あ',120)),"Memo remains reachable after returning from policy");
            yield return Capture("floor-memo-after-policy-change",false);Press(view,"Close");yield return null;
            Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));
            LogAssert.Expect(LogType.Log,new Regex("^HQ_FRAME_METRICS \\{"));
            const string hash="e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d";
            LogAssert.Expect(LogType.Log,"MOBILE_G1_RESULT ProtectAnchor "+hash);
            Press(view,"Start");yield return null;
            var pauseRect=view.Button("Play").GetComponent<RectTransform>();
            var pausePoint=RectTransformUtility.WorldToScreenPoint(Camera.main,pauseRect.TransformPoint(pauseRect.rect.center));
            Assert.That(pauseRect.rect.height,Is.EqualTo(48).Within(.01f));
            Assert.That(view.Button("Play").transform.Find("Label").GetComponent<Text>().text,Does.Contain("一時停止"));
            Press(view,"Play");Assert.That(view.Paused,Is.True);
            Assert.That(view.Button("Play").transform.Find("Label").GetComponent<Text>().text,Does.Contain("再開"));
            Press(view,"Play");Assert.That(view.Paused,Is.False);
            var oldPauseCorners=new Vector3[4];pauseRect.GetWorldCorners(oldPauseCorners);
            var previousFocus=InputSystem.settings.backgroundBehavior;
            var previousRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=pausePoint});
                yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=pausePoint}.WithButton(MouseButton.Left));
                yield return null;yield return null;
                var module=(InputSystemUIInputModule)EventSystem.current.currentInputModule;
                Assert.That(module.GetLastRaycastResult(mouse.deviceId).gameObject,Is.EqualTo(view.Button("Play").gameObject),"Pointer is actually held over Pause before the transition");
                var deadline=Time.realtimeSinceStartup+65;
                while((view.Session.Replay.State.Outcome==Outcome.Active || view.Busy) && Time.realtimeSinceStartup<deadline)yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=pausePoint});
                yield return null;yield return null;
                Assert.That(view.IsExploring,Is.False,"Releasing an old Pause press after victory must not start exploration");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=previousFocus;InputSystem.settings.editorInputBehaviorInPlayMode=previousRouting;
            }
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash),"One Start runs the whole frozen battle without per-turn input");
            Assert.That(view.Button("Compare").gameObject.activeInHierarchy,Is.False,"Comparison is optional in record");
            Assert.That(view.Button("Reset").gameObject.activeInHierarchy,Is.False,"Exploration is the primary continuation");
            Assert.That(view.Button("Explore").gameObject.activeInHierarchy,Is.True);yield return Capture("floor-auto-victory");
            var reserve=view.SafeRoot.Find("Terminal transport reserve");
            var terminalLabel=reserve.Find("Terminal result").GetComponent<Text>();
            Assert.That(terminalLabel.text,Is.EqualTo("勝利\n4人が生還"),"Only the observed result and survivors are shown");
            Assert.That(terminalLabel.preferredWidth,Is.LessThanOrEqualTo(terminalLabel.rectTransform.rect.width+.1f));
            Assert.That(terminalLabel.preferredHeight,Is.LessThanOrEqualTo(terminalLabel.rectTransform.rect.height+.1f));
            Assert.That(reserve.GetComponent<Button>(),Is.Null);
            var terminalState=view.Session.Replay.State.Serialize();
            foreach(var fraction in new[]{.02f,.5f,.98f})
            {
                var point=Vector3.Lerp(oldPauseCorners[0],oldPauseCorners[2],fraction);point.y=(oldPauseCorners[0].y+oldPauseCorners[2].y)/2;
                var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(Camera.main,point)};
                var terminalHits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,terminalHits);
                Assert.That(terminalHits,Is.Not.Empty);
                Assert.That(terminalHits[0].gameObject,Is.EqualTo(reserve.gameObject),"The former Pause region is a non-actionable input sink, not Explore or Field");
                ExecuteEvents.Execute(terminalHits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
            }
            Assert.That(view.IsExploring,Is.False);Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(terminalState));
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo(hash));
            Press(view,"Details");yield return null;yield return Capture("floor-auto-record",false);
            Assert.That(reserve.gameObject.activeInHierarchy,Is.False,"Terminal result yields to the modal");
            Press(view,"RecordCompare");yield return null;yield return null;
            var viewport=view.SafeRoot.Find("Details panel/Details viewport").GetComponent<RectTransform>();var scroll=viewport.GetComponent<ScrollRect>();
            var body=scroll.content.GetComponent<Text>();Assert.That(body.text,Does.Contain(new string('あ',120)));
            Assert.That(body.text,Does.Not.Contain("未入力"));Assert.That(body.text,Does.Contain("4人が生還"));
            Assert.That(scroll.content.rect.height,Is.GreaterThan(viewport.rect.height),"A real saved maximum-length memo needs reachable overflow");
            Assert.That(view.Button("RecordDown").gameObject.activeInHierarchy,Is.True);
            var viewportCorners=new Vector3[4];var pageCorners=new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);view.Button("RecordDown").GetComponent<RectTransform>().GetWorldCorners(pageCorners);
            Assert.That(viewportCorners[0].y,Is.GreaterThanOrEqualTo(pageCorners[1].y),"Record body must end above page controls");
            Press(view,"RecordUp");yield return null;var top=scroll.verticalNormalizedPosition;
            Press(view,"RecordDown");yield return null;
            Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(top-.05f),"Real UI paging exposes saved record content");
            yield return Capture("floor-reference-journal-paged",false);Press(view,"RecordRetry");yield return null;
            Assert.That(view.Session.Started,Is.False);Assert.That(view.Session.Trials.Count,Is.EqualTo(1),"Retry keeps the actual previous record");
            Assert.That(reserve.gameObject.activeInHierarchy,Is.False,"Retry clears the terminal reserve");
            Camera.main.targetTexture=null;Object.Destroy(rt);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator FloorMapYieldsToPolicyAndOptionalRecordMemo()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return Capture("floor-before-policy");
            var state=view.Session.Replay.State.Serialize();Press(view,"Policy");yield return null;yield return null;
            Assert.That(Object.FindFirstObjectByType<FloorMapGraphic>(),Is.Null,"Map cannot cover policy or docked keyboard input");
            Assert.That(view.Prediction.gameObject.activeInHierarchy,Is.False);Press(view,"ClosePolicy");yield return null;
            yield return Capture("floor-after-policy");Press(view,"Details");yield return null;
            Assert.That(view.Prediction.gameObject.activeInHierarchy,Is.True);
            view.Prediction.text="かつ";view.ApplyKeyboardArea(Rect.zero,true);Canvas.ForceUpdateCanvases();
            var input=view.Prediction.GetComponent<RectTransform>();var corners=new Vector3[4];input.GetWorldCorners(corners);
            Assert.That(RectTransformUtility.WorldToScreenPoint(Camera.main,corners[0]).y,Is.GreaterThan(600));
            view.ApplyKeyboardArea(Rect.zero);yield return Capture("floor-policy",false);
            yield return Capture("floor-record",false);
            Press(view,"Close");yield return null;yield return Capture("floor-after-record");
            Assert.That(view.Prediction.text,Is.EqualTo("かつ"));Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));
            Camera.main.targetTexture=null;Object.Destroy(rt);LogAssert.NoUnexpectedReceived();
        }
    }
}
