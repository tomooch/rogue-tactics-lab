using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        // Display-only feedback. Numbers float from the queued Core event that caused them; a pale lag segment
        // shows health just lost and drains after it. The real bars keep their exact Core widths throughout.
        private sealed class EventNumber{public Text Label;public Vector2 From;public float Age;}
        private readonly List<EventNumber> eventNumbers=new List<EventNumber>();
        private readonly Dictionary<Image,Image> healthLag=new Dictionary<Image,Image>();
        private readonly Dictionary<Image,float> lagHold=new Dictionary<Image,float>();

        private void PopEventNumber(BattleEvent e)
        {
            if(e.Amount<=0 || (e.Kind!=EventKind.Damage && e.Kind!=EventKind.Counter && e.Kind!=EventKind.Heal))return;
            if(!bodies.TryGetValue(e.Target,out var renderer) || !renderer.gameObject.activeInHierarchy)return;
            var body=ProjectedBody(renderer);var heal=e.Kind==EventKind.Heal;
            var label=Label(Field,"Event number",(heal?"+":"-")+e.Amount,0,0,64,30,heal?19:22,TextAnchor.MiddleCenter);
            label.fontStyle=FontStyle.Bold;label.raycastTarget=false;
            label.color=heal?new Color(.62f,1,.66f):e.Kind==EventKind.Counter?new Color(1,.80f,.42f):new Color(1,.97f,.90f);
            var outline=label.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(1.6f,-1.6f);
            outline.effectColor=heal?new Color(.02f,.25f,.08f,.95f):new Color(.42f,.04f,.03f,.95f);
            eventNumbers.Add(new EventNumber{Label=label,From=new Vector2(body.center.x-32,body.yMax-body.height*.18f)});
        }

        private void AnimateJuice()
        {
            var dt=Paused?0:Time.unscaledDeltaTime;
            for(var i=eventNumbers.Count-1;i>=0;i--)
            {
                var n=eventNumbers[i];n.Age+=dt;var t=n.Age/.95f;
                if(t>=1 || n.Label==null){if(n.Label!=null)Destroy(n.Label.gameObject);eventNumbers.RemoveAt(i);continue;}
                // Pop in larger, settle, rise and fade.
                var pop=t<.12f?Mathf.Lerp(.3f,1.35f,t/.12f):Mathf.Lerp(1.35f,1,Mathf.Clamp01((t-.12f)/.14f));
                n.Label.rectTransform.anchoredPosition=n.From+Vector2.up*(Mathf.Sin(Mathf.Min(1,t*1.8f)*Mathf.PI*.5f)*28);
                n.Label.rectTransform.localScale=Vector3.one*pop;
                var c=n.Label.color;c.a=t>.62f?1-(t-.62f)/.38f:1;n.Label.color=c;
            }
            foreach(var bar in cardHealth.Values.Concat(hpBars.Values))Lag(bar);
            YieldMapToCast();
            // Later intents flow toward their targets; the next actor's solid arc stays still.
            foreach(var g in roundIntents)if(g.Dashed && g.gameObject.activeInHierarchy){g.DashPhase=Time.unscaledTime*18;g.SetVerticesDirty();}
        }
        // The lower-right map turns see-through while any actor's body or foot HP lies under it, so it never hides the cast.
        public float KnownMapAlpha=>floorMini!=null && floorMini.transform.parent.TryGetComponent<CanvasGroup>(out var group)?group.alpha:1;
        private void YieldMapToCast()
        {
            if(floorMini==null || DeveloperMode)return;var map=(RectTransform)floorMini.transform.parent;if(!map.gameObject.activeInHierarchy)return;
            if(!map.TryGetComponent<CanvasGroup>(out var group))group=map.gameObject.AddComponent<CanvasGroup>();
            var corners=new Vector3[4];map.GetWorldCorners(corners);var a=(Vector2)Field.InverseTransformPoint(corners[0]);var b=(Vector2)Field.InverseTransformPoint(corners[2]);
            var area=UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
            var covered=bodyRects.Values.Any(body=>body.Overlaps(area)) || badges.Values.Any(badge=>badge.gameObject.activeInHierarchy && Overlap(badge.GetComponent<RectTransform>(),area));
            group.alpha=Mathf.MoveTowards(group.alpha,covered?.2f:1,Time.unscaledDeltaTime*5);
        }
        private bool Overlap(RectTransform r,UnityEngine.Rect area)
        {var corners=new Vector3[4];r.GetWorldCorners(corners);var a=(Vector2)Field.InverseTransformPoint(corners[0]);var b=(Vector2)Field.InverseTransformPoint(corners[2]);return UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y)).Overlaps(area);}
        private void Lag(Image bar)
        {
            if(bar==null)return;
            if(!healthLag.TryGetValue(bar,out var lag))
            {
                lag=Rect(bar.transform.parent,"Health lag",0,0,1,1).gameObject.AddComponent<Image>();lag.raycastTarget=false;lag.color=new Color(1,.90f,.72f,.92f);
                lag.transform.SetSiblingIndex(bar.transform.GetSiblingIndex());healthLag[bar]=lag;lag.rectTransform.sizeDelta=bar.rectTransform.sizeDelta;
            }
            var target=bar.rectTransform;var r=lag.rectTransform;r.anchoredPosition=target.anchoredPosition;
            var width=target.sizeDelta.x;var shown=r.sizeDelta.x;
            lag.gameObject.SetActive(bar.gameObject.activeSelf);
            if(width>=shown || !bar.gameObject.activeInHierarchy){shown=width;lagHold[bar]=.35f;}
            else
            {
                // Hold the lost segment briefly so the eye catches it, then drain.
                var hold=lagHold.TryGetValue(bar,out var h)?h:.35f;
                if(hold>0)lagHold[bar]=hold-Time.unscaledDeltaTime;
                else shown=Mathf.MoveTowards(shown,width,Time.unscaledDeltaTime*Mathf.Max(14,(shown-width)*3));
            }
            r.sizeDelta=new Vector2(shown,target.sizeDelta.y);
        }
    }
    // A short press dip for every button; it only scales the visual and never moves the hit rect's layout.
    public sealed class HudPress:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        private bool held;
        public void OnPointerDown(PointerEventData e){held=true;}
        public void OnPointerUp(PointerEventData e){held=false;}
        public void OnPointerExit(PointerEventData e){held=false;}
        private void OnDisable(){held=false;transform.localScale=Vector3.one;}
        private void Update()
        {
            var goal=held?.95f:1;var s=transform.localScale.x;
            if(Mathf.Abs(s-goal)>.0005f)transform.localScale=Vector3.one*Mathf.MoveTowards(s,goal,Time.unscaledDeltaTime*1.2f);
        }
    }
}
