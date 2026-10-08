using RogueTactics.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RogueTactics.Editor
{
    public static class PortraitComparisonSetup
    {
        public const string ScenePath = "Assets/RogueTactics/Scenes/PortraitComparison.unity";
        [MenuItem("Tools/Rogue Tactics/Create Portrait Comparison")]
        public static void Configure()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Fictional UX Comparison").AddComponent<PortraitComparisonView>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("PORTRAIT_COMPARISON_CONFIGURED " + Application.unityVersion);
        }
    }
}
