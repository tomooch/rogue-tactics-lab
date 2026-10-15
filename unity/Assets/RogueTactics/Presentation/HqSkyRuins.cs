using System.Collections.Generic;
using UnityEngine;

namespace RogueTactics.Presentation
{
    /// <summary>Procedural, presentation-only environment around a centered 5x5 board.</summary>
    public static class HqSkyRuins
    {
        private enum Layer { Limestone, PavingWarm, PavingCool, ShadowStone, Blue, Gold, Water, Foliage, Blossom }

        // Center is the board center. At cell=2.5, board centers span +/-5 world units.
        public static GameObject Build(Material template, Vector3 center, float cell)
        {
            if (template == null) throw new System.ArgumentNullException(nameof(template));
            if (cell <= 0) throw new System.ArgumentOutOfRangeException(nameof(cell));
            float u = cell / 2.5f;
            var root = new GameObject("HQ Sky Ruins Environment");
            root.transform.position = center;
            var lifetime = root.AddComponent<OwnedAssets>();
            var g = new Dictionary<Layer, MeshData>();
            foreach (Layer layer in System.Enum.GetValues(typeof(Layer))) g[layer] = new MeshData();

            // Broad floating island: a chamfered, hand-shaped rim at ±7.2 x / ±6.7 z,
            // with a tapered four-unit hanging cliff below the existing flat combat floor.
            AddIsland(g[Layer.ShadowStone], u);
            AddPaving(g[Layer.Limestone], u);
            for(var row=0;row<9;row++)for(var col=0;col<9;col++)
            {
                var x=(col-4)*1.40f+(row%2)*.55f;var z=(row-4)*1.38f;
                if(x>6.1f)continue;
                var layer=(col*3+row)%5==0?Layer.PavingCool:Layer.PavingWarm;
                BeveledSlab(g[layer],new Vector3(x,-.10f,z)*u,1.38f,1.35f,.22f,.045f,u,col+row*9);
            }

            // Chunky, broken perimeter columns provide vertical scale while keeping the board clear.
            Column(g[Layer.ShadowStone], new Vector3(-6.55f, -.1f, -3.9f), .92f, 2.7f, u, 0);
            Column(g[Layer.Limestone], new Vector3(6.45f, -.1f, -3.4f), .78f, 2.1f, u, 2);
            Column(g[Layer.Limestone], new Vector3(-6.55f, -.1f, 3.0f), .76f, 2.4f, u, 1);
            Column(g[Layer.ShadowStone], new Vector3(6.55f, -.1f, 3.4f), 1.02f, 2.9f, u, 3);

            Column(g[Layer.Limestone],new Vector3(-4.75f,-.1f,4.5f),.65f,2.8f,u,5);
            Column(g[Layer.Limestone],new Vector3(4.8f,-.1f,4.7f),.67f,3.2f,u,6);
            Banner(g,new Vector3(-4.6f,1.9f,4.3f),1.9f,u);
            Garden(g,new Vector3(-4.7f,2.55f,4.5f),1.1f,42,u,9);
            Garden(g,new Vector3(4.8f,3.0f,4.7f),1.1f,40,u,10);
            // Rear arch sits at +7 units and is assembled from individually beveled voussoirs.
            Arch(g, u, new Vector3(0, 0, 5.2f));

            // Small, broken skyline ruins remain close enough to silhouette against the sky.
            FloatingRuin(g, new Vector3(-5.5f, 3.7f, 4.5f), .9f, u);
            FloatingRuin(g, new Vector3(4.5f, 3.0f, 5.1f), .72f, u);
            FloatingRuin(g, new Vector3(-2.9f, 4.8f, 5.3f), .55f, u);

            Banner(g, new Vector3(-5.9f, .05f, 4.0f), 1.8f, u);
            Banner(g, new Vector3(5.95f, .05f, 4.5f), 1.5f, u);

            // Water leaves the island from just outside the playable square.
            Waterfall(g[Layer.Water], new Vector3(-6.5f, -.15f, 1.0f), .85f, 5.0f, u);
            Waterfall(g[Layer.Water], new Vector3(6.5f, -.15f, -1.2f), .85f, 5.0f, u);
            Waterfall(g[Layer.Water], new Vector3(2.8f, -.35f, -6.5f), .8f, 3.0f, u);

            // Dense clustered foliage, small pale flowers, and a few blue blooms on the ledges.
            Garden(g, new Vector3(-5.7f, .12f, 3.0f), 1.0f, 29, u, 0);
            Garden(g, new Vector3(5.7f, .12f, 3.1f), 1.0f, 27, u, 1);
            Garden(g, new Vector3(-4.9f, .12f, 5.5f), 1.0f, 24, u, 2);
            Garden(g, new Vector3(4.8f, .12f, 5.5f), 1.0f, 25, u, 3);
            Garden(g, new Vector3(-5.5f, 3.85f, 4.5f), .7f, 15, u, 1);

            Garden(g,new Vector3(-4.8f,.14f,.4f),1.15f,35,u,7);Garden(g,new Vector3(4.8f,.14f,.5f),1.15f,35,u,8);
            var grain=new Texture2D(96,96,TextureFormat.RGBA32,true){name="Authored limestone grain",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Repeat};
            var pixels=new Color[96*96];for(var y=0;y<96;y++)for(var x=0;x<96;x++){var noise=Mathf.PerlinNoise(x*.22f,y*.22f)*.14f+Mathf.PerlinNoise(x*.9f,y*.9f)*.06f;pixels[y*96+x]=new Color(.85f+noise,.85f+noise,.85f+noise,1);}grain.SetPixels(pixels);grain.Apply(true,true);lifetime.Own(grain);
            foreach (var part in g)
            {
                if (part.Value.Count == 0) continue;
                Mesh mesh = part.Value.CreateMesh("HQ Sky Ruins " + part.Key);
                var material = new Material(template) { name = "HQ Sky Ruins " + part.Key };
                material.color = Tint(part.Key);material.SetFloat("_Smoothness",.12f);
                if(part.Key==Layer.Limestone || part.Key==Layer.PavingWarm || part.Key==Layer.PavingCool || part.Key==Layer.ShadowStone)material.SetTexture("_BaseMap",grain);
                if ((part.Key == Layer.Blue || part.Key == Layer.Water || part.Key == Layer.Foliage || part.Key == Layer.Blossom) && material.HasProperty("_Cull")) material.SetFloat("_Cull", 0);
                lifetime.Own(mesh); lifetime.Own(material);
                var child = new GameObject(part.Key.ToString()); child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return root;
        }

        private static Color Tint(Layer layer)
        {
            switch (layer)
            {
                case Layer.Limestone: return new Color(.91f, .84f, .69f);
                case Layer.PavingWarm: return new Color(.82f,.78f,.64f);
                case Layer.PavingCool: return new Color(.74f,.78f,.67f);
                case Layer.ShadowStone: return new Color(.67f, .59f, .46f);
                case Layer.Blue: return new Color(.12f, .43f, .70f);
                case Layer.Gold: return new Color(.91f, .66f, .25f);
                case Layer.Water: return new Color(.3f, .76f, .88f);
                case Layer.Foliage: return new Color(.18f, .43f, .25f);
                default: return new Color(1f, .9f, .83f);
            }
        }

        private static void AddIsland(MeshData mesh, float u)
        {
            Vector2[] outline = {
                new Vector2(-7.0f,-3.6f), new Vector2(-6.4f,-5.4f), new Vector2(-4.2f,-6.4f),
                new Vector2(-1.0f,-6.75f), new Vector2(2.5f,-6.35f), new Vector2(5.7f,-5.5f),
                new Vector2(7.0f,-3.6f), new Vector2(6.8f,2.7f), new Vector2(5.5f,5.0f),
                new Vector2(2.4f,6.2f), new Vector2(-1.5f,6.55f), new Vector2(-5.0f,5.7f),
                new Vector2(-6.7f,4.1f)
            };
            for(var i=0;i<outline.Length;i++)outline[i].x*=.9f;
            Vector2[] lower = new Vector2[outline.Length];
            for (int i=0;i<outline.Length;i++) lower[i]=outline[i]*1.035f;
            ExtrudedBevel(mesh, outline, lower, -.18f*u, -3.65f*u, .32f*u, u);
            // Lower hanging strata taper and break into broad irregular stone plates.
            for(int i=0;i<13;i++)
            {
                float a=i*Mathf.PI*2f/13f;
                float x=Mathf.Cos(a)*6.35f*u, z=Mathf.Sin(a)*5.95f*u;
                float width=i%3==0?1.5f:.9f, height=i%4==0?.62f:.38f;
                BeveledSlab(mesh,new Vector3(x,-2.45f*u,z),width,height,.75f,.1f,u,i);
            }
        }

        private static void AddPaving(MeshData mesh, float u)
        {
            // Irregular overlapping-looking slabs form a worn halo outside the exact 5x5 footprint.
            // Each has a lipped lower course, chipped corners and an inset, slightly uneven top.
            int seed=0;
            for(int row=0;row<2;row++)
            for(int i=0;i<13;i++)
            {
                float a=(i+.16f*(row%2))*Mathf.PI*2f/13f;
                float rx=row==0?5.8f:6.55f, rz=row==0?5.55f:6.0f;
                float x=Mathf.Cos(a)*rx*u, z=Mathf.Sin(a)*rz*u;
                if(Mathf.Abs(x)<5.35f*u && Mathf.Abs(z)<5.2f*u) continue;
                float w=.88f+(i%4)*.15f, d=.82f+(i%3)*.17f;
                BeveledSlab(mesh,new Vector3(x,-.03f*u,z),w,d,.24f,.13f,u,seed++);
            }
        }

        private static void ExtrudedBevel(MeshData g, Vector2[] top, Vector2[] bottom, float topY, float bottomY, float bevel, float u)
        {
            int n=top.Length; Vector3[] low=new Vector3[n], shoulder=new Vector3[n], crown=new Vector3[n];
            Vector2 centroid=Vector2.zero; foreach(var p in top)centroid+=p; centroid/=n;
            for(int i=0;i<n;i++)
            {
                low[i]=new Vector3(bottom[i].x*u,bottomY,bottom[i].y*u);
                shoulder[i]=new Vector3(top[i].x*u,topY-bevel,top[i].y*u);
                Vector2 inset=Vector2.Lerp(top[i],centroid,bevel/8f);
                crown[i]=new Vector3(inset.x*u,topY,inset.y*u);
            }
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                g.Quad(low[j],low[i],shoulder[i],shoulder[j]);
                g.Quad(shoulder[j],shoulder[i],crown[i],crown[j]);
            }
            for(int i=1;i<n-1;i++)g.Triangle(crown[0],crown[i+1],crown[i]);
        }

