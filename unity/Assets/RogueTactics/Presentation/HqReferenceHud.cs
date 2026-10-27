using System.Linq;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private RectTransform referenceFooter;
        private Text journeyStatus, discoveredStatus;
        private HudPanelGraphic[] companionFrames;
        private FloorMapGraphic expandedKnownMap;
        private RectTransform referenceModalCurtain;
        private bool referenceSettingsOpen;
        private static readonly Color[] CompanionColors={new Color(.32f,.66f,1),new Color(.43f,.80f,.48f),new Color(1,.77f,.32f),new Color(1,.46f,.63f)};
        private static readonly string[] CoreRoles={"反撃","回復","連携","方針"};

        private HudPanelGraphic Frame(RectTransform root,Color fill,Color border,float radius=10)
        {
            var r=Rect(root,"Illustrated frame",0,0,root.rect.width,root.rect.height);
            r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.SetAsFirstSibling();
            var g=r.gameObject.AddComponent<HudPanelGraphic>();g.Fill=fill;g.Border=border;g.Radius=radius;g.raycastTarget=false;return g;
        }
        private HudGlyphGraphic Glyph(Transform parent,string name,HudSymbol symbol,float x,float y,float size,Color tint,bool gem=false,bool round=false)
        {var g=Rect(parent,name,x,y,size,size).gameObject.AddComponent<HudGlyphGraphic>();g.Symbol=symbol;g.color=tint;g.Gem=gem;g.Round=round;g.raycastTarget=false;return g;}
        private void StyleReferenceButton(string id,HudSymbol symbol,Color accent)
        {
            var b=buttons[id];b.GetComponent<Image>().color=Color.clear;
            b.targetGraphic=Frame(b.GetComponent<RectTransform>(),new Color(.035f,.085f,.13f,.96f),accent,10);
            Glyph(b.transform,"Action symbol",symbol,13,14,20,accent,true);
        }
        // The one forward action on screen reads as a warm, filled call to action.
        private void FrameButton(string id,Color accent)
        {var b=buttons[id];b.GetComponent<Image>().color=Color.clear;b.targetGraphic=Frame(b.GetComponent<RectTransform>(),new Color(.035f,.085f,.13f,.96f),accent,10);}
        // Sheets share one language: framed body, gem-marked title over a thin gold rule, framed controls.
        private void BuildReferenceSheets()
        {
            Glyph(modal,"Heading gem",HudSymbol.Book,22,18,18,new Color(.62f,.76f,.92f),true);
            var rule=Rect(modal,"Heading rule",18,50,334,1.5f);rule.anchorMax=new Vector2(1,1);rule.offsetMin=new Vector2(18,-51.5f);rule.offsetMax=new Vector2(-18,-50);
            var ruleImage=rule.gameObject.AddComponent<Image>();ruleImage.color=new Color(Gold.r,Gold.g,Gold.b,.55f);ruleImage.raycastTarget=false;
            StyleReferenceButton("Close",HudSymbol.Close,new Color(.70f,.80f,.92f));
            foreach(var id in new[]{"ClosePolicy","RecordUp","RecordDown","Effects","Diagnostics"})FrameButton(id,new Color(.55f,.68f,.80f));
            var input=Prediction.GetComponent<Image>();input.color=Color.clear;
            Prediction.targetGraphic=Frame(Prediction.GetComponent<RectTransform>(),new Color(.02f,.05f,.09f,.95f),new Color(.45f,.58f,.72f,.8f),10);
            eventMark=Glyph(battleControls,"Event mark",HudSymbol.Steps,0,0,18,Jade,true,true);eventMark.gameObject.SetActive(false);
        }
        private HudGlyphGraphic eventMark;
        // The lower shelf names the latest observed event with its kind's gem; the caption text is unchanged.
        private void RefreshEventShelf()
        {
            if(eventMark==null)return;
            var on=AutonomousProduct && eventBackdrop.gameObject.activeInHierarchy && eventCaption.gameObject.activeInHierarchy && !string.IsNullOrEmpty(eventCaption.text);
            eventMark.gameObject.SetActive(on);if(!on)return;
            var back=eventBackdrop.rectTransform;eventMark.rectTransform.anchoredPosition=back.anchoredPosition+new Vector2(15,-15);
            var last=IsExploring?null:DisplayedEvents.LastOrDefault(e=>e.Kind!=EventKind.End);
            var kind=last==null?EventKind.Move:last.Kind;
            var symbol=IsExploring || kind==EventKind.Move?HudSymbol.Steps:kind==EventKind.Heal?HudSymbol.Heal:kind==EventKind.Protect?HudSymbol.Shield:kind==EventKind.Counter?HudSymbol.Counter:kind==EventKind.Death?HudSymbol.Alert:HudSymbol.Swords;
            var tint=IsExploring?Jade:kind==EventKind.Heal?new Color(.40f,.86f,.50f):kind==EventKind.Protect?new Color(.42f,.78f,1):kind==EventKind.Death?new Color(1,.30f,.28f):kind==EventKind.Move?Jade:new Color(1,.58f,.40f);
            if(eventMark.Symbol!=symbol || eventMark.color!=tint){eventMark.Symbol=symbol;eventMark.color=tint;eventMark.SetVerticesDirty();}
            var caption=eventCaption.rectTransform;caption.anchoredPosition=new Vector2(back.anchoredPosition.x+44,caption.anchoredPosition.y);
            caption.sizeDelta=new Vector2(back.sizeDelta.x-50,caption.sizeDelta.y);
        }
        private void PrimaryFill(string id)
        {var panel=buttons[id].GetComponentInChildren<HudPanelGraphic>();panel.Fill=new Color(.36f,.23f,.06f,.97f);panel.Border=new Color(1,.84f,.46f);panel.SetVerticesDirty();}
        private void BuildReferenceHud()
        {
            if(!HqWorld)return;
            var plate=SafeRoot.Find("Title plate").GetComponent<Image>();plate.color=Color.clear;
            Frame(plate.rectTransform,new Color(.045f,.085f,.14f,.90f),new Color(.70f,.76f,.84f,.8f),24);
            Glyph(plate.transform,"Ruin emblem",HudSymbol.Star,10,10,28,Gold,true,true);
            var title=SafeRoot.Find("Title").GetComponent<Text>();title.fontSize=20;title.fontStyle=FontStyle.Bold;
            var titleShadow=title.gameObject.AddComponent<Shadow>();titleShadow.effectColor=new Color(.01f,.02f,.05f,.9f);titleShadow.effectDistance=new Vector2(1,-1.5f);
            var boundary=SafeRoot.Find("Boundary").GetComponent<Text>();boundary.color=new Color(.90f,.94f,1);boundary.fontSize=10;
            var instruction=Rect(SafeRoot,"Context instruction",12,56,226,22);Frame(instruction,new Color(.035f,.075f,.13f,.82f),new Color(.55f,.68f,.80f,.5f),10);instruction.SetSiblingIndex(boundary.transform.GetSiblingIndex());
            var status=Rect(SafeRoot,"Observed journey",0,70,60,80);
            Frame(status,new Color(.035f,.085f,.14f,.83f),new Color(.70f,.76f,.84f,.6f),12);
            Glyph(status,"Journey emblem",HudSymbol.Compass,17,8,26,Gold);
            journeyStatus=Label(status,"Current phase","準備",4,35,52,18,12,TextAnchor.MiddleCenter);
            discoveredStatus=Label(status,"Discovered rooms","",3,55,54,20,10,TextAnchor.MiddleCenter);
            referenceFooter=Rect(canvas.transform,"Foreground readability gradient",0,0,390,230);
            referenceFooter.anchorMin=referenceFooter.anchorMax=Vector2.zero;referenceFooter.pivot=Vector2.zero;referenceFooter.anchoredPosition=Vector2.zero;
            var gradient=referenceFooter.gameObject.AddComponent<HudGradientGraphic>();gradient.raycastTarget=false;
            referenceFooter.SetAsFirstSibling();
            companionFrames=new HudPanelGraphic[4];
            for(var i=0;i<4;i++)
            {
                var id=CardOrder[i];var r=cardValues[id].transform.parent.GetComponent<RectTransform>();
                r.GetComponent<Image>().color=Color.clear;var outline=r.GetComponent<Outline>();outline.enabled=false;
                companionFrames[i]=Frame(r,new Color(.035f,.065f,.12f,1),CompanionColors[i]);
                companionFrames[i].TopFill=Color.Lerp(new Color(.035f,.065f,.12f,1),CompanionColors[i],.24f);
                companionFrames[i].PortraitSurface=true;
                var portrait=r.Find("Approved portrait").GetComponent<RawImage>();portrait.uvRect=new Rect(.15f,.50f,.70f,.37f);
                var fade=Rect(r,"Portrait name fade",4,62,78,35).gameObject.AddComponent<HudPortraitFadeGraphic>();fade.raycastTarget=false;
                r.Find("Name").SetAsLastSibling();
                var role=Rect(r,"Core role",0,0,18,18);
                Glyph(role,"Role symbol",i==0?HudSymbol.Counter:i==1?HudSymbol.Heal:i==2?HudSymbol.Swords:HudSymbol.Shield,0,0,18,CompanionColors[i],true);
                var nameShadow=r.Find("Name").gameObject.AddComponent<Shadow>();nameShadow.effectColor=new Color(.01f,.02f,.05f,.9f);nameShadow.effectDistance=new Vector2(1,-1);
                Label(r,"Role meaning",CoreRoles[i],2,82,80,16,9,TextAnchor.MiddleCenter).color=Color.Lerp(CompanionColors[i],Color.white,.28f);
                cardHealth[id].color=new Color(.34f,.94f,.49f);r.Find("Health track").GetComponent<Image>().color=new Color(.02f,.05f,.09f,.95f);
                var gloss=Rect(cardHealth[id].transform,"Health gloss",0,0,1,1);gloss.anchorMin=new Vector2(0,.5f);gloss.anchorMax=Vector2.one;gloss.offsetMin=gloss.offsetMax=Vector2.zero;
                var shine=gloss.gameObject.AddComponent<Image>();shine.color=new Color(1,1,1,.30f);shine.raycastTarget=false;
                if(id=="B")
                {
                    var policyRow=Rect(r,"Policy row",4,97,78,20);
                    buttons["Policy"].targetGraphic=Frame(policyRow,new Color(.16f,.075f,.13f,1),new Color(.62f,.30f,.43f),6);
                    policyRow.SetSiblingIndex(r.Find("Role meaning").GetSiblingIndex());
                    Glyph(r,"Policy disclosure",HudSymbol.Chevron,68,99,12,CompanionColors[i]);
                }
            }
            // Foot health: brighter fills with a top gloss, same geometry as before.
            foreach(var entry in hpBars)
            {
                entry.Value.color=Fixture.Baseline().Find(entry.Key).Enemy?new Color(1,.42f,.38f):new Color(.36f,.95f,.52f);
                var gloss=Rect(entry.Value.transform,"HP gloss",0,0,1,1);gloss.anchorMin=new Vector2(0,.5f);gloss.anchorMax=Vector2.one;gloss.offsetMin=gloss.offsetMax=Vector2.zero;
                var shine=gloss.gameObject.AddComponent<Image>();shine.color=new Color(1,1,1,.38f);shine.raycastTarget=false;
            }
            // Initiative tiles: Core order, ally/enemy rims, the next actor in gold.
            orderStrip.GetComponent<Image>().color=Color.clear;
            Frame(orderStrip,new Color(.035f,.075f,.13f,.82f),new Color(.55f,.68f,.80f,.45f),10);
            foreach(var tile in orderFrames.Values){tile.color=Color.clear;Frame(tile.rectTransform,new Color(.05f,.10f,.16f,.95f),new Color(.40f,.50f,.60f),6);}
            StyleReferenceButton("Details",HudSymbol.Book,new Color(.70f,.80f,.94f));
            StyleReferenceButton("Start",HudSymbol.Play,Gold);PrimaryFill("Start");StyleReferenceButton("Play",HudSymbol.Pause,Gold);
            StyleReferenceButton("Reset",HudSymbol.Reset,new Color(.62f,.74f,.84f));StyleReferenceButton("Compare",HudSymbol.Compare,Gold);
            StyleReferenceButton("Protect",HudSymbol.Shield,new Color(.48f,.83f,1));StyleReferenceButton("Attack",HudSymbol.Swords,new Color(1,.60f,.48f));
            MakeButton(controls,"PolicyShortcut","モモの方針",12,0,160,48,()=>{if(!Session.Started){policyEditing=true;SetComparisonCamera(CameraPitch);}},14);
            StyleReferenceButton("PolicyShortcut",HudSymbol.Shield,CompanionColors[3]);
            policyBackdrop.color=Color.clear;Frame(setup,new Color(.035f,.07f,.12f,.98f),new Color(.76f,.68f,.58f),14);
            eventBackdrop.color=Color.clear;
            Frame(eventBackdrop.rectTransform,new Color(.035f,.085f,.13f,.86f),new Color(.43f,.60f,.72f,.6f),10);
            modal.GetComponent<Image>().color=Color.clear;Frame(modal,new Color(.035f,.065f,.11f,1),new Color(.60f,.72f,.84f),14);
            referenceModalCurtain=Rect(SafeRoot,"Modal input curtain",0,0,390,844);
            referenceModalCurtain.anchorMin=Vector2.zero;referenceModalCurtain.anchorMax=Vector2.one;referenceModalCurtain.offsetMin=referenceModalCurtain.offsetMax=Vector2.zero;
            referenceModalCurtain.gameObject.AddComponent<Image>().color=new Color(.015f,.035f,.065f,.56f);referenceModalCurtain.gameObject.SetActive(false);
            MakeButton(modal,"PresentationSettings","表示・検証設定",18,516,334,48,OpenReferenceSettings,12);
            StyleReferenceButton("PresentationSettings",HudSymbol.Book,new Color(.60f,.73f,.84f));
            MakeButton(modal,"RecordUp","上へ",18,516,52,48,()=>PageRecord(-1),12);
            MakeButton(modal,"RecordDown","下へ",76,516,52,48,()=>PageRecord(1),12);
            BuildReferenceTypography();BuildAutonomousHud();BuildReferenceSheets();
        }
        private void PageRecord(int direction)
        {
            Canvas.ForceUpdateCanvases();
            var scroll=detailsViewport.GetComponent<ScrollRect>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(modalText.rectTransform);
            Canvas.ForceUpdateCanvases();
            var overflow=Mathf.Max(0,scroll.content.rect.height-detailsViewport.rect.height);
            if(overflow<=0)return;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-direction*detailsViewport.rect.height*.8f/overflow);
        }
        private void LayoutReferenceHud(float w,float h)
        {
            if(referenceFooter==null || DeveloperMode)return;
            var plate=SafeRoot.Find("Title plate").GetComponent<RectTransform>();plate.anchoredPosition=new Vector2(12,-8);plate.sizeDelta=new Vector2(Mathf.Min(w-86,226),44);
            var title=SafeRoot.Find("Title").GetComponent<RectTransform>();title.anchoredPosition=new Vector2(58,-12);title.sizeDelta=new Vector2(176,34);
            var boundary=SafeRoot.Find("Boundary").GetComponent<RectTransform>();boundary.anchoredPosition=new Vector2(20,-57);boundary.sizeDelta=new Vector2(210,20);
            SafeRoot.Find("Observed journey").GetComponent<RectTransform>().anchoredPosition=new Vector2(w-72,-70);
            referenceFooter.anchoredPosition=Vector2.zero;referenceFooter.sizeDelta=new Vector2(canvas.GetComponent<RectTransform>().rect.width,230);
            roster.anchoredPosition=new Vector2(9,-(h-192));roster.sizeDelta=new Vector2(w-18,124);
            var width=(w-36)/4;
            for(var i=0;i<4;i++)
            {
                var id=CardOrder[i];var r=cardValues[id].transform.parent.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(i*(width+6),0);r.sizeDelta=new Vector2(width,124);
                var art=r.Find("Approved portrait").GetComponent<RectTransform>();art.anchoredPosition=new Vector2(5,-5);art.sizeDelta=new Vector2(width-10,60);
                var role=r.Find("Core role").GetComponent<RectTransform>();role.anchoredPosition=new Vector2(width-28,-3);
                var name=r.Find("Name").GetComponent<Text>();name.rectTransform.anchoredPosition=new Vector2(2,-65);name.rectTransform.sizeDelta=new Vector2(width-4,18);name.fontSize=12;
                var meaning=r.Find("Role meaning").GetComponent<RectTransform>();meaning.sizeDelta=new Vector2(width-4,16);
                cardValues[id].rectTransform.anchoredPosition=new Vector2(4,-105);cardValues[id].rectTransform.sizeDelta=new Vector2(width-8,18);cardValues[id].fontSize=10;
                var track=r.Find("Health track").GetComponent<RectTransform>();track.anchoredPosition=new Vector2(7,-99);track.sizeDelta=new Vector2(width-14,5);
                cardHealth[id].rectTransform.anchoredPosition=new Vector2(7,-99);
            }
            controls.anchoredPosition=new Vector2(0,-(h-58));
            LayoutReferenceTypography(w,h);
        }
        private void PositionReferenceAction(string id,float x,float y,float width,string text)
        {
            ResizeButton(id,x,y,width);var label=buttons[id].transform.Find("Label").GetComponent<Text>();
            label.text=text;label.rectTransform.anchoredPosition=new Vector2(44,-2);label.rectTransform.sizeDelta=new Vector2(width-48,44);label.fontSize=13;label.color=Ink;
        }
        private void CenterPrimaryAction(string id)
        {
            var button=buttons[id].GetComponent<RectTransform>();
            var label=button.Find("Label").GetComponent<Text>();
            var symbol=button.Find("Action symbol").GetComponent<RectTransform>();
            var labelWidth=Mathf.Min(label.preferredWidth+2,button.rect.width-60);
            var left=(button.rect.width-(20+14+labelWidth))/2;
            symbol.anchoredPosition=new Vector2(left,-14);
            label.rectTransform.anchoredPosition=new Vector2(left+34,-2);
            label.rectTransform.sizeDelta=new Vector2(labelWidth,44);
        }
        private void RefreshReferenceHud()
        {
            if(referenceFooter==null)return;
            referenceFooter.gameObject.SetActive(!DeveloperMode);SafeRoot.Find("Observed journey").gameObject.SetActive(!DeveloperMode && !policyEditing && !modalObject.activeSelf);
            buttons["PolicyShortcut"].gameObject.SetActive(!DeveloperMode && !Session.Started && !policyEditing);
            if(eventMark!=null){modal.Find("Heading gem").gameObject.SetActive(!DeveloperMode);modal.Find("Heading rule").gameObject.SetActive(!DeveloperMode);}
            if(DeveloperMode){if(terminalTransportReserve!=null)terminalTransportReserve.gameObject.SetActive(false);return;}
            LayoutReferenceHud(SafeRoot.rect.width,SafeRoot.rect.height);
            var w=SafeRoot.rect.width;
            if(policyEditing)setup.anchoredPosition=new Vector2(0,298);
            for(var i=0;i<4;i++)
            {
                var id=CardOrder[i];var unit=Fixture.Baseline().Find(id);var r=cardValues[id].transform.parent;
                r.Find("Name").GetComponent<Text>().text=HeroName(id);
                r.Find("Role meaning").GetComponent<Text>().text=dead.Contains(id)?"戦闘不能":id=="B"?(Session.Preference==Preference.ProtectAnchor?"シロを守る":"敵を攻める"):CoreRoles[i];
                cardHealth[id].rectTransform.sizeDelta=new Vector2(((w-36)/4-14)*hp[id]/unit.MaxHp,5);
                r.Find("Approved portrait").GetComponent<RawImage>().color=dead.Contains(id)?new Color(.45f,.45f,.48f):Color.white;
                companionFrames[i].Border=dead.Contains(id)?new Color(.32f,.36f,.42f):CompanionColors[i];companionFrames[i].SetVerticesDirty();
                if(id=="B")
                {
                    r.Find("Policy row").gameObject.SetActive(!Session.Started);
                    r.Find("Policy disclosure").gameObject.SetActive(!Session.Started);
                }
            }
            if(!Session.Started)
            {
                SafeRoot.Find("Boundary").GetComponent<Text>().text="モモの方針を選び、仲間に戦いを託す";
                buttons["Start"].GetComponent<Image>().color=Color.clear;
                if(policyEditing){PositionReferenceAction("Start",12,0,w-24,"この方針で戦闘開始");}
                else{PositionReferenceAction("PolicyShortcut",12,0,(w-30)/2,Session.Preference==Preference.ProtectAnchor?"モモ：シロを守る":"モモ：敵を攻める");PositionReferenceAction("Start",18+(w-30)/2,0,(w-30)/2,"戦闘開始");}
                eventBackdrop.gameObject.SetActive(false);eventCaption.gameObject.SetActive(false);
            }
            else if(!IsExploring)
            {
                if(terminalSeen)
                {PositionReferenceAction("Reset",12,0,(w-30)/2,"戦闘を再試行");PositionReferenceAction("Compare",18+(w-30)/2,0,(w-30)/2,"予想と結果");}
                else
                {PositionReferenceAction("Play",w-116,0,104,Auto?"一時停止":"再開");}
            }
            PositionReferenceAction("Protect",12,46,(w-30)/2,"シロを守る");PositionReferenceAction("Attack",18+(w-30)/2,46,(w-30)/2,"敵へ攻める");
            foreach(var id in new[]{"Protect","Attack"})
            {
                var selected=(id=="Protect")== (Session.Preference==Preference.ProtectAnchor);var panel=buttons[id].GetComponentInChildren<HudPanelGraphic>();
                panel.Border=selected?Gold:new Color(.40f,.50f,.60f);panel.Fill=selected?new Color(.12f,.22f,.28f,.99f):new Color(.035f,.07f,.12f,.96f);panel.SetVerticesDirty();
                if(selected)buttons[id].transform.Find("Label").GetComponent<Text>().text=id=="Protect"?"選択中\nシロを守る":"選択中\n敵を攻める";
            }
            PositionReferenceAction("Details",w-68,6,56,"");
            var book=buttons["Details"].transform.Find("Action symbol").GetComponent<RectTransform>();book.anchoredPosition=new Vector2(18,-6);
            var bookLabel=buttons["Details"].transform.Find("Label").GetComponent<Text>();bookLabel.text="記録";bookLabel.fontSize=10;bookLabel.rectTransform.anchoredPosition=new Vector2(3,-30);bookLabel.rectTransform.sizeDelta=new Vector2(50,17);
            journeyStatus.text=IsExploring?"探索":!Session.Started?"準備":terminalSeen?"結果":"戦闘";
            discoveredStatus.text=ExplorationEnabled?"":"試しの戦い";
            SetTransportSymbol(!Auto);
            var phaseGlyph=journeyStatus.transform.parent.Find("Journey emblem").GetComponent<HudGlyphGraphic>();phaseGlyph.Symbol=IsExploring?HudSymbol.Steps:Session.Started?HudSymbol.Swords:HudSymbol.Compass;phaseGlyph.SetVerticesDirty();
            if(!ExplorationEnabled)orderStrip.anchoredPosition=(Vector2)Field.InverseTransformPoint(SafeRoot.TransformPoint(new Vector3(12,-96,0)));
            RefreshReferenceOrder();
            LayoutReferenceTypography(w,SafeRoot.rect.height);RefreshAutonomousHud();
        }
        private void RefreshReferenceOrder()
        {
            var state=Session.Replay.State;var initiative=state.Initiative;
            // While round intents are drawn, the strip starts at (and golds) the next actor, matching the solid arc.
            var lead=GauntletRules && Session.Started && !Busy && !terminalSeen && state.Outcome==Outcome.Active
                ?Rules.Intents(state,new Command(Session.Replay.Preference)).Select(p=>p.Actor).FirstOrDefault()??shownActor:shownActor;
            var start=initiative.ToList().IndexOf(lead);if(start<0)start=0;var slot=0;
            ReferenceText(orderLabel,8,1,80,14,9,TextAnchor.MiddleLeft);
            for(var i=0;i<initiative.Count;i++)
            {
                var id=initiative[(start+i)%initiative.Count];var tile=orderFrames[id];if(!tile.gameObject.activeSelf)continue;
                tile.color=Color.clear;tile.rectTransform.anchoredPosition=new Vector2(6+slot*24,-15);tile.rectTransform.sizeDelta=new Vector2(22,22);slot++;
                var enemy=state.Find(id).Enemy;var next=id==lead;
                var frame=tile.transform.Find("Illustrated frame").GetComponent<HudPanelGraphic>();
                frame.Border=next?Gold:enemy?new Color(1,.45f,.42f,.9f):new Color(.45f,.75f,1,.9f);
                frame.Fill=next?new Color(.36f,.23f,.06f,.97f):enemy?new Color(.22f,.05f,.07f,.95f):new Color(.05f,.10f,.16f,.95f);frame.SetVerticesDirty();
                var face=tile.transform.Find("Actor portrait");
                if(face!=null)
                {
                    var art=face.GetComponent<RawImage>();var r=face.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(2,-2);r.sizeDelta=new Vector2(18,18);
                    var uvHeight=.30f;var uvWidth=uvHeight*art.texture.height/art.texture.width;art.uvRect=new Rect(.51f-uvWidth/2,.58f,uvWidth,uvHeight);
                }
                ReferenceText(tile.transform.Find("Actor").GetComponent<Text>(),0,0,22,22,11,TextAnchor.MiddleCenter);
            }
            orderStrip.sizeDelta=new Vector2(10+slot*24,40);
        }
        private void ConfigureReferenceMap(RectTransform map)
        {
            if(referenceFooter==null)return;
            map.GetComponent<Image>().color=Color.clear;Frame(map,new Color(.035f,.075f,.14f,.90f),new Color(.46f,.70f,.91f),12);
            var caption=map.Find("Known map caption").GetComponent<Text>();caption.text="周辺地図";caption.fontSize=10;
            MakeButton(map,"Map","",64,52,44,44,OpenKnownMap,10);StyleReferenceButton("Map",HudSymbol.Search,new Color(.43f,.79f,1));
            // The search gem is the visible control; the 44pt button keeps its full touch target.
            var smallFrame=buttons["Map"].transform.Find("Illustrated frame").GetComponent<HudPanelGraphic>();smallFrame.Fill=smallFrame.Border=Color.clear;
            var search=buttons["Map"].transform.Find("Action symbol").GetComponent<RectTransform>();search.anchoredPosition=new Vector2(12,-12);
            StyleReferenceButton("Explore",HudSymbol.Steps,Gold);PrimaryFill("Explore");
        }
        private void SetTransportSymbol(bool paused)
        {
            var symbol=buttons["Play"].transform.Find("Action symbol").GetComponent<HudGlyphGraphic>();
            var next=paused?HudSymbol.Play:HudSymbol.Pause;if(symbol.Symbol==next)return;symbol.Symbol=next;symbol.SetVerticesDirty();
        }
        private void OpenKnownMap()
        {
            referenceSettingsOpen=false;
            if(IsExploring && travelRoutine!=null)traveller.SetPaused(true);
            if(Session.Started && !terminalSeen){Auto=false;Paused=true;Refresh();}
            showingResults=false;headline.text="見つけた遺跡";modalText.text="青：仲間　赤：敵\n見つけた床と通路だけを表示";LayoutDetails();
            if(expandedKnownMap==null){expandedKnownMap=Rect(modal,"Expanded known terrain",22,112,320,280).gameObject.AddComponent<FloorMapGraphic>();expandedKnownMap.raycastTarget=false;}
            expandedKnownMap.rectTransform.sizeDelta=new Vector2(modal.rect.width-44,Mathf.Max(110,modal.rect.height-225));
            buttons["Effects"].gameObject.SetActive(false);buttons["Diagnostics"].gameObject.SetActive(false);
            buttons["PresentationSettings"].gameObject.SetActive(false);
            buttons["RecordUp"].gameObject.SetActive(false);buttons["RecordDown"].gameObject.SetActive(false);
            expandedKnownMap.Configure(floorMap,displayedKnown,positions.Where(p=>!dead.Contains(p.Key)));expandedKnownMap.gameObject.SetActive(true);
            modalObject.SetActive(true);modalObject.transform.SetAsLastSibling();RefreshFloorUi();
            SyncReferenceModal();
        }
        private void OpenReferenceSettings()
        {
            referenceSettingsOpen=true;headline.text="表示・検証設定";modalText.text="描画の負荷を切り替えられます。\n開発用コントロールでは実験の手動再生を確認できます。";
            LayoutDetails();
        }
        private void SyncReferenceModal()
        {
            if(referenceModalCurtain==null)return;
            LayoutAutonomousJournal();
            var open=modalObject.activeSelf;
            var sheetGem=modal.Find("Heading gem")?.GetComponent<HudGlyphGraphic>();
            var sheet=expandedKnownMap!=null && expandedKnownMap.gameObject.activeSelf?HudSymbol.Search:showingResults?HudSymbol.Compare:HudSymbol.Book;
            if(sheetGem!=null && sheetGem.Symbol!=sheet){sheetGem.Symbol=sheet;sheetGem.SetVerticesDirty();}
            if(terminalTransportReserve!=null)
                terminalTransportReserve.gameObject.SetActive(AutonomousProduct && terminalSeen && !IsExploring && !open && Session.Replay.State.Outcome==Outcome.Victory);
            if(referenceModalCurtain.gameObject.activeSelf==open)return;
            referenceModalCurtain.gameObject.SetActive(open);
            if(open){referenceModalCurtain.SetAsLastSibling();modal.SetAsLastSibling();}
        }
        private void RefreshReferenceFloorUi()
        {
            if(referenceFooter==null || floorMini==null)return;
            var map=floorMini.transform.parent.GetComponent<RectTransform>();map.anchoredPosition=new Vector2(12,-82);map.sizeDelta=new Vector2(110,102);
            floorMini.rectTransform.anchoredPosition=new Vector2(6,-20);floorMini.rectTransform.sizeDelta=new Vector2(98,74);
            var discovered=floorMap.Rooms.Count(r=>displayedKnown.Any(p=>r.Contains(p)));
            discoveredStatus.text="発見 "+discovered+"室";
            SafeRoot.Find("Observed journey").gameObject.SetActive(!DeveloperMode && !policyEditing && !modalObject.activeSelf);
            if(expandedKnownMap!=null && !modalObject.activeSelf)expandedKnownMap.gameObject.SetActive(false);
            var w=SafeRoot.rect.width;var h=SafeRoot.rect.height;
            var explore=buttons["Explore"].GetComponent<RectTransform>();explore.anchoredPosition=new Vector2(12,-(h-240));
            PositionReferenceAction("Explore",12,h-240,w-24,"この先の遺跡を探索する");
            // Initiative is meaningful, but never spans the cast. Keep it below the compact map.
            orderStrip.anchoredPosition=(Vector2)Field.InverseTransformPoint(SafeRoot.TransformPoint(new Vector3(12,-193,0)));
            if(IsExploring)
            {
                journeyStatus.text=traveller.Paused?"停止中":"探索";
                var travelGlyph=journeyStatus.transform.parent.Find("Journey emblem").GetComponent<HudGlyphGraphic>();travelGlyph.Symbol=traveller.Paused?HudSymbol.Pause:HudSymbol.Steps;travelGlyph.SetVerticesDirty();
                SafeRoot.Find("Boundary").GetComponent<Text>().text="見えている床や通路を選んで進む";
                PositionReferenceAction("Reset",12,0,122,"戦闘を再試行");PositionReferenceAction("Play",w-116,0,104,traveller.Paused?"再開":"一時停止");
                SetTransportSymbol(traveller.Paused);
                eventCaption.rectTransform.anchoredPosition=new Vector2(143,-3);eventCaption.rectTransform.sizeDelta=new Vector2(w-268,44);
                eventBackdrop.rectTransform.anchoredPosition=new Vector2(138,0);eventBackdrop.rectTransform.sizeDelta=new Vector2(w-260,48);
            }
            LayoutReferenceTypography(w,h);RefreshAutonomousHud();
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudPanelGraphic:MaskableGraphic
    {
        public Color Fill,Border,TopFill;public float Radius=10;public bool PortraitSurface;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var radius=Mathf.Min(Radius,Mathf.Min(r.width,r.height)/2);const int n=32;
            Vector2 Ring(int i,float inset)
            {
                var corner=i/8;var angle=(corner*90+i%8*90f/7)*Mathf.Deg2Rad;
                var cx=corner==0 || corner==3?r.xMax-radius:r.xMin+radius;var cy=corner<2?r.yMax-radius:r.yMin+radius;
                return new Vector2(cx,cy)+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(radius-inset);
            }
            Color Surface(Vector2 at)
            {
                if(PortraitSurface)return Color.Lerp(Fill,TopFill,Mathf.InverseLerp(r.yMin+r.height*.35f,r.yMax,at.y));
                var lit=Color.Lerp(Fill,Color.white,.08f);lit.a=Fill.a;return Color.Lerp(Fill,lit,Mathf.InverseLerp(r.yMin,r.yMax,at.y));
            }
            // Soft drop shadow outside the rect, then border band, then lit surface. Hit rect is unchanged.
            var drop=new Vector2(0,-2);var shadow=new Color(0,.01f,.03f,.34f*Fill.a);var clear=new Color(0,.01f,.03f,0);
            for(var i=0;i<n;i++)vh.AddVert(Ring(i,-4)+drop,clear,Vector2.zero);
            for(var i=0;i<n;i++)vh.AddVert(Ring(i,0)+drop,shadow,Vector2.zero);
            for(var i=0;i<n;i++)vh.AddVert(Ring(i,0),Border,Vector2.zero);
            for(var i=0;i<n;i++){var at=Ring(i,1.8f);vh.AddVert(at,Surface(at),Vector2.zero);}
            var center=vh.currentVertCount;vh.AddVert(r.center,Surface(r.center),Vector2.zero);
            for(var i=0;i<n;i++)
            {
                var j=(i+1)%n;
                vh.AddTriangle(i,j,n+i);vh.AddTriangle(j,n+j,n+i);
                vh.AddTriangle(2*n+i,2*n+j,3*n+i);vh.AddTriangle(2*n+j,3*n+j,3*n+i);
                vh.AddTriangle(center,3*n+i,3*n+j);
            }
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudPortraitFadeGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // A soft shade under the name; the portrait stays visible through it.
            vh.Clear();var r=rectTransform.rect;
            vh.AddVert(new Vector2(r.xMin,r.yMax),new Color(.035f,.065f,.12f,0),Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),new Color(.035f,.065f,.12f,0),Vector2.zero);
            var edge=r.yMax-r.height*.45f;
            vh.AddVert(new Vector2(r.xMax,edge),new Color(.035f,.065f,.12f,.55f),Vector2.zero);
            vh.AddVert(new Vector2(r.xMin,edge),new Color(.035f,.065f,.12f,.55f),Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMin),new Color(.035f,.065f,.12f,.88f),Vector2.zero);
            vh.AddVert(new Vector2(r.xMin,r.yMin),new Color(.035f,.065f,.12f,.88f),Vector2.zero);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);vh.AddTriangle(3,2,4);vh.AddTriangle(3,4,5);
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudGradientGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector2(r.xMin,r.yMax),new Color(.025f,.045f,.09f,0),Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMax),new Color(.025f,.045f,.09f,0),Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),new Color(.025f,.045f,.09f,.92f),Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMin),new Color(.025f,.045f,.09f,.92f),Vector2.zero);vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);}
    }
    // Dotted floor axis that fades out at both ends: a location reference, not a floor count.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudAxisGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;const float step=7, dot=2.6f;
            for(var y=r.yMin+step/2;y<r.yMax;y+=step)
            {
                var fade=Mathf.Clamp01(Mathf.Min(Mathf.InverseLerp(r.yMin,r.yMax,y),1-Mathf.InverseLerp(r.yMin,r.yMax,y))*4);
                // Dark-rimmed light dots stay readable over both pale cloud and deep sky.
                foreach(var (size,ink) in new[]{(dot+2.4f,new Color(.04f,.08f,.15f,.75f*fade)),(dot,new Color(color.r,color.g,color.b,color.a*fade))})
                {
                    var i=vh.currentVertCount;var c=new Vector2(r.center.x,y);
                    foreach(var v in new[]{c+new Vector2(-size/2,-size/2),c+new Vector2(-size/2,size/2),c+new Vector2(size/2,size/2),c+new Vector2(size/2,-size/2)})vh.AddVert(v,ink,Vector2.zero);
                    vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
                }
            }
        }
    }
    public enum HudSymbol {Compass,Book,Search,Shield,Swords,Counter,Heal,Play,Pause,Reset,Compare,Steps,Chevron,Star,Descend,Alert,Close}
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudGlyphGraphic:MaskableGraphic
    {
        public HudSymbol Symbol;
        // Gem: a white glyph with a soft halo on a lit plate of the accent colour (reference icon style).
        // The plate grows outward from the rect, so existing glyph placement and hit areas stay unchanged.
        public bool Gem, Round;
        private static void Plate(VertexHelper vh,Rect r,float radius,Color bottom,Color top)
        {
            radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)/2);const int n=32;var first=vh.currentVertCount;
            Color At(Vector2 p)=>Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y));
            vh.AddVert(r.center,At(r.center),Vector2.zero);
            for(var i=0;i<n;i++)
            {
                var corner=i/8;var angle=(corner*90+i%8*90f/7)*Mathf.Deg2Rad;
                var c=new Vector2(corner==0 || corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                var p=c+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;vh.AddVert(p,At(p),Vector2.zero);
            }
            for(var i=0;i<n;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%n);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var area=r;
            if(Gem)
            {
                var pad=r.width*.2f;var body=new Rect(r.xMin-pad,r.yMin-pad,r.width+2*pad,r.height+2*pad);
                var radius=Round?body.width/2:body.width*.26f;
                var deep=Color.Lerp(color,Color.black,.62f);deep.a=1;var lit=Color.Lerp(color,Color.white,.18f);lit.a=1;
                var rim=Color.Lerp(color,Color.white,.62f);rim.a=1;
                Plate(vh,new Rect(body.xMin-1.4f,body.yMin-1.4f,body.width+2.8f,body.height+2.8f),radius+1.4f,new Color(.01f,.02f,.05f,.85f),new Color(.01f,.02f,.05f,.85f));
                Plate(vh,body,radius,rim,rim);
                Plate(vh,new Rect(body.xMin+1.4f,body.yMin+1.4f,body.width-2.8f,body.height-2.8f),radius-1.4f,deep,lit);
                // Upper gloss: a fading highlight, as on the reference's faceted icons.
                Plate(vh,new Rect(body.xMin+3,body.center.y,body.width-6,body.height/2-3),Mathf.Max(1,radius-3),new Color(1,1,1,0),new Color(1,1,1,.26f));
                area=new Rect(r.xMin+r.width*.1f,r.yMin+r.height*.1f,r.width*.8f,r.height*.8f);
            }
            var passes=Gem?new[]{(new Color(Mathf.Lerp(color.r,1,.5f),Mathf.Lerp(color.g,1,.5f),Mathf.Lerp(color.b,1,.5f),.40f),2.5f),(Color.white,1.15f)}:new[]{(color,1f)};
            foreach(var pass in passes)
            {
                var ink=pass.Item1;var weight=pass.Item2;
                Vector2 P(float x,float y)=>new Vector2(area.xMin+x*area.width,area.yMax-y*area.height);
                void Line(float x,float y,float a,float b,float t=.085f){var p=P(x,y);var q=P(a,b);var d=(q-p).normalized;var n=new Vector2(-d.y,d.x)*area.width*t*weight/2;var e=d*area.width*t*(weight-1)/2;var i=vh.currentVertCount;foreach(var v in new[]{p-e+n,q+e+n,q+e-n,p-e-n})vh.AddVert(v,ink,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
                void Circle(float x,float y,float radius){for(var i=0;i<20;i++){var a=i*Mathf.PI/10;var b=(i+1)*Mathf.PI/10;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,.065f);}}
                // Star-shaped filled outline, fanned from its centre; the halo pass scales it outward.
                void Fill(float cx,float cy,params float[] xy)
                {
                    var grow=1+.07f*(weight-1);var c=P(cx,cy);var i=vh.currentVertCount;vh.AddVert(c,ink,Vector2.zero);
                    for(var k=0;k<xy.Length;k+=2)vh.AddVert(c+(P(xy[k],xy[k+1])-c)*grow,ink,Vector2.zero);
                    var count=xy.Length/2;for(var k=0;k<count;k++)vh.AddTriangle(i,i+1+k,i+1+(k+1)%count);
                }
                switch(Symbol)
                {
                    case HudSymbol.Compass:Line(.5f,.05f,.75f,.5f);Line(.75f,.5f,.5f,.95f);Line(.5f,.95f,.25f,.5f);Line(.25f,.5f,.5f,.05f);Line(.05f,.5f,.95f,.5f);Circle(.5f,.5f,.1f);break;
                    case HudSymbol.Search:Circle(.42f,.42f,.28f);Line(.62f,.62f,.92f,.92f,.12f);break;
                    case HudSymbol.Book:Line(.12f,.15f,.48f,.24f);Line(.48f,.24f,.88f,.15f);Line(.12f,.15f,.12f,.82f);Line(.88f,.15f,.88f,.82f);Line(.12f,.82f,.48f,.92f);Line(.48f,.92f,.88f,.82f);Line(.5f,.25f,.5f,.90f);break;
                    case HudSymbol.Shield:
                        if(!Gem){Line(.1f,.15f,.5f,.05f);Line(.5f,.05f,.9f,.15f);Line(.1f,.15f,.2f,.65f);Line(.9f,.15f,.8f,.65f);Line(.2f,.65f,.5f,.95f);Line(.8f,.65f,.5f,.95f);Line(.5f,.27f,.5f,.7f);Line(.3f,.46f,.7f,.46f);break;}
                        Fill(.5f,.45f,.5f,.04f,.9f,.15f,.84f,.6f,.5f,.96f,.16f,.6f,.1f,.15f);
                        if(weight<2){var cut=Color.Lerp(color,Color.black,.35f);var keep=ink;ink=cut;Line(.5f,.26f,.5f,.72f,.11f);Line(.3f,.44f,.7f,.44f,.11f);ink=keep;}
                        break;
                    case HudSymbol.Heal:Line(.5f,.12f,.5f,.88f,.22f);Line(.12f,.5f,.88f,.5f,.22f);break;
                    case HudSymbol.Swords:Line(.15f,.15f,.85f,.85f);Line(.85f,.15f,.15f,.85f);Line(.08f,.67f,.33f,.92f);Line(.67f,.92f,.92f,.67f);break;
                    case HudSymbol.Counter:Line(.85f,.3f,.22f,.3f);Line(.22f,.3f,.22f,.7f);Line(.22f,.7f,.82f,.7f);Line(.55f,.48f,.82f,.7f);Line(.55f,.92f,.82f,.7f);break;
                    case HudSymbol.Play:if(Gem)Fill(.43f,.5f,.25f,.1f,.88f,.5f,.25f,.9f);else{Line(.25f,.1f,.85f,.5f);Line(.85f,.5f,.25f,.9f);Line(.25f,.9f,.25f,.1f);}break;
                    case HudSymbol.Pause:Line(.3f,.12f,.3f,.88f,.18f);Line(.7f,.12f,.7f,.88f,.18f);break;
                    case HudSymbol.Reset:Circle(.5f,.52f,.34f);Line(.16f,.15f,.16f,.47f);Line(.16f,.47f,.45f,.43f);break;
                    case HudSymbol.Compare:Line(.12f,.3f,.88f,.3f);Line(.65f,.08f,.88f,.3f);Line(.65f,.52f,.88f,.3f);Line(.88f,.75f,.12f,.75f);Line(.35f,.53f,.12f,.75f);Line(.35f,.97f,.12f,.75f);break;
                    case HudSymbol.Steps:Line(.28f,.08f,.19f,.28f,.20f);Line(.21f,.42f,.16f,.61f,.18f);Line(.78f,.38f,.69f,.58f,.20f);Line(.71f,.72f,.66f,.91f,.18f);break;
                    case HudSymbol.Chevron:Line(.30f,.15f,.70f,.5f,.12f);Line(.70f,.5f,.30f,.85f,.12f);break;
                    case HudSymbol.Star:Fill(.5f,.5f,.5f,0,.62f,.38f,1,.5f,.62f,.62f,.5f,1,.38f,.62f,0,.5f,.38f,.38f);break;
                    case HudSymbol.Close:Line(.2f,.2f,.8f,.8f,.16f);Line(.8f,.2f,.2f,.8f,.16f);break;
                    case HudSymbol.Alert:Line(.5f,.08f,.5f,.60f,.22f);Line(.5f,.76f,.5f,.94f,.24f);break;
                    case HudSymbol.Descend:Line(.5f,.08f,.5f,.62f,.16f);Fill(.5f,.66f,.18f,.52f,.82f,.52f,.5f,.94f);break;
                }
            }
        }
    }
}
