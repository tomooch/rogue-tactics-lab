Shader "RogueTactics/AuthoredRuins" {
 Properties {
  [MainColor] _BaseColor("Authored colour multiplier", Color)=(1,1,1,1)
  _BaseMap("Base",2D)="white"{}
  _BumpMap("Normal",2D)="bump"{}
  _Cutoff("Cutoff",Range(0,1))=.5
  _Cull("Cull", Float)=2
  _Leaves("Two-sided soft leaf light", Float)=0
  _Emission("Lantern light", Float)=0
 }
 SubShader {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
  Pass {
   Name "ForwardLit"
   Tags { "LightMode"="UniversalForward" }
   Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   half _Leaves,_Emission;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 color:COLOR;};
   Varyings vert(Attributes a){Varyings o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.color=a.color;return o;}
   half4 frag(Varyings i):SV_Target {
    Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half ndl=dot(normalize(i.normalWS),light.direction);
    ndl=lerp(saturate(ndl),abs(ndl),_Leaves);
    half shade=.48h+.52h*ndl*lerp(.62h,1.h,light.shadowAttenuation);
    half3 sky=lerp(half3(.78,.87,1),half3(1,.98,.91),ndl);
    half3 rgb=i.color.rgb*_BaseColor.rgb*(shade*sky+_Emission);
    return half4(rgb,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
