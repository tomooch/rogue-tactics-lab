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
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return null;
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
            {var at=GameObject.Find("Fixture "+u.Id).transform.position;Assert.That(Vector3.Distance(at,new Vector3((u.Position.X+origin.X)*2.5f,0,(u.Position.Y+origin.Y)*3.125f)),Is.LessThan(.01f),u.Id+" stands on its room cell");}
            view.Select(start);yield return Capture("gauntlet-"+(index+1)+"-setup");
            Press(view,"Start");yield return WaitBattle(view,true);
            Assert.That(view.CrisisPaused,Is.True,"Auto play stops when an ally is newly threatened");
            Assert.That(view.CrisisAllies,Does.Contain("A"));
            var intents=Object.FindObjectsByType<MobileRouteGraphic>(FindObjectsSortMode.None).Count(g=>g.name.StartsWith("Round intent") && g.gameObject.activeInHierarchy);
            Assert.That(intents,Is.GreaterThanOrEqualTo(3),"A round of present intents is visible while stopped");
            // The initiative strip follows this encounter's Core order (it once kept the frozen G1 order).
            var core=view.Session.Replay.State.Initiative.Where(id=>view.Session.Replay.State.Find(id).Alive).ToList();
            var shown=view.Field.Find("Discrete actor order").Cast<Transform>().Where(t=>t.name.StartsWith("Order ") && t.name.Length==7 && t.gameObject.activeSelf)
                .OrderBy(t=>t.GetComponent<RectTransform>().anchoredPosition.x).Select(t=>t.name.Substring(6)).ToList();
            var first=core.IndexOf(shown[0]);
            Assert.That(shown,Is.EqualTo(core.Skip(first).Concat(core.Take(first)).ToList()),"Initiative strip shows the encounter's own Core order");
            yield return Capture("gauntlet-"+(index+1)+"-crisis");
            Press(view,"Correct");Assert.That(view.Session.Replay.CorrectionsLeft,Is.EqualTo(0));
            yield return WaitBattle(view,false);
            Assert.That(view.Busy,Is.False);Assert.That(view.Session.Replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(view.Session.Replay.State.Units.Count(u=>!u.Enemy && u.Alive),Is.EqualTo(4),"The right correction at the crisis saves everyone");
            yield return Capture("gauntlet-"+(index+1)+"-end");
        }
        [UnityTest,Timeout(900000)]
        public IEnumerator ConsecutiveEncountersOnTheSameFloorWithCrisisCorrection()
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
