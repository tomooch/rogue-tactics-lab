using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using RogueTactics.Presentation.Gfx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RogueTactics.Tests.PlayMode
{
    // Relic icons and the floor spring, built on their own before anything wires them into the floor flow.
    // Set RT_GFX_CAPTURE=<n> (e.g. 3) to also save a gallery under Logs/hq/private/gallery for review: the icons at
    // 3x/2x/1x of 44 pt and as bare silhouettes, the spring close up (idle and drinking), and the spring and two icons
    // on the battle board beside the companions. Captures include approved art: they stay on this machine.
    public class GfxRelicSpringGalleryTests
    {
        private static bool Capturing=>int.TryParse(Environment.GetEnvironmentVariable("RT_GFX_CAPTURE"),out var n) && n>0;
        private static string Gallery=>Path.GetFullPath("Logs/hq/private/gallery");

        [Test]
        public void RelicIconsAreFilledAndToldApartBySilhouette()
        {
            Assert.That(HqRelicArt.Ids.Length,Is.EqualTo(6));
            var masks=new Dictionary<string,bool[]>();
            foreach(var id in HqRelicArt.Ids)
            {
                var icon=HqRelicArt.Icon(id);
                Assert.That(icon,Is.Not.Null,id);Assert.That(icon.width,Is.EqualTo(HqRelicArt.Size));Assert.That(icon.height,Is.EqualTo(HqRelicArt.Size));
                Assert.That(HqRelicArt.Icon(id),Is.SameAs(icon),id+" is cached");
                Assert.That(HqRelicArt.Sprite(id).texture,Is.SameAs(icon),id+" sprite uses the cached icon");
                // Judged as it will be seen: shrunk to 44 px.
                var small=Downsample(icon,44);masks[id]=small.Select(c=>c.a>.5f).ToArray();
                var cover=masks[id].Count(b=>b)/(float)masks[id].Length;
                Debug.Log($"RELIC {id} cover44={cover:0.000}");
                Assert.That(cover,Is.InRange(.18f,.80f),id+" fills its box without becoming a square");
                var opaque=small.Where(c=>c.a>.9f).ToArray();
                var luma=opaque.Select(c=>c.r*.3f+c.g*.59f+c.b*.11f).ToArray();
                Assert.That(luma.Max()-luma.Min(),Is.GreaterThan(.5f),id+" has an outline and a highlight, not a flat blob");
            }
            if(Capturing)SaveIconSheet();
            var failures=new List<string>();
            for(var i=0;i<HqRelicArt.Ids.Length;i++)for(var j=i+1;j<HqRelicArt.Ids.Length;j++)
            {
                var a=masks[HqRelicArt.Ids[i]];var b=masks[HqRelicArt.Ids[j]];
                var differ=Enumerable.Range(0,a.Length).Count(k=>a[k]!=b[k]);var union=Enumerable.Range(0,a.Length).Count(k=>a[k]||b[k]);
                var silhouette=differ/(float)union;
                var ca=HqRelicArt.Accent(HqRelicArt.Ids[i]);var cb=HqRelicArt.Accent(HqRelicArt.Ids[j]);
                var hue=Mathf.Sqrt((ca.r-cb.r)*(ca.r-cb.r)+(ca.g-cb.g)*(ca.g-cb.g)+(ca.b-cb.b)*(ca.b-cb.b));
                Debug.Log($"RELIC {HqRelicArt.Ids[i]} vs {HqRelicArt.Ids[j]} silhouette={silhouette:0.000} colour={hue:0.000}");
                // Shape first: at 44 px at least a quarter of the two silhouettes' combined area must disagree.
                if(silhouette<=.25f)failures.Add($"{HqRelicArt.Ids[i]} and {HqRelicArt.Ids[j]} share their shape at 44 px ({silhouette:0.000})");
                if(hue<=.2f)failures.Add($"{HqRelicArt.Ids[i]} and {HqRelicArt.Ids[j]} share their colour ({hue:0.000})");
            }
            Assert.That(failures,Is.Empty);
            Assert.Throws<ArgumentException>(()=>HqRelicArt.Icon("no_such_relic"));
            Assert.Throws<ArgumentException>(()=>HqRelicArt.Accent("no_such_relic"));
        }

        [UnityTest]
        public IEnumerator SpringFitsOneCellShimmersAndShowsTheDrink()
        {
            var made=new List<Object>();
            try
            {
                // Far from the board, in case a previous test left a scene loaded.
                var origin=new Vector3(900,-300,900);
                var camera=GalleryCamera(origin+new Vector3(0,.4f,0),1.9f);made.Add(camera.gameObject);
                var rt=new RenderTexture(512,512,24){antiAliasing=4};made.Add(rt);camera.targetTexture=rt;
                var spring=HqSpringProp.Build(null,origin,camera);made.Add(spring.gameObject);
                Assert.That(spring.Spent,Is.False);
                var renderers=spring.GetComponentsInChildren<Renderer>();
                Assert.That(renderers.Length,Is.GreaterThan(10),"basin, rim stones, moss, lily pad, water and light");
                Assert.That(renderers.Select(r=>r.sharedMaterial.shader.name),Has.Some.EqualTo("RogueTactics/Spring"),"the water and light use the spring shader");
                var solid=renderers.Where(r=>!r.name.Contains("light")).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
                Debug.Log($"SPRING solid bounds size={solid.size} top={solid.max.y:0.000}");
                Assert.That(solid.size.x,Is.LessThanOrEqualTo(2.5f-.2f),"fits one board cell across");
                Assert.That(solid.size.z,Is.LessThanOrEqualTo(2.5f-.2f),"fits one board cell deep");
                Assert.That(solid.max.y-origin.y,Is.LessThanOrEqualTo(HqSpringProp.Height),"low enough not to hide a companion behind it");

                yield return null;yield return null;
                var first=Read(rt);yield return new WaitForSecondsRealtime(.6f);var later=Read(rt);
                var background=camera.backgroundColor;
                var drawn=first.Count(c=>Mathf.Abs(c.r-background.r)+Mathf.Abs(c.g-background.g)+Mathf.Abs(c.b-background.b)>.08f)/(float)first.Length;
                var moved=Enumerable.Range(0,first.Length).Count(i=>Mathf.Abs(first[i].r-later[i].r)+Mathf.Abs(first[i].g-later[i].g)+Mathf.Abs(first[i].b-later[i].b)>.03f)/(float)first.Length;
                Debug.Log($"SPRING drawn={drawn:0.000} shimmer={moved:0.000}");
                Assert.That(drawn,Is.GreaterThan(.15f),"the spring is drawn");
                Assert.That(moved,Is.GreaterThan(.005f),"the water shimmers");

                var vfx=HqVfx.Create(camera,null);made.Add(vfx.gameObject);vfx.Ambient=false;
                var party=new[]{origin+new Vector3(-2.5f,0,0),origin+new Vector3(2.5f,0,0),origin+new Vector3(0,0,2.5f),origin+new Vector3(0,0,-2.5f)};
                if(Capturing){Directory.CreateDirectory(Gallery);for(var i=0;i<3;i++){Save(rt,"spring_idle_"+i);yield return new WaitForSecondsRealtime(.45f);}}
                var times=spring.Drink(vfx,party);
                Assert.That(spring.Spent,Is.True);
                Assert.That(times,Is.Ordered.Ascending);Assert.That(times.Length,Is.EqualTo(4));Assert.That(times.Last(),Is.LessThan(HqSpringProp.Duration));
                var start=Time.realtimeSinceStartup;var peak=0;var shots=0;
                while(Time.realtimeSinceStartup-start<HqSpringProp.Duration+.3f)
                {
                    yield return null;peak=Mathf.Max(peak,vfx.Alive);
                    if(Capturing && Time.realtimeSinceStartup-start>shots*.15f){Save(rt,$"spring_drink_{shots:00}");shots++;}
                }
                Assert.That(peak,Is.GreaterThan(0),"each companion gets the heal light");
                yield return new WaitForSecondsRealtime(1f);
                if(Capturing)Save(rt,"spring_spent");
                spring.SetSpent(false);Assert.That(spring.Spent,Is.False);
            }
            finally{foreach(var o in made)if(o!=null)Object.Destroy(o);}
        }

        // Capture only: the spring on an empty cell of the battle board beside the companions, and a pair of relic
        // icons at 44 pt over the board, to judge both at phone size next to the approved characters.
        [UnityTest]
        public IEnumerator CaptureSpringAndIconsOnTheBoard()
        {
            if(!Capturing)Assert.Ignore("Set RT_GFX_CAPTURE to capture the board view.");
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var rt=new RenderTexture(780,1688,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,68,780,1526),780,1688);yield return null;yield return new WaitForSecondsRealtime(.5f);
            var pawns=Fixture.Baseline().Units.Select(u=>GameObject.Find("Fixture "+u.Id)).Where(g=>g!=null).Select(g=>g.transform.position).ToArray();
            Assert.That(pawns.Length,Is.GreaterThan(0));
            var centre=pawns.Aggregate(Vector3.zero,(a,b)=>a+b)/pawns.Length;
            var cell=pawns.SelectMany(p=>new[]{p+Vector3.right*2.5f,p+Vector3.left*2.5f,p+Vector3.forward*2.5f,p+Vector3.back*2.5f})
                .Where(c=>pawns.All(p=>(p-c).sqrMagnitude>1))
                .OrderBy(c=>(c-centre).sqrMagnitude).First();
            cell.y=0;
            var field=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Dungeon Camera");
            var vfx=Object.FindFirstObjectByType<HqVfx>();
            var spring=HqSpringProp.Build(null,cell,field);
            yield return new WaitForSecondsRealtime(.6f);
            Directory.CreateDirectory(Gallery);
            var frame=ReadTexture(rt);Overlay(frame);SaveTexture(frame,"board_spring_idle");Object.Destroy(frame);
            var companions=Fixture.Baseline().Units.Where(u=>!u.Enemy).Select(u=>GameObject.Find("Fixture "+u.Id)).Where(g=>g!=null).Select(g=>g.transform.position).ToArray();
            spring.Drink(vfx,companions);
            for(var i=0;i<6;i++){yield return new WaitForSecondsRealtime(.2f);var f=ReadTexture(rt);SaveTexture(f,$"board_spring_drink_{i}");Object.Destroy(f);}
            Camera.main.targetTexture=null;Object.Destroy(rt);Object.Destroy(spring.gameObject);
        }

        // ---- Helpers ----------------------------------------------------------------------------------

        private static Camera GalleryCamera(Vector3 centre,float size)
        {
            // The board's own view: orthographic, pitched 75 degrees, no yaw (MobileG1View.CameraPitch).
            var camera=new GameObject("Gallery camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=size;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.86f,.84f,.78f);
            camera.transform.position=centre+new Vector3(0,Mathf.Sin(75*Mathf.Deg2Rad)*21,-Mathf.Cos(75*Mathf.Deg2Rad)*21);camera.transform.LookAt(centre);
            camera.nearClipPlane=.1f;camera.farClipPlane=60;return camera;
        }

        private static Color[] Downsample(Texture2D source,int size)
        {
            var result=new Color[size*size];const int k=4;
            for(var y=0;y<size;y++)for(var x=0;x<size;x++)
            {
                var sum=Color.clear;
                for(var j=0;j<k;j++)for(var i=0;i<k;i++)
                {
                    var c=source.GetPixelBilinear((x+(i+.5f)/k)/size,(y+(j+.5f)/k)/size,0);
                    sum+=new Color(c.r*c.a,c.g*c.a,c.b*c.a,c.a);
                }
                sum/=k*k;result[y*size+x]=sum.a>1e-4f?new Color(sum.r/sum.a,sum.g/sum.a,sum.b/sum.a,sum.a):Color.clear;
            }
            return result;
        }

        private static Color[] Read(RenderTexture rt)
        {
            var t=ReadTexture(rt);var pixels=t.GetPixels();Object.Destroy(t);return pixels;
        }
        private static Texture2D ReadTexture(RenderTexture rt)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();
            RenderTexture.active=previous;return t;
        }
        private static void Save(RenderTexture rt,string name){var t=ReadTexture(rt);SaveTexture(t,name);Object.Destroy(t);}
        private static void SaveTexture(Texture2D t,string name)
        {Directory.CreateDirectory(Gallery);File.WriteAllBytes(Path.Combine(Gallery,name+".jpg"),t.EncodeToJPG(92));}

        private static void Paint(Texture2D sheet,Color[] pixels,int size,int left,int bottom)
        {
            for(var y=0;y<size;y++)for(var x=0;x<size;x++)
            {
                var c=pixels[y*size+x];if(c.a<=0)continue;
                var under=sheet.GetPixel(left+x,bottom+y);sheet.SetPixel(left+x,bottom+y,Color.Lerp(under,new Color(c.r,c.g,c.b,1),c.a));
            }
        }
        private static void Fill(Texture2D sheet,int left,int bottom,int width,int height,Color colour)
        {for(var y=0;y<height;y++)for(var x=0;x<width;x++)sheet.SetPixel(left+x,bottom+y,colour);}

        // Rows, top to bottom: 3x (132 px) on a cream card, 2x (88 px) on a dark card, 1x (44 px) on board stone,
        // and the 44 px silhouettes in plain black, blown up 2x so their shapes can be compared side by side.
        private static void SaveIconSheet()
        {
            const int column=150;var ids=HqRelicArt.Ids;var width=column*ids.Length;var rows=new[]{170,124,76,124};var height=rows.Sum();
            var sheet=new Texture2D(width,height,TextureFormat.RGB24,false);
            var top=height;
            var backgrounds=new[]{new Color(.97f,.94f,.86f),new Color(.13f,.16f,.27f),new Color(.80f,.80f,.76f),Color.white};
            for(var row=0;row<rows.Length;row++)
            {
                top-=rows[row];Fill(sheet,0,top,width,rows[row],backgrounds[row]);
                for(var i=0;i<ids.Length;i++)
                {
                    var size=row==0?132:row==1?88:row==2?44:88;
                    Color[] pixels;
                    if(row==0)pixels=HqRelicArt.Icon(ids[i]).GetPixels();
                    else if(row<3)pixels=Downsample(HqRelicArt.Icon(ids[i]),size);
                    else
                    {
                        var small=Downsample(HqRelicArt.Icon(ids[i]),44);pixels=new Color[88*88];
                        for(var y=0;y<88;y++)for(var x=0;x<88;x++)pixels[y*88+x]=small[(y/2)*44+x/2].a>.5f?Color.black:Color.clear;
                    }
                    Paint(sheet,pixels,size,i*column+(column-size)/2,top+(rows[row]-size)/2);
                }
            }
            sheet.Apply();SaveTexture(sheet,"relic_icons");Object.Destroy(sheet);
        }

        // Two relics offered on the board (04 §4 shows a pair over the board): drawn at 44 pt (88 px at 2x) above the spring.
        private static void Overlay(Texture2D frame)
        {
            // The field camera renders into the UI, so the pair sits at a fixed spot over the upper middle of the board.
            var at=new Vector2(390,1000);
            var pair=new[]{"watch_lantern","thorn_charm"};
            for(var i=0;i<2;i++)
            {
                var pixels=Downsample(HqRelicArt.Icon(pair[i]),88);
                Paint(frame,pixels,88,Mathf.Clamp((int)at.x-100+i*112,0,780-88),Mathf.Clamp((int)at.y,0,1688-88));
            }
            frame.Apply();
        }
    }
}
