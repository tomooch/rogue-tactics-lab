// The floor spring's water and its light, drawn from maths (no textures). Display only: HqSpringProp drives
// _Clock (pausable), _Glow (1 fresh, low once drunk) and _Surge (0..1 while the party drinks).
// _Mode 0: water surface on a disc, uv -1..1. Opaque.
// _Mode 1: a soft column of light with motes rising through it, on a camera-facing quad. Premultiplied.
// _Mode 2: a pool of light on the floor around the basin, on a ground quad. Premultiplied.
Shader "RogueTactics/Spring" {
 Properties {
  _Mode("Mode", Float) = 0
  _Deep("Deep water", Color) = (.10,.52,.70,1)
  _Shallow("Shallow water", Color) = (.46,.92,.92,1)
  _Tint("Light colour", Color) = (.62,1,.92,1)
  _Clock("Clock", Float) = 0
  _Glow("Glow", Range(0,1)) = 1
  _Surge("Surge", Range(0,1)) = 0
  [Enum(UnityEngine.Rendering.BlendMode)] _Src("Src", Float) = 1
  [Enum(UnityEngine.Rendering.BlendMode)] _Dst("Dst", Float) = 0
  [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1
 }
 SubShader {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Blend [_Src] [_Dst]
   ZWrite [_ZWrite]
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Deep, _Shallow, _Tint;
   float _Mode, _Clock, _Glow, _Surge;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings vert(Attributes a) { Varyings o; o.positionCS = TransformObjectToHClip(a.positionOS.xyz); o.uv = a.uv; return o; }

   float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

   half4 water(float2 uv) {
    float t = _Clock;
    float r = length(uv);
    // Deep in the middle, shallow and bright toward the stone lip.
    // The deepest water sits a little toward the far side, so it reads as depth and not as a bullseye.
    half3 c = lerp(_Deep.rgb, _Shallow.rgb, smoothstep(-.45, 1.05, length(uv - float2(.05, .22))));
    // Sky caught on the far side of the water (the camera looks down from the near side).
    c += half3(.16, .18, .16) * smoothstep(-.2, 1, uv.y) * (1 - smoothstep(.75, 1, r));
    // Light dancing on the bottom: two slow interfering waves pushed into thin bright lines.
    float2 q = uv * 4.6;
    float w = sin(q.x + t * .9 + sin(q.y * 1.3 + t * .7)) + sin(q.y * 1.15 - t * .8 + sin(q.x * 1.7 - t * .55));
    float caustic = smoothstep(1.15, 1.75, w);
    c = lerp(c, half3(.86, 1, .97), caustic * .45 * (.45 + .55 * _Glow));
    // One slow ring breathing outward from the middle, as if the spring wells up.
    float ring = frac(t * .32);
    float rd = (r - ring * .95) / .04;
    float band = exp(-rd * rd) * (1 - ring) * smoothstep(.35, .6, ring);
    c += half3(.40, .46, .42) * band * (.35 + .65 * _Glow);
    // A few glints that wink on and off.
    float2 cell = floor(uv * 3.2 + 7);
    float2 f = frac(uv * 3.2 + 7) - .5;
    float h = hash(cell);
    float2 off = (float2(hash(cell + 3.1), hash(cell + 5.7)) - .5) * .5;
    float star = smoothstep(.10, 0, abs(f.x - off.x)) * smoothstep(.30, 0, abs(f.y - off.y))
               + smoothstep(.10, 0, abs(f.y - off.y)) * smoothstep(.30, 0, abs(f.x - off.x));
    float twinkle = pow(saturate(sin(t * 1.7 + h * 6.283)), 14) * step(.45, h);
    c += star * twinkle * .9 * _Glow;
    // The lip: a thin light line where the water meets the stone.
    c += half3(.6, .7, .66) * smoothstep(.84, .98, r) * .55;
    // Welling up when the party drinks: the whole surface lifts toward light.
    c = lerp(c, half3(.88, 1, .96), _Surge * .55 * (1 - r * .4));
    // Spent: calmer and a little deeper.
    c = lerp(c * half3(.82, .88, .92), c, .35 + .65 * _Glow);
    return half4(c, 1);
   }

   half4 column(float2 uv) {
    float t = _Clock;
    // uv.y -1 (water) .. 1 (top). The column rises taller and brighter in a surge.
    float y = uv.y * .5 + .5;
    float height = lerp(.42, 1.0, _Surge);
    float width = lerp(.32, .44, _Surge);
    float core = exp(-uv.x * uv.x / (width * width)) * (1 - smoothstep(height * .3, height, y)) * smoothstep(0, .28, y);
    float strength = .26 * _Glow + 1.15 * _Surge;
    float a = core * strength;
    // Motes drifting up through the light.
    for (int i = 0; i < 7; i++) {
     float h = hash(float2(i, 3.7));
     float my = frac(t * (.10 + h * .09) + h * 3.1);
     float mx = sin(t * (.6 + h) + h * 9) * .32 * (1 - my * .5) + (h - .5) * .3;
     float2 d = float2((uv.x - mx) * 1.0, (y - my * lerp(.8, 1.0, _Surge)) * 2.2);
     float dotm = exp(-dot(d, d) / .0016) * sin(my * 3.14159);
     a += dotm * (.55 * _Glow + .8 * _Surge);
    }
    a = saturate(a);
    half3 c = lerp(_Tint.rgb, half3(1, 1, 1), saturate(a * .3));
    // Light only partly covers what is behind it, so it tints the pale stone instead of vanishing into it.
    return half4(c * a, a * .42);
   }

   half4 pool(float2 uv) {
    float r = length(uv);
    float rd = (r - .58) / .26;
    float a = exp(-rd * rd) * (.22 * _Glow + .45 * _Surge);
    a *= .85 + .15 * sin(_Clock * 1.4);
    return half4(_Tint.rgb * a, a * .42);
   }

   half4 frag(Varyings i):SV_Target {
    if (_Mode < .5) return water(i.uv);
    if (_Mode < 1.5) return column(i.uv);
    return pool(i.uv);
   }
   ENDHLSL
  }
 }
}
