using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
            view.HqWorld=true;view.CameraPitch=35.264f;view.CameraYaw=-45;view.EventMotion=true;
            view.JapaneseFont=AssetDatabase.LoadAssetAtPath<Font>(Art+"NotoSansJP.ttf");
            var authoredPaths=Directory.GetFiles("Assets/RogueTactics/Art/HqRuins", "*.obj").OrderBy(p=>p).ToArray();
            foreach(var path in authoredPaths)
            {var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.globalScale=1;importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();}
            view.EnvironmentMeshes=authoredPaths.Select(AuthoredRuinsImport.Import).ToArray();
            if(view.EnvironmentMeshes.Length==0)throw new Exception("Author sky ruins mesh assets before Configure");
            view.AmbientOcclusion=ConfigureContactLighting();
            view.SkyTemplate=Material("Sky.mat","RogueTactics/HqSky");
            view.RuinsTemplate=Material("AuthoredRuins.mat","RogueTactics/AuthoredRuins");
            view.StoneTemplate=Material("Stone.mat","Universal Render Pipeline/Lit");
            view.SpriteTemplate=Material("StaticSkin.mat","RogueTactics/StaticSkin");
            var skins=new[]{"shiro","momo","garu","myaru"};
            for(var i=0;i<skins.Length;i++)
            {
                var path=Art+"Private/"+skins[i]+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer!=null){importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
                view.CharacterArt[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.defaultScreenWidth=390;PlayerSettings.defaultScreenHeight=844;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.tomooch.roguetacticslab");PlayerSettings.bundleVersion="0.1.0";PlayerSettings.iOS.buildNumber="5";
            PlayerSettings.iOS.appleDeveloperTeamID="9HXM297A8U";PlayerSettings.iOS.appleEnableAutomaticSigning=false;
            PlayerSettings.iOS.targetOSVersionString="16.0";
            EditorSceneManager.SaveScene(scene,ScenePath);
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            AssetDatabase.SaveAssets();Debug.Log("MOBILE_G1_CONFIGURED "+Application.unityVersion+" approved_skins="+view.HasAllArt);
        }
        private static ScriptableRendererFeature ConfigureContactLighting()
        {
            var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
            var feature=data.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if(feature==null){feature=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();feature.name="HQ mobile contact light";AssetDatabase.AddObjectToAsset(feature,data);data.rendererFeatures.Add(feature);}
            var serialized=new SerializedObject(feature);var settings=serialized.FindProperty("m_Settings");
            settings.FindPropertyRelative("Intensity").floatValue=.8f;settings.FindPropertyRelative("Radius").floatValue=.45f;
            settings.FindPropertyRelative("Downsample").boolValue=true;settings.FindPropertyRelative("AOMethod").enumValueIndex=1;
            settings.FindPropertyRelative("Source").enumValueIndex=1;settings.FindPropertyRelative("Samples").enumValueIndex=2;
            settings.FindPropertyRelative("BlurQuality").enumValueIndex=1;serialized.ApplyModifiedPropertiesWithoutUndo();
            feature.SetActive(true);data.SetDirty();EditorUtility.SetDirty(feature);EditorUtility.SetDirty(data);return feature;
        }
        private static Material Material(string file,string shaderName)
        {
            var path=Art+file;var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){var shader=Shader.Find(shaderName);if(shader==null)throw new Exception("Missing shader "+shaderName);m=new Material(shader);AssetDatabase.CreateAsset(m,path);}return m;
        }
        public static void ExportIos()
        {
#if UNITY_IOS
            MobileIosSigning.PrepareIosIcons();
#endif
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
#if UNITY_IOS
            MobileIosSigning.PrepareIosIcons();
#endif
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
        // A public, procedural test-build emblem; contains no private character raster.
        public static void PrepareIosIcons()
        {
            const string path="Assets/RogueTactics/Art/MobileG1/InternalIcon.png";
            if(!File.Exists(path))
            {
                var image=new Texture2D(1024,1024,TextureFormat.RGB24,false);var pixels=new Color32[1024*1024];
                for(var y=0;y<1024;y++)for(var x=0;x<1024;x++)
                {
                    var u=(x-511.5f)/512;var v=(y-511.5f)/512;
                    var glow=Mathf.Max(0,1-Mathf.Sqrt(u*u+v*v));
                    var color=Color.Lerp(new Color(.025f,.065f,.09f),new Color(.09f,.22f,.24f),glow);
                    var diamond=Mathf.Abs(u)/.77f+Mathf.Abs(v+.05f)/.44f;
                    if(diamond<1)color=Color.Lerp(new Color(.22f,.34f,.34f),new Color(.42f,.53f,.48f),(v+.49f)/.88f);
                    if(diamond>.94f && diamond<1)color=new Color(.72f,.65f,.40f);
                    for(var i=0;i<4;i++)
                    {
                        var cx=(i%2==0?-.23f:.23f);var cy=(i<2?-.09f:.16f);
                        if((u-cx)*(u-cx)+(v-cy)*(v-cy)<.013f)
                            color=new[]{new Color(.54f,.72f,.94f),new Color(.96f,.65f,.74f),new Color(.88f,.75f,.43f),new Color(.47f,.81f,.65f)}[i];
                    }
                    var spark=Mathf.Abs(u)/.10f+Mathf.Abs(v-.59f)/.16f;
                    if(spark<1)color=new Color(1,.84f,.43f);
                    pixels[y*1024+x]=color;
                }
                image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaSource=TextureImporterAlphaSource.None;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            foreach(var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS,kind);
                foreach(var icon in icons)icon.SetTextures(Enumerable.Repeat(texture,icon.minLayerCount).ToArray());
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS,kind,icons);
            }
            AssetDatabase.SaveAssets();Debug.Log("MOBILE_IOS_ICONS_COMPLETE opaque1024");
        }
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
