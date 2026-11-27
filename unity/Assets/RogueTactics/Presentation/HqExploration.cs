using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core;
using RogueTactics.Core.Exploration;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private FloorMap floorMap;
        private bool descending;
        private GameObject firstFloorRuins;
        private HqFloorScenery floorScenery;
        private FloorTraveller traveller;
        private Gfx.HqSpringProp floorSpring;
        private HashSet<GridPosition> displayedKnown=new HashSet<GridPosition>();
        private FloorMapGraphic floorMini;
        private Coroutine travelRoutine;
        private LineRenderer selectedFloorMarker;
        public GridPosition? TravelSelection {get;private set;}
        private Vector3 cameraFromParty;
        private string travelMessage="見えている床・通路を選ぶ";
        public bool IsExploring { get; private set; }
        public FloorTraveller Exploration=>traveller;
        public bool TravelAnimating=>travelRoutine!=null;
        private IEnumerable<(GridPosition Position,bool Enemy)> KnownMapActors()
            =>positions.Where(entry=>!dead.Contains(entry.Key))
                .Select(entry=>(entry.Value,Session.Replay.State.Find(entry.Key).Enemy));
        private void BuildFloorScenery(GameObject startingRuins)
        {
            firstFloorRuins=startingRuins;
            floorMap=new FloorMap();floorScenery=HqFloorScenery.Build(floorMap,EnvironmentMeshes,RuinsTemplate,startingRuins);
            displayedKnown=new HashSet<GridPosition>(new FloorTraveller(floorMap,Session.Replay.State.Units,battleOrigin).KnownCells);
            floorScenery.Reveal(floorMap,displayedKnown);
        }
        private void BuildFloorUi()
        {
            MakeButton(SafeRoot,"Explore","探索を続ける",12,590,366,44,BeginExploration,16);
            MakeButton(SafeRoot,"Descend","2階へ降りる",12,590,366,48,Descend,16);
            StyleReferenceButton("Descend",HudSymbol.Descend,Gold);PrimaryFill("Descend");
            BuildRewardUi();
            var map=Rect(SafeRoot,"Explored floor map",254,65,124,104);
            map.gameObject.AddComponent<Image>().color=new Color(.035f,.09f,.13f,.87f);
            Label(map,"Known map caption","周辺地図",5,2,114,18,11);
            floorMini=Rect(map,"Known terrain",5,22,114,76).gameObject.AddComponent<FloorMapGraphic>();floorMini.raycastTarget=false;
            floorMini.Configure(floorMap,displayedKnown,KnownMapActors());
            ConfigureReferenceMap(map);
            fieldImage.raycastTarget=true;Field.gameObject.AddComponent<FloorPointer>().View=this;
            // A local ground outline for the player's accepted known tile. No route,
            // hidden corridor endpoint, future room or predicted party pose is drawn.
            selectedFloorMarker=new GameObject("Chosen known floor").AddComponent<LineRenderer>();
            selectedFloorMarker.transform.SetParent(FieldCamera.transform.parent,false);
            selectedFloorMarker.useWorldSpace=true;selectedFloorMarker.loop=true;selectedFloorMarker.positionCount=4;
            selectedFloorMarker.startWidth=selectedFloorMarker.endWidth=.075f;
            // Clone a scene-referenced shader: Shader.Find-only Unlit was stripped
            // from IL2CPP builds and aborted the first native UI initialization.
            var markerMaterial=new Material(SpriteTemplate){mainTexture=Texture2D.whiteTexture,renderQueue=2450};markerMaterial.SetColor("_Color",Gold);
            materials.Add(markerMaterial);selectedFloorMarker.sharedMaterial=markerMaterial;selectedFloorMarker.gameObject.SetActive(false);
            RefreshFloorUi();
        }
        private void ResetFloorExploration()
        {
            if(!ExplorationEnabled)return;
            if(travelRoutine!=null){StopCoroutine(travelRoutine);travelRoutine=null;}
            ClearRewardAltar();
            traveller?.Cancel();traveller=null;IsExploring=false;
            if(floorSpring!=null)floorSpring.gameObject.SetActive(false);visualTweenActive=false;
            TravelSelection=null;if(selectedFloorMarker!=null)selectedFloorMarker.gameObject.SetActive(false);
            if(eventBackdrop!=null){eventBackdrop.rectTransform.anchoredPosition=new Vector2(12,0);eventBackdrop.rectTransform.sizeDelta=new Vector2(SafeRoot.rect.width-140,48);}
            // A later gauntlet retry keeps the floor the party has already uncovered.
            if(floorMap!=null && Session.EncounterIndex==0)
            {
                displayedKnown=new HashSet<GridPosition>(new FloorTraveller(floorMap,Session.Replay.State.Units,battleOrigin).KnownCells);
                floorScenery.Reveal(floorMap,displayedKnown);
                if(floorMini!=null)floorMini.Configure(floorMap,displayedKnown,KnownMapActors());
            }
        }
        public void BeginExploration()
        {
            if(!ExplorationEnabled || IsExploring || Busy || !terminalSeen || Session.Replay.State.Outcome!=Outcome.Victory)return;
            if(UseVictoryRecovery)
            {
                StartCoroutine(RecoverAndExplore());
                return;
            }
            EnterExploration(new FloorTraveller(floorMap,Session.Replay.State.Units,battleOrigin));
            SaveSettledBattle();
        }

        private IEnumerator RecoverAndExplore()
        {
            var recovered=FloorTraveller.AfterVictory(floorMap,Session.Replay.State,battleOrigin,25);
            Busy=true;
            Refresh();
            foreach(var id in dead)
                if(recovered.Party.TryGetValue(id,out var cell) && cell.Equals(positions[id]))
                    RaiseFallen(id,true);
            if(rising.Count>0)yield return new WaitForSecondsRealtime(RiseSeconds);
            Busy=false;
            EnterExploration(recovered);
            SaveSettledBattle();
        }

        private void EnterExploration(FloorTraveller entered)
        {
            traveller=entered;
            // Projection follows the Core party while the battle replay keeps its final snapshot.
            IsExploring=true;Auto=false;Paused=false;modalObject.SetActive(false);ClearRelation();
            var entryCenter=PartyCenter();
            cameraFromParty=FieldCamera.transform.position-entryCenter;
            travelMessage=Session.FloorNumber<3 && NextEncounter==Session.EncounterCount && UseVictoryRecovery
                ?"通路の先に下り階段を探す":"見えている床・通路を選ぶ";
            // After a later encounter the party may stand at a board edge: frame them from the board's own offset.
            if(Session.EncounterIndex>0)cameraFromParty=FieldCamera.transform.position-World(Shown(new GridPosition(2,2)));
            foreach(var entry in traveller.Party)
            {
                positions[entry.Key]=entry.Value;
                hp[entry.Key]=traveller.Health[entry.Key];
                if(dead.Remove(entry.Key))ClearFallen(entry.Key);
            }
            RefreshActors();
            if(PartyCenter()!=entryCenter)FieldCamera.transform.position=PartyCenter()+cameraFromParty;
            SyncDiscoveredTerrain();Refresh();
        }
        private static readonly string[] SecondFloorRelics={BattleRules.WatchLantern,BattleRules.ThornCharm};
        private static readonly string[] SecondFloorRelicNames={"見張りの灯","荊棘の護符"};
        private static readonly string[] SecondFloorRelicRules={"モモが2マス先からシロを守れる","シロの反撃が2から3になる"};
        private Gfx.HqRewardAltar rewardAltar;
        private RectTransform rewardPanel;
        private RawImage heldRelicIcon;
        private Text heldRelicInfo;
        private bool showHeldRelicInfo;
        private float relicFlashUntil;
        private int selectedRelic=-1;
        private bool resolvingReward;
        public bool RewardPending=>IsExploring && Session.FloorNumber==2 &&
            NextEncounter==Session.EncounterCount && !Session.RewardResolved;

        private void BuildRewardUi()
        {
            rewardPanel=Rect(SafeRoot,"Expedition reward choice",12,0,366,170);
            Frame(rewardPanel,new Color(.035f,.065f,.11f,.98f),new Color(.60f,.72f,.84f),12);
            for(var i=0;i<SecondFloorRelics.Length;i++)
            {
                var option=i;
                var key="Relic"+option;
                MakeButton(rewardPanel,key,SecondFloorRelicNames[i]+"\n"+SecondFloorRelicRules[i],
                    8+i*175,8,173,90,()=>{selectedRelic=option;RefreshRewardUi();},12);
            }
            MakeButton(rewardPanel,"KeepRelic","これを持つ",8,112,173,48,()=>StartCoroutine(ResolveReward(selectedRelic)),15);
            MakeButton(rewardPanel,"DeclineRelic","持たずに進む",185,112,173,48,()=>StartCoroutine(ResolveReward(-1)),15);
            rewardPanel.gameObject.SetActive(false);
            MakeButton(SafeRoot,"HeldRelic","",0,190,44,44,()=>showHeldRelicInfo=!showHeldRelicInfo,12);
            heldRelicIcon=Rect(buttons["HeldRelic"].transform,"Relic art",2,2,40,40).gameObject.AddComponent<RawImage>();
            heldRelicIcon.raycastTarget=false;
            heldRelicInfo=Label(SafeRoot,"Held relic rule","",0,237,186,48,12,TextAnchor.MiddleRight);
        }

        private void RefreshRewardUi()
        {
            if(rewardPanel==null)return;
            RefreshHeldRelic();
            var visible=RewardPending && !modalObject.activeSelf && !resolvingReward;
            rewardPanel.gameObject.SetActive(visible);
            if(!visible)return;
            if(rewardAltar==null)
            {
                var center=PartyCenter()+Vector3.ProjectOnPlane(FieldCamera.transform.up,Vector3.up).normalized*2.5f;
                var right=FieldCamera.transform.right*2.1f;
                rewardAltar=Gfx.HqRewardAltar.Build(floorScenery.transform,center-right,center+right,
                    SecondFloorRelics[0],SecondFloorRelics[1],FieldCamera,vfx);
            }
            var width=SafeRoot.rect.width-24;
            var optionWidth=(width-20)/2;
            rewardPanel.anchoredPosition=new Vector2(12,-(SafeRoot.rect.height-382));
            rewardPanel.sizeDelta=new Vector2(width,170);
            for(var i=0;i<SecondFloorRelics.Length;i++)
            {
                var key="Relic"+i;
                ResizeButton(key,8+i*(optionWidth+4),8,optionWidth);
                buttons[key].GetComponent<Image>().color=i==selectedRelic?new Color(.25f,.30f,.37f):new Color(.12f,.19f,.24f);
            }
            ResizeButton("KeepRelic",8,112,optionWidth);
            ResizeButton("DeclineRelic",12+optionWidth,112,optionWidth);
            buttons["KeepRelic"].interactable=selectedRelic>=0;
        }

        private void RefreshHeldRelic()
        {
            var index=System.Array.IndexOf(SecondFloorRelics,Session.Relic);
            var visible=index>=0 && Session.FloorNumber>=3 && !modalObject.activeSelf && !policyEditing;
            var button=buttons["HeldRelic"];
            button.gameObject.SetActive(visible);
            heldRelicInfo.gameObject.SetActive(visible && showHeldRelicInfo);
            if(!visible)return;
            button.GetComponent<RectTransform>().anchoredPosition=new Vector2(SafeRoot.rect.width-56,-190);
            var texture=Gfx.HqRelicArt.Icon(Session.Relic);
            if(heldRelicIcon.texture!=texture)heldRelicIcon.texture=texture;
            button.GetComponent<Image>().color=Time.unscaledTime<relicFlashUntil
                ?Gfx.HqRelicArt.Accent(Session.Relic):new Color(.12f,.19f,.24f);
            heldRelicInfo.rectTransform.anchoredPosition=new Vector2(SafeRoot.rect.width-198,-237);
            heldRelicInfo.text=SecondFloorRelicNames[index]+"\n"+SecondFloorRelicRules[index];
        }

        private IEnumerator ResolveReward(int option)
        {
            if(!RewardPending || Busy || resolvingReward || rewardAltar==null)yield break;
            resolvingReward=true;Busy=true;Refresh();
            var delay=option<0?rewardAltar.Decline():rewardAltar.Choose(option,PartyCenter());
            yield return new WaitForSecondsRealtime(delay);
            Session.ResolveReward(option<0?"":SecondFloorRelics[option]);
            ClearRewardAltar();
            Busy=false;
            travelMessage="通路の先に下り階段を探す";
            SaveSettledBattle();
            Refresh();
        }

        private void ClearRewardAltar()
        {
            if(rewardAltar!=null){rewardAltar.gameObject.SetActive(false);Destroy(rewardAltar.gameObject);}
            rewardAltar=null;selectedRelic=-1;resolvingReward=false;
        }

        public bool DescentAvailable=>UseVictoryRecovery && IsExploring && !Busy && travelRoutine==null
            && !modalObject.activeSelf && Session.FloorNumber<3 && NextEncounter==Session.EncounterCount
            && !RewardPending
            && displayedKnown.Contains(floorMap.Stairs)
            && traveller.Party.Values.Any(cell=>Rules.Distance(cell,floorMap.Stairs)<=1);

        public void Descend()
        {
            if(DescentAvailable)StartCoroutine(DescendToNextFloor());
        }

        private IEnumerator DescendToNextFloor()
        {
            var next=new MobileG1Session(Session.FloorNumber+1,Session.Relic);
            next.LoadEncounter(0,traveller.HealthAfterDescent());
            next.Select(Session.Preference);
            Busy=true;descending=true;
            var allies=traveller.Party.Keys.ToArray();
            var landing=next.Replay.State.Units.Where(unit=>!unit.Enemy)
                .Select(unit=>World(unit.Position)).ToArray();
            var beats=Gfx.HqFloorDescent.Play(vfx,FieldCamera,next.FloorNumber,World(floorMap.Stairs),
                allies.Select(id=>pawns[id].position).ToArray(),landing,applyLook:false);
            Refresh();
            var elapsed=0f;
            while(elapsed<beats.Switch)
            {
                elapsed+=Time.unscaledDeltaTime;
                for(var i=0;i<allies.Length;i++)
                    if(elapsed>=beats.Taken[i])pawns[allies[i]].gameObject.SetActive(false);
                yield return null;
            }
            traveller.Cancel();traveller=null;
            descending=false;IsExploring=false;TravelSelection=null;selectedFloorMarker.gameObject.SetActive(false);
            Session=next;
            SwitchFloor();
            ProjectEncounter(false);
            floorMini.Configure(floorMap,displayedKnown,KnownMapActors());
            SetComparisonCamera(CameraPitch);
            foreach(var id in allies)pawns[id].gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(beats.Land-beats.Switch);
            foreach(var id in allies)pawns[id].gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(beats.End-beats.Land);
            Busy=false;
            Refresh();
            SaveSettledBattle();
        }

        private void SwitchFloor()
        {
            floorScenery.gameObject.SetActive(false);
            Destroy(floorScenery.gameObject);
            floorSpring=null;
            firstFloorRuins.SetActive(false);
            floorMap=new FloorMap(Session.FloorNumber);
            battleOrigin=GauntletRooms.Origin(floorMap,Session.EncounterIndex);
            displayedKnown=new HashSet<GridPosition>(new FloorTraveller(floorMap,Session.Replay.State.Units,battleOrigin).KnownCells);
            floorScenery=HqFloorScenery.Build(floorMap,EnvironmentMeshes,RuinsTemplate,null);
            floorScenery.Reveal(floorMap,displayedKnown);
            Gfx.HqFloorLook.Apply(Session.FloorNumber,FieldCamera);
        }

        private Vector3 PartyCenter()
        {
            var ids=traveller?.Party.Keys.ToArray()??new string[0];if(ids.Length==0)return World(new GridPosition(2,2));
            return ids.Select(id=>pawns[id].position).Aggregate(Vector3.zero,(sum,p)=>sum+p)/ids.Length;
        }
        private void SyncDiscoveredTerrain()
        {
            displayedKnown=new HashSet<GridPosition>(traveller.KnownCells);floorScenery.Reveal(floorMap,displayedKnown);
            var springVisible=UseVictoryRecovery && Session.EncounterIndex==1 && displayedKnown.Contains(floorMap.Spring);
            if(springVisible && floorSpring==null)
                floorSpring=Gfx.HqSpringProp.Build(floorScenery.transform,World(floorMap.Spring),FieldCamera);
            if(floorSpring!=null)
            {
                floorSpring.gameObject.SetActive(springVisible);
                if(springVisible && traveller.SpringUsed!=floorSpring.Spent)floorSpring.SetSpent(traveller.SpringUsed);
            }
            floorMini.Configure(floorMap,displayedKnown,KnownMapActors());
        }
        public bool EntrustTravel(GridPosition target)
        {
            if(!IsExploring || Busy || travelRoutine!=null || modalObject.activeSelf || RewardPending)return false;
            if(!traveller.Entrust(target)){travelMessage="見えている床・通路を選ぶ";return false;}
            TravelSelection=target;var at=World(target,.04f);
            selectedFloorMarker.SetPositions(new[]{at+new Vector3(-.95f,0,-1.2f),at+new Vector3(-.95f,0,1.2f),at+new Vector3(.95f,0,1.2f),at+new Vector3(.95f,0,-1.2f)});
            selectedFloorMarker.gameObject.SetActive(true);
            travelMessage=floorMap.RoomAt(target)<0?"通路の先へ移動中":"目的地へ移動中";
            travelRoutine=StartCoroutine(TravelFloor());return true;
        }
        public void TapFloor(Vector2 screen)
        {
            if(!IsExploring || modalObject.activeSelf || policyEditing || !RectTransformUtility.ScreenPointToLocalPointInRectangle(Field,screen,canvas.worldCamera,out var point))return;
            var ray=FieldCamera.ViewportPointToRay(new Vector3(point.x/Field.rect.width,1+point.y/Field.rect.height,0));
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out var distance))
            {
                var hit=ray.GetPoint(distance);EntrustTravel(new GridPosition(Mathf.RoundToInt(hit.x/Cell),Mathf.RoundToInt(hit.z/(Cell*DepthSpacing))));
            }
        }
        private IEnumerator TravelFloor()
        {
            // Floor Core commits a step; projection consumes each move before revealing
            // the newly discovered terrain. A paused tween retains pose and cursor.
            yield return null;
            while(traveller.Moving && IsExploring)
            {
                while(traveller.Paused)yield return null;
                var step=traveller.Advance();visualTweenActive=true;
                foreach(var move in step.Moves)
                {
                    var from=pawns[move.Actor].position;var to=World(move.To);var elapsed=0f;
                    while(elapsed<.18f)
                    {
                        while(traveller.Paused)yield return null;
                        elapsed+=Time.deltaTime;var t=Mathf.Clamp01(elapsed/.18f);pawns[move.Actor].position=Vector3.Lerp(from,to,t*t*(3-2*t));yield return null;
                    }
                    pawns[move.Actor].position=to;positions[move.Actor]=move.To;
                }
                visualTweenActive=false;SyncDiscoveredTerrain();RefreshActors();
                if(floorSpring!=null && floorSpring.gameObject.activeSelf && traveller.TryDrinkSpring())
                {
                    traveller.Cancel();
                    SaveSettledBattle();
                    yield return PresentSpringRecovery();
                    break;
                }
                SaveSettledBattle();
                if(step.Stop!=TravelStop.None)
                    travelMessage=step.Stop==TravelStop.NewRoom?"新しい部屋を見つけた":step.Stop==TravelStop.Arrived?"目的地に到着": "ここからは進めない";
            }
            travelRoutine=null;RefreshFloorUi();
        }
        private IEnumerator PresentSpringRecovery()
        {
            travelMessage="泉で体力を回復";
            RefreshFloorUi();
            var actors=traveller.Party.Keys.ToArray();
            var times=floorSpring.Drink(vfx,actors.Select(id=>World(positions[id])).ToArray());
            var shownAt=0f;
            for(var i=0;i<actors.Length;i++)
            {
                yield return new WaitForSecondsRealtime(times[i]-shownAt);
                shownAt=times[i];
                var id=actors[i];
                var restored=traveller.Health[id];
                PopEventNumber(id,EventKind.Heal,restored-hp[id]);
                hp[id]=restored;
                RefreshActors();
                RefreshProductHud();
            }
        }

        private void ToggleFloorPause()
        {if(traveller==null || travelRoutine==null)return;traveller.SetPaused(!traveller.Paused);RefreshFloorUi();}
        private void TickExploration()
        {
            if(!ExplorationEnabled || floorMini==null)return;
            if(IsExploring && !descending)
            {
                var follow=PartyCenter()+cameraFromParty;
                FieldCamera.transform.position=Session.EncounterIndex>0?Vector3.Lerp(FieldCamera.transform.position,follow,1-Mathf.Exp(-Time.deltaTime*5)):follow;
                RefreshActors();
            }
            floorMini.SetParty(KnownMapActors());
            RefreshFloorUi();
        }
        private void RefreshFloorUi()
        {
            if(!ExplorationEnabled || !buttons.ContainsKey("Explore"))return;
            RefreshRewardUi();
            buttons["Descend"].gameObject.SetActive(DescentAvailable);
            if(DescentAvailable)PositionReferenceAction("Descend",12,SafeRoot.rect.height-268,SafeRoot.rect.width-24,(Session.FloorNumber+1)+"階へ降りる");
            floorMini.transform.parent.gameObject.SetActive(!policyEditing && !modalObject.activeSelf);
            var w=SafeRoot.rect.width;var h=SafeRoot.rect.height;
            var explore=buttons["Explore"].GetComponent<RectTransform>();explore.anchoredPosition=new Vector2(12,-(h-205));explore.sizeDelta=new Vector2(w-24,44);
            explore.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(w-32,40);
            buttons["Explore"].interactable=!Busy;
            explore.gameObject.SetActive(!IsExploring && terminalSeen && Session.Replay.State.Outcome==Outcome.Victory);
            floorMini.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(w-136,-65);
            if(!IsExploring){RefreshReferenceFloorUi();return;}
            setup.gameObject.SetActive(false);orderStrip.gameObject.SetActive(false);buttons["Start"].gameObject.SetActive(false);
            SafeRoot.Find("Boundary").GetComponent<Text>().text="同じ遺跡を、仲間と探索する";
            buttons["Compare"].gameObject.SetActive(false);buttons["Reset"].gameObject.SetActive(true);ResizeButton("Reset",12,0,104);
            buttons["Reset"].GetComponentInChildren<Text>().text="戦闘を再試行";buttons["Reset"].GetComponentInChildren<Text>().fontSize=12;
            var moving=travelRoutine!=null;buttons["Play"].gameObject.SetActive(moving);buttons["Play"].interactable=moving;
            ResizeButton("Play",w-116,0,104);buttons["Play"].GetComponentInChildren<Text>().text=traveller.Paused?"再開する":"一時停止";
            eventCaption.gameObject.SetActive(true);eventBackdrop.gameObject.SetActive(true);
            eventCaption.rectTransform.anchoredPosition=new Vector2(126,-3);eventCaption.rectTransform.sizeDelta=new Vector2(w-250,44);eventCaption.fontSize=11;
            eventCaption.text=traveller.Paused?"移動を一時停止中":travelMessage;
            eventBackdrop.rectTransform.anchoredPosition=new Vector2(120,0);eventBackdrop.rectTransform.sizeDelta=new Vector2(w-240,48);
            RefreshReferenceFloorUi();
        }
    }
    public sealed class FloorPointer : MonoBehaviour,IPointerClickHandler
    {
        public MobileG1View View;
        public void OnPointerClick(PointerEventData pointer){if(pointer.dragging)return;View.TapFloor(pointer.position);}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FloorMapGraphic : MaskableGraphic
    {
        private FloorMap map;private HashSet<GridPosition> known=new HashSet<GridPosition>();
        private (GridPosition Position,bool Enemy)[] party=System.Array.Empty<(GridPosition,bool)>();
        public void Configure(FloorMap floor,IEnumerable<GridPosition> discovered,IEnumerable<(GridPosition Position,bool Enemy)> actors)
        {map=floor;known=new HashSet<GridPosition>(discovered);SetParty(actors);}
        public void SetParty(IEnumerable<(GridPosition Position,bool Enemy)> actors)
        {party=actors.ToArray();SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();if(map==null || known.Count==0)return;var rect=rectTransform.rect;
            // Frame only observed terrain: no hidden floor extent or tiny initial room.
            var minX=known.Min(p=>p.X);var minY=known.Min(p=>p.Y);
            var width=known.Max(p=>p.X)-minX+1;var height=known.Max(p=>p.Y)-minY+1;
            var size=Mathf.Min(rect.width/(width+2),rect.height/(height+2));
            var origin=rect.center-new Vector2(width,height)*size*.5f;
            void Square(GridPosition p,Color tint,float extent)
            {
                var center=origin+new Vector2(p.X-minX+.5f,p.Y-minY+.5f)*size;var start=helper.currentVertCount;
                foreach(var q in new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1)})helper.AddVert(center+q*extent,tint,Vector2.zero);
                helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start,start+2,start+3);
            }
            void Disc(GridPosition p,Color tint,float radius)
            {
                var center=origin+new Vector2(p.X-minX+.5f,p.Y-minY+.5f)*size;var start=helper.currentVertCount;helper.AddVert(center,tint,Vector2.zero);
                for(var k=0;k<16;k++){var a=k*Mathf.PI/8;helper.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,Vector2.zero);}
                for(var k=0;k<16;k++)helper.AddTriangle(start,start+1+k,start+1+(k+1)%16);
            }
            // Bevelled sunlit stone tiles over a dark joint; actors are rimmed glowing discs.
            foreach(var p in known)Square(p,new Color(.24f,.25f,.27f),size*.5f);
            foreach(var p in known)Square(p,new Color(.60f,.58f,.52f),size*.43f);
            foreach(var p in known){var c=origin+new Vector2(p.X-minX+.5f,p.Y-minY+.62f)*size;var start=helper.currentVertCount;
                foreach(var q in new[]{new Vector2(-.43f,-.3f),new Vector2(-.43f,.31f),new Vector2(.43f,.31f),new Vector2(.43f,-.3f)})helper.AddVert(c+q*size,new Color(.78f,.75f,.65f),Vector2.zero);
                helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start,start+2,start+3);}
            foreach(var a in party.Where(p=>known.Contains(p.Position)))
            {
                var tint=a.Enemy?new Color(1,.40f,.38f):new Color(.36f,.86f,1);
                Disc(a.Position,new Color(tint.r,tint.g,tint.b,.35f),size*.62f);Disc(a.Position,new Color(.02f,.04f,.08f,.95f),size*.40f);Disc(a.Position,tint,size*.32f);
            }
        }
    }
}
