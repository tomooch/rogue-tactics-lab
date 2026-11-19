using System.Collections.Generic;
using UnityEngine;
namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// The end-of-floor reward (design/specs/04_RELICS_AND_GEAR.md 4): two relics offered on the board, one per pedestal.
    /// Display only. Both pedestals are the same stone and the same height and both icons float the same way: no colour,
    /// size or motion says which is better (04 §4: "おすすめの印、色での優劣は出さない"). The only colour is each relic's own.
    /// The Executor builds it with the pair Core offers, then calls Choose(index) or Decline() with the player's answer and
    /// gives the relic to Core at the returned second.
    /// </summary>
    public sealed class HqRewardAltar:MonoBehaviour
    {
        public const float PedestalHeight=.85f,IconHeight=2.0f,IconSize=1.6f;
        public const float ChooseSeconds=.75f,FadeSeconds=.4f;
        public string[] Ids{get;private set;}
        public Transform[] Icons{get;private set;}
        public Transform[] Pedestals{get;private set;}
        public bool Resolved{get;private set;}
        private readonly List<Object> owned=new List<Object>();
        private SpriteRenderer[] sprites;private Transform[] glows;private Camera view;private HqVfx vfx;
        private float time,resolveAge=-1,nextGlint;private int chosen=-2;private Vector3 gatherTo;private Vector3[] iconRest;

        public static HqRewardAltar Build(Transform parent,Vector3 leftGround,Vector3 rightGround,string leftId,string rightId,Camera view,HqVfx vfx)
        {
            var altar=new GameObject("Reward altar").AddComponent<HqRewardAltar>();altar.transform.SetParent(parent,false);
            altar.view=view;altar.vfx=vfx;altar.Ids=new[]{leftId,rightId};altar.Assemble(new[]{leftGround,rightGround});return altar;
        }

        /// <summary>The player keeps relic <paramref name="index"/>: it rises, flies to the party and is gathered in light;
        /// the other fades. Returns the second (from now) at which it reaches the party.</summary>
        public float Choose(int index,Vector3 partyCentre)
        {
            if(Resolved)return 0;Resolved=true;chosen=Mathf.Clamp(index,0,1);gatherTo=partyCentre+Vector3.up*1.2f;resolveAge=0;return ChooseSeconds;
        }
        /// <summary>The player goes on without a relic: both fade where they stand. Returns the seconds it takes.</summary>
        public float Decline(){if(Resolved)return 0;Resolved=true;chosen=-1;resolveAge=0;return FadeSeconds;}

        private void Assemble(Vector3[] grounds)
        {
            var toon=Resources.Load<Shader>("Gfx/HqCreature");
            Material Toon(Color c,Color shade)
            {
                var m=toon!=null?new Material(toon):new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if(toon!=null){m.SetColor("_Color",c);m.SetColor("_Shade",shade);m.SetFloat("_Gloss",.2f);}else m.color=c;
                owned.Add(m);return m;
            }
            var stone=Toon(new Color(.78f,.76f,.72f),new Color(.46f,.46f,.54f));var trim=Toon(new Color(1f,.80f,.40f),new Color(.78f,.52f,.24f));
            var glowShader=Resources.Load<Shader>("Gfx/HqFloorGlow");
            Icons=new Transform[2];Pedestals=new Transform[2];glows=new Transform[2];sprites=new SpriteRenderer[2];iconRest=new Vector3[2];
            for(var i=0;i<2;i++)
            {
                var pedestal=new GameObject("Reward pedestal "+i).transform;pedestal.SetParent(transform,false);pedestal.position=grounds[i];Pedestals[i]=pedestal;
                Piece(PrimitiveType.Cylinder,"Pedestal foot",pedestal,new Vector3(0,.08f,0),new Vector3(1.10f,.08f,1.10f),stone);
                Piece(PrimitiveType.Cylinder,"Pedestal foot trim",pedestal,new Vector3(0,.17f,0),new Vector3(.96f,.025f,.96f),trim);
                Piece(PrimitiveType.Cylinder,"Pedestal column",pedestal,new Vector3(0,.48f,0),new Vector3(.58f,.32f,.58f),stone);
                Piece(PrimitiveType.Cylinder,"Pedestal top",pedestal,new Vector3(0,PedestalHeight-.06f,0),new Vector3(.98f,.06f,.98f),stone);
                Piece(PrimitiveType.Cylinder,"Pedestal trim",pedestal,new Vector3(0,PedestalHeight-.13f,0),new Vector3(.90f,.025f,.90f),trim);
                var icon=new GameObject("Reward relic "+Ids[i]).transform;icon.SetParent(pedestal,false);icon.localPosition=Vector3.up*IconHeight;icon.localScale=Vector3.one*IconSize;
                var sprite=icon.gameObject.AddComponent<SpriteRenderer>();sprite.sprite=HqRelicArt.Sprite(Ids[i]);
                // HqRelicArt sprites are 132 px at 100 px/unit; scale so the icon is IconSize world units wide.
                var width=sprite.sprite!=null?sprite.sprite.bounds.size.x:1;icon.localScale=Vector3.one*(IconSize/Mathf.Max(.01f,width));
                Icons[i]=icon;sprites[i]=sprite;iconRest[i]=icon.localPosition;
                // A soft light behind the icon in the relic's own colour, the same strength for both.
                if(glowShader!=null)
                {
                    var g=new Material(glowShader);g.SetColor("_Tint",HqRelicArt.Accent(Ids[i]));g.SetFloat("_Strength",.75f);owned.Add(g);
                    var glow=GameObject.CreatePrimitive(PrimitiveType.Quad);glow.name="Reward relic light";Destroy(glow.GetComponent<Collider>());glow.GetComponent<MeshFilter>().sharedMesh=GlowQuad();
                    glow.transform.SetParent(pedestal,false);glow.transform.localPosition=Vector3.up*IconHeight;glow.transform.localScale=Vector3.one*IconSize*1.9f;
                    var gr=glow.GetComponent<Renderer>();gr.sharedMaterial=g;gr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;glows[i]=glow.transform;
                }
            }
            nextGlint=.6f;Face();
        }
        private void Piece(PrimitiveType type,string name,Transform parent,Vector3 at,Vector3 scale,Material m)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;Destroy(o.GetComponent<Collider>());o.transform.SetParent(parent,false);o.transform.localPosition=at;o.transform.localScale=scale;
            var r=o.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // HqFloorGlow expects uv -1..1 over the quad (its light fades to zero at the edge).
        private Mesh glowQuad;
        private Mesh GlowQuad()
        {
            if(glowQuad!=null)return glowQuad;
            glowQuad=new Mesh{name="Reward glow quad",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},
                uv=new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1)},triangles=new[]{0,2,1,2,3,1}};
            glowQuad.RecalculateBounds();owned.Add(glowQuad);return glowQuad;
        }
        private void Face(){if(view==null)return;foreach(var icon in Icons)if(icon!=null)icon.rotation=view.transform.rotation;foreach(var g in glows)if(g!=null)g.rotation=view.transform.rotation;}

        private void Update()=>Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            dt=Mathf.Min(dt,.05f);time+=dt;Face();
            if(resolveAge<0)
            {
                // Both float on the same slow breath, half a beat apart only so they do not move as one block.
                for(var i=0;i<2;i++){Icons[i].localPosition=iconRest[i]+Vector3.up*(.07f*Mathf.Sin(time*2.2f+i*Mathf.PI));if(glows[i]!=null)glows[i].localPosition=Icons[i].localPosition;}
                nextGlint-=dt;if(nextGlint<=0 && vfx!=null){nextGlint=1.3f;for(var i=0;i<2;i++)vfx.Glint(Icons[i].position+view.transform.right*.3f+view.transform.up*.3f);}
                return;
            }
            resolveAge+=dt;
            foreach(var g in glows)if(g!=null)g.localScale=Vector3.one*IconSize*1.9f*Mathf.Max(0,1-resolveAge/FadeSeconds);
            for(var i=0;i<2;i++)
            {
                if(i==chosen)
                {
                    var u=Mathf.Clamp01(resolveAge/ChooseSeconds);var rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.35f));var fly=Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.3f)/.7f));
                    var lifted=Pedestals[i].position+iconRest[i]+Vector3.up*.5f*rise;
                    Icons[i].position=Vector3.Lerp(lifted,gatherTo,fly)+Vector3.up*Mathf.Sin(fly*Mathf.PI)*.8f;
                    Icons[i].localScale=Vector3.one*(IconSize/Mathf.Max(.01f,sprites[i].sprite.bounds.size.x))*Mathf.Lerp(1,.35f,fly);
                    if(u>=1 && sprites[i].enabled){sprites[i].enabled=false;if(vfx!=null){vfx.Glint(gatherTo);vfx.Heal(gatherTo+Vector3.down*1.2f);}}
                }
                else
                {
                    var k=Mathf.Clamp01(resolveAge/FadeSeconds);var c=sprites[i].color;c.a=1-k;sprites[i].color=c;
                }
            }
        }
        private void OnDestroy(){foreach(var o in owned)if(o!=null)Destroy(o);}
    }
}
