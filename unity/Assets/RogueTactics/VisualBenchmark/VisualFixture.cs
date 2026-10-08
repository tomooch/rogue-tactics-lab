using UnityEngine;
using UnityEngine.Rendering;
namespace RogueTactics.VisualBenchmark
{
    // Authored, static presentation only. No Core, input, AI, rules or skill execution.
    public sealed class VisualFixture : MonoBehaviour
    {
        public const string ScenePath = "Assets/RogueTactics/Scenes/VisualBenchmark.unity";
        public const int Width = 390, Height = 844;
        public RenderPipelineAsset renderProfile;
        RenderPipelineAsset previousGraphics, previousQuality;
        bool applied;
        int previousAntiAliasing;
        public void ApplyRenderProfile()
        {
            if(applied || !renderProfile) return;
            previousAntiAliasing=QualitySettings.antiAliasing;
            previousGraphics=GraphicsSettings.defaultRenderPipeline;
            previousQuality=QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline=renderProfile;
            QualitySettings.renderPipeline=renderProfile;
            applied=true;
        }
        public void RestoreRenderProfile()
        {
            if(!applied) return;
            GraphicsSettings.defaultRenderPipeline=previousGraphics;
            QualitySettings.renderPipeline=previousQuality;
            QualitySettings.antiAliasing=previousAntiAliasing;
            applied=false;
        }
        void Awake() => ApplyRenderProfile();
        void OnDestroy() => RestoreRenderProfile();
        public Transform[] companions;
        public Transform[] monsters;
        public Transform arrowOrigin, arrowDestination;
        public RectTransform fieldWindow;
    }
}
