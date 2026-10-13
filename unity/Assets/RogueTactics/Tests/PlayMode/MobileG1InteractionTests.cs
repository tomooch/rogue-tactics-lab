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
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return null;
            var rt=Camera.main.targetTexture;var old=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(390,844,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,390,844),0,0);texture.Apply();
            Assert.That(texture.GetPixels32().Count(p=>p.r>150 && p.g>150 && p.b>150),Is.GreaterThan(300),"Visible Japanese HUD and approved art");
            if(name=="02-mobile-rules" || name=="07-mobile-comparison")
                Assert.That(texture.GetPixels(25,250,335,400).Count(p=>p.r>.45f && p.g>.45f && p.b>.45f),Is.GreaterThan(1000),"Actual modal body must be visible through stencil mask");
            Directory.CreateDirectory(Evidence);File.WriteAllBytes(Path.Combine(Evidence,name+".png"),texture.EncodeToPNG());RenderTexture.active=old;Object.Destroy(texture);
        }
        [UnityTest]
        public IEnumerator JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();Assert.That(view.HasAllArt,Is.True,"Approved private originals are required for V0");
            var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;yield return null;yield return null;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;yield return null;
            CheckLayout(view);yield return Capture("01-mobile-setup-safearea");
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
                LogAssert.Expect(LogType.Log,new Regex("^MOBILE_G1_RESULT "+preference+" "));
                Click(view,"Start");while(view.Busy)yield return null;
                CheckLayout(view);yield return Capture(preference==Preference.ProtectAnchor?"03-protect":"05-attack");
                Click(view,"Next");while(view.Busy)yield return null;
                yield return Capture(preference==Preference.ProtectAnchor?"04-guard-damage-counter":"06-damage-counter");
                Click(view,"Play");yield return null;yield return null;Click(view,"Play");while(view.Busy)yield return null;
                Assert.That(view.Paused,Is.True);var stopped=view.Session.Replay.State.Serialize();yield return new WaitForSecondsRealtime(.6f);
                Assert.That(view.Session.Replay.State.Serialize(),Is.EqualTo(stopped));
                Click(view,"Play");while(view.Busy || view.Session.Replay.State.Outcome==Outcome.Active)yield return null;
                Assert.That(view.Session.Replay.Hash(),Is.EqualTo(preference==Preference.ProtectAnchor?"e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d":"85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e"));
                Assert.That(view.DisplayedEvents.Select(e=>e.Serialize()),Is.EqualTo(view.Session.Replay.Events.Select(e=>e.Serialize())));
                CheckLayout(view);Click(view,"Reset");yield return null;yield return null;
            }
            Assert.That(view.Session.Trials.Count,Is.EqualTo(2));Click(view,"Compare");yield return null;yield return null;CheckLayout(view);yield return Capture("07-mobile-comparison");
            Click(view,"Close");yield return null;yield return null;CheckLayout(view);
            Camera.main.targetTexture=null;Object.Destroy(rt);LogAssert.NoUnexpectedReceived();
        }
    }
}
