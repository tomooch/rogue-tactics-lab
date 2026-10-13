using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using RogueTactics.Presentation;

namespace RogueTactics.Editor
{
    public static class G1Setup
    {
        public const string ScenePath="Assets/RogueTactics/Scenes/G1Battle.unity";
        [MenuItem("Tools/Rogue Tactics/Create G1 Battle")]
        public static void Configure()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("G1 UI Camera").AddComponent<Camera>(); camera.tag="MainCamera";
            camera.orthographic=true; camera.orthographicSize=5; camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;
            var view=new GameObject("G1 Experiment").AddComponent<G1View>();
            const string materialPath="Assets/RogueTactics/Art/Placeholder/G1Placeholder.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,materialPath); }
            view.PlaceholderTemplate=material;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(Stage0Setup.ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("G1_CONFIGURED "+Application.unityVersion);
        }
        public static void BuildMac()
        {
            PlayerSettings.defaultScreenWidth=390; PlayerSettings.defaultScreenHeight=844;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var path=Path.GetFullPath("../build/G1Battle.app");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName=path,target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded) throw new Exception("G1 Mac build failed: "+report.summary.result);
            Debug.Log("G1_BUILD_COMPLETE "+path);
        }
    }
}
