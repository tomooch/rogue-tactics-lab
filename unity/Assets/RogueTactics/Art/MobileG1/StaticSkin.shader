Shader "RogueTactics/StaticSkin" {
 Properties { _MainTex("Approved private skin", 2D) = "white" {}
 _Color("Event light", Color) = (1,1,1,1)
 _Pose("Display pose: mirror, lean, squash, lift", Vector) = (0,0,0,0)
 _Tilt("Display fall tilt", Float) = 0 }
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
   float4 _Pose; float _Tilt;
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings vert(Attributes a) {
    // Display-only pose of the single approved image, pivoting at its feet (quad y = -.5).
    // Vertex offsets leave the renderer bounds, and so every projected rect, unchanged.
    float3 p=a.positionOS.xyz;float h=p.y+.5;
    p.x*=1+_Pose.z;h*=1-_Pose.z;p.x+=_Pose.y*h;
    // Rotate in the 3:4 quad's true proportions so a fall does not shear the art.
    float2 q=float2(p.x*.75,h);float c=cos(_Tilt),s=sin(_Tilt);q=float2(q.x*c-q.y*s,q.x*s+q.y*c);
    p.x=q.x/.75;p.y=q.y-.5+_Pose.w;
    Varyings o; o.positionCS=TransformObjectToHClip(p);o.uv=float2(lerp(a.uv.x,1-a.uv.x,_Pose.x),a.uv.y);return o; }
   half4 frag(Varyings i):SV_Target {return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*_Color;}
   ENDHLSL
  }
 }
}
