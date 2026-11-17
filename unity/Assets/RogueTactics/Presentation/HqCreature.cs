using System.Collections.Generic;
using RogueTactics.Presentation.Gfx;
using UnityEngine;
namespace RogueTactics.Presentation
{
    // Locally authored ruin creatures. Visuals only; their identity and HP stay X/Y in Core.
    // One look means one behaviour (design/overall/05_AGENCY.md 3.5): moss and bloom spirits are kept for the
    // nearest / weakest hunters of later floors; the first floor's anchor hunters are wardens.
    public enum SpiritKind { Moss, Bloom, Warden }
    // The body mesh (name, shape, transform) is the anchor other code measures, so it is built exactly as before;
    // everything that makes the spirit feel alive is the toon material, the face turned to the camera and HqCreatureAnim.
    public static class HqCreature
    {
        // Which look each Core enemy wears. Today every enemy is the G1 anchor hunter, so all are wardens;
        // X wears the gold gem, Y the teal one. Replace with the roster once enemy kinds exist (design/CODE_BRIDGE.md).
        public static SpiritKind KindOf(string id)=>SpiritKind.Warden;
        public static bool AltOf(string id)=>id=="Y";
        public static readonly Color WardenGold=new Color(1f,.80f,.30f),WardenTeal=new Color(.36f,.92f,.86f);
        public static Color TintOf(string id)=>KindOf(id)==SpiritKind.Warden?(AltOf(id)?WardenTeal:WardenGold):AltOf(id)?HqVfx.Violet:HqVfx.Moss;
        public static string NameOf(string id)=>KindOf(id)==SpiritKind.Warden?(AltOf(id)?"碧の番兵":"金の番兵"):AltOf(id)?"花の精":"苔の精";

        public static Renderer Build(Transform parent,Material template,SpiritKind kind,bool alt,Camera view=null)
        {
            var violet=kind==SpiritKind.Bloom;var warden=kind==SpiritKind.Warden;
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
            var skin=warden?Mat(new Color(.60f,.68f,.90f),new Color(.36f,.40f,.66f),0,.45f):violet?Mat(new Color(.70f,.53f,.88f),new Color(.43f,.31f,.66f)):Mat(new Color(.60f,.84f,.34f),new Color(.30f,.58f,.30f));
            var leaf=violet?Mat(new Color(.42f,.66f,.40f),new Color(.24f,.44f,.30f),0,.2f):Mat(new Color(.36f,.68f,.26f),new Color(.20f,.46f,.22f),0,.2f);
            var petal=Mat(violet?new Color(1f,.80f,.90f):new Color(1f,.97f,.84f),violet?new Color(.86f,.56f,.74f):new Color(.86f,.80f,.60f),0,.15f);
            var cream=Mat(new Color(1,.98f,.92f),Color.white,1,0);var dark=Mat(new Color(.13f,.10f,.20f),Color.black,1,0);
            var blush=Mat(new Color(1f,.62f,.66f),Color.white,1,0);var pollen=Mat(new Color(1f,.82f,.32f),new Color(.86f,.56f,.20f),0,.3f);

            var mesh=new Mesh{name="Authored scalloped spirit body"};owner.assets.Add(mesh);var verts=new List<Vector3>();var tris=new List<int>();
            const int rings=13,sides=24;
            for(var j=0;j<=rings;j++)for(var i=0;i<=sides;i++)
            {
                var a=i*2*Mathf.PI/sides;var t=j*Mathf.PI/rings;var scallop=1+.055f*Mathf.Cos(a*5)*Mathf.Sin(t);var radius=Mathf.Sin(t)*scallop;
                verts.Add(new Vector3(Mathf.Cos(a)*radius*.68f,Mathf.Max(.11f,.46f+Mathf.Cos(t)*.40f),.55f+Mathf.Sin(a)*radius*.56f));
                if(j<rings && i<sides){var n=j*(sides+1)+i;tris.AddRange(new[]{n,n+sides+1,n+1,n+1,n+sides+1,n+sides+2});}
            }
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var body=new GameObject("Fixture slime",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(parent,false);body.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=body.GetComponent<Renderer>();renderer.sharedMaterial=skin;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;

            // The camera looks down on the board, so the face sits on the part of the skin that actually faces it:
            // a frame centred on the body whose -z points at the camera (turned, and tipped up most of the way).
            var centre=new Vector3(0,.46f,.55f);
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
                var reach=1/Mathf.Sqrt(ray.x*ray.x/(.68f*.68f)+ray.y*ray.y/(.40f*.40f)+ray.z*ray.z/(.56f*.56f));
                return rig.InverseTransformDirection(parent.TransformDirection(ray))*(reach+lift);
            }
            foreach(var side in new[]{-1f,1f})
            {
                var eye=Shape("Bright spirit eye",Face(side*.20f,.04f,.004f),new Vector3(.15f,.215f,.05f),dark);if(anim!=null)anim.Eyes.Add(eye);
                Shape("Eye catchlight",new Vector3(-.16f,.22f,-.35f),new Vector3(.36f,.30f,.5f),cream,eye);
                Shape("Eye catchlight",new Vector3(.20f,-.24f,-.35f),new Vector3(.18f,.14f,.5f),cream,eye);
                Shape("Rosy cheek",Face(side*.36f,-.10f,.0f),new Vector3(.15f,.075f,.03f),blush);
            }
            Shape("Smile",Face(0,-.11f,.008f),new Vector3(.075f,.045f,.03f),dark);

            var crown=new GameObject("Spirit crown").transform;crown.SetParent(upright,false);crown.localPosition=new Vector3(0,.37f,0);if(anim!=null)anim.Crown=crown;
            if(warden)
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
            if(anim!=null){anim.Pivot=parent;anim.View=view;anim.Phase=alt?1.7f:0;anim.Tint=warden?(alt?WardenTeal:WardenGold):violet?HqVfx.Violet:HqVfx.Moss;anim.Capture();}
            return renderer;
        }
        private sealed class AssetsLifetime:MonoBehaviour
        {public readonly List<Object> assets=new List<Object>();private void OnDestroy(){foreach(var asset in assets)if(asset!=null)Object.Destroy(asset);}}
    }
}
