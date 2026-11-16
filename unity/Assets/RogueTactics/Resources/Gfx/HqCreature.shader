// Soft toon shading for the locally authored garden spirits: two-tone light, a warm bounce from the
// stone, a glossy highlight and a bright rim so they read against the ruins at phone size.
// All motion (squash, lean, hop) is a vertex offset around the creature's foot pivot, so transforms,
// renderer bounds and projected rects never move.
Shader "RogueTactics/Creature" {
 Properties {
  _Color("Colour", Color) = (.55,.76,.29,1)
  _Shade("Shadow colour", Color) = (.30,.50,.30,1)
  _Rim("Rim light rgb, strength", Color) = (1,1,.85,.55)
  _Gloss("Gloss", Range(0,1)) = .6
  _Flat("Unlit (eyes, catchlights)", Range(0,1)) = 0
  _Flash("Hit flash rgb, amount", Color) = (1,1,1,0)
  _Dissolve("Dissolve", Range(0,1)) = 0
  [HideInInspector] _CreaturePivot("World foot pivot", Vector) = (0,0,0,0)
  [HideInInspector] _CreaturePose("Squash, lean, lift, wobble", Vector) = (0,0,0,0)
  [HideInInspector] _CreatureRight("Camera right", Vector) = (1,0,0,0)
  [HideInInspector] _CreatureDash("World offset of a pounce or a knock-back", Vector) = (0,0,0,0)
 }
 SubShader {
  Tags { "RenderType"="Opaque" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Cull Back
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color, _Shade, _Rim, _Flash;
   half _Gloss, _Flat, _Dissolve;
   // Per-creature pose; every creature owns its material instances.
   float4 _CreaturePivot;   // world foot pivot
   float4 _CreaturePose;    // x squash (+ flatter), y lean along camera right, z lift, w wobble
   float4 _CreatureRight;   // camera right in world space
   float4 _CreatureDash;    // world offset: pounce toward a target, or thrown by a blow
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float3 rest:TEXCOORD2; };

   Varyings vert(Attributes a) {
    float3 wp = TransformObjectToWorld(a.positionOS.xyz);
    float3 rel = wp - _CreaturePivot.xyz;
    float squash = _CreaturePose.x;
    rel.y *= 1 - squash;
    rel.xz *= 1 + squash * .55;
    rel += _CreatureRight.xyz * (_CreaturePose.y * rel.y + _CreaturePose.w * sin(rel.y * 5.0) * .06);
    rel.y += _CreaturePose.z;
    rel += _CreatureDash.xyz;
    Varyings o;
    o.positionWS = _CreaturePivot.xyz + rel;
    o.rest = wp - _CreaturePivot.xyz;
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.normalWS = TransformObjectToWorldNormal(a.normalOS);
    return o;
   }

   float hash(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }

   half4 frag(Varyings i):SV_Target {
    float3 n = normalize(i.normalWS);
    float3 toCamera = normalize(UNITY_MATRIX_V[2].xyz);      // orthographic: one direction for every pixel
    float3 light = normalize(float3(-.45, .80, -.40));       // key from upper left, towards the viewer
    float ndl = dot(n, light);
    float lit = smoothstep(-.08, .22, ndl);
    half3 base = lerp(_Shade.rgb, _Color.rgb, lit);
    base = lerp(base, _Color.rgb * 1.12, smoothstep(.55, .95, ndl) * .5);
    // Stone bounce from below, sky from above.
    base += half3(.10, .07, .02) * saturate(-n.y) + half3(.02, .05, .08) * saturate(n.y) * .6;
    float fres = pow(1 - saturate(dot(n, toCamera)), 2.6);
    base += _Rim.rgb * fres * _Rim.a * (.45 + .55 * saturate(n.y * .5 + .5));
    float3 hv = normalize(light + toCamera);
    float spec = smoothstep(.965, .985, dot(n, hv));
    base += spec * _Gloss;
    half3 rgb = lerp(base, _Color.rgb, _Flat);
    rgb = lerp(rgb, _Flash.rgb, _Flash.a);
    // Defeat: break into seeds from the top down.
    float nse = hash(floor(i.rest * 14)) * .65 + saturate(1 - i.rest.y) * .35;
    if (_Dissolve > .0005) clip(nse - _Dissolve * 1.1);
    rgb = lerp(rgb, half3(1, .97, .8), (1 - smoothstep(0, .09, nse - _Dissolve * 1.1)) * step(.001, _Dissolve));
    return half4(rgb, 1);
   }
   ENDHLSL
  }
 }
}
