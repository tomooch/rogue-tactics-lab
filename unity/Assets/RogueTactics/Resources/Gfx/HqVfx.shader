// Every effect sprite is drawn here from maths, not textures: one material, one draw, no art to license.
// uv0.xy = quad position in -1..1, uv0.z = shape, uv0.w = shape parameter (0..1, animated per particle)
// uv1.x  = 1 for light (additive), 0 for matter (alpha); uv1.y = per-particle seed
// Output is premultiplied so light and matter share a single pass and keep their spawn order.
Shader "RogueTactics/Vfx" {
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent+120" "RenderPipeline"="UniversalPipeline" }
  Pass {
   Blend One OneMinusSrcAlpha
   ZWrite Off
   ZTest Always
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float4 uv:TEXCOORD0; float2 mode:TEXCOORD1; };
   struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float4 uv:TEXCOORD0; float2 mode:TEXCOORD1; };
   Varyings vert(Attributes a) { Varyings o; o.positionCS = TransformObjectToHClip(a.positionOS.xyz); o.color = a.color; o.uv = a.uv; o.mode = a.mode; return o; }

   float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
   float noise(float2 p) {
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
   }
   float band(float x, float centre, float halfWidth, float soft) { return 1 - smoothstep(halfWidth, halfWidth + soft, abs(x - centre)); }

   // Returns coverage in x and "white heat" in y.
   float2 shape(float2 p, float id, float k, float seed) {
    float r = length(p);
    float ang = atan2(p.y, p.x);
    if (id < .5) {                      // 0 soft glow
     float g = exp(-r * r * 7.0) * (1 - smoothstep(.75, 1, r));
     return float2(g, g * g * g);
    }
    if (id < 1.5) {                     // 1 ring; k thins it as it grows
     float w = lerp(.16, .025, k);
     float g = band(r, .86 - w, w, .05);
     return float2(g, g * .5);
    }
    if (id < 2.5) {                     // 2 four-point sparkle
     float d = pow(abs(p.x), .55) + pow(abs(p.y), .55);
     float g = saturate(1.05 - d); g = g * g * 2.2 + exp(-r * r * 30) * .9;
     return float2(saturate(g), saturate(g * g));
    }
    if (id < 3.5) {                     // 3 crescent slash, revealed along the arc by k
     float a01 = (ang / 3.14159265 + 1) * .5;            // 0..1 around the circle
     float sweep = saturate((a01 - .18) / .64);           // the visible arc
     float open = step(.18, a01) * step(a01, .82);
     float taper = sin(sweep * 3.14159265);
     float w = .17 * taper;
     float g = band(r, .80 - w * .6, w, .03) * open;
     float lead = saturate(k * 1.5);                       // head of the cut
     float tail = saturate(k * 1.5 - .55);                 // fading start
     g *= smoothstep(lead, lead - .12, sweep) * smoothstep(tail - .01, tail + .25, sweep + .001);
     float core = band(r, .80 - w * .25, w * .35, .02) * open;
     return float2(g, g * core);
    }
    if (id < 4.5) {                     // 4 streak: bright head at +x, thin tail
     float t = saturate((p.x + 1) * .5);
     float w = .04 + .30 * t * t;
     float g = saturate(1 - abs(p.y) / w) * smoothstep(0, .12, t) * (1 - smoothstep(.9, 1, t));
     return float2(g * g, g * g * t);
    }
    if (id < 5.5) {                     // 5 petal / leaf
     float w = .46 * (1 - p.x * p.x);
     float g = 1 - smoothstep(w - .06, w, abs(p.y + .12 * sin(p.x * 3.14)));
     g *= step(abs(p.x), 1);
     float vein = 1 - smoothstep(0, .05, abs(p.y));
     return float2(g, vein * g * .25);
    }
    if (id < 6.5) {                     // 6 plus: mending light
     float arm = max(1 - smoothstep(.20, .26, abs(p.x)), 0) * (1 - smoothstep(.74, .80, abs(p.y)));
     float arm2 = max(1 - smoothstep(.20, .26, abs(p.y)), 0) * (1 - smoothstep(.74, .80, abs(p.x)));
     float g = max(arm, arm2);
     return float2(saturate(g + exp(-r * r * 3) * .35), g * .7);
    }
    if (id < 7.5) {                     // 7 puff: a soft lump of dust or cloud, eaten away by k
     float n = noise(p * 2.6 + seed * 17) * .55 + noise(p * 5.3 - seed * 9) * .25;
     float g = 1 - smoothstep(.40, .95, r + n * .5 + k * .55);
     return float2(g, 0);
    }
    if (id < 8.5) {                     // 8 shard: a facet of crystal
     float d = abs(p.x) * 2.1 + abs(p.y);
     float g = 1 - smoothstep(.86, .92, d);
     float facet = step(0, p.x * p.y) * .35 + .25;
     return float2(g, g * facet);
    }
    if (id < 9.5) {                     // 9 ward: a faceted dome that rings where it was struck
     float inside = 1 - smoothstep(.93, .97, r);
     float rim = pow(saturate(r), 3.5);
     float2 q = p * 5.2; q.x += .5 * floor(q.y);
     float2 cell = abs(frac(q) - .5);
     float lines = smoothstep(.40, .47, max(cell.x, cell.y));
     float ripple = band(r, k * 1.05, .07, .08) * (1 - k);
     float g = inside * saturate(rim * .9 + lines * (.22 + rim * .5) + ripple * .9 + .07);
     return float2(g, g * (rim * .5 + ripple));
    }
    if (id < 10.5) {                    // 10 rune circle on the ground, rotating with k
     float a2 = ang + k * 6.2831853;
     float outer = band(r, .90, .03, .02) + band(r, .66, .018, .02);
     float ticks = band(r, .78, .07, .01) * step(.55, frac(a2 * 12 / 6.2831853));
     float star = band(abs(cos(a2 * 2.5)) * r, .42, .025, .02) * step(r, .62);
     float g = saturate(outer + ticks * .8 + star * .8);
     return float2(g, g * .35);
    }
    if (id < 11.5) {                    // 11 pillar: a column of light, bright at its base
     float t = saturate((p.y + 1) * .5);
     float w = lerp(.55, .28, t);
     float g = saturate(1 - abs(p.x) / w); g = g * g * (1 - t) * smoothstep(0, .06, t);
     return float2(g, g * g);
    }
    if (id < 12.5) {                    // 12 veil: a soft dark halo that lets the light in front of it read on pale stone
     float g = smoothstep(.04, .45, r) * (1 - smoothstep(.5, 1, r));
     return float2(g, 0);
    }
    {                                   // 13 burst: impact lines thrown outward; k pushes them away from the centre
     float a01 = ang / 6.2831853 + .5;
     float c = floor(a01 * 46), f = frac(a01 * 46);
     float rnd = hash(float2(c, seed * 91 + 3)), rnd2 = hash(float2(c + 7, seed * 53 + 1));
     float lines = step(.42, rnd) * smoothstep(.26, .46, .5 - abs(f - .5));
     float inner = lerp(.14, .60, k) + rnd2 * .16, outer = .58 + .42 * rnd;
     float g = lines * smoothstep(inner, inner + .10, r) * (1 - smoothstep(outer - .22, outer, r));
     return float2(g, g * .7);
    }
   }

   half4 frag(Varyings i):SV_Target {
    float2 s = shape(i.uv.xy, i.uv.z, i.uv.w, i.mode.y);
    float a = saturate(s.x) * i.color.a;
    half3 rgb = lerp(i.color.rgb, half3(1, 1, 1), saturate(s.y) * .5);
    // Light adds (alpha 0 keeps what is behind); matter covers.
    // The ruins are sunlit and pale, so pure addition burns out at once: light also veils a little of what is behind it.
    return half4(rgb * a * lerp(1, .92, i.mode.x), a * lerp(1, .36, i.mode.x));
   }
   ENDHLSL
  }
 }
}
