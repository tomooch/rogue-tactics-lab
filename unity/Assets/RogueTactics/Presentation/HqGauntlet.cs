using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core;
using RogueTactics.Core.Exploration;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    // Gauntlet experiment (Human 2026-10-27): consecutive encounters in later rooms of the same floor.
    // Encounter 0 stays the frozen G1 flow. From encounter 1 on, every living actor's present intent is
    // drawn, auto play stops when Core reports a newly threatened ally, and B's preference can be
    // corrected once per encounter. All of it reads Core; nothing here decides combat.
    public sealed partial class MobileG1View
    {
        private GridPosition battleOrigin;
        private bool crisisActive, enteringEncounter;
        private IReadOnlyList<string> crisisAllies=new string[0];
        private readonly List<MobileRouteGraphic> roundIntents=new List<MobileRouteGraphic>();
        private HudGlyphGraphic nextIntentMark;
        private RectTransform resultBanner;
        private Text resultTitle, resultDetail;
        private readonly List<HudGlyphGraphic> crisisMarks=new List<HudGlyphGraphic>();
        private bool GauntletRules=>ExplorationEnabled && Session.EncounterIndex>0;
        private GridPosition Shown(GridPosition core)=>core.Offset(battleOrigin.X,battleOrigin.Y);
        private Vector3 BattleCenter()
            =>new Vector3(2*Cell,HqWorld?-2f-(!DeveloperMode && policyEditing?3.5f:0):-1.4f,2*Cell*DepthSpacing-.4f)+World(battleOrigin);
        public bool CrisisPaused=>crisisActive && Paused && !Auto;
        public IReadOnlyList<string> CrisisAllies=>crisisAllies;

        private void EnsureGauntletUi()
        {
            if(buttons.ContainsKey("Challenge") || !ExplorationEnabled || !HqWorld)return;
            MakeButton(SafeRoot,"Challenge","守り手に挑む",12,0,366,48,ChallengeNextEncounter,15);
            StyleReferenceButton("Challenge",HudSymbol.Swords,Gold);PrimaryFill("Challenge");
            MakeButton(SafeRoot,"Correct","",12,0,366,48,RequestInstruction,13);
            StyleReferenceButton("Correct",HudSymbol.Shield,new Color(1,.46f,.42f));
            PrimaryFill("Correct",new Color(1,.26f,.26f),new Color(1,.66f,.60f));
            for(var i=0;i<6;i++)
            {var strip=Strip(Field,"Round intent "+i,Color.white);var g=strip.GetComponent<MobileRouteGraphic>();g.Glow=true;strip.gameObject.SetActive(false);roundIntents.Add(g);}
            nextIntentMark=Glyph(Field,"Next intent mark",HudSymbol.Swords,0,0,16,Color.white,true,true);nextIntentMark.gameObject.SetActive(false);
            // Threatened allies get a red alert gem above the head while the crisis stop waits for a choice.
            for(var i=0;i<4;i++){var mark=Glyph(Field,"Crisis mark "+i,HudSymbol.Alert,0,0,16,new Color(1,.28f,.26f),true,true);mark.rectTransform.pivot=new Vector2(.5f,.5f);mark.gameObject.SetActive(false);crisisMarks.Add(mark);}
            // Encounter result: a gold banner between the board and the cards. Display only; Core decided the outcome.
            resultBanner=Rect(SafeRoot,"Result banner",12,0,366,66);
            Frame(resultBanner,new Color(.08f,.065f,.03f,.95f),Gold,16).TopFill=new Color(.24f,.17f,.06f,.95f);
            resultBanner.GetComponentInChildren<HudPanelGraphic>().PortraitSurface=true;
            Glyph(resultBanner,"Left star",HudSymbol.Star,20,21,24,Gold,true,true);Glyph(resultBanner,"Right star",HudSymbol.Star,322,21,24,Gold,true,true);
            resultTitle=Label(resultBanner,"Result title","",60,3,246,36,26,TextAnchor.MiddleCenter);resultTitle.fontStyle=FontStyle.Bold;resultTitle.color=new Color(1,.88f,.55f);
            var titleShadow=resultTitle.gameObject.AddComponent<Shadow>();titleShadow.effectColor=new Color(.10f,.05f,0,.95f);titleShadow.effectDistance=new Vector2(1.5f,-1.5f);
            resultDetail=Label(resultBanner,"Result detail","",60,39,246,22,12,TextAnchor.MiddleCenter);
            resultBanner.gameObject.SetActive(false);
        }
        private int NextEncounter=>Session.EncounterIndex+1;
        private bool PartyInRoom(int room)
            =>traveller!=null && traveller.Party.Values.Any(p=>floorMap.RoomAt(p)==room);
        private bool CanChallenge=>ExplorationEnabled && IsExploring && travelRoutine==null && !modalObject.activeSelf &&
            NextEncounter<Fixture.EncounterCount && PartyInRoom(GauntletRooms.Room[NextEncounter]);
        public bool ChallengeAvailable=>CanChallenge;

        public void ChallengeNextEncounter()
        {
            if(!CanChallenge || enteringEncounter)return;
            StartCoroutine(EnterEncounter(NextEncounter));
        }
        private IEnumerator EnterEncounter(int index)
        {
            enteringEncounter=true;
            var from=pawns.ToDictionary(p=>p.Key,p=>p.Value.position);var cameraFrom=FieldCamera.transform.position;
            traveller.Cancel();traveller=null;IsExploring=false;TravelSelection=null;selectedFloorMarker.gameObject.SetActive(false);
            battleOrigin=GauntletRooms.Origin(floorMap,index);
            Session.LoadEncounter(index);Auto=false;Paused=false;policyEditing=false;Prediction.text="";ResetProjection();
            SetComparisonCamera(CameraPitch);var cameraTo=FieldCamera.transform.position;
            // Allies walk into the room's formation; guardians rise where Core placed them.
            visualTweenActive=true;
            foreach(var u in Session.Replay.State.Units)pawns[u.Id].gameObject.SetActive(true);
            for(var t=0f;t<1;t+=Time.deltaTime/.6f)
            {
                var k=t*t*(3-2*t);
                foreach(var u in Session.Replay.State.Units)
                {
                    var to=World(positions[u.Id]);
                    pawns[u.Id].position=u.Enemy?to+Vector3.down*(1-k)*1.2f:Vector3.Lerp(from[u.Id],to,k);
                }
                FieldCamera.transform.position=Vector3.Lerp(cameraFrom,cameraTo,k);
                yield return null;
            }
            visualTweenActive=false;FieldCamera.transform.position=cameraTo;enteringEncounter=false;Refresh();
        }
        private bool interruptRequested;
        private string directedAlly="", directedEnemy="";
        private RectTransform instructionOverlay, instructionSheet;
        private Text instructionSummary, instructionReceipt;
        private string orderedActor="", orderedTarget="", instructionReceiptText="";
        private void UpdateInstructionReceipt(ResolvedStep step)
        {
            if(orderedActor=="")return;
            if(step.Plan.Actor==orderedActor)
            {
                var hits=step.Events.Where(e=>e.Actor==orderedActor && e.Kind==EventKind.Damage).Sum(e=>e.Amount);
                instructionReceiptText=HeroName(orderedActor)+"への指示："+(step.Plan.Kind==ActionKind.Attack?HeroName(orderedTarget)+"に"+hits+"ダメージ":step.Plan.Kind==ActionKind.Move?"1マス接近した":"進めず待機した");
                orderedActor="";orderedTarget="";
            }
            else if(!Session.Replay.State.Find(orderedActor).Alive || !Session.Replay.State.Find(orderedTarget).Alive)
            {
                instructionReceiptText="指示は終了：仲間か対象が戦闘不能になった";
                orderedActor="";orderedTarget="";
            }
        }
        public bool InstructionOpen=>instructionOverlay!=null && instructionOverlay.gameObject.activeSelf;
        public void RequestInstruction()
        {
            if(!Session.Started || terminalSeen || IsExploring || Session.Replay.CorrectionsLeft<1 || InstructionOpen)return;
            interruptRequested=true;Auto=false;
            // Finish the already resolved action's presentation before exposing any new state.
            Paused=false;if(!Busy)OpenInstruction();Refresh();
        }
        private void OpenInstruction()
        {
            interruptRequested=false;
            if(terminalSeen || Session.Replay.State.Outcome!=Outcome.Active)return;
            Paused=true;directedAlly="";directedEnemy="";
            instructionOverlay.gameObject.SetActive(true);instructionOverlay.SetAsLastSibling();RefreshInstruction();Canvas.ForceUpdateCanvases();
        }
        private void EnsureInstructionUi()
        {
            if(instructionOverlay!=null)return;
            MakeButton(SafeRoot,"Interrupt","指示する · 残り1回",12,0,366,48,RequestInstruction,16);
            StyleReferenceButton("Interrupt",HudSymbol.Swords,Gold);
            var receiptRoot=Rect(SafeRoot,"Instruction receipt panel",12,0,366,48);
            Frame(receiptRoot,new Color(.045f,.085f,.12f,.95f),new Color(.45f,.57f,.66f),10);
            instructionReceipt=Label(receiptRoot,"Instruction receipt","",8,0,350,48,13,TextAnchor.MiddleCenter);
            instructionOverlay=Rect(SafeRoot,"Instruction overlay",0,0,390,844);
            instructionOverlay.gameObject.AddComponent<Image>().color=new Color(0,0,0,.15f);
            instructionSheet=Rect(instructionOverlay,"Instruction sheet",10,0,370,304);
            instructionSheet.gameObject.AddComponent<Image>().color=new Color(.045f,.085f,.12f,1);
            Frame(instructionSheet,new Color(.045f,.085f,.12f,1),new Color(.45f,.57f,.66f),14);
            Label(instructionSheet,"Heading","仲間に一手を託す",16,6,338,40,19);
            Label(instructionSheet,"Help","仲間と狙う相手を選ぶ · 残り1回",16,40,338,24,12);
            for(var i=0;i<CardOrder.Length;i++)
            {
                var id=CardOrder[i];MakeButton(instructionSheet,"DirectAlly"+id,HeroName(id),16+i*85,72,80,76,()=>{directedAlly=id;RefreshInstruction();},15);
                var button=buttons["DirectAlly"+id];
                var portrait=Rect(button.transform,"Approved face",4,3,42,54).gameObject.AddComponent<RawImage>();
                var art=CharacterArt[id[0]-'A'];portrait.texture=art;
                var uw=.5f*42/54*art.height/art.width;portrait.uvRect=new Rect(.5f-uw/2,.43f,uw,.5f);portrait.raycastTarget=false;
                var name=button.transform.Find("Label").GetComponent<Text>();name.fontSize=12;name.rectTransform.anchoredPosition=new Vector2(2,-49);name.rectTransform.sizeDelta=new Vector2(76,25);
                Label(button.transform,"HP", "",47,9,31,40,10,TextAnchor.MiddleCenter);
            }
            MakeButton(instructionSheet,"DirectEnemyX",HeroName("X"),16,158,164,44,()=>{directedEnemy="X";RefreshInstruction();},15);
            MakeButton(instructionSheet,"DirectEnemyY",HeroName("Y"),190,158,164,44,()=>{directedEnemy="Y";RefreshInstruction();},15);
            instructionSummary=Label(instructionSheet,"Selection","",16,207,338,40,12);
            MakeButton(instructionSheet,"DirectCancel","やめて任せる",16,250,164,44,CancelInstruction,15);
            MakeButton(instructionSheet,"DirectConfirm","この一手を託す",190,250,164,44,ConfirmInstruction,15);
            instructionOverlay.gameObject.SetActive(false);
        }
        private void RefreshInstruction()
        {
            foreach(var id in CardOrder)
            {var b=buttons["DirectAlly"+id];var u=Session.Replay.State.Find(id);b.interactable=u.Alive;b.transform.Find("HP").GetComponent<Text>().text="HP\n"+u.Hp;b.GetComponent<Image>().color=id==directedAlly?new Color(.4f,.3f,.10f):new Color(.12f,.24f,.26f);}
            foreach(var id in new[]{"X","Y"})
            {var b=buttons["DirectEnemy"+id];b.interactable=Session.Replay.State.Find(id).Alive;b.GetComponent<Image>().color=id==directedEnemy?new Color(.4f,.3f,.10f):new Color(.12f,.24f,.26f);b.transform.Find("Label").GetComponent<Text>().text=HeroName(id)+"   HP "+Session.Replay.State.Find(id).Hp;}
            buttons["DirectConfirm"].interactable=directedAlly!="" && directedEnemy!="";
            instructionSummary.text=directedAlly==""?"誰に一手を託しますか？":directedEnemy==""?HeroName(directedAlly)+"が狙う相手を選んでください":"";
            if(directedAlly!="" && directedEnemy!="")
            {
                var p=Rules.Preview(Session.Replay.State,new Command(Session.Preference),directedAlly,directedEnemy);
                var action=p.Kind==ActionKind.Attack?"攻撃":p.Kind==ActionKind.Move?"1マス接近":"進路が塞がれて待機";
                instructionSummary.text=HeroName(directedAlly)+" → "+HeroName(directedEnemy)+"：今なら"+action+"\n本人の次の1行動に適用。順番までに状況は変わります。";
            }
            RefreshRoundIntents();
        }
        private void CancelInstruction()
        {instructionOverlay.gameObject.SetActive(false);Paused=false;Auto=true;Refresh();RefreshGauntletUi();Next();}
        private void ConfirmInstruction()
        {
            if(directedAlly=="" || directedEnemy=="" || Busy)return;
            Session.Replay.TargetOnce(directedAlly,directedEnemy);
            orderedActor=directedAlly;orderedTarget=directedEnemy;
            instructionReceiptText=HeroName(orderedActor)+" → "+HeroName(orderedTarget)+" · 次の行動を待っています";
            captions.Add(HeroName(directedAlly)+"に指示："+HeroName(directedEnemy)+"を狙う（次の1行動）");
            CancelInstruction();
        }
        private void RefreshRoundIntents()
        {
            if(roundIntents.Count==0)return;
            var show=GauntletRules && Session.Started && !Busy && !terminalSeen && !modalObject.activeSelf && !IsExploring;
            var plans=show?Session.Replay.Intents().Where(p=>p.Kind!=ActionKind.Maintain).ToList():new List<Plan>();
            if(InstructionOpen)
            {
                plans.Clear();
                if(directedAlly!="" && directedEnemy!="")
                {
                    var selected=Rules.Preview(Session.Replay.State,new Command(Session.Preference),directedAlly,directedEnemy);
                    if(selected.Kind!=ActionKind.Maintain)plans.Add(selected);
                }
            }
            nextIntentMark.gameObject.SetActive(false);
            for(var i=0;i<roundIntents.Count;i++)
            {
                var g=roundIntents[i];var active=i<plans.Count;g.gameObject.SetActive(active);if(!active)continue;
                var p=plans[i];var actor=Session.Replay.State.Find(p.Actor);
                // First actor is the next to move: solid and bright. Later ones are dashed and fainter: still their
                // present intent, not a forecast.
                var tint=actor.Enemy?new Color(1,.40f,.38f):new Color(.42f,.84f,1);tint.a=i==0?1:.58f;g.color=tint;g.Dashed=i>0;
                // An arc from the actor's upper body to the target (or destination cell): who acts on whom reads at a glance.
                var from=BodyRect(positions[p.Actor]);var start=new Vector2(from.center.x,from.yMax-from.height*.32f);
                var move=p.Kind==ActionKind.Move;Vector2 end;
                if(move)end=Point(Shown(p.Destination),.05f);
                else{var target=BodyRect(positions[p.Target]);end=new Vector2(target.center.x,target.yMax-target.height*.38f);}
                var span=end-start;if(span.sqrMagnitude<64){g.gameObject.SetActive(false);continue;}
                var lift=Mathf.Clamp(span.magnitude*(move?.28f:.5f),move?14:26,70);var control=(start+end)/2+Vector2.up*lift;
                var arc=new Vector2[18];
                for(var k=0;k<arc.Length;k++){var t=k/(arc.Length-1f);arc[k]=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;}
                g.rectTransform.anchoredPosition=Vector2.zero;g.rectTransform.sizeDelta=Field.rect.size;g.SetPath(arc);
                if(i>0)continue;
                g.transform.SetAsLastSibling();
                // The next actor's action kind sits on its arc's apex as a gem.
                var apex=.25f*start+.5f*control+.25f*end;
                nextIntentMark.Symbol=p.Kind==ActionKind.Attack?HudSymbol.Swords:p.Kind==ActionKind.Heal?HudSymbol.Heal:p.Kind==ActionKind.Protect?HudSymbol.Shield:HudSymbol.Steps;
                nextIntentMark.color=actor.Enemy?new Color(.95f,.32f,.30f):new Color(.30f,.66f,.98f);nextIntentMark.SetVerticesDirty();
                nextIntentMark.rectTransform.anchoredPosition=apex+new Vector2(-8,8);nextIntentMark.gameObject.SetActive(true);nextIntentMark.transform.SetAsLastSibling();
            }
        }
        private void RefreshGauntletUi()
        {
            if(!ExplorationEnabled || !HqWorld || DeveloperMode)return;
            EnsureGauntletUi();EnsureInstructionUi();
            var w=SafeRoot.rect.width;var h=SafeRoot.rect.height;
            var challenge=buttons["Challenge"];challenge.gameObject.SetActive(CanChallenge && !enteringEncounter);
            if(challenge.gameObject.activeSelf)PositionReferenceAction("Challenge",12,h-268,w-24,"守り手に挑む（"+(NextEncounter+1)+" / "+Fixture.EncounterCount+"戦目）");
            instructionOverlay.sizeDelta=SafeRoot.rect.size;
            instructionSheet.anchoredPosition=new Vector2(10,-(h-314));
            var canDirect=Session.Started && !terminalSeen && !IsExploring && !modalObject.activeSelf && Session.Replay.CorrectionsLeft>0;
            buttons["Interrupt"].gameObject.SetActive(canDirect);
            PositionReferenceAction("Interrupt",12,h-268,w-24,interruptRequested?"行動が終わるのを待っています…":"指示する · 残り1回");
            buttons["Interrupt"].interactable=!interruptRequested;
            instructionReceipt.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(12,-(h-268));
            instructionReceipt.text=instructionReceiptText;
            instructionReceipt.transform.parent.gameObject.SetActive(instructionReceiptText!="" && !terminalSeen && !IsExploring && !InstructionOpen && !modalObject.activeSelf);
            var correct=buttons["Correct"];correct.gameObject.SetActive(false);
            if(correct.gameObject.activeSelf)
            {
                var names=string.Join("・",crisisAllies.Select(HeroName));
                PositionReferenceAction("Correct",12,h-268,w-24,names+"が危ない！ モモを「"+(Session.Preference==Preference.ProtectAnchor?"敵を攻める":"シロを守る")+"」に変える（残り1回）");
                correct.transform.Find("Label").GetComponent<Text>().fontSize=12;
            }
            var alive=CardOrder.Count(id=>!dead.Contains(id));
            var result=terminalSeen && !IsExploring && !Busy && !modalObject.activeSelf && Session.Started;
            resultBanner.gameObject.SetActive(result);
            if(result)
            {
                resultBanner.anchoredPosition=new Vector2(12,-(h-284));resultBanner.sizeDelta=new Vector2(w-24,66);
                resultBanner.Find("Right star").GetComponent<RectTransform>().anchoredPosition=new Vector2(w-24-44,-21);
                var won=Session.Replay.State.Outcome==Outcome.Victory;var final=won && Session.EncounterIndex==Fixture.EncounterCount-1;
                resultTitle.text=won?"勝利！":"戦闘終了";resultTitle.rectTransform.sizeDelta=new Vector2(w-24-120,36);
                resultDetail.text=(final?"3つの戦いを越えた・":"")+alive+"人が生還";resultDetail.rectTransform.sizeDelta=new Vector2(w-24-120,22);
            }
            RefreshEventShelf();
            var pulse=1+.12f*Mathf.Sin(Time.unscaledTime*6);
            for(var i=0;i<crisisMarks.Count;i++)
            {
                var mark=crisisMarks[i];var on=CrisisPaused && !Busy && !modalObject.activeSelf && i<crisisAllies.Count && positions.ContainsKey(crisisAllies[i]);
                mark.gameObject.SetActive(on);if(!on)continue;
                var body=BodyRect(positions[crisisAllies[i]]);mark.rectTransform.anchoredPosition=new Vector2(body.center.x,body.yMax+16);
                mark.rectTransform.localScale=Vector3.one*pulse;mark.transform.SetAsLastSibling();
            }
            if(Session.EncounterIndex>0 || IsExploring)
            {
                var boundary=SafeRoot.Find("Boundary").GetComponent<Text>();
                if(CrisisPaused)boundary.text="危機で停止中：変えるか、そのまま再開する";
                else if(CanChallenge)boundary.text="守り手の気配がする部屋だ";
                else if(Session.EncounterIndex==Fixture.EncounterCount-1 && terminalSeen && Session.Replay.State.Outcome==Outcome.Victory)boundary.text="3つの戦いを越えた！";
                else if(GauntletRules && !Session.Started)boundary.text=(Session.EncounterIndex+1)+"戦目：モモの方針を選んで託す";
                else if(GauntletRules && Session.Started && !terminalSeen)boundary.text="修正 残り"+Session.Replay.CorrectionsLeft+"回・任意に指示できる";
            }
        }
    }
}