        private static void BeveledSlab(MeshData g, Vector3 c, float width, float depth, float height, float bevel, float u, int seed)
        {
            Vector2[] shape=RoundedFootprint(width,depth,seed);
            Vector2[] inner=new Vector2[shape.Length];
            for(int i=0;i<shape.Length;i++)inner[i]=Vector2.Lerp(shape[i],Vector2.zero,.09f);
            Vector3[] bottom=new Vector3[shape.Length], rim=new Vector3[shape.Length], top=new Vector3[shape.Length];
            for(int i=0;i<shape.Length;i++)
            {
                bottom[i]=c+new Vector3(shape[i].x*u,-height*.5f*u,shape[i].y*u);
                rim[i]=c+new Vector3(shape[i].x*u,(height*.5f-bevel)*u,shape[i].y*u);
                top[i]=c+new Vector3(inner[i].x*u,height*.5f*u,inner[i].y*u);
            }
            for(int i=0;i<shape.Length;i++)
            {
                int j=(i+1)%shape.Length;
                g.Quad(bottom[j],bottom[i],rim[i],rim[j]);
                g.Quad(rim[j],rim[i],top[i],top[j]);
            }
            for(int i=1;i<top.Length-1;i++)g.Triangle(top[0],top[i+1],top[i]);
        }

