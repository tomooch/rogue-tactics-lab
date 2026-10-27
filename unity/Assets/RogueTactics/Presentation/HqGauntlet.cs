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
            MakeButton(SafeRoot,"Correct","",12,0,366,48,CorrectAtCrisis,13);
            StyleReferenceButton("Correct",HudSymbol.Shield,new Color(1,.46f,.42f));
            var panel=buttons["Correct"].GetComponentInChildren<HudPanelGraphic>();panel.Fill=new Color(.30f,.07f,.08f,.97f);panel.SetVerticesDirty();
            for(var i=0;i<6;i++)
            {var strip=Strip(Field,"Round intent "+i,Color.white);var g=strip.GetComponent<MobileRouteGraphic>();g.Dashed=true;strip.gameObject.SetActive(false);roundIntents.Add(g);}
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
        // True when auto play has just stopped for a newly threatened ally.
        private bool CrisisStop()
        {
            if(!GauntletRules || Session.Replay.CorrectionsLeft<1){crisisActive=false;return false;}
            var threatened=Rules.Threatened(Session.Replay.State,new Command(Session.Replay.Preference));
            var arrived=threatened.Count>0 && !crisisActive;crisisActive=threatened.Count>0;crisisAllies=threatened;
            if(!arrived)return false;
            Auto=false;Paused=true;Refresh();return true;
        }
        private Preference Other(Preference p)=>p==Preference.ProtectAnchor?Preference.DirectAttack:Preference.ProtectAnchor;
        public void CorrectAtCrisis()
        {
            if(!CrisisPaused || Busy || Session.Replay.CorrectionsLeft<1)return;
            Session.Correct(Other(Session.Preference));
            captions.Add("方針を変更：モモは"+(Session.Preference==Preference.ProtectAnchor?"シロを守る":"敵を攻める"));
            crisisActive=false;Auto=true;Paused=false;Refresh();Next();
        }
        private void RefreshRoundIntents()
        {
            if(roundIntents.Count==0)return;
            var show=GauntletRules && Session.Started && !Busy && !terminalSeen && !modalObject.activeSelf && !IsExploring;
            var plans=show?Rules.Intents(Session.Replay.State,new Command(Session.Replay.Preference)).Where(p=>p.Kind!=ActionKind.Maintain).ToList():new List<Plan>();
            for(var i=0;i<roundIntents.Count;i++)
            {
                var g=roundIntents[i];var active=i<plans.Count;g.gameObject.SetActive(active);if(!active)continue;
                var p=plans[i];var actor=Session.Replay.State.Find(p.Actor);
                // First actor is the next to move: full strength. Later ones are fainter: still their intent, not a forecast.
                var tint=actor.Enemy?new Color(1,.42f,.40f):new Color(.40f,.82f,1);tint.a=i==0?1:.62f;g.color=tint;
                // Straight and short: each intent reads as actor -> target, never a detour off the board.
                var from=BodyRect(positions[p.Actor]);var to=p.Kind==ActionKind.Move?Point(Shown(p.Destination),.05f):BodyRect(positions[p.Target]).center;
                var d=to-from.center;if(d.sqrMagnitude<1){g.gameObject.SetActive(false);continue;}
                g.rectTransform.anchoredPosition=Vector2.zero;g.rectTransform.sizeDelta=Field.rect.size;
                g.SetPath(new[]{Outside(from,d),p.Kind==ActionKind.Move?to:Outside(BodyRect(positions[p.Target]),-d)});
            }
        }
        private void RefreshGauntletUi()
        {
            if(!ExplorationEnabled || !HqWorld || DeveloperMode)return;
            EnsureGauntletUi();
            var w=SafeRoot.rect.width;var h=SafeRoot.rect.height;
            var challenge=buttons["Challenge"];challenge.gameObject.SetActive(CanChallenge && !enteringEncounter);
            if(challenge.gameObject.activeSelf)PositionReferenceAction("Challenge",12,h-268,w-24,"守り手に挑む（"+(NextEncounter+1)+" / "+Fixture.EncounterCount+"戦目）");
            var correct=buttons["Correct"];correct.gameObject.SetActive(CrisisPaused && !Busy && !modalObject.activeSelf);
            if(correct.gameObject.activeSelf)
            {
                var names=string.Join("・",crisisAllies.Select(HeroName));
                PositionReferenceAction("Correct",12,h-268,w-24,names+"が危ない！ モモを「"+(Session.Preference==Preference.ProtectAnchor?"敵を攻める":"シロを守る")+"」に変える（残り1回）");
                correct.transform.Find("Label").GetComponent<Text>().fontSize=12;
            }
            if(Session.EncounterIndex>0 || IsExploring)
            {
                var boundary=SafeRoot.Find("Boundary").GetComponent<Text>();
                if(CrisisPaused)boundary.text="危機で停止中：変えるか、そのまま再開する";
                else if(CanChallenge)boundary.text="守り手の気配がする部屋だ";
                else if(Session.EncounterIndex==Fixture.EncounterCount-1 && terminalSeen && Session.Replay.State.Outcome==Outcome.Victory)boundary.text="3つの戦いを越えた！";
                else if(GauntletRules && !Session.Started)boundary.text=(Session.EncounterIndex+1)+"戦目：モモの方針を選んで託す";
                else if(GauntletRules && Session.Started && !terminalSeen)boundary.text="修正 残り"+Session.Replay.CorrectionsLeft+"回・危機で止まる";
            }
        }
    }
}
