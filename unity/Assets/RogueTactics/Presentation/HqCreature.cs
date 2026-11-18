using System.Collections.Generic;
using RogueTactics.Presentation.Gfx;
using UnityEngine;
namespace RogueTactics.Presentation
{
    // Locally authored ruin creatures. Visuals only; their identity and HP stay X/Y in Core.
    // One look means one behaviour (design/overall/05_AGENCY.md 3.5): moss and bloom spirits are kept for the
    // nearest / weakest hunters of later floors; the first floor's anchor hunters are wardens.
    // Cap, Bat and Guardian are the later floors' kinds (design/specs/02_ENEMIES.md 4-6); their looks exist before
    // their Core behaviour so the Executor only has to map ids to kinds.
    public enum SpiritKind { Moss, Bloom, Warden, Cap, Bat, Guardian }
    // The body mesh (name, shape, transform) is the anchor other code measures, so it is built exactly as before;
    // everything that makes the spirit feel alive is the toon material, the face turned to the camera and HqCreatureAnim.
    public static class HqCreature
    {
        // Which look each Core enemy wears. Today every enemy is the G1 anchor hunter, so all are wardens;
        // X wears the gold gem, Y the teal one. Replace with the roster once enemy kinds exist (design/CODE_BRIDGE.md).
        public static SpiritKind KindOf(string id)=>SpiritKind.Warden;
        public static bool AltOf(string id)=>id=="Y";
        public static readonly Color WardenGold=new Color(1f,.80f,.30f),WardenTeal=new Color(.36f,.92f,.86f);
        public static readonly Color CapRed=new Color(.96f,.36f,.34f),BatViolet=new Color(.66f,.48f,.98f),GuardianAmber=new Color(1f,.70f,.36f);
        public static Color TintOf(string id)=>TintOf(KindOf(id),AltOf(id));
        public static Color TintOf(SpiritKind kind,bool alt)=>kind switch
        {
            SpiritKind.Warden=>alt?WardenTeal:WardenGold,SpiritKind.Bloom=>HqVfx.Violet,SpiritKind.Cap=>CapRed,
            SpiritKind.Bat=>BatViolet,SpiritKind.Guardian=>GuardianAmber,_=>HqVfx.Moss
        };
        /// <summary>One character for the turn-order strip.</summary>
        public static string GlyphOf(string id)=>GlyphOf(KindOf(id),AltOf(id));
        public static string GlyphOf(SpiritKind kind,bool alt)=>kind switch
        {SpiritKind.Warden=>alt?"碧":"金",SpiritKind.Bloom=>"花",SpiritKind.Cap=>"茸",SpiritKind.Bat=>"蝠",SpiritKind.Guardian=>"守",_=>"苔"};
        public static string NameOf(string id)=>NameOf(KindOf(id),AltOf(id));
        public static string NameOf(SpiritKind kind,bool alt)=>kind switch
        {SpiritKind.Warden=>alt?"碧の番兵":"金の番兵",SpiritKind.Bloom=>"花の精",SpiritKind.Cap=>"茸の番人",SpiritKind.Bat=>"影こうもり",SpiritKind.Guardian=>"遺跡の守護者",_=>"苔の精"};
        // Body ellipsoid per kind: half widths across (x) and deep (z), half height (y), centre height, and the scallop.
        // Box: 1 = ellipsoid; below 1 squares the body off (a superellipsoid), for stone.
        private static (float X,float Y,float Z,float Centre,float Scallop,float Box) BodyOf(SpiritKind kind)=>kind switch
        {
            SpiritKind.Cap=>(.62f,.34f,.52f,.40f,.02f,1f),        // stout stem under a wide cap
            SpiritKind.Bat=>(.44f,.30f,.38f,.46f,0f,1f),          // small: the wings make its size
            SpiritKind.Guardian=>(.74f,.86f,.60f,.96f,0f,.45f),   // a carved block of the ruins, two cells tall
            _=>(.68f,.40f,.56f,.46f,.055f,1f)
        };
        private static float Squared(float v,float box)=>Mathf.Sign(v)*Mathf.Pow(Mathf.Abs(v),box);