        private static Vector2[] RoundedFootprint(float width,float depth,int seed)
        {
            var x=width*.5f;var z=depth*.5f;var cut=Mathf.Min(width,depth)*.11f;
            var pts=new[]{new Vector2(-x,-z+cut),new Vector2(-x+cut,-z),new Vector2(x-cut,-z),new Vector2(x,-z+cut),new Vector2(x,z-cut),new Vector2(x-cut,z),new Vector2(-x+cut,z),new Vector2(-x,z-cut)};
            for(var i=0;i<pts.Length;i++)pts[i]*=1+.025f*Mathf.Sin(seed*1.71f+i*2.13f);
            return pts;
        }

        private static void Column(MeshData g,Vector3 p,float radius,float height,float u,int seed)
        {
            // A tapered, segmented octagonal shaft with capstones and a chipped plinth.
            BeveledSlab(g,p+Vector3.up*(.12f*u),radius*2.3f,radius*2.1f,.34f,.1f,u,seed);
            int n=9; Vector3[] lo=new Vector3[n],mid=new Vector3[n],hi=new Vector3[n],cap=new Vector3[n];
            for(int i=0;i<n;i++)
            {
                float a=(i+.08f*(seed%3))*Mathf.PI*2f/n;
                float r=radius*(1f+.05f*Mathf.Sin(i*3.7f+seed));
                lo[i]=p+new Vector3(Mathf.Cos(a)*r*u,.25f*u,Mathf.Sin(a)*r*u);
                mid[i]=p+new Vector3(Mathf.Cos(a)*r*.84f*u,height*.52f*u,Mathf.Sin(a)*r*.84f*u);
                hi[i]=p+new Vector3(Mathf.Cos(a)*r*.9f*u,height*.82f*u,Mathf.Sin(a)*r*.9f*u);
                cap[i]=p+new Vector3(Mathf.Cos(a)*r*1.12f*u,height*.9f*u,Mathf.Sin(a)*r*1.12f*u);
            }
            for(int i=0;i<n;i++){int j=(i+1)%n;g.Quad(lo[i],lo[j],mid[j],mid[i]);g.Quad(mid[i],mid[j],hi[j],hi[i]);g.Quad(hi[i],hi[j],cap[j],cap[i]);}
            for(int i=1;i<n-1;i++)g.Triangle(cap[0],cap[i+1],cap[i]);
            BeveledSlab(g,p+Vector3.up*(height*.92f*u),radius*2.35f,radius*2.3f,.42f,.12f,u,seed+4);
        }

