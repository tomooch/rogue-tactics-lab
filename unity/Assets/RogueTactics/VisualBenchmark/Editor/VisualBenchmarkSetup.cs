using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RogueTactics.VisualBenchmark.Editor
{
    // All world art is authored here from geometry; private reference images are never imported.
    public static class VisualBenchmarkSetup
    {
        const string Art = "Assets/RogueTactics/Art/VisualBenchmark";
        static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        static readonly List<Mesh> meshes = new List<Mesh>();
        static Camera camera;
        static Font font;
        static Sprite panel, disc, flat;
        static Transform world;
        static Mesh bevel;
        static readonly Color Ink = Hex("243651"), Cream = Hex("FFF3D6"), Gold = Hex("EFC879");
        static readonly Color[] accents = { Hex("79B9F4"), Hex("F2BD67"), Hex("83CB9E"), Hex("ECA1BA") };
        static readonly string[] names = { "シロ", "ガル", "ミヤル", "モモ" };
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        [MenuItem("Tools/Rogue Tactics/Create Visual Benchmark")]
        public static void Create()
        {
            palette.Clear(); meshes.Clear(); bevel=null;
            Directory.CreateDirectory(Art);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("A2BACB");
            RenderSettings.ambientEquatorColor = Hex("869EAB");
            RenderSettings.ambientGroundColor = Hex("798B95");
            RenderSettings.fog = true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=Hex("B1D3E1");RenderSettings.fogStartDistance=28;RenderSettings.fogEndDistance=50;
            camera = new GameObject("Hero Camera • 390 × 844").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 7.7f;
            camera.transform.rotation = Quaternion.Euler(39, -13, 0);
            camera.transform.position = new Vector3(0, .8f, 1.1f) - camera.transform.forward * 24;
            camera.backgroundColor = Hex("B1D3E1"); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = .1f; camera.farClipPlane = 70;
            camera.allowHDR = false; camera.allowMSAA = true;
            var sun = new GameObject("Warm morning key").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(43, -35, 0); sun.color = Hex("FFE3AF"); sun.intensity = .95f;
            sun.shadows = LightShadows.Soft;
            var fill = new GameObject("Sky fill").AddComponent<Light>(); fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(32, 150, 0); fill.color = Hex("B5D7FA"); fill.intensity = .3f;
            world = new GameObject("Skygarden • hand-authored diorama").transform;
            var fixture = new GameObject("Visual fixture • fictional / no execution").AddComponent<VisualFixture>();
            fixture.renderProfile=RenderProfile();
            fixture.ApplyRenderProfile();
            try
            {
            BuildIsland(); BuildBackdrop();
            var party=ImportAtlas("PartyAtlas",4,true);
            var enemies=ImportAtlas("EnemyAtlas",3,false);
            fixture.companions = new Transform[4];
            Vector3[] places = { new Vector3(-1.2f,.2f,-1.4f), new Vector3(.65f,.2f,-1.1f), new Vector3(-1.35f,.2f,-3.25f), new Vector3(.55f,.2f,-2.85f) };
            for (int i=0;i<4;i++) { fixture.companions[i]=SpriteActor("ABCD"[i]+" • "+names[i],party[i],places[i],i==0?2.15f:2f,10+i); Health(fixture.companions[i], accents[i], false); }
            fixture.monsters = new [] { SpriteActor("Mossy mushroom guardian",enemies[0],new Vector3(.5f,.2f,2.3f),1.75f,2), SpriteActor("Sprout slime",enemies[1],new Vector3(-1.05f,.2f,1.3f),1.15f,3), SpriteActor("Plum bat",enemies[2],new Vector3(.4f,1.2f,3.5f),1.4f,1) };
            foreach(var m in fixture.monsters) Health(m,Hex("E88899"),true);
            var a = new Vector3(.95f,.25f,-1.2f); var b = new Vector3(.8f,.28f,1.0f);
            fixture.arrowOrigin = new GameObject("Fictional intent origin").transform; fixture.arrowOrigin.position = a;
            fixture.arrowDestination = new GameObject("Fictional intent destination").transform; fixture.arrowDestination.position = b;
            Intent(a,b);
            BakeGeometry(); AssetDatabase.SaveAssets();
            font=AssetDatabase.LoadAssetAtPath<Font>(Art+"/NotoSansJP.ttf");
            panel=Texture("RoundedPanel",128,128,(x,y)=>Rounded(x,y,128,128,20));
            disc=Texture("Disc",64,64,(x,y)=>Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))<31);
            flat=Texture("Flat",4,4,(x,y)=>true);
            var portraits = new Sprite[4];
            for(int i=0;i<4;i++) portraits[i]=party[i+4];
            BuildHud(fixture,portraits);
            EditorSceneManager.SaveScene(scene,VisualFixture.ScenePath);
            var usedMeshes=new HashSet<string>();
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))usedMeshes.Add(AssetDatabase.GetAssetPath(mf.sharedMesh));
            foreach(var path in Directory.GetFiles(Art,"Mesh-*.asset"))
                if(!usedMeshes.Contains(path.Replace('\\','/')))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
            // Remove only superseded task-generated meshes/portraits, never reference originals.
            foreach(var path in Directory.GetFiles(Art,"SculptedMesh*.asset"))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
            foreach(var path in Directory.GetFiles(Art,"Portrait*.png"))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
            AssetDatabase.SaveAssets();
            Debug.Log("VISUAL_BENCHMARK_CREATED " + Application.unityVersion);
            }
            finally { fixture.RestoreRenderProfile(); }
        }

        public static void CreateAndCapture()
        {
            Create();
            var fixture=Object.FindFirstObjectByType<VisualFixture>();
            fixture.ApplyRenderProfile();
            try
            {
            var target=new RenderTexture(390,844,24) { antiAliasing=4 };
            camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(390,844,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,390,844),0,0);texture.Apply();
            Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/visual-benchmark-editor.png",texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;
            Object.DestroyImmediate(texture);Object.DestroyImmediate(target);
            Debug.Log("VISUAL_CAPTURED 390x844");
            }
            finally { fixture.RestoreRenderProfile(); }
        }

        static RenderPipelineAsset RenderProfile()
        {
            const string path=Art+"/VisualBenchmark_RP.asset";
            var profile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            if(!profile) { AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset",path);profile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path); }
            var data=new SerializedObject(profile);
            data.FindProperty("m_RenderScale").floatValue=1;
            data.FindProperty("m_MSAA").intValue=4;
            data.FindProperty("m_SoftShadowsSupported").boolValue=true;
            data.FindProperty("m_MainLightShadowmapResolution").intValue=2048;
            data.FindProperty("m_ShadowDepthBias").floatValue=.3f;
            data.FindProperty("m_ShadowNormalBias").floatValue=.3f;
            data.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        static Material Mat(string hex, bool unlit=false)
        {
            string key=hex+(unlit?"_unlit":""); if(palette.TryGetValue(key,out var result)) return result;
            string path=Art+"/"+key+".mat";
            result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!result) { result=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result,path); }
            result.SetColor("_BaseColor",Hex(hex)); result.SetFloat("_Smoothness",.18f); palette[key]=result; return result;
        }
        static GameObject Shape(string name,PrimitiveType kind,Vector3 pos,Vector3 scale,string color,Transform parent=null)
        {
            var o=GameObject.CreatePrimitive(kind); o.name=name; o.transform.SetParent(parent?parent:world,false);
            o.transform.localPosition=pos; o.transform.localScale=scale; o.GetComponent<Renderer>().sharedMaterial=Mat(color);
            Object.DestroyImmediate(o.GetComponent<Collider>()); return o;
        }
        static GameObject Ball(string name,Vector3 pos,Vector3 scale,string color,Transform parent=null) => Shape(name,PrimitiveType.Sphere,pos,scale,color,parent);
        static GameObject Block(string name,Vector3 pos,Vector3 scale,string color,Transform parent=null)
        {
            if(!bevel) bevel=BevelMesh();
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent?parent:world,false);
            o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<MeshFilter>().sharedMesh=bevel;o.GetComponent<MeshRenderer>().sharedMaterial=Mat(color);return o;
        }
        static Mesh BevelMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();float h=.5f,b=.44f;
            Action<Vector3[]> face = poly =>
            {
                int start=vertices.Count;var normal=Vector3.Cross(poly[1]-poly[0],poly[2]-poly[0]);var center=Vector3.zero;
                foreach(var v in poly)center+=v;
                if(Vector3.Dot(normal,center)<0)Array.Reverse(poly);
                vertices.AddRange(poly);for(int i=1;i<poly.Length-1;i++)triangles.AddRange(new[]{start,start+i,start+i+1});
            };
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;var poly=new Vector3[4];int[] a={-1,1,1,-1},c={-1,-1,1,1};
                for(int i=0;i<4;i++){poly[i][axis]=sign*h;poly[i][u]=a[i]*b;poly[i][v]=c[i]*b;}face(poly);
            }
            for(int axis=0;axis<3;axis++)for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;var p=new Vector3[4];
                p[0][axis]=-b;p[0][u]=s*h;p[0][v]=t*b;
                p[1][axis]=b;p[1][u]=s*h;p[1][v]=t*b;
                p[2][axis]=b;p[2][u]=s*b;p[2][v]=t*h;
                p[3][axis]=-b;p[3][u]=s*b;p[3][v]=t*h;face(p);
            }
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                face(new[]{new Vector3(x*h,y*b,z*b),new Vector3(x*b,y*h,z*b),new Vector3(x*b,y*b,z*h)});
            var mesh=new Mesh{name="Soft chamfered stone"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();meshes.Add(mesh);return mesh;
        }
        static GameObject Rod(string name,Vector3 a,Vector3 b,float radius,string color,Transform parent=null)
        {
            var o=Shape(name,PrimitiveType.Cylinder,(a+b)/2,new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2),color,parent);
            o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a); return o;
        }
        static GameObject Custom(string name,Vector3[] vertices,int[] triangles,string color,Transform parent)
        {
            var mesh=new Mesh { name=name }; mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals(); meshes.Add(mesh);
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); o.transform.SetParent(parent,false);
            o.GetComponent<MeshFilter>().sharedMesh=mesh; o.GetComponent<MeshRenderer>().sharedMaterial=Mat(color); return o;
        }
        static void Cone(string name,Vector3 center,float radius,float height,string color,Transform parent,int sides=24)
        {
            var v=new Vector3[sides+2]; v[0]=center+Vector3.up*height; v[1]=center;
            for(int i=0;i<sides;i++){ float t=i*Mathf.PI*2/sides; v[i+2]=center+new Vector3(Mathf.Cos(t)*radius,0,Mathf.Sin(t)*radius); }
            var tlist=new List<int>(); for(int i=0;i<sides;i++){int j=(i+1)%sides;tlist.AddRange(new[]{0,j+2,i+2,1,i+2,j+2});}
            Custom(name,v,tlist.ToArray(),color,parent);
        }
        static void BuildIsland()
        {
            Block("Floating sandstone strata",new Vector3(0,-.55f,.5f),new Vector3(4.6f,1.2f,8.7f),"929D9E");
            var rng=new System.Random(12);
            for(int z=0;z<12;z++) for(int x=0;x<6;x++)
            {
                float xx=-1.9f+x*.76f+(x>0&&x<5?(z%2)*.14f:0),zz=-3.6f+z*.72f;
                var o=Block("Weathered paving stone",new Vector3(xx,.08f,zz),new Vector3(.71f+(float)rng.NextDouble()*.05f,.25f,.68f),new[]{"D4CFB8","C9CBB6","DAD5BF","B6C3AD"}[rng.Next(4)]);
                o.transform.localRotation=Quaternion.Euler(0,(float)(rng.NextDouble()*3-1.5),0);
                if(x==0||x==5) { if(rng.NextDouble()<.7) Fern(new Vector3(xx,.23f,zz),.34f); if(rng.NextDouble()<.4) Flower(new Vector3(xx,.42f,zz),.09f); }
            }
            for(int z=0;z<10;z++)
            {
                float zz=-3.1f+z*.86f;
                foreach(float x in new[]{-2.26f,2.26f})
                {
                    Block("Broken edge masonry",new Vector3(x,-.2f,zz),new Vector3(.39f,.63f,.75f),z%2==0?"B5B5A2":"C8C4AD");
                    if(z%2==0) Fern(new Vector3(x,-.05f,zz),.4f);
                    for(int j=0;j<3;j++) Ball("Trailing ivy leaf",new Vector3(x+Mathf.Sign(x)*.1f,-.4f-j*.23f,zz),new Vector3(.24f,.26f,.17f),j%2==0?"819D64":"A7BC73");
                }
            }
            // Deliberately local terrain details rather than a permanent tactical grid.
            Pillar(new Vector3(-1.98f,.2f,3.65f),2.4f); Pillar(new Vector3(1.96f,.2f,4.05f),2.9f);
            for(int i=0;i<5;i++) Block("Ruined lintel",new Vector3(-1.7f+i*.76f,2.78f,4.25f),new Vector3(.73f,.4f,.52f),"C3C4AF");
            Fern(new Vector3(-1.7f,3f,4.1f),.6f); Fern(new Vector3(1.6f,3.2f,4f),.5f);
            Lamp(new Vector3(-2.05f,.3f,.2f)); Lamp(new Vector3(2.2f,.3f,2.1f));
            Banner(new Vector3(-2.15f,2.2f,3.2f));
            Chest(new Vector3(1.75f,.27f,-.15f));
            for(int i=0;i<12;i++) { var p=new Vector3(-2.15f+(i%2)*4.3f,.33f,-3.2f+(i/2)*1.3f); Flower(p,.11f); }
            // Fallen stones and a fork of leafy branches frame the front edge.
            for(int i=0;i<3;i++) Block("Ancient front steps",new Vector3(.15f,-.16f-i*.23f,-3.98f-i*.35f),new Vector3(2.0f,.3f,.45f),"BCBBA6");
        }
        static void Pillar(Vector3 p,float height)
        {
            Block("Pillar foot",p,new Vector3(.9f,.3f,.9f),"B8B7A4");
            for(int i=0;i<(int)(height/.42f);i++) { var o=Block("Individual ruin block",p+new Vector3((i%2)*.04f,.25f+i*.42f,0),new Vector3(.64f,.38f,.64f),i%2==0?"DDD4BB":"C5C5AE"); o.transform.localRotation=Quaternion.Euler(0,i%2==0?2:-3,0); }
            Block("Pillar crown",p+Vector3.up*height,new Vector3(.82f,.24f,.82f),"E0D7BC");
            for(int i=0;i<4;i++) Fern(p+new Vector3(-.25f,.5f+i*.48f,-.38f),.24f);
        }
        static void Fern(Vector3 p,float size)
        {
            for(int i=0;i<5;i++)
            {
                float angle=i*1.256f; var leaf=Ball("Soft jade leaf",p+new Vector3(Mathf.Cos(angle)*size*.4f,size*.28f,Mathf.Sin(angle)*size*.4f),new Vector3(size*.36f,size*.18f,size),i%2==0?"88AA66":"A9BE74");
                leaf.transform.localRotation=Quaternion.Euler(-24,i*72,18);
            }
        }
        static void Flower(Vector3 p,float size)
        {
            Rod("Flower stem",p,p+Vector3.up*.17f,.012f,"709561");
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Ball("Ivory petal",p+new Vector3(Mathf.Cos(a)*size,.19f,Mathf.Sin(a)*size),new Vector3(size,size*.55f,size),"FFF1D3");}
            Ball("Honey pollen",p+Vector3.up*.22f,Vector3.one*size*.65f,"EBC775");
        }
        static void Lamp(Vector3 p)
        {
            Rod("Lantern post",p,p+Vector3.up*1.55f,.045f,"5C6B67");
            var q=p+Vector3.up*1.5f;
            Block("Amber lantern",q,new Vector3(.29f,.43f,.29f),"FFE2A1");
            Cone("Lantern roof",q+Vector3.up*.25f,.27f,.18f,"67746B",world,8);
            for(int i=0;i<4;i++) {float a=Mathf.PI/4+i*Mathf.PI/2; Rod("Lantern frame",q+new Vector3(Mathf.Cos(a)*.19f,-.23f,Mathf.Sin(a)*.19f),q+new Vector3(Mathf.Cos(a)*.19f,.23f,Mathf.Sin(a)*.19f),.02f,"8D7954");}
            var light=new GameObject("Lantern honey glow").AddComponent<Light>(); light.transform.position=q; light.type=LightType.Point; light.color=Hex("FFCE78");light.range=2.6f;light.intensity=1.3f;
        }
        static void Banner(Vector3 p)
        {
            Rod("Banner arm",p,p+Vector3.right*.85f,.04f,"7C755F");
            var v=new[]{p+new Vector3(.12f,-.06f,-.02f),p+new Vector3(.78f,-.06f,-.02f),p+new Vector3(.73f,-1.34f,-.02f),p+new Vector3(.43f,-1.14f,-.04f),p+new Vector3(.13f,-1.34f,-.02f)};
            Custom("Azure swallowtail banner",v,new[]{0,1,3,1,2,3,0,3,4,3,1,0,3,2,1,4,3,0},"628EB0",world);
            Ball("Sun crest",p+new Vector3(.43f,-.5f,-.055f),new Vector3(.28f,.28f,.035f),"E6C77E");
        }
        static void Chest(Vector3 p)
        {
            Block("Treasure coffer",p+Vector3.up*.24f,new Vector3(.7f,.5f,.5f),"A3764E");
            Ball("Rounded chest lid",p+Vector3.up*.48f,new Vector3(.74f,.37f,.51f),"BD9056");
            foreach(float x in new[]{-.25f,.25f})Block("Brass chest band",p+new Vector3(x,.3f,-.27f),new Vector3(.075f,.52f,.04f),"EDCA78");
            Block("Chest lock",p+new Vector3(0,.3f,-.29f),new Vector3(.16f,.19f,.05f),"F4D785");
            Fern(p+Vector3.right*.42f,.32f);
        }
        static void BuildBackdrop()
        {
            // Real distant ruin geometry and clouds, independently rendered behind the island.
            for(int i=0;i<8;i++)
            {
                float x=-9+i*2.8f,z=9+(i%3)*3;
                Block("Distant floating ruin",new Vector3(x,-1.5f,z),new Vector3(1.8f,3.2f,2),"89ADBE");
                Block("Distant tower",new Vector3(x,1.5f,z+.5f),new Vector3(.8f,2.6f+(i%3),.8f),"ABC5CA");
                Cone("Distant azure roof",new Vector3(x,3.2f+(i%3)*.5f,z+.5f),.68f,.95f,"91B0C3",world,6);
                for(int j=0;j<2;j++)Block("Distant golden window",new Vector3(x,.85f+j*.95f,z+.07f),new Vector3(.2f,.48f,.04f),"C8DFDE");
                Ball("Distant cloud",new Vector3(x,-2.5f,z-1),new Vector3(5,1,2.5f),"D5E8E8");
            }
            for(int i=0;i<9;i++)Ball("Lower cloud bank",new Vector3(-8+i*2,-3.0f,1+(i%3)*2),new Vector3(3.8f,.65f,2),"DAEAEA");
        }
        static Sprite[] ImportAtlas(string name,int count,bool portraits)
        {
            string path=Art+"/"+name+".png";
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));
            var pixels=texture.GetPixels32();var slices=new List<SpriteMetaData>();
            int halfW=texture.width/2,halfH=texture.height/2;
            for(int i=0;i<count;i++)
            {
                int startX=(i%2)*halfW,startY=(1-i/2)*halfH;
                int minX=startX+halfW,minY=startY+halfH,maxX=startX,maxY=startY;
                for(int y=startY;y<startY+halfH;y++)for(int x=startX;x<startX+halfW;x++)
                    if(pixels[y*texture.width+x].a>30){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
                var rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
                slices.Add(new SpriteMetaData{name=name+i,rect=rect,alignment=(int)SpriteAlignment.BottomCenter,pivot=new Vector2(.5f,0)});
                if(portraits)
                {
                    float size=rect.width*.84f;
                    slices.Add(new SpriteMetaData{name=name+"Portrait"+i,rect=new Rect(rect.center.x-size*.5f,rect.yMax-rect.height*.08f-size*.625f,size,size*.625f),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)});
                }
            }
            Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;SetFullRect(importer);
