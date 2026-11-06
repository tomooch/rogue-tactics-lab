using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ShiroWalkPlaybackTests
{
    [Serializable] public class PointData
    {
        public string file;
        public float faceX, faceY, torsoX, torsoY, leftSoleX, leftSoleY, rightSoleX, rightSoleY;
    }
    [Serializable] public class ExpectedData { public string atlasSha256, baselineAtlasSha256; public PointData[] frames; }
    [Serializable] public class Transition { public string sprite; public float gameTime, normalizedTime; }
    [Serializable] public class RuntimeData
    {
        public string atlasSha256;
        public Vector3 rootPosition;
        public List<Transition> transitions = new List<Transition>();
        public List<Vector3> faceWorld = new List<Vector3>(), torsoWorld = new List<Vector3>();
    }
    static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    [UnityTest]
    public IEnumerator SixSlicesActuallyPlayAndRenderAtPortraitSize()
    {
        yield return new EnterPlayMode();
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ShiroSpikeSetup.ScenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var renderer = Object.FindFirstObjectByType<SpriteRenderer>();
        var animator = renderer.GetComponent<Animator>();
        var expected = JsonUtility.FromJson<ExpectedData>(File.ReadAllText(ShiroSpikeSetup.Root + "/Art/shiro_walk_front_manifest.json"));
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ShiroSpikeSetup.AtlasPath);
        Assert.That(Hash(ShiroSpikeSetup.AtlasPath), Is.EqualTo(expected.atlasSha256));
        Assert.That(expected.atlasSha256, Is.Not.EqualTo(expected.baselineAtlasSha256), "Must import the new B atlas");
        var rootPosition = renderer.transform.position;
        Assert.That(rootPosition, Is.EqualTo(Vector3.zero));
        Assert.That(renderer.transform.localScale, Is.EqualTo(Vector3.one));
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ShiroSpikeSetup.ClipPath);
        Assert.That(clip.frameRate, Is.EqualTo(8)); Assert.That(clip.length, Is.EqualTo(0.75f).Within(0.001));
        var keys = AnimationUtility.GetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
        Assert.That(keys.Length, Is.EqualTo(6));
        for (int i = 0; i < 6; i++)
        {
            Assert.That(keys[i].time, Is.EqualTo(i / 8f).Within(0.001));
            Assert.That(keys[i].value.name, Is.EqualTo($"shiro_walk_front_{i + 1:00}"));
        }
        var runtime = new RuntimeData { atlasSha256 = expected.atlasSha256, rootPosition = rootPosition };
        var seen = new HashSet<string>(); string previous = null; int previousIndex = -1;
        float deadline = Time.realtimeSinceStartup + 2;
        while (Time.realtimeSinceStartup < deadline)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            Assert.That(state.IsName("ShiroFrontWalk"), Is.True);
            Assert.That(state.speed, Is.EqualTo(1)); Assert.That(animator.speed, Is.EqualTo(1));
            Assert.That(renderer.transform.position, Is.EqualTo(rootPosition), "No per-frame sole/root compensation");
            Assert.That(renderer.sprite.texture, Is.EqualTo(atlas));
            string name = renderer.sprite.name; seen.Add(name);
            int index = int.Parse(name.Substring(name.Length - 2)) - 1;
            float phase = state.normalizedTime % 1f;
            int predicted = Mathf.Min(5, Mathf.FloorToInt(phase * 6));
            // Native object curves can evaluate on either side of an exact key boundary.
            if (Mathf.Abs(phase * 6 - Mathf.Round(phase * 6)) > 0.005f)
                Assert.That(index, Is.EqualTo(predicted), "Actual sprite must follow 8fps clip phase");
            if (name != previous)
            {
                if (previousIndex >= 0) Assert.That(index, Is.EqualTo((previousIndex + 1) % 6), "Actual frame order, including 6->1");
                runtime.transitions.Add(new Transition { sprite = name, gameTime = Time.time, normalizedTime = state.normalizedTime });
                previous = name; previousIndex = index;
            }
            yield return null;
        }
        Assert.That(seen.Count, Is.EqualTo(6));
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(1f));
        Assert.That(runtime.transitions.Count, Is.GreaterThan(12));
        animator.enabled = false;
        var camera = Camera.main;
        var target = new RenderTexture(390, 844, 24); camera.targetTexture = target;
        var image = new Texture2D(390, 844, TextureFormat.RGB24, false);
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/shiro-spike-B"));
        Directory.CreateDirectory(output);
        for (int i = 0; i < 6; i++)
        {
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ShiroSpikeSetup.Root}/Slices/shiro_walk_front_{i + 1:00}.asset");
            Assert.That(renderer.sprite.texture, Is.EqualTo(atlas));
            Assert.That(renderer.sprite.rect, Is.EqualTo(new Rect((i % 3) * 512, (1 - i / 3) * 512, 512, 512)));
            Assert.That(renderer.sprite.pivot, Is.EqualTo(new Vector2(256, 256)));
            Assert.That(renderer.transform.position, Is.EqualTo(rootPosition));
            var points = expected.frames[i];
            var face = renderer.transform.TransformPoint(new Vector3((points.faceX - 256) / 100, (256 - points.faceY) / 100, 0));
            var torso = renderer.transform.TransformPoint(new Vector3((points.torsoX - 256) / 100, (256 - points.torsoY) / 100, 0));
            Assert.That((face.x + torso.x) / 2, Is.EqualTo(0).Within(0.0051));
            runtime.faceWorld.Add(face); runtime.torsoWorld.Add(torso);
            foreach (var dark in new[] { false, true })
            {
                camera.backgroundColor = dark ? new Color(0.055f, 0.065f, 0.09f) : new Color(0.92f, 0.95f, 0.99f);
                yield return null; yield return null;
                var active = RenderTexture.active; RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 390, 844), 0, 0); image.Apply(); RenderTexture.active = active;
                var pixels = image.GetPixels32(); var bg = pixels[0]; int foreground = 0;
                foreach (var p in pixels) if (Math.Abs(p.r - bg.r) + Math.Abs(p.g - bg.g) + Math.Abs(p.b - bg.b) > 40) foreground++;
                Assert.That(foreground, Is.GreaterThan(5000));
                File.WriteAllBytes(Path.Combine(output, $"{(dark ? "dark" : "light")}_{i + 1:00}.png"), image.EncodeToPNG());
            }
        }
        File.WriteAllText(Path.Combine(output, "playback-observation.json"), JsonUtility.ToJson(runtime, true));
        camera.targetTexture = null; Object.Destroy(image); Object.Destroy(target);
        LogAssert.NoUnexpectedReceived();
        Debug.Log("SHIRO_B_PLAYBACK_PASS B atlas hash, 6 ordered sprites, 8fps phase, stable root, 12 portrait renders");
        yield return new ExitPlayMode();
    }
}
