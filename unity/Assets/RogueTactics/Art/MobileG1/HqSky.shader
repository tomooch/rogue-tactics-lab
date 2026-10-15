Shader "RogueTactics/HqSky"
{
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque"}
  Pass
  {
   Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
   float noise(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float smoothNoise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(noise(i),noise(i+float2(1,0)),f.x),lerp(noise(i+float2(0,1)),noise(i+1),f.x),f.y);}
   half4 frag(V i):SV_Target
   {
    float2 p=i.uv*9;float cloud=smoothNoise(p)*.6+smoothNoise(p*2)*.25+smoothNoise(p*4)*.15;
    float h=saturate(i.uv.y);float3 sky=lerp(float3(.78,.90,.96),float3(.30,.64,.85),h);
    sky=lerp(sky,float3(.96,.98,1),smoothstep(.46,.72,cloud)*.65);
    float glow=exp(-length(i.uv-float2(.27,.72))*8);sky+=float3(.16,.12,.04)*glow;
    return half4(sky,1);
   }
   ENDHLSL
  }
 }
}
