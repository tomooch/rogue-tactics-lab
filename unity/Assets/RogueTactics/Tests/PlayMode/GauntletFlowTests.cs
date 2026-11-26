using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core;
using RogueTactics.Core.Exploration;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RogueTactics.Tests
{
    // Gauntlet experiment: later rooms hold new encounters on the same field.
    public class GauntletFlowTests
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
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return null;
            var previous=RenderTexture.active;RenderTexture.active=Camera.main.targetTexture;var image=new Texture2D(390,844,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,390,844),0,0);image.Apply();var directory=Path.GetFullPath("Logs/hq/private");Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());RenderTexture.active=previous;Object.Destroy(image);
        }
        private static bool Touchable(MobileG1View view,GridPosition target)
        {
            var v=view.FieldCamera.WorldToViewportPoint(new Vector3(target.X*2.5f,0,target.Y*2.5f));
            if(v.x<=.01f || v.x>=.99f || v.y<=.01f || v.y>=.99f)return false;
            var canvas=view.Field.GetComponentInParent<Canvas>();var local=new Vector3(v.x*view.Field.rect.width,(v.y-1)*view.Field.rect.height,0);
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,view.Field.TransformPoint(local))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);return hits.Count>0 && hits[0].gameObject==view.Field.gameObject;
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
        private static IEnumerator WaitBattle(MobileG1View view,bool stopAtCrisis)
        {
            var deadline=Time.realtimeSinceStartup+120;
            while(Time.realtimeSinceStartup<deadline && (view.Busy || view.Session.Replay.State.Outcome==Outcome.Active) && !(stopAtCrisis && view.CrisisPaused))yield return null;
        }
        private static IEnumerator Encounter(MobileG1View view,int index,Preference start)
        {
            var room=view.Exploration.Map.Rooms[GauntletRooms.Room[index]];
            yield return new WaitForSecondsRealtime(1);yield return Capture("gauntlet-"+(index+1)+"-explore");
            yield return WalkTo(view,room.Center);
            Assert.That(view.ChallengeAvailable,Is.True,"A guardian room offers the next fight");
            yield return Capture("gauntlet-"+(index+1)+"-challenge");Press(view,"Challenge");
            var deadline=Time.realtimeSinceStartup+5;while(view.IsExploring || view.Session.EncounterIndex!=index || view.Busy)if(Time.realtimeSinceStartup>deadline)break;else yield return null;
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(view.Session.EncounterIndex,Is.EqualTo(index));Assert.That(view.IsExploring,Is.False);
            var origin=GauntletRooms.Origin(view.Exploration==null?new FloorMap():view.Exploration.Map,index);
            foreach(var u in view.Session.Replay.State.Units)
            {var at=GameObject.Find("Fixture "+u.Id).transform.position;Assert.That(Vector3.Distance(at,new Vector3((u.Position.X+origin.X)*2.5f,0,(u.Position.Y+origin.Y)*2.5f)),Is.LessThan(.01f),u.Id+" stands on its room cell");}
            view.Select(start);AssertMapClearOfCast(view,"encounter "+(index+1)+" setup");yield return Capture("gauntlet-"+(index+1)+"-setup");
            Press(view,"Start");yield return new WaitForSecondsRealtime(.2f);
            Press(view,"Interrupt");
            var stopDeadline=Time.realtimeSinceStartup+10;
            while(!view.InstructionOpen && Time.realtimeSinceStartup<stopDeadline)yield return null;
            Assert.That(view.InstructionOpen,Is.True);Assert.That(view.Busy,Is.False);
            var state=view.Session.Replay.State.Serialize();yield return new WaitForSecondsRealtime(.3f);
            Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(state));
            Assert.That(view.Session.Replay.CorrectionsLeft,Is.EqualTo(1));
            yield return Capture("instruction-"+index+"-choose");
            Press(view,"DirectCancel");Assert.That(view.Session.Replay.CorrectionsLeft,Is.EqualTo(1));
            yield return null;Press(view,"Interrupt");
            stopDeadline=Time.realtimeSinceStartup+10;
            while(!view.InstructionOpen && Time.realtimeSinceStartup<stopDeadline)yield return null;
            Assert.That(view.InstructionOpen,Is.True);
            yield return null;yield return null; // Let the reopened canvas submit its graphics before the next real click.
            var ally=view.Session.Replay.State.Units.First(u=>!u.Enemy && u.Alive).Id;
            var enemy=view.Session.Replay.State.Units.First(u=>u.Enemy && u.Alive).Id;
            Press(view,"DirectAlly"+ally);Press(view,"DirectEnemy"+enemy);
            var selection=view.SafeRoot.Find("Instruction overlay/Instruction sheet/Selection").GetComponent<Text>().text;
            Assert.That(selection,Does.Contain("今なら"));Assert.That(selection,Does.Contain("状況は変わります"));
            var routes=Object.FindObjectsByType<MobileRouteGraphic>(FindObjectsSortMode.None).Count(g=>g.name.StartsWith("Round intent") && g.gameObject.activeInHierarchy);
            Assert.That(routes,Is.LessThanOrEqualTo(1),"Only the chosen instruction is drawn while deciding");
            yield return Capture("instruction-"+index+"-confirm");
            Press(view,"DirectConfirm");Assert.That(view.Session.Replay.CorrectionsLeft,Is.EqualTo(0));
            Assert.That(view.SafeRoot.Find("Instruction receipt panel/Instruction receipt").GetComponent<Text>().text,Does.Contain("待っています"));
            var receipt=view.SafeRoot.Find("Instruction receipt panel/Instruction receipt").GetComponent<Text>();
            var receiptDeadline=Time.realtimeSinceStartup+30;
            while((receipt.text.Contains("待っています") || receipt.text.Contains("実行中")) && Time.realtimeSinceStartup<receiptDeadline)yield return null;
            Assert.That(receipt.text,Does.Not.Contain("待っています"),"Confirmed instruction receives an observed outcome");
            Assert.That(receipt.text,Does.Contain("指示"));
            yield return Capture("instruction-"+index+"-outcome");
            yield return WaitBattle(view,false);
            Assert.That(view.Busy,Is.False);Assert.That(view.Session.Replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(view.CrisisPaused,Is.False,"No forced crisis interruption");
            yield return null; // HUD Update follows the final presentation coroutine.
            Canvas.ForceUpdateCanvases();
            var resultDetail=view.SafeRoot.Find("Result banner/Result detail").GetComponent<Text>();
            Assert.That(resultDetail.text,Does.Contain(receipt.text),"The observed instruction outcome survives the victory transition");
            Canvas.ForceUpdateCanvases();
            var resultTitle=view.SafeRoot.Find("Result banner/Result title").GetComponent<Text>();
            Assert.That(resultTitle.cachedTextGenerator.vertexCount,Is.GreaterThan(0),"Victory title must actually render, not only contain text");
            AssertMapClearOfCast(view,"encounter "+(index+1));yield return Capture("gauntlet-"+(index+1)+"-end");
        }
        [UnityTest]
        public IEnumerator CompanionHealthNumbersHaveRenderedGlyphsAtPhoneScales()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            Camera.main.targetTexture=new RenderTexture(390,844,24);
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;
            Canvas.ForceUpdateCanvases();
            foreach(var id in new[]{"A","D","C","B"})
            {
                var label=view.SafeRoot.Find("Companion portraits/Companion "+id+"/Actual HP").GetComponent<Text>();
                Assert.That(label.text,Does.StartWith("HP "));
                Assert.That(label.cachedTextGenerator.vertexCount,Is.GreaterThan(0),"Actual health must create visible glyphs, not merely hold text");
                Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height));
                foreach(var scale in new[]{1f,2f,3f})
                {
                    var settings=label.GetGenerationSettings(label.rectTransform.rect.size);settings.scaleFactor=scale;
                    using(var generator=new TextGenerator())
                    {generator.Populate(label.text,settings);Assert.That(generator.vertexCount,Is.GreaterThan(0),id+" health glyphs at "+scale+"x");}
                }
            }
            yield return Capture("health-number-layout");
        }
        // Diagnostic only: preserve the conservative readability gate. Real material/sorting,
        // no replacement shader, camera change, alpha threshold or acceptance threshold.
        [UnityTest,Timeout(120000)]
        public IEnumerator InitialActorRenderedContributionDiagnostic()
        {
            var oldTime=Time.timeScale;
            try
            {
                foreach(var scene in new[]{"MobileG1","HqExploration"})
                {
                    yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;
                    var view=Object.FindFirstObjectByType<MobileG1View>();
                    view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
                    Time.timeScale=0;
                    view.enabled=false; // Freeze unscaled breathing at its actual rendered pose.
                    var ambients=Object.FindObjectsByType<HqAmbient>(FindObjectsSortMode.None).Where(a=>a.enabled).ToArray();
                    foreach(var ambient in ambients)ambient.enabled=false;
                    yield return null;yield return null;
                    var bodies=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                        .Where(r=>r.enabled && r.gameObject.activeInHierarchy &&
                            (r.name=="Private approved static skin" || r.name=="Fixture slime"))
                        .OrderBy(r=>r.transform.parent.name).ToArray();
                    Assert.That(bodies.Length,Is.EqualTo(6));
                    var directory=Path.GetFullPath("Logs/hq/private/occlusion-"+scene);Directory.CreateDirectory(directory);
                    Color32[] Read(string name)
                    {
                        var previous=RenderTexture.active;RenderTexture.active=view.FieldCamera.targetTexture;
                        var image=new Texture2D(RenderTexture.active.width,RenderTexture.active.height,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
                        var pixels=image.GetPixels32();File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());
                        RenderTexture.active=previous;Object.Destroy(image);return pixels;
                    }
                    bool Different(Color32 a,Color32 b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b))>3;
                    var full=Read("full");yield return null;yield return null;var repeated=Read("repeat");
                    var csv=new List<string>{"actor,isolated_contribution_pixels,visible_contribution_pixels,hidden_contribution_pixels,unstable_baseline_pixels"};
                    try
                    {
                        foreach(var body in bodies)body.enabled=false;
                        yield return null;yield return null;var background=Read("without-bodies");
                        for(var n=0;n<bodies.Length;n++)
                        {
                            var body=bodies[n];body.enabled=true;yield return null;yield return null;
                            var isolated=Read("isolated-"+n);body.enabled=false;
                            foreach(var other in bodies)if(other!=body)other.enabled=true;
                            yield return null;yield return null;var without=Read("without-"+n);
                            int total=0,visible=0,unstable=0;
                            for(var i=0;i<isolated.Length;i++)if(Different(isolated[i],background[i]))
                            {total++;if(Different(full[i],without[i]))visible++;if(Different(full[i],repeated[i]))unstable++;}
                            csv.Add(body.transform.parent.name+","+total+","+visible+","+(total-visible)+","+unstable);
                            foreach(var other in bodies)other.enabled=false;
                        }
                    }
                    finally {foreach(var body in bodies)body.enabled=true;view.enabled=true;foreach(var ambient in ambients)ambient.enabled=true;}
                    File.WriteAllLines(Path.Combine(directory,"contribution.csv"),csv);
                    Assert.That(view.Session.Replay.State.Step,Is.EqualTo(0),"Diagnostic never advances Core");
                }
            }
            finally {Time.timeScale=oldTime;}
        }
        [UnityTest,Timeout(300000)]
        public IEnumerator FirstEncounterAlternativeInstructionChangesObservedOutcome()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var target=new RenderTexture(390,844,24);Camera.main.targetTexture=target;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
            Press(view,"Policy");yield return null;Press(view,"Attack");Press(view,"ClosePolicy");yield return null;
            Press(view,"Start");yield return WaitBattle(view,false);yield return null;
            Assert.That(view.Session.Replay.State.Units.Count(u=>!u.Enemy && u.Alive),Is.EqualTo(3));
            yield return Capture("choice-baseline-three-survivors");
            Press(view,"RetryEncounter");yield return null;yield return null;
            Assert.That(view.Session.Preference,Is.EqualTo(Preference.DirectAttack));
            Assert.That(view.Button("DirectCardC").gameObject.activeSelf,Is.False,"Preparation keeps original card behavior");
            Press(view,"Start");yield return null;yield return null;Press(view,"DirectCardC");
            var deadline=Time.realtimeSinceStartup+15;
            while(!view.InstructionOpen && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(view.InstructionOpen,Is.True);yield return null;yield return null;
            Assert.That(view.SafeRoot.Find("Instruction overlay/Instruction sheet/Help").GetComponent<Text>().text,Does.Contain("ガル"),"Portrait selects the requested living ally after the action boundary");
            Press(view,"DirectEnemyY");
            var reason=view.SafeRoot.Find("Companion portraits/Companion C/Role meaning").GetComponent<Text>();
            Assert.That(reason.text,Is.EqualTo("指示に従う"));
            Assert.That(reason.preferredWidth,Is.LessThanOrEqualTo(reason.rectTransform.rect.width+.1f));
            Assert.That(view.SafeRoot.Find("Instruction overlay/Instruction sheet/Selection").GetComponent<Text>().text,Does.Contain("攻撃"));
            yield return Capture("choice-garu-flower-selection");
            Press(view,"DirectConfirm");yield return WaitBattle(view,false);yield return null;
            Assert.That(view.Session.Replay.State.Units.Count(u=>!u.Enemy && u.Alive),Is.EqualTo(4));
            Assert.That(view.Button("DirectCardC").gameObject.activeSelf,Is.False,"Used instruction cannot be offered again");
            Assert.That(view.SafeRoot.Find("Result banner/Result detail").GetComponent<Text>().text,Does.Contain("ガルへの指示"));
            Assert.That(view.SafeRoot.Find("Result banner/Result detail").GetComponent<Text>().text,Does.Contain("4人が生還（前回3人）"));
            yield return Capture("choice-directed-four-survivors");
        }
        private static IEnumerator ReopenSavedBattle(BattleCheckpointStore store,RenderTexture target,
            System.Action<MobileG1View> opened)
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");
            var original=SceneManager.GetActiveScene();
            var template=Object.FindFirstObjectByType<MobileG1View>();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Resume with a battle checkpoint"));
            var host=new GameObject("Battle before initialization");
            host.SetActive(false);
            var view=Object.Instantiate(template,host.transform);
            var camera=Object.Instantiate(Camera.main);
            camera.targetTexture=target;
            view.ConfigureBattlePersistence(store);
            yield return SceneManager.UnloadSceneAsync(original);
            host.SetActive(true);
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
            yield return null;yield return null;
            opened(view);
        }

        [UnityTest]
        public IEnumerator DiskCheckpointRestoresPausedHudAndTerminalResult()
        {
            var folder=Path.Combine(Path.GetTempPath(),System.Guid.NewGuid().ToString("N"));
            var store=new BattleCheckpointStore(Path.Combine(folder,"battle.json"));
            var source=new MobileG1Session();
            source.Select(Preference.DirectAttack);
            source.Start("復元する予想");
            source.Advance();
            source.Advance();
            source.TargetOnce("C","X");
            var target=new RenderTexture(390,844,24);
            MobileG1View view=null;
            try
            {
                Assert.That(store.TryWrite(source.CaptureBattle()),Is.True);
                yield return ReopenSavedBattle(store,target,opened=>view=opened);
                Assert.That(view.Session.Replay.Hash(),Is.EqualTo(source.Replay.Hash()));
                Assert.That(view.DisplayedEvents.Select(e=>e.Serialize()),Is.EqualTo(source.Replay.Events.Select(e=>e.Serialize())));
                Assert.That(view.Session.Replay.CorrectionsLeft,Is.Zero);
                Assert.That(view.Paused,Is.True);
                var ally=source.Replay.State.Find("A");
                Assert.That(ally.Hp,Is.LessThan(ally.MaxHp));
                var hpLabel=view.SafeRoot.Find("Companion portraits/Companion A/Actual HP").GetComponent<Text>();
                Assert.That(hpLabel.text,Is.EqualTo("HP "+ally.Hp+" / "+ally.MaxHp));
                Assert.That(view.Busy,Is.False,"Starting from a checkpoint does not play historical events");
                var previousStep=view.Session.Replay.State.Step;
                Press(view,"Play");
                Assert.That(view.Session.Replay.State.Step,Is.EqualTo(previousStep+1));
                Assert.That(view.Busy,Is.True);
                view.SendMessage("OnApplicationPause",true);
                Assert.That(store.Read(out var saved),Is.EqualTo(BattleCheckpointReadStatus.Loaded));
                Assert.That(saved.Steps,Is.EqualTo(previousStep),"Interruption during animation keeps the last fully shown step");

                // Restore a finished battle into a different fresh view as though restarted.
                var finished=new MobileG1Session();
                finished.LoadEncounter(1);
                finished.Start("");
                while(finished.Replay.State.Outcome==Outcome.Active)finished.Advance();
                Assert.That(store.TryWrite(finished.CaptureBattle()),Is.True);
                Camera.main.targetTexture=null;
                yield return ReopenSavedBattle(store,target,opened=>view=opened);
                Assert.That(view.Session.Replay.Hash(),Is.EqualTo(finished.Replay.Hash()));
                Assert.That(view.Session.EncounterIndex,Is.EqualTo(1));
                var roomOrigin=GauntletRooms.Origin(new FloorMap(),1);
                var position=finished.Replay.State.Find("B").Position.Offset(roomOrigin.X,roomOrigin.Y);
                var pawn=GameObject.Find("Fixture B").transform.position;
                Assert.That(pawn,Is.EqualTo(new Vector3(position.X*2.5f,0,position.Y*2.5f)));
                Assert.That(view.Button("Explore").gameObject.activeInHierarchy,Is.True);
                Assert.That(GameObject.Find("Fixture A"),Is.Null);
                Assert.That(GameObject.Find("Fallen rest A"),Is.Not.Null);
                Assert.That(view.Busy,Is.False,"Starting from a checkpoint does not play historical events");
                Press(view,"Explore");
                var floor=view.Exploration;
                var partyBefore=floor.Party.Values.ToArray();
                var cell=floor.Map.Neighbours(partyBefore[0]).First(p=>!partyBefore.Contains(p) && floor.KnownCells.Contains(p));
                Assert.That(view.EntrustTravel(cell),Is.True);
                var travelDeadline=Time.realtimeSinceStartup+15;
                while(view.TravelAnimating && Time.realtimeSinceStartup<travelDeadline)yield return null;
                Assert.That(view.TravelAnimating,Is.False);
                Assert.That(floor.Party.Values,Is.Not.EqualTo(partyBefore));
                var expected=floor.Capture();
                Assert.That(store.Read(out var afterTravel),Is.EqualTo(BattleCheckpointReadStatus.Loaded));
                Assert.That(afterTravel.Floor.Party,Is.EqualTo(expected.Party));
                Camera.main.targetTexture=null;
                yield return ReopenSavedBattle(store,target,opened=>view=opened);
                Assert.That(view.IsExploring,Is.True);
                Assert.That(view.Exploration.Party,Is.EqualTo(expected.Party));
                Assert.That(view.Exploration.KnownCells,Is.EqualTo(expected.KnownCells));
                Assert.That(view.Exploration.DiscoveredRooms,Is.EqualTo(expected.DiscoveredRooms.Count));
                Assert.That(view.Exploration.Health,Is.EqualTo(floor.Health));
                view.ResetBaseline();
                Assert.That(store.Read(out _),Is.EqualTo(BattleCheckpointReadStatus.Missing));
                yield return null;yield return null;
                Press(view,"Start");
                Assert.That(store.Read(out var started),Is.EqualTo(BattleCheckpointReadStatus.Loaded));
                Assert.That(started.Steps,Is.Zero);
                view.RequestInstruction();
                var deadline=Time.realtimeSinceStartup+15;
                while(!view.InstructionOpen && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(view.InstructionOpen,Is.True);
                yield return null;yield return null;
                Press(view,"DirectAllyC");
                Press(view,"DirectEnemyY");
                var instructionStep=view.Session.Replay.State.Step;
                Press(view,"DirectConfirm");
                Assert.That(store.Read(out var instructed),Is.EqualTo(BattleCheckpointReadStatus.Loaded));
                Assert.That(instructed.Steps,Is.EqualTo(instructionStep));
                Assert.That(instructed.Input.Target,Is.EqualTo("Y"));
            }
            finally
            {
                if(view!=null)view.gameObject.SetActive(false);
                if(Camera.main!=null)Camera.main.targetTexture=null;
                Object.Destroy(target);
                if(Directory.Exists(folder))Directory.Delete(folder,true);
            }
        }

        [UnityTest]
        public IEnumerator ThreeEnemiesAppearOnBoardAndCanBeSelected()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");
            var original=SceneManager.GetActiveScene();
            var template=Object.FindFirstObjectByType<MobileG1View>();
            var testScene=SceneManager.CreateScene("Three-enemy presentation");
            SceneManager.SetActiveScene(testScene);
            var host=new GameObject("Encounter before initialization");
            host.SetActive(false);
            var view=Object.Instantiate(template,host.transform);
            var camera=Object.Instantiate(Camera.main);
            var target=new RenderTexture(390,844,24);
            camera.targetTexture=target;
            yield return SceneManager.UnloadSceneAsync(original);
            var party=Fixture.Baseline().Units.Where(unit=>!unit.Enemy);
            // R03 geometry and initiative exercise three targets with the existing G1 enemy rules.
            var positions=new[]{new GridPosition(3,1),new GridPosition(2,0),new GridPosition(3,4),new GridPosition(3,0)};
            var units=party.Select((unit,index)=>new Unit(unit.Id,unit.Role,unit.MaxHp,unit.Attack,positions[index])).Concat(new[]{
                new Unit("X",Role.Enemy,10,4,new GridPosition(1,3)),
                new Unit("Y",Role.Enemy,12,5,new GridPosition(2,2)),
                new Unit("Z",Role.Enemy,18,6,new GridPosition(4,2))
            });
            view.ConfigureEncounter(new EncounterState(5,5,units,new[]{"B","Z","C","Y","A","X","D"}));
            try
            {
                host.SetActive(true);
                view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
                yield return null;
                Assert.That(GameObject.Find("Fixture Z").transform.Find("Fixture slime"),Is.Not.Null);
                Assert.That(view.Field.Find("HP Z/HP bar").gameObject.activeInHierarchy,Is.True);
                Press(view,"Start");
                view.RequestInstruction();
                var deadline=Time.realtimeSinceStartup+15;
                while(!view.InstructionOpen && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(view.InstructionOpen,Is.True);
                yield return null;yield return null;
                Assert.That(view.Field.Find("Discrete actor order/Order Z").gameObject.activeSelf,Is.True);
                Press(view,"DirectAllyC");
                Press(view,"DirectEnemyZ");
                Assert.That(view.Button("DirectEnemyZ").GetComponentInChildren<Text>().text,Does.Contain("HP 18"));
                Assert.That(view.Session.Replay.CorrectionsLeft,Is.EqualTo(1));
                Press(view,"DirectConfirm");
                Assert.That(view.Session.Replay.CorrectionsLeft,Is.Zero);
                Assert.That(view.Session.Replay.Serialize(),Does.Contain("target:C,Z"));
            }
            finally
            {
                camera.targetTexture=null;
                Object.Destroy(target);
            }
        }

        [UnityTest,Timeout(900000)]
        public IEnumerator ConsecutiveEncountersWithVoluntaryTargetInstruction()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var target=new RenderTexture(390,844,24);Camera.main.targetTexture=target;view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
            Press(view,"Start");
            var actionCaption=view.SafeRoot.GetComponentsInChildren<Text>(true).First(t=>t.name=="Events");
            Assert.That(actionCaption.text,Does.Contain("モモ"));
            Assert.That(actionCaption.text,Does.Contain("守護"));
            Assert.That(view.Session.Replay.State.Step,Is.EqualTo(1));
            yield return Capture("gauntlet-current-action");
            yield return WaitBattle(view,false);
            Assert.That(view.CrisisPaused,Is.False,"Encounter 1 keeps the frozen G1 flow");
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo("e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d"));
            Press(view,"Explore");
            yield return Encounter(view,1,Preference.ProtectAnchor);
            Press(view,"Explore");
            yield return Encounter(view,2,Preference.DirectAttack);
            Assert.That(view.SafeRoot.Find("Boundary").GetComponent<Text>().text,Does.Contain("3つの戦い"));
            var encounter=view.Session.EncounterIndex;var preference=view.Session.Preference;
            Press(view,"RetryEncounter");yield return null;yield return null;
            Assert.That(view.Session.EncounterIndex,Is.EqualTo(encounter),"Retry stays in the same encounter");
            Assert.That(view.Session.Preference,Is.EqualTo(preference));
            Assert.That(view.Session.Started,Is.False);Assert.That(view.Session.Replay.CorrectionsLeft,Is.EqualTo(1));
            Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(Fixture.Encounter(encounter).Serialize()));
            Assert.That(view.SafeRoot.Find("Instruction receipt panel").gameObject.activeSelf,Is.False,"Previous instruction receipt clears");
            yield return Capture("gauntlet-retry-ready");
            Press(view,"Start");yield return WaitBattle(view,false);
            Assert.That(view.Session.Replay.State.Outcome,Is.EqualTo(Outcome.Victory));
        }
    }
}