#pragma warning disable 0618
            importer.spritesheet=slices.ToArray();
#pragma warning restore 0618
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;importer.SaveAndReimport();
            var result=new Sprite[portraits?count*2:count];
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite sprite)
                for(int i=0;i<count;i++){if(sprite.name==name+i)result[i]=sprite;if(portraits&&sprite.name==name+"Portrait"+i)result[i+count]=sprite;}
            foreach(var sprite in result)if(!sprite)throw new InvalidOperationException("Missing atlas sprite: "+name);
            return result;
        }
        static Transform SpriteActor(string name,Sprite sprite,Vector3 position,float height,int order)
        {
            var root=new GameObject(name+" • independent 2.5D art").transform;root.SetParent(world,false);root.position=position;
            var art=new GameObject("Painted character sprite",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();art.transform.SetParent(root,false);
            art.sprite=sprite;art.sortingOrder=order;art.transform.rotation=camera.transform.rotation;
            float scale=height/sprite.bounds.size.y;art.transform.localScale=Vector3.one*scale;
            // The bottom-center sprite pivot is grounded; no camera-facing backdrop or full-screen art.
            art.transform.localPosition=Vector3.zero;
            if(!name.StartsWith("Plum"))Ball("Ground contact shade",new Vector3(0,.008f,0),new Vector3(.75f,.014f,.45f),"8C9B92",root);
            return root;
        }
        static void Curve(string name,Vector3[] points,float width,string color,Transform parent)
        {
            var o=new GameObject(name,typeof(LineRenderer));o.transform.SetParent(parent,false);
            var line=o.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;
            line.numCapVertices=5;line.numCornerVertices=4;line.sharedMaterial=Mat(color,true);line.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void Ring(string name,Vector3 p,float radius,float width,string color,Transform parent,bool ground=true)
        {
            var points=new Vector3[49];for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;points[i]=p+(ground?new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius):new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}Curve(name,points,width,color,parent);
        }
        static void Intent(Vector3 a,Vector3 b)
        {
            Ring("Grounded origin ring",a,.32f,.035f,"A6E4F1",world);
            Ring("Destination ring",b,.38f,.045f,"BFEAF0",world);
            var points=new Vector3[40];for(int i=0;i<40;i++){float t=i/39f;points[i]=Vector3.Lerp(a,b,t)+Vector3.up*(.25f+Mathf.Sin(t*Mathf.PI)*.65f)+Vector3.right*Mathf.Sin(t*Mathf.PI)*.9f;}
            Curve("Arcing fictional intent • blue rim",points,.11f,"70BDDC",world);
            Curve("Arcing fictional intent • cream core",points,.044f,"EDFFFF",world);
            var tip=points[39];var dir=(tip-points[37]).normalized;var side=Vector3.Cross(dir,camera.transform.forward).normalized;
            Custom("Intent arrowhead",new[]{tip+dir*.25f,tip-dir*.15f+side*.2f,tip-dir*.15f-side*.2f},new[]{0,1,2,2,1,0},"EDFFFF",world);
            // Faint destination cue is a visual hypothesis, not legal movement or a Core plan.
            Ball("Destination shimmer",b+Vector3.up*.02f,new Vector3(.35f,.035f,.35f),"BDE7E8");
        }
        static void Health(Transform actor,Color color,bool enemy)
        {
            float h=enemy?(actor.name.StartsWith("Plum")?1.52f:actor.name.StartsWith("Mossy")?1.87f:1.27f):actor.name.StartsWith("A")?2.27f:2.12f;
            var root=new GameObject("Illustrative vitality bar").transform;root.SetParent(actor,false);root.position=actor.position+camera.transform.up*h;root.rotation=camera.transform.rotation;
            var bg=Block("Bar border",Vector3.zero,new Vector3(.75f,.085f,.02f),"3D5360",root);bg.GetComponent<Renderer>().sharedMaterial=Mat("3D5360",true);
            var inner=Block("Static vitality",new Vector3(-.035f,0,-.014f),new Vector3(.63f,.044f,.015f),enemy?"EC9BA3":"B3D893",root);inner.GetComponent<Renderer>().sharedMaterial=Mat(enemy?"EC9BA3":"B3D893",true);
        }
        static void BakeGeometry()
        {
            foreach(var mesh in meshes)
            {
                // Content addressed assets avoid reusing stale GPU buffers from a different sculpt.
                var bytes=new List<byte>();
                foreach(var vertex in mesh.vertices) { bytes.AddRange(BitConverter.GetBytes(vertex.x));bytes.AddRange(BitConverter.GetBytes(vertex.y));bytes.AddRange(BitConverter.GetBytes(vertex.z)); }
                foreach(var index in mesh.triangles)bytes.AddRange(BitConverter.GetBytes(index));
                string hash;
                using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-","").Substring(0,20);
                string path=Art+"/Mesh-"+hash+".asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!saved) { AssetDatabase.CreateAsset(mesh,path);saved=mesh; }
                foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))if(mf.sharedMesh==mesh)mf.sharedMesh=saved;
            }
        }
        static bool Rounded(int x,int y,int w,int h,int r)
        {
            float cx=Mathf.Clamp(x,r,w-r-1),cy=Mathf.Clamp(y,r,h-r-1);return (x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r;
        }
        static Sprite Texture(string name,int width,int height,Func<int,int,bool> inside)
        {
            var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=inside(x,y)?Color.white:Color.clear;
            tex.SetPixels(pixels);tex.Apply();string path=Art+"/"+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);
            return ImportSprite(path,name=="RoundedPanel"?new Vector4(22,22,22,22):Vector4.zero);
        }
        static void SetFullRect(TextureImporter importer)
        {
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        }
        static Sprite ImportSprite(string path,Vector4 border)
        {
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;SetFullRect(importer);importer.spriteBorder=border;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        static Image Image(Transform parent,string name,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var r=Rect(parent,name,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite?sprite:panel;im.color=color;im.type=im.sprite==panel?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;im.raycastTarget=false;return im;
        }
        static Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var r=Rect(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=alignment;
            t.fontStyle=size>=12?FontStyle.Bold:FontStyle.Normal;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        static void BuildHud(VisualFixture fixture,Sprite[] portraits)
        {
            var canvas=new GameObject("Portrait HUD • all motifs illustrative",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(390,844);scaler.matchWidthOrHeight=0;
            var hud=canvas.transform;
            fixture.fieldWindow=Rect(hud,"Field composition window",8,126,374,524);
            Image(hud,"Title border",14,25,276,58,Gold);
            Image(hud,"Title panel",16,27,272,54,Ink);
            Label(hud,"Location name","天空の遺跡",30,26,244,34,21,Cream);
            Label(hud,"Floor subtitle","SKYGARDEN  /  12F・15F",31,60,241,17,10,Hex("C9DADD"));
            Image(hud,"Goal pill",15,88,284,28,new Color(Ink.r,Ink.g,Ink.b,.95f));
            Label(hud,"Human-stated goal","目的  ·  奥の宝箱へ進みたい",26,89,263,26,12,Cream);
            Minimap(hud);Floors(hud);
            // Low-density, neutral authored differences. E never exists in the field.
            Image(hud,"E adviser panel",14,650,362,52,Ink);
            Image(hud,"E portrait badge",22,658,32,32,Hex("CED9BF"),disc);
            Label(hud,"E identity","E",22,658,32,32,18,Ink,TextAnchor.MiddleCenter);
            Label(hud,"E fictional proposal","E の提案  ·  ガルが先に様子を見る",63,653,305,22,12,Cream);
            Label(hud,"Companion fictional intent","ガルの意図  ·  右側の足場へ移りたい",63,678,305,19,11,Hex("B6DDE7"));
            for(int i=0;i<4;i++) Card(hud,i,portraits[i]);
            Label(hud,"Fixture disclosure","表示モック · スキル実行なし / 意図は架空",14,823,362,18,10,Ink,TextAnchor.MiddleCenter);
        }
        static void Minimap(Transform hud)
        {
            var map=Image(hud,"Compact separate minimap",16,129,84,84,new Color(Ink.r,Ink.g,Ink.b,.91f)).transform;
            Label(map,"Map title","MAP",8,2,68,16,9,Hex("D1E1DE"));
            var cells=new[]{new Vector2(1,4),new Vector2(2,4),new Vector2(2,3),new Vector2(2,2),new Vector2(3,2),new Vector2(3,1),new Vector2(4,1),new Vector2(4,0),new Vector2(1,2),new Vector2(0,2),new Vector2(4,3)};
            foreach(var c in cells)Image(map,"Mini-map room",12+c.x*12,23+c.y*10,11,9,Hex("728A99"),flat);
            Image(map,"Party dot",35,64,7,7,Hex("A6E4F1"),disc);
            Image(map,"Chest dot",62,23,7,7,Gold,disc);
            Image(map,"Enemy dot",49,35,7,7,Hex("E8A1AB"),disc);
        }
        static void Floors(Transform hud)
        {
            var t=Image(hud,"Vertical floor timeline",323,127,53,195,new Color(Ink.r,Ink.g,Ink.b,.82f)).transform;
            // Cat crown silhouette follows the user's cropped timeline reference.
            Image(t,"Cat head",12,8,17,14,Cream,disc);
            Image(t,"Cat left ear",11,6,6,7,Cream,disc);Image(t,"Cat right ear",24,6,6,7,Cream,disc);
            Image(t,"Cat eye L",16,13,2,3,Ink,disc);Image(t,"Cat eye R",23,13,2,3,Ink,disc);
            Image(t,"Connected floors",19,28,2,145,Hex("BAC7CE"),flat);
            for(int i=0;i<5;i++)
            {
                float y=37+i*31;
                if(i==3)Image(t,"Current floor highlight",4,y-5,45,25,Hex("B59B63"));
                Image(t,"Floor node "+(15-i),13,y,14,14,i==3?Cream:Hex("A5B3C0"),disc);
                Label(t,"Floor label "+(15-i),(15-i)+"F",30,y-3,22,20,10,i==3?Cream:Hex("CDD9DF"));
                if(i==3)Image(t,"Current floor center",17,y+4,6,6,Gold,disc);
            }
        }
        static void Card(Transform hud,int i,Sprite portrait)
        {
            float x=14+i*92;
            var c=Image(hud,"Companion card "+i,x,711,86,111,Ink).transform;
            Image(c,"Portrait frame",3,3,80,50,accents[i]);
            Image(c,"Rendered portrait",4,4,78,48,Color.white,portrait).preserveAspect=false;
            Label(c,"Companion name",names[i],5,51,76,19,12,Cream);
            Image(c,"Vitality track",5,72,76,5,Hex("44576D"));Image(c,"Illustrative vitality",5,72,63,5,Hex("A9D28C"));
            for(int j=0;j<2;j++)
            {
                var badge=Image(c,"Skill motif "+j,6+j*39,79,34,32,accents[i]).transform;
                Image(badge,"Skill inset",1,1,32,30,Ink);
                var sprite=SkillIcon(i,j);
                Image(badge,"Illustration only",3,2,28,28,Color.white,sprite);
            }
        }
        static Sprite SkillIcon(int role,int index)
        {
            // Original pixel silhouettes: crystals, shield, bow/arrow, heart/flower.
            Func<int,int,bool> shape=(x,y)=>
            {
                float xx=(x-31.5f)/28,yy=(y-31.5f)/28;
                if(role==0) return index==0 ? Mathf.Abs(xx)*.8f+Mathf.Abs(yy)<.86f : Mathf.Abs(xx*xx+yy*yy-.48f)<.13f || Mathf.Abs(xx+yy)<.11f && Mathf.Abs(yy)<.7f;
                if(role==1) return index==0 ? Mathf.Abs(xx)<.61f && yy<.65f && yy>-.8f+Mathf.Abs(xx)*.62f : Mathf.Abs(xx)<.15f && Mathf.Abs(yy)<.75f || Mathf.Abs(yy)<.15f && Mathf.Abs(xx)<.7f;
                if(role==2) return index==0 ? Mathf.Abs(xx-yy)<.14f && Mathf.Abs(yy)<.7f || yy>.25f && xx>.18f && xx<.8f && yy<.8f : Mathf.Abs(Mathf.Sqrt((xx+.3f)*(xx+.3f)+yy*yy)-.68f)<.1f && xx>.05f || Mathf.Abs(xx-.36f)<.035f && Mathf.Abs(yy)<.57f;
                return index==0 ? yy<.22f && yy>-.85f+Mathf.Abs(xx)*1.4f || (xx-.32f)*(xx-.32f)+(yy-.28f)*(yy-.28f)<.17f || (xx+.32f)*(xx+.32f)+(yy-.28f)*(yy-.28f)<.17f : Mathf.Abs(xx)<.12f && Mathf.Abs(yy)<.7f || Mathf.Abs(yy)<.12f && Mathf.Abs(xx)<.7f || Mathf.Abs(xx-yy)<.09f && Mathf.Abs(xx)<.45f || Mathf.Abs(xx+yy)<.09f && Mathf.Abs(xx)<.45f;
            };
            var tex=new Texture2D(96,96,TextureFormat.RGBA32,false);var pixels=new Color[96*96];
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)
            {
                float xx=(x-47.5f)/44,yy=(y-47.5f)/44;float radius=Mathf.Sqrt(xx*xx+yy*yy);
                float glow=Mathf.Exp(-radius*radius*2.4f);
                Color c=Color.Lerp(Ink,accents[role],glow*.6f);
                if(shape(x*64/96,y*64/96))c=Color.Lerp(accents[role],Cream,.62f+glow*.3f);
                bool star=(Mathf.Abs(xx+.65f)<.025f&&Mathf.Abs(yy-.6f)<.16f)||(Mathf.Abs(yy-.6f)<.025f&&Mathf.Abs(xx+.65f)<.16f);
                if(star)c=Cream;
                c.a=Rounded(x,y,96,96,10)?1:0;pixels[y*96+x]=c;
            }
            tex.SetPixels(pixels);tex.Apply();string path=Art+"/Skill"+role+index+".png";
            File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);return ImportSprite(path,Vector4.zero);
        }
    }
}