        private static void Arch(Dictionary<Layer,MeshData> g,float u,Vector3 origin)
        {
            float radius=2.4f*u, baseY=.05f*u, crownY=3.5f*u;
            Column(g[Layer.ShadowStone],origin+new Vector3(-radius,baseY,0),.58f,2.45f,u,5);
            Column(g[Layer.Limestone],origin+new Vector3(radius,baseY,0),.58f,2.45f,u,7);
            for(int i=0;i<13;i++)
            {
                float a=Mathf.PI*i/12f;
                Vector3 p=origin+new Vector3(Mathf.Cos(a)*radius,baseY+2.2f*u+Mathf.Sin(a)*1.4f*u,0);
                Vector3 tangent=new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0);
                var block=RoundedFootprint(.62f,.9f,i+3);
                AddOrientedStoneBlock(g[Layer.Limestone],p,tangent,block,.58f*u,u);
            }
            BeveledSlab(g[Layer.Limestone],origin+new Vector3(0,crownY,0),1.2f,.95f,.36f,.12f,u,14);
        }

        private static void AddOrientedStoneBlock(MeshData g,Vector3 center,Vector3 along,Vector2[] footprint,float height,float u)
        {
            Vector3 right=along.normalized, forward=Vector3.Cross(right,Vector3.up).normalized;
            for(int i=0;i<footprint.Length;i++)footprint[i]=new Vector2(footprint[i].x*u,footprint[i].y*u);
            Vector3[] lo=new Vector3[footprint.Length],hi=new Vector3[footprint.Length];
            for(int i=0;i<footprint.Length;i++)
            {
                Vector3 offset=right*footprint[i].x+forward*footprint[i].y;
                lo[i]=center+offset-Vector3.up*height*.5f; hi[i]=center+offset+Vector3.up*height*.5f;
            }
            for(int i=0;i<footprint.Length;i++){int j=(i+1)%footprint.Length;g.Quad(lo[j],lo[i],hi[i],hi[j]);}
            for(int i=1;i<hi.Length-1;i++)g.Triangle(hi[0],hi[i+1],hi[i]);
        }

        private static void FloatingRuin(Dictionary<Layer,MeshData> g,Vector3 p,float size,float u)
        {
            BeveledSlab(g[Layer.ShadowStone],p+Vector3.down*.45f*u,2.6f*size,1.55f*size,.75f,.22f,u,20);
            BeveledSlab(g[Layer.Limestone],p,2.9f*size,1.7f*size,.36f,.14f,u,21);
            Column(g[Layer.Limestone],p+new Vector3(-.72f*size*u,.18f*u,0),.38f*size,1.4f*size,u,22);
            BeveledSlab(g[Layer.Limestone],p+new Vector3(-.72f*size*u,1.48f*size*u,0),.9f*size,.85f*size,.2f,.07f,u,23);
        }

        private static void Banner(Dictionary<Layer,MeshData> g,Vector3 p,float height,float u)
        {
            CylinderPole(g[Layer.Gold],p+Vector3.up*(height*.5f*u),.045f*u,height*u,u);
            Vector3 a=p+Vector3.up*.9f*height*u, b=p+Vector3.up*.12f*height*u;
            Vector3 a2=a+Vector3.right*.85f*height*u+Vector3.forward*.08f*u;
            Vector3 b2=b+Vector3.right*.85f*height*u+Vector3.forward*.22f*u;
            Vector3 fold=(a+b+a2+b2)*.25f+Vector3.back*.13f*height*u;
            Vector3 tip=(b+b2)*.5f+Vector3.down*.18f*height*u;
            g[Layer.Blue].Quad(a,a2,fold,(a+b)*.5f+Vector3.back*.1f*height*u);
            g[Layer.Blue].Quad(fold,a2,b2,tip);
            g[Layer.Gold].Quad(a,a+Vector3.up*.045f*height*u,a2+Vector3.up*.045f*height*u,a2);
        }

