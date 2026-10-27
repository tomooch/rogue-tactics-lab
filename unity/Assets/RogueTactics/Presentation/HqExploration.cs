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
        private HqFloorScenery floorScenery;
        private FloorTraveller traveller;
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
        private void BuildFloorScenery(GameObject startingRuins)
        {
            floorMap=new FloorMap();floorScenery=HqFloorScenery.Build(floorMap,EnvironmentMeshes,RuinsTemplate,startingRuins);
            displayedKnown=new HashSet<GridPosition>(new FloorTraveller(floorMap,Fixture.Baseline().Units).KnownCells);
            floorScenery.Reveal(floorMap,displayedKnown);
        }
        private void BuildFloorUi()
        {
            MakeButton(SafeRoot,"Explore","探索を続ける",12,590,366,44,BeginExploration,16);
            var map=Rect(SafeRoot,"Explored floor map",254,65,124,104);
            map.gameObject.AddComponent<Image>().color=new Color(.035f,.09f,.13f,.87f);
            Label(map,"Known map caption","周辺地図",5,2,114,18,11);
            floorMini=Rect(map,"Known terrain",5,22,114,76).gameObject.AddComponent<FloorMapGraphic>();floorMini.raycastTarget=false;
            floorMini.Configure(floorMap,displayedKnown,positions.Where(p=>!dead.Contains(p.Key)));
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
            traveller?.Cancel();traveller=null;IsExploring=false;visualTweenActive=false;
            TravelSelection=null;if(selectedFloorMarker!=null)selectedFloorMarker.gameObject.SetActive(false);
            if(eventBackdrop!=null){eventBackdrop.rectTransform.anchoredPosition=new Vector2(12,0);eventBackdrop.rectTransform.sizeDelta=new Vector2(SafeRoot.rect.width-140,48);}
            // A later gauntlet retry keeps the floor the party has already uncovered.
            if(floorMap!=null && Session.EncounterIndex==0)
            {
                displayedKnown=new HashSet<GridPosition>(new FloorTraveller(floorMap,Fixture.Baseline().Units).KnownCells);
                floorScenery.Reveal(floorMap,displayedKnown);
                if(floorMini!=null)floorMini.Configure(floorMap,displayedKnown,positions.Where(p=>!dead.Contains(p.Key)));
            }
        }
        public void BeginExploration()
        {
            if(!ExplorationEnabled || IsExploring || Busy || !terminalSeen || Session.Replay.State.Outcome!=Outcome.Victory)return;
            traveller=new FloorTraveller(floorMap,Session.Replay.State.Units,battleOrigin);
            // This handoff uses the exact post-fight positions/HP/survivors. No formation
            // reset, revival, separate scene, or camera cut at the exploration boundary.
            IsExploring=true;Auto=false;Paused=false;modalObject.SetActive(false);ClearRelation();
            cameraFromParty=FieldCamera.transform.position-PartyCenter();travelMessage="見えている床・通路を選ぶ";
            // After a later encounter the party may stand at a board edge: frame them from the board's own offset.
            if(Session.EncounterIndex>0)cameraFromParty=FieldCamera.transform.position-World(Shown(new GridPosition(2,2)));
            SyncDiscoveredTerrain();Refresh();
        }
        private Vector3 PartyCenter()
        {
            var ids=traveller?.Party.Keys.ToArray()??new string[0];if(ids.Length==0)return World(new GridPosition(2,2));
            return ids.Select(id=>pawns[id].position).Aggregate(Vector3.zero,(sum,p)=>sum+p)/ids.Length;
        }
        private void SyncDiscoveredTerrain()
        {
            displayedKnown=new HashSet<GridPosition>(traveller.KnownCells);floorScenery.Reveal(floorMap,displayedKnown);
            floorMini.Configure(floorMap,displayedKnown,positions.Where(p=>!dead.Contains(p.Key)));
        }
        public bool EntrustTravel(GridPosition target)
        {
            if(!IsExploring || travelRoutine!=null || modalObject.activeSelf)return false;
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
                if(step.Stop!=TravelStop.None)
                    travelMessage=step.Stop==TravelStop.NewRoom?"新しい部屋を見つけた":step.Stop==TravelStop.Arrived?"目的地に到着": "ここからは進めない";
            }
            travelRoutine=null;RefreshFloorUi();
        }
        private void ToggleFloorPause()
        {if(traveller==null || travelRoutine==null)return;traveller.SetPaused(!traveller.Paused);RefreshFloorUi();}
        private void TickExploration()
        {
            if(!ExplorationEnabled || floorMini==null)return;
            if(IsExploring)
            {
                var follow=PartyCenter()+cameraFromParty;
                FieldCamera.transform.position=Session.EncounterIndex>0?Vector3.Lerp(FieldCamera.transform.position,follow,1-Mathf.Exp(-Time.deltaTime*5)):follow;
                RefreshActors();
            }
            floorMini.SetParty(positions.Where(p=>!dead.Contains(p.Key)));
            RefreshFloorUi();
        }
        private void RefreshFloorUi()
        {
            if(!ExplorationEnabled || !buttons.ContainsKey("Explore"))return;
            floorMini.transform.parent.gameObject.SetActive(!policyEditing && !modalObject.activeSelf);
            var w=SafeRoot.rect.width;var h=SafeRoot.rect.height;
            var explore=buttons["Explore"].GetComponent<RectTransform>();explore.anchoredPosition=new Vector2(12,-(h-205));explore.sizeDelta=new Vector2(w-24,44);
            explore.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(w-32,40);
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
        private KeyValuePair<string,GridPosition>[] party=new KeyValuePair<string,GridPosition>[0];
        public void Configure(FloorMap floor,IEnumerable<GridPosition> discovered,IEnumerable<KeyValuePair<string,GridPosition>> actors)
        {map=floor;known=new HashSet<GridPosition>(discovered);SetParty(actors);}
        public void SetParty(IEnumerable<KeyValuePair<string,GridPosition>> actors)
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
            foreach(var p in known)Square(p,new Color(.56f,.65f,.66f),size*.46f);
            foreach(var a in party.Where(p=>known.Contains(p.Value)))Square(a.Value,a.Key=="X" || a.Key=="Y"?new Color(.94f,.43f,.43f):new Color(.35f,.85f,1),size*.30f);
        }
    }
}
