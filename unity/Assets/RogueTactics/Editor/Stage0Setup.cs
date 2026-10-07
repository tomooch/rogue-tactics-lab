using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using RogueTactics.Presentation;

namespace RogueTactics.Editor
{
    public static class Stage0Setup
    {
        public const string ScenePath = "Assets/RogueTactics/Scenes/Stage0.unity";
        [MenuItem("Tools/Rogue Tactics/Configure Stage 0")]
        public static void Configure()
        {
            PlayerSettings.companyName = "Rogue Tactics Lab";
            PlayerSettings.productName = "Rogue Tactics Lab";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 390;
            PlayerSettings.defaultScreenHeight = 844;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.tomooch.roguetacticslab");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.tomooch.roguetacticslab");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            var mobilePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (mobilePipeline == null) throw new System.InvalidOperationException("Mobile URP asset missing");
            GraphicsSettings.defaultRenderPipeline = mobilePipeline;
            var originalQuality = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = mobilePipeline;
            }
            QualitySettings.SetQualityLevel(originalQuality, false);
            foreach (var templateAsset in new[] { "Assets/TutorialInfo", "Assets/Readme.asset", "Assets/Scenes", "Assets/InputSystem_Actions.inputactions" })
                AssetDatabase.DeleteAsset(templateAsset);
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Portrait Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.09f);
            new GameObject("Stage 0 Presentation").AddComponent<Stage0View>();
            var canvas = new GameObject("Portrait Scaffold", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.matchWidthOrHeight = 0;
            Label(canvas.transform, "Title", "ROGUE TACTICS LAB", new Vector2(0, 0.83f), new Vector2(1, 0.93f), 24);
            Label(canvas.transform, "Status", "UNITY STAGE 0", new Vector2(0, 0.73f), new Vector2(1, 0.81f), 18);
            Label(canvas.transform, "Scope", "Foundation only\nGameplay input, resolution chunk\nand intent horizon remain undecided.", new Vector2(0, 0.36f), new Vector2(1, 0.64f), 17);
            Label(canvas.transform, "Reference", "390 x 844 / PORTRAIT\nCore and Presentation separated", new Vector2(0, 0.09f), new Vector2(1, 0.23f), 16);
            Directory.CreateDirectory("Assets/RogueTactics/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("STAGE0_CONFIGURED " + Application.unityVersion);
        }
        private static void Label(Transform parent, string name, string content, Vector2 min, Vector2 max, int size)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            var rect = text.rectTransform;
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = new Vector2(20, 0); rect.offsetMax = new Vector2(-20, 0);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content; text.fontSize = size;
            text.color = new Color(0.85f, 0.91f, 0.91f);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }
    }
}
