// A soft additive glow for the floor looks' props (mushroom light, lanterns, pollen, fireflies, glyphs), on a
// camera-facing quad with uv -1..1. Display only; no texture, no time: HqFloorLook moves the quads it wants to drift.
Shader "RogueTactics/FloorGlow" {
 Properties {
  _Tint("Glow colour", Color) = (1,.8,.4,1)
  _Strength("Strength", Range(0,2)) = .6
  _Core("Core sharpness", Range(1,12)) = 4
 }
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Blend One One
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint;
   half _Strength,_Core;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
   half4 frag(V i):SV_Target{
    float r2=dot(i.uv,i.uv);
    // A broad halo plus a tighter core; zero at the quad's edge so no square ever shows.
    float a=(exp(-r2*2.6)*.55+exp(-r2*_Core*2.6)*.45)*saturate(1-r2);
    return half4(_Tint.rgb*a*_Strength,0);
   }
   ENDHLSL
  }
 }
}
