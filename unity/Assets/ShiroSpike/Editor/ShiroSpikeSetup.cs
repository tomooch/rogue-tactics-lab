using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Isolated asset experiment. Does not modify build settings or gameplay.
public static class ShiroSpikeSetup
{
    public const string Root = "Assets/ShiroSpike";
    public const string ScenePath = Root + "/ShiroFrontWalk.unity";
    public const string ClipPath = Root + "/ShiroFrontWalk.anim";
    public const string AtlasPath = Root + "/Art/shiro_walk_front_atlas.png";
    public const float Fps = 8;

    [MenuItem("Tools/Shiro Spike/Import and configure")]
    public static void Configure()
    {
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        if (importer == null) throw new InvalidOperationException("Shiro atlas missing");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlas.width != 1536 || atlas.height != 1024) throw new InvalidOperationException("Atlas was resized");
        Directory.CreateDirectory(Root + "/Slices");
        AssetDatabase.Refresh();
        var sprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            // PNG row order is top-down; Unity texture rects are bottom-up.
            var rect = new Rect((i % 3) * 512, (1 - i / 3) * 512, 512, 512);
            var sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 42f / 512f), 100,
                0, SpriteMeshType.FullRect);
            sprite.name = $"shiro_walk_front_{i + 1:00}";
            string path = $"{Root}/Slices/{sprite.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing == null) AssetDatabase.CreateAsset(sprite, path);
            else { EditorUtility.CopySerialized(sprite, existing); UnityEngine.Object.DestroyImmediate(sprite); }
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ClipPath); }
        clip.name = "ShiroFrontWalk"; clip.legacy = true; clip.frameRate = Fps; clip.wrapMode = WrapMode.Loop;
        var keys = new ObjectReferenceKeyframe[7];
        for (int i = 0; i < 7; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / Fps, value = sprites[i % 6] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Shiro Spike Camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 6;
        camera.transform.position = new Vector3(0, 2.6f, -10);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.92f, 0.95f, 0.99f);
        var actor = new GameObject("Shiro / provisional front walk");
        var renderer = actor.AddComponent<SpriteRenderer>(); renderer.sprite = sprites[0];
        renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        string materialPath = Root + "/ShiroUnlit.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = renderer.sharedMaterial; AssetDatabase.CreateAsset(material, materialPath); }
        else { UnityEngine.Object.DestroyImmediate(renderer.sharedMaterial); renderer.sharedMaterial = material; }
        var animation = actor.AddComponent<Animation>();
        animation.AddClip(clip, "ShiroFrontWalk"); animation.clip = clip;
        animation.playAutomatically = true; animation.wrapMode = WrapMode.Loop;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("SHIRO_SPIKE_CONFIGURED six native Sprite.Create slices; 8fps loop; no build settings change");
    }
}
