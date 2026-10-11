using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ShiroWalkPlaybackTests
{
    [UnityTest]
    public IEnumerator SixSlicesActuallyPlayAndRenderAtPortraitSize()
    {
        // Editor-only assets are inspected by an EditMode coroutine that enters actual Play Mode.
        yield return new EnterPlayMode();
        var scene = EditorSceneManager.LoadSceneAsyncInPlayMode(ShiroSpikeSetup.ScenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return scene; yield return null;
        var renderer = Object.FindFirstObjectByType<SpriteRenderer>();
        var animation = renderer.GetComponent<Animation>();
        Assert.That(animation.IsPlaying("ShiroFrontWalk"), Is.True);
        var seen = new HashSet<string>();
        float deadline = Time.realtimeSinceStartup + 2;
        while (Time.realtimeSinceStartup < deadline)
        {
            Assert.That(renderer.sprite, Is.Not.Null);
            seen.Add(renderer.sprite.name);
            yield return null;
        }
        Assert.That(seen.Count, Is.EqualTo(6), "Actual animation playback must reach all six sprites");
        animation.Stop();
        var camera = Camera.main;
        var target = new RenderTexture(390, 844, 24);
        camera.targetTexture = target;
        var image = new Texture2D(390, 844, TextureFormat.RGB24, false);
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/shiro-spike"));
        Directory.CreateDirectory(output);
        for (int i = 0; i < 6; i++)
        {
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ShiroSpikeSetup.Root}/Slices/shiro_walk_front_{i + 1:00}.asset");
            Assert.That(renderer.sprite.rect, Is.EqualTo(new Rect((i % 3) * 512, (1 - i / 3) * 512, 512, 512)));
            Assert.That(renderer.sprite.pivot.x, Is.EqualTo(256).Within(0.01));
            Assert.That(renderer.sprite.pivot.y, Is.EqualTo(42).Within(0.01));
            foreach (var dark in new[] { false, true })
            {
                camera.backgroundColor = dark ? new Color(0.055f, 0.065f, 0.09f) : new Color(0.92f, 0.95f, 0.99f);
                yield return null;
                yield return null;
                var previous = RenderTexture.active; RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 390, 844), 0, 0); image.Apply();
                RenderTexture.active = previous;
                var pixels = image.GetPixels32();
                var bg = pixels[0]; int foreground = 0;
                foreach (var p in pixels)
                    if (System.Math.Abs(p.r - bg.r) + System.Math.Abs(p.g - bg.g) + System.Math.Abs(p.b - bg.b) > 40) foreground++;
                Assert.That(foreground, Is.GreaterThan(5000), "Actual render should contain visible character");
                File.WriteAllBytes(Path.Combine(output, $"{(dark ? "dark" : "light")}_{i + 1:00}.png"), image.EncodeToPNG());
            }
        }
        camera.targetTexture = null;
        Object.Destroy(image); Object.Destroy(target);
        LogAssert.NoUnexpectedReceived();
        Debug.Log("SHIRO_SPIKE_PLAYBACK_PASS 6 distinct sprites observed in real playback; 12 portrait renders");
        yield return new ExitPlayMode();
    }
}
