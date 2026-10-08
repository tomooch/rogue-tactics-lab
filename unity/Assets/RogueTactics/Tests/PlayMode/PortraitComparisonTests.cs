using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RogueTactics.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace RogueTactics.Tests
{
    public class PortraitComparisonTests
    {
        static void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = button.GetComponent<RectTransform>();
            var point = RectTransformUtility.WorldToScreenPoint(Camera.main, rect.TransformPoint(rect.rect.center));
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = point };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), button.name + " hit target");
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button), button.name + " unobstructed");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        [UnityTest]
        public IEnumerator SameFixtureSwitchesReasonsAndExplainsWithoutExecuting()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/RogueTactics/Scenes/PortraitComparison.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            Assert.Ignore("Editor presentation fixture only");
#endif
            yield return null;
            var view = Object.FindFirstObjectByType<PortraitComparisonView>();
            Assert.That(view, Is.Not.Null);
            var target = new RenderTexture(390, 844, 24);
            var camera = Camera.main; camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            yield return null;
            // Both comparison images have identical selection, fixture, geometry and viewport.
            CheckText(camera);
            Capture(target, "variant-a.png");
            Click(view.VariantBButton);
            yield return null;
            Assert.That(view.ShowAllReasons, Is.False);
            for (var i = 0; i < 4; i++) Assert.That(view.ReasonVisible(i), Is.EqualTo(i == 0));
            CheckText(camera);
            Capture(target, "variant-b.png");
            for (var selected = 0; selected < 4; selected++)
            {
                Click(view.CompanionButton(selected));
                Assert.That(view.SelectedCompanion, Is.EqualTo(selected));
                for (var i = 0; i < 4; i++) Assert.That(view.ReasonVisible(i), Is.EqualTo(i == selected));
                for (var i = 0; i < 4; i++)
                {
                    var action = GameObject.Find("Action" + i).GetComponent<Text>();
                    Assert.That(action.text, Does.Contain(PortraitComparisonView.Actions[i]));
                }
            }
            Click(view.CorrectionButton);
            yield return null; // Newly enabled graphics need a rendered frame before raycast.
            Assert.That(view.ExplanationOpen, Is.True);
            Assert.That(GameObject.Find("ExplanationTitle").GetComponent<Text>().text, Does.Contain("D"));
            CheckText(camera);
            Click(view.CloseButton);
            Assert.That(view.ExplanationOpen, Is.False);
            Click(view.EntrustButton);
            yield return null;
            Assert.That(view.ExplanationOpen, Is.True);
            CheckText(camera);
            Click(view.CloseButton);
            Assert.That(GameObject.Find("CorrectionBudget").GetComponent<Text>().text, Does.Contain("残り1回"));
            Click(view.VariantAButton);
            for (var i = 0; i < 4; i++) Assert.That(view.ReasonVisible(i), Is.True);
            LogAssert.NoUnexpectedReceived();
            camera.targetTexture = null;
            Object.Destroy(target);
        }
        static void CheckText(Camera camera)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name + " width");
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name + " height");
                var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var point = camera.WorldToViewportPoint(corner);
                    Assert.That(point.x, Is.InRange(-0.001f, 1.001f), text.name);
                    Assert.That(point.y, Is.InRange(-0.001f, 1.001f), text.name);
                }
            }
        }
        static void Capture(RenderTexture target, string name)
        {
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(390, 844, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 390, 844), 0, 0); image.Apply();
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../evidence/issue-7", name));
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(image);
        }
    }
}
