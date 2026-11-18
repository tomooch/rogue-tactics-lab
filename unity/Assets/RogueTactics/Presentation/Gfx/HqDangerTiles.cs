using System.Collections.Generic;
using UnityEngine;
namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// The cells a telegraphed blow will hit (the ruin guardian's announced area, design/specs/02_ENEMIES.md 6).
    /// Display only: Presentation passes the cells Core announced (EncounterState.Telegraphed / Plan.Area) and never
    /// decides them. Each cell gets a warm amber plate with a hatched rim and a slow pulse, laid just above the floor so
    /// it never hides feet, foot HP or faces; Strike() flashes them once as the blow lands, then they go.
    /// </summary>
    public sealed class HqDangerTiles:MonoBehaviour
    {
        // Yellow-amber: apart from the enemy cells' red and the party's blue, and from the gold of a chosen floor.
        public static readonly Color Amber=new Color(1f,.84f,.22f);
        // Size of a plate relative to a cell: a margin keeps neighbouring plates apart so each cell reads on its own.
        public const float Fill=.86f,Lift=.09f; // the worn floor stones' tops sit at .05
        private readonly List<Renderer> plates=new List<Renderer>();
        private Material material;private Texture2D texture;private float time,strike=-1;
        public int Count=>plates.Count;
        public bool Showing=>plates.Count>0;

        /// <param name="centres">World centres of the announced cells, on the floor.</param>
        /// <param name="cell">World size of one cell.</param>
        public void Show(IEnumerable<Vector3> centres,float cell)
        {
            Clear();
            if(material==null)
            {
                var shader=Resources.Load<Shader>("Gfx/HqDangerCell");if(shader==null)return;
                texture=Plate();material=new Material(shader){mainTexture=texture,renderQueue=3000}; // with the known-cell markers: after the ruins floor (drawn transparent), sorted back to front
            }
            foreach(var centre in centres)
            {
                var plate=GameObject.CreatePrimitive(PrimitiveType.Quad);plate.name="Danger cell";Destroy(plate.GetComponent<Collider>());
                plate.transform.SetParent(transform,false);plate.transform.position=centre+Vector3.up*Lift;
                plate.transform.rotation=Quaternion.Euler(90,0,0);plate.transform.localScale=new Vector3(cell*Fill,cell*Fill,1);
                var r=plate.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
                plates.Add(r);
            }
            time=0;strike=-1;Tick(0);
        }
        /// <summary>The blow lands: one bright flash, then the plates fade out over .35 s and are removed.</summary>
        public void Strike(){if(plates.Count>0)strike=0;}
        public void Clear(){foreach(var p in plates)if(p!=null)Destroy(p.gameObject);plates.Clear();strike=-1;}

        private void Update()=>Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            if(material==null || plates.Count==0)return;
            time+=Mathf.Min(dt,.05f);
            float alpha,white;
            if(strike>=0)
            {
                strike+=Mathf.Min(dt,.05f);var k=Mathf.Clamp01(strike/.35f);
                alpha=(1-k)*.95f;white=.6f*(1-k);
                if(k>=1){Clear();return;}
            }
            else
            {
                // Ease in over .25 s, then breathe at a calm 1.4 Hz: noticeable, never a flicker.
                var rise=Mathf.Clamp01(time/.25f);alpha=rise*(.80f+.15f*Mathf.Sin(time*Mathf.PI*2*1.4f));white=0;
            }
            material.SetColor("_Color",new Color(Mathf.Lerp(Amber.r,1,white),Mathf.Lerp(Amber.g,1,white),Mathf.Lerp(Amber.b,1,white),alpha));
        }
        private void OnDestroy(){Clear();if(material!=null)Destroy(material);if(texture!=null)Destroy(texture);}

        // A soft-cornered plate: faint fill, a bright rim, and diagonal hatching inside the rim ("keep off").
        private static Texture2D Plate()
        {
            const int n=128;var t=new Texture2D(n,n,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,name="Danger plate"};
            var pixels=new Color32[n*n];
            for(var y=0;y<n;y++)for(var x=0;x<n;x++)
            {
                var u=(x+.5f)/n*2-1;var v=(y+.5f)/n*2-1;
                // Rounded square distance (corner radius .22).
                var qx=Mathf.Max(Mathf.Abs(u)-.78f,0);var qy=Mathf.Max(Mathf.Abs(v)-.78f,0);var d=Mathf.Sqrt(qx*qx+qy*qy)-.22f+Mathf.Min(Mathf.Max(Mathf.Abs(u)-.78f,Mathf.Abs(v)-.78f),0);
                var inside=Mathf.Clamp01(-d*n*.5f);
                var rim=Mathf.Clamp01(1-Mathf.Abs(d+.07f)/.07f);
                var hatch=Mathf.Clamp01((Mathf.Abs(Mathf.Repeat((u+v)*3.5f,2)-1)-.55f)*5)*Mathf.Clamp01((-d-.12f)*12)*.55f;
                var a=Mathf.Clamp01(inside*(.28f+hatch)+rim);
                pixels[y*n+x]=new Color32(255,255,255,(byte)(a*255));
            }
            t.SetPixels32(pixels);t.Apply();return t;
        }
    }
}
