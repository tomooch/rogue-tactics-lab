using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Presentation;
using RogueTactics.Presentation.Gfx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RogueTactics.Tests.PlayMode
{
    // The guardian's announced area and the player's "retreat" (design/specs/02_ENEMIES.md 6) are drawn before Core has
    // them. This lays danger plates under two companions and two empty cells on the first board, steps one companion out
    // with HqVfx.Retreat, lands the blow (Strike) and checks the plates come and go without hiding the party.
    // RT_GFX_CAPTURE=1 saves the frames under Logs/hq/private/danger (never committed).
    public sealed class GfxDangerRetreatTests
    {

        [UnityTest]
        public IEnumerator AnnouncedCellsPulseThenFlashAwayAndRetreatLeavesTheArea()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var rt=new RenderTexture(780,1688,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,68,780,1526),780,1688);yield return null;
            var capture=System.Environment.GetEnvironmentVariable("RT_GFX_CAPTURE")!=null;var directory=Path.GetFullPath("Logs/hq/private/danger");
            if(capture){if(Directory.Exists(directory))Directory.Delete(directory,true);Directory.CreateDirectory(directory);}
            var vfx=Object.FindFirstObjectByType<HqVfx>();Assert.That(vfx,Is.Not.Null,"The board has its effects");
            var a=GameObject.Find("Fixture A").transform.position;var c=GameObject.Find("Fixture C").transform.position;
            // One cell on screen: the nearest two pawns stand on neighbouring cells (the board root may be scaled).
            var feet=new[]{"A","B","C","D","X","Y"}.Select(id=>GameObject.Find("Fixture "+id).transform.position).ToArray();
            var Cell=feet.SelectMany((p,i)=>feet.Skip(i+1).Select(q=>Vector3.Distance(p,q))).Min();
            var host=new GameObject("Danger test").AddComponent<HqDangerTiles>();
            try
            {
                var cells=new[]{a,c,a+Vector3.forward*Cell,c+Vector3.back*Cell};
                host.Show(cells,Cell);
                Assert.That(host.Count,Is.EqualTo(4));
                foreach(Transform plate in host.transform)
                {
                    Assert.That(plate.position.y-a.y,Is.LessThanOrEqualTo(HqDangerTiles.Lift+.01f),"A plate lies on the floor");
                    Assert.That(plate.localScale.x,Is.LessThan(Cell),"A plate stays inside its cell");
                }
                yield return new WaitForSecondsRealtime(.6f);
                if(capture)Save(rt,Path.Combine(directory,"1_announced.jpg"));
                vfx.Retreat(a,a+Vector3.left*Cell,HqVfx.Azure);
                yield return new WaitForSecondsRealtime(.22f);
                if(capture)Save(rt,Path.Combine(directory,"2_retreat.jpg"));
                yield return new WaitForSecondsRealtime(.4f);
                host.Strike();yield return new WaitForSecondsRealtime(.06f);
                if(capture)Save(rt,Path.Combine(directory,"3_strike.jpg"));
                var deadline=Time.realtimeSinceStartup+2;while(host.Showing && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(host.Showing,Is.False,"The plates go once the blow has landed");
                yield return null;Assert.That(host.transform.childCount,Is.Zero);
            }
            finally{Object.Destroy(host.gameObject);Camera.main.targetTexture=null;Object.Destroy(rt);}
        }

        private static void Save(RenderTexture rt,string path)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            RenderTexture.active=previous;File.WriteAllBytes(path,image.EncodeToJPG(90));Object.Destroy(image);
        }
    }
}
