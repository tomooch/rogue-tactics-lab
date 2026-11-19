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
    // Going down a floor without covering the screen (specs/07 7): the stair opens, light takes the party, the world turns
    // into floor 2's look, the party lands. Checks the beats, that nothing measured moves, and that no frame is mostly
    // white or black. RT_GFX_CAPTURE=1 saves the frames under Logs/hq/private/descent (never committed).
    public sealed class GfxFloorDescentTests
    {
        [UnityTest]
        public IEnumerator DescendingTurnsTheWorldIntoTheNextFloorWithoutCoveringTheScreen()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;
            var capture=System.Environment.GetEnvironmentVariable("RT_GFX_CAPTURE")!=null;var directory=Path.GetFullPath("Logs/hq/private/descent");
            if(capture){if(Directory.Exists(directory))Directory.Delete(directory,true);Directory.CreateDirectory(directory);}
            var vfx=Object.FindFirstObjectByType<HqVfx>();Assert.That(vfx,Is.Not.Null);
            var ids=new[]{"A","B","C","D","X","Y"};var feet=ids.ToDictionary(id=>id,id=>GameObject.Find("Fixture "+id).transform.position);
            var cell=feet.Values.SelectMany((p,i)=>feet.Values.Skip(i+1).Select(q=>Vector3.Distance(p,q))).Min();
            var party=new[]{"A","B","C","D"}.Select(id=>feet[id]).ToArray();
            var stair=party.Aggregate(Vector3.zero,(s,p)=>s+p)/party.Length+Vector3.back*cell*1.5f;
            try
            {
                var start=Time.realtimeSinceStartup;
                var beats=HqFloorDescent.Play(vfx,view.FieldCamera,2,stair,party);
                Assert.That(beats.Taken.Length,Is.EqualTo(4));
                Assert.That(beats.Taken.Max(),Is.LessThan(beats.Switch),"Everyone is in the light before the world changes");
                Assert.That(beats.Switch,Is.LessThan(beats.Land));Assert.That(beats.End,Is.LessThanOrEqualTo(2f),"Within two seconds");
                var shots=new[]{.2f,.6f,1.0f,1.4f,beats.End+.1f};var shot=0;
                while(Time.realtimeSinceStartup-start<beats.End+.2f)
                {
                    yield return null;
                    var t=Time.realtimeSinceStartup-start;
                    if(shot<shots.Length && t>=shots[shot])
                    {
                        var image=Read(rt);var px=image.GetPixels32();Object.Destroy(image);
                        var extreme=px.Count(c=>{var l=(c.r*299+c.g*587+c.b*114)/1000;return l>245||l<12;})/(float)px.Length;
                        Assert.That(extreme,Is.LessThan(.6f),$"Frame at {t:0.00}s is mostly white or black ({extreme:P0})");
                        if(capture){var keep=Read(rt);File.WriteAllBytes(Path.Combine(directory,$"{shot}_{t:0.00}s.jpg"),keep.EncodeToJPG(90));Object.Destroy(keep);}
                        shot++;
                    }
                }
                var look=Object.FindFirstObjectByType<HqFloorLook>();
                Assert.That(look,Is.Not.Null);Assert.That(look.Floor,Is.EqualTo(2),"The world now shows floor 2");
                foreach(var id in ids)Assert.That(GameObject.Find("Fixture "+id).transform.position,Is.EqualTo(feet[id]),id+" never moves");
            }
            finally{HqFloorLook.Apply(1,view.FieldCamera);Camera.main.targetTexture=null;Object.Destroy(rt);}
        }

        private static Texture2D Read(RenderTexture rt)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            RenderTexture.active=previous;return image;
        }
    }
}
