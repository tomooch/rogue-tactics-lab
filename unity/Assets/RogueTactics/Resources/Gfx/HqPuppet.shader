// One approved image, moved as a soft puppet. Bones are 2D affine transforms in "image space"
// (x = quad.x * aspect, y = quad.y + .5, so the feet sit near y = 0 and the art is 1 unit tall).
// The mesh itself never changes, so renderer bounds and every projected rect stay as authored.
// The texture's own RGB is only ever multiplied by light or blended toward a flash colour.
Shader "RogueTactics/Puppet" {
 Properties {
  _MainTex("Approved private skin", 2D) = "white" {}
  _Color("Event light", Color) = (1,1,1,1)
  _Flash("Hit flash rgb, amount", Color) = (1,1,1,0)
  _Outline("Outline rgb, strength", Color) = (.10,.13,.24,.85)
  _OutlineWidth("Outline width (texels)", Float) = 9
  _Aura("Aura rgb, strength", Color) = (1,.86,.45,0)
  _FootTint("Ground bounce at the feet", Color) = (.80,.86,.96,1)
  _Dissolve("Dissolve", Range(0,1)) = 0
  _Mirror("Mirror", Float) = 0
  _Aspect("Width / height", Float) = .75
 }
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
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   float4 _MainTex_TexelSize;
   half4 _Color, _Flash, _Outline, _Aura, _FootTint;
   float _OutlineWidth, _Dissolve, _Mirror, _Aspect;
   float4 _BoneX[16];
   float4 _BoneY[16];
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 bones:TEXCOORD1; float4 weights:TEXCOORD2; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

   Varyings vert(Attributes a) {
    float3 h = float3(a.positionOS.x * _Aspect, a.positionOS.y + .5, 1);
    float2 q = 0;
    for (int k = 0; k < 4; k++) {
     int i = (int)(a.bones[k] + .5);
     q += a.weights[k] * float2(dot(_BoneX[i].xyz, h), dot(_BoneY[i].xyz, h));
    }
    q.x *= 1 - 2 * _Mirror;
    Varyings o;
    o.positionCS = TransformObjectToHClip(float3(q.x / _Aspect, q.y - .5, a.positionOS.z));
    o.uv = a.uv;
    return o;
   }

   float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
   float noise(float2 p) {
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
   }

   half4 frag(Varyings i):SV_Target {
    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    // Silhouette ring: the widest neighbouring alpha that this texel does not already cover.
    float2 o = _MainTex_TexelSize.xy * _OutlineWidth;
    half ring = 0;
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( o.x, 0)).a);
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-o.x, 0)).a);
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0,  o.y)).a);
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, -o.y)).a);
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + o * .7071).a);
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - o * .7071).a);
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(o.x, -o.y) * .7071).a);
    ring = max(ring, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-o.x, o.y) * .7071).a);
    // A wider, softer ring carries the acting aura.
    float2 w = o * 2.4;
    half glow = 0;
    glow += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( w.x, 0)).a;
    glow += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-w.x, 0)).a;
    glow += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0,  w.y)).a;
    glow += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, -w.y)).a;
    glow = saturate(glow * .25) * _Aura.a;

    half3 lit = c.rgb * lerp(_FootTint.rgb, half3(1, 1, 1), smoothstep(.04, .30, i.uv.y)) * _Color.rgb;
    lit = lerp(lit, _Flash.rgb, _Flash.a);
    half edge = ring * _Outline.a * (1 - c.a);
    half halo = glow * (1 - max(c.a, edge));
    half alpha = c.a + edge + halo;
    half3 rgb = (lit * c.a + lerp(_Outline.rgb, _Aura.rgb, _Aura.a * .6) * edge + _Aura.rgb * halo) / max(alpha, .0001);

    // Death: the figure scatters upward into light. Burn line glows before the texel disappears.
    float n = noise(i.uv * float2(34, 46)) * .7 + (1 - i.uv.y) * .3;
    float gone = smoothstep(_Dissolve * 1.15 - .08, _Dissolve * 1.15, n);
    float burn = (1 - smoothstep(_Dissolve * 1.15, _Dissolve * 1.15 + .10, n)) * step(.001, _Dissolve);
    rgb = lerp(rgb, half3(1, .96, .82), burn * .85);
    alpha *= lerp(1, gone, step(.001, _Dissolve));
    return half4(rgb, saturate(alpha) * _Color.a);
   }
   ENDHLSL
  }
 }
}
