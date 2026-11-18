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
    // The later floors' spirits (design/specs/02_ENEMIES.md 4-6) exist as looks before their Core behaviour.
    // This stands each pair on the first board in place of the two wardens, idle and struck, so their silhouettes can be
    // judged next to the party. Set RT_GFX_CAPTURE=1 to save the frames under Logs/hq/private/gallery (never committed).
    public sealed class GfxSpiritGalleryTests
    {
        [UnityTest]
        public IEnumerator LaterFloorSpiritsStandOnTheBoardWithTheirOwnSilhouettes()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var rt=new RenderTexture(780,1688,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,68,780,1526),780,1688);yield return null;
            var capture=System.Environment.GetEnvironmentVariable("RT_GFX_CAPTURE")!=null;var directory=Path.GetFullPath("Logs/hq/private/gallery");
            if(capture){if(Directory.Exists(directory))Directory.Delete(directory,true);Directory.CreateDirectory(directory);}
            var originals=new[]{"X","Y"}.Select(id=>GameObject.Find("Fixture "+id).transform).ToArray();
            try
            {
                foreach(var pair in new[]{(SpiritKind.Cap,SpiritKind.Bat),(SpiritKind.Guardian,SpiritKind.Cap),(SpiritKind.Bat,SpiritKind.Guardian),(SpiritKind.Moss,SpiritKind.Bloom)})
                {
                    var kinds=new[]{pair.Item1,pair.Item2};var roots=new Transform[2];var anims=new HqCreatureAnim[2];
                    for(var i=0;i<2;i++)
                    {
                        var original=originals[i];original.gameObject.SetActive(false);
                        var root=new GameObject("Gallery "+kinds[i]).transform;root.SetParent(original.parent,false);root.SetPositionAndRotation(original.position,original.rotation);root.localScale=original.localScale;
                        foreach(Transform child in original)if(child.name=="Contact shadow" || child.name=="Soft foot contact"){var copy=Object.Instantiate(child.gameObject,root,false);copy.name=child.name;copy.SetActive(true);}
                        var body=HqCreature.Build(root,view.StoneTemplate,kinds[i],i==1,view.FieldCamera);
                        Assert.That(body.name,Is.EqualTo("Fixture slime"),kinds[i]+" keeps the measured body name");
                        Assert.That(root.GetComponentsInChildren<Transform>().Count(t=>t.name=="Bright spirit eye"),Is.EqualTo(2),kinds[i]+" has two eyes");
                        roots[i]=root;anims[i]=root.GetComponent<HqCreatureAnim>();Assert.That(anims[i],Is.Not.Null);
                    }
                    var names=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>()).Select(t=>t.name).ToList();
                    foreach(var kind in kinds)
                    {
                        if(kind==SpiritKind.Cap)Assert.That(names,Does.Contain("Cap dome"));
                        if(kind==SpiritKind.Bat)Assert.That(names.Count(n=>n=="Bat wing"),Is.GreaterThanOrEqualTo(2));
                        if(kind==SpiritKind.Guardian)Assert.That(names,Does.Contain("Guardian rune"));
                    }
                    for(var frame=0;frame<48;frame++)
                    {
                        if(frame==30)foreach(var a in anims){a.Play(PuppetClip.Hit,.5f,1);a.Flash(.25f);}
                        foreach(var a in anims)a.Tick(1/30f);
                        yield return null;
                        if(capture && (frame==28 || frame==33))Save(rt,Path.Combine(directory,$"{kinds[0]}_{kinds[1]}_{(frame<30?"idle":"hit")}.jpg"));
                    }
                    for(var i=0;i<2;i++){Object.Destroy(roots[i].gameObject);originals[i].gameObject.SetActive(true);}
                    yield return null;
                }
                Assert.That(new[]{SpiritKind.Moss,SpiritKind.Bloom,SpiritKind.Cap,SpiritKind.Bat,SpiritKind.Guardian}.Select(k=>HqCreature.GlyphOf(k,false)).Distinct().Count(),Is.EqualTo(5),"Every kind has its own glyph");
            }
            finally{foreach(var o in originals)if(o!=null)o.gameObject.SetActive(true);Camera.main.targetTexture=null;Object.Destroy(rt);}
        }

        private static void Save(RenderTexture rt,string path)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            RenderTexture.active=previous;File.WriteAllBytes(path,image.EncodeToJPG(90));Object.Destroy(image);
        }
    }
}
