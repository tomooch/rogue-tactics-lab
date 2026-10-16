Shader "RogueTactics/AuthoredRuins" {
 Properties {
  [MainColor] _BaseColor("Authored colour multiplier", Color)=(1,1,1,1)
  _BaseMap("Base",2D)="white"{}
  _BumpMap("Normal",2D)="bump"{}
  _Cutoff("Cutoff",Range(0,1))=.5
  _Cull("Cull", Float)=2
  _Leaves("Two-sided soft leaf light", Float)=0
  _Emission("Lantern light", Float)=0
  _SrgbPalette("Offline sRGB palette", Float)=0
  _FlowWater("Curved waterfall", Float)=0
  _FlowSpeed("Water motion", Float)=1
  _WeatheredStone("Authored paving surface", Float)=0
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
   half _Leaves,_Emission,_SrgbPalette,_FlowWater,_FlowSpeed,_WeatheredStone;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 color:COLOR;float2 uv:TEXCOORD2;};
   Varyings vert(Attributes a){Varyings o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.color=a.color;o.uv=a.uv;return o;}
   half4 frag(Varyings i):SV_Target {
    Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half ndl=dot(normalize(i.normalWS),light.direction);
    ndl=lerp(saturate(ndl),abs(ndl),_Leaves);
    half shade=.48h+.52h*ndl*lerp(.62h,1.h,light.shadowAttenuation);
    half3 sky=lerp(half3(.78,.87,1),half3(1,.98,.91),ndl);
    // Mesh vertex colours have no texture sRGB decoding. Only the new ledge opts
    // into its explicitly authored display palette; approved sprite sampling is separate.
    half3 palette=lerp(i.color.rgb,SRGBToLinear(i.color.rgb),_SrgbPalette);
    half3 rgb=palette*_BaseColor.rgb*(shade*sky+_Emission);
    if(_WeatheredStone>.5h){
     // Broad mineral mottling and fine grain use stable object/world position, not screen noise.
     float2 p=i.positionWS.xz;
     float cloud=sin(p.x*2.1+sin(p.y*1.7))*sin(p.y*2.4-p.x*.8);
     float grain=frac(sin(dot(floor(p*180),float2(127.1,311.7)))*43758.5453);
     rgb*=.94+cloud*.055+(grain-.5)*.035;
    }
    if(_FlowWater>.5h){
     float fall=i.uv.y-_Time.y*.48*_FlowSpeed;
     float threads=sin(i.uv.x*89+sin(fall*8)*.35)*.5+.5;
     float ribbons=smoothstep(.72,1,threads)*(.35+.65*(sin(fall*23+i.uv.x*7)*.5+.5));
     // Continuous vertical ribbons, flowing downward along the curved sheet.
     half3 deep=SRGBToLinear(half3(.12,.48,.62));
     half3 clear=SRGBToLinear(half3(.50,.84,.88));
     rgb=lerp(deep,clear,.40+ribbons*.60);
    }
    return half4(rgb,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