        public static Renderer Build(Transform parent,Material template,SpiritKind kind,bool alt,Camera view=null)
        {
            var violet=kind==SpiritKind.Bloom;var warden=kind==SpiritKind.Warden;var body=BodyOf(kind);
            var owner=parent.gameObject.AddComponent<AssetsLifetime>();
            var toon=Resources.Load<Shader>("Gfx/HqCreature");
            var anim=toon!=null?parent.gameObject.AddComponent<HqCreatureAnim>():null;
            Material Mat(Color c,Color shade,float flat=0,float gloss=.55f)
            {
                Material m;
                if(toon!=null){m=new Material(toon);m.SetColor("_Color",c);m.SetColor("_Shade",shade);m.SetFloat("_Flat",flat);m.SetFloat("_Gloss",gloss);anim.Materials.Add(m);}
                else {m=new Material(template);m.color=c;m.SetFloat("_Smoothness",.24f);}
                owner.assets.Add(m);return m;
            }
            var skin=kind==SpiritKind.Cap?Mat(new Color(.99f,.95f,.88f),new Color(.80f,.70f,.66f),0,.35f)
                :kind==SpiritKind.Bat?Mat(new Color(.50f,.42f,.74f),new Color(.30f,.24f,.50f),0,.5f)
                :kind==SpiritKind.Guardian?Mat(new Color(.58f,.64f,.72f),new Color(.34f,.38f,.52f),0,.10f)
                :warden?Mat(new Color(.60f,.68f,.90f),new Color(.36f,.40f,.66f),0,.45f):violet?Mat(new Color(.70f,.53f,.88f),new Color(.43f,.31f,.66f)):Mat(new Color(.60f,.84f,.34f),new Color(.30f,.58f,.30f));
            var leaf=violet?Mat(new Color(.42f,.66f,.40f),new Color(.24f,.44f,.30f),0,.2f):Mat(new Color(.36f,.68f,.26f),new Color(.20f,.46f,.22f),0,.2f);
            var petal=Mat(violet?new Color(1f,.80f,.90f):new Color(1f,.97f,.84f),violet?new Color(.86f,.56f,.74f):new Color(.86f,.80f,.60f),0,.15f);
            var cream=Mat(new Color(1,.98f,.92f),Color.white,1,0);var dark=Mat(new Color(.13f,.10f,.20f),Color.black,1,0);
            var blush=Mat(new Color(1f,.62f,.66f),Color.white,1,0);var pollen=Mat(new Color(1f,.82f,.32f),new Color(.86f,.56f,.20f),0,.3f);

            var mesh=new Mesh{name="Authored scalloped spirit body"};owner.assets.Add(mesh);var verts=new List<Vector3>();var tris=new List<int>();
            const int rings=13,sides=24;
            for(var j=0;j<=rings;j++)for(var i=0;i<=sides;i++)
            {
                var a=i*2*Mathf.PI/sides;var t=j*Mathf.PI/rings;var scallop=1+body.Scallop*Mathf.Cos(a*5)*Mathf.Sin(t);
                if(body.Box>=1){var radius=Mathf.Sin(t)*scallop;verts.Add(new Vector3(Mathf.Cos(a)*radius*body.X,Mathf.Max(.11f,body.Centre+Mathf.Cos(t)*body.Y),.55f+Mathf.Sin(a)*radius*body.Z));}
                else {var ring=Squared(Mathf.Sin(t),body.Box);verts.Add(new Vector3(Squared(Mathf.Cos(a),body.Box)*ring*body.X,Mathf.Max(.11f,body.Centre+Squared(Mathf.Cos(t),body.Box)*body.Y),.55f+Squared(Mathf.Sin(a),body.Box)*ring*body.Z));}
                if(j<rings && i<sides){var n=j*(sides+1)+i;tris.AddRange(new[]{n,n+sides+1,n+1,n+1,n+sides+1,n+sides+2});}
            }
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var bodyObject=new GameObject("Fixture slime",typeof(MeshFilter),typeof(MeshRenderer));bodyObject.transform.SetParent(parent,false);bodyObject.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=bodyObject.GetComponent<Renderer>();renderer.sharedMaterial=skin;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;

            // The camera looks down on the board, so the face sits on the part of the skin that actually faces it:
            // a frame centred on the body whose -z points at the camera (turned, and tipped up most of the way).
            var centre=new Vector3(0,body.Centre,.55f);
            var flat=view!=null?-view.transform.forward:Vector3.back;var rise=Mathf.Clamp(flat.y,0,.9f)*.8f;flat.y=0;
            flat=flat.sqrMagnitude<1e-4f?Vector3.back:flat.normalized;var towards=(flat+Vector3.up*rise).normalized;
            var rig=new GameObject("Spirit face").transform;rig.SetParent(parent,false);rig.localPosition=centre;
            rig.rotation=Quaternion.LookRotation(-towards,Vector3.up);
            var upright=new GameObject("Spirit top").transform;upright.SetParent(parent,false);upright.localPosition=centre;
            upright.rotation=Quaternion.LookRotation(-flat,Vector3.up);
            Transform Shape(string name,Vector3 at,Vector3 size,Material material,Transform under=null)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(under!=null?under:rig,false);obj.transform.localPosition=at;obj.transform.localScale=size;
                var r=obj.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Object.Destroy(obj.GetComponent<Collider>());return obj.transform;
            }
            // A point of the face (x across, y up, as seen by the camera), cast onto the ellipsoid skin.
            Vector3 Face(float x,float y,float lift=0)
            {
                var ray=parent.InverseTransformDirection(rig.rotation*new Vector3(x*1.55f,y*1.55f,-1).normalized);
                var k=2/body.Box;var reach=body.Box>=1?1/Mathf.Sqrt(ray.x*ray.x/(body.X*body.X)+ray.y*ray.y/(body.Y*body.Y)+ray.z*ray.z/(body.Z*body.Z))
                    :Mathf.Pow(Mathf.Pow(Mathf.Abs(ray.x)/body.X,k)+Mathf.Pow(Mathf.Abs(ray.y)/body.Y,k)+Mathf.Pow(Mathf.Abs(ray.z)/body.Z,k),-1/k);
                return rig.InverseTransformDirection(parent.TransformDirection(ray))*(reach+lift);
            }
            foreach(var side in new[]{-1f,1f})
            {
                var big=kind==SpiritKind.Guardian?1.3f:1;var eye=Shape("Bright spirit eye",Face(side*.20f*big,.04f,.004f),new Vector3(.15f,.215f,.05f)*big,dark);if(anim!=null)anim.Eyes.Add(eye);
                Shape("Eye catchlight",new Vector3(-.16f,.22f,-.35f),new Vector3(.36f,.30f,.5f),cream,eye);
                Shape("Eye catchlight",new Vector3(.20f,-.24f,-.35f),new Vector3(.18f,.14f,.5f),cream,eye);
                Shape("Rosy cheek",Face(side*.36f,-.10f,.0f),new Vector3(.15f,.075f,.03f),blush);
            }
            Shape("Smile",Face(0,-.11f,.008f),new Vector3(.075f,.045f,.03f),dark);

