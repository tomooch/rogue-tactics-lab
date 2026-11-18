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
  // Floor looks (HqFloorLook). Read only with _FLOOR_LOOK; without it the shader is the floor-1 original.
  [HideInInspector] _LookCool("Floor look: shadow-side light", Vector)=(.78,.87,1,0)
  [HideInInspector] _LookWarm("Floor look: lit-side light", Vector)=(1,.98,.91,0)
  [HideInInspector] _LookStone("Floor look: paving colour (sRGB)", Vector)=(.96,.89,.76,0)
  [HideInInspector] _LookMoss("Floor look: moss in the joints (sRGB), w more/less moss", Vector)=(.50,.66,.33,0)
  [HideInInspector] _LookClover("Floor look: clover specks (sRGB)", Vector)=(.36,.58,.22,0)
  [HideInInspector] _LookPetal("Floor look: paving blooms (sRGB), w more blooms", Vector)=(.99,.97,.92,0)
  [HideInInspector] _LookHeart("Floor look: bloom hearts (sRGB)", Vector)=(1,.82,.35,0)
  [HideInInspector] _LookDeep("Floor look: deep water (sRGB)", Vector)=(.12,.48,.62,0)
  [HideInInspector] _LookClear("Floor look: clear water (sRGB)", Vector)=(.50,.84,.88,0)
  [HideInInspector] _LookHue("Floor look: recolour toward rgb by a", Vector)=(1,1,1,0)
  [HideInInspector] _LookTint("Floor look: final multiply", Vector)=(1,1,1,0)
  [HideInInspector] _LookGlow("Floor look: final add", Vector)=(0,0,0,0)
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
   #pragma multi_compile_local_fragment _ _FLOOR_LOOK
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   half _Leaves,_Emission,_SrgbPalette,_FlowWater,_FlowSpeed,_WeatheredStone;
   half4 _LookCool,_LookWarm,_LookStone,_LookMoss,_LookClover,_LookPetal,_LookHeart,_LookDeep,_LookClear,_LookHue,_LookTint,_LookGlow;
   CBUFFER_END
   // Without _FLOOR_LOOK every macro is the original literal, so floor 1 compiles to the same code as before.
   #ifdef _FLOOR_LOOK
    #define RT_COOL _LookCool.rgb
    #define RT_WARM _LookWarm.rgb
    #define RT_STONE _LookStone.rgb
    #define RT_MOSS _LookMoss.rgb
    #define RT_CLOVER _LookClover.rgb
    #define RT_PETAL _LookPetal.rgb
    #define RT_HEART _LookHeart.rgb
    #define RT_DEEP _LookDeep.rgb
    #define RT_CLEAR _LookClear.rgb
    #define RT_MOSS_MIX saturate(.6h*(1+_LookMoss.w))
    #define RT_BLOOM_AT (.86-_LookPetal.w)
   #else
    #define RT_COOL half3(.78,.87,1)
    #define RT_WARM half3(1,.98,.91)
    #define RT_STONE half3(.96,.89,.76)
    #define RT_MOSS half3(.50,.66,.33)
    #define RT_CLOVER half3(.36,.58,.22)
    #define RT_PETAL half3(.99,.97,.92)
    #define RT_HEART half3(1,.82,.35)
    #define RT_DEEP half3(.12,.48,.62)
    #define RT_CLEAR half3(.50,.84,.88)
    #define RT_MOSS_MIX .6h
    #define RT_BLOOM_AT .86
   #endif
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 color:COLOR;float2 uv:TEXCOORD2;};
   Varyings vert(Attributes a){Varyings o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.color=a.color;o.uv=a.uv;return o;}
   half4 frag(Varyings i):SV_Target {
    Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half ndl=dot(normalize(i.normalWS),light.direction);
    ndl=lerp(saturate(ndl),abs(ndl),_Leaves);
    half shade=.48h+.52h*ndl*lerp(.62h,1.h,light.shadowAttenuation);
    half3 sky=lerp(RT_COOL,RT_WARM,ndl);
    // Mesh vertex colours have no texture sRGB decoding. Only the new ledge opts
    // into its explicitly authored display palette; approved sprite sampling is separate.
    half3 palette=lerp(i.color.rgb,SRGBToLinear(i.color.rgb),_SrgbPalette);
    #ifdef _FLOOR_LOOK
    // Recolour toward a hue while keeping each vertex's lightness (banners, leaves, lanterns).
    palette=lerp(palette,_LookHue.rgb*(dot(palette,half3(.30,.59,.11))/max(dot(_LookHue.rgb,half3(.30,.59,.11)),.05h)),_LookHue.a);
    #endif
    half3 rgb=palette*_BaseColor.rgb*(shade*sky+_Emission);
    if(_WeatheredStone>.5h){
     // Broad mineral mottling and fine grain use stable object/world position, not screen noise.
     float2 p=i.positionWS.xz;
     float cloud=sin(p.x*2.1+sin(p.y*1.7))*sin(p.y*2.4-p.x*.8);
     float grain=frac(sin(dot(floor(p*180),float2(127.1,311.7)))*43758.5453);
     rgb*=.94+cloud*.055+(grain-.5)*.035;
     // Reference grade: sunlit cream limestone, moss gathering in darker joints, sparse white blooms.
     half lum=dot(rgb,half3(.30,.59,.11));
     half3 cream=SRGBToLinear(RT_STONE);
     rgb=lerp(rgb,lum*cream*1.38h,.72h);
     // Broad warm/cool stone families so the paving does not read as one flat beige sheet.
     float tone=sin(p.x*.53+1.3)*sin(p.y*.47-.7)+.5*sin((p.x+p.y)*1.07);
     rgb*=lerp(half3(1.05,1.0,.92),half3(.90,.94,1.0),saturate(tone*.45+.5))*(.95+.06*tone);
     half up=saturate((i.normalWS.y-.6h)*2.5h);
     float2 c=floor(p*.9);float2 f=frac(p*.9);
     float patch=frac(sin(dot(c,float2(41.3,289.1)))*15731.7);
     float blend=sin(p.x*.9+sin(p.y*1.3)*1.4)*sin(p.y*.7-p.x*.4);
     // Joints are the darker authored vertex colours, independent of lighting.
     half joint=saturate((.20h-dot(palette,half3(.30,.59,.11)))*7.0h);
     half moss=up*saturate(smoothstep(.35,.85,blend)*.45+joint*.7);
     rgb=lerp(rgb,SRGBToLinear(RT_MOSS)*(shade*sky),moss*RT_MOSS_MIX);
     // Clover specks gather where moss is: small, sparse, stable in world space.
     float2 speckCell=floor(p*7.0);float2 speckAt=frac(p*7.0)-.5;
     float speck=frac(sin(dot(speckCell,float2(27.17,91.31)))*24634.63);
     half clover=up*step(.62,speck)*saturate(moss*2.2h)*(1-smoothstep(.14,.22,length(speckAt-(float2(frac(speck*5.3),frac(speck*9.1))-.5)*.5)));
     rgb=lerp(rgb,SRGBToLinear(RT_CLOVER)*(shade*sky),clover*.85h);
     float2 bloomCell=floor(p*2.2);float2 bloomAt=frac(p*2.2)-.5;
     float seed=frac(sin(dot(bloomCell,float2(12.9898,78.233)))*43758.5453);
     float2 offset=float2(frac(seed*7.1),frac(seed*13.7))-.5;
     float d=length(bloomAt-offset*.5);
     half bloom=up*step(RT_BLOOM_AT,seed)*step(.45,blend*.5+.5);
     rgb=lerp(rgb,SRGBToLinear(RT_PETAL)*(shade*sky+.15h),bloom*(1-smoothstep(.10,.14,d)));
     rgb=lerp(rgb,SRGBToLinear(RT_HEART)*(shade*sky+.15h),bloom*(1-smoothstep(.035,.055,d)));
    }
    if(_FlowWater>.5h){
     float fall=i.uv.y-_Time.y*.48*_FlowSpeed;
     float threads=sin(i.uv.x*89+sin(fall*8)*.35)*.5+.5;
     float ribbons=smoothstep(.72,1,threads)*(.35+.65*(sin(fall*23+i.uv.x*7)*.5+.5));
     // Continuous vertical ribbons, flowing downward along the curved sheet.
     half3 deep=SRGBToLinear(RT_DEEP);
     half3 clear=SRGBToLinear(RT_CLEAR);
     rgb=lerp(deep,clear,.40+ribbons*.60);
    }
    #ifdef _FLOOR_LOOK
    rgb=rgb*_LookTint.rgb+_LookGlow.rgb;
    #endif
    return half4(rgb,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
