// A danger plate on the floor (HqDangerTiles): the plate texture's alpha times a colour, alpha-blended, drawn after the
// opaque board and before the characters' transparent quads so feet, foot HP and faces always sit on top of it.
Shader "RogueTactics/DangerCell" {
 Properties {
  _MainTex("Plate", 2D) = "white" {}
  _Color("Colour", Color) = (1,.62,.2,.6)
 }
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST;half4 _Color;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
   half4 frag(V i):SV_Target{half4 t=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);return half4(_Color.rgb,t.a*_Color.a);}
   ENDHLSL
  }
 }
}
