using System.Collections.Generic;
using RogueTactics.Core.G1;
using RogueTactics.Presentation.Gfx;
using UnityEngine;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        // Display-only life for the cast. Each ally is its single approved image moved as a soft puppet
        // (HqPuppet); each garden spirit is posed in its shader (HqCreatureAnim); HqVfx draws what passes
        // between them. All of it is caused by the queued Core event being shown and none of it changes
        // Core state, pawn transforms, renderer bounds or any projected rect.
        private sealed class Pose
        {
            public Material Skin;public HqPuppet Puppet;public Transform Quad;
            public Vector3 Last;public bool Placed,Mirrored,Hidden;public float Stride,Stepping;
        }
        private readonly Dictionary<string,Pose> poses=new Dictionary<string,Pose>();
        private readonly Dictionary<string,HqCreatureAnim> spirits=new Dictionary<string,HqCreatureAnim>();
        private readonly HashSet<string> hiddenSpirits=new HashSet<string>();
        // A fallen ally's pawn is hidden by the projection (it is no longer measured); this dimmed, unmeasured copy
        // of its last pose keeps them lying on their cell until they stand again.
        private readonly Dictionary<string,GameObject> fallenRest=new Dictionary<string,GameObject>();
        private static readonly Color RestTint=new Color(.84f,.86f,.94f,1f);
        // A won encounter lifts each resting ally back onto their feet (design/specs/01 5.1: they rejoin the next fight).
        // The copy gets its own puppet for this; the measured pawn stays hidden as the projection left it.
        private readonly Dictionary<string,HqPuppet> rising=new Dictionary<string,HqPuppet>();
        private const float RiseSeconds=.9f;
        private readonly List<Mesh> puppetMeshes=new List<Mesh>();
        private HqVfx vfx;private float hitStop;private string actingId="";
        public bool PoseMirrored(string id)=>poses.TryGetValue(id,out var pose) && pose.Mirrored;
        /// <summary>Live effect quads; lets tests see that an event really drew something.</summary>
        public int EffectCount=>vfx!=null?vfx.Alive:0;

        // Called once per ally when the stage is built: swap the plain quad for the same-sized puppet grid.
        private void RigPuppet(string id,int index,GameObject quad,Texture2D art)
        {
            var shader=Resources.Load<Shader>("Gfx/HqPuppet");
            var pose=new Pose{Quad=quad.transform,Skin=quad.GetComponent<Renderer>().sharedMaterial};poses[id]=pose;
            if(shader==null)return;
            var rig=HqPuppet.Authored(index);puppetMeshes.Add(rig.Mesh);
            var skin=new Material(shader){name="Puppet skin "+id,mainTexture=art};materials.Add(skin);
            quad.GetComponent<MeshFilter>().sharedMesh=rig.Mesh;quad.GetComponent<Renderer>().sharedMaterial=skin;
            pose.Skin=skin;pose.Puppet=new HqPuppet(rig,skin,index*1.7f);
        }
        private void BuildEffects()
        {
            vfx=HqVfx.Create(FieldCamera,()=>Paused);if(vfx==null)return;
            vfx.AmbientCentre=BattleCenter();vfx.Ambient=HqWorld;
            foreach(var pair in pawns){var spirit=pair.Value.GetComponent<HqCreatureAnim>();if(spirit!=null)spirits[pair.Key]=spirit;}
        }

        private Pose PoseOf(string id)=>id!=null && poses.TryGetValue(id,out var pose)?pose:null;
        // Screen-horizontal side of a world direction: +1 right, -1 left, 0 when straight up/down the screen.
        private float ScreenSide(Vector3 direction){var x=Vector3.Dot(direction,FieldCamera.transform.right);return Mathf.Abs(x)<.05f?0:Mathf.Sign(x);}
        private void Face(Pose pose,float side){if(side!=0)pose.Mirrored=side>0;}
        private static Color TintOf(string id)=>id=="A"?HqVfx.Azure:id=="B"?HqVfx.Rose:id=="C"?HqVfx.Steel:id=="D"?HqVfx.Leaf:HqCreature.TintOf(id);
        private Vector3 Ground(string id)=>pawns[id].position;
        // Where a blow lands on, or leaves, a body.
        private Vector3 Chest(string id)
        {
            if(poses.ContainsKey(id))return pawns[id].position+Vector3.up*.12f+FieldCamera.transform.up*1.25f;
            return bodies.TryGetValue(id,out var body)?body.bounds.center:pawns[id].position+Vector3.up*.6f;
        }
        // How far a melee attacker is carried toward its target: it stops a body's width short of it.
        private Vector3 Lunge(Vector3 from,Vector3 to)
        {var d=to-from;d.y=0;var length=d.magnitude;return length<.01f?Vector3.zero:d/length*Mathf.Clamp(length-1.05f,0,2.8f);}
        private Vector2 OnScreen(Vector3 world)=>new Vector2(Vector3.Dot(world,FieldCamera.transform.right),Vector3.Dot(world,FieldCamera.transform.up));
        private Vector3 Muzzle(string id){var pose=PoseOf(id);return pose?.Puppet!=null?pose.Puppet.Muzzle(pose.Quad):Chest(id);}

        // The start of a shown Core event: choose the wind-up and schedule what flies before it lands.
        private void KickPoses(BattleEvent e,Vector3 actorAt,Vector3 targetAt,float duration)
        {
            var side=ScreenSide(targetAt-actorAt);var along=side!=0?1f:0f;
            var actor=PoseOf(e.Actor);spirits.TryGetValue(e.Actor??"",out var actorSpirit);
            actingId=e.Kind==EventKind.Death?"":e.Actor;
            switch(e.Kind)
            {
                case EventKind.Damage:
                case EventKind.Counter:
                    var lunge=Lunge(actorAt,targetAt);
                    if(actor!=null)
                    {
                        Face(actor,side);
                        if(actor.Puppet!=null)
                        {
                            var style=actor.Puppet.Skeleton.Style;var melee=style==PuppetStyle.Blade || style==PuppetStyle.Guardian;
                            actor.Puppet.Dash=melee?OnScreen(lunge)/Mathf.Max(.01f,actor.Quad.lossyScale.y):Vector2.zero;
                            actor.Puppet.Play(PuppetClip.Attack,duration,along);
                        }
                    }
                    if(actorSpirit!=null){actorSpirit.Dash=lunge;actorSpirit.Play(PuppetClip.Attack,duration,side);}
                    if(vfx==null || !pawns.ContainsKey(e.Target))break;
                    if(e.Kind==EventKind.Counter)vfx.Reaction(Chest(e.Actor)+FieldCamera.transform.up*.9f);
                    var tint=TintOf(e.Actor);var to=Chest(e.Target);var id=e.Actor;
                    if(actorSpirit!=null)vfx.After(duration*.62f,()=>vfx.Swoosh(Chest(id),to,tint));
                    else switch(actor?.Puppet?.Skeleton.Style)
                    {
                        case PuppetStyle.Caster:
                            vfx.CastCircle(Ground(id),tint,duration);vfx.Charge(Muzzle(id),tint,duration*.64f);
                            vfx.After(duration*.66f,()=>vfx.Shoot(Muzzle(id),to,duration*.34f,tint,false));break;
                        case PuppetStyle.Archer:
                            vfx.Charge(Muzzle(id),tint,duration*.6f);
                            vfx.After(duration*.68f,()=>vfx.Shoot(Muzzle(id),to,duration*.32f,tint,true));break;
                        case PuppetStyle.Blade:
                            vfx.After(duration*.74f,()=>vfx.Slash(to,side>=0?0:180,tint,2.0f,true));break;
                        default:
                            vfx.After(duration*.72f,()=>vfx.Swoosh(Muzzle(id),to,tint));break;
                    }
                    break;
                case EventKind.Protect:
                    if(actor!=null){Face(actor,side);actor.Puppet?.Play(PuppetClip.Guard,duration,along);}
                    actorSpirit?.Play(PuppetClip.Guard,duration,side);
                    if(vfx!=null && pawns.ContainsKey(e.Target)){var ward=e.Target;vfx.After(duration*.3f,()=>vfx.Shield(Chest(ward),new Color(.55f,.86f,1f),false));}
                    break;
                case EventKind.Heal:
                    if(actor!=null){Face(actor,side);actor.Puppet?.Play(PuppetClip.Cast,duration,along);}
                    actorSpirit?.Play(PuppetClip.Cast,duration,side);
                    if(vfx!=null){vfx.CastCircle(Ground(e.Actor),HqVfx.Jade,duration);vfx.Charge(Muzzle(e.Actor),HqVfx.Jade,duration*.8f);}
                    break;
                case EventKind.Death:
                {
                    var fallen=PoseOf(e.Target);var away=side!=0?side:fallen!=null && fallen.Mirrored?-1:1;
                    if(fallen?.Puppet!=null)
                    {
                        // Tip toward the side of the screen with more room, so the resting figure does not lie across a neighbour.
                        var screenSide=FreeSide(e.Target,away);
                        fallen.Puppet.Lean=screenSide*(fallen.Mirrored?-1:1);fallen.Puppet.Play(PuppetClip.Fall,duration,1);
                    }
                    if(spirits.TryGetValue(e.Target,out var spirit))spirit.Play(PuppetClip.Fall,duration,away);
                    // A spirit bursts and is gone; a companion is only knocked down (LandPoses shows the landing).
                    if(vfx!=null && spirit!=null){var gone=e.Target;vfx.After(duration*.45f,()=>vfx.Vanish(Chest(gone),TintOf(gone),true));}
                    break;
                }
                case EventKind.Move:
                    actorSpirit?.Play(PuppetClip.Cheer,duration,side);
                    if(vfx!=null && pawns.ContainsKey(e.Actor))vfx.Dust(Ground(e.Actor));
                    break;
                case EventKind.End:
                    if(e.Detail==Outcome.Victory.ToString())
                    {
                        if(vfx!=null)vfx.Celebrate(BattleCenter());
                        foreach(var pair in poses)if(pawns[pair.Key].gameObject.activeInHierarchy)pair.Value.Puppet?.Play(PuppetClip.Cheer,.7f,0);
                        foreach(var resting in new List<string>(fallenRest.Keys))RaiseFallen(resting);
                    }
                    break;
            }
        }

        // The instant the shown event changes what the player sees (HP, guard): the blow lands, the light arrives.
        private void RestFallen(string id)
        {
            var pose=PoseOf(id);if(pose?.Puppet==null || pose.Quad==null || fallenRest.ContainsKey(id))return;
            var holder=pawns[id].parent;var copy=new GameObject("Fallen rest "+id,typeof(MeshFilter),typeof(MeshRenderer));
            copy.transform.SetParent(holder,false);copy.transform.SetPositionAndRotation(pose.Quad.position,pose.Quad.rotation);
            var parentScale=holder!=null?holder.lossyScale:Vector3.one;var s=pose.Quad.lossyScale;
            copy.transform.localScale=new Vector3(s.x/parentScale.x,s.y/parentScale.y,s.z/parentScale.z);
            copy.GetComponent<MeshFilter>().sharedMesh=pose.Quad.GetComponent<MeshFilter>().sharedMesh;
            var skin=new Material(pose.Skin){name="Fallen rest "+id};materials.Add(skin);
            // Arrays set at run time are not carried by the Material copy; without the bones every vertex collapses.
            skin.SetVectorArray("_BoneX",pose.Skin.GetVectorArray("_BoneX"));skin.SetVectorArray("_BoneY",pose.Skin.GetVectorArray("_BoneY"));
            skin.SetFloat("_Mirror",pose.Skin.GetFloat("_Mirror"));
            skin.SetFloat("_Dissolve",0);skin.SetColor("_Color",RestTint);
            var aura=skin.GetColor("_Aura");aura.a=0;skin.SetColor("_Aura",aura);var flash=skin.GetColor("_Flash");flash.a=0;skin.SetColor("_Flash",flash);
            var r=copy.GetComponent<MeshRenderer>();r.sharedMaterial=skin;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            fallenRest[id]=copy;
        }
        private void ClearAllFallen(){foreach(var id in new List<string>(fallenRest.Keys))ClearFallen(id);}
        private void ClearFallen(string id)
        {if(fallenRest.TryGetValue(id,out var copy)){if(copy!=null)Destroy(copy);fallenRest.Remove(id);}rising.Remove(id);}
        private void RaiseFallen(string id)
        {
            var pose=PoseOf(id);if(pose?.Puppet==null || rising.ContainsKey(id) || !fallenRest.TryGetValue(id,out var copy) || copy==null)return;
            // Someone now stands on the resting ally's cell, so the copy is hidden: rising there would put the mending light
            // at the other figure's feet and read as if they were healed. They simply rejoin at the next encounter.
            if(!copy.activeSelf)return;
            // Only where a lost companion really comes back. In the floor flow (ExplorationEnabled) Core keeps them out of the
            // party after the fight (FloorExplorationFlowTests.AttackHandoffDoesNotReviveOrMoveDeadCompanion), so they stay down
            // until a retry or a later revival rule (design/specs/01 5.1, not yet in Core) says otherwise.
            if(ExplorationEnabled)return;
            var skin=copy.GetComponent<MeshRenderer>().sharedMaterial;
            var puppet=new HqPuppet(pose.Puppet.Skeleton,skin,0){Mirrored=skin.GetFloat("_Mirror")>.5f,Lean=pose.Puppet.Lean};
            puppet.Play(PuppetClip.Rise,RiseSeconds,1);puppet.Tick(0,0,0,0);rising[id]=puppet;
            if(vfx!=null)vfx.After(RiseSeconds*.7f,()=>{if(fallenRest.ContainsKey(id))vfx.Heal(Ground(id));});
        }
        // The copy regains its colour as it stands.
        private void TickRising(float dt)
        {
            foreach(var pair in rising)
            {
                var puppet=pair.Value;puppet.Tick(dt,0,0,0);
                var up=puppet.Clip==PuppetClip.Rise?Mathf.Clamp01(puppet.ClipTime/RiseSeconds):1;
                puppet.Skin.SetColor("_Color",Color.Lerp(RestTint,Color.white,up));
            }
        }

        // Which screen side a fallen companion tips toward: +1 right, -1 left. A body lying at ground level hides whoever
        // stands just behind it (higher on screen), while one in front merely hides the fallen; so a neighbour behind weighs
        // 2 and one in front 1, and the lighter side wins. Anyone straight above or below is on neither side. Ties go to `prefer`.
        private float FreeSide(string id,float prefer)
        {
            var at=pawns[id].position;var right=FieldCamera.transform.right;var up=FieldCamera.transform.up;int left=0,rightWeight=0;
            foreach(var other in pawns)
            {
                if(other.Key==id || !other.Value.gameObject.activeInHierarchy)continue;
                var d=other.Value.position-at;var dx=Vector3.Dot(d,right);var dy=Vector3.Dot(d,up);
                if(Mathf.Abs(dx)<Cell*.3f || Mathf.Abs(dx)>Cell*1.2f || Mathf.Abs(dy)>Cell*.9f)continue;
                var weight=dy>0?2:1;if(dx>0)rightWeight+=weight;else left+=weight;
            }
            return rightWeight<left?1:left<rightWeight?-1:(prefer>=0?1:-1);
        }

        private void LandPoses(BattleEvent e,Vector3 actorAt,Vector3 targetAt)
        {
            actingId="";
            if(e.Kind==EventKind.Death && poses.ContainsKey(e.Target??""))
            {
                RestFallen(e.Target);
                if(vfx!=null)
                {
                    var pose=PoseOf(e.Target);var lean=pose.Puppet!=null?pose.Puppet.Lean*(pose.Mirrored?-1:1):1;
                    var ground=Ground(e.Target);vfx.KnockDown(ground+FieldCamera.transform.right*(.35f*lean),ground+FieldCamera.transform.right*(1.1f*lean)+FieldCamera.transform.up*.25f,TintOf(e.Target));
                }
            }
            if(!pawns.TryGetValue(e.Target??"",out var targetRoot) || !targetRoot.gameObject.activeInHierarchy)return;
            var side=ScreenSide(targetAt-actorAt);var target=PoseOf(e.Target);spirits.TryGetValue(e.Target,out var spirit);
            if(e.Kind==EventKind.Damage || e.Kind==EventKind.Counter)
            {
                var guarded=e.Detail!=null && e.Detail.StartsWith("Guard");
                // The target turns to the blow and is thrown back from it.
                var thrown=targetAt-actorAt;thrown.y=0;thrown=thrown.sqrMagnitude<1e-4f?Vector3.zero:thrown.normalized;
                if(target?.Puppet!=null)target.Puppet.Knock=OnScreen(thrown).normalized*(guarded?.45f:1);
                if(spirit!=null)spirit.Knock=thrown;
                var attackerStyle=PoseOf(e.Actor)?.Puppet?.Skeleton.Style;
                // A spirit ramming a companion: the blow lands lower and the body flash is softer, so the face stays readable.
                var rammed=!guarded && target?.Puppet!=null && attackerStyle!=PuppetStyle.Archer && attackerStyle!=PuppetStyle.Caster && attackerStyle!=PuppetStyle.Blade;
                if(target!=null){Face(target,-side);target.Puppet?.Play(PuppetClip.Hit,.5f,side!=0?1:0);target.Puppet?.Flash(guarded?new Color(.75f,.92f,1f):new Color(1f,.93f,.90f),guarded?.6f:rammed?.55f:1);}
                if(spirit!=null){spirit.Play(PuppetClip.Hit,.5f,side);spirit.Flash(.25f);}
                if(vfx!=null)
                {
                    if(guarded){vfx.Shield(Chest(e.Target),new Color(.55f,.86f,1f),true);vfx.Impact(Chest(e.Target),TintOf(e.Actor),.55f);}
                    else
                    {
                        // Each way of fighting lands differently, so the eye can tell who struck without reading.
                        // A spirit's eyes sit at its body's centre and its pale body whitens fast, so blows land on its lower body
                        // and its face stays readable (S1-2, watchdog §5.4.1).
                        var at=spirit!=null?Vector3.Lerp(Chest(e.Target),Ground(e.Target),.4f):Chest(e.Target);var tint=TintOf(e.Actor);var white=spirit!=null?.45f:1f;var power=e.Kind==EventKind.Counter?.85f:1;
                        // Shape tells them apart at phone size: blade = a line, magic = a coloured rune, arrow = a thin
                        // shaft, blunt (and every spirit) = the round white burst with a ring over the floor.
                        if(attackerStyle==PuppetStyle.Archer){vfx.Impact(at,tint,power*.6f,0,0,white);vfx.Pierce(at,targetAt-actorAt,tint);}
                        else if(attackerStyle==PuppetStyle.Caster){vfx.Impact(at,tint,power*.8f,0,0,white);vfx.Blast(at,tint,spirit!=null?.5f:1);}
                        else if(attackerStyle==PuppetStyle.Blade){vfx.Impact(at,tint,power*.8f,.3f,0,white);vfx.Cut(at,side>=0?-28:208,tint);}
                        // A companion's face must stay readable while it is hit (watchdog S1-13), so a ram on a companion
                        // keeps the round burst and floor ring but with fewer, softer white rays over the head.
                        else if(rammed){vfx.Impact(Vector3.Lerp(at,Ground(e.Target),.35f),tint,power*.85f,.45f,.55f);vfx.Slam(Ground(e.Target),tint);}
                        else if(spirit!=null){vfx.Impact(at,tint,power*.85f,.6f,.5f,white);vfx.Slam(Ground(e.Target),tint);}
                        else {vfx.Impact(at,tint,power);vfx.Slam(Ground(e.Target),tint);}
                    }
                }
                hitStop=guarded?.06f:.10f;
            }
            if(e.Kind==EventKind.Heal)
            {
                target?.Puppet?.Play(PuppetClip.Cheer,.55f,0);target?.Puppet?.Flash(new Color(.72f,1f,.80f),.55f);
                if(spirit!=null){spirit.Play(PuppetClip.Cheer,.55f,0);}
                if(vfx!=null)vfx.Heal(Ground(e.Target));
            }
        }

        private void AnimatePoses()
        {
            if(FieldCamera==null)return;
            var frame=Time.unscaledDeltaTime;var dt=Paused?0:frame;
            // Hit-stop: the cast holds its pose for a few frames when a blow lands.
            if(hitStop>0){hitStop-=frame;dt=0;}
            var right=FieldCamera.transform.right;
            TickRising(dt);
            foreach(var pair in pawns)
            {
                if(spirits.TryGetValue(pair.Key,out var spirit))
                {
                    // A defeated spirit is hidden by the projection; whenever it is shown again it is whole.
                    if(!pair.Value.gameObject.activeInHierarchy){hiddenSpirits.Add(pair.Key);continue;}
                    if(hiddenSpirits.Remove(pair.Key))spirit.ResetPose();
                    spirit.Tick(dt);continue;
                }
                var pose=PoseOf(pair.Key);if(pose==null || pose.Skin==null)continue;
                // A resting ally makes way when a living one stands on the same cell.
                if(fallenRest.TryGetValue(pair.Key,out var resting) && resting!=null)
                {
                    var covered=false;foreach(var other in pawns)if(other.Key!=pair.Key && other.Value.gameObject.activeInHierarchy && Vector3.Distance(other.Value.position,pair.Value.position)<Cell*.5f)covered=true;
                    resting.SetActive(!covered);
                }
                // A fallen ally is hidden by the projection; whenever it is shown again it starts upright.
                if(!pair.Value.gameObject.activeInHierarchy){pose.Hidden=true;pose.Placed=false;continue;}
                if(pose.Hidden){pose.Hidden=false;pose.Puppet?.Reset();}
                ClearFallen(pair.Key);
                var at=pair.Value.position;var delta=at-pose.Last;pose.Last=at;
                // A reset or retry teleports pawns; only a short per-frame step counts as walking.
                var walked=pose.Placed && delta.magnitude<Cell*.6f?new Vector3(delta.x,0,delta.z).magnitude:0;pose.Placed=true;
                var before=pose.Stride;
                if(walked>.002f){Face(pose,ScreenSide(delta));pose.Stride+=walked*3.1f;}
                pose.Stepping=Mathf.MoveTowards(pose.Stepping,walked>.002f?1:0,frame*(walked>.002f?12:5));
                // Each footfall lifts a little dust.
                if(vfx!=null && walked>.002f && Mathf.Floor(pose.Stride/Mathf.PI)!=Mathf.Floor(before/Mathf.PI))vfx.Dust(at,.7f);
                if(pose.Puppet==null)continue;
                pose.Puppet.Mirrored=pose.Mirrored;pose.Puppet.Aura=actingId==pair.Key?1:0;
                // Screen speed in image heights per second, in the art's own (unmirrored) frame.
                var speed=frame>0 && walked>0?Vector3.Dot(delta,right)/frame/3f:0;if(pose.Mirrored)speed=-speed;
                pose.Puppet.Tick(dt,speed,pose.Stepping,pose.Stride);
            }
        }
    }
}
