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
            yield return Capture("instruction-"+index+"-confirm");
            Press(view,"DirectConfirm");Assert.That(view.Session.Replay.CorrectionsLeft,Is.EqualTo(0));
            yield return WaitBattle(view,false);
            Assert.That(view.Busy,Is.False);Assert.That(view.Session.Replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(view.CrisisPaused,Is.False,"No forced crisis interruption");
            AssertMapClearOfCast(view,"encounter "+(index+1));yield return Capture("gauntlet-"+(index+1)+"-end");
        }
        [UnityTest,Timeout(900000)]
        public IEnumerator ConsecutiveEncountersWithVoluntaryTargetInstruction()
        {
            yield return SceneManager.LoadSceneAsync("HqExploration");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var target=new RenderTexture(390,844,24);Camera.main.targetTexture=target;view.SetTestSafeArea(new Rect(0,34,390,763),390,844);
            Press(view,"Start");yield return WaitBattle(view,false);
            Assert.That(view.CrisisPaused,Is.False,"Encounter 1 keeps the frozen G1 flow");
            Assert.That(view.Session.Replay.Hash(),Is.EqualTo("e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d"));
            Press(view,"Explore");
            yield return Encounter(view,1,Preference.ProtectAnchor);
            Press(view,"Explore");
            yield return Encounter(view,2,Preference.DirectAttack);
            Assert.That(view.SafeRoot.Find("Boundary").GetComponent<Text>().text,Does.Contain("3つの戦い"));
        }
    }
}
