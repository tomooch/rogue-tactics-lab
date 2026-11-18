using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// Display-only icons for the six S2 relics (design/specs/04_RELICS_AND_GEAR.md). Each icon is painted at run time
    /// from 2D distance fields into a Texture2D: no image assets, no text. The relics are told apart by silhouette first
    /// (spiky sun, lantern, shield, ring, drop, bell) and by colour second; every shape carries a dark outline and a cream
    /// sticker edge so it reads on both the pale board and a dark card at 44 pt. Nothing here knows the rules.
    /// </summary>
    public static class HqRelicArt
    {
        /// <summary>Relic ids in the order of the spec table (04 §3).</summary>
        public static readonly string[] Ids={"thorn_charm","watch_lantern","petal_crest","ring_of_accord","dawn_dew","pilgrim_bell"};
        /// <summary>Pixel size of a cached icon: 44 pt at 3x. Draw it in a 44×44 pt box (RawImage or Image).</summary>
        public const int Size=132;

        private static readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>();
        private static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();

        /// <summary>The cached icon for a relic id (e.g. "thorn_charm"). Unknown ids throw: a missing icon must not pass silently.</summary>
        public static Texture2D Icon(string relicId)
        {
            if(icons.TryGetValue(relicId??"",out var cached) && cached!=null)return cached;
            var texture=Render(relicId,Size);
            // Held only by this cache, so keep Resources.UnloadUnusedAssets from taking it out from under the UI.
            texture.hideFlags=HideFlags.DontUnloadUnusedAsset;icons[relicId]=texture;return texture;
        }

        /// <summary>The cached icon as a UI sprite (pivot centre, 132 px per 44 pt when the canvas uses 3 px per unit).</summary>
        public static Sprite Sprite(string relicId)
        {
            if(sprites.TryGetValue(relicId??"",out var cached) && cached!=null && cached.texture!=null)return cached;
            var texture=Icon(relicId);var sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width/44f);
            sprite.name="Relic icon "+relicId;sprite.hideFlags=HideFlags.DontUnloadUnusedAsset;sprites[relicId]=sprite;return sprite;
        }

        /// <summary>The relic's main colour, for a card edge, a pick-up glint or the moment it takes effect.</summary>
        public static Color Accent(string relicId)
        {
            switch(relicId)
            {
                case "thorn_charm":return new Color(.93f,.36f,.52f);
                case "watch_lantern":return new Color(1f,.56f,.22f);
                case "petal_crest":return new Color(.45f,.62f,1f);
                case "ring_of_accord":return new Color(1f,.86f,.30f);
                case "dawn_dew":return new Color(.36f,.90f,.86f);
                case "pilgrim_bell":return new Color(.72f,.60f,1f);
                default:throw new ArgumentException("Unknown relic id: "+relicId,nameof(relicId));
            }
        }

        /// <summary>Paints a fresh, uncached icon at any size (the caller owns and destroys it).</summary>
        public static Texture2D Render(string relicId,int size)
        {
            var layers=Layers(relicId);
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Relic icon "+relicId,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=1};
            texture.SetPixels(Rasterize(layers,size));texture.Apply(true,false);
            return texture;
        }

        // ---- Painting ---------------------------------------------------------------------------------

        private delegate float Sdf(Vector2 p);
        private sealed class Layer
        {
            public Sdf Shape;public Color Fill,Shade,Line;
            public float LineWidth=.05f,ShadeShift=.11f,Glow;public bool Outline=true,Silhouette=true;
        }

        // Light comes from the upper left, like the board's key light: the lower right of every part falls into shade.
        private static readonly Vector2 Light=new Vector2(-.6f,.8f);
        private static readonly Color Sticker=new Color(1f,.98f,.93f),Shadow=new Color(.05f,.07f,.22f);
        private const float StickerWidth=.045f;

        private static Color[] Rasterize(List<Layer> layers,int size)
        {
            var pixels=new Color[size*size];var px=2f/size;
            for(var y=0;y<size;y++)for(var x=0;x<size;x++)
            {
                var p=new Vector2((x+.5f)*px-1,(y+.5f)*px-1);
                var rgb=Vector3.zero;var a=0f;var union=float.MaxValue;var lifted=float.MaxValue;
                foreach(var l in layers)
                {
                    var d=l.Shape(p);
                    if(l.Glow>0)
                    {
                        // A soft light: full inside, falling off outside. Never part of the silhouette.
                        var g=l.Fill.a*Mathf.Exp(-Mathf.Max(0,d)/l.Glow);Over(ref rgb,ref a,l.Fill,g);continue;
                    }
                    if(l.Outline)Over(ref rgb,ref a,l.Line,Cover(d-l.LineWidth,px));
                    var c=Cover(d,px);
                    if(c>0)
                    {
                        var lit=l.ShadeShift>0?Cover(l.Shape(p-Light*l.ShadeShift),px):1;
                        var colour=Color.Lerp(l.Shade,l.Fill,lit);Over(ref rgb,ref a,colour,c*l.Fill.a);
                    }
                    if(l.Silhouette)
                    {
                        var edge=l.Outline?l.LineWidth:0;union=Mathf.Min(union,d-edge);
                        lifted=Mathf.Min(lifted,l.Shape(p+new Vector2(0,.075f))-edge);
                    }
                }
                // Behind everything: a cream sticker edge, then a soft shadow dropped below it.
                var under=Vector3.zero;var underA=0f;
                Over(ref under,ref underA,Shadow,.30f*Mathf.Clamp01(.5f-(lifted-StickerWidth)/(px*5)));
                Over(ref under,ref underA,Sticker,Cover(union-StickerWidth,px));
                rgb+=under*(1-a);a+=underA*(1-a);
                pixels[y*size+x]=a>1e-4f?new Color(rgb.x/a,rgb.y/a,rgb.z/a,a):new Color(0,0,0,0);
            }
            return pixels;
        }

        private static float Cover(float d,float px)=>Mathf.Clamp01(.5f-d/px);
        private static void Over(ref Vector3 rgb,ref float a,Color c,float alpha)
        {if(alpha<=0)return;rgb=new Vector3(c.r,c.g,c.b)*alpha+rgb*(1-alpha);a=alpha+a*(1-alpha);}

        // ---- Distance fields (p in -1..1, y up) ------------------------------------------------------

        private static Sdf Circle(float x,float y,float r)=>p=>(p-new Vector2(x,y)).magnitude-r;
        private static Sdf Ellipse(float x,float y,float rx,float ry,float degrees=0)
        {
            var c=Mathf.Cos(-degrees*Mathf.Deg2Rad);var s=Mathf.Sin(-degrees*Mathf.Deg2Rad);
            return p=>{var q=p-new Vector2(x,y);q=new Vector2(q.x*c-q.y*s,q.x*s+q.y*c);var k=new Vector2(q.x/rx,q.y/ry).magnitude;return (k-1)*Mathf.Min(rx,ry);};
        }
        private static Sdf Box(float x,float y,float hx,float hy,float r)
            =>p=>{var q=new Vector2(Mathf.Abs(p.x-x)-hx+r,Mathf.Abs(p.y-y)-hy+r);return Vector2.Max(q,Vector2.zero).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-r;};
        private static Sdf Annulus(float x,float y,float r,float w,float squash=1)=>p=>Mathf.Abs(new Vector2(p.x-x,(p.y-y)*squash).magnitude-r)-w;
        private static Sdf Capsule(Vector2 a,Vector2 b,float r)
            =>p=>{var pa=p-a;var ba=b-a;var h=Mathf.Clamp01(Vector2.Dot(pa,ba)/Vector2.Dot(ba,ba));return (pa-ba*h).magnitude-r;};
        private static Sdf Polygon(float round,params Vector2[] v)
            =>p=>
            {
                var d=Vector2.Dot(p-v[0],p-v[0]);var s=1f;
                for(int i=0,j=v.Length-1;i<v.Length;j=i,i++)
                {
                    var e=v[j]-v[i];var w=p-v[i];var b=w-e*Mathf.Clamp01(Vector2.Dot(w,e)/Vector2.Dot(e,e));d=Mathf.Min(d,Vector2.Dot(b,b));
                    var c1=p.y>=v[i].y;var c2=p.y<v[j].y;var c3=e.x*w.y>e.y*w.x;
                    if((c1&&c2&&c3)||(!c1&&!c2&&!c3))s=-s;
                }
                return s*Mathf.Sqrt(d)-round;
            };
        private static Sdf Union(params Sdf[] f)=>p=>{var d=float.MaxValue;foreach(var g in f)d=Mathf.Min(d,g(p));return d;};
        private static Sdf Smooth(float k,Sdf a,Sdf b)=>p=>{var da=a(p);var db=b(p);var h=Mathf.Clamp01(.5f+.5f*(db-da)/k);return Mathf.Lerp(db,da,h)-k*h*(1-h);};
        private static Sdf Intersect(params Sdf[] f)=>p=>{var d=float.MinValue;foreach(var g in f)d=Mathf.Max(d,g(p));return d;};
        private static Sdf Inset(Sdf f,float by)=>p=>f(p)+by;
        /// <summary>A four-point twinkle.</summary>
        private static Sdf Twinkle(float x,float y,float r)
            =>Union(Polygon(0,new Vector2(x,y+r),new Vector2(x+r*.24f,y),new Vector2(x,y-r),new Vector2(x-r*.24f,y)),
                    Polygon(0,new Vector2(x-r,y),new Vector2(x,y+r*.24f),new Vector2(x+r,y),new Vector2(x,y-r*.24f)));

        private static Color Ink(Color c)=>new Color(c.r*.30f+.07f,c.g*.26f+.05f,c.b*.30f+.13f);
        private static Layer Part(Sdf shape,Color fill,Color shade,float shadeShift=.11f)=>new Layer{Shape=shape,Fill=fill,Shade=shade,Line=Ink(shade),ShadeShift=shadeShift};
        private static Layer Spot(Sdf shape,float alpha=.9f)=>new Layer{Shape=shape,Fill=new Color(1,1,1,alpha),Shade=new Color(1,1,1,alpha),Outline=false,Silhouette=false,ShadeShift=0};
        private static Layer Halo(Sdf shape,Color c,float alpha,float falloff)=>new Layer{Shape=shape,Fill=new Color(c.r,c.g,c.b,alpha),Glow=falloff,Outline=false,Silhouette=false};
        private static Layer Flat(Sdf shape,Color c)=>new Layer{Shape=shape,Fill=c,Shade=c,Outline=false,Silhouette=false,ShadeShift=0};

        // ---- The six relics ---------------------------------------------------------------------------

        private static List<Layer> Layers(string id)
        {
            var l=new List<Layer>();
            switch(id)
            {
                case "thorn_charm":
                {
                    // 荊棘の護符 (counter 2→3): a berry-red charm in a crown of thorns. Silhouette: a spiky sun.
                    var thornFill=new Color(.42f,.70f,.34f);var thornShade=new Color(.24f,.48f,.28f);
                    var thorns=new List<Sdf>();
                    for(var i=0;i<8;i++)
                    {
                        var a=(90+i*45)*Mathf.Deg2Rad;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var side=new Vector2(-dir.y,dir.x);
                        // Each thorn hooks a little clockwise, so the ring reads as grown and not as a cog.
                        thorns.Add(Polygon(.025f,dir*.42f+side*.17f,dir*.84f-side*.07f,dir*.42f-side*.13f));
                    }
                    l.Add(Part(Union(Union(thorns.ToArray()),Annulus(0,0,.53f,.085f)),thornFill,thornShade,.08f));
                    l.Add(Part(Circle(0,0,.44f),new Color(.95f,.40f,.56f),new Color(.72f,.20f,.40f)));
                    l.Add(Part(Circle(0,.0f,.22f),new Color(1f,.70f,.80f),new Color(.93f,.46f,.62f),.07f));
                    l.Add(Spot(Ellipse(-.17f,.20f,.11f,.07f,35)));
                    l.Add(Spot(Circle(-.06f,.07f,.05f)));
                    break;
                }
                case "watch_lantern":
                {
                    // 見張りの灯 (protect from two cells): a warm lantern on a carrying loop. Silhouette: tall, loop on top.
                    var frame=new Color(.30f,.50f,.56f);var frameShade=new Color(.18f,.32f,.42f);
                    l.Add(Halo(Box(0,-.04f,.30f,.28f,.12f),new Color(1f,.70f,.30f),.55f,.16f));
                    l.Add(Part(Annulus(0,.70f,.15f,.05f),frame,frameShade,.06f));
                    l.Add(Part(Box(0,-.02f,.36f,.34f,.12f),new Color(1f,.80f,.40f),new Color(1f,.58f,.24f),.10f));
                    l.Add(Flat(Smooth(.12f,Circle(0,-.12f,.13f),Polygon(0,new Vector2(-.10f,-.10f),new Vector2(.10f,-.10f),new Vector2(0,.20f))),new Color(1f,.98f,.86f)));
                    l.Add(Part(Union(Box(-.36f,-.02f,.07f,.36f,.05f),Box(.36f,-.02f,.07f,.36f,.05f)),frame,frameShade,.05f));
                    l.Add(Part(Polygon(.06f,new Vector2(-.44f,.30f),new Vector2(.44f,.30f),new Vector2(.20f,.52f),new Vector2(-.20f,.52f)),frame,frameShade,.08f));
                    l.Add(Part(Box(0,-.44f,.44f,.09f,.08f),frame,frameShade,.07f));
                    l.Add(Spot(Box(-.20f,.06f,.035f,.16f,.035f),.75f));
                    l.Add(Spot(Ellipse(-.12f,.40f,.08f,.04f,10),.6f));
                    // The lantern is the narrowest shape; drawn a little larger it carries the same weight as the others.
                    foreach(var layer in l){var f=layer.Shape;layer.Shape=p=>f(p/1.12f)*1.12f;}
                    break;
                }
                case "petal_crest":
                {
                    // 花盾の紋 (guard 3→4): a heater shield bearing a five-petal flower. Silhouette: flat top, pointed foot.
                    var shield=Intersect(Box(0,-.08f,.66f,.80f,.12f),Circle(.62f,.30f,1.24f),Circle(-.62f,.30f,1.24f));
                    l.Add(Part(shield,new Color(.50f,.68f,1f),new Color(.30f,.44f,.88f),.12f));
                    l.Add(Part(Inset(shield,.14f),new Color(1f,.97f,.90f),new Color(.88f,.84f,.80f),.07f));
                    var flower=new List<Sdf>();
                    for(var i=0;i<5;i++){var a=(90+i*72)*Mathf.Deg2Rad;flower.Add(Circle(Mathf.Cos(a)*.17f,.10f+Mathf.Sin(a)*.17f,.14f));}
                    l.Add(Part(Union(flower.ToArray()),new Color(1f,.62f,.78f),new Color(.90f,.40f,.60f),.07f));
                    l.Add(Part(Circle(0,.10f,.085f),new Color(1f,.86f,.36f),new Color(.92f,.62f,.24f),.05f));
                    l.Add(Spot(Ellipse(-.40f,.44f,.10f,.05f,20),.85f));
                    break;
                }
                case "ring_of_accord":
                {
                    // 連携の輪 (flank beside anyone): a gold ring set with three stones in the party's colours. Silhouette: a hoop.
                    l.Add(Part(Annulus(0,-.02f,.50f,.16f,1.12f),new Color(1f,.82f,.34f),new Color(.86f,.56f,.20f),.10f));
                    var stones=new[]{(90f,HqVfx.Azure),(210f,new Color(1f,.56f,.72f)),(330f,new Color(.56f,.92f,.42f))};
                    foreach(var (deg,c) in stones)
                    {
                        var a=deg*Mathf.Deg2Rad;var x=Mathf.Cos(a)*.50f;var y=-.02f+Mathf.Sin(a)*.50f/1.12f;
                        l.Add(Part(Circle(x,y,.17f),c,Color.Lerp(c,new Color(.2f,.2f,.45f),.38f),.07f));
                        l.Add(Spot(Circle(x-.06f,y+.06f,.045f)));
                    }
                    l.Add(Spot(Ellipse(-.36f,.24f,.08f,.035f,50),.8f));
                    break;
                }
                case "dawn_dew":
                {
                    // 朝露の雫 (one large heal): a single round drop with a morning twinkle. Silhouette: a teardrop.
                    var drop=Smooth(.22f,Circle(0,-.22f,.52f),Polygon(.02f,new Vector2(-.24f,.10f),new Vector2(.24f,.10f),new Vector2(-.02f,.84f)));
                    l.Add(Halo(Circle(0,-.22f,.40f),new Color(.6f,1f,.96f),.35f,.12f));
                    l.Add(Part(drop,new Color(.50f,.90f,.96f),new Color(.24f,.62f,.84f),.14f));
                    l.Add(Flat(Ellipse(.06f,-.30f,.30f,.26f),new Color(.70f,.97f,1f,.55f)));
                    l.Add(Spot(Ellipse(-.24f,-.04f,.08f,.20f,-18)));
                    l.Add(Spot(Circle(-.14f,.20f,.045f)));
                    l.Add(Part(Twinkle(.56f,.48f,.26f),new Color(1f,.95f,.62f),new Color(1f,.82f,.40f),0));
                    break;
                }
                case "pilgrim_bell":
                {
                    // 巡礼の鈴 (three small heals): a lilac hand bell tied with a leaf-green bow. Silhouette: dome, flared lip, clapper.
                    var bell=Smooth(.10f,Circle(0,.12f,.36f),Polygon(.02f,new Vector2(-.36f,.12f),new Vector2(.36f,.12f),new Vector2(.58f,-.40f),new Vector2(-.58f,-.40f)));
                    var fill=new Color(.80f,.70f,1f);var shade=new Color(.56f,.44f,.86f);
                    l.Add(Part(Circle(0,-.64f,.13f),new Color(.98f,.84f,.42f),new Color(.82f,.58f,.24f),.06f));
                    l.Add(Part(bell,fill,shade,.13f));
                    l.Add(Part(Box(0,-.44f,.68f,.10f,.09f),fill,shade,.07f));
                    l.Add(Flat(Box(0,-.16f,.46f,.035f,.03f),shade));
                    var bowFill=new Color(.58f,.88f,.42f);var bowShade=new Color(.32f,.64f,.30f);
                    l.Add(Part(Union(Polygon(.04f,new Vector2(-.04f,.56f),new Vector2(-.40f,.76f),new Vector2(-.36f,.46f)),
                                     Polygon(.04f,new Vector2(.04f,.56f),new Vector2(.40f,.76f),new Vector2(.36f,.46f))),bowFill,bowShade,.06f));
                    l.Add(Part(Circle(0,.56f,.09f),bowFill,bowShade,.05f));
                    l.Add(Spot(Ellipse(-.18f,.24f,.06f,.15f,-20)));
                    break;
                }
                default:throw new ArgumentException("Unknown relic id: "+id,nameof(id));
            }
            return l;
        }
    }
}