        private static void CylinderPole(MeshData g,Vector3 center,float radius,float height,float u)
        {
            const int n=7;
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
                Vector3 p0=center+new Vector3(Mathf.Cos(a)*radius*u,-height*.5f,Mathf.Sin(a)*radius*u);
                Vector3 p1=center+new Vector3(Mathf.Cos(b)*radius*u,-height*.5f,Mathf.Sin(b)*radius*u);
                Vector3 p2=p1+Vector3.up*height,p3=p0+Vector3.up*height;
                g.Quad(p1,p0,p3,p2);
            }
        }

        private static void Waterfall(MeshData g,Vector3 p,float width,float depth,float u)
        {
            Vector3 a=p+new Vector3(-width*.5f*u,0,0),b=p+new Vector3(width*.5f*u,0,0);
            Vector3 c=p+new Vector3(width*.32f*u,-depth*u,.18f*u),d=p+new Vector3(-width*.4f*u,-depth*u,-.12f*u);
            g.Quad(a,b,c,d);
            g.Quad(a+Vector3.back*.035f*u,b+Vector3.back*.035f*u,c+Vector3.back*.035f*u,d+Vector3.back*.035f*u);
            g.Triangle(d,c,p+new Vector3(.08f*u,-depth*1.12f*u,.05f*u));
        }

        private static void Garden(Dictionary<Layer,MeshData> g,Vector3 center,float scale,int count,float u,int seed)
        {
            for(int i=0;i<count;i++)
            {
                float f=(i+.41f)/count;
                float x=center.x+(Mathf.Lerp(-.7f,.7f,f)+Mathf.Sin(i*2.17f+seed)*.28f)*scale*u;
                float z=center.z+Mathf.Cos(i*1.91f+seed)*.34f*scale*u;
                float y=center.y+Mathf.Sin(i*1.31f+seed)*.09f*scale*u;
                Vector3 p=new Vector3(x,y,z), d=new Vector3(Mathf.Sin(i*3.7f),.38f,Mathf.Cos(i*2.3f)).normalized;
                float r=(.22f+.07f*(i%3))*scale*u;
                d=new Vector3(d.x,0,d.z).normalized;var side=new Vector3(-d.z,0,d.x)*r*.42f;var middle=p+d*r*.55f+Vector3.up*.07f;
                g[Layer.Foliage].Triangle(p,p+d*r*.5f+side,middle);g[Layer.Foliage].Triangle(p+d*r*.5f+side,p+d*r,middle);g[Layer.Foliage].Triangle(p+d*r,p+d*r*.5f-side,middle);g[Layer.Foliage].Triangle(p+d*r*.5f-side,p,middle);
                if(i%2==0)Flower(g[Layer.Blossom],p+Vector3.up*r*.8f,.105f*scale*u);
            }
        }

        private static void Flower(MeshData g,Vector3 p,float r)
        {
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                Vector3 c=p+new Vector3(Mathf.Cos(a)*r*.52f,0,Mathf.Sin(a)*r*.52f);
                Vector3 side=new Vector3(-Mathf.Sin(a),.15f,Mathf.Cos(a))*r*.38f;
                g.Triangle(c+side,c-side,p);
            }
        }

        private sealed class MeshData
        {
            private readonly List<Vector3> v=new List<Vector3>();
            private readonly List<int> t=new List<int>();
            public int Count=>v.Count;
            public void Triangle(Vector3 a,Vector3 b,Vector3 c){int i=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(i);t.Add(i+1);t.Add(i+2);}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Triangle(a,b,c);Triangle(a,c,d);}
            public Mesh CreateMesh(string name){var m=new Mesh{name=name,indexFormat=v.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};m.SetVertices(v);var uv=new List<Vector2>(v.Count);foreach(var point in v)uv.Add(new Vector2(point.x*.7f+point.y*.21f,point.z*.7f+point.y*.31f));m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }

        private sealed class OwnedAssets:MonoBehaviour
        {
            private readonly List<Object> assets=new List<Object>();
            public void Own(Object asset){assets.Add(asset);}
            private void OnDestroy(){foreach(var asset in assets)if(asset!=null)Object.Destroy(asset);assets.Clear();}
        }
    }
}
