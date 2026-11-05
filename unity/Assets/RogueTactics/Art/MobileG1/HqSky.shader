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
   // Static by design: captures stay reproducible and nothing here reads Core.
   float noise(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float smoothNoise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(noise(i),noise(i+float2(1,0)),f.x),lerp(noise(i+float2(0,1)),noise(i+1),f.x),f.y);}
   float fbm(float2 p){float v=0,a=.5;for(int k=0;k<5;k++){v+=smoothNoise(p)*a;p=p*2.03+float2(17.1,9.3);a*=.5;}return v;}
   float coverage(float d){return saturate(.5-d/max(fwidth(d),1e-5));}
   // Signed distance (in island units) of a floating ruin: rocky underside, flat crown, a few towers.
   float islandDistance(float2 q,float seed)
   {
    float ax=abs(q.x);
    float jag=smoothNoise(float2(q.x*7+seed*13,seed))*.28+smoothNoise(float2(q.x*19+seed,seed*3))*.08;
    float bottom=-(1.05+jag)*pow(saturate(1-ax*ax),.75);
    float top=.05*smoothNoise(float2(q.x*11+seed*5,2));
    float body=max(max(ax-1,bottom-q.y),q.y-top);
    float cell=(q.x+1)*3.5;float column=floor(cell);float r=noise(float2(column,seed*7.3));
    float height=r>.42?.18+r*.95:-1;
    float tower=max(max(abs(frac(cell)-.5)*(1/3.5)-.075,top-.02-q.y),q.y-(top+height));
    float notch=max(abs(frac(cell*3)-.5)*(1/10.5)-.025,(top+height-.06)-q.y);
    tower=max(tower,-notch);
    return min(body,ax<.82?tower:1);
   }
   float3 skyAt(float2 uv)
   {
    float3 sky=lerp(float3(.80,.90,.97),float3(.27,.55,.86),smoothstep(.22,.95,uv.y));
    float sun=exp(-length((uv-float2(.18,.93))*float2(1.4,1))*5.5);
    return sky+float3(.30,.24,.12)*sun;
   }
   void island(inout float3 color,float2 p,float2 c,float s,float seed,float haze,float2 uv)
   {
    float2 q=(p-c)/s;
    if(abs(q.x)>1.2 || q.y<-1.6 || q.y>1.4)return;
    float d=islandDistance(q,seed)*s;
    float m=coverage(d);
    float shade=saturate(q.y*.45+.78);
    float3 rock=lerp(float3(.40,.45,.62),float3(.95,.92,.86),shade);
    float moss=smoothstep(-.18,.02,q.y)*step(q.y,.12)*(.6+.4*smoothNoise(q*9+seed));
    rock=lerp(rock,float3(.56,.72,.55),moss*.55);
    rock+=float3(.14,.11,.05)*saturate(1-abs(q.y-.04)*8);
    color=lerp(color,lerp(rock,skyAt(uv),haze),m);
   }
   void waterfall(inout float3 color,float2 p,float x,float top,float bottom,float width,float haze,float2 uv)
   {
    float d=abs(p.x-x)-width*(1+(top-p.y)*1.6);
    float fall=coverage(d)*smoothstep(bottom,lerp(bottom,top,.45),p.y)*step(p.y,top);
    float streak=.75+.25*smoothNoise(float2(p.x*900,p.y*40));
    color=lerp(color,lerp(float3(.93,.97,1)*streak,skyAt(uv),haze),fall*.85);
   }
   float cloud(float2 p,float floor_,float soft)
   {
    float n=fbm(p);
    return smoothstep(floor_,floor_+soft,n);
   }
   half4 frag(V i):SV_Target
   {
    float2 uv=i.uv;float aspect=_ScreenParams.x/_ScreenParams.y;
    float2 p=float2(uv.x*aspect,uv.y);
    float3 color=skyAt(uv);
    // High wisps.
    float wisp=cloud(float2(p.x*6,p.y*14)+float2(3,1),.56,.22)*smoothstep(.70,.95,uv.y);
    color=lerp(color,float3(.97,.98,1),wisp*.35);
    // Far ruins, heavily hazed.
    island(color,p,float2(.07*aspect/.46,.865),.030,1.7,.45,uv);
    island(color,p,float2(.31*aspect/.46,.905),.038,4.2,.48,uv);
    island(color,p,float2(.42*aspect/.46,.835),.026,7.9,.44,uv);
    island(color,p,float2(.20*aspect/.46,.80),.022,2.6,.5,uv);
    // Middle band of soft cloud.
    float band=cloud(p*float2(9,22)+float2(11,4),.5,.2)*smoothstep(.70,.78,uv.y)*smoothstep(.90,.80,uv.y);
    color=lerp(color,float3(.95,.97,1),band*.4);
    // Nearer ruins with falls at the screen edges.
    island(color,p,float2(.025*aspect/.46,.735),.060,3.3,.16,uv);
    waterfall(color,p,.032*aspect/.46,.70,.56,.0035,.35,uv);
    island(color,p,float2(.445*aspect/.46,.70),.068,5.1,.14,uv);
    waterfall(color,p,.43*aspect/.46,.66,.50,.004,.3,uv);
    island(color,p,float2(.40*aspect/.46,.205),.075,8.4,.12,uv);
    waterfall(color,p,.392*aspect/.46,.16,.0,.005,.25,uv);
    island(color,p,float2(.04*aspect/.46,.25),.055,6.6,.16,uv);
    // Lit cumulus sea below the diorama: bright crowns, cool undersides.
    float2 cp=p*float2(7,9)+float2(5,2);
    float sea=cloud(cp,.42,.16)*smoothstep(.34,.10,uv.y);
    float lit=saturate((fbm(cp+float2(0,.35))-fbm(cp))*4+.6);
    float3 cumulus=lerp(float3(.70,.79,.90),float3(1,.99,.96),lit);
    color=lerp(color,cumulus,sea*.92);
    float low=smoothstep(.16,.0,uv.y);color=lerp(color,float3(.93,.96,.99),low*.5);
    // Corner puffs frame the top HUD like the reference.
    float2 cq=p*float2(8,8)+float2(1,7);
    float puff=cloud(cq,.48,.16)*(smoothstep(.12,.0,uv.x)+smoothstep(.88,1,uv.x))*smoothstep(.55,.75,uv.y);
    color=lerp(color,float3(.98,.99,1),saturate(puff)*.5);
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
