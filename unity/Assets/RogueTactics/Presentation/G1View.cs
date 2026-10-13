using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RogueTactics.Core;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    // Controlled experiment adapter. Not final product chunk/horizon or character identity mapping.
    public sealed class G1View : MonoBehaviour
    {
        public Material PlaceholderTemplate;
        public Replay Battle { get; private set; }
        public Preference Selected { get; private set; }
        public bool Started { get; private set; }
        public bool Playing => playback != null;
        public IReadOnlyList<BattleEvent> ShownEvents => shown.AsReadOnly();
        public InputField Prediction { get; private set; }
        public string RecordedPrediction { get; private set; } = "";
        public Dictionary<Preference, string> Results { get; } = new Dictionary<Preference, string>();
        private readonly List<BattleEvent> shown = new List<BattleEvent>();
        private readonly Dictionary<string, int> hp = new Dictionary<string, int>();
        private readonly Dictionary<string, GridPosition> positions = new Dictionary<string, GridPosition>();
        private readonly Dictionary<string, Transform> pawns = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Text> labels = new Dictionary<string, Text>();
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        public Dictionary<Preference, string> Predictions { get; } = new Dictionary<Preference, string>();
        public Dictionary<Preference, string> TraceHashes { get; } = new Dictionary<Preference, string>();
        private Text rulesText, trialText;
        private Text planText, eventText, resultText, statusText;
        private RectTransform field, line, ghost;
        private Text semantic;
        private Camera fieldCamera;
        private Canvas canvas;
        private GameObject facts;
        private Coroutine playback;
        private RenderTexture fieldTarget;
        private Font font;
        private Material floorMaterial;
        private readonly List<Material> materials = new List<Material>();
        private static readonly Color Ink = new Color(.88f,.94f,.94f);
        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build(); ResetBaseline();
        }
        private void Build()
        {
            var uiCamera = Camera.main;
            uiCamera.cullingMask = 1 << 5;
            uiCamera.backgroundColor = new Color(.045f,.07f,.09f);
            canvas = new GameObject("G1 Portrait",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.gameObject.layer=5;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCamera; canvas.planeDistance = 5;
            var scale = canvas.GetComponent<CanvasScaler>(); scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution=new Vector2(390,844); scale.matchWidthOrHeight=0;
            var es = new GameObject("G1 Input",typeof(EventSystem),typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Label(canvas.transform,"Title","TACTICS LAB / G1",16,12,358,28,23);
            Label(canvas.transform,"Boundary","Anonymous A/B/C/D roles / fixed encounter\nTest playback + next plan; product UX undecided",16,43,358,34,12);
            CreateStage();
            field = Rect(canvas.transform,"Field",16,88,358,244);
            var raw=field.gameObject.AddComponent<RawImage>(); raw.texture=fieldTarget; raw.raycastTarget=false;
            foreach(var u in Fixture.Baseline().Units)
                labels[u.Id]=Label(field,"Unit "+u.Id,"",0,0,38,32,11,TextAnchor.MiddleCenter);
            line=Rect(field,"Core plan line",0,0,1,3); line.gameObject.AddComponent<Image>().color=new Color(.95f,.82f,.4f,.8f);
            line.GetComponent<Image>().raycastTarget=false; line.SetAsFirstSibling();
            ghost=Rect(field,"Core destination ghost",0,0,32,18); ghost.gameObject.AddComponent<Image>().color=new Color(.9f,.9f,.6f,.3f); ghost.GetComponent<Image>().raycastTarget=false;
            semantic=Label(field,"Core plan icon","",0,0,42,24,12,TextAnchor.MiddleCenter);
            Label(canvas.transform,"Axis","Change only B's preference",16,335,260,18,13);
            MakeButton("Facts","Rules",286,309,88,44,ToggleFacts,12);
            MakeButton("Protect","B: Protect A",16,358,175,46,()=>Select(Preference.ProtectAnchor));
            MakeButton("Attack","B: Direct attack",199,358,175,46,()=>Select(Preference.DirectAttack));
            var inputRect=Rect(canvas.transform,"Prediction",16,414,358,44);
            inputRect.gameObject.AddComponent<Image>().color=new Color(.12f,.18f,.22f);
            Prediction=inputRect.gameObject.AddComponent<InputField>();
            var inputText=Label(inputRect,"Value","",10,3,338,38,15); inputText.supportRichText=false;
            var placeholder=Label(inputRect,"Placeholder","Your prediction (optional)",10,3,338,38,15); placeholder.color=new Color(.55f,.65f,.69f);
            Prediction.textComponent=inputText; Prediction.placeholder=placeholder; Prediction.characterLimit=120;
            Prediction.lineType=InputField.LineType.SingleLine;
            MakeButton("Start","Start this setup",16,468,358,46,StartBattle);
            statusText=Label(canvas.transform,"Status","",16,518,358,18,12);
            planText=Label(canvas.transform,"Plan","",16,539,358,42,13);
            eventText=Label(canvas.transform,"Events","",16,586,358,66,13);
            MakeButton("Next","Next action",16,661,112,46,ResolveOne,13);
            MakeButton("Playback","Play to end",139,661,112,46,TogglePlayback,13);
            MakeButton("Reset","Baseline",262,661,112,46,ResetBaseline,13);
            resultText=Label(canvas.transform,"Compare","",16,716,358,91,13);
            Label(canvas.transform,"Limits","G1 experiment. Human play / phone not verified.",16,814,358,18,11);
            BuildFacts();
        }
        private void CreateStage()
        {
            fieldCamera=new GameObject("G1 Diorama Camera").AddComponent<Camera>();
            fieldCamera.orthographic=true; fieldCamera.orthographicSize=3.1f;
            fieldCamera.transform.position=new Vector3(2,12,-5); fieldCamera.transform.LookAt(new Vector3(2,0,2));
            fieldCamera.cullingMask=~(1<<5); fieldCamera.clearFlags=CameraClearFlags.SolidColor;
            fieldCamera.backgroundColor=new Color(.09f,.13f,.16f);
            fieldTarget=new RenderTexture(716,488,24); fieldCamera.targetTexture=fieldTarget;
            floorMaterial=Mat(new Color(.24f,.33f,.34f));
            var slab=GameObject.CreatePrimitive(PrimitiveType.Cube); slab.name="G1 Diorama floor";
            slab.transform.position=new Vector3(2,-.18f,2); slab.transform.localScale=new Vector3(5,.35f,5);
            slab.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            var light=new GameObject("G1 light").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.2f;
            light.transform.rotation=Quaternion.Euler(45,-30,0);
            foreach(var u in Fixture.Baseline().Units)
            {
                var pawn=GameObject.CreatePrimitive(PrimitiveType.Cylinder); pawn.name="Anonymous "+u.Id;
                pawn.transform.localScale=new Vector3(.48f,.38f,.48f);
                pawn.GetComponent<Renderer>().sharedMaterial=Mat(u.Enemy ? new Color(.78f,.34f,.28f) : new Color(.3f,.66f,.71f));
                pawns[u.Id]=pawn.transform;
            }
        }
        private Material Mat(Color color)
        {
            var m=new Material(PlaceholderTemplate); m.color=color; materials.Add(m); return m;
        }
        private void BuildFacts()
        {
            var panel=Rect(canvas.transform,"Rules panel",12,83,366,571); facts=panel.gameObject;
            facts.AddComponent<Image>().color=new Color(.065f,.105f,.135f,.99f);
            Label(panel,"Details title","G1 RULES",14,12,166,44,17);
            rulesText=Label(panel,"Rules text","A: HP18 / attack5 / living hit -> counter2\nB: HP16 / attack4 / protect reduces hit by3\nC: HP12 / attack4 / flank +2 when A also\n    borders that enemy from another position\nD: HP10 / attack2 / heal4, two charges\n    Range2; lowest HP ally missing at least4\nX: HP18 / attack6   Y: HP14 / attack5\n\nOrder: B > X > A > C > Y > D; skip dead\nEnemies target living A; else nearest / ID\nOthers attack nearest / ID or approach it\nMoves: empty adjacent tile, N/E/S/W tie\nGuard expires when B next acts; no guard\nif B dies or is no longer adjacent to A\nDead units cannot act, heal or counter\n\nOnly B's preference changes between setups.\nEach trial starts from the SAME baseline.\nHP, survivors, actions and charges are\nseparate measures; neither setup is ranked.\n\nStatic fixture roles are NOT named heroes.\nOne-action / full playback and next-plan\npreview are G1 adapters, NOT product defaults.",14,65,338,440,13);
            trialText=Label(panel,"Trial records","",14,65,338,440,14); trialText.supportRichText=false;
            MakeButton("Trials","Trials / predictions",180,12,172,44,ToggleTrials,13);
            buttons["Trials"].transform.SetParent(panel,false);
            buttons["Trials"].GetComponent<RectTransform>().anchoredPosition=new Vector2(180,-12);
            MakeButton("CloseFacts","Close rules",28,594,334,46,ToggleFacts);
            buttons["CloseFacts"].transform.SetParent(panel,false);
            var r=buttons["CloseFacts"].GetComponent<RectTransform>(); r.anchoredPosition=new Vector2(14,-515);
            facts.SetActive(false);
        }
        private void ToggleFacts()
        {
            if(!facts.activeSelf) { rulesText.gameObject.SetActive(true); trialText.gameObject.SetActive(false); }
            facts.SetActive(!facts.activeSelf);
        }
        private void ToggleTrials()
        {
            rulesText.gameObject.SetActive(!rulesText.gameObject.activeSelf); trialText.gameObject.SetActive(!rulesText.gameObject.activeSelf);
            trialText.text=Predictions.Count==0 ? "No completed trials yet." : string.Join("\n\n",
                new[]{Preference.ProtectAnchor,Preference.DirectAttack}.Where(p=>Predictions.ContainsKey(p)).Select(p=>
                Results[p]+"\nPrediction: "+(Predictions[p]=="" ? "(none entered)" : Predictions[p])+"\nTrace SHA256: "+TraceHashes[p].Substring(0,16)));
        }
        public Button Button(string id) => buttons[id];
        public void Select(Preference p)
        {
            if(Started) return;
            Selected=p; Prediction.text=""; ResetBaseline();
        }
        public void ResetBaseline()
        {
            StopPlayback(); Battle=new Replay(Fixture.Baseline(),Selected); Started=false; shown.Clear(); RecordedPrediction="";
            Prediction.text=""; hp.Clear(); positions.Clear();
            foreach(var u in Battle.State.Units) { hp[u.Id]=u.Hp; positions[u.Id]=u.Position; }
            Refresh();
        }
        public void StartBattle()
        {
            if(Started) return;
            RecordedPrediction=Prediction.text; Started=true; ResolveOne();
        }
        public void ResolveOne()
        {
            if(!Started || Battle.State.Outcome!=Outcome.Active) return;
            var result=Battle.Advance();
            foreach(var e in result.Events)
            {
                shown.Add(e);
                if(e.Kind==EventKind.Damage || e.Kind==EventKind.Counter || e.Kind==EventKind.Heal || e.Kind==EventKind.Death) hp[e.Target]=e.HpAfter;
                if(e.Kind==EventKind.Move) positions[e.Target]=e.Position;
            }
            // A strict observation boundary: displayed changes were applied only from Core events.
            foreach(var u in Battle.State.Units)
                if(hp[u.Id]!=u.Hp || !positions[u.Id].Equals(u.Position)) throw new InvalidOperationException("Core/render projection diverged");
            if(Battle.State.Outcome!=Outcome.Active)
            {
                Results[Selected]=ResultSummary(Battle);
                Predictions[Selected]=RecordedPrediction; TraceHashes[Selected]=Battle.Hash();
                Debug.Log("G1_RESULT "+Selected+" "+Battle.Hash()+" "+Battle.State.Serialize());
            }
            Refresh();
        }
        private string ResultSummary(Replay r)
        {
            var s=r.State;
            return (Selected==Preference.ProtectAnchor ? "Protect" : "Attack")+": "+s.Outcome+" / "+s.Step+" actions\n"+
                "HP "+string.Join(" ",s.Units.Where(u=>!u.Enemy).Select(u=>u.Id+":"+u.Hp))+" / alive "+s.Units.Count(u=>!u.Enemy && u.Alive)+" / heals "+s.HealCharges;
        }
        private void TogglePlayback()
        {
            if(Playing) { StopPlayback(); Refresh(); return; }
            if(Started && Battle.State.Outcome==Outcome.Active) playback=StartCoroutine(PlayEncounter());
            Refresh();
        }
        private IEnumerator PlayEncounter()
        {
            // Presentation delay only; each resolved Core step is rendered before the next explicit adapter step.
            while(Battle.State.Outcome==Outcome.Active) { ResolveOne(); yield return new WaitForSeconds(.45f); }
            playback=null; Refresh();
        }
        private void StopPlayback() { if(playback!=null) StopCoroutine(playback); playback=null; }
        private void Refresh()
        {
            foreach(var u in Battle.State.Units)
            {
                var p=positions[u.Id]; pawns[u.Id].position=new Vector3(p.X,.38f,p.Y); pawns[u.Id].gameObject.SetActive(hp[u.Id]>0);
                var at=FieldPoint(p); labels[u.Id].rectTransform.anchoredPosition=new Vector2(at.x-19,-at.y+14);
                labels[u.Id].gameObject.SetActive(hp[u.Id]>0);
                labels[u.Id].text=u.Id+"\n"+hp[u.Id]+"/"+u.MaxHp;
                labels[u.Id].color=hp[u.Id]>0 ? Ink : new Color(.7f,.5f,.48f);
            }
            var plan=Rules.Preview(Battle.State,new Command(Selected));
            line.gameObject.SetActive(plan!=null && plan.Kind!=ActionKind.Maintain); ghost.gameObject.SetActive(plan?.Kind==ActionKind.Move);
            semantic.gameObject.SetActive(plan!=null);
            if(plan!=null)
            {
                var from=FieldPoint(positions[plan.Actor]);
                var to=FieldPoint(plan.Kind==ActionKind.Move ? plan.Destination : (plan.Target!="" ? positions[plan.Target] : positions[plan.Actor]));
                var delta=new Vector2(to.x-from.x,from.y-to.y);
                line.pivot=new Vector2(0,.5f); line.anchoredPosition=new Vector2(from.x,-from.y); line.sizeDelta=new Vector2(delta.magnitude,3);
                line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                ghost.anchoredPosition=new Vector2(to.x-16,-to.y+9);
                semantic.rectTransform.anchoredPosition=new Vector2(8,-8);
                semantic.text=plan.Kind==ActionKind.Attack ? "ATK" : plan.Kind==ActionKind.Protect ? "GRD" : plan.Kind==ActionKind.Heal ? "HEAL" : plan.Kind==ActionKind.Move ? "MOVE" : "HOLD";
            }
            planText.text=plan==null ? "Encounter complete. Compare, then return to baseline." :
                "Core next: "+plan.Actor+" / "+plan.Kind+(plan.Target!="" ? " -> "+plan.Target : "")+"\n"+plan.Reason;
            statusText.text=Started ? "Action "+Battle.State.Step+" / "+Battle.State.Outcome+" / heal charges "+Battle.State.HealCharges : "Setup / same baseline / only B preference differs";
            var recent=shown.Where(e=>e.Kind!=EventKind.Action).Reverse().Take(3).Reverse().Select(Describe);
            eventText.text=shown.Count==0 ? "Core events appear here after you start." : string.Join("\n",recent);
            resultText.text=Results.Count==0 ? "Comparison: finish each setup to retain its result.\nReturn to Baseline, change B, predict and start again." :
                string.Join("\n",new[]{Preference.ProtectAnchor,Preference.DirectAttack}.Where(p=>Results.ContainsKey(p)).Select(p=>Results[p]));
            buttons["Protect"].interactable=!Started; buttons["Attack"].interactable=!Started; Prediction.interactable=!Started;
            buttons["Start"].interactable=!Started;
            buttons["Next"].interactable=Started && Battle.State.Outcome==Outcome.Active && !Playing;
            buttons["Playback"].interactable=Started && Battle.State.Outcome==Outcome.Active;
            buttons["Playback"].GetComponentInChildren<Text>().text=Playing ? "Pause" : "Play to end";
            buttons["Protect"].GetComponent<Image>().color=Selected==Preference.ProtectAnchor ? new Color(.23f,.39f,.43f) : new Color(.12f,.22f,.27f);
            buttons["Attack"].GetComponent<Image>().color=Selected==Preference.DirectAttack ? new Color(.23f,.39f,.43f) : new Color(.12f,.22f,.27f);
        }
        private Vector2 FieldPoint(GridPosition p)
        { var v=fieldCamera.WorldToViewportPoint(new Vector3(p.X,.6f,p.Y)); return new Vector2(v.x*358,(1-v.y)*244); }
        public static string Describe(BattleEvent e)
        {
            if(e.Kind==EventKind.Damage || e.Kind==EventKind.Counter) return e.Step+" "+e.Actor+" -> "+e.Target+" "+e.Kind+" -"+e.Amount+" (HP "+e.HpAfter+") "+(e.Detail.StartsWith("Guard") ? "[guard]" : e.Detail=="Flank +2" ? "[flank]" : "");
            if(e.Kind==EventKind.Heal) return e.Step+" "+e.Actor+" -> "+e.Target+" Heal +"+e.Amount+" (HP "+e.HpAfter+")";
            if(e.Kind==EventKind.Move) return e.Step+" "+e.Actor+" Move ("+e.Position.X+","+e.Position.Y+")";
            if(e.Kind==EventKind.Protect) return e.Step+" "+e.Actor+" -> "+e.Target+" Protect / reduction "+e.Amount;
            return e.Step+" "+e.Target+" "+e.Kind+" "+e.Detail;
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
            r.gameObject.layer=5; r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
        }
        private Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size,TextAnchor align=TextAnchor.UpperLeft)
        {
            var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>(); t.font=font; t.text=value; t.fontSize=size; t.color=Ink;
            t.alignment=align; t.raycastTarget=false; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; return t;
        }
        private void MakeButton(string id,string title,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,int size=15)
        {
            var r=Rect(canvas.transform,id,x,y,w,h); var image=r.gameObject.AddComponent<Image>(); image.color=new Color(.12f,.22f,.27f);
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image; b.onClick.AddListener(action); buttons[id]=b;
            Label(r,"Label",title,4,2,w-8,h-4,size,TextAnchor.MiddleCenter);
        }
        public void SaveTrace(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path,Battle.Serialize());
        }
        private void OnDestroy()
        { StopPlayback(); if(fieldTarget!=null) { fieldTarget.Release(); Destroy(fieldTarget); } foreach(var m in materials) Destroy(m); }
    }
}
