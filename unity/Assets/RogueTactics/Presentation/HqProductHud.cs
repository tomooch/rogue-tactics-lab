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
        private string shownActor="", reaction="";
        private Text orderLabel;
        private bool reducedEffects;
        private bool policyEditing;
        private Text policyHeading;
        private Image policyBackdrop, eventBackdrop;
        private static readonly string[] CardOrder={"A","D","C","B"};
        private static string HeroName(string id)=>id=="A"?"シロ":id=="D"?"ミャル":id=="C"?"ガル":id=="B"?"モモ":id=="X"?"苔の精":"花の精";
        public void SetDeveloperMode(bool enabled){DeveloperMode=enabled;policyEditing=false;ApplySafeArea(safeRect,screenW,screenH);SetComparisonCamera(CameraPitch);}
        private void BuildProductHud()
        {
            buttons["Start"].transform.SetParent(controls,false);
            policyBackdrop=setup.gameObject.AddComponent<Image>();policyBackdrop.color=new Color(.035f,.08f,.11f,.96f);policyBackdrop.raycastTarget=false;
            policyHeading=Label(setup,"Policy owner","モモにどう動いてほしい？",18,8,270,30,15);
            MakeButton(setup,"ClosePolicy","閉じる",302,0,76,44,()=>{Prediction.DeactivateInputField();policyEditing=false;SetComparisonCamera(CameraPitch);},12);
            eventBackdrop=Rect(battleControls,"Event contrast",12,0,246,48).gameObject.AddComponent<Image>();eventBackdrop.color=new Color(.035f,.08f,.11f,.86f);eventBackdrop.raycastTarget=false;eventBackdrop.transform.SetAsFirstSibling();
            orderStrip=Rect(Field,"Discrete actor order",8,8,374,49);
            var orderBackdrop=orderStrip.gameObject.AddComponent<Image>();orderBackdrop.color=new Color(.035f,.09f,.13f,.96f);orderBackdrop.raycastTarget=false;
            orderLabel=Label(orderStrip,"Order caption","行動順",0,0,53,20,12);
            // One tile per actor; the order shown is read from the current encounter's Core state on refresh.
            foreach(var id in Fixture.Baseline().Initiative)
            {
                var r=Rect(orderStrip,"Order "+id,0,17,57,30);var frame=r.gameObject.AddComponent<Image>();frame.color=new Color(.06f,.14f,.19f,.9f);frame.raycastTarget=false;orderFrames[id]=frame;
                if(id.Length==1 && id[0]>='A' && id[0]<='D'){var icon=Rect(r,"Actor portrait",0,0,24,28).gameObject.AddComponent<RawImage>();var art=CharacterArt[id[0]-'A'];icon.texture=art;var uvWidth=.5f*24/28*art.height/art.width;icon.uvRect=new Rect(.5f-uvWidth/2,.43f,uvWidth,.5f);icon.raycastTarget=false;}
                Label(r,"Actor",HeroName(id),24,3,31,23,10,TextAnchor.MiddleCenter);
            }
            roster=Rect(SafeRoot,"Companion portraits",12,0,366,88);
            foreach(var id in CardOrder)
            {
                var r=Rect(roster,"Companion "+id,0,0,86,88);var panel=r.gameObject.AddComponent<Image>();panel.color=new Color(.045f,.10f,.15f,.90f);panel.raycastTarget=id=="B";
                var edge=r.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.65f,.71f,.64f,.6f);edge.effectDistance=new Vector2(1,-1);
                var portrait=Rect(r,"Approved portrait",7,4,68,47).gameObject.AddComponent<RawImage>();portrait.texture=CharacterArt[id[0]-'A'];portrait.uvRect=new Rect(.18f,.51f,.64f,.33f);portrait.raycastTarget=false;
                Label(r,"Name",id=="B"?"モモ・方針変更":HeroName(id),3,54,80,17,11,TextAnchor.MiddleCenter);
                cardValues[id]=Label(r,"Actual HP","",3,71,80,15,10,TextAnchor.MiddleCenter);
                var track=Rect(r,"Health track",6,86,74,2).gameObject.AddComponent<Image>();track.color=new Color(.12f,.22f,.25f);track.raycastTarget=false;
                var bar=Rect(r,"Actual health",6,86,74,2).gameObject.AddComponent<Image>();bar.color=Jade;bar.raycastTarget=false;cardHealth[id]=bar;
                if(id=="B")
                {
                    edge.effectColor=Gold;
                    var policy=r.gameObject.AddComponent<Button>();policy.targetGraphic=panel;buttons["Policy"]=policy;
                    policy.onClick.AddListener(()=>{if(Session.Started)return;policyEditing=!policyEditing;SetComparisonCamera(CameraPitch);});
                }
            }
            MakeButton(modal,"Effects","描画：通常",18,480,334,44,()=>{reducedEffects=!reducedEffects;if(AmbientOcclusion!=null)AmbientOcclusion.SetActive(!reducedEffects);if(runtimeProfile!=null)foreach(var effect in runtimeProfile.components)effect.active=!reducedEffects;var ambient=FindFirstObjectByType<HqAmbient>();if(ambient!=null)ambient.SetReduced(reducedEffects);buttons["Effects"].GetComponentInChildren<Text>().text=reducedEffects?"描画：軽量":"描画：通常";},13);
            MakeButton(modal,"Diagnostics","開発用コントロール",18,535,334,44,()=>{modalObject.SetActive(false);SetDeveloperMode(!DeveloperMode);},13);
        }
        private void LayoutProductHud(float w,float h,float fieldH)
        {
            roster.anchoredPosition=new Vector2(12,-(h-150));roster.sizeDelta=new Vector2(w-24,88);
            for(var i=0;i<CardOrder.Length;i++)
            {var r=cardValues[CardOrder[i]].transform.parent.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(i*(w-20)/4,0);r.sizeDelta=new Vector2((w-36)/4,88);}
            controls.anchoredPosition=new Vector2(0,-(h-54));
            SafeRoot.Find("Title").GetComponent<RectTransform>().sizeDelta=new Vector2(242,30);
            var boundary=SafeRoot.Find("Boundary").GetComponent<Text>();boundary.rectTransform.anchoredPosition=new Vector2(24,-44);boundary.rectTransform.sizeDelta=new Vector2(242,18);boundary.fontSize=11;
            orderStrip.sizeDelta=new Vector2(DeveloperMode?w-16:208,49);orderStrip.anchoredPosition=DeveloperMode?new Vector2(8,-8):(Vector2)Field.InverseTransformPoint(SafeRoot.TransformPoint(new Vector3(12,-76,0)));
            setup.sizeDelta=new Vector2(w,158);policyHeading.rectTransform.sizeDelta=new Vector2(w-108,30);ResizeButton("ClosePolicy",w-88,0,76);
            eventBackdrop.rectTransform.sizeDelta=new Vector2(w-140,48);
            ResizeButton("Diagnostics",18,modal.rect.height-112,w-56);ResizeButton("Effects",18,modal.rect.height-166,w-56);
            LayoutDetails();
            LayoutReferenceHud(w,h);
        }
        private void LayoutDetails()
        {
            var result=showingResults && !DeveloperMode;var w=SafeRoot.rect.width;var h=SafeRoot.rect.height;
            var height=result?Mathf.Min(h-100,Session.Trials.Count>1?440:300):Mathf.Min(628,h-90);
            modal.sizeDelta=new Vector2(w-20,height);modal.anchoredPosition=new Vector2(10,-(result?(h-height)/2:66));
            detailsViewport.sizeDelta=new Vector2(w-56,height-(result?126:229));
            ResizeButton("Close",18,height-58,w-56);
            buttons["Effects"].gameObject.SetActive(!result);buttons["Diagnostics"].gameObject.SetActive(!result);
            ResizeButton("Effects",18,height-166,w-56);ResizeButton("Diagnostics",18,height-112,w-56);
            if(referenceFooter!=null && !DeveloperMode)
            {
                buttons["Effects"].gameObject.SetActive(!result && referenceSettingsOpen);buttons["Diagnostics"].gameObject.SetActive(!result && referenceSettingsOpen);
                buttons["PresentationSettings"].gameObject.SetActive(!result && !referenceSettingsOpen);
                ResizeButton("PresentationSettings",134,height-112,w-172);
                var overflow=modalText.preferredHeight>detailsViewport.rect.height+1;
                buttons["RecordUp"].gameObject.SetActive(!referenceSettingsOpen && overflow);
                buttons["RecordDown"].gameObject.SetActive(!referenceSettingsOpen && overflow);
                ResizeButton("RecordUp",18,height-112,52);ResizeButton("RecordDown",76,height-112,52);
                if(!result && !referenceSettingsOpen)detailsViewport.sizeDelta=new Vector2(w-56,height-180);
            }
            LayoutAutonomousJournal();
        }
        private void RefreshProductHud()
        {
            if(roster==null)return;
            var active=Session.Started;
            buttons["Start"].gameObject.SetActive(!active);buttons["Policy"].interactable=!active;
            policyBackdrop.enabled=!DeveloperMode;policyHeading.gameObject.SetActive(!DeveloperMode);buttons["ClosePolicy"].gameObject.SetActive(!DeveloperMode);
            SafeRoot.Find("Boundary").GetComponent<Text>().text=active?(terminalSeen?"方針と結果を振り返る":"仲間が自分で動く戦いを見守る"):"モモのカードで方針と予想を決める";
            phase.text=active?(terminalSeen?"戦いを振り返る":"仲間の戦いを見守る"):"モモに託す方針を選ぶ";
            if(!DeveloperMode)
            {
                setup.gameObject.SetActive(!active && policyEditing);
                setup.anchoredPosition=new Vector2(0,264);battleControls.anchoredPosition=Vector2.zero;
                // The lower shelf is contextual; laboratory single stepping is available only through Record > developer controls.
                phase.gameObject.SetActive(false);intent.gameObject.SetActive(false);
                buttons["Next"].gameObject.SetActive(false);
                buttons["Play"].gameObject.SetActive(active && !terminalSeen);
                buttons["Reset"].gameObject.SetActive(terminalSeen);
                // Completed trials are compared before leaving the result shelf; setup owns input/start space.
                buttons["Compare"].gameObject.SetActive(terminalSeen);
                eventCaption.gameObject.SetActive(!terminalSeen);eventBackdrop.gameObject.SetActive(!terminalSeen);
                eventCaption.rectTransform.anchoredPosition=new Vector2(18,-4);eventCaption.rectTransform.sizeDelta=new Vector2(SafeRoot.rect.width-152,40);eventCaption.fontSize=12;eventCaption.text=captions.LastOrDefault()??"仲間の戦いが始まります";
                // Only describe the Action already being presented, never a later Core plan or result.
                if(Busy && displayed.Count>0 && displayed[displayed.Count-1].Kind==EventKind.Action)
                    eventCaption.text=HeroName(executingPlan.Actor)+(executingPlan.Target!=""?" → "+HeroName(executingPlan.Target):"")+"\n"+PlanName(executingPlan.Kind);
                ResizeButton("Play",SafeRoot.rect.width-116,0,104);ResizeButton("Reset",12,0,(SafeRoot.rect.width-30)/2);
                buttons["Reset"].GetComponentInChildren<Text>().text="もう一度試す";
                ResizeButton("Compare",18+(SafeRoot.rect.width-30)/2,0,(SafeRoot.rect.width-30)/2);buttons["Compare"].GetComponentInChildren<Text>().text="予想と結果を比べる";
                buttons["Play"].GetComponentInChildren<Text>().text=Auto?"一時停止":"再開する";
                ResizeButton("Protect",12,46,(SafeRoot.rect.width-30)/2);ResizeButton("Attack",18+(SafeRoot.rect.width-30)/2,46,(SafeRoot.rect.width-30)/2);
                buttons["Protect"].GetComponentInChildren<Text>().text="シロのそばで守る";buttons["Attack"].GetComponentInChildren<Text>().text="近くの敵を攻める";
                buttons["Protect"].GetComponentInChildren<Text>().fontSize=14;buttons["Attack"].GetComponentInChildren<Text>().fontSize=14;
                Prediction.GetComponent<RectTransform>().anchoredPosition=new Vector2(12,-102);Prediction.GetComponent<RectTransform>().sizeDelta=new Vector2(SafeRoot.rect.width-24,44);
                Prediction.textComponent.rectTransform.sizeDelta=new Vector2(SafeRoot.rect.width-44,40);Prediction.placeholder.GetComponent<RectTransform>().sizeDelta=new Vector2(SafeRoot.rect.width-44,40);Prediction.placeholder.GetComponent<Text>().text="戦いの予想を残す（任意）";
                ResizeButton("Start",12,0,SafeRoot.rect.width-24);var startLabel=buttons["Start"].GetComponentInChildren<Text>();startLabel.text="この方針で戦闘開始";startLabel.fontSize=17;startLabel.color=new Color(.075f,.12f,.13f);buttons["Start"].GetComponent<Image>().color=Gold;
                foreach(var id in new[]{"Play","Reset","Compare"})buttons[id].GetComponentInChildren<Text>().fontSize=14;
            }
            else
            {
                setup.gameObject.SetActive(!active);setup.anchoredPosition=new Vector2(0,-26);battleControls.anchoredPosition=new Vector2(0,-26);eventCaption.gameObject.SetActive(true);eventBackdrop.gameObject.SetActive(false);
                Prediction.GetComponent<RectTransform>().anchoredPosition=new Vector2(12,-54);
                phase.gameObject.SetActive(true);intent.gameObject.SetActive(true);buttons["Reset"].GetComponentInChildren<Text>().text="戻す";
                ResizeButton("Next",12,108,(SafeRoot.rect.width-118)/2);ResizeButton("Play",20+(SafeRoot.rect.width-118)/2,108,(SafeRoot.rect.width-118)/2);ResizeButton("Reset",SafeRoot.rect.width-90,108,78);ResizeButton("Compare",12,193,SafeRoot.rect.width-24);
                ResizeButton("Start",12,134,SafeRoot.rect.width-24);ResizeButton("Protect",12,0,(SafeRoot.rect.width-30)/2);ResizeButton("Attack",18+(SafeRoot.rect.width-30)/2,0,(SafeRoot.rect.width-30)/2);
                eventCaption.rectTransform.sizeDelta=new Vector2(SafeRoot.rect.width-32,54);intent.rectTransform.anchoredPosition=new Vector2(16,-59);buttons["Next"].gameObject.SetActive(true);buttons["Play"].gameObject.SetActive(true);buttons["Reset"].gameObject.SetActive(true);buttons["Compare"].gameObject.SetActive(true);
                // QA uses the existing full-size transport shelf; it deliberately covers the portrait row.
                controls.anchoredPosition=new Vector2(0,-(60+Field.rect.height-4));roster.gameObject.SetActive(false);
            }
            if(!DeveloperMode)roster.gameObject.SetActive(true);
            foreach(var id in CardOrder)
            {var u=Fixture.Baseline().Find(id);cardValues[id].text="HP "+hp[id]+" / "+u.MaxHp;cardHealth[id].rectTransform.sizeDelta=new Vector2(74f*hp[id]/u.MaxHp,2);}
            cardValues["B"].transform.parent.Find("Name").GetComponent<Text>().text=Session.Preference==Preference.ProtectAnchor?"モモ：守る":"モモ：攻める";
            orderStrip.gameObject.SetActive(active && !terminalSeen);
            var initiative=Session.Replay.State.Initiative;
            var start=initiative.ToList().IndexOf(shownActor);if(start<0)start=0;var slot=0;
            for(var i=0;i<initiative.Count;i++)
            {
                var id=initiative[(start+i)%initiative.Count];var f=orderFrames[id];f.gameObject.SetActive(!dead.Contains(id));if(dead.Contains(id))continue;
                f.rectTransform.anchoredPosition=new Vector2(slot*(Field.rect.width-24)/6,-19);slot++;
                var actorLabel=f.transform.Find("Actor").GetComponent<Text>();actorLabel.gameObject.SetActive(DeveloperMode || id=="X" || id=="Y");
                if(!DeveloperMode)
                {f.rectTransform.anchoredPosition=new Vector2((slot-1)*34,-19);f.rectTransform.sizeDelta=new Vector2(32,28);actorLabel.text=id=="X"?"苔":id=="Y"?"花":HeroName(id);actorLabel.rectTransform.anchoredPosition=Vector2.zero;actorLabel.rectTransform.sizeDelta=new Vector2(32,26);}
                f.color=id==shownActor?new Color(.50f,.36f,.12f,.97f):new Color(.04f,.12f,.18f,.92f);
            }
            orderLabel.text=reaction!=""?reaction:relationActor!="" && relationTarget!=""?HeroName(relationActor)+" → "+HeroName(relationTarget)+" / "+PlanName(relationKind):"行動順  •  "+(shownActor==""?"開始":HeroName(shownActor)+" 行動中");orderLabel.rectTransform.sizeDelta=new Vector2(340,19);
            if(!DeveloperMode){orderLabel.text="行動順";orderLabel.rectTransform.sizeDelta=new Vector2(208,19);}
            RefreshReferenceHud();
        }
    }
}
