using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using RogueTactics.Presentation.Gfx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RogueTactics.Tests.PlayMode
{
    // Character, animation and effect presentation. These check that the display layer is alive and that it
    // stays display-only. Set RT_GFX_CAPTURE=<n> to also save every n-th frame under Logs/hq/private/gfx for review.
    public class GfxPresentationTests
    {
        private static readonly System.Collections.Generic.Dictionary<string,Renderer> skins=new System.Collections.Generic.Dictionary<string,Renderer>();
        // Found once while everyone stands: a fallen actor is inactive and GameObject.Find no longer sees it.
        private static Renderer Skin(string id)=>skins.TryGetValue(id,out var r) && r!=null?r:skins[id]=Find(id);
        private static Renderer Find(string id)=>GameObject.Find("Fixture "+id).GetComponentsInChildren<Renderer>().First(r=>r.name=="Private approved static skin" || r.name=="Fixture slime");

        [UnityTest]
        public IEnumerator BattleIsAnimatedByPuppetsSpiritsAndEffectsWithoutMovingAnythingMeasured()
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var every=int.TryParse(System.Environment.GetEnvironmentVariable("RT_GFX_CAPTURE"),out var n)?n:0;
            var scale=every>0?2:1;var rt=new RenderTexture(390*scale,844*scale,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34*scale,390*scale,763*scale),390*scale,844*scale);yield return null;

            skins.Clear();
            var allies=Fixture.Baseline().Units.Where(u=>!u.Enemy).Select(u=>u.Id).ToArray();
            var enemies=Fixture.Baseline().Units.Where(u=>u.Enemy).Select(u=>u.Id).ToArray();
            foreach(var id in allies)
            {
                var skin=Skin(id);
                Assert.That(skin.sharedMaterial.shader.name,Is.EqualTo("RogueTactics/Puppet"),id+" is drawn as a puppet of its approved image");
                var mesh=skin.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
                Assert.That(vertices.Length,Is.GreaterThan(100),"The puppet has enough vertices to bend");
                Assert.That(vertices.Min(v=>v.x),Is.EqualTo(-.5f).Within(1e-5f));Assert.That(vertices.Max(v=>v.x),Is.EqualTo(.5f).Within(1e-5f));
                Assert.That(vertices.Min(v=>v.y),Is.EqualTo(-.5f).Within(1e-5f));Assert.That(vertices.Max(v=>v.y),Is.EqualTo(.5f).Within(1e-5f));
            }
            foreach(var id in enemies)Assert.That(GameObject.Find("Fixture "+id).GetComponent<HqCreatureAnim>(),Is.Not.Null,id+" is an animated spirit");

            foreach(var id in enemies)Skin(id);
            var restMesh=allies.ToDictionary(id=>id,id=>Skin(id).GetComponent<MeshFilter>().sharedMesh.vertices);
            var restScale=allies.Concat(enemies).ToDictionary(id=>id,id=>Skin(id).transform.localScale);
            var bones=Shader.PropertyToID("_BoneX");var idle=Skin("A").sharedMaterial.GetVectorArray(bones);
            yield return new WaitForSecondsRealtime(.4f);
            var later=Skin("A").sharedMaterial.GetVectorArray(bones);
            Assert.That(Enumerable.Range(0,idle.Length).Any(i=>(idle[i]-later[i]).sqrMagnitude>1e-9f),Is.True,"A standing companion breathes");

            view.Button("Attack").onClick.Invoke();view.Button("Start").onClick.Invoke();
            var frame=0;var peakEffects=0;var sawFlash=false;var deadline=Time.realtimeSinceStartup+90;var directory=Path.GetFullPath("Logs/hq/private/gfx");
            if(every>0){if(Directory.Exists(directory))Directory.Delete(directory,true);Directory.CreateDirectory(directory);}
            // When capturing, keep going a little past the end so the victory (and any ally getting up) is recorded too.
            var tail=-1f;
            while((!view.DisplayedEvents.Any(e=>e.Kind==EventKind.End) || (every>0 && (tail<0?(tail=Time.realtimeSinceStartup+2.5f)>0:Time.realtimeSinceStartup<tail))) && Time.realtimeSinceStartup<deadline)
            {
                yield return null;frame++;peakEffects=Mathf.Max(peakEffects,view.EffectCount);
                foreach(var id in allies)if(Skin(id).sharedMaterial.GetColor("_Flash").a>.2f)sawFlash=true;
                if(every>0 && frame%every==0)
                {
                    var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=previous;
                    var last=view.DisplayedEvents.LastOrDefault();
                    File.WriteAllBytes(Path.Combine(directory,$"f{frame:0000}_e{view.DisplayedEvents.Count:00}_{(last==null?"none":last.Kind+"_"+last.Actor+last.Target)}.jpg"),image.EncodeToJPG(88));Object.Destroy(image);
                }
            }
            Assert.That(view.DisplayedEvents.Any(e=>e.Kind==EventKind.End),Is.True,"The battle is shown to its end");
            Assert.That(peakEffects,Is.GreaterThan(20),"Blows, spells and falls draw effects");
            if(view.DisplayedEvents.Any(e=>(e.Kind==EventKind.Damage || e.Kind==EventKind.Counter) && allies.Contains(e.Target)))
                Assert.That(sawFlash,Is.True,"A struck companion flashes");
            // Display only: the measured meshes and transforms are exactly what they were before the battle.
            foreach(var id in allies)Assert.That(Skin(id).GetComponent<MeshFilter>().sharedMesh.vertices,Is.EqualTo(restMesh[id]),id+" rest mesh is untouched");
            foreach(var id in restScale.Keys)Assert.That(Skin(id).transform.localScale,Is.EqualTo(restScale[id]),id+" transform scale is untouched");
            Camera.main.targetTexture=null;Object.Destroy(rt);
        }
    }
}