            var crown=new GameObject("Spirit crown").transform;crown.SetParent(upright,false);crown.localPosition=new Vector3(0,body.Y-.03f,0);if(anim!=null)anim.Crown=crown;
            if(kind==SpiritKind.Cap)
            {
                // Cap keeper: a wide red cap tipped toward the party like a raised shield, cream spots, a frill beneath.
                var red=Mat(CapRed,new Color(.70f,.18f,.24f),0,.5f);
                var lid=new GameObject("Cap").transform;lid.SetParent(crown,false);lid.localPosition=new Vector3(0,.10f,-.04f);lid.localRotation=Quaternion.Euler(-14,0,0);
                Shape("Cap dome",Vector3.zero,new Vector3(1.20f,.46f,1.02f),red,lid);
                Shape("Cap frill",new Vector3(0,-.12f,0),new Vector3(1.04f,.10f,.88f),petal,lid);
                foreach(var (x,z,size) in new[]{(-.30f,-.26f,.20f),(.24f,-.30f,.16f),(.02f,-.40f,.12f),(.40f,-.02f,.13f),(-.44f,.06f,.12f),(.0f,.10f,.17f)})
                    Shape("Cap spot",new Vector3(x,.16f,z),new Vector3(size,.07f,size),cream,lid);
                if(anim!=null)anim.Leaves.Add(lid);
            }
            else if(kind==SpiritKind.Bat)
            {
                // Shadow bat: round ears and two scalloped wings that beat; it floats (its shadow stays on the floor).
                var wing=Mat(new Color(.40f,.30f,.64f),new Color(.24f,.18f,.42f),0,.4f);var inner=Mat(new Color(.78f,.64f,.98f),new Color(.56f,.44f,.80f),0,.2f);
                foreach(var side in new[]{-1f,1f})
                {
                    Shape("Bat ear",new Vector3(side*.20f,.06f,0),new Vector3(.20f,.34f,.10f),skin,crown).localRotation=Quaternion.Euler(0,0,side*-20);
                    Shape("Bat ear inner",new Vector3(side*.20f,.07f,-.05f),new Vector3(.10f,.20f,.04f),inner,crown).localRotation=Quaternion.Euler(0,0,side*-20);
                    var w=new GameObject("Bat wing").transform;w.SetParent(upright,false);w.localPosition=new Vector3(side*.34f,.10f,.05f);
                    Shape("Wing membrane",new Vector3(side*.26f,.10f,.01f),new Vector3(.50f,.34f,.04f),inner,w).localRotation=Quaternion.Euler(0,0,side*-18);
                    // Three finger bones fanned up and out, so the wing reads as a bat's even at phone size.
                    foreach(var (angle,length) in new[]{(35f,.46f),(70f,.42f),(105f,.32f)})
                    {
                        var dir=new Vector3(side*Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad),0);
                        Shape("Wing finger",dir*length*.5f,new Vector3(.10f,length,.05f),wing,w).localRotation=Quaternion.Euler(0,0,-side*angle);
                    }
                    if(anim!=null)anim.Leaves.Add(w);
                }
                Shape("Little fang",Face(.05f,-.17f,.006f),new Vector3(.035f,.05f,.02f),cream);
            }
            else if(kind==SpiritKind.Guardian)
            {
                // Ruin guardian: a carved block with a stone brow, a lintel on top grown over with ivy and an amber rune
                // that will glow when it announces a blow. Big friendly eyes keep it from reading as a monster.
                var stone=Mat(new Color(.50f,.56f,.64f),new Color(.30f,.33f,.46f),0,.1f);var carve=Mat(new Color(.36f,.40f,.52f),new Color(.24f,.26f,.38f),1,0);var ivy=Mat(new Color(.40f,.70f,.32f),new Color(.20f,.44f,.24f),0,.25f);
                var rune=Mat(GuardianAmber,new Color(1f,.58f,.22f),1,.9f);
                Shape("Stone brow",Face(0,.20f,.02f),new Vector3(.62f,.10f,.10f),stone);
                Shape("Guardian rune",Face(0,.33f,.03f),new Vector3(.17f,.17f,.04f),rune);
                foreach(var y in new[]{-.24f,-.36f})Shape("Carved band",Face(0,y,.012f),new Vector3(.80f,.035f,.04f),carve);
                foreach(var (x,y) in new[]{(-.30f,-.30f),(.34f,-.42f),(.12f,-.30f)})Shape("Moss patch",Face(x,y,.016f),new Vector3(.16f,.07f,.04f),ivy);
                Shape("Stone lintel",new Vector3(0,.04f,0),new Vector3(1.10f,.18f,.80f),stone,crown);
                Shape("Stone lintel cap",new Vector3(0,.14f,0),new Vector3(.86f,.10f,.62f),stone,crown);
                foreach(var (x,y,r) in new[]{(-.44f,.10f,30f),(-.30f,.18f,-10f),(.38f,.16f,-24f),(.48f,.04f,40f),(-.10f,.22f,8f)})
                {var leafy=Shape("Ivy leaf",new Vector3(x,y,-.30f),new Vector3(.20f,.05f,.14f),ivy,crown);leafy.localRotation=Quaternion.Euler(-30,0,r);if(anim!=null)anim.Leaves.Add(leafy);}
                foreach(var side in new[]{-1f,1f})Shape("Stone shoulder",new Vector3(side*.70f,-.36f,.02f),new Vector3(.36f,.30f,.38f),stone,upright);
            }
            else if(warden)
            {
                // Warden: a carved stone cap crowned by a star gem of the ruins' banners. The gem watches the party.
                var stone=Mat(new Color(.97f,.93f,.84f),new Color(.72f,.66f,.58f),0,.2f);var gemColor=alt?WardenTeal:WardenGold;
                var gem=Mat(gemColor,Color.Lerp(gemColor,new Color(.45f,.30f,.10f),.45f),.35f,.9f);
                Shape("Stone cap",new Vector3(0,.0f,0),new Vector3(.40f,.10f,.34f),stone,crown);
                Shape("Stone cap rim",new Vector3(0,.05f,0),new Vector3(.24f,.10f,.20f),stone,crown);
                var star=new GameObject("Warden star").transform;star.SetParent(crown,false);star.localPosition=new Vector3(0,.25f,0);
                // Face the star to the camera like the face does, so it reads as a star and not as two sticks.
                star.rotation=Quaternion.LookRotation(-towards,Vector3.up);
                // Long rays up/down and across, short ones on the diagonals: an eight-point sparkle that stays a star when small.
                Shape("Star ray",Vector3.zero,new Vector3(.12f,.42f,.06f),gem,star);
                Shape("Star ray",Vector3.zero,new Vector3(.34f,.10f,.06f),gem,star);
                foreach(var d in new[]{45f,-45f})Shape("Star ray",Vector3.zero,new Vector3(.06f,.22f,.05f),gem,star).localRotation=Quaternion.Euler(0,0,d);
                Shape("Star heart",new Vector3(0,0,-.01f),new Vector3(.15f,.15f,.07f),gem,star);
                Shape("Star glint",new Vector3(-.03f,.04f,-.045f),new Vector3(.05f,.05f,.02f),cream,star);
                if(anim!=null)anim.Leaves.Add(star);
            }
            else if(!violet)
            {
                // Moss spirit: a sprout of two broad leaves.
                Shape("Sprout stem",new Vector3(0,.06f,0),new Vector3(.05f,.16f,.05f),leaf,crown);
                for(var i=0;i<2;i++)
                {
                    var l=Shape("Crown leaf",new Vector3((i*2-1)*.17f,.19f,0),new Vector3(.34f,.05f,.20f),leaf,crown);
                    l.localRotation=Quaternion.Euler(0,0,(i*2-1)*-28);if(anim!=null)anim.Leaves.Add(l);
                }
            }
            else
            {
                // Flower spirit: a five-petal bloom, tipped toward the camera, with two leaves beneath.
                var bloom=new GameObject("Bloom").transform;bloom.SetParent(crown,false);bloom.localPosition=new Vector3(.12f,.10f,-.05f);bloom.localRotation=Quaternion.Euler(-38,0,-12);
                for(var i=0;i<5;i++)
                {
                    var a=i*Mathf.PI*2/5;var p=Shape("Little crown petal",new Vector3(Mathf.Cos(a)*.13f,0,Mathf.Sin(a)*.13f),new Vector3(.20f,.045f,.15f),petal,bloom);
                    p.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                }
                Shape("Pollen",new Vector3(0,.025f,0),new Vector3(.11f,.07f,.11f),pollen,bloom);
                if(anim!=null)anim.Leaves.Add(bloom);
                for(var i=0;i<2;i++)
                {
                    var l=Shape("Crown leaf",new Vector3((i*2-1)*.20f-.04f,.02f,.03f),new Vector3(.26f,.045f,.15f),leaf,crown);
                    l.localRotation=Quaternion.Euler(0,0,(i*2-1)*-16);if(anim!=null)anim.Leaves.Add(l);
                }
            }
            foreach(Transform child in parent)if(child.name!="Soft foot contact" && child.name!="Contact shadow" && !child.name.StartsWith("Known ")){child.localPosition*=1.3f;child.localScale*=1.3f;}
            if(anim!=null){anim.Pivot=parent;anim.View=view;anim.Phase=alt?1.7f:0;anim.Tint=TintOf(kind,alt);anim.Hover=kind==SpiritKind.Bat?.55f:0;anim.Jelly=kind==SpiritKind.Guardian?.25f:kind==SpiritKind.Cap?.6f:1;anim.Capture();}
            return renderer;
        }
        private sealed class AssetsLifetime:MonoBehaviour
        {public readonly List<Object> assets=new List<Object>();private void OnDestroy(){foreach(var asset in assets)if(asset!=null)Object.Destroy(asset);}}
    }
}
