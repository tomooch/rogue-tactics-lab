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
    // The end-of-floor reward (design/specs/04_RELICS_AND_GEAR.md 4) drawn before Core offers relics: two pedestals on
    // the first board, the floor-2 pair, then the player keeps one; a second altar is declined. RT_GFX_CAPTURE=1 saves
    // frames under Logs/hq/private/reward (never committed).
    public sealed class GfxRewardAltarTests
    {
        [UnityTest]
        public IEnumerator TwoEqualPedestalsOfferRelicsAndTheChosenOneJoinsTheParty()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var rt=new RenderTexture(780,1688,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,68,780,1526),780,1688);yield return null;
            var capture=System.Environment.GetEnvironmentVariable("RT_GFX_CAPTURE")!=null;var directory=Path.GetFullPath("Logs/hq/private/reward");
            if(capture){if(Directory.Exists(directory))Directory.Delete(directory,true);Directory.CreateDirectory(directory);}
            var vfx=Object.FindFirstObjectByType<HqVfx>();
            var ids=new[]{"A","B","C","D","X","Y"};var feet=ids.ToDictionary(id=>id,id=>GameObject.Find("Fixture "+id).transform.position);
            var cell=feet.Values.SelectMany((p,i)=>feet.Values.Skip(i+1).Select(q=>Vector3.Distance(p,q))).Min();
            var partyCentre=new[]{"A","B","C","D"}.Aggregate(Vector3.zero,(s,id)=>s+feet[id])/4;
            // Two free cells in front of the party (towards the camera), one cell apart.
            var front=partyCentre+Vector3.back*cell*1.6f;var left=front+Vector3.left*cell;var right=front+Vector3.right*cell;
            HqRewardAltar altar=null,second=null;
            try
            {
                altar=HqRewardAltar.Build(view.FieldCamera.transform.parent,left,right,"watch_lantern","thorn_charm",view.FieldCamera,vfx);
                Assert.That(altar.Pedestals[0].localScale,Is.EqualTo(altar.Pedestals[1].localScale),"No pedestal is grander");
                Assert.That(altar.Icons[0].GetComponent<SpriteRenderer>().bounds.size.x,Is.EqualTo(altar.Icons[1].GetComponent<SpriteRenderer>().bounds.size.x).Within(.05f),"Both icons are the same size");
                yield return new WaitForSecondsRealtime(.8f);
                if(capture)Save(rt,Path.Combine(directory,"1_offer.jpg"));
                var arrives=altar.Choose(0,partyCentre);Assert.That(arrives,Is.EqualTo(HqRewardAltar.ChooseSeconds));
                Assert.That(altar.Choose(1,partyCentre),Is.Zero,"A second answer is ignored");
                yield return new WaitForSecondsRealtime(arrives*.5f);
                if(capture)Save(rt,Path.Combine(directory,"2_choose.jpg"));
                yield return new WaitForSecondsRealtime(arrives*.5f+.15f);
                if(capture)Save(rt,Path.Combine(directory,"3_kept.jpg"));
                Assert.That(altar.Icons[0].GetComponent<SpriteRenderer>().enabled,Is.False,"The kept relic has joined the party");
                Assert.That(altar.Icons[1].GetComponent<SpriteRenderer>().color.a,Is.LessThan(.05f),"The other relic has faded");
                foreach(var id in ids)Assert.That(GameObject.Find("Fixture "+id).transform.position,Is.EqualTo(feet[id]),id+" never moves");

                second=HqRewardAltar.Build(view.FieldCamera.transform.parent,left,right,"dawn_dew","pilgrim_bell",view.FieldCamera,vfx);
                yield return null;var fade=second.Decline();
                yield return new WaitForSecondsRealtime(fade+.1f);
                Assert.That(second.Icons.All(i=>i.GetComponent<SpriteRenderer>().color.a<.05f),Is.True,"Declining fades both");
            }
            finally{if(altar!=null)Object.Destroy(altar.gameObject);if(second!=null)Object.Destroy(second.gameObject);Camera.main.targetTexture=null;Object.Destroy(rt);}
        }

        private static void Save(RenderTexture rt,string path)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            RenderTexture.active=previous;File.WriteAllBytes(path,image.EncodeToJPG(90));Object.Destroy(image);
        }
    }
}
