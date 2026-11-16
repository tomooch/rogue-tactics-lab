using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// Display-only effects. A small CPU particle pool drawn as one mesh with one procedural material:
    /// no textures, no Unity ParticleSystem assets, no allocation after warm-up. Nothing here reads or
    /// changes rules; callers spawn a recipe when they show the Core event it illustrates.
    /// </summary>
    public sealed class HqVfx:MonoBehaviour
    {
        public const float Glow=0,Ring=1,Sparkle=2,Crescent=3,Streak=4,Petal=5,Plus=6,Puff=7,Shard=8,Ward=9,Rune=10,Pillar=11;
        private const int Capacity=3000;
        private enum Facing:byte{Camera,Ground,Velocity}

        private struct Particle
        {
            public Vector3 Position,Velocity;public float Gravity,Drag,Age,Life,Delay;
            public float Size0,Size1,Aspect,Rotation,Spin,Sway;
            public Color Color0,Color1;public float Shape,K0,K1,Light,Seed,FadeIn,FadeFrom;public Facing Facing;
        }
        private sealed class Shot
        {public Vector3 From,To;public float Age,Life,Arc,Trail;public Color Tint;public bool Arrow;}
        private struct Job{public float Delay;public Action Run;}

        public Camera View;
        /// <summary>Effects freeze with the battle when the player pauses.</summary>
        public Func<bool> IsPaused;
        /// <summary>Drifting petals and motes over the board; purely atmosphere.</summary>
        public bool Ambient=true;public Vector3 AmbientCentre;public float AmbientRadius=9;
        public int Alive=>particles.Count;

        private readonly List<Particle> particles=new List<Particle>(512);
        private readonly List<Shot> shots=new List<Shot>();
        private readonly List<Job> jobs=new List<Job>();
        private readonly List<Vector3> vertices=new List<Vector3>(2048);
        private readonly List<Color> colors=new List<Color>(2048);
        private readonly List<Vector4> uv=new List<Vector4>(2048);
        private readonly List<Vector2> mode=new List<Vector2>(2048);
        private readonly List<int> indices=new List<int>(3072);
        private readonly System.Random random=new System.Random(20261116);
        private Mesh mesh;private Material material;private float ambientClock;

        public static HqVfx Create(Camera view,Func<bool> paused)
        {
            var shader=Resources.Load<Shader>("Gfx/HqVfx");if(shader==null)return null;
            var host=new GameObject("Battle effects",typeof(MeshFilter),typeof(MeshRenderer));
            var vfx=host.AddComponent<HqVfx>();vfx.View=view;vfx.IsPaused=paused;
            vfx.material=new Material(shader){name="Procedural effects"};
            vfx.mesh=new Mesh{name="Effect quads"};vfx.mesh.MarkDynamic();
            host.GetComponent<MeshFilter>().sharedMesh=vfx.mesh;
            var renderer=host.GetComponent<MeshRenderer>();renderer.sharedMaterial=vfx.material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            return vfx;
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}

        // ---- Recipes ----------------------------------------------------------------------------------

        public static readonly Color Azure=new Color(.42f,.74f,1f),Rose=new Color(1f,.52f,.70f),Steel=new Color(.80f,.90f,1f),Leaf=new Color(.62f,1f,.42f),
            Gold=new Color(1f,.84f,.36f),Jade=new Color(.50f,1f,.68f),Moss=new Color(.72f,.95f,.35f),Violet=new Color(.80f,.52f,1f),Cream=new Color(1f,.96f,.86f);

        /// <summary>The moment a blow lands: flash, shock ring, sparks thrown outward, a little debris.</summary>
        public void Impact(Vector3 at,Color tint,float power=1)
        {
            Emit(Glow,at,.4f,1.25f*power,.12f,With(Color.white,.8f),With(tint,.5f),light:1,fadeFrom:.15f);
            Emit(Ring,at,.3f,1.6f*power,.28f,With(Color.white,.9f),tint,light:1,k0:.35f,k1:1,fadeFrom:.3f);
            for(var i=0;i<11;i++)
            {
                var dir=Screen(Range(0,360),1);var speed=Range(5,10)*power;
                Emit(Streak,at+dir*.15f,.34f,.05f,Range(.20f,.34f),Color.white,tint,light:1,velocity:dir*speed,drag:6,facing:Facing.Velocity,aspect:2.3f);
            }
            for(var i=0;i<4;i++)Emit(Sparkle,at+Screen(Range(0,360),Range(.3f,.8f)*power),.26f,0,Range(.25f,.42f),Color.white,tint,light:1,spin:Range(-4,4),delay:Range(0,.08f));
            for(var i=0;i<3;i++)
            {
                var dir=Screen(Range(20,160),1);
                Emit(Puff,at+dir*.2f,.28f,.62f,Range(.32f,.46f),With(Color.Lerp(tint,Cream,.7f),.55f),With(Cream,0),velocity:dir*Range(.8f,1.8f),drag:3,k0:.05f,k1:1,seed:Range(0,1));
            }
        }

        /// <summary>A blade's cut hanging in the air. <paramref name="degrees"/> is the screen direction the cut bulges toward.</summary>
        public void Slash(Vector3 at,float degrees,Color tint,float size=1.9f,bool downward=true)
        {
            var flip=downward?-1f:1f;
            Emit(Crescent,at,size,size*1.08f,.24f,Color.white,tint,light:1,rotation:degrees,k0:0,k1:1,fadeFrom:.55f,aspect:flip);
            Emit(Crescent,at,size*1.12f,size*1.22f,.30f,With(tint,.55f),With(tint,0),light:1,rotation:degrees,k0:0,k1:.9f,fadeFrom:.4f,aspect:flip,delay:.02f);
            for(var i=0;i<7;i++)
            {
                var a=degrees+flip*Range(-80,80);var p=at+Screen(a,size*.74f);var dir=Screen(a-flip*90,1);
                Emit(Streak,p,.22f,.03f,Range(.18f,.3f),Color.white,tint,light:1,velocity:dir*Range(2,5),drag:4,facing:Facing.Velocity,aspect:2,delay:Range(.02f,.12f));
            }
        }

        /// <summary>Magic gathering at a point before it is loosed.</summary>
        public void Charge(Vector3 at,Color tint,float seconds)
        {
            Emit(Glow,at,.15f,.6f,seconds,With(tint,.7f),With(Color.white,.8f),light:1,fadeIn:.3f,fadeFrom:.85f);
            for(var i=0;i<12;i++)
            {
                var dir=Screen(Range(0,360),1);var delay=Range(0,seconds*.7f);var life=seconds-delay;var radius=Range(.9f,1.5f);
                Emit(Sparkle,at+dir*radius,.16f,.04f,life,tint,Color.white,light:1,velocity:-dir*(radius/life),delay:delay,spin:Range(-3,3),fadeIn:.25f,fadeFrom:.9f);
            }
        }

        /// <summary>A circle of runes turning on the ground under a caster, with light rising from it.</summary>
        public void CastCircle(Vector3 ground,Color tint,float seconds)
        {
            Emit(Rune,ground,1.15f,1.45f,seconds+.25f,With(tint,.95f),With(tint,.5f),light:1,k0:0,k1:.35f,facing:Facing.Ground,fadeIn:.15f,fadeFrom:.7f);
            Emit(Pillar,ground+Up*1.3f,1.3f,1.5f,seconds+.2f,With(tint,.42f),With(tint,0),light:1,aspect:.62f,fadeIn:.25f,fadeFrom:.5f);
            for(var i=0;i<9;i++)
                Emit(Sparkle,ground+Wide(Range(-1,1),Range(-.6f,.6f))+Up*Range(0,.4f),.13f,.02f,Range(.5f,.8f),Color.white,tint,light:1,velocity:Up*Range(1.2f,2.6f),delay:Range(0,seconds*.8f),spin:Range(-3,3),fadeIn:.2f);
        }

        /// <summary>A thrown spell or a loosed arrow. It arrives exactly after <paramref name="seconds"/>; the caller shows the impact then.</summary>
        public void Shoot(Vector3 from,Vector3 to,float seconds,Color tint,bool arrow)
        {shots.Add(new Shot{From=from,To=to,Life=Mathf.Max(.05f,seconds),Tint=tint,Arrow=arrow,Arc=arrow?.55f:.25f});}

        /// <summary>A protective dome snapping into place; <paramref name="struck"/> rings it like a bell.</summary>
        public void Shield(Vector3 at,Color tint,bool struck)
        {
            Emit(Ward,at,struck?1.25f:.7f,struck?1.5f:1.4f,struck?.42f:.62f,With(tint,.95f),With(tint,.4f),light:1,k0:0,k1:1,fadeIn:.12f,fadeFrom:.55f);
            Emit(Ring,at,.5f,1.8f,.36f,With(Color.white,.85f),tint,light:1,k0:.4f,k1:1,fadeFrom:.3f);
            if(struck)for(var i=0;i<8;i++){var dir=Screen(Range(0,360),1);Emit(Shard,at+dir*.9f,.13f,.03f,Range(.3f,.5f),Color.white,tint,light:1,velocity:dir*Range(2,4.5f),drag:3,gravity:-4,spin:Range(-9,9),rotation:Range(0,360));}
            else for(var i=0;i<6;i++)Emit(Sparkle,at+Screen(Range(0,360),Range(.8f,1.3f)),.16f,0,Range(.4f,.7f),Color.white,tint,light:1,delay:Range(.05f,.3f),spin:Range(-3,3));
        }

        /// <summary>Mending: a column of soft green light, plus signs and motes rising through it.</summary>
        public void Heal(Vector3 ground)
        {
            Emit(Ring,ground,.4f,1.5f,.5f,Color.white,Jade,light:1,k1:1,facing:Facing.Ground,fadeFrom:.4f);
            Emit(Pillar,ground+Up*1.5f,1.4f,1.7f,.8f,With(Jade,.5f),With(Jade,0),light:1,aspect:.55f,fadeIn:.2f,fadeFrom:.45f);
            for(var i=0;i<9;i++)
                Emit(Plus,ground+Wide(Range(-.8f,.8f),Range(-.3f,.3f))+Up*Range(.2f,1.2f),.17f,.09f,Range(.7f,1f),Color.white,Jade,light:1,velocity:Up*Range(1.3f,2.4f),drag:1,delay:Range(0,.35f),sway:Range(-1.4f,1.4f),fadeIn:.15f);
            for(var i=0;i<10;i++)
                Emit(Sparkle,ground+Wide(Range(-1,1),Range(-.4f,.4f))+Up*Range(0,2.2f),.12f,0,Range(.4f,.8f),Color.white,Jade,light:1,velocity:Up*Range(.5f,1.5f),delay:Range(0,.5f),spin:Range(-4,4));
        }

        /// <summary>A defeated garden spirit bursts into cloud and seeds; a fallen companion leaves as rising light.</summary>
        public void Vanish(Vector3 at,Color tint,bool enemy)
        {
            Emit(Ring,at,.3f,1.6f,.36f,Color.white,tint,light:1,k1:1,fadeFrom:.3f);
            if(enemy)
            {
                Emit(Glow,at,.5f,1.3f,.16f,With(Color.white,.8f),With(tint,.5f),light:1,fadeFrom:.2f);
                for(var i=0;i<9;i++)
                {var dir=Screen(i*40+Range(-15,15),1);Emit(Puff,at+dir*.2f,.42f,.85f,Range(.45f,.7f),With(Color.Lerp(tint,Color.white,.72f),.9f),With(Color.white,0),velocity:dir*Range(1.5f,3.2f)+Up*.6f,drag:3.2f,k0:0,k1:1,seed:Range(0,1),fadeFrom:.35f);}
                for(var i=0;i<8;i++)
                {var dir=Screen(Range(30,150),1);Emit(Petal,at,.16f,.10f,Range(.7f,1.1f),tint,With(tint,0),velocity:dir*Range(2.5f,5),gravity:-7,drag:1.2f,spin:Range(-10,10),rotation:Range(0,360),fadeFrom:.6f);}
            }
            else
            {
                Emit(Pillar,at+Up*.6f,1.2f,1.6f,1f,With(Cream,.6f),With(Cream,0),light:1,aspect:.5f,fadeIn:.15f,fadeFrom:.4f);
                for(var i=0;i<12;i++)
                    Emit(Petal,at+Wide(Range(-.6f,.6f),Range(-.3f,.3f))+Up*Range(-.8f,.8f),.14f,.08f,Range(.9f,1.5f),Cream,With(tint,0),velocity:Up*Range(.8f,2.2f)+Screen(Range(0,360),.5f),drag:.6f,sway:Range(-2,2),spin:Range(-5,5),rotation:Range(0,360),delay:Range(0,.35f),fadeFrom:.5f);
            }
            for(var i=0;i<8;i++)Emit(Sparkle,at+Screen(Range(0,360),Range(.2f,1.1f)),.18f,0,Range(.35f,.7f),Color.white,tint,light:1,velocity:Up*Range(.3f,1.4f),delay:Range(0,.25f),spin:Range(-4,4));
        }

        /// <summary>The flash of an answer: a reaction that interrupts the turn.</summary>
        public void Reaction(Vector3 at)
        {
            Emit(Sparkle,at,.15f,.85f,.26f,Color.white,Gold,light:1,rotation:22,fadeFrom:.35f);
            Emit(Sparkle,at,.15f,.55f,.30f,Color.white,Gold,light:1,rotation:67,fadeFrom:.35f,delay:.03f);
            Emit(Ring,at,.15f,.8f,.24f,With(Color.white,.8f),Gold,light:1,k0:.5f,k1:1);
        }

        /// <summary>Feet leaving or meeting stone.</summary>
        public void Dust(Vector3 ground,float amount=1)
        {
            for(var i=0;i<3;i++)
            {var dir=Wide(Range(-1,1),Range(-.3f,.3f)).normalized;Emit(Puff,ground+Up*.12f+dir*.2f,.16f*amount,.42f*amount,Range(.3f,.5f),With(Cream,.5f),With(Cream,0),velocity:dir*Range(.5f,1.3f)+Up*Range(.2f,.6f),drag:3,k0:.1f,k1:1,seed:Range(0,1));}
        }

        /// <summary>A heavy swing or a lunge cutting the air between two points.</summary>
        public void Swoosh(Vector3 from,Vector3 to,Color tint)
        {
            var dir=to-from;var length=dir.magnitude;if(length<.01f)return;
            for(var i=0;i<3;i++)Emit(Streak,from+dir*(.25f+i*.1f)+Up*Range(-.25f,.25f),length*.42f,length*.2f,.20f,With(Color.white,.85f),With(tint,0),light:1,velocity:dir/length*6,drag:5,facing:Facing.Velocity,aspect:1,delay:i*.02f,fadeFrom:.3f);
        }

        /// <summary>The encounter is won.</summary>
        public void Celebrate(Vector3 centre)
        {
            for(var i=0;i<26;i++)
            {
                var tint=i%3==0?Gold:i%3==1?Rose:Cream;
                Emit(Petal,centre+Wide(Range(-4,4),Range(-2,2))+Up*Range(2.5f,5),.17f,.12f,Range(1.6f,2.6f),tint,With(tint,0),velocity:Screen(Range(200,340),Range(.6f,1.6f)),gravity:-1.2f,drag:.8f,sway:Range(-2,2),spin:Range(-6,6),rotation:Range(0,360),delay:Range(0,.6f),fadeIn:.1f,fadeFrom:.7f);
            }
            for(var i=0;i<14;i++)Emit(Sparkle,centre+Wide(Range(-4,4),Range(-2,2))+Up*Range(.5f,4),.24f,0,Range(.5f,.9f),Color.white,Gold,light:1,delay:Range(0,.9f),spin:Range(-3,3));
        }

        public void After(float seconds,Action run){jobs.Add(new Job{Delay=seconds,Run=run});}
        public void Clear(){particles.Clear();shots.Clear();jobs.Clear();}

        // ---- Simulation -------------------------------------------------------------------------------

        private Vector3 Up=>View.transform.up;
        private Vector3 Right=>View.transform.right;
        private float Range(float a,float b)=>a+(float)random.NextDouble()*(b-a);
        private Vector3 Screen(float degrees,float length){var r=degrees*Mathf.Deg2Rad;return (Right*Mathf.Cos(r)+Up*Mathf.Sin(r))*length;}
        private Vector3 Wide(float x,float z)=>new Vector3(x,0,z);
        private static Color With(Color c,float alpha){c.a=alpha;return c;}

        private void Emit(float shape,Vector3 position,float size0,float size1,float life,Color color0,Color color1,float light=0,Vector3 velocity=default,
            float gravity=0,float drag=0,float rotation=0,float spin=0,float k0=0,float k1=0,float delay=0,float aspect=1,float sway=0,float seed=0,
            float fadeIn=0,float fadeFrom=.5f,Facing facing=Facing.Camera)
        {
            if(particles.Count>=Capacity)return;
            particles.Add(new Particle{Shape=shape,Position=position,Size0=size0,Size1=size1,Life=Mathf.Max(.01f,life),Color0=color0,Color1=color1,Light=light,Velocity=velocity,
                Gravity=gravity,Drag=drag,Rotation=rotation*Mathf.Deg2Rad,Spin=spin,K0=k0,K1=k1,Delay=delay,Aspect=aspect,Sway=sway,Seed=seed,FadeIn=fadeIn,FadeFrom=fadeFrom,Facing=facing});
        }

        private void LateUpdate()
        {
            if(View==null||mesh==null)return;
            var dt=IsPaused!=null&&IsPaused()?0:Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(dt>0)
            {
                for(var i=jobs.Count-1;i>=0;i--){var job=jobs[i];job.Delay-=dt;if(job.Delay<=0){jobs.RemoveAt(i);job.Run();}else jobs[i]=job;}
                Fly(dt);Drift(dt);
                for(var i=particles.Count-1;i>=0;i--)
                {
                    var p=particles[i];
                    if(p.Delay>0){p.Delay-=dt;particles[i]=p;continue;}
                    p.Age+=dt;if(p.Age>=p.Life){particles[i]=particles[particles.Count-1];particles.RemoveAt(particles.Count-1);continue;}
                    p.Velocity+=Up*(p.Gravity*dt);p.Velocity*=Mathf.Max(0,1-p.Drag*dt);
                    p.Position+=p.Velocity*dt+Right*(p.Sway*Mathf.Sin((p.Age+p.Seed*7+i)*5)*dt*.35f);p.Rotation+=p.Spin*dt;particles[i]=p;
                }
            }
            Draw();
        }

        private void Fly(float dt)
        {
            for(var i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i];var before=Path(shot,shot.Age/shot.Life);shot.Age+=dt;var t=Mathf.Clamp01(shot.Age/shot.Life);var at=Path(shot,t);
                var velocity=(at-before)/Mathf.Max(dt,1e-4f);
                if(shot.Arrow)
                {
                    Emit(Streak,at,.62f,.62f,.035f,Color.white,Cream,light:1,velocity:velocity.normalized*.01f,facing:Facing.Velocity,aspect:1,fadeFrom:.9f);
                    shot.Trail+=dt;if(shot.Trail>.035f){shot.Trail=0;Emit(Petal,at,.11f,.05f,.45f,Leaf,With(Leaf,0),velocity:-velocity*.08f+Screen(Range(0,360),.5f),gravity:-1.5f,spin:Range(-8,8),rotation:Range(0,360));
                        Emit(Glow,at,.20f,.05f,.2f,With(shot.Tint,.8f),With(shot.Tint,0),light:1);}
                }
                else
                {
                    Emit(Glow,at,.46f,.46f,.035f,Color.white,shot.Tint,light:1,fadeFrom:.9f);
                    Emit(Sparkle,at,.36f,.30f,.035f,Color.white,Color.white,light:1,rotation:shot.Age*400,fadeFrom:.9f);
                    shot.Trail+=dt;if(shot.Trail>.016f){shot.Trail=0;
                        Emit(Glow,at+Screen(Range(0,360),.08f),.26f,.03f,.28f,With(shot.Tint,.9f),With(shot.Tint,0),light:1);
                        Emit(Shard,at+Screen(Range(0,360),.15f),.09f,.02f,.32f,Color.white,shot.Tint,light:1,velocity:-velocity*.12f+Screen(Range(0,360),.8f),spin:Range(-9,9),rotation:Range(0,360));}
                }
                if(t>=1)shots.RemoveAt(i);
            }
        }
        private Vector3 Path(Shot shot,float t){t=Mathf.Clamp01(t);var eased=t*t*(3-2*t)*.35f+t*.65f;return Vector3.Lerp(shot.From,shot.To,eased)+Up*(Mathf.Sin(t*Mathf.PI)*shot.Arc);}

        private void Drift(float dt)
        {
            if(!Ambient)return;ambientClock-=dt;if(ambientClock>0)return;ambientClock=Range(.22f,.5f);
            var at=AmbientCentre+Wide(Range(-AmbientRadius,AmbientRadius),Range(-AmbientRadius,AmbientRadius))+Up*Range(1,7);
            if(random.Next(3)==0)Emit(Glow,at,.07f,.11f,Range(3,5),With(Cream,.0f),With(Gold,.55f),light:1,velocity:Up*Range(.1f,.3f)+Right*Range(-.2f,.2f),sway:Range(-1,1),fadeIn:.3f,fadeFrom:.6f);
            else {var tint=random.Next(2)==0?Cream:new Color(1,.86f,.90f);
                Emit(Petal,at,.10f,.10f,Range(4,7),With(tint,.85f),With(tint,.85f),velocity:Screen(Range(195,235),Range(.5f,1.1f)),sway:Range(-2,2),spin:Range(-2.5f,2.5f),rotation:Range(0,360),fadeIn:.15f,fadeFrom:.8f);}
        }

        private void Draw()
        {
            vertices.Clear();colors.Clear();uv.Clear();mode.Clear();indices.Clear();
            var right=Right;var up=Up;var forward=View.transform.forward;
            foreach(var p in particles)
            {
                if(p.Delay>0)continue;
                var t=p.Age/p.Life;var size=Mathf.Lerp(p.Size0,p.Size1,1-(1-t)*(1-t));var color=Color.Lerp(p.Color0,p.Color1,t);
                var fade=(p.FadeIn>0?Mathf.Clamp01(t/p.FadeIn):1)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(p.FadeFrom,1,t)));color.a*=fade;
                if(color.a<=.002f||size<=0)continue;
                Vector3 x,y;
                if(p.Facing==Facing.Ground){x=Vector3.right;y=Vector3.forward;}
                else if(p.Facing==Facing.Velocity)
                {
                    var flat=p.Velocity-forward*Vector3.Dot(p.Velocity,forward);if(flat.sqrMagnitude<1e-8f)flat=right;
                    x=flat.normalized;y=Vector3.Cross(forward,x);
                }
                else {x=right;y=up;}
                var c=Mathf.Cos(p.Rotation);var s=Mathf.Sin(p.Rotation);var ax=(x*c+y*s)*size;var ay=(y*c-x*s)*size;
                // Aspect stretches the long axis for streaks, narrows pillars, and a negative value mirrors a crescent.
                if(p.Facing==Facing.Velocity)ax*=p.Aspect;else if(p.Aspect<0)ay=-ay;else ax*=p.Aspect;
                var centre=p.Position+(p.Facing==Facing.Ground?Vector3.up*.07f:Vector3.zero);var n=vertices.Count;
                vertices.Add(centre-ax-ay);vertices.Add(centre+ax-ay);vertices.Add(centre+ax+ay);vertices.Add(centre-ax+ay);
                var k=Mathf.Lerp(p.K0,p.K1,t);
                uv.Add(new Vector4(-1,-1,p.Shape,k));uv.Add(new Vector4(1,-1,p.Shape,k));uv.Add(new Vector4(1,1,p.Shape,k));uv.Add(new Vector4(-1,1,p.Shape,k));
                var m=new Vector2(p.Light,p.Seed);
                for(var q=0;q<4;q++){colors.Add(color);mode.Add(m);}
                indices.Add(n);indices.Add(n+1);indices.Add(n+2);indices.Add(n);indices.Add(n+2);indices.Add(n+3);
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetUVs(1,mode);mesh.SetTriangles(indices,0,false);
            mesh.bounds=new Bounds(View.transform.position,Vector3.one*400);
        }
    }
}
