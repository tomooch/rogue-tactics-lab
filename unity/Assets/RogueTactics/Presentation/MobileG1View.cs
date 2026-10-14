using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed class MobileG1View : MonoBehaviour
    {
        public Font JapaneseFont;
        public Material StoneTemplate, SpriteTemplate;
        // Private textures supplied locally by setup; never public-Git originals.
        public Texture2D[] CharacterArt = new Texture2D[4]; // A Shiro / B Momo / C Garu / D Myaru: temporary skins only
        public MobileG1Session Session { get; } = new MobileG1Session();
        public bool Busy { get; private set; }
        public bool Auto { get; private set; }
        public bool Paused { get; private set; }
        public InputField Prediction { get; private set; }
        public RectTransform SafeRoot { get; private set; }
        public RectTransform Field { get; private set; }
        public Camera FieldCamera { get; private set; }
        public IReadOnlyList<BattleEvent> DisplayedEvents => displayed.AsReadOnly();
        private readonly Dictionary<string,int> hp=new Dictionary<string,int>();
        private readonly Dictionary<string,GridPosition> positions=new Dictionary<string,GridPosition>();
        private readonly Dictionary<string,Transform> pawns=new Dictionary<string,Transform>();
        private readonly Dictionary<string,CanvasGroup> badges=new Dictionary<string,CanvasGroup>();
        private readonly Dictionary<string,Text> hpTexts=new Dictionary<string,Text>();
        private readonly Dictionary<string,Image> hpBars=new Dictionary<string,Image>();
        private readonly Dictionary<string,Button> buttons=new Dictionary<string,Button>();
        private readonly List<Material> materials=new List<Material>();
        private readonly List<BattleEvent> displayed=new List<BattleEvent>();
        private readonly List<string> captions=new List<string>();
        private readonly HashSet<string> dead=new HashSet<string>();
        private Canvas canvas;
        private RenderTexture fieldTarget;
        private RectTransform controls, setup, battleControls, modal, detailsViewport, guardLink, actionLink, ghost, shield;
        private Text phase, intent, eventCaption, modalText, headline, floating;
        private Text artNotice;
        private RawImage fieldImage;
        private GameObject modalObject;
        private Rect safeRect;
        private int screenW, screenH;
        private Plan executingPlan;
        private bool terminalSeen, testViewport;
        private int shownStep;
        private float floatUntil;
        private Vector2 floatAt;
        private const float Cell=1.6f;
        private static readonly Color Ink=new Color(.93f,.94f,.88f), Gold=new Color(.96f,.79f,.40f), Jade=new Color(.35f,.85f,.76f);
        public Button Button(string id) => buttons[id];
        public bool HasAllArt => CharacterArt.Length==4 && CharacterArt.All(t=>t!=null);
        private void Awake()
        {
            Application.targetFrameRate=60;
            Build(); ResetBaseline();
        }
        private void Build()
        {
            var ui=Camera.main;ui.cullingMask=1<<5;ui.backgroundColor=new Color(.035f,.06f,.075f);
            canvas=new GameObject("Mobile Japanese HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.gameObject.layer=5;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=5;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(390,844);scaler.matchWidthOrHeight=0;
            var es=new GameObject("Mobile Input",typeof(EventSystem),typeof(InputSystemUIInputModule));es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            SafeRoot=Rect(canvas.transform,"SafeArea",0,0,390,844);SafeRoot.anchorMin=Vector2.zero;SafeRoot.anchorMax=Vector2.one;SafeRoot.offsetMin=SafeRoot.offsetMax=Vector2.zero;
            Label(SafeRoot,"Title","遺跡の戦術実験",16,0,250,42,20);
            Label(SafeRoot,"Boundary","G1  /  A–Dは実験役・外見は仮対応",16,39,312,21,12);
            MakeButton(SafeRoot,"Details","詳細",322,6,56,44,()=>OpenDetails(false),14);
            BuildStage();
            Field=Rect(SafeRoot,"Field",0,60,390,520);fieldImage=Field.gameObject.AddComponent<RawImage>();fieldImage.texture=fieldTarget;fieldImage.raycastTarget=false;
            guardLink=Strip(Field,"Guard link",Jade);actionLink=Strip(Field,"Core intention",Gold);
            ghost=Rect(Field,"Legal destination",0,0,34,18);ghost.gameObject.AddComponent<Image>().color=new Color(.8f,.95f,.8f,.25f);ghost.GetComponent<Image>().raycastTarget=false;
            foreach(var u in Fixture.Baseline().Units)
            {
                var r=Rect(Field,"HP "+u.Id,0,0,62,32);r.gameObject.AddComponent<Image>().color=new Color(.025f,.06f,.065f,.87f);
                badges[u.Id]=r.gameObject.AddComponent<CanvasGroup>();
                hpTexts[u.Id]=Label(r,"Value",u.Id,2,0,58,20,13,TextAnchor.MiddleCenter);
                var bar=Rect(r,"HP bar",4,23,54,5).gameObject.AddComponent<Image>();bar.color=u.Enemy?new Color(.92f,.44f,.38f):Jade;
                hpBars[u.Id]=bar;
            }
            shield=Rect(Field,"Protect",0,0,40,38);shield.gameObject.AddComponent<Image>().color=new Color(.1f,.7f,.7f,.4f);
            Label(shield,"Shield","守",0,0,40,38,19,TextAnchor.MiddleCenter);
            floating=Label(Field,"Event feedback","",0,0,110,40,23,TextAnchor.MiddleCenter);
            artNotice=Label(Field,"Missing original","承認原画未設定 — V0未達",14,10,362,26,14);artNotice.gameObject.SetActive(!HasAllArt);
            controls=Rect(SafeRoot,"Controls",0,580,390,244);
            phase=Label(controls,"Phase","",16,0,358,22,14);
            setup=Rect(controls,"Setup",0,26,390,162);
            MakeButton(setup,"Protect","B：Aを守る",12,0,180,46,()=>Select(Preference.ProtectAnchor),17);
            MakeButton(setup,"Attack","B：直接攻撃",198,0,180,46,()=>Select(Preference.DirectAttack),17);
            var input=Rect(setup,"Prediction",12,54,366,44);input.gameObject.AddComponent<Image>().color=new Color(.1f,.16f,.19f);
            Prediction=input.gameObject.AddComponent<InputField>();var value=Label(input,"Value","",10,2,346,40,16);value.supportRichText=false;
            Prediction.textComponent=value;Prediction.placeholder=Label(input,"Placeholder","自分の予想（任意）",10,2,346,40,16);Prediction.placeholder.color=new Color(.61f,.71f,.70f);
            Prediction.characterLimit=120;Prediction.lineType=InputField.LineType.SingleLine;
            MakeButton(setup,"Start","この条件で戦闘開始",12,108,366,48,StartBattle,18);
            battleControls=Rect(controls,"Battle",0,26,390,164);
            eventCaption=Label(battleControls,"Events","",16,0,358,54,17);
            intent=Label(battleControls,"Plan","",16,59,358,40,14);
            MakeButton(battleControls,"Next","次の行動",12,108,136,48,Next,17);
            MakeButton(battleControls,"Play","続けて見る",156,108,136,48,ToggleAuto,17);
            MakeButton(battleControls,"Reset","戻す",300,108,78,48,ResetBaseline,16);
            MakeButton(controls,"Compare","結果・予想を比較",12,193,366,44,()=>OpenDetails(true),15);
            modal=Rect(SafeRoot,"Details panel",10,66,370,628);modalObject=modal.gameObject;
            modalObject.AddComponent<Image>().color=new Color(.055f,.095f,.12f,.995f);
            headline=Label(modal,"Heading","実験の詳細",18,12,334,32,20);
            detailsViewport=Rect(modal,"Details viewport",18,54,334,502);
            detailsViewport.gameObject.AddComponent<Image>().color=Color.white;detailsViewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            modalText=Label(detailsViewport,"Body","",0,0,334,502,16);modalText.supportRichText=false;
            modalText.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=detailsViewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=detailsViewport;scroll.content=modalText.rectTransform;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            MakeButton(modal,"Close","閉じる",18,570,334,46,()=>modalObject.SetActive(false),17);modalObject.SetActive(false);
            ApplySafeArea(Screen.safeArea,Screen.width,Screen.height);
        }
        public void ApplySafeArea(Rect area,int width,int height)
        {
            if(width<1 || height<1)return;
            safeRect=area;screenW=width;screenH=height;
            SafeRoot.anchorMin=new Vector2(area.x/width,area.y/height);SafeRoot.anchorMax=new Vector2(area.xMax/width,area.yMax/height);SafeRoot.offsetMin=SafeRoot.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();var h=SafeRoot.rect.height;var w=SafeRoot.rect.width;
            var fieldH=Mathf.Max(280,h-310);Field.sizeDelta=new Vector2(w,fieldH);controls.anchoredPosition=new Vector2(0,-(60+fieldH+8));
            var modalH=Mathf.Min(628,h-90);modal.sizeDelta=new Vector2(w-20,modalH);
            detailsViewport.sizeDelta=new Vector2(w-56,modalH-126);modalText.rectTransform.sizeDelta=new Vector2(w-56,modalText.rectTransform.sizeDelta.y);
            headline.rectTransform.sizeDelta=new Vector2(w-56,32);
            setup.sizeDelta=new Vector2(w,162);battleControls.sizeDelta=new Vector2(w,164);controls.sizeDelta=new Vector2(w,244);
            phase.rectTransform.sizeDelta=new Vector2(w-32,22);eventCaption.rectTransform.sizeDelta=new Vector2(w-32,54);intent.rectTransform.sizeDelta=new Vector2(w-32,40);
            ResizeButton("Protect",12,0,(w-30)/2);ResizeButton("Attack",18+(w-30)/2,0,(w-30)/2);
            Prediction.GetComponent<RectTransform>().sizeDelta=new Vector2(w-24,44);Prediction.textComponent.rectTransform.sizeDelta=new Vector2(w-44,40);
            Prediction.placeholder.GetComponent<RectTransform>().sizeDelta=new Vector2(w-44,40);
            ResizeButton("Start",12,108,w-24);ResizeButton("Next",12,108,(w-118)/2);ResizeButton("Play",20+(w-118)/2,108,(w-118)/2);ResizeButton("Reset",w-90,108,78);
            ResizeButton("Compare",12,193,w-24);ResizeButton("Close",18,modalH-58,w-56);ResizeButton("Details",w-68,6,56);
            buttons["Close"].GetComponent<RectTransform>().anchoredPosition=new Vector2(18,-(modalH-58));
            FieldCamera.aspect=w/fieldH;
        }
        public void SetTestSafeArea(Rect area,int width,int height) {testViewport=true;ApplySafeArea(area,width,height);}
        // Keep the editable value visible; native keyboards can cover the lower setup controls.
        public void ApplyKeyboardArea(Rect keyboardArea)
        {
            var input=Prediction.GetComponent<RectTransform>();input.anchoredPosition=new Vector2(12,-54);
            if(keyboardArea.height<=0 || !setup.gameObject.activeInHierarchy)return;
            Canvas.ForceUpdateCanvases();var corners=new Vector3[4];input.GetWorldCorners(corners);
            var bottom=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,corners[0]).y;
            var top=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,corners[1]).y;
            var pixelsPerUnit=(top-bottom)/input.rect.height;
            if(pixelsPerUnit>0)input.anchoredPosition+=Vector2.up*Mathf.Max(0,(keyboardArea.yMax+12-bottom)/pixelsPerUnit);
        }

        private void ResizeButton(string id,float x,float y,float width)
        {var r=buttons[id].GetComponent<RectTransform>();r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,48);buttons[id].GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(width-8,44);}
        private void Update()
        {
            if(!testViewport && (Screen.width!=screenW || Screen.height!=screenH || Screen.safeArea!=safeRect))ApplySafeArea(Screen.safeArea,Screen.width,Screen.height);
            ApplyKeyboardArea(Prediction.isFocused && TouchScreenKeyboard.visible ? TouchScreenKeyboard.area : UnityEngine.Rect.zero);
            if(floatUntil>Time.unscaledTime) {var p=floatAt;floating.rectTransform.anchoredPosition=new Vector2(p.x-55,-p.y-35-(1-(floatUntil-Time.unscaledTime))*20);}
            else floating.text="";
        }
        private void BuildStage()
        {
            FieldCamera=new GameObject("Dungeon Camera").AddComponent<Camera>();FieldCamera.orthographic=true;FieldCamera.orthographicSize=4.8f;
            FieldCamera.transform.position=new Vector3(9,14,-8);FieldCamera.transform.LookAt(new Vector3(3.2f,0,3.2f));FieldCamera.cullingMask=~(1<<5);
            FieldCamera.backgroundColor=new Color(.065f,.13f,.16f);FieldCamera.clearFlags=CameraClearFlags.SolidColor;
            FieldCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            fieldTarget=new RenderTexture(780,1040,24){name="Mobile dungeon actual render"};FieldCamera.targetTexture=fieldTarget;
            var vol=new GameObject("Dungeon light bloom").AddComponent<Volume>();vol.isGlobal=true;vol.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom=vol.profile.Add<Bloom>();bloom.intensity.Override(.3f);bloom.threshold.Override(1.1f);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.37f,.55f,.65f);RenderSettings.ambientEquatorColor=new Color(.23f,.34f,.38f);RenderSettings.ambientGroundColor=new Color(.10f,.15f,.18f);
            var sun=new GameObject("Soft dungeon light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.color=new Color(1,.91f,.76f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(55,-35,0);
            Box("Diorama base",new Vector3(3.2f,-.55f,3.2f),new Vector3(9.4f,.95f,9.4f),new Color(.14f,.24f,.26f));
            for(var x=-1;x<=5;x++)for(var z=-1;z<=5;z++)
            {
                var shade=((x*11+z*7)&3)*.023f;Box("Worn stone",new Vector3(x*Cell,-.02f,z*Cell),new Vector3(1.54f,.14f,1.54f),new Color(.43f+shade,.52f+shade,.49f+shade));
                if((x*13+z*5)%4==0)Box("Stone seam",new Vector3(x*Cell+.27f,.06f,z*Cell+.14f),new Vector3(.55f,.015f,.025f),new Color(.31f,.39f,.38f));
            }
            for(var i=0;i<7;i++)
            {
                Box("Back wall stone",new Vector3(-1.65f,.35f+i*.03f,i*Cell-1.4f),new Vector3(.7f,.85f,1.45f),new Color(.29f,.39f,.39f));
                Box("Far wall stone",new Vector3(i*Cell-1.4f,.65f,8.45f),new Vector3(1.45f,1.4f,.75f),new Color(.25f,.36f,.36f));
            }
            foreach(var p in new[]{new Vector3(-1.8f,0,-1.4f),new Vector3(-1.8f,0,8.4f),new Vector3(8.4f,0,8.4f)})
            {
                Box("Old pillar plinth",p+Vector3.up*.15f,new Vector3(1.1f,.35f,1.1f),new Color(.36f,.46f,.44f));
                Box("Old pillar",p+Vector3.up*1.4f,new Vector3(.66f,2.6f,.66f),new Color(.38f,.48f,.46f));
                Box("Pillar crown",p+Vector3.up*2.7f,new Vector3(.9f,.3f,.9f),new Color(.43f,.53f,.49f));
                var lamp=Sphere("Warm lantern",p+Vector3.up*1.8f+new Vector3(.46f,0,-.46f),new Vector3(.25f,.3f,.25f),new Color(1,.65f,.19f));
                var m=lamp.GetComponent<Renderer>().sharedMaterial;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(2.5f,1.1f,.18f));
                var light=new GameObject("Lantern glow").AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.55f,.19f);light.intensity=2.0f;light.range=4;light.transform.position=lamp.transform.position;
            }
            foreach(var p in new[]{new Vector3(-.8f,0,4.8f),new Vector3(6.8f,0,6.8f)})
            {
                Box("Lantern niche",p+Vector3.up*.7f,new Vector3(.7f,1.4f,.7f),new Color(.22f,.32f,.34f));
                var lamp=Sphere("Dungeon ember",p+Vector3.up*1.45f,new Vector3(.25f,.35f,.25f),new Color(1,.65f,.19f));
                var m=lamp.GetComponent<Renderer>().sharedMaterial;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(3,1.3f,.2f));
                var light=new GameObject("Near lantern glow").AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.62f,.25f);light.intensity=2.5f;light.range=5;light.transform.position=lamp.transform.position;
            }
            for(var i=0;i<16;i++)
            {
                var x=(i%2==0?-1.4f:8.1f);var z=(i*1.31f)%9-1.3f;
                Sphere("Moss",new Vector3(x,.12f,z),new Vector3(.7f,.22f,.58f),new Color(.25f,.42f,.31f));
                Box("Fallen rock",new Vector3(x+.2f,.25f,z+.38f),new Vector3(.4f,.4f,.5f),new Color(.35f,.45f,.42f)).transform.rotation=Quaternion.Euler(12,i*31,18);
            }
            foreach(var u in Fixture.Baseline().Units)
            {
                var root=new GameObject("Fixture "+u.Id).transform;pawns[u.Id]=root;
                var shadow=Sphere("Contact shadow",Vector3.zero,new Vector3(.72f,.025f,.44f),new Color(.14f,.22f,.23f));shadow.transform.SetParent(root,false);shadow.transform.localPosition=new Vector3(0,.09f,0);
                if(!u.Enemy)
                {
                    var index=u.Id[0]-'A';
                    if(index<CharacterArt.Length && CharacterArt[index]!=null)
                    {
                        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Private approved static skin";quad.transform.SetParent(root,false);
                        quad.transform.localPosition=new Vector3(0,.90f,0);quad.transform.rotation=FieldCamera.transform.rotation;
                        quad.transform.localScale=new Vector3(1.45f,1.92f,1);var m=new Material(SpriteTemplate);m.mainTexture=CharacterArt[index];materials.Add(m);quad.GetComponent<Renderer>().sharedMaterial=m;
                    }
                    else Sphere("Missing approved original (not final art)",Vector3.zero,new Vector3(.55f,.7f,.5f),new Color(.5f,.55f,.5f)).transform.SetParent(root,false);
                }
                else
                {
                    var body=Sphere("Fixture slime",Vector3.zero,new Vector3(1.2f,.8f,1.08f),u.Id=="X"?new Color(.4f,.65f,.57f):new Color(.63f,.40f,.63f));body.transform.SetParent(root,false);body.transform.localPosition=Vector3.up*.45f;
                    foreach(var dx in new[]{-.22f,.22f})
                    {
                        var eye=Sphere("Slime eye",Vector3.zero,new Vector3(.2f,.22f,.12f),new Color(.96f,.95f,.85f));eye.transform.SetParent(root,false);eye.transform.localPosition=new Vector3(dx,.61f,-.47f);
                        var pupil=Sphere("Slime pupil",Vector3.zero,new Vector3(.08f,.1f,.08f),new Color(.06f,.12f,.14f));pupil.transform.SetParent(root,false);pupil.transform.localPosition=new Vector3(dx,.6f,-.53f);
                    }
                }
            }
        }
        private GameObject Box(string name,Vector3 p,Vector3 scale,Color color) => Shape(PrimitiveType.Cube,name,p,scale,color);
        private GameObject Sphere(string name,Vector3 p,Vector3 scale,Color color) => Shape(PrimitiveType.Sphere,name,p,scale,color);
        private GameObject Shape(PrimitiveType kind,string name,Vector3 p,Vector3 scale,Color color)
        {
            var obj=GameObject.CreatePrimitive(kind);obj.name=name;obj.transform.position=p;obj.transform.localScale=scale;
            var m=new Material(StoneTemplate);m.color=color;materials.Add(m);obj.GetComponent<Renderer>().sharedMaterial=m;Destroy(obj.GetComponent<Collider>());return obj;
        }
        public void Select(Preference p) {if(Session.Started || Busy)return;Session.Select(p);ResetProjection();Prediction.text="";Refresh();}
        public void ResetBaseline()
        { if(Busy)return;Auto=false;Paused=false;Session.Reset();Prediction.text="";ResetProjection();Refresh(); }
        private void ResetProjection()
        {
            displayed.Clear();captions.Clear();dead.Clear();hp.Clear();positions.Clear();terminalSeen=false;shownStep=0;foreach(var u in Session.Replay.State.Units){hp[u.Id]=u.Hp;positions[u.Id]=u.Position;}
            shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);floating.text="";foreach(var b in badges.Values)b.alpha=1;
        }
        private void StartBattle() {if(Session.Started || Busy)return;Session.Start(Prediction.text);Next();}
        private void Next() {if(Busy || !Session.Started || Session.Replay.State.Outcome!=Outcome.Active)return;StartCoroutine(ResolveAndDisplay());}
        private void ToggleAuto()
        { if(!Session.Started || Session.Replay.State.Outcome!=Outcome.Active)return;Auto=!Auto;Paused=!Auto;Refresh();if(Auto && !Busy)Next(); }
        private IEnumerator ResolveAndDisplay()
        {
            Busy=true;var result=Session.Advance();executingPlan=result.Plan;Refresh();
            foreach(var e in result.Events)
            {
                Display(e);RefreshActors();Refresh();yield return new WaitForSecondsRealtime(e.Kind == EventKind.Action ? 0.16f : 0.35f);
            }
            foreach(var u in Session.Replay.State.Units)
                if(hp[u.Id]!=u.Hp || !positions[u.Id].Equals(u.Position))throw new InvalidOperationException("Mobile event projection mismatch");
            Busy=false;Refresh();
            if(Session.Replay.State.Outcome!=Outcome.Active){Auto=false;Debug.Log("MOBILE_G1_RESULT "+Session.Preference+" "+Session.Replay.Hash());}
            else if(Auto)Next();
        }
        private void Display(BattleEvent e)
        {
            displayed.Add(e);
            if(e.Kind==EventKind.Action)shownStep=e.Step;
            if(e.Kind==EventKind.End)terminalSeen=true;
            switch(e.Kind)
            {
                case EventKind.Move:positions[e.Target]=e.Position;
                    if(e.Actor=="A" || e.Actor=="B"){shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);}break;
                case EventKind.Protect:
                    shield.gameObject.SetActive(true);shield.anchoredPosition=Point(positions[e.Target],.7f)+new Vector2(-20,19);guardLink.gameObject.SetActive(true);Link(guardLink,positions[e.Actor],positions[e.Target]);break;
                case EventKind.Damage:case EventKind.Counter:case EventKind.Heal:
                    hp[e.Target]=e.HpAfter;floating.text=(e.Kind==EventKind.Heal?"+":"−")+e.Amount;floating.color=e.Kind==EventKind.Heal?Jade:e.Kind==EventKind.Counter?Gold:new Color(1,.65f,.56f);
                    var at=Point(positions[e.Target],1.8f);floatAt=new Vector2(at.x,-at.y);floatUntil=Time.unscaledTime+1;
                    actionLink.gameObject.SetActive(true);Link(actionLink,positions[e.Actor],positions[e.Target]);
                    if(e.Detail.StartsWith("Guard"))captions.Add("守護でダメージ軽減");break;
                case EventKind.Death:dead.Add(e.Target);if(e.Target=="A" || e.Target=="B"){shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);}hp[e.Target]=0;pawns[e.Target].gameObject.SetActive(false);badges[e.Target].alpha=.45f;break;
            }
            if(e.Kind!=EventKind.Action)captions.Add(Describe(e));
            if(e.Kind==EventKind.Action && e.Actor=="B"){shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);}
        }
        public static string Describe(BattleEvent e)
        {
            switch(e.Kind)
            {
                case EventKind.Protect:return e.Actor+" → "+e.Target+" を守る（軽減 "+e.Amount+"）";
                case EventKind.Damage:return e.Actor+" → "+e.Target+" 攻撃  −"+e.Amount+(e.Detail.StartsWith("Guard")?"（守護で軽減）":"");
                case EventKind.Counter:return e.Actor+" → "+e.Target+" 反撃  −"+e.Amount;
                case EventKind.Heal:return e.Actor+" → "+e.Target+" 回復  ＋"+e.Amount;
                case EventKind.Death:return e.Target+" は戦闘不能";
                case EventKind.Move:return e.Actor+" が移動";
                case EventKind.End:return "戦闘終了："+(e.Detail=="Victory"?"勝利":e.Detail=="Defeat"?"敗北":"上限");
                default:return "";
            }
        }
        private void RefreshActors()
        {
            var occupied=new List<Rect>();
            foreach(var u in Session.Replay.State.Units)
            {
                var p=positions[u.Id];pawns[u.Id].position=new Vector3(p.X*Cell,0,p.Y*Cell);pawns[u.Id].gameObject.SetActive(!dead.Contains(u.Id));
                var at=Point(p,u.Enemy?1.1f:2.12f);var label=new Rect(at.x-31,-at.y-16,62,32);
                // Pack only the HUD labels; the Core positions remain untouched.
                foreach(var offset in new[]{Vector2.zero,new Vector2(0,-34),new Vector2(-64,0),new Vector2(64,0),new Vector2(0,-68),new Vector2(-64,-34),new Vector2(64,-34)})
                {
                    var candidate=new Rect(Mathf.Clamp(label.x+offset.x,2,Field.rect.width-64),Mathf.Clamp(label.y+offset.y,2,Field.rect.height-34),62,32);
                    if(occupied.All(r=>!r.Overlaps(candidate))){label=candidate;break;}
                }
                if(!dead.Contains(u.Id))occupied.Add(label);
                badges[u.Id].GetComponent<RectTransform>().anchoredPosition=new Vector2(label.x,-label.y);
                badges[u.Id].gameObject.SetActive(!dead.Contains(u.Id));hpTexts[u.Id].text=u.Id+"  "+hp[u.Id]+"/"+u.MaxHp;hpBars[u.Id].rectTransform.sizeDelta=new Vector2(54f*hp[u.Id]/u.MaxHp,5);
            }
        }
        private void Refresh()
        {
            RefreshActors();var active=Session.Started;setup.gameObject.SetActive(!active);battleControls.gameObject.SetActive(active);
            phase.text=active?"行動 "+shownStep+"  /  "+(!terminalSeen?"観察中":"戦闘終了"):"Bの選好だけを変える  /  同じ初期戦況";
            if(active)
            {
                eventCaption.text=string.Join("\n",captions.AsEnumerable().Reverse().Take(2).Reverse());
                var p=Busy?executingPlan:Rules.Preview(Session.Replay.State,new Command(Session.Preference));
                intent.text=p==null?"結果を比べ、同じ初期戦況から再試行できます。":(Busy?"実行  ":"次の意図  ")+p.Actor+"："+PlanName(p.Kind)+(p.Target!=""?" → "+p.Target:"");
            }
            if(!Busy)
            {
                var p=Rules.Preview(Session.Replay.State,new Command(Session.Preference));actionLink.gameObject.SetActive(p!=null && p.Kind!=ActionKind.Maintain);
                ghost.gameObject.SetActive(p?.Kind==ActionKind.Move);
                if(p!=null){Link(actionLink,positions[p.Actor],p.Kind==ActionKind.Move?p.Destination:(p.Target!=""?positions[p.Target]:positions[p.Actor]));if(p.Kind==ActionKind.Move)ghost.anchoredPosition=Point(p.Destination,.05f)+new Vector2(-17,9);}
            }
            buttons["Next"].interactable=active && Session.Replay.State.Outcome==Outcome.Active && !Busy && !Auto;
            buttons["Play"].interactable=active && Session.Replay.State.Outcome==Outcome.Active;buttons["Play"].GetComponentInChildren<Text>().text=Auto?"一時停止":"続けて見る";
            buttons["Reset"].interactable=!Busy;buttons["Compare"].interactable=Session.Trials.Count>0 && !Busy;
            buttons["Protect"].GetComponent<Image>().color=Session.Preference==Preference.ProtectAnchor?new Color(.22f,.40f,.39f):new Color(.12f,.24f,.26f);
            buttons["Attack"].GetComponent<Image>().color=Session.Preference==Preference.DirectAttack?new Color(.22f,.40f,.39f):new Color(.12f,.24f,.26f);
        }
        private static string PlanName(ActionKind kind) => kind==ActionKind.Protect?"守護":kind==ActionKind.Attack?"攻撃":kind==ActionKind.Heal?"回復":kind==ActionKind.Move?"移動":"待機";
        private void OpenDetails(bool trials)
        {
            if(trials)
            {
                headline.text="結果と事前予想";
                modalText.text=string.Join("\n\n",new[]{Preference.ProtectAnchor,Preference.DirectAttack}.Where(p=>Session.Trials.ContainsKey(p)).Select(p=>{
                    var t=Session.Trials[p];return (p==Preference.ProtectAnchor?"B：Aを守る":"B：直接攻撃")+"\n"+(t.Outcome==Outcome.Victory?"勝利":"終了")+" / "+t.Actions+"行動 / 生存 "+t.Alive+"人\nHP "+t.Hp+"\n回復残数 "+t.Heals+"\n予想："+(t.Prediction==""?"（未入力）":t.Prediction)+"\nSHA "+t.Hash.Substring(0,16);
                }));
            }
            else
            {
                headline.text="G1の実験ルール";
                modalText.text="A–Dは匿名の実験役です。\n外見の仮対応：Aシロ / Bモモ / Cガル / Dミャル\n正式キャラのスキルや能力は確定していません。\n\nA：HP18 / 攻撃5 / 生存被弾後に反撃2\nB：HP16 / 攻撃4 / 隣接Aを守り被弾を3軽減\nC：HP12 / 攻撃4 / Aも同じ敵に隣接時＋2\nD：HP10 / 攻撃2 / 射程2で4回復（残2回）\nX：HP18 / 攻撃6、Y：HP14 / 攻撃5\n\n順番 B→X→A→C→Y→D（死者を除外）\n敵は生存A優先。その他は距離→ID順。\n移動は距離が縮む空き隣接マス。\nBの次行動・死亡・離隔で守護は失効。\n回復はHPが4以上欠けた生存味方に行う。\n\n2条件はBの選好以外すべて同じ。\n数値は実験用で、どちらも推奨しません。\n次の意図・再生単位はG1検証用であり、\n正式なhorizon/chunkは未決です。";
            }
            modalObject.SetActive(true);detailsViewport.GetComponent<ScrollRect>().verticalNormalizedPosition=1;
        }
        private Vector2 Point(GridPosition p,float y=.7f)
        {var v=FieldCamera.WorldToViewportPoint(new Vector3(p.X*Cell,y,p.Y*Cell));return new Vector2(v.x*Field.rect.width,-(1-v.y)*Field.rect.height);}
        private void Link(RectTransform strip,GridPosition a,GridPosition b)
        {var from=Point(a);var to=Point(b);var d=to-from;strip.pivot=new Vector2(0,.5f);strip.anchoredPosition=from;strip.sizeDelta=new Vector2(d.magnitude,3);strip.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
        private static RectTransform Strip(Transform parent,string name,Color color)
        {var r=Rect(parent,name,0,0,1,3);r.gameObject.AddComponent<Image>().color=color;r.GetComponent<Image>().raycastTarget=false;return r;}
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=5;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        private Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size,TextAnchor anchor=TextAnchor.UpperLeft)
        {var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=JapaneseFont;t.fontSize=size;t.text=value;t.color=Ink;t.alignment=anchor;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        private void MakeButton(Transform parent,string id,string name,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,int size)
        {var r=Rect(parent,id,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.color=new Color(.12f,.24f,.26f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;b.onClick.AddListener(action);buttons[id]=b;Label(r,"Label",name,4,2,w-8,h-4,size,TextAnchor.MiddleCenter);}
        private void OnDestroy(){if(fieldTarget!=null){fieldTarget.Release();Destroy(fieldTarget);}foreach(var m in materials)Destroy(m);}
    }
}
