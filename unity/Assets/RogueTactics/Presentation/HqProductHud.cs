using System;
using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        public bool DeveloperMode { get; private set; }
        private RectTransform roster, orderStrip;
        private readonly Dictionary<string,Text> cardValues=new Dictionary<string,Text>();
        private readonly Dictionary<string,Image> cardHealth=new Dictionary<string,Image>();
        private readonly Dictionary<string,Image> orderFrames=new Dictionary<string,Image>();
        private readonly List<string> initiative=new List<string>();
        private string shownActor="", reaction="";
        private Text orderLabel;
        private bool reducedEffects;
        private static readonly string[] CardOrder={"A","D","C","B"};
        private static string HeroName(string id)=>id=="A"?"シロ":id=="D"?"ミャル":id=="C"?"ガル":id=="B"?"モモ":id=="X"?"苔の精":"花の精";
        public void SetDeveloperMode(bool enabled){DeveloperMode=enabled;ApplySafeArea(safeRect,screenW,screenH);Refresh();}
        private void BuildProductHud()
        {
            // Derive the regular fixture order from Core-authored Action events. No action plans/results are shown here.
            var probe=new Replay(Fixture.Baseline(),Preference.ProtectAnchor);
            for(var i=0;i<6;i++)
            {
                var step=probe.Advance();var actor=step.Events.First(e=>e.Kind==EventKind.Action).Actor;
                if(!initiative.Contains(actor))initiative.Add(actor);
            }
            orderStrip=Rect(Field,"Discrete actor order",8,8,374,49);
            var orderBackdrop=orderStrip.gameObject.AddComponent<Image>();orderBackdrop.color=new Color(.035f,.09f,.13f,.96f);orderBackdrop.raycastTarget=false;
            orderLabel=Label(orderStrip,"Order caption","行動順",0,0,53,20,12);
            foreach(var id in initiative)
            {
                var r=Rect(orderStrip,"Order "+id,0,17,57,30);var frame=r.gameObject.AddComponent<Image>();frame.color=new Color(.06f,.14f,.19f,.9f);frame.raycastTarget=false;orderFrames[id]=frame;
                if(id.Length==1 && id[0]>='A' && id[0]<='D'){var icon=Rect(r,"Actor portrait",0,0,24,28).gameObject.AddComponent<RawImage>();icon.texture=CharacterArt[id[0]-'A'];icon.uvRect=new Rect(.18f,.43f,.64f,.5f);icon.raycastTarget=false;}
                Label(r,"Actor",HeroName(id),24,3,31,23,10,TextAnchor.MiddleCenter);
            }
            roster=Rect(SafeRoot,"Companion portraits",12,0,366,83);
            foreach(var id in CardOrder)
            {
                var r=Rect(roster,"Companion "+id,0,0,86,83);var panel=r.gameObject.AddComponent<Image>();panel.color=new Color(.045f,.10f,.15f,.97f);panel.raycastTarget=false;
                var edge=r.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.65f,.71f,.64f,.6f);edge.effectDistance=new Vector2(1,-1);
                var portrait=Rect(r,"Approved portrait",8,2,70,48).gameObject.AddComponent<RawImage>();portrait.texture=CharacterArt[id[0]-'A'];portrait.uvRect=new Rect(.18f,.51f,.64f,.33f);portrait.raycastTarget=false;
                Label(r,"Name",HeroName(id),3,49,80,17,11,TextAnchor.MiddleCenter);
                cardValues[id]=Label(r,"Actual HP","",3,65,80,15,10,TextAnchor.MiddleCenter);
                var track=Rect(r,"Health track",6,79,74,2).gameObject.AddComponent<Image>();track.color=new Color(.12f,.22f,.25f);track.raycastTarget=false;
                var bar=Rect(r,"Actual health",6,79,74,2).gameObject.AddComponent<Image>();bar.color=Jade;bar.raycastTarget=false;cardHealth[id]=bar;
            }
            MakeButton(modal,"Effects","描画：通常",18,480,334,44,()=>{reducedEffects=!reducedEffects;if(AmbientOcclusion!=null)AmbientOcclusion.SetActive(!reducedEffects);if(runtimeProfile!=null)foreach(var effect in runtimeProfile.components)effect.active=!reducedEffects;var ambient=FindFirstObjectByType<HqAmbient>();if(ambient!=null)ambient.SetReduced(reducedEffects);buttons["Effects"].GetComponentInChildren<Text>().text=reducedEffects?"描画：軽量":"描画：通常";},13);
            MakeButton(modal,"Diagnostics","開発用コントロール",18,535,334,44,()=>{modalObject.SetActive(false);SetDeveloperMode(!DeveloperMode);},13);
        }
        private void LayoutProductHud(float w,float h,float fieldH)
        {
            roster.anchoredPosition=new Vector2(12,-(h-270));roster.sizeDelta=new Vector2(w-24,83);
            for(var i=0;i<CardOrder.Length;i++)
            {var r=cardValues[CardOrder[i]].transform.parent.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(i*(w-20)/4,0);r.sizeDelta=new Vector2((w-36)/4,83);}
            controls.anchoredPosition=new Vector2(0,-(h-180));
            orderStrip.sizeDelta=new Vector2(w-16,49);
            ResizeButton("Diagnostics",18,modal.rect.height-112,w-56);ResizeButton("Effects",18,modal.rect.height-166,w-56);
            detailsViewport.sizeDelta=new Vector2(w-56,modal.rect.height-229);
        }
        private void RefreshProductHud()
        {
            if(roster==null)return;
            var active=Session.Started;
            phase.text=active?(terminalSeen?"戦いを振り返る":"仲間の戦いを見守る"):"モモに託す方針を選ぶ";
            if(!DeveloperMode)
            {
                setup.gameObject.SetActive(!active);
                // The lower shelf is contextual; laboratory single stepping is available only through Record > developer controls.
                phase.gameObject.SetActive(false);intent.gameObject.SetActive(false);
                buttons["Next"].gameObject.SetActive(false);
                buttons["Play"].gameObject.SetActive(active && !terminalSeen);
                buttons["Reset"].gameObject.SetActive(terminalSeen);
                // Completed trials are compared before leaving the result shelf; setup owns input/start space.
                buttons["Compare"].gameObject.SetActive(terminalSeen);
                eventCaption.rectTransform.anchoredPosition=new Vector2(16,0);eventCaption.rectTransform.sizeDelta=new Vector2(SafeRoot.rect.width-32,42);eventCaption.fontSize=14;
                ResizeButton("Play",12,44,SafeRoot.rect.width-24);ResizeButton("Reset",12,44,SafeRoot.rect.width-24);
                buttons["Reset"].GetComponentInChildren<Text>().text="方針を変えて、もう一度";
                ResizeButton("Compare",12,98,SafeRoot.rect.width-24);
                buttons["Play"].GetComponentInChildren<Text>().text=Auto?"一時停止 / 観察する":"再開する";
                ResizeButton("Protect",12,0,(SafeRoot.rect.width-30)/2);ResizeButton("Attack",18+(SafeRoot.rect.width-30)/2,0,(SafeRoot.rect.width-30)/2);
                buttons["Protect"].GetComponentInChildren<Text>().text="シロを守る";buttons["Attack"].GetComponentInChildren<Text>().text="敵へ攻める";
                Prediction.GetComponent<RectTransform>().anchoredPosition=new Vector2(12,-51);
                ResizeButton("Start",12,101,SafeRoot.rect.width-24);buttons["Start"].GetComponentInChildren<Text>().text="仲間に託す";
            }
            else
            {
                phase.gameObject.SetActive(true);intent.gameObject.SetActive(true);buttons["Reset"].GetComponentInChildren<Text>().text="戻す";
                ResizeButton("Next",12,108,(SafeRoot.rect.width-118)/2);ResizeButton("Play",20+(SafeRoot.rect.width-118)/2,108,(SafeRoot.rect.width-118)/2);ResizeButton("Reset",SafeRoot.rect.width-90,108,78);ResizeButton("Compare",12,193,SafeRoot.rect.width-24);
                eventCaption.rectTransform.sizeDelta=new Vector2(SafeRoot.rect.width-32,54);intent.rectTransform.anchoredPosition=new Vector2(16,-59);buttons["Next"].gameObject.SetActive(true);buttons["Play"].gameObject.SetActive(true);buttons["Reset"].gameObject.SetActive(true);buttons["Compare"].gameObject.SetActive(true);
                // QA uses the existing full-size transport shelf; it deliberately covers the portrait row.
                controls.anchoredPosition=new Vector2(0,-(60+Field.rect.height-4));roster.gameObject.SetActive(false);
            }
            if(!DeveloperMode)roster.gameObject.SetActive(true);
            foreach(var id in CardOrder)
            {var u=Fixture.Baseline().Find(id);cardValues[id].text="HP "+hp[id]+" / "+u.MaxHp;cardHealth[id].rectTransform.sizeDelta=new Vector2(74f*hp[id]/u.MaxHp,2);}
            orderStrip.gameObject.SetActive(active && !terminalSeen);
            var start=initiative.IndexOf(shownActor);if(start<0)start=0;var slot=0;
            for(var i=0;i<initiative.Count;i++)
            {
                var id=initiative[(start+i)%initiative.Count];var f=orderFrames[id];f.gameObject.SetActive(!dead.Contains(id));if(dead.Contains(id))continue;
                f.rectTransform.anchoredPosition=new Vector2(slot*(Field.rect.width-24)/6,-19);slot++;
                f.color=id==shownActor?new Color(.50f,.36f,.12f,.97f):new Color(.04f,.12f,.18f,.92f);
            }
            orderLabel.text=reaction!=""?reaction:relationActor!="" && relationTarget!=""?HeroName(relationActor)+" → "+HeroName(relationTarget)+" / "+PlanName(relationKind):"行動順  •  "+(shownActor==""?"開始":HeroName(shownActor)+" 行動中");orderLabel.rectTransform.sizeDelta=new Vector2(340,19);
        }
    }
}
