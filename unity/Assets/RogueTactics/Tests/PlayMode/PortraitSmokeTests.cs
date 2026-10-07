using System.Collections;
using System.IO;
using NUnit.Framework;
using RogueTactics.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RogueTactics.Tests
{
    public class PortraitSmokeTests
    {
        [UnityTest]
        public IEnumerator Stage0RendersAtPortraitReferenceWithoutHorizontalClipping()
        {
            yield return SceneManager.LoadSceneAsync("Stage0");
            yield return null;
            Assert.That(Object.FindFirstObjectByType<Stage0View>(), Is.Not.Null);
            var camera = Camera.main;
            Assert.That(camera.orthographic, Is.True);
            var target = new RenderTexture(390, 844, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            yield return null;
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                var corners = new Vector3[4];
                text.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var viewport = camera.WorldToViewportPoint(corner);
                    Assert.That(viewport.x, Is.InRange(-0.001f, 1.001f), text.name);
                }
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
            }
            yield return null;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(390, 844, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 390, 844), 0, 0); image.Apply();
            var evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/stage0-portrait.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(evidence));
            File.WriteAllBytes(evidence, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.Destroy(image); Object.Destroy(target);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
