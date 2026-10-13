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
    public class G1InteractionTests
    {
        private static readonly string Evidence=Path.GetFullPath("Logs/g1");
        private static void Click(G1View view,string id)
        {
            var button=view.Button(id);
            Assert.That(button.interactable,Is.True,id);
            var rect=button.GetComponent<RectTransform>();
            Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44),id+" touch height");
            var camera=button.GetComponentInParent<Canvas>().worldCamera;
            var pointer=new PointerEventData(EventSystem.current) {position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),id+" must be rendered and hit");
            Assert.That(hits[0].gameObject,Is.EqualTo(button.gameObject),id+" top hit");
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return null;
            var camera=Camera.main; var rt=camera.targetTexture;
            Assert.That(rt.width,Is.EqualTo(390)); Assert.That(rt.height,Is.EqualTo(844));
            yield return null; yield return null;
            var old=RenderTexture.active; RenderTexture.active=rt;
            var texture=new Texture2D(390,844,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,390,844),0,0);texture.Apply();
            var pixels=texture.GetPixels32();
            Assert.That(pixels.Select(p=>p.r+","+p.g+","+p.b).Distinct().Take(41).Count(),Is.GreaterThan(40),"Actual UI + field must render; blank is failure");
            Assert.That(pixels.Count(p=>p.r>150 && p.g>150 && p.b>150),Is.GreaterThan(100),"Readable text pixels required");
            Directory.CreateDirectory(Evidence);File.WriteAllBytes(Path.Combine(Evidence,name+".png"),texture.EncodeToPNG());
            RenderTexture.active=old;Object.Destroy(texture);
        }
        private static void CheckText()
        {
            Canvas.ForceUpdateCanvases();
            var unitLabels=Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Unit ") && t.gameObject.activeInHierarchy).ToArray();
            for(var i=0;i<unitLabels.Length;i++) for(var j=i+1;j<unitLabels.Length;j++)
            {
                var a=new Vector3[4];var b=new Vector3[4];unitLabels[i].rectTransform.GetWorldCorners(a);unitLabels[j].rectTransform.GetWorldCorners(b);
                Assert.That(a[2].x<=b[0].x || b[2].x<=a[0].x || a[2].y<=b[0].y || b[2].y<=a[0].y,Is.True,"Living role/HP labels overlap");
            }
            foreach(var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if(!text.gameObject.activeInHierarchy) continue;
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name+" height: "+text.text);
                var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                foreach(var c in corners)
                {var p=Camera.main.WorldToViewportPoint(c);Assert.That(p.x,Is.InRange(-.001f,1.001f),text.name+" screen width");}
            }
        }
        [UnityTest]
        public IEnumerator InputToBattleToAlternativeComparisonRendersOnlyCoreEvents()
        {
            yield return SceneManager.LoadSceneAsync("G1Battle");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<G1View>(); Assert.That(view,Is.Not.Null);
            var portrait=new RenderTexture(390,844,24);Camera.main.targetTexture=portrait;
            yield return null;yield return null;
            var baseline=view.Battle.State.Serialize();
            CheckText();yield return Capture("01-setup");
            Click(view,"Facts");yield return null;yield return null;CheckText();yield return Capture("02-rules");
            Click(view,"CloseFacts");yield return null;yield return null;
            Click(view,"Attack");yield return null;
            Assert.That(view.Battle.State.Serialize(),Is.EqualTo(baseline));
            view.Prediction.text="I want to compare how B acts.";
            Click(view,"Start");yield return null;
            Assert.That(view.RecordedPrediction,Is.EqualTo("I want to compare how B acts."));
            Assert.That(view.Prediction.interactable,Is.False);
            Assert.That(view.Button("Protect").interactable,Is.False);
            Click(view,"Next");yield return null;CheckText();yield return Capture("03-attack-events");
            LogAssert.Expect(LogType.Log,new Regex("^G1_RESULT "+view.Selected+" "));
            while(view.Battle.State.Outcome==Outcome.Active) {Click(view,"Next");yield return null;}
            CheckText();yield return Capture("04-attack-result");
            view.SaveTrace(Path.Combine(Evidence,"attack.trace")); var attackHash=view.Battle.Hash();
            Assert.That(view.ShownEvents.Select(e=>e.Serialize()),Is.EqualTo(view.Battle.Events.Select(e=>e.Serialize())));
            Click(view,"Facts");yield return null;yield return null;Click(view,"Trials");yield return null;yield return null;
            CheckText();yield return Capture("07-trial-predictions");
            Click(view,"CloseFacts");yield return null;yield return null;
            Click(view,"Reset");yield return null;Click(view,"Protect");yield return null;
            Assert.That(view.Battle.State.Serialize(),Is.EqualTo(baseline)); Assert.That(view.Prediction.text,Is.Empty);
            view.Prediction.text="I will observe the other setup.";
            Click(view,"Start");yield return null;
            yield return Capture("05-protect-events");
            Click(view,"Playback");yield return null;yield return null;Click(view,"Playback");yield return null;
            Assert.That(view.Playing,Is.False,"Pause stops adapter playback");
            var stopped=view.Battle.State.Serialize();yield return new WaitForSeconds(.6f);
            Assert.That(view.Battle.State.Serialize(),Is.EqualTo(stopped),"UI time cannot advance paused Core");
            LogAssert.Expect(LogType.Log,new Regex("^G1_RESULT "+view.Selected+" "));
            while(view.Battle.State.Outcome==Outcome.Active) {Click(view,"Next");yield return null;}
            CheckText();yield return Capture("06-comparison");
            Assert.That(view.Predictions[Preference.DirectAttack],Is.EqualTo("I want to compare how B acts."));
            Assert.That(view.Predictions[Preference.ProtectAnchor],Is.EqualTo("I will observe the other setup."));
            Assert.That(view.Results.Count,Is.EqualTo(2)); Assert.That(view.Battle.Hash(),Is.Not.EqualTo(attackHash));
            view.SaveTrace(Path.Combine(Evidence,"protect.trace"));
            File.WriteAllText(Path.Combine(Evidence,"results.txt"),string.Join("\n",view.Results.Values)+"\nattack sha256 "+attackHash+"\nprotect sha256 "+view.Battle.Hash());
            Assert.That(view.ShownEvents.Select(e=>e.Serialize()),Is.EqualTo(view.Battle.Events.Select(e=>e.Serialize())));
            Click(view,"Facts");yield return null;yield return null;Click(view,"Trials");yield return null;yield return null;
            CheckText();yield return Capture("07-trial-predictions");
            Click(view,"CloseFacts");yield return null;yield return null;
            Click(view,"Reset");yield return null;Click(view,"Start");yield return null;
            LogAssert.Expect(LogType.Log,new Regex("^G1_RESULT "+view.Selected+" "));
            while(view.Battle.State.Outcome==Outcome.Active) {Click(view,"Next");yield return null;}
            Assert.That(view.Battle.Hash(),Is.EqualTo(File.ReadAllText(Path.Combine(Evidence,"results.txt")).Split(new[]{"protect sha256 "},System.StringSplitOptions.None)[1]));
            Camera.main.targetTexture=null;Object.Destroy(portrait);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
