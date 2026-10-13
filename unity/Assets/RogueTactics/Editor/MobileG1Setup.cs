using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using RogueTactics.Presentation;

namespace RogueTactics.Editor
{
    public static class MobileG1Setup
    {
        public const string ScenePath="Assets/RogueTactics/Scenes/MobileG1.unity";
        private const string Art="Assets/RogueTactics/Art/MobileG1/";
        [MenuItem("Tools/Rogue Tactics/Create Mobile G1")]
        public static void Configure()
        {
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Mobile HUD Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(0,0,-10);
            var view=new GameObject("Mobile G1 Experiment").AddComponent<MobileG1View>();
            view.JapaneseFont=AssetDatabase.LoadAssetAtPath<Font>(Art+"NotoSansJP.ttf");
            view.StoneTemplate=Material("Stone.mat","Universal Render Pipeline/Lit");
            view.SpriteTemplate=Material("StaticSkin.mat","RogueTactics/StaticSkin");
            var skins=new[]{"shiro","momo","garu","myaru"};
            for(var i=0;i<skins.Length;i++)
            {
                var path=Art+"Private/"+skins[i]+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer!=null){importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
                view.CharacterArt[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.defaultScreenWidth=390;PlayerSettings.defaultScreenHeight=844;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.tomooch.roguetacticslab");PlayerSettings.bundleVersion="0.1.0";PlayerSettings.iOS.buildNumber="1";
            PlayerSettings.iOS.appleDeveloperTeamID="9HXM297A8U";PlayerSettings.iOS.appleEnableAutomaticSigning=false;
            PlayerSettings.iOS.targetOSVersionString="16.0";
            EditorSceneManager.SaveScene(scene,ScenePath);
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            AssetDatabase.SaveAssets();Debug.Log("MOBILE_G1_CONFIGURED "+Application.unityVersion+" approved_skins="+view.HasAllArt);
        }
        private static Material Material(string file,string shaderName)
        {
            var path=Art+file;var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){var shader=Shader.Find(shaderName);if(shader==null)throw new Exception("Missing shader "+shaderName);m=new Material(shader);AssetDatabase.CreateAsset(m,path);}return m;
        }
        public static void ExportIos()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.GetFullPath("../build/ios"),target=BuildTarget.iOS,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("iOS export failed: "+result.summary.result);
            #if UNITY_IOS
            MobileIosSigning.ConfigureExport(Path.GetFullPath("../build/ios"));
#endif
            Debug.Log("MOBILE_IOS_EXPORT_COMPLETE");
        }
        public static void ExportIosSimulator()
        {
            var sdk=PlayerSettings.iOS.sdkVersion;
            var architecture=PlayerSettings.iOS.simulatorSdkArchitecture;
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
                PlayerSettings.iOS.sdkVersion=iOSSdkVersion.SimulatorSDK;
                PlayerSettings.iOS.simulatorSdkArchitecture=AppleMobileArchitectureSimulator.ARM64;
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.GetFullPath("../build/ios-simulator"),target=BuildTarget.iOS,options=BuildOptions.Development});
                if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Simulator export failed");
                Debug.Log("MOBILE_IOS_SIM_EXPORT_COMPLETE");
            }
            finally{PlayerSettings.iOS.sdkVersion=sdk;PlayerSettings.iOS.simulatorSdkArchitecture=architecture;}
        }
        public static void BuildMacPreview()
        {
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.GetFullPath("../build/MobileG1.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Preview build failed");
        }
    }
}

#if UNITY_IOS

namespace RogueTactics.Editor
{
    using System.Text.RegularExpressions;
    using UnityEditor.iOS.Xcode;
    public static class MobileIosSigning
    {
        // Explicit game-only build metadata. Credentials/private keys never enter this project.
        public static void ConfigureExport(string directory)
        {
            var certificate=Environment.GetEnvironmentVariable("ROGUE_IOS_SIGNING_SHA");
            var profile=Environment.GetEnvironmentVariable("ROGUE_IOS_PROFILE");
            if(string.IsNullOrEmpty(certificate) && string.IsNullOrEmpty(profile))return;
            if(!Regex.IsMatch(certificate??"","^[0-9A-Fa-f]{40}$") || string.IsNullOrWhiteSpace(profile))
                throw new InvalidOperationException("Supply both game signing fingerprint and profile name");
            var path=PBXProject.GetPBXProjectPath(directory);var project=new PBXProject();project.ReadFromFile(path);
            var app=project.GetUnityMainTargetGuid();var framework=project.GetUnityFrameworkTargetGuid();
            foreach(var target in new[]{app,framework})
            {
                project.SetBuildProperty(target,"CODE_SIGN_STYLE","Manual");
                project.SetBuildProperty(target,"DEVELOPMENT_TEAM","9HXM297A8U");
                project.SetBuildProperty(target,"CODE_SIGN_IDENTITY",certificate);
                // Unity emits this SDK-specific Developer identity; it overrides the generic setting.
                project.SetBuildProperty(target,"CODE_SIGN_IDENTITY[sdk=iphoneos*]",certificate);
            }
            project.SetBuildProperty(app,"PROVISIONING_PROFILE_SPECIFIER",profile);
            project.SetBuildProperty(app,"PRODUCT_BUNDLE_IDENTIFIER","com.tomooch.roguetacticslab");
            // Embedded frameworks are signed with this identity, without an app provisioning profile.
            project.SetBuildProperty(framework,"PROVISIONING_PROFILE_SPECIFIER","");
            project.WriteToFile(path);
        }
    }
}
#endif
