using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// The look of each dungeon floor (design: specs/07 §8). Display only: it never reads or writes Core, and it never
    /// moves, renames or resizes a measured object ("Fixture &lt;id&gt;", "Private approved static skin", "Fixture slime",
    /// the "Courtyard" paving). A floor is made of three things over the existing sky-ruins world:
    /// <list type="bullet">
    /// <item>the sky (the same procedural sky with another palette, stars and a moon),</item>
    /// <item>the authored ruins recoloured through material copies (light, paving, moss, leaves, banners, lanterns, water),</item>
    /// <item>a few additive props on the island's rim and in the sky, never on a board cell.</item>
    /// </list>
    /// Floor 1 is the approved sky ruins exactly: <see cref="Apply"/>(1, ...) restores every original material and removes
    /// every prop, so the world is as if no look had been applied. Characters (puppets, spirits, HUD) use their own
    /// shaders and are untouched by any floor.
    /// </summary>
    public sealed class HqFloorLook : MonoBehaviour
    {
        public const int Floors=5;

        /// <summary>One floor's palette and props. Colours are in each shader's own space, next to floor 1's values.</summary>
        public sealed class Look
        {
            public int Floor;public string Name,Mood,Enemies;
            // Sky (RogueTactics/HqSky): horizon, zenith, sun glow and where it sits, the far ruins, clouds, falls.
            public Vector4 SkyLow,SkyHigh,SkySun,SkySunAt,RockDark,RockLit,RockMoss,RockRim,Cloud,CloudShade,Fall;
            public Vector4 Stars,Moon;   // Stars: amount, rgb. Moon: uv, radius, amount.
            // Ruins (RogueTactics/AuthoredRuins): light on the shadow and lit sides; paving, moss, clover, blooms (sRGB); water.
            public Vector4 LightCool,LightWarm,Stone,Moss,Clover,Petal,Heart,WaterDeep,WaterClear;
            // Multiplies per part; hue recolours (rgb toward, a = amount).
            public Color CourtTint=Color.white,CliffTint=Color.white,DistantTint=Color.white,PlantTint=Color.white,WaterTint=Color.white;
            public Color LeafHue,FlowerHue,BannerHue,LanternHue,GoldHue;
            public Color LanternGlow=Color.black;   // added on the lantern parts (lit windows at night)
            public Color ReferenceTint=Color.white; // the cut-out rim parts (ivy block, banner crate, lantern pillar)
            public Action<HqFloorLook> Props;
        }

        private static Look[] looks;
        public static IReadOnlyList<Look> All=>looks??=Build();
        public static Look For(int floor)
        {
            if(floor<1 || floor>Floors)throw new ArgumentOutOfRangeException(nameof(floor),floor,"floors are 1.."+Floors);
            return All[floor-1];
        }

        /// <summary>The floor currently shown (1 when no look has been applied).</summary>
        public int Floor{get;private set;}=1;
        public Transform PropRoot{get;private set;}
        private Camera view;private Vector3 centre;private Bounds court;
        private readonly Dictionary<Renderer,Material> originals=new Dictionary<Renderer,Material>();
        private readonly List<Material> owned=new List<Material>();
        private readonly List<UnityEngine.Object> propAssets=new List<UnityEngine.Object>();
        private readonly List<Transform> facing=new List<Transform>();
        private readonly List<(Transform t,Vector3 at,float phase,float speed)> drifting=new List<(Transform,Vector3,float,float)>();

        /// <summary>
        /// Shows floor <paramref name="floor"/> (1..5) over the sky-ruins world already built by the view. Idempotent and
        /// reversible: applying 1 restores the approved floor-1 world. <paramref name="field"/> is the board camera
        /// (MobileG1View.FieldCamera); props that face the camera turn with it.
        /// </summary>
        public static HqFloorLook Apply(int floor,Camera field)
        {
            var look=For(floor);
            var owner=FindFirstObjectByType<HqFloorLook>();
            if(owner==null)
            {
                if(floor==1)return null;   // nothing to undo: floor 1 is the world as built
                owner=new GameObject("HQ floor look").AddComponent<HqFloorLook>();
            }
            owner.view=field;owner.Restore();
            if(floor==1)return owner;
            owner.Floor=floor;owner.Recolour(look);owner.BuildProps(look);
            return owner;
        }

        // ---- Recolour -------------------------------------------------------------------------------------

        private void Recolour(Look look)
        {
            var ruins=GameObject.Find("HQ Sky Ruins Environment");
            centre=ruins!=null?ruins.transform.position:new Vector3(5,0,5);
            court=new Bounds(centre,new Vector3(17,0,17));
            foreach(var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var m=r.sharedMaterial;if(m==null || m.shader==null || OnAFigure(r.transform))continue;
                if(r.name=="Courtyard" && ruins!=null && r.transform.IsChildOf(ruins.transform))court=r.bounds;
                switch(m.shader.name)
                {
                    case "RogueTactics/AuthoredRuins":Swap(r,c=>Ruins(c,r.name,look));break;
                    case "RogueTactics/HqSky":Swap(r,c=>Sky(c,look));break;
                    default:
                        if(r.transform.parent!=null && r.transform.parent.name=="Private reference environment parts" && m.HasProperty("_Color"))
                            Swap(r,c=>c.SetColor("_Color",m.GetColor("_Color")*look.ReferenceTint));
                        else if(r.name=="Continuous bridge support" && m.HasProperty("_BaseColor"))
                            Swap(r,c=>c.SetColor("_BaseColor",m.GetColor("_BaseColor")*look.CliffTint));
                        break;
                }
            }
        }
        // Characters, their shadows, markers and HUD are never recoloured, whatever shader they use.
        private static bool OnAFigure(Transform t)
        {
            for(;t!=null;t=t.parent)if(t.name.StartsWith("Fixture "))return true;
            return false;
        }
        private void Swap(Renderer r,Action<Material> set)
        {
            var source=r.sharedMaterial;var copy=new Material(source){name=source.name+" (floor "+Floor+")"};
            set(copy);owned.Add(copy);originals[r]=source;r.sharedMaterial=copy;
        }
        private static void Sky(Material m,Look k)
        {
            m.EnableKeyword("_FLOOR_LOOK");
            m.SetVector("_SkyLow",k.SkyLow);m.SetVector("_SkyHigh",k.SkyHigh);m.SetVector("_SkySun",k.SkySun);m.SetVector("_SkySunAt",k.SkySunAt);
            m.SetVector("_SkyRockDark",k.RockDark);m.SetVector("_SkyRockLit",k.RockLit);m.SetVector("_SkyRockMoss",k.RockMoss);m.SetVector("_SkyRockRim",k.RockRim);
            m.SetVector("_SkyCloud",k.Cloud);m.SetVector("_SkyCloudShade",k.CloudShade);m.SetVector("_SkyFall",k.Fall);
            m.SetVector("_SkyStars",k.Stars);m.SetVector("_SkyMoon",k.Moon);
        }
        private static void Ruins(Material m,string part,Look k)
        {
            m.EnableKeyword("_FLOOR_LOOK");
            m.SetVector("_LookCool",k.LightCool);m.SetVector("_LookWarm",k.LightWarm);m.SetVector("_LookStone",k.Stone);m.SetVector("_LookMoss",k.Moss);
            m.SetVector("_LookClover",k.Clover);m.SetVector("_LookPetal",k.Petal);m.SetVector("_LookHeart",k.Heart);
            m.SetVector("_LookDeep",k.WaterDeep);m.SetVector("_LookClear",k.WaterClear);
            var tint=k.CliffTint;var hue=new Vector4(1,1,1,0);var glow=Color.black;
            bool Is(string name)=>part==name || part.StartsWith(name+" ") || part.StartsWith("HeroLedge"+name);
            if(Is("Courtyard") || part=="Authored passage stone" || part.StartsWith("Stone passage"))tint=k.CourtTint;
            else if(Is("Foliage")){tint=k.PlantTint;hue=k.LeafHue;}
            else if(Is("Flowers")){tint=k.PlantTint;hue=k.FlowerHue;}
            else if(Is("Blue"))hue=k.BannerHue;
            else if(Is("Gold"))hue=k.GoldHue;
            else if(Is("Warm")){tint=Color.white;hue=k.LanternHue;glow=k.LanternGlow;}
            else if(Is("Water") || Is("Foam"))tint=k.WaterTint;
            else if(Is("Distant"))tint=k.DistantTint;
            m.SetVector("_LookTint",(Vector4)tint);m.SetVector("_LookHue",hue);m.SetVector("_LookGlow",(Vector4)glow);
        }

        private void Restore()
        {
            foreach(var pair in originals)
            {
                if(pair.Key==null)continue;var copy=pair.Key.sharedMaterial;
                // HqAmbient.SetReduced may have stopped the water on the copy meanwhile: carry that back.
                if(copy!=null && copy!=pair.Value && copy.HasProperty("_FlowSpeed") && pair.Value.HasProperty("_FlowSpeed"))pair.Value.SetFloat("_FlowSpeed",copy.GetFloat("_FlowSpeed"));
                pair.Key.sharedMaterial=pair.Value;
            }
            originals.Clear();
            foreach(var m in owned)if(m!=null)Destroy(m);owned.Clear();
            // Hidden at once (Destroy waits for the end of the frame), so a restored floor 1 renders clean this frame.
            if(PropRoot!=null){PropRoot.gameObject.SetActive(false);Destroy(PropRoot.gameObject);}PropRoot=null;
            foreach(var a in propAssets)if(a!=null)Destroy(a);propAssets.Clear();
            facing.Clear();drifting.Clear();Floor=1;
        }
        private void OnDestroy()=>Restore();

        // ---- Props --------------------------------------------------------------------------------------
        // The paving covers the 5x5 board almost edge to edge, so nothing is placed on the island's top. Props are
        // (1) small floating islets in open sky: below the island and in the free upper-right corner, (2) things hanging
        // on the island's front cliff face, (3) a few drifting floaters at the sides. All are placed in the board camera's
        // frame (x = screen right, s = screen up, from the board centre) where the screen is empty: below the island's
        // lowest cell and outside the figures' columns. The face tests in GfxFloorLookGalleryTests check none covers a figure.

        private void BuildProps(Look look)
        {
            PropRoot=new GameObject("Floor "+look.Floor+" props").transform;PropRoot.SetParent(transform,false);
            look.Props?.Invoke(this);
        }
        /// <summary>World point at screen offset (x right, s up) from the board centre, d further along the view.</summary>
        public Vector3 Screen(float x,float s,float d)
        {
            var t=view!=null?view.transform:null;
            if(t==null)return centre+new Vector3(x,0,s);
            return centre+t.right*x+t.up*s+t.forward*d;
        }
        /// <summary>A point on the island's front cliff face (board-relative x), y below the paving.</summary>
        public Vector3 Cliff(float x,float y)=>centre+new Vector3(x,y,-5.65f);
        /// <summary>The paving's world bounds.</summary>
        public Bounds Court=>court;
        private float Yaw=>view!=null?view.transform.eulerAngles.y:0;

        private Shader toon,glowShader;
        public Material Toon(Color c,Color shade,float gloss=.2f,float flat=0,float rim=.3f)
        {
            if(toon==null)toon=Resources.Load<Shader>("Gfx/HqCreature");
            Material m;
            if(toon!=null)
            {
                m=new Material(toon);m.SetColor("_Color",c);m.SetColor("_Shade",shade);m.SetFloat("_Gloss",gloss);m.SetFloat("_Flat",flat);
                m.SetColor("_Rim",new Color(1,1,.92f,rim));
            }
            else {m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.color=c;}
            propAssets.Add(m);return m;
        }
        /// <summary>A soft additive glow on a camera-facing quad.</summary>
        public Transform Glow(string name,Vector3 at,float size,Color tint,float strength=.6f,Transform parent=null)
        {
            if(glowShader==null)glowShader=Resources.Load<Shader>("Gfx/HqFloorGlow");if(glowShader==null)return null;
            var m=new Material(glowShader){name=name};m.SetColor("_Tint",tint);m.SetFloat("_Strength",strength);propAssets.Add(m);
            var t=Part(name,Quad(size),m,parent);t.position=at;facing.Add(t);return t;
        }
        public Transform Part(string name,Mesh mesh,Material m,Transform parent)
        {
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent??PropRoot,false);
            o.GetComponent<MeshFilter>().sharedMesh=mesh;var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=m;
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return o.transform;
        }
        /// <summary>A primitive under <paramref name="parent"/> at a local position, size and rotation.</summary>
        public static Transform Put(PrimitiveType type,string name,Transform parent,Vector3 local,Vector3 size,Material m,Vector3 euler=default)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;var c=o.GetComponent<Collider>();if(c!=null)Destroy(c);
            o.transform.SetParent(parent,false);o.transform.localPosition=local;o.transform.localScale=size;o.transform.localRotation=Quaternion.Euler(euler);
            var r=o.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return o.transform;
        }
        /// <summary>
        /// An empty group at a world point. Tilted back by <paramref name="tilt"/> degrees so that, under the steep board
        /// camera, upright things on it read upright instead of squashed (75 would face the camera fully).
        /// </summary>
        public Transform Group(string name,Vector3 at,float tilt=0,float turn=0,Transform parent=null)
        {
            var g=new GameObject(name).transform;g.SetParent(parent??PropRoot,false);g.position=at;g.rotation=Quaternion.Euler(tilt,Yaw+turn,0);return g;
        }
        /// <summary>A lathe-turned solid (profile x = radius, y = height) under <paramref name="parent"/>.</summary>
        public Transform Turned(string name,Vector2[] profile,Material m,Transform parent,Vector3 local,float scale=1,Vector3 euler=default)
        {
            var mesh=Lathe(profile,28);propAssets.Add(mesh);var t=Part(name,mesh,m,parent);
            t.localPosition=local;t.localScale=Vector3.one*scale;t.localRotation=Quaternion.Euler(euler);return t;
        }
        /// <summary>
        /// A floating islet of rock: a jagged inverted cone with a cap of the floor's ground. Returns the group whose
        /// local y = 0 is the cap's top, tilted for the steep camera.
        /// </summary>
        public Transform Islet(string name,Vector3 at,float radius,Material rock,Material rockDark,Material cap,float tilt=50)
        {
            var g=Group(name,at,tilt);
            Turned(name+" rock",new[]{new Vector2(0,-1.9f),new Vector2(.18f,-1.7f),new Vector2(.38f,-1.25f),new Vector2(.62f,-.78f),new Vector2(.86f,-.36f),new Vector2(1f,-.10f),new Vector2(.98f,0),new Vector2(0,.02f)},rock,g,Vector3.zero,radius);
            // Two hanging shards break the cone's regularity.
            Put(PrimitiveType.Sphere,name+" shard",g,new Vector3(.42f,-.95f,.1f)*radius,new Vector3(.34f,.9f,.34f)*radius,rockDark,new Vector3(0,0,14));
            Put(PrimitiveType.Sphere,name+" shard",g,new Vector3(-.36f,-.70f,-.2f)*radius,new Vector3(.28f,.7f,.28f)*radius,rockDark,new Vector3(0,0,-18));
            Put(PrimitiveType.Sphere,name+" cap",g,new Vector3(0,.02f,0),new Vector3(2.06f,.22f,2.06f)*radius,cap);
            return g;
        }
        /// <summary>Slow vertical bobbing for things that float. <see cref="Still"/> stops it (reduced motion, captures).</summary>
        public void Drift(Transform t,float phase,float speed=1){if(t!=null)drifting.Add((t,t.localPosition,phase,speed));}
        public bool Still{get;set;}

        private void LateUpdate()
        {
            if(view==null)return;
            foreach(var t in facing)if(t!=null)t.rotation=Quaternion.LookRotation(view.transform.forward,view.transform.up);
            if(Still)return;var time=Time.unscaledTime;
            foreach(var (t,at,phase,speed) in drifting)if(t!=null)t.localPosition=at+Vector3.up*Mathf.Sin(time*.8f*speed+phase)*.12f;
        }

        private static Mesh Lathe(Vector2[] profile,int sides)
        {
            var mesh=new Mesh{name="Floor prop lathe"};var v=new List<Vector3>();var t=new List<int>();var rows=profile.Length;
            for(var i=0;i<=sides;i++)
            {
                var a=i*2*Mathf.PI/sides;var c=Mathf.Cos(a);var s=Mathf.Sin(a);
                // A little irregularity around the turn so rocks and caps do not look machined.
                var k=1+.06f*Mathf.Sin(a*3+1.3f)+.04f*Mathf.Sin(a*7);
                foreach(var p in profile)v.Add(new Vector3(c*p.x*k,p.y,s*p.x*k));
            }
            for(var i=0;i<sides;i++)for(var j=0;j<rows-1;j++)
            {var n=i*rows+j;t.AddRange(new[]{n,n+1,n+rows,n+1,n+rows+1,n+rows});}
            mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private Mesh Quad(float size)
        {
            var mesh=new Mesh{name="Floor glow quad"};var h=size/2;
            mesh.SetVertices(new List<Vector3>{new Vector3(-h,-h,0),new Vector3(h,-h,0),new Vector3(h,h,0),new Vector3(-h,h,0)});
            mesh.SetUVs(0,new List<Vector2>{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)});
            mesh.SetTriangles(new[]{0,2,1,0,3,2},0);mesh.RecalculateBounds();propAssets.Add(mesh);return mesh;
        }

        // Where the islets float, in the board camera's frame: two in the open sky under the island's left and right
        // undersides (above the cards), one in the upper-right corner beside the arch, right of every figure's column.
        private static readonly (float x,float s,float r)[] IsletSpots={(-5.3f,-7.6f,1.0f),(5.9f,-8.9f,.8f),(6.9f,5.4f,.58f)};
        private const float IsletDepth=-8f;

        // ---- The five floors ----------------------------------------------------------------------------

        private static Vector4 V(float x,float y,float z,float w=0)=>new Vector4(x,y,z,w);
        private static Color C(float r,float g,float b,float a=1)=>new Color(r,g,b,a);

        private static Look[] Build()=>new[]
        {
            // 1. The approved sky ruins: never recoloured (Apply(1) restores the originals). Values = shader defaults.
            new Look{Floor=1,Name="天空の遺跡",Mood="朝の光。白い石と青い旗、雲の海",Enemies="金の番兵・碧の番兵",
                SkyLow=V(.80f,.90f,.97f),SkyHigh=V(.27f,.55f,.86f),SkySun=V(.30f,.24f,.12f),SkySunAt=V(.18f,.93f,0),
                RockDark=V(.40f,.45f,.62f),RockLit=V(.95f,.92f,.86f),RockMoss=V(.56f,.72f,.55f),RockRim=V(.14f,.11f,.05f),
                Cloud=V(1,.99f,.96f),CloudShade=V(.70f,.79f,.90f),Fall=V(.93f,.97f,1),Stars=V(0,1,1,1),Moon=V(.8f,.88f,.035f,0),
                LightCool=V(.78f,.87f,1),LightWarm=V(1,.98f,.91f),Stone=V(.96f,.89f,.76f),Moss=V(.50f,.66f,.33f),Clover=V(.36f,.58f,.22f),
                Petal=V(.99f,.97f,.92f),Heart=V(1,.82f,.35f),WaterDeep=V(.12f,.48f,.62f),WaterClear=V(.50f,.84f,.88f),
                LeafHue=C(1,1,1,0),FlowerHue=C(1,1,1,0),BannerHue=C(1,1,1,0),LanternHue=C(1,1,1,0),GoldHue=C(1,1,1,0)},

            // 2. Moss garden: where the moss and bloom spirits live. Humid, green-gold, violet bells.
            new Look{Floor=2,Name="翠苔の庭",Mood="しっとりした昼前。苔と蔓が石を覆い、紫の釣鐘花が咲く",Enemies="苔の精・花の精",
                SkyLow=V(.80f,.94f,.86f),SkyHigh=V(.20f,.56f,.62f),SkySun=V(.26f,.28f,.10f),SkySunAt=V(.84f,.92f,0),
                RockDark=V(.30f,.44f,.44f),RockLit=V(.86f,.93f,.80f),RockMoss=V(.42f,.72f,.40f),RockRim=V(.10f,.13f,.04f),
                Cloud=V(.96f,1,.94f),CloudShade=V(.60f,.80f,.74f),Fall=V(.86f,.98f,.94f),Stars=V(0,1,1,1),Moon=V(.8f,.88f,.035f,0),
                LightCool=V(.72f,.90f,.90f),LightWarm=V(1,1,.88f),Stone=V(.90f,.90f,.74f),Moss=V(.40f,.66f,.28f,.5f),Clover=V(.30f,.62f,.22f),
                Petal=V(.88f,.74f,.98f,.08f),Heart=V(.98f,.86f,.42f),WaterDeep=V(.08f,.44f,.46f),WaterClear=V(.46f,.88f,.76f),
                CliffTint=C(.94f,1,.94f),PlantTint=C(1,1.04f,1),DistantTint=C(.92f,1,.96f),
                LeafHue=C(.36f,.68f,.26f,.45f),FlowerHue=C(.72f,.50f,.94f,.8f),BannerHue=C(.22f,.56f,.48f,.9f),LanternHue=C(1,1,1,0),GoldHue=C(.78f,.86f,.50f,.4f),
                Props=GardenProps},

            // 3. Sunset colonnade: the middle of the climb. Honey stone, long amber light, broken columns, paper lanterns.
            new Look{Floor=3,Name="夕映えの回廊",Mood="傾いた陽。蜂蜜色の石、橙の雲、灯りがともり始める",Enemies="苔の精・花の精（3体の部屋）",
                SkyLow=V(1,.80f,.50f),SkyHigh=V(.62f,.48f,.62f),SkySun=V(.52f,.28f,.07f),SkySunAt=V(.80f,.76f,0),
                RockDark=V(.42f,.32f,.52f),RockLit=V(1,.80f,.60f),RockMoss=V(.64f,.62f,.42f),RockRim=V(.34f,.14f,.02f),
                Cloud=V(1,.86f,.72f),CloudShade=V(.62f,.52f,.70f),Fall=V(1,.90f,.80f),Stars=V(0,1,1,1),Moon=V(.8f,.88f,.035f,0),
                LightCool=V(.76f,.72f,.92f),LightWarm=V(1,.90f,.74f),Stone=V(.98f,.86f,.70f),Moss=V(.58f,.58f,.30f,-.3f),Clover=V(.54f,.52f,.22f),
                Petal=V(1,.92f,.80f),Heart=V(1,.60f,.24f),WaterDeep=V(.22f,.34f,.54f),WaterClear=V(.74f,.74f,.80f),
                CliffTint=C(1,.92f,.86f),PlantTint=C(1,.96f,.90f),DistantTint=C(1,.86f,.80f),
                LeafHue=C(.86f,.56f,.24f,.6f),FlowerHue=C(1,.76f,.52f,.5f),BannerHue=C(.92f,.60f,.20f,.9f),LanternHue=C(1,1,1,0),GoldHue=C(1,.72f,.36f,.3f),
                LanternGlow=C(.10f,.04f,0),Props=ColonnadeProps},

            // 4. Moonlit mushroom hollow: cap wardens and shadow bats. Night; the board is a moonlit clearing, glow is teal.
            new Look{Floor=4,Name="月影の茸窟",Mood="夜。月明かりの石、青緑に光る茸と蛍、遠くの島は影",Enemies="茸の番人・影こうもり",
                SkyLow=V(.22f,.26f,.44f),SkyHigh=V(.04f,.06f,.16f),SkySun=V(.10f,.14f,.26f),SkySunAt=V(.80f,.74f,0),
                RockDark=V(.08f,.10f,.20f),RockLit=V(.34f,.40f,.56f),RockMoss=V(.18f,.40f,.44f),RockRim=V(.04f,.12f,.16f),
                Cloud=V(.40f,.46f,.64f),CloudShade=V(.14f,.18f,.34f),Fall=V(.46f,.66f,.78f),Stars=V(1,.92f,.96f,1),Moon=V(.80f,.74f,.032f,1),
                LightCool=V(.70f,.70f,.84f),LightWarm=V(1,.97f,.95f),Stone=V(.92f,.88f,.82f),Moss=V(.26f,.54f,.56f),Clover=V(.28f,.64f,.60f),
                Petal=V(.72f,.98f,.94f,.05f),Heart=V(.50f,1,.88f),WaterDeep=V(.06f,.20f,.40f),WaterClear=V(.30f,.62f,.80f),
                CourtTint=C(.95f,.96f,1),CliffTint=C(.50f,.54f,.72f),PlantTint=C(.62f,.68f,.82f),DistantTint=C(.30f,.34f,.52f),WaterTint=C(.70f,.80f,1),
                LeafHue=C(.20f,.46f,.50f,.7f),FlowerHue=C(.50f,.96f,.90f,.85f),BannerHue=C(.36f,.38f,.72f,.9f),LanternHue=C(.40f,1,.86f,1),GoldHue=C(.60f,.66f,.86f,.5f),
                LanternGlow=C(.10f,.30f,.26f),ReferenceTint=C(.62f,.68f,.86f),Props=HollowProps},

            // 5. Guardian's sanctum: above the clouds at the day's last light. Carved monoliths share the guardian's amber glyph.
            new Look{Floor=5,Name="星守の聖域",Mood="雲の上の黄昏。薔薇色の地平と藍の天、琥珀の紋が灯る石柱",Enemies="遺跡の守護者",
                SkyLow=V(.96f,.62f,.56f),SkyHigh=V(.06f,.06f,.22f),SkySun=V(.42f,.20f,.10f),SkySunAt=V(.22f,.70f,0),
                RockDark=V(.22f,.20f,.36f),RockLit=V(.82f,.70f,.68f),RockMoss=V(.46f,.52f,.48f),RockRim=V(.32f,.16f,.06f),
                Cloud=V(.98f,.78f,.72f),CloudShade=V(.40f,.32f,.54f),Fall=V(.92f,.82f,.84f),Stars=V(.9f,1,.92f,.80f),Moon=V(.8f,.88f,.035f,0),
                LightCool=V(.70f,.70f,.90f),LightWarm=V(1,.92f,.80f),Stone=V(.95f,.86f,.72f),Moss=V(.52f,.56f,.40f,-.4f),Clover=V(.46f,.50f,.30f),
                Petal=V(.98f,.92f,.80f),Heart=V(1,.70f,.30f),WaterDeep=V(.18f,.24f,.48f),WaterClear=V(.64f,.62f,.84f),
                CliffTint=C(.80f,.78f,.90f),PlantTint=C(.84f,.84f,.90f),DistantTint=C(.62f,.56f,.74f),
                LeafHue=C(.46f,.56f,.40f,.3f),FlowerHue=C(1,.80f,.50f,.5f),BannerHue=C(.86f,.64f,.24f,.9f),LanternHue=C(1,.78f,.40f,.6f),GoldHue=C(1,.76f,.36f,.4f),
                LanternGlow=C(.08f,.04f,0),ReferenceTint=C(.86f,.82f,.90f),Props=SanctumProps},
        };


        // Floor 2: islets of moss with violet bell-flowers, vines down the cliff, drifting pollen.
        private static void GardenProps(HqFloorLook f)
        {
            var rock=f.Toon(C(.66f,.74f,.70f),C(.40f,.50f,.52f));var rockDark=f.Toon(C(.50f,.60f,.58f),C(.30f,.40f,.42f));
            var moss=f.Toon(C(.52f,.80f,.34f),C(.28f,.56f,.30f),.1f);var stem=f.Toon(C(.40f,.68f,.30f),C(.22f,.46f,.22f));
            var leaf=f.Toon(C(.50f,.80f,.32f),C(.26f,.54f,.26f));var bell=f.Toon(C(.76f,.54f,.96f),C(.48f,.30f,.74f),.4f);
            var throat=f.Toon(C(1,.94f,.80f),C(.96f,.80f,.70f),.1f,.7f);var vine=f.Toon(C(.36f,.62f,.30f),C(.20f,.42f,.22f));
            var pollen=C(1,.92f,.50f);
            void Bell(Transform g,Vector3 at,float h,float s,float lean)
            {
                Put(PrimitiveType.Cylinder,"Bell stem",g,at+new Vector3(lean*h*.25f,h*.5f,0),new Vector3(.10f*s,h*.5f,.10f*s),stem,new Vector3(0,0,-lean*25));
                Put(PrimitiveType.Sphere,"Bell leaf",g,at+new Vector3(.26f*s,h*.22f,0),new Vector3(.54f*s,.08f*s,.24f*s),leaf,new Vector3(0,15,-30));
                Put(PrimitiveType.Sphere,"Bell leaf",g,at+new Vector3(-.24f*s,h*.36f,0),new Vector3(.46f*s,.08f*s,.22f*s),leaf,new Vector3(0,-25,28));
                var head=at+new Vector3(lean*h*.5f,h,0);
                f.Turned("Bell",new[]{new Vector2(.02f,.58f),new Vector2(.26f,.54f),new Vector2(.36f,.32f),new Vector2(.42f,.08f),new Vector2(.56f,-.08f),new Vector2(.50f,-.04f)},bell,g,head+Vector3.down*.5f*s,s,new Vector3(0,0,-lean*40));
                Put(PrimitiveType.Sphere,"Bell throat",g,head+Vector3.down*.53f*s,new Vector3(.66f,.10f,.66f)*s,throat);
                f.Glow("Bell pollen glow",g.TransformPoint(head+Vector3.down*.55f*s),1.5f*s,pollen,.45f,g);
            }
            var spots=IsletSpots;
            var a=f.Islet("Moss islet",f.Screen(spots[0].x,spots[0].s,IsletDepth),spots[0].r,rock,rockDark,moss);
            Bell(a,new Vector3(-.35f,0,0),1.7f,1.0f,.25f);Bell(a,new Vector3(.45f,0,.2f),1.1f,.7f,-.35f);
            Put(PrimitiveType.Sphere,"Moss mound",a,new Vector3(.2f,.08f,-.5f),new Vector3(.9f,.4f,.7f),moss);
            var b=f.Islet("Moss islet",f.Screen(spots[1].x,spots[1].s,IsletDepth),spots[1].r,rock,rockDark,moss);
            Bell(b,new Vector3(.1f,0,0),1.3f,.85f,-.2f);
            Put(PrimitiveType.Sphere,"Moss mound",b,new Vector3(-.45f,.06f,-.3f),new Vector3(.7f,.35f,.6f),moss);
            var c=f.Islet("Moss islet",f.Screen(spots[2].x,spots[2].s,IsletDepth*.5f),spots[2].r,rock,rockDark,moss);
            Bell(c,Vector3.zero,.8f,.55f,.2f);
            // Ivy curtains draped from the paving's front edge: a thin stem with alternating leaves, in clumps.
            for(var i=0;i<6;i++)
            {
                var x0=-5.4f+i*2.15f+.3f*Mathf.Sin(i*2.1f);
                for(var j=0;j<3;j++)
                {
                    var x=x0+(j-1)*.32f;var len=.9f+1.1f*Mathf.Abs(Mathf.Sin(i*1.3f+j*2.2f));var n=Mathf.RoundToInt(len/.22f);
                    var v=f.Group("Hanging ivy",f.Cliff(x,.02f),0);
                    Put(PrimitiveType.Capsule,"Ivy stem",v,new Vector3(0,-len*.5f,-.06f),new Vector3(.05f,len*.5f,.05f),vine);
                    for(var k=0;k<n;k++)Put(PrimitiveType.Sphere,"Ivy leaf",v,new Vector3((k%2==0?.09f:-.09f),-.12f-k*.22f,-.12f),new Vector3(.24f,.18f,.06f)*(1-.25f*k/n),k%3==0?moss:leaf,new Vector3(0,0,k%2==0?-35:35));
                }
            }
            for(var i=0;i<7;i++)
            {
                var side=i%2==0?-1:1;var g=f.Glow("Floating pollen",f.Screen(side*(7.2f+.3f*(i%3)),-7.5f+i*1.9f,-6),.65f,pollen,.75f);f.Drift(g,i*1.1f);
            }
        }

        // Floor 3: islets of warm sandstone with broken columns and paper lanterns, autumn shrubs, drifting leaves.
        private static void ColonnadeProps(HqFloorLook f)
        {
            var rock=f.Toon(C(.94f,.74f,.60f),C(.62f,.44f,.50f));var rockDark=f.Toon(C(.80f,.58f,.50f),C(.50f,.34f,.42f));
            var cap=f.Toon(C(1,.84f,.62f),C(.80f,.58f,.48f),.1f);
            var stone=f.Toon(C(1,.88f,.72f),C(.74f,.54f,.54f),.15f);var dark=f.Toon(C(.88f,.70f,.58f),C(.58f,.40f,.44f),.1f);
            var paper=f.Toon(C(1,.82f,.50f),C(1,.64f,.32f),.1f,.85f);var wood=f.Toon(C(.54f,.36f,.28f),C(.34f,.22f,.22f));
            var shrub=f.Toon(C(.96f,.62f,.26f),C(.68f,.34f,.20f),.1f);var shrub2=f.Toon(C(.88f,.44f,.22f),C(.58f,.24f,.18f),.1f);
            var lamp=C(1,.62f,.26f);
            void Column(Transform g,Vector3 at,float h,float w,bool broken)
            {
                Put(PrimitiveType.Cube,"Column plinth",g,at+Vector3.up*.12f*w,new Vector3(1.0f,.24f,1.0f)*w,dark);
                Put(PrimitiveType.Cylinder,"Column shaft",g,at+Vector3.up*(.24f*w+h*.5f),new Vector3(.62f*w,h*.5f,.62f*w),stone);
                if(broken)Put(PrimitiveType.Cylinder,"Column break",g,at+Vector3.up*(.24f*w+h),new Vector3(.64f*w,.05f,.64f*w),dark,new Vector3(14,0,-10));
                else Put(PrimitiveType.Cube,"Column capital",g,at+Vector3.up*(.24f*w+h+.1f),new Vector3(.92f,.22f,.92f)*w,dark);
            }
            void Lantern(Transform g,Vector3 at,float h,float s)
            {
                Put(PrimitiveType.Cylinder,"Lantern post",g,at+Vector3.up*h*.5f,new Vector3(.08f*s,h*.5f,.08f*s),wood);
                Put(PrimitiveType.Cube,"Lantern arm",g,at+new Vector3(.2f*s,h-.04f,0),new Vector3(.44f*s,.06f,.06f),wood);
                var body=at+new Vector3(.40f*s,h-.36f*s,0);
                Put(PrimitiveType.Sphere,"Lantern paper",g,body,new Vector3(.36f,.46f,.36f)*s,paper);
                Put(PrimitiveType.Cylinder,"Lantern cap",g,body+Vector3.up*.24f*s,new Vector3(.20f*s,.03f,.20f*s),wood);
                f.Glow("Lantern glow",g.TransformPoint(body),1.9f*s,lamp,.7f,g);
            }
            var spots=IsletSpots;
            var a=f.Islet("Sandstone islet",f.Screen(spots[0].x,spots[0].s,IsletDepth),spots[0].r,rock,rockDark,cap);
            Column(a,new Vector3(-.45f,0,.15f),1.9f,1f,false);Column(a,new Vector3(.35f,0,.25f),.9f,.9f,true);
            Put(PrimitiveType.Sphere,"Autumn shrub",a,new Vector3(.55f,.18f,-.45f),new Vector3(.75f,.5f,.6f),shrub);
            Put(PrimitiveType.Cube,"Fallen drum",a,new Vector3(-.1f,.16f,-.55f),new Vector3(.5f,.3f,.5f),stone,new Vector3(0,30,90));
            var b=f.Islet("Sandstone islet",f.Screen(spots[1].x,spots[1].s,IsletDepth),spots[1].r,rock,rockDark,cap);
            Lantern(b,new Vector3(-.2f,0,0),1.6f,1f);
            Put(PrimitiveType.Sphere,"Autumn shrub",b,new Vector3(.40f,.16f,-.3f),new Vector3(.62f,.44f,.52f),shrub2);
            var c=f.Islet("Sandstone islet",f.Screen(spots[2].x,spots[2].s,IsletDepth*.5f),spots[2].r,rock,rockDark,cap);
            Column(c,Vector3.zero,.8f,.6f,true);
            // Lanterns hung along the cliff's top edge (in front of the board's lowest row, below its feet).
            foreach(var x in new[]{-4.4f,0f,4.4f})
            {
                var g=f.Group("Cliff lantern",f.Cliff(x,-.25f),0);
                Put(PrimitiveType.Cylinder,"Lantern cord",g,new Vector3(0,-.2f,-.1f),new Vector3(.03f,.2f,.03f),wood);
                Put(PrimitiveType.Sphere,"Lantern paper",g,new Vector3(0,-.62f,-.12f),new Vector3(.36f,.46f,.36f),paper);
                f.Glow("Lantern glow",g.TransformPoint(new Vector3(0,-.62f,-.3f)),1.7f,lamp,.6f,g);
            }
            for(var i=0;i<6;i++)
            {
                var side=i%2==0?-1:1;var g=f.Group("Drifting leaf",f.Screen(side*(7.2f+.3f*(i%3)),-7.2f+i*2.1f,-6),0);
                Put(PrimitiveType.Sphere,"Leaf",g,Vector3.zero,new Vector3(.30f,.05f,.18f),i%2==0?shrub:shrub2,new Vector3(70,i*40,20));f.Drift(g,i*.9f,.7f);
            }
        }

        // Floor 4: dark islets carrying glowing teal mushrooms (never red: the cap warden's red stays its own), roots
        // hanging from the cliff, fireflies.
        private static void HollowProps(HqFloorLook f)
        {
            var rock=f.Toon(C(.36f,.40f,.56f),C(.18f,.20f,.34f));var rockDark=f.Toon(C(.26f,.28f,.42f),C(.12f,.14f,.26f));
            var cap=f.Toon(C(.30f,.48f,.52f),C(.16f,.28f,.36f),.1f);
            var stem=f.Toon(C(.88f,.92f,.98f),C(.54f,.60f,.76f),.2f);var teal=f.Toon(C(.30f,.88f,.82f),C(.12f,.48f,.58f),.45f,.4f);
            var blue=f.Toon(C(.44f,.68f,.98f),C(.22f,.36f,.68f),.45f,.35f);var gill=f.Toon(C(.72f,1,.94f),C(.48f,.88f,.88f),.1f,.9f);
            var root=f.Toon(C(.30f,.52f,.56f),C(.18f,.34f,.40f));var fly=C(.66f,1,.78f);var glow=C(.30f,1,.86f);
            void Shroom(Transform g,Vector3 at,float h,float r,Material capM,float tilt)
            {
                Put(PrimitiveType.Cylinder,"Mushroom stem",g,at+Vector3.up*h*.5f,new Vector3(.24f*r,h*.5f,.24f*r),stem,new Vector3(0,0,tilt));
                var top=at+new Vector3(-Mathf.Sin(tilt*Mathf.Deg2Rad)*h,h,0);
                f.Turned("Mushroom cap",new[]{new Vector2(0,.50f),new Vector2(.42f,.44f),new Vector2(.74f,.22f),new Vector2(.84f,.02f),new Vector2(.62f,-.03f),new Vector2(.1f,-.03f)},capM,g,top,r,new Vector3(0,0,tilt));
                Put(PrimitiveType.Cylinder,"Mushroom gills",g,top+Vector3.down*.03f*r,new Vector3(1.3f*r,.02f,1.3f*r),gill,new Vector3(0,0,tilt));
                f.Glow("Mushroom glow",g.TransformPoint(top),2.6f*r,glow,.5f,g);
            }
            var spots=IsletSpots;
            var a=f.Islet("Hollow islet",f.Screen(spots[0].x,spots[0].s,IsletDepth),spots[0].r,rock,rockDark,cap);
            Shroom(a,new Vector3(-.3f,0,.1f),1.5f,.95f,teal,8);Shroom(a,new Vector3(.55f,0,.2f),.8f,.55f,blue,-14);Shroom(a,new Vector3(.2f,0,-.55f),.45f,.38f,teal,10);
            var b=f.Islet("Hollow islet",f.Screen(spots[1].x,spots[1].s,IsletDepth),spots[1].r,rock,rockDark,cap);
            Shroom(b,new Vector3(.15f,0,0),1.1f,.75f,blue,-8);Shroom(b,new Vector3(-.5f,0,-.2f),.5f,.4f,teal,12);
            var c=f.Islet("Hollow islet",f.Screen(spots[2].x,spots[2].s,IsletDepth*.5f),spots[2].r,rock,rockDark,cap);
            Shroom(c,Vector3.zero,.6f,.45f,teal,6);
            // Small glowing caps tucked along the cliff's top edge, below the front row's feet.
            // Shelf mushrooms growing out of the cliff just under the paving's edge.
            for(var i=0;i<4;i++)
            {
                var g=f.Group("Cliff mushrooms",f.Cliff(-4.6f+i*3.1f,-.75f)+Vector3.back*.1f,0);
                f.Turned("Shelf mushroom",new[]{new Vector2(0,.16f),new Vector2(.36f,.12f),new Vector2(.52f,.02f),new Vector2(.40f,-.03f),new Vector2(0,-.03f)},i%2==0?teal:blue,g,Vector3.zero,1.1f,new Vector3(-12,0,0));
                f.Turned("Shelf mushroom",new[]{new Vector2(0,.12f),new Vector2(.26f,.09f),new Vector2(.38f,.02f),new Vector2(.30f,-.02f),new Vector2(0,-.02f)},i%2==0?blue:teal,g,new Vector3(.55f,-.42f,-.05f),1f,new Vector3(-12,0,0));
                f.Glow("Shelf glow",g.TransformPoint(new Vector3(.2f,-.15f,-.4f)),2.2f,glow,.35f,g);
            }
            // Glow-moss strands hanging off the edge, each ending in a bead of light.
            var bead=f.Toon(C(.70f,1,.92f),C(.50f,.90f,.86f),.1f,1);
            for(var i=0;i<9;i++)
            {
                var x=-5.8f+i*1.45f+.3f*Mathf.Sin(i*1.9f);var len=.8f+1.3f*Mathf.Abs(Mathf.Sin(i*2.7f));
                var g=f.Group("Glow moss strand",f.Cliff(x,.02f),0);
                Put(PrimitiveType.Capsule,"Moss strand",g,new Vector3(0,-len*.5f,-.06f),new Vector3(.06f,len*.5f,.06f),root);
                Put(PrimitiveType.Sphere,"Moss bead",g,new Vector3(0,-len,-.08f),Vector3.one*.16f,bead);
                f.Glow("Bead glow",g.TransformPoint(new Vector3(0,-len,-.2f)),.8f,glow,.6f,g);
            }
            for(var i=0;i<10;i++)
            {
                var side=i%2==0?-1:1;var g=f.Glow("Firefly",f.Screen(side*(6.9f+.35f*(i%3)),-8.2f+i*1.55f,-6),.55f,fly,1.0f);f.Drift(g,i*.7f,1.3f);
            }
        }

        // Floor 5: islets of blue-grey stone carrying carved monoliths with amber glyph bands (the guardian's mark),
        // floating rune stones.
        private static void SanctumProps(HqFloorLook f)
        {
            var rock=f.Toon(C(.60f,.62f,.78f),C(.34f,.34f,.52f));var rockDark=f.Toon(C(.46f,.46f,.64f),C(.24f,.24f,.40f));
            var cap=f.Toon(C(.90f,.82f,.70f),C(.62f,.54f,.58f),.1f);
            var stone=f.Toon(C(.64f,.68f,.82f),C(.36f,.38f,.56f),.15f);var dark=f.Toon(C(.48f,.50f,.66f),C(.26f,.26f,.42f),.1f);
            var glyph=f.Toon(C(1,.78f,.36f),C(1,.64f,.26f),.1f,.95f);var amber=C(1,.66f,.26f);
            void Monolith(Transform g,Vector3 at,float h,float w)
            {
                Put(PrimitiveType.Cube,"Monolith base",g,at+Vector3.up*.12f,new Vector3(w*1.4f,.24f,w*1.2f),dark);
                Put(PrimitiveType.Cube,"Monolith",g,at+Vector3.up*(.24f+h*.5f),new Vector3(w,h,w*.7f),stone);
                Put(PrimitiveType.Cube,"Monolith crown",g,at+Vector3.up*(.24f+h+.1f),new Vector3(w*1.2f,.2f,w*.9f),dark);
                for(var k=0;k<2;k++)Put(PrimitiveType.Cube,"Amber glyph band",g,at+new Vector3(0,.24f+h*(.62f+k*.16f),-w*.36f),new Vector3(w*.80f,.06f,.02f),glyph);
                Put(PrimitiveType.Sphere,"Amber glyph",g,at+new Vector3(0,.24f+h*.40f,-w*.36f),new Vector3(w*.36f,w*.36f,.04f),glyph);
                f.Glow("Glyph glow",g.TransformPoint(at+new Vector3(0,.24f+h*.45f,-w*.5f)),h*.9f,amber,.4f,g);
            }
            var spots=IsletSpots;
            var a=f.Islet("Sanctum islet",f.Screen(spots[0].x,spots[0].s,IsletDepth),spots[0].r,rock,rockDark,cap);
            Monolith(a,new Vector3(-.2f,0,.1f),2.2f,.7f);Monolith(a,new Vector3(.62f,0,.3f),1.1f,.45f);
            var b=f.Islet("Sanctum islet",f.Screen(spots[1].x,spots[1].s,IsletDepth),spots[1].r,rock,rockDark,cap);
            Monolith(b,new Vector3(0,0,0),1.6f,.6f);
            var c=f.Islet("Sanctum islet",f.Screen(spots[2].x,spots[2].s,IsletDepth*.5f),spots[2].r,rock,rockDark,cap);
            Monolith(c,Vector3.zero,.9f,.38f);
            for(var i=0;i<6;i++)
            {
                var side=i%2==0?-1:1;var k=.6f+.12f*(i%3);
                var g=f.Group("Floating rune stone",f.Screen(side*(7.1f+.3f*(i%2)),-7.6f+i*2.2f,-6),55,side*12);
                Put(PrimitiveType.Cube,"Rune stone",g,Vector3.zero,new Vector3(.5f,.7f,.36f)*k,stone);
                Put(PrimitiveType.Cube,"Rune mark",g,new Vector3(0,0,-.19f*k),new Vector3(.22f,.30f,.02f)*k,glyph);
                f.Drift(g,i*1.3f,.6f);
            }
        }
    }
}
