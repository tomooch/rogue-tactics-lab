using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueTactics.Presentation.Gfx
{
    public enum PuppetStyle { Caster, Guardian, Blade, Archer }
    public enum PuppetClip { None, Attack, Hit, Guard, Cast, Cheer, Fall, Rise }

    /// <summary>
    /// Display-only puppet for one approved still image. A soft 2D skeleton is laid over the art and
    /// skinned on the GPU, so the picture breathes, steps, swings its weapon and trails hair and cloth
    /// without a single new drawing. It reads no rules and writes no state: the view tells it which
    /// Core event is being shown and it only chooses how that looks.
    /// </summary>
    public sealed class HqPuppet
    {
        public const int Root=0,Body=1,Head=2,Weapon=3,LegL=4,LegR=5,CapeL=6,CapeR=7,HairL=8,HairR=9,Extra=10,BoneCount=11;
        private const int ShaderBones=16;
        private const float Aspect=.75f;

        public struct Bone
        {
            public int Parent;public Vector2 Pivot,A,B;public float Radius,Strength;
            // Secondary motion: +1 hangs from its pivot (hair, cape, tail), -1 stands up (hat tip), 0 is driven only by clips.
            public float Hang;public float Stiffness,Damping;
        }
        public sealed class Rig
        {
            public PuppetStyle Style;public readonly Bone[] Bones=new Bone[BoneCount];
            /// <summary>Where magic, arrows and swings leave the art, in image space, carried by the weapon bone.</summary>
            public Vector2 Muzzle;
            public Mesh Mesh;
        }

        private struct M
        {
            public float a,b,c,d,e,f;
            public static readonly M Identity=new M{a=1,e=1};
            public static M operator*(M p,M l)=>new M{a=p.a*l.a+p.b*l.d,b=p.a*l.b+p.b*l.e,c=p.a*l.c+p.b*l.f+p.c,d=p.d*l.a+p.e*l.d,e=p.d*l.b+p.e*l.e,f=p.d*l.c+p.e*l.f+p.f};
            public Vector2 Apply(Vector2 p)=>new Vector2(a*p.x+b*p.y+c,d*p.x+e*p.y+f);
        }

        private static readonly int BoneXId=Shader.PropertyToID("_BoneX"),BoneYId=Shader.PropertyToID("_BoneY"),MirrorId=Shader.PropertyToID("_Mirror"),
            FlashId=Shader.PropertyToID("_Flash"),AuraId=Shader.PropertyToID("_Aura"),DissolveId=Shader.PropertyToID("_Dissolve");

        public readonly Rig Skeleton;public readonly Material Skin;
        public bool Mirrored;
        /// <summary>0..1 gold rim while this companion is the one acting.</summary>
        public float Aura;
        /// <summary>Where a melee strike carries the figure, as a screen offset in image heights (x right, y up). Zero for ranged styles.</summary>
        public Vector2 Dash;
        /// <summary>Screen direction a blow throws the figure (unit length), or zero to fall back to the facing.</summary>
        public Vector2 Knock;
        /// <summary>Which way a fall tips in the art's own frame: +1 clockwise (head to the art's right), -1 the other way. Rise uses the same.</summary>
        public float Lean=1;
        public PuppetClip Clip{get;private set;}
        public float ClipTime{get;private set;}

        private readonly float[] rot=new float[BoneCount],tx=new float[BoneCount],ty=new float[BoneCount],sx=new float[BoneCount],sy=new float[BoneCount];
        private readonly float[] spring=new float[BoneCount],springVelocity=new float[BoneCount];
        private readonly Vector2[] lastPivot=new Vector2[BoneCount];
        private readonly M[] world=new M[BoneCount];
        private readonly Vector4[] boneX=new Vector4[ShaderBones],boneY=new Vector4[ShaderBones];
        private readonly float phase;
        private float clipLength=1,clipSide,flash,auraShown,time;
        private Color flashColor=Color.white;
        private bool primed;

        public HqPuppet(Rig rig,Material skin,float phase)
        {
            Skeleton=rig;Skin=skin;this.phase=phase;
            for(var i=0;i<ShaderBones;i++){boneX[i]=new Vector4(1,0,0,0);boneY[i]=new Vector4(0,1,0,0);}
            Push();
        }

        /// <param name="length">Seconds until the shown Core event lands (the strike, the block, the fall).</param>
        /// <param name="side">1 when the action runs along the facing, 0 when it runs straight up or down the screen.</param>
        public void Play(PuppetClip clip,float length,float side){Clip=clip;ClipTime=0;clipLength=Mathf.Max(.05f,length);clipSide=side;}
        public void Flash(Color color,float amount=1){flashColor=color;flash=amount;}
        public void Reset(){Clip=PuppetClip.None;ClipTime=0;flash=0;primed=false;Array.Clear(spring,0,BoneCount);Array.Clear(springVelocity,0,BoneCount);Skin.SetFloat(DissolveId,0);}

        /// <param name="moveX">Screen-right speed of the pawn, in image heights per second.</param>
        /// <param name="stepping">0..1 walk blend.</param>
        /// <param name="stride">Walk phase; one footfall per pi.</param>
        public void Tick(float dt,float moveX,float stepping,float stride)
        {
            dt=Mathf.Min(dt,.05f);time+=dt;var t=time+phase;
            for(var i=0;i<BoneCount;i++){rot[i]=0;tx[i]=0;ty[i]=0;sx[i]=1;sy[i]=1;}

            // Idle: breath from the feet, a slow sway, the head a beat behind the chest.
            var calm=Clip==PuppetClip.Fall?0:1;
            var breath=Mathf.Sin(t*2.3f);
            sy[Root]+=.012f*breath*calm;sx[Root]-=.006f*breath*calm;
            rot[Body]+=.012f*Mathf.Sin(t*1.1f)*calm;
            rot[Head]+=.024f*Mathf.Sin(t*1.4f+.6f)*calm;ty[Head]+=.004f*Mathf.Sin(t*2.3f-.9f)*calm;
            rot[Weapon]+=.028f*Mathf.Sin(t*1.7f+1.3f)*calm;

            // Walk: a pattering hop. The whole figure rocks from one foot to the other as a rigid piece;
            // the legs are never bent or stretched (the owner rejected rubbery limbs), and a drawn
            // 8-frame walk cycle can replace this without touching anything else.
            var bounce=Mathf.Abs(Mathf.Sin(stride));var swing=Mathf.Sin(stride);
            ty[Root]+=stepping*bounce*.026f;sy[Root]+=stepping*(.010f-bounce*.018f);sx[Root]-=stepping*(.007f-bounce*.012f);
            rot[Root]+=stepping*(.03f+.05f*swing);
            rot[Head]-=stepping*.03f*Mathf.Sin(stride*2);rot[Weapon]+=stepping*.05f*Mathf.Sin(stride*2+1);

            var flare=0f;var dissolve=0f;
            if(Clip!=PuppetClip.None)
            {
                ClipTime+=dt;var u=ClipTime/clipLength;var post=ClipTime-clipLength;var h=clipSide;
                switch(Clip)
                {
                    case PuppetClip.Attack:
                    {
                        // Gather, then land exactly when the event lands (u = 1), then settle.
                        var gather=u<.7f?OutCubic(u/.7f):Mathf.Max(0,1-InCubic((u-.7f)/.3f));
                        var strike=u<.7f?0:u<1?Square((u-.7f)/.3f):Mathf.Max(0,1-OutCubic(Mathf.Max(0,post-.07f)/.34f));
                        if(u>=1)gather=0;
                        Attack(gather,strike,h,ref flare);
                        // The step through: a small draw back, then the whole figure carried to the target and home again.
                        var carry=strike-.10f*gather;tx[Root]+=(Mirrored?-Dash.x:Dash.x)*carry;ty[Root]+=Dash.y*carry;
                        sx[Root]+=.06f*strike*(u<1?1:0);
                        if(post>.45f)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Cast:
                    {
                        // Lift onto the toes with the staff or hands raised, release as the effect lands.
                        var lift=u<1?OutCubic(u):Mathf.Max(0,1-OutCubic(post/.35f));
                        ty[Root]+=.020f*lift;sy[Root]+=.035f*lift;sx[Root]-=.02f*lift;rot[Weapon]+=.22f*lift;ty[Weapon]+=.025f*lift;rot[Head]-=.05f*lift;flare+=lift*.8f;
                        if(post>.36f)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Guard:
                    {
                        // Plant the feet and push the shield (or the whole body) into the line of attack.
                        var brace=u<1?OutBack(Mathf.Clamp01(u/.45f)):Mathf.Max(0,1-OutCubic(post/.3f));
                        sy[Root]-=.05f*brace;sx[Root]+=.035f*brace;rot[Root]+=.05f*brace*h;tx[Root]-=.018f*brace*h;
                        if(Skeleton.Style==PuppetStyle.Guardian){tx[Extra]-=.035f*brace;ty[Extra]+=.02f*brace;sx[Extra]+=.14f*brace;sy[Extra]+=.14f*brace;rot[Weapon]-=.2f*brace;}
                        else rot[Weapon]+=.18f*brace;
                        if(post>.31f)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Hit:
                    {
                        // Knocked away from the blow, head a touch later, three damped wobbles back to rest.
                        var k=Mathf.Exp(-ClipTime*7.5f);var wobble=Mathf.Cos(ClipTime*26f);
                        rot[Root]-=(.20f*k*(.55f+.45f*wobble))*(h==0?.5f:1);
                        if(Knock!=Vector2.zero){var shove=.11f*Mathf.Exp(-ClipTime*5f)*Mathf.Min(1,ClipTime*30);tx[Root]+=(Mirrored?-Knock.x:Knock.x)*shove;ty[Root]+=Knock.y*shove*.6f+.03f*Mathf.Sin(Mathf.Min(1,ClipTime*5)*Mathf.PI);}
                        else {tx[Root]+=.045f*k*h;ty[Root]+=.012f*k*(1-h);}
                        sy[Root]-=.10f*k;sx[Root]+=.07f*k;rot[Head]-=.16f*Mathf.Exp(-Mathf.Max(0,ClipTime-.04f)*6f)*Mathf.Cos(ClipTime*19f);
                        flare+=k;
                        if(ClipTime>.7f)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Cheer:
                    {
                        // Two light hops when mended.
                        var hop=Mathf.Abs(Mathf.Sin(Mathf.Clamp01(u)*Mathf.PI*2));var land=1-hop;
                        ty[Root]+=.045f*hop;sy[Root]+=.05f*hop-.03f*land*Mathf.Clamp01(u*4)*(u<1?1:0);rot[Head]+=.05f*Mathf.Sin(u*Mathf.PI*2);flare+=hop*.6f;
                        if(u>=1)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Fall:
                    {
                        // Stagger back and sink down to rest. A fallen ally is not gone: a victory brings them back
                        // (design/specs/01 5.1), so the figure stays whole and lies still; the view keeps a dimmed copy.
                        var e=Mathf.Clamp01(u);var tip=e*e;
                        var rest=u>=1?Mathf.Exp(-(ClipTime-clipLength)*10)*Mathf.Sin((ClipTime-clipLength)*22)*.04f:0;
                        rot[Root]-=(1.25f*tip+rest)*Lean;tx[Root]+=.06f*OutCubic(e)*(h==0?.6f:1)*Lean;sy[Root]-=.06f*tip;rot[Head]-=.2f*Mathf.Sin(e*Mathf.PI)*Lean;
                        rot[Weapon]-=.6f*tip*Lean;flare+=.5f*(1-e);
                        break;
                    }
                    case PuppetClip.Rise:
                    {
                        // The fall played backwards from where it left them: a slow push up, upright, then one small hop
                        // as they find their feet. Same pivots and the same rigid tilt, so they read as the same person.
                        var e=Mathf.Clamp01(u/.72f);var lie=1-e*e*(3-2*e);
                        rot[Root]-=1.25f*lie*Lean;tx[Root]+=.06f*lie*(h==0?.6f:1)*Lean;sy[Root]-=.06f*lie;rot[Weapon]-=.6f*lie*Lean;
                        rot[Head]+=.12f*Mathf.Sin(e*Mathf.PI)*Lean;
                        var hop=u>.72f?Mathf.Sin(Mathf.Clamp01((u-.72f)/.28f)*Mathf.PI):0;
                        ty[Root]+=.04f*hop;sy[Root]+=.03f*hop;flare+=.4f*hop+.3f*Mathf.Sin(e*Mathf.PI);
                        if(u>=1)Clip=PuppetClip.None;
                        break;
                    }
                }
            }

            // Hierarchy, with hair, cloth and tails following a beat behind as damped springs.
            for(var i=0;i<BoneCount;i++)
            {
                var bone=Skeleton.Bones[i];var parent=bone.Parent<0?M.Identity:world[bone.Parent];
                if(bone.Hang!=0 && bone.Strength>0)
                {
                    var pivot=parent.Apply(bone.Pivot);
                    var velocity=primed&&dt>0?(pivot-lastPivot[i])/dt:Vector2.zero;lastPivot[i]=pivot;velocity.x+=moveX;
                    var outward=Mathf.Sign(bone.B.x-bone.Pivot.x+.0001f);
                    var wind=.035f*Mathf.Sin(t*1.3f+i*1.9f)+.018f*Mathf.Sin(t*3.1f+i);
                    var target=bone.Hang*Mathf.Clamp(-velocity.x*.45f,-.7f,.7f)+wind
                        +outward*bone.Hang*(Mathf.Clamp(-velocity.y*.35f,-.25f,.5f)+flare*.22f);
                    var step=dt;
                    springVelocity[i]+=(bone.Stiffness*(target-spring[i])-bone.Damping*springVelocity[i])*step;spring[i]+=springVelocity[i]*step;
                    spring[i]=Mathf.Clamp(spring[i],-1.1f,1.1f);rot[i]+=spring[i];
                }
                world[i]=parent*Local(bone.Pivot,rot[i],tx[i],ty[i],sx[i],sy[i]);
                boneX[i]=new Vector4(world[i].a,world[i].b,world[i].c,0);boneY[i]=new Vector4(world[i].d,world[i].e,world[i].f,0);
            }
            primed=true;
            flash=Mathf.MoveTowards(flash,0,dt*7f);auraShown=Mathf.MoveTowards(auraShown,Aura,dt*5);
            Skin.SetFloat(DissolveId,dissolve);
            Push();
        }

        private void Attack(float gather,float strike,float h,ref float flare)
        {
            switch(Skeleton.Style)
            {
                case PuppetStyle.Blade:
                    // Greatsword: haul it back over the shoulder, then step through the cut.
                    rot[Root]+=-.10f*gather+.17f*strike;tx[Root]+=(.035f*gather-.085f*strike)*h;ty[Root]+=.02f*strike*(1-h);
                    sy[Root]+=-.07f*gather+.05f*strike;sx[Root]+=.05f*gather-.035f*strike;
                    rot[Weapon]+=-.50f*gather+.95f*strike;rot[Head]+=-.06f*gather+.08f*strike;rot[Body]+=.05f*strike;flare+=strike;
                    break;
                case PuppetStyle.Guardian:
                    // Mace: swing it up behind, bring it over the top.
                    rot[Root]+=-.08f*gather+.14f*strike;tx[Root]+=(.03f*gather-.065f*strike)*h;ty[Root]+=.02f*strike*(1-h);
                    sy[Root]+=-.06f*gather+.045f*strike;sx[Root]+=.045f*gather-.03f*strike;
                    rot[Weapon]+=-.55f*gather+1.0f*strike;tx[Extra]-=.02f*strike;rot[Head]+=.06f*strike;flare+=strike*.7f;
                    break;
                case PuppetStyle.Archer:
                    // Bow: lean back into the draw, snap forward on the loose, the tail whips after.
                    rot[Root]+=-.08f*gather+.05f*strike;sx[Root]+=-.04f*gather+.04f*strike;sy[Root]+=.02f*gather-.02f*strike;
                    rot[Body]+=-.05f*gather;rot[Weapon]+=-.14f*gather+.12f*strike;tx[Weapon]+=(.012f*gather-.02f*strike);rot[Head]+=-.04f*gather;
                    tx[Root]+=.02f*strike*h;flare+=strike*.5f;
                    break;
                default:
                    // Staff: draw power in, rise, and thrust the gem toward the target.
                    ty[Root]+=.012f*gather+.026f*strike;sy[Root]+=-.04f*gather+.05f*strike;sx[Root]+=.03f*gather-.03f*strike;
                    rot[Weapon]+=-.20f*gather+.50f*strike;ty[Weapon]+=.02f*gather+.015f*strike;tx[Weapon]-=.025f*strike;rot[Head]+=-.05f*gather+.04f*strike;
                    rot[Root]+=.05f*strike;flare+=gather*.5f+strike;
                    break;
            }
        }

        private void Push()
        {
            Skin.SetVectorArray(BoneXId,boneX);Skin.SetVectorArray(BoneYId,boneY);Skin.SetFloat(MirrorId,Mirrored?1:0);
            Skin.SetColor(FlashId,new Color(flashColor.r,flashColor.g,flashColor.b,Mathf.Clamp01(flash)*.6f));
            var aura=Skin.GetColor(AuraId);aura.a=auraShown;Skin.SetColor(AuraId,aura);
        }

        /// <summary>World position of a point on the art as it is currently posed (for effects that leave a weapon).</summary>
        public Vector3 WorldPoint(Transform quad,int bone,Vector2 imagePoint)
        {
            var p=world[bone].Apply(imagePoint);if(Mirrored)p.x=-p.x;
            return quad.TransformPoint(new Vector3(p.x/Aspect,p.y-.5f,0));
        }
        public Vector3 Muzzle(Transform quad)=>WorldPoint(quad,Weapon,Skeleton.Muzzle);

        private static M Local(Vector2 p,float r,float x,float y,float scaleX,float scaleY)
        {
            var c=Mathf.Cos(r);var s=Mathf.Sin(r);var m=new M{a=c*scaleX,b=-s*scaleY,d=s*scaleX,e=c*scaleY};
            m.c=p.x+x-(m.a*p.x+m.b*p.y);m.f=p.y+y-(m.d*p.x+m.e*p.y);return m;
        }
        private static float OutCubic(float x){x=1-Mathf.Clamp01(x);return 1-x*x*x;}
        private static float InCubic(float x){x=Mathf.Clamp01(x);return x*x*x;}
        private static float Square(float x){x=Mathf.Clamp01(x);return x*x;}
        private static float OutBack(float x){x=Mathf.Clamp01(x)-1;return 1+2.2f*x*x*x+1.2f*x*x;}

        // ---- Rigs -------------------------------------------------------------------------------------

        /// <summary>Image-space point from texture coordinates (u right, v up, both 0..1).</summary>
        public static Vector2 At(float u,float v)=>new Vector2((u-.5f)*Aspect,v);

        private static Bone Limb(int parent,Vector2 pivot,Vector2 a,Vector2 b,float radius,float strength=1,float hang=0,float stiffness=70,float damping=7)
            =>new Bone{Parent=parent,Pivot=pivot,A=a,B=b,Radius=radius,Strength=strength,Hang=hang,Stiffness=stiffness,Damping=damping};
        private static Bone None(int parent)=>new Bone{Parent=parent};

        /// <summary>Skeletons authored against the four approved 3:4 images. Index: 0 Shiro, 1 Momo, 2 Garu, 3 Myaru.</summary>
        public static Rig Authored(int index)
        {
            var rig=new Rig();var b=rig.Bones;
            b[Root]=new Bone{Parent=-1,Pivot=At(.5f,.08f),A=At(.5f,.08f),B=At(.5f,.08f),Radius=1,Strength=0};
            switch(index)
            {
                case 0: // Shiro: witch hat, long hair, wide cape, staff held at her right.
                    rig.Style=PuppetStyle.Caster;rig.Muzzle=At(.84f,.735f);
                    b[Body]=Limb(Root,At(.47f,.36f),At(.47f,.38f),At(.46f,.58f),.13f);
                    b[Head]=Limb(Body,At(.45f,.60f),At(.44f,.64f),At(.40f,.80f),.21f,1.5f);
                    b[Weapon]=Limb(Body,At(.75f,.535f),At(.63f,.30f),At(.84f,.75f),.045f,1.8f);
                    b[LegL]=Limb(Root,At(.40f,.33f),At(.39f,.28f),At(.34f,.10f),.065f);
                    b[LegR]=Limb(Root,At(.55f,.33f),At(.56f,.28f),At(.59f,.10f),.065f);
                    b[CapeL]=Limb(Body,At(.27f,.50f),At(.22f,.45f),At(.09f,.37f),.085f,1,1,55,6);
                    b[CapeR]=Limb(Body,At(.70f,.46f),At(.76f,.42f),At(.87f,.33f),.075f,1,1,55,6);
                    b[HairL]=Limb(Head,At(.26f,.62f),At(.24f,.56f),At(.20f,.42f),.065f,1,1,48,5.5f);
                    b[HairR]=Limb(Head,At(.62f,.60f),At(.63f,.55f),At(.66f,.46f),.045f,1,1,60,6);
                    b[Extra]=Limb(Head,At(.33f,.85f),At(.29f,.88f),At(.20f,.92f),.075f,1.2f,-1,80,8);
                    break;
                case 1: // Momo: round shield on her right arm (screen left), mace raised on the other side.
                    rig.Style=PuppetStyle.Guardian;rig.Muzzle=At(.885f,.66f);
                    b[Body]=Limb(Root,At(.52f,.35f),At(.52f,.37f),At(.52f,.56f),.13f);
                    b[Head]=Limb(Body,At(.52f,.585f),At(.52f,.63f),At(.52f,.80f),.20f,1.5f);
                    b[Weapon]=Limb(Body,At(.78f,.50f),At(.74f,.44f),At(.885f,.67f),.06f,1.8f);
                    b[LegL]=Limb(Root,At(.42f,.32f),At(.40f,.27f),At(.30f,.09f),.07f);
                    b[LegR]=Limb(Root,At(.62f,.32f),At(.64f,.27f),At(.71f,.09f),.07f);
                    b[CapeL]=Limb(Body,At(.36f,.34f),At(.33f,.30f),At(.27f,.22f),.06f,.8f,1,60,6);
                    b[CapeR]=Limb(Body,At(.68f,.34f),At(.72f,.30f),At(.80f,.22f),.06f,.8f,1,60,6);
                    b[HairL]=Limb(Head,At(.31f,.72f),At(.27f,.69f),At(.20f,.62f),.065f,1,1,50,5.5f);
                    b[HairR]=Limb(Head,At(.70f,.71f),At(.73f,.67f),At(.77f,.60f),.055f,1,1,54,5.5f);
                    b[Extra]=Limb(Body,At(.34f,.47f),At(.22f,.36f),At(.26f,.56f),.13f,1.6f);
                    break;
                case 2: // Garu: wide stance, greatsword from his hands up to the upper left.
                    rig.Style=PuppetStyle.Blade;rig.Muzzle=At(.10f,.84f);
                    b[Body]=Limb(Root,At(.53f,.38f),At(.53f,.40f),At(.53f,.57f),.13f);
                    b[Head]=Limb(Body,At(.53f,.60f),At(.53f,.65f),At(.53f,.82f),.20f,1.5f);
                    b[Weapon]=Limb(Body,At(.44f,.45f),At(.38f,.51f),At(.03f,.93f),.05f,2f);
                    b[LegL]=Limb(Root,At(.43f,.35f),At(.40f,.30f),At(.28f,.10f),.075f);
                    b[LegR]=Limb(Root,At(.64f,.35f),At(.68f,.30f),At(.82f,.11f),.075f);
                    b[CapeL]=Limb(Body,At(.27f,.52f),At(.21f,.49f),At(.08f,.42f),.09f,1,1,52,6);
                    b[CapeR]=Limb(Body,At(.77f,.50f),At(.83f,.45f),At(.92f,.36f),.09f,1,1,52,6);
                    b[HairL]=None(Head);b[HairR]=None(Head);b[Extra]=None(Body);
                    break;
                default: // Myaru: fox ears and tail, leaf cloak, bow held out at her left (screen right).
                    rig.Style=PuppetStyle.Archer;rig.Muzzle=At(.80f,.50f);
                    b[Body]=Limb(Root,At(.47f,.37f),At(.47f,.39f),At(.46f,.56f),.12f);
                    b[Head]=Limb(Body,At(.45f,.585f),At(.45f,.63f),At(.45f,.80f),.20f,1.5f);
                    b[Weapon]=Limb(Body,At(.77f,.49f),At(.73f,.27f),At(.84f,.84f),.05f,1.7f);
                    b[LegL]=Limb(Root,At(.40f,.33f),At(.39f,.28f),At(.33f,.09f),.065f);
                    b[LegR]=Limb(Root,At(.56f,.33f),At(.59f,.28f),At(.68f,.09f),.065f);
                    b[CapeL]=Limb(Body,At(.27f,.50f),At(.22f,.47f),At(.10f,.42f),.085f,1,1,52,6);
                    b[CapeR]=None(Body);
                    b[HairL]=Limb(Head,At(.26f,.66f),At(.24f,.61f),At(.20f,.50f),.06f,1,1,50,5.5f);
                    b[HairR]=Limb(Head,At(.63f,.66f),At(.65f,.62f),At(.67f,.55f),.045f,1,1,58,6);
                    b[Extra]=Limb(Body,At(.62f,.33f),At(.70f,.27f),At(.85f,.19f),.10f,1.5f,1,34,4.2f);
                    break;
            }
            rig.Mesh=BuildMesh(rig);
            return rig;
        }

        // A plain grid with the unit quad's exact extents; only the per-vertex bone weights differ per character.
        private static Mesh BuildMesh(Rig rig)
        {
            const int nx=20,ny=28;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var bones=new List<Vector4>();var weights=new List<Vector4>();var triangles=new List<int>();
            var score=new float[BoneCount];
            for(var j=0;j<=ny;j++)for(var i=0;i<=nx;i++)
            {
                var u=(float)i/nx;var v=(float)j/ny;vertices.Add(new Vector3(u-.5f,v-.5f,0));uv.Add(new Vector2(u,v));
                var p=At(u,v);
                for(var k=0;k<BoneCount;k++)
                {
                    var bone=rig.Bones[k];
                    if(k==Root){score[k]=.035f;continue;}
                    if(bone.Strength<=0){score[k]=0;continue;}
                    var d=SegmentDistance(p,bone.A,bone.B)/bone.Radius;score[k]=bone.Strength*Mathf.Exp(-d*d);
                }
                var index=Vector4.zero;var weight=Vector4.zero;var sum=0f;
                for(var slot=0;slot<4;slot++)
                {
                    var best=0;for(var k=1;k<BoneCount;k++)if(score[k]>score[best])best=k;
                    index[slot]=best;weight[slot]=score[best];sum+=score[best];score[best]=-1;
                }
                bones.Add(index);weights.Add(weight/Mathf.Max(sum,1e-6f));
                if(i<nx && j<ny){var n=j*(nx+1)+i;triangles.AddRange(new[]{n,n+nx+1,n+1,n+1,n+nx+1,n+nx+2});}
            }
            var mesh=new Mesh{name="Puppet grid"};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,bones);mesh.SetUVs(2,weights);mesh.SetTriangles(triangles,0);
            mesh.bounds=new Bounds(Vector3.zero,new Vector3(1,1,0));
            return mesh;
        }
        private static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
        {var ab=b-a;var t=ab.sqrMagnitude<1e-8f?0:Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);return (p-(a+ab*t)).magnitude;}
    }
}
