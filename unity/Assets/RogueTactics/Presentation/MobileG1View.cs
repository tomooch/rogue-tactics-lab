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
    public sealed partial class MobileG1View : MonoBehaviour
    {
        public Font JapaneseFont;
        public bool HqWorld;
        public float CameraPitch=75;
        public float CameraYaw;
        public bool EventMotion;
        public Material StoneTemplate, SpriteTemplate, SkyTemplate, RuinsTemplate;
        public Mesh[] EnvironmentMeshes = Array.Empty<Mesh>();
        public ScriptableRendererFeature AmbientOcclusion;
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
        private readonly Dictionary<string,Renderer> bodies=new Dictionary<string,Renderer>();
        private readonly Dictionary<string,Rect> bodyRects=new Dictionary<string,Rect>();
        private readonly Dictionary<string,CanvasGroup> badges=new Dictionary<string,CanvasGroup>();
        private readonly Dictionary<string,Text> hpTexts=new Dictionary<string,Text>();
        private readonly Dictionary<string,Image> hpBars=new Dictionary<string,Image>();
        private readonly Dictionary<string,Button> buttons=new Dictionary<string,Button>();
        private readonly List<Material> materials=new List<Material>();
        private readonly List<BattleEvent> displayed=new List<BattleEvent>();
        private readonly List<string> captions=new List<string>();
        private readonly HashSet<string> dead=new HashSet<string>();
        private Canvas canvas;
        private Transform skyBackdrop;
        private RenderTexture fieldTarget;
        private VolumeProfile runtimeProfile;
        private bool visualTweenActive;
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
        private const float Cell=2.5f, DepthSpacing=2.0f;
        private static Vector3 World(GridPosition p,float y=0)=>new Vector3(p.X*Cell,y,p.Y*Cell*DepthSpacing);
        private static readonly Color Ink=new Color(.93f,.94f,.88f), Gold=new Color(.96f,.79f,.40f), Jade=new Color(.35f,.85f,.76f);
        public Button Button(string id) => buttons[id];
        public bool HasAllArt => CharacterArt.Length==4 && CharacterArt.All(t=>t!=null);
        private void Awake()
        {
            if(AmbientOcclusion!=null)AmbientOcclusion.SetActive(true);
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
            Label(SafeRoot,"Title","天空の遺跡",16,0,250,37,22);
            Label(SafeRoot,"Boundary","仲間に託す、小さな戦い  /  G1",16,36,300,22,12);
            MakeButton(SafeRoot,"Details","記録",322,6,56,44,()=>OpenDetails(false),14);
            BuildStage();
            Field=Rect(SafeRoot,"Field",0,60,390,520);fieldImage=Field.gameObject.AddComponent<RawImage>();fieldImage.texture=fieldTarget;fieldImage.raycastTarget=false;
            guardLink=Strip(Field,"Guard link",Jade);actionLink=Strip(Field,"Core intention",Gold);
            ghost=Rect(Field,"Legal destination",0,0,34,18);ghost.gameObject.AddComponent<Image>().color=new Color(.8f,.95f,.8f,.25f);ghost.GetComponent<Image>().raycastTarget=false;
            foreach(var u in Fixture.Baseline().Units)
            {
                var r=Rect(Field,"HP "+u.Id,0,0,44,22);r.gameObject.AddComponent<Image>().color=new Color(.025f,.06f,.065f,.96f);
                badges[u.Id]=r.gameObject.AddComponent<CanvasGroup>();
                hpTexts[u.Id]=Label(r,"Value",u.Id,1,0,42,18,10,TextAnchor.MiddleCenter);
                var bar=Rect(r,"HP bar",4,20,36,2).gameObject.AddComponent<Image>();bar.color=u.Enemy?new Color(.92f,.44f,.38f):Jade;
                hpBars[u.Id]=bar;
            }
            shield=Rect(Field,"Protect",0,0,24,24);shield.gameObject.AddComponent<Image>().color=new Color(.035f,.20f,.22f,.98f);
            Label(shield,"Shield","守",0,0,24,24,16,TextAnchor.MiddleCenter);
            floating=Label(Field,"Event feedback","",0,0,110,40,23,TextAnchor.MiddleCenter);floating.gameObject.SetActive(!EventMotion);
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
            modalObject.AddComponent<Image>().color=new Color(.055f,.095f,.12f,1f);
            headline=Label(modal,"Heading","実験の詳細",18,12,334,32,20);
            detailsViewport=Rect(modal,"Details viewport",18,54,334,502);
            detailsViewport.gameObject.AddComponent<Image>().color=Color.white;detailsViewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            modalText=Label(detailsViewport,"Body","",0,0,334,502,16);modalText.supportRichText=false;
            modalText.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=detailsViewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=detailsViewport;scroll.content=modalText.rectTransform;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            MakeButton(modal,"Close","閉じる",18,570,334,46,()=>modalObject.SetActive(false),17);modalObject.SetActive(false);
            BuildProductHud();BuildRelationCues();
            ApplySafeArea(Screen.safeArea,Screen.width,Screen.height);
        }
        public void ApplySafeArea(Rect area,int width,int height)
        {
            if(width<1 || height<1)return;
            safeRect=area;screenW=width;screenH=height;
            SafeRoot.anchorMin=new Vector2(area.x/width,area.y/height);SafeRoot.anchorMax=new Vector2(area.xMax/width,area.yMax/height);SafeRoot.offsetMin=SafeRoot.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();var h=SafeRoot.rect.height;var w=SafeRoot.rect.width;
            var fieldH=Mathf.Max(280,h-(DeveloperMode?310:264));Field.anchoredPosition=new Vector2(0,-60);Field.sizeDelta=new Vector2(w,fieldH);controls.anchoredPosition=new Vector2(0,-(60+fieldH+8));
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
            LayoutProductHud(w,h,fieldH);
            FieldCamera.aspect=w/fieldH;if(skyBackdrop!=null)skyBackdrop.localScale=new Vector3(FieldCamera.orthographicSize*2*FieldCamera.aspect,FieldCamera.orthographicSize*2,1);
            if(hp.Count>0)Refresh();
        }
        public void SetTestSafeArea(Rect area,int width,int height) {testViewport=true;ApplySafeArea(area,width,height);}
        // Keep the editable value visible; native keyboards can cover the lower setup controls.
        public void ApplyKeyboardArea(Rect keyboardArea,bool editing=false)
        {
            var input=Prediction.GetComponent<RectTransform>();input.anchoredPosition=new Vector2(12,DeveloperMode?-54:-56);
            // Dock below the title while native editing owns focus. This also works during keyboard opening/area=0.
            if(editing && setup.gameObject.activeInHierarchy){input.position=SafeRoot.TransformPoint(new Vector3(12,-100,0));return;}
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
            SampleFrame();
            if(!testViewport && (Screen.width!=screenW || Screen.height!=screenH || Screen.safeArea!=safeRect))ApplySafeArea(Screen.safeArea,Screen.width,Screen.height);
            ApplyKeyboardArea(UnityEngine.Rect.zero,Prediction.isFocused && TouchScreenKeyboard.isSupported);
            if(Paused && floatUntil>Time.unscaledTime)floatUntil+=Time.unscaledDeltaTime;
            if(floatUntil>Time.unscaledTime)floating.rectTransform.anchoredPosition=new Vector2(12,-62);
            else floating.text="";
        }
        private void BuildStage()
        {
            FieldCamera=new GameObject("Dungeon Camera").AddComponent<Camera>();FieldCamera.orthographic=true;FieldCamera.orthographicSize=5.4f;
            // Presentation-only spacing and a straight, steeper view preserve every Core cell.
            var center=new Vector3(2*Cell,-1.4f,2*Cell*DepthSpacing-.4f);
            FieldCamera.transform.position=center+Quaternion.Euler(0,CameraYaw,0)*new Vector3(0,Mathf.Sin(CameraPitch*Mathf.Deg2Rad)*21,-Mathf.Cos(CameraPitch*Mathf.Deg2Rad)*21);FieldCamera.transform.LookAt(center);FieldCamera.cullingMask=~(1<<5);
            if(HqWorld)FieldCamera.orthographicSize=10f;
            FieldCamera.backgroundColor=HqWorld?new Color(.56f,.77f,.89f):new Color(.065f,.13f,.16f);FieldCamera.clearFlags=CameraClearFlags.SolidColor;
            // Same-camera Unity A/B showed the HQ post-process path dulling approved
            // sprites and sky. Keep their unprocessed sampling; retain local contact light.
            FieldCamera.GetUniversalAdditionalCameraData().renderPostProcessing=!HqWorld;
            fieldTarget=new RenderTexture(780,1040,24){antiAliasing=4,name="Mobile dungeon actual render"};FieldCamera.targetTexture=fieldTarget;
            FieldCamera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            if(!HqWorld)
            {
                var vol=new GameObject("Dungeon light bloom").AddComponent<Volume>();vol.isGlobal=true;vol.profile=runtimeProfile=ScriptableObject.CreateInstance<VolumeProfile>();
                var dof=vol.profile.Add<DepthOfField>();dof.mode.Override(DepthOfFieldMode.Gaussian);dof.gaussianStart.Override(25);dof.gaussianEnd.Override(45);dof.gaussianMaxRadius.Override(.7f);
                var bloom=vol.profile.Add<Bloom>();bloom.intensity.Override(.3f);bloom.threshold.Override(1.1f);
            }
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.37f,.55f,.65f);RenderSettings.ambientEquatorColor=new Color(.23f,.34f,.38f);RenderSettings.ambientGroundColor=new Color(.10f,.15f,.18f);
            var sun=new GameObject("Soft dungeon light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.color=new Color(1,.91f,.76f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(55,-35,0);
            if(HqWorld)
            {
                var ruins=HqSkyRuins.Build(RuinsTemplate,new Vector3(2*Cell,0,2*Cell*DepthSpacing),EnvironmentMeshes);ruins.AddComponent<HqAmbient>();
                if(SkyTemplate!=null){var sky=GameObject.CreatePrimitive(PrimitiveType.Quad);sky.name="Procedural sky haze";Destroy(sky.GetComponent<Collider>());sky.transform.SetParent(FieldCamera.transform,false);skyBackdrop=sky.transform;sky.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;sky.transform.localPosition=new Vector3(0,0,90);sky.transform.localScale=new Vector3(50,50,1);sky.GetComponent<Renderer>().sharedMaterial=SkyTemplate;}
                RenderSettings.ambientSkyColor=new Color(.58f,.70f,.80f);RenderSettings.ambientEquatorColor=new Color(.40f,.49f,.53f);RenderSettings.ambientGroundColor=new Color(.23f,.29f,.25f);sun.intensity=1.55f;sun.color=new Color(1,.93f,.78f);
            }
            else
            {
            Box("Diorama base",center+Vector3.down*.55f,new Vector3(Cell*5.9f,.95f,Cell*5.9f),new Color(.10f,.18f,.20f));
            for(var x=-1;x<=5;x++)for(var z=-1;z<=5;z++)
            {
                var shade=((x*11+z*7)&3)*.012f;Box("Worn stone",new Vector3(x*Cell,-.02f,z*Cell),new Vector3(Cell-.06f,.14f,Cell-.06f),new Color(.22f+shade,.31f+shade,.32f+shade));
                if((x*13+z*5)%4==0)Box("Stone seam",new Vector3(x*Cell+.27f,.06f,z*Cell+.14f),new Vector3(.55f,.015f,.025f),new Color(.18f,.25f,.26f));
            }
            for(var i=0;i<7;i++)
            {
                Box("Back wall stone",new Vector3(-Cell,.35f+i*.03f,(i-1)*Cell),new Vector3(.7f,.85f,Cell-.08f),new Color(.22f,.31f,.33f));
                Box("Far wall stone",new Vector3((i-1)*Cell,.65f,5.25f*Cell),new Vector3(Cell-.08f,1.4f,.75f),new Color(.21f,.30f,.32f));
            }
            foreach(var p in new[]{new Vector3(-Cell,0,-Cell),new Vector3(-Cell,0,5*Cell),new Vector3(5*Cell,0,5*Cell)})
            {
                Box("Old pillar plinth",p+Vector3.up*.15f,new Vector3(1.1f,.35f,1.1f),new Color(.36f,.46f,.44f));
                Box("Old pillar",p+Vector3.up*1.4f,new Vector3(.66f,2.6f,.66f),new Color(.38f,.48f,.46f));
                Box("Pillar crown",p+Vector3.up*2.7f,new Vector3(.9f,.3f,.9f),new Color(.43f,.53f,.49f));
                var lamp=Sphere("Warm lantern",p+Vector3.up*1.8f+new Vector3(.46f,0,-.46f),new Vector3(.25f,.3f,.25f),new Color(1,.65f,.19f));
                var m=lamp.GetComponent<Renderer>().sharedMaterial;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(2.5f,1.1f,.18f));
                var light=new GameObject("Lantern glow").AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.55f,.19f);light.intensity=2.0f;light.range=4;light.transform.position=lamp.transform.position;
            }
            foreach(var p in new[]{new Vector3(.2f*Cell,0,3.5f*Cell),new Vector3(3.8f*Cell,0,3.5f*Cell)})
            {
                Box("Lantern niche",p+Vector3.up*.7f,new Vector3(.7f,1.4f,.7f),new Color(.22f,.32f,.34f));
                var lamp=Sphere("Dungeon ember",p+Vector3.up*1.45f,new Vector3(.25f,.35f,.25f),new Color(1,.65f,.19f));
                var m=lamp.GetComponent<Renderer>().sharedMaterial;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(3,1.3f,.2f));
                var light=new GameObject("Near lantern glow").AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.62f,.25f);light.intensity=2.5f;light.range=5;light.transform.position=lamp.transform.position;
            }
            for(var i=0;i<16;i++)
            {
                var x=(i%2==0?-.85f*Cell:4.85f*Cell);var z=(i*1.31f)%(6*Cell)-Cell;
                Sphere("Moss",new Vector3(x,.12f,z),new Vector3(.7f,.22f,.58f),new Color(.25f,.42f,.31f));
                Box("Fallen rock",new Vector3(x+.2f,.25f,z+.38f),new Vector3(.4f,.4f,.5f),new Color(.35f,.45f,.42f)).transform.rotation=Quaternion.Euler(12,i*31,18);
            }
            }
            foreach(var u in Fixture.Baseline().Units)
            {
                var root=new GameObject("Fixture "+u.Id).transform;pawns[u.Id]=root;
                if(HqWorld)MakeKnownPositionMarker(root,u.Enemy);
                var shadow=HqWorld?MakeContactShadow(root,u.Enemy):Sphere("Contact shadow",Vector3.zero,new Vector3(.72f,.025f,.44f),new Color(.14f,.22f,.23f));shadow.transform.SetParent(root,false);if(!HqWorld)shadow.transform.localPosition=new Vector3(0,.09f,0);
                if(!u.Enemy)
                {
                    var index=u.Id[0]-'A';
                    if(index<CharacterArt.Length && CharacterArt[index]!=null)
                    {
                        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Private approved static skin";quad.transform.SetParent(root,false);
                        quad.transform.localPosition=Vector3.up*.12f+FieldCamera.transform.up*1.5f;quad.transform.rotation=FieldCamera.transform.rotation;
                        quad.transform.localScale=new Vector3(2.25f,3.0f,1);var m=new Material(SpriteTemplate);m.mainTexture=CharacterArt[index];materials.Add(m);quad.GetComponent<Renderer>().sharedMaterial=m;bodies[u.Id]=quad.GetComponent<Renderer>();
                    }
                    else {var missing=Sphere("Missing approved original (not final art)",Vector3.zero,new Vector3(.55f,.7f,.5f),new Color(.5f,.55f,.5f));missing.transform.SetParent(root,false);bodies[u.Id]=missing.GetComponent<Renderer>();}
                }
                else
                {
                    // Anchor the front contact edge to the cell, like the static portraits' feet.
                    // A centred sphere otherwise extends over the next row's portrait/foot HP.
                    if(!HqWorld)shadow.transform.localPosition+=Vector3.forward*.55f;
                    if(HqWorld)bodies[u.Id]=HqCreature.Build(root,StoneTemplate,u.Id=="Y");
                    else
                    {
                    var body=Sphere("Fixture slime",Vector3.zero,new Vector3(1.2f,.8f,1.08f),u.Id=="X"?new Color(.4f,.65f,.57f):new Color(.63f,.40f,.63f));body.transform.SetParent(root,false);body.transform.localPosition=new Vector3(0,.45f,.55f);bodies[u.Id]=body.GetComponent<Renderer>();
                    foreach(var dx in new[]{-.22f,.22f})
                    {
                        var eye=Sphere("Slime eye",Vector3.zero,new Vector3(.2f,.22f,.12f),new Color(.96f,.95f,.85f));eye.transform.SetParent(root,false);eye.transform.localPosition=new Vector3(dx,.61f,.08f);
                        var pupil=Sphere("Slime pupil",Vector3.zero,new Vector3(.08f,.1f,.08f),new Color(.06f,.12f,.14f));pupil.transform.SetParent(root,false);pupil.transform.localPosition=new Vector3(dx,.6f,.02f);
                    }
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
            ClearRelation();frameIntervals.Clear();LastFrameReport=null;displayed.Clear();captions.Clear();dead.Clear();hp.Clear();positions.Clear();terminalSeen=false;shownStep=0;shownActor="";reaction="";foreach(var u in Session.Replay.State.Units){hp[u.Id]=u.Hp;positions[u.Id]=u.Position;}
            shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);floating.text="";foreach(var b in badges.Values)b.alpha=1;
        }
        private void StartBattle() {if(Session.Started || Busy)return;Session.Start(Prediction.text);Auto=!DeveloperMode;Next();}
        private void Next() {if(Busy || !Session.Started || Session.Replay.State.Outcome!=Outcome.Active)return;StartCoroutine(ResolveAndDisplay());}
        private void ToggleAuto()
        { if(!Session.Started || terminalSeen)return;Auto=!Auto;Paused=!Auto;Refresh();if(Auto && !Busy)Next(); }
        private IEnumerator ResolveAndDisplay()
        {
            Busy=true;var result=Session.Advance();executingPlan=result.Plan;Refresh();
            foreach(var e in result.Events)
            {
                yield return PresentEvent(e);
            }
            foreach(var u in Session.Replay.State.Units)
                if(hp[u.Id]!=u.Hp || !positions[u.Id].Equals(u.Position))throw new InvalidOperationException("Mobile event projection mismatch");
            Busy=false;Refresh();
            if(Session.Replay.State.Outcome!=Outcome.Active){Auto=false;FinishFrameReport();Debug.Log("MOBILE_G1_RESULT "+Session.Preference+" "+Session.Replay.Hash());}
            else if(Auto)Next();
        }
        private void Display(BattleEvent e)
        {
            displayed.Add(e);
            if(e.Kind==EventKind.Action){shownStep=e.Step;shownActor=e.Actor;reaction="";if(executingPlan.Kind==ActionKind.Maintain || executingPlan.Kind==ActionKind.Move)ClearRelation();else ShowRelation(e.Actor,executingPlan.Target,executingPlan.Kind,false);}
            if(e.Kind==EventKind.Counter)reaction=HeroName(e.Actor)+" 反撃（割込）";
            if(e.Kind==EventKind.End)terminalSeen=true;
            switch(e.Kind)
            {
                case EventKind.Move:positions[e.Target]=e.Position;
                    actionLink.gameObject.SetActive(false);ghost.gameObject.SetActive(false);
                    if(e.Actor=="A" || e.Actor=="B"){shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);}break;
                case EventKind.Protect:
                    shield.gameObject.SetActive(!EventMotion);ShowRelation(e.Actor,e.Target,ActionKind.Protect,false);shield.anchoredPosition=(BodyRect(positions[e.Actor]).center+BodyRect(positions[e.Target]).center)/2+new Vector2(-12,12);
                    guardLink.gameObject.SetActive(!EventMotion);if(!EventMotion)Link(guardLink,positions[e.Actor],positions[e.Target]);break;
                case EventKind.Damage:case EventKind.Counter:case EventKind.Heal:
                    hp[e.Target]=e.HpAfter;ShowImpactRelation(e);floating.text=HeroName(e.Target)+"  "+(e.Kind==EventKind.Heal?"+":"−")+e.Amount;floating.color=e.Kind==EventKind.Heal?Jade:e.Kind==EventKind.Counter?Gold:new Color(1,.65f,.56f);
                    floatUntil=Time.unscaledTime+1;
                    actionLink.gameObject.SetActive(!EventMotion);if(!EventMotion)Link(actionLink,positions[e.Actor],positions[e.Target]);
                    if(e.Detail.StartsWith("Guard"))captions.Add("守護でダメージ軽減");break;
                case EventKind.Death:dead.Add(e.Target);actionLink.gameObject.SetActive(false);ghost.gameObject.SetActive(false);if(e.Target=="A" || e.Target=="B"){shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);}hp[e.Target]=0;pawns[e.Target].gameObject.SetActive(false);badges[e.Target].alpha=.45f;break;
            }
            if(e.Kind!=EventKind.Action)captions.Add(Describe(e));
            if(e.Kind==EventKind.Action && e.Actor=="B"){shield.gameObject.SetActive(false);guardLink.gameObject.SetActive(false);}
        }
        public static string Describe(BattleEvent e)
        {
            switch(e.Kind)
            {
                case EventKind.Protect:return HeroName(e.Actor)+" → "+HeroName(e.Target)+" を守る（軽減 "+e.Amount+"）";
                case EventKind.Damage:return HeroName(e.Actor)+" → "+HeroName(e.Target)+" 攻撃  −"+e.Amount+(e.Detail.StartsWith("Guard")?"（守護で軽減）":"");
                case EventKind.Counter:return HeroName(e.Actor)+" → "+HeroName(e.Target)+" 反撃  −"+e.Amount;
                case EventKind.Heal:return HeroName(e.Actor)+" → "+HeroName(e.Target)+" 回復  ＋"+e.Amount;
                case EventKind.Death:return HeroName(e.Target)+" は戦闘不能";
                case EventKind.Move:return HeroName(e.Actor)+" が移動";
                case EventKind.End:return "戦闘終了："+(e.Detail=="Victory"?"勝利":e.Detail=="Defeat"?"敗北":"上限");
                default:return "";
            }
        }
        private void RefreshActors()
        {
            bodyRects.Clear();
            // HP belongs beneath its own silhouette, never on a face or a neighbour.
            foreach(var u in Session.Replay.State.Units)
            {
                var p=positions[u.Id];if(!visualTweenActive)pawns[u.Id].position=World(p);pawns[u.Id].gameObject.SetActive(!dead.Contains(u.Id));
                var body=ProjectedBody(bodies[u.Id]);if(!dead.Contains(u.Id))bodyRects[u.Id]=body;
                badges[u.Id].GetComponent<RectTransform>().anchoredPosition=new Vector2(body.center.x-22,body.yMin-5);
                badges[u.Id].gameObject.SetActive(!dead.Contains(u.Id));hpTexts[u.Id].text=u.Id+" "+hp[u.Id];hpBars[u.Id].rectTransform.sizeDelta=new Vector2(36f*hp[u.Id]/u.MaxHp,2);
            }
            LayoutFootBadges();RefreshRelationCues();
        }
        private void Refresh()
        {
            RefreshActors();if(guardLink.gameObject.activeSelf)Link(guardLink,positions["B"],positions["A"]);var active=Session.Started;setup.gameObject.SetActive(!active);battleControls.gameObject.SetActive(active);
            phase.text=active?"行動 "+shownStep+"  /  "+(!terminalSeen?"観察中":"戦闘終了"):"Bの選好だけを変える  /  同じ初期戦況";
            if(active)
            {
                eventCaption.text=string.Join("\n",captions.AsEnumerable().Reverse().Take(2).Reverse());
                var p=Busy?executingPlan:Rules.Preview(Session.Replay.State,new Command(Session.Preference));
                intent.text=p==null?"結果を比べ、同じ初期戦況から再試行できます。":(Busy?"実行  ":"次の意図  ")+p.Actor+"："+PlanName(p.Kind)+(p.Target!=""?" → "+p.Target:"");
            }
            actionLink.GetComponent<MobileRouteGraphic>().color=Busy?Gold:new Color(.37f,.79f,1);
            if(!Busy)
            {
                var p=active?Rules.Preview(Session.Replay.State,new Command(Session.Preference)):null;actionLink.gameObject.SetActive(p!=null && p.Kind==ActionKind.Move);
                if(p!=null && p.Kind!=ActionKind.Move && p.Kind!=ActionKind.Maintain)ShowRelation(p.Actor,p.Target,p.Kind,true);else ClearRelation();
                ghost.gameObject.SetActive(p?.Kind==ActionKind.Move);
                if(p!=null){Link(actionLink,positions[p.Actor],p.Kind==ActionKind.Move?p.Destination:(p.Target!=""?positions[p.Target]:positions[p.Actor]));if(p.Kind==ActionKind.Move)ghost.anchoredPosition=Point(p.Destination,.05f)+new Vector2(-17,9);}
            }
            buttons["Next"].interactable=active && Session.Replay.State.Outcome==Outcome.Active && !Busy && !Auto;
            buttons["Play"].interactable=active && !terminalSeen;buttons["Play"].GetComponentInChildren<Text>().text=Auto?"一時停止":"続けて見る";
            buttons["Reset"].interactable=!Busy;buttons["Compare"].interactable=Session.Trials.Count>0 && !Busy;
            buttons["Protect"].GetComponent<Image>().color=Session.Preference==Preference.ProtectAnchor?new Color(.22f,.40f,.39f):new Color(.12f,.24f,.26f);
            buttons["Attack"].GetComponent<Image>().color=Session.Preference==Preference.DirectAttack?new Color(.22f,.40f,.39f):new Color(.12f,.24f,.26f);
            RefreshProductHud();
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
                modalText.text="青：仲間の位置 / 赤：敵の位置\nA–Dは匿名の実験役です。\n外見の仮対応：Aシロ / Bモモ / Cガル / Dミャル\n正式キャラのスキルや能力は確定していません。\n\nA：HP18 / 攻撃5 / 生存被弾後に反撃2\nB：HP16 / 攻撃4 / 隣接Aを守り被弾を3軽減\nC：HP12 / 攻撃4 / Aも同じ敵に隣接時＋2\nD：HP10 / 攻撃2 / 射程2で4回復（残2回）\nX：HP18 / 攻撃6、Y：HP14 / 攻撃5\n\n順番 B→X→A→C→Y→D（死者を除外）\n敵は生存A優先。その他は距離→ID順。\n移動は距離が縮む空き隣接マス。\nBの次行動・死亡・離隔で守護は失効。\n回復はHPが4以上欠けた生存味方に行う。\n\n2条件はBの選好以外すべて同じ。\n数値は実験用で、どちらも推奨しません。\n次の意図・再生単位はG1検証用であり、\n正式なhorizon/chunkは未決です。";
            }
            if(Session.Started && !terminalSeen){Auto=false;Paused=true;Refresh();}
            modalObject.SetActive(true);modalObject.transform.SetAsLastSibling();detailsViewport.GetComponent<ScrollRect>().verticalNormalizedPosition=1;
        }
        private Vector2 Point(GridPosition p,float y=.7f)
        {var v=FieldCamera.WorldToViewportPoint(World(p,y));return new Vector2(v.x*Field.rect.width,-(1-v.y)*Field.rect.height);}
        private Rect ProjectedBody(Renderer body)
        {
            var bounds=body.localBounds;var min=new Vector2(float.MaxValue,float.MaxValue);var max=new Vector2(float.MinValue,float.MinValue);
            for(var i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var v=FieldCamera.WorldToViewportPoint(body.transform.TransformPoint(corner));var p=new Vector2(v.x*Field.rect.width,-(1-v.y)*Field.rect.height);min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private Rect BodyRect(GridPosition p)
        {
            foreach(var item in bodyRects)if(positions[item.Key].Equals(p))return item.Value;
            var at=Point(p,.12f);return new Rect(at.x-12,at.y-12,24,24);
        }
        private static Vector2 Outside(Rect r,Vector2 direction)
        {
            var n=direction.normalized;var distance=Mathf.Min(Mathf.Abs(n.x)>.001f?r.width/2/Mathf.Abs(n.x):float.MaxValue,Mathf.Abs(n.y)>.001f?r.height/2/Mathf.Abs(n.y):float.MaxValue);
            return r.center+n*(distance+7);
        }
        private void Link(RectTransform strip,GridPosition a,GridPosition b)
        {
            var from=BodyRect(a);var to=BodyRect(b);var d=to.center-from.center;
            var direct=new[]{Outside(from,d),Outside(to,-d)};
            var routes=new List<Vector2[]>{direct};
            // A short side lane keeps vertical arrows out of upright portraits and foot HP.
            foreach(var x in new[]{(from.center.x+to.center.x)/2,Mathf.Max(from.xMax,to.xMax)+18,Mathf.Min(from.xMin,to.xMin)-18})
            {
                if(x>from.xMin-7 && x<from.xMax+7 || x>to.xMin-7 && x<to.xMax+7)continue;
                var start=new Vector2(x>from.center.x?from.xMax+7:from.xMin-7,from.center.y);
                var end=new Vector2(x>to.center.x?to.xMax+7:to.xMin-7,to.center.y);
                routes.Add(new[]{start,new Vector2(x,start.y),new Vector2(x,end.y),end});
            }
            var obstacles=bodyRects.Values.Concat(badges.Values.Where(v=>v.gameObject.activeInHierarchy).Select(v=>{
                var r=v.GetComponent<RectTransform>();return new Rect(r.anchoredPosition.x,r.anchoredPosition.y-r.rect.height,r.rect.width,r.rect.height);
            })).ToArray();
            var path=routes.OrderBy(r=>Crossings(r,obstacles)).ThenBy(r=>Enumerable.Range(1,r.Length-1).Sum(i=>Vector2.Distance(r[i-1],r[i]))).First();
            if(Crossings(path,obstacles)>0)path=ClearRoute(from,to,obstacles,path);
            strip.anchoredPosition=Vector2.zero;strip.sizeDelta=Field.rect.size;strip.GetComponent<MobileRouteGraphic>().SetPath(path);
        }
        private static int Crossings(Vector2[] path,Rect[] obstacles)
        {
            var count=0;
            foreach(var r in obstacles)for(var i=1;i<path.Length;i++)
                for(var s=0;s<=16;s++)if(new Rect(r.x-4,r.y-4,r.width+8,r.height+8).Contains(Vector2.Lerp(path[i-1],path[i],s/16f))){count++;break;}
            return count;
        }
        private static RectTransform Strip(Transform parent,string name,Color color)
        {var r=Rect(parent,name,0,0,1,3);var g=r.gameObject.AddComponent<MobileRouteGraphic>();g.color=color;g.Dashed=name=="Guard link";g.raycastTarget=false;return r;}
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=5;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        private Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size,TextAnchor anchor=TextAnchor.UpperLeft)
        {var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=JapaneseFont;t.fontSize=size;t.text=value;t.color=Ink;t.alignment=anchor;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        private void MakeButton(Transform parent,string id,string name,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,int size)
        {var r=Rect(parent,id,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.color=new Color(.12f,.24f,.26f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;b.onClick.AddListener(action);buttons[id]=b;Label(r,"Label",name,4,2,w-8,h-4,size,TextAnchor.MiddleCenter);}
        private void OnDestroy(){if(knownPositionTexture!=null)Destroy(knownPositionTexture);if(contactTexture!=null)Destroy(contactTexture);if(runtimeProfile!=null)Destroy(runtimeProfile);if(fieldTarget!=null){fieldTarget.Release();Destroy(fieldTarget);}foreach(var m in materials)Destroy(m);}
    }
    // Draws only the supplied Presentation path; it never selects a gameplay target.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MobileRouteGraphic : MaskableGraphic
    {
        public bool Dashed;
        private float progress=1;
        public float Progress {get=>progress;set{progress=value;SetVerticesDirty();}}
        private Vector2[] path=Array.Empty<Vector2>();
        public IReadOnlyList<Vector2> Points => path;
        public void SetPath(Vector2[] points){path=points;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(path.Length<2)return;
            var outline=new Color(.015f,.035f,.045f,.98f);
            if(progress<1){var sum=0f;for(var k=1;k<path.Length;k++)sum+=Vector2.Distance(path[k-1],path[k]);var remaining=sum*progress;var partial=new System.Collections.Generic.List<Vector2>{path[0]};for(var k=1;k<path.Length;k++){var length=Vector2.Distance(path[k-1],path[k]);if(remaining>=length){partial.Add(path[k]);remaining-=length;}else{partial.Add(Vector2.Lerp(path[k-1],path[k],length>0?remaining/length:0));break;}}DrawPath(vh,partial.ToArray(),outline);return;}
            DrawPath(vh,path,outline);
        }
        private void DrawPath(VertexHelper vh,Vector2[] path,Color outline)
        {
            if(path.Length<2)return;
            for(var k=0;k<12;k++){var a=k*Mathf.PI/6;var b=(k+1)*Mathf.PI/6;Segment(vh,path[0]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5,path[0]+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*5,2,color);}
            for(var i=1;i<path.Length;i++)Segment(vh,path[i-1],path[i],7,outline);
            for(var i=1;i<path.Length;i++)
            {
                var d=path[i]-path[i-1];var length=d.magnitude;if(length<.01f)continue;
                if(!Dashed)Segment(vh,path[i-1],path[i],4,color);
                else for(var at=0f;at<length;at+=12)Segment(vh,path[i-1]+d.normalized*at,path[i-1]+d.normalized*Mathf.Min(at+7,length),4,color);
            }
            var end=path[path.Length-1];var last=end-path[path.Length-2];if(last.sqrMagnitude<1)return;
            var n=last.normalized;var side=new Vector2(-n.y,n.x);var size=Mathf.Min(12,last.magnitude*.8f);
            Triangle(vh,end+n*2,end-n*(size+2)+side*10,end-n*(size+2)-side*10,outline);
            Triangle(vh,end,end-n*size+side*7,end-n*size-side*7,color);
        }
        private static void Segment(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            var d=b-a;if(d.sqrMagnitude<.01f)return;var n=new Vector2(-d.y,d.x).normalized*(width/2);
            var start=vh.currentVertCount;foreach(var p in new[]{a-n,b-n,b+n,a+n})vh.AddVert(p,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {var start=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);}
    }
}
