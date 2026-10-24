using System;
using System.IO;
using System.Linq;
using RogueTactics.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RogueTactics.Editor
{
    public static class HqFloorSetup
    {
        public const string ScenePath="Assets/RogueTactics/Scenes/HqExploration.unity";
        [MenuItem("Tools/Rogue Tactics/Create Connected Exploration Floor")]
        public static void Configure()
        {
            AssetDatabase.Refresh();
            var baseline="Assets/RogueTactics/Scenes/MobileG1.unity";var bytes=File.ReadAllBytes(baseline);
            var scene=EditorSceneManager.OpenScene(baseline,OpenSceneMode.Single);
            var view=UnityEngine.Object.FindFirstObjectByType<MobileG1View>();view.ExplorationEnabled=true;
            EditorSceneManager.SaveScene(scene,ScenePath);
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            if(!bytes.SequenceEqual(File.ReadAllBytes(baseline)))throw new InvalidOperationException("Exploration setup changed the frozen G1 scene");
            AssetDatabase.SaveAssets();Debug.Log("HQ_EXPLORATION_CONFIGURED "+Application.unityVersion);
        }
        public static void BuildMacPreview()
        {
            var result=UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.GetFullPath("../build/Exploration.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Exploration preview build failed");
        }
        public static void ExportIosSimulator()
        {
            var sdk=PlayerSettings.iOS.sdkVersion;var architecture=PlayerSettings.iOS.simulatorSdkArchitecture;
            var backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS);
            try
            {
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
                PlayerSettings.iOS.sdkVersion=iOSSdkVersion.SimulatorSDK;
                PlayerSettings.iOS.simulatorSdkArchitecture=AppleMobileArchitectureSimulator.ARM64;
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.GetFullPath("../build/ios-floor-simulator"),target=BuildTarget.iOS,options=BuildOptions.Development});
                if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Exploration Simulator export failed");
                Debug.Log("HQ_FLOOR_IOS_SIM_EXPORT_COMPLETE");
            }
            finally
            {PlayerSettings.iOS.sdkVersion=sdk;PlayerSettings.iOS.simulatorSdkArchitecture=architecture;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS,backend);}
        }
    }
}
