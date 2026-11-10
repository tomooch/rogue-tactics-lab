using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace RogueTactics.VisualBenchmark.Tests
{
    public class VisualBenchmarkSmokeTests
    {
        [UnityTest]
        public IEnumerator HeroSceneRendersFourCompanionsAndReferenceMotifsWithoutClipping()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(VisualFixture.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("This opt-in fixture is not a default build scene.");
#endif
            yield return null;
            var fixture=Object.FindFirstObjectByType<VisualFixture>();
            Assert.That(fixture,Is.Not.Null);
            Assert.That(fixture.companions,Has.Length.EqualTo(4));
            Assert.That(fixture.monsters,Has.Length.EqualTo(3));
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsSortMode.None),Is.Empty,"No skill execution controls");
            var camera=Camera.main;
            Assert.That(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline,Is.SameAs(fixture.renderProfile));
            Assert.That(camera.orthographic,Is.True);
            var target=new RenderTexture(390,844,24) { antiAliasing=4 };
            camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            var canvas=Object.FindFirstObjectByType<Canvas>();
            var canvasRect=((RectTransform)canvas.transform).rect;
            LogAssert.Expect(LogType.Log,new System.Text.RegularExpressions.Regex(@"^VISUAL_RENDER_TARGET 390x844 CANVAS 390x84[34](\.\d+)?$"));
            Debug.Log($"VISUAL_RENDER_TARGET {camera.pixelWidth}x{camera.pixelHeight} CANVAS {canvasRect.width}x{canvasRect.height}");
            Assert.That(canvasRect.width,Is.EqualTo(390).Within(.1f));
            Assert.That(canvasRect.height,Is.EqualTo(844).Within(.1f));
            foreach(var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                foreach(var p in corners)
                {
                    var v=camera.WorldToViewportPoint(p);
                    Assert.That(v.x,Is.InRange(-.001f,1.001f),text.name);
                    Assert.That(v.y,Is.InRange(-.001f,1.001f),text.name);
                }
                Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width+1),text.name);
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);
            }
            foreach(var actor in fixture.companions)
            {
                var point=camera.WorldToViewportPoint(actor.position+Vector3.up*.9f);
                Assert.That(point.x,Is.InRange(.05f,.95f),actor.name);
                Assert.That(point.y,Is.InRange(.23f,.82f),actor.name);
            }
            Assert.That(GameObject.Find("Vertical floor timeline"),Is.Not.Null);
            Assert.That(GameObject.Find("Compact separate minimap"),Is.Not.Null);
            Assert.That(GameObject.Find("Arcing fictional intent • cream core"),Is.Not.Null);
            int skillCount=0;
            foreach(var image in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
                if(image.name=="Illustration only")skillCount++;
            Assert.That(skillCount,Is.EqualTo(8));
            Assert.That(fixture.fieldWindow.rect.height/844f,Is.InRange(.60f,.75f));
            yield return null;
            var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(390,844,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,390,844),0,0);texture.Apply();
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/visual-benchmark-390x844.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;
            Object.Destroy(texture);Object.Destroy(target);
            // Switching back must leave the accepted Stage0 profile intact.
            yield return SceneManager.LoadSceneAsync("Stage0");
            yield return null;
            Assert.That(Object.FindFirstObjectByType<VisualFixture>(),Is.Null);
            Assert.That(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,Is.EqualTo("Mobile_RPAsset"));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
