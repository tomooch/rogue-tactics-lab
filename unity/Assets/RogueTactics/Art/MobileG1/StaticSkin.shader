Shader "RogueTactics/StaticSkin" {
 Properties { _MainTex("Approved private skin", 2D) = "white" {}
 _Color("Event light", Color) = (1,1,1,1) }
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   half4 _Color;
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings vert(Attributes a) { Varyings o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o; }
   half4 frag(Varyings i):SV_Target {return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*_Color;}
   ENDHLSL
  }
 }
}
