using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// The floor spring (泉, design/specs/03_DUNGEON_AND_ROOMS.md §5): a low ring of round stones holding bright water
    /// that shimmers, with a soft column of light and a faint pool of light on the floor. It fits inside one board cell
    /// (2.5 world units) and stands under half a metre, so a companion behind it keeps everything above the ankles.
    /// Display only: Core decides that the party is healed and by how much; <see cref="Drink"/> only shows it.
    /// </summary>
    public sealed class HqSpringProp:MonoBehaviour
    {
        /// <summary>Seconds from <see cref="Drink"/> to the first companion's heal light, and between companions.</summary>
        public const float FirstHeal=.30f,Stagger=.20f;
        /// <summary>The spring transition of design/specs/07_UI_AND_CAMERA.md §7.</summary>
        public const float Duration=1.2f;
        /// <summary>Widest extent of the stones from the centre, and the top of the rim, in world units.</summary>
        public const float Radius=1.10f,Height=.56f;
        public const float WaterLevel=.27f;

        public Camera View;
        /// <summary>The water stills with the battle when the player pauses.</summary>
        public Func<bool> IsPaused;
        /// <summary>True once the party has drunk; the light settles low so the spring reads as used.</summary>
        public bool Spent{get;private set;}

        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private readonly List<Material> lightMaterials=new List<Material>();
        private Transform column;private float clock,glow=1,surgeAge=-1;
        private static readonly int ClockId=Shader.PropertyToID("_Clock"),GlowId=Shader.PropertyToID("_Glow"),SurgeId=Shader.PropertyToID("_Surge");

        /// <summary>Builds a spring standing on <paramref name="ground"/> (the centre of a board cell, y = floor).</summary>
        public static HqSpringProp Build(Transform parent,Vector3 ground,Camera view,Func<bool> paused=null)
        {
            var root=new GameObject("Floor spring");if(parent!=null)root.transform.SetParent(parent,false);root.transform.position=ground;
            var spring=root.AddComponent<HqSpringProp>();spring.View=view;spring.IsPaused=paused;
            spring.Assemble();return spring;
        }

        /// <summary>
        /// Shows the party drinking: the water wells up and a column of light rises, then each companion gets the existing
        /// heal light (<see cref="HqVfx.Heal"/>) in turn. Returns the second, from now, at which each one's light lands;
        /// pop that companion's heal number at the same moment. The spring is spent afterwards.
        /// </summary>
        public float[] Drink(HqVfx vfx,IReadOnlyList<Vector3> partyGrounds)
        {
            var count=partyGrounds?.Count??0;var times=new float[count];
            surgeAge=0;Spent=true;
            for(var i=0;i<count;i++)
            {
                times[i]=FirstHeal+i*Stagger;var at=partyGrounds[i];
                if(vfx!=null)vfx.After(times[i],()=>vfx.Heal(at));
            }
            return times;
        }

        /// <summary>Restores a fresh or a used spring at once, e.g. when a saved floor is shown again.</summary>
        public void SetSpent(bool spent){Spent=spent;surgeAge=-1;glow=spent?SpentGlow:1;Push(0);}

        private const float SpentGlow=.22f;

        // ---- Build ------------------------------------------------------------------------------------

        private void Assemble()
        {
            var toon=Resources.Load<Shader>("Gfx/HqCreature");var light=Resources.Load<Shader>("Gfx/HqSpring");
            Material Toon(Color c,Color shade,float gloss=.25f)
            {
                Material m;
                if(toon!=null){m=new Material(toon);m.SetColor("_Color",c);m.SetColor("_Shade",shade);m.SetFloat("_Gloss",gloss);m.SetColor("_Rim",new Color(1,1,.9f,.35f));}
                else {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=c;}
                owned.Add(m);return m;
            }
            var stone=Toon(new Color(.74f,.78f,.90f),new Color(.46f,.50f,.68f));
            var stoneWarm=Toon(new Color(.90f,.86f,.80f),new Color(.62f,.58f,.62f));
            var moss=Toon(new Color(.56f,.82f,.36f),new Color(.30f,.56f,.32f),.1f);
            var pad=Toon(new Color(.44f,.78f,.40f),new Color(.24f,.52f,.32f),.3f);
            var bud=Toon(new Color(1f,.70f,.84f),new Color(.88f,.46f,.64f),.3f);

            // The basin: a round tub turned on a lathe, its lip curling in toward the water.
            var profile=new[]{new Vector2(.96f,0),new Vector2(1.02f,.09f),new Vector2(1.04f,.22f),new Vector2(1f,.32f),new Vector2(.92f,.37f),new Vector2(.84f,.36f),new Vector2(.80f,.30f),new Vector2(.79f,.20f)};
            Part("Spring basin",Lathe(profile,40),stone);

            // Round stones set along the lip, a little uneven, so it reads as built by hand rather than cast.
            for(var i=0;i<10;i++)
            {
                var a=(i*36+8)*Mathf.Deg2Rad;var r=.93f;var wobble=Mathf.Sin(i*2.3f)*.04f;
                var s=Sphere("Spring rim stone",new Vector3(Mathf.Cos(a)*r,.37f+wobble*.5f,Mathf.Sin(a)*r),new Vector3(.40f+wobble,.19f,.33f),i%3==1?stoneWarm:stone);
                s.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,0);
            }
            // Moss in two tufts at the back, a lily pad and a bud on the water: alive, but low.
            foreach(var (x,z,size) in new[]{(-.62f,.66f,.20f),(-.40f,.84f,.15f),(.72f,.58f,.14f)})
                Sphere("Spring moss",new Vector3(x,.47f,z),new Vector3(size*1.5f,size*.8f,size*1.3f),moss);
            Sphere("Spring lily pad",new Vector3(.34f,WaterLevel+.012f,-.18f),new Vector3(.44f,.025f,.36f),pad);
            Sphere("Spring lily bud",new Vector3(.38f,WaterLevel+.07f,-.16f),new Vector3(.13f,.12f,.13f),bud);

            if(light==null)return;
            Material Light(float mode,bool opaque)
            {
                var m=new Material(light){name=mode==0?"Spring water":"Spring light"};m.SetFloat("_Mode",mode);
                m.SetFloat("_Src",1);m.SetFloat("_Dst",opaque?0:10);m.SetFloat("_ZWrite",opaque?1:0);m.renderQueue=opaque?2000:3000;
                owned.Add(m);lightMaterials.Add(m);return m;
            }
            Part("Spring water",Disc(.82f,48),Light(0,true)).localPosition=new Vector3(0,WaterLevel,0);
            var pool=Part("Spring pool light",Quad(2.4f,2.4f,false),Light(2,false));pool.localPosition=new Vector3(0,.035f,0);pool.localRotation=Quaternion.Euler(90,0,0);
            column=Part("Spring light",Quad(1.7f,3.2f,true),Light(1,false));column.localPosition=new Vector3(0,WaterLevel,0);
            Face();Push(0);
        }

        private Transform Part(string name,Mesh mesh,Material material)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;var r=obj.GetComponent<MeshRenderer>();r.sharedMaterial=material;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;owned.Add(mesh);return obj.transform;
        }
        private Transform Sphere(string name,Vector3 at,Vector3 size,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(transform,false);obj.transform.localPosition=at;obj.transform.localScale=size;
            var r=obj.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return obj.transform;
        }

        private static Mesh Lathe(Vector2[] profile,int sides)
        {
            var mesh=new Mesh{name="Spring basin lathe"};var v=new List<Vector3>();var t=new List<int>();var rows=profile.Length;
            for(var i=0;i<=sides;i++)
            {
                var a=i*2*Mathf.PI/sides;var c=Mathf.Cos(a);var s=Mathf.Sin(a);
                foreach(var p in profile)v.Add(new Vector3(c*p.x,p.y,s*p.x));
            }
            for(var i=0;i<sides;i++)for(var j=0;j<rows-1;j++)
            {var n=i*rows+j;t.AddRange(new[]{n,n+1,n+rows,n+1,n+rows+1,n+rows});}
            mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Disc(float radius,int sides)
        {
            var mesh=new Mesh{name="Spring water disc"};var v=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{Vector2.zero};var t=new List<int>();
            for(var i=0;i<=sides;i++){var a=i*2*Mathf.PI/sides;var c=Mathf.Cos(a);var s=Mathf.Sin(a);v.Add(new Vector3(c*radius,0,s*radius));uv.Add(new Vector2(c,s));}
            for(var i=1;i<=sides;i++)t.AddRange(new[]{0,i+1,i});
            mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Quad(float width,float height,bool fromBottom)
        {
            var mesh=new Mesh{name="Spring light quad"};var y0=fromBottom?0:-height/2;var y1=y0+height;var x=width/2;
            mesh.SetVertices(new List<Vector3>{new Vector3(-x,y0,0),new Vector3(x,y0,0),new Vector3(x,y1,0),new Vector3(-x,y1,0)});
            mesh.SetUVs(0,new List<Vector2>{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)});
            mesh.SetTriangles(new[]{0,2,1,0,3,2},0);mesh.RecalculateBounds();return mesh;
        }

        // ---- Motion -----------------------------------------------------------------------------------

        private void Update()
        {
            var dt=IsPaused!=null&&IsPaused()?0:Mathf.Min(Time.unscaledDeltaTime,.05f);
            clock+=dt;var surge=0f;
            if(surgeAge>=0)
            {
                surgeAge+=dt;
                // Wells up fast, holds while the lights reach the party, and sinks back by the end of the transition.
                surge=Mathf.SmoothStep(0,1,surgeAge/.25f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,Duration,surgeAge)));
                if(surgeAge>=Duration)surgeAge=-1;
            }
            var target=Spent&&surgeAge<0?SpentGlow:1;glow=Mathf.MoveTowards(glow,target,dt*1.2f);
            Push(surge);
        }
        private void LateUpdate()=>Face();
        private void Face()
        {
            // The column always turns its face to the camera; it hangs upright from the water.
            if(column!=null && View!=null)column.rotation=Quaternion.LookRotation(View.transform.forward,View.transform.up);
        }
        private void Push(float surge)
        {
            foreach(var m in lightMaterials){m.SetFloat(ClockId,clock);m.SetFloat(GlowId,glow);m.SetFloat(SurgeId,surge);}
        }
        private void OnDestroy(){foreach(var o in owned)if(o!=null)Destroy(o);}
    }
}
