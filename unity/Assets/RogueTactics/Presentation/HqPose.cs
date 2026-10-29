using System.Collections.Generic;
using RogueTactics.Core.G1;
using UnityEngine;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        // Display-only motion for the single approved image of each ally: facing, breath, steps,
        // and short poses caused by queued Core events. Only shader vertex offsets change, so Core
        // state, pawn transforms, renderer bounds and every projected rect stay exactly as before.
        private sealed class Pose
        {
            public Material Skin;public Vector3 Last;public bool Placed;public bool Mirrored;public float Stride,Stepping;
            public EventKind Kick;public float KickAt=-1,KickLength,KickSide;
        }
        private readonly Dictionary<string,Pose> poses=new Dictionary<string,Pose>();
        private static readonly int PoseId=Shader.PropertyToID("_Pose"),TiltId=Shader.PropertyToID("_Tilt");
        public bool PoseMirrored(string id)=>poses.TryGetValue(id,out var pose) && pose.Mirrored;

        private Pose PoseOf(string id)
        {
            if(poses.TryGetValue(id,out var pose))return pose;
            if(!bodies.TryGetValue(id,out var body) || body.name!="Private approved static skin")return null;
            return poses[id]=new Pose{Skin=body.sharedMaterial};
        }
        // Screen-horizontal side of a world direction: +1 right, -1 left, 0 when straight up/down the screen.
        private float ScreenSide(Vector3 direction){var x=Vector3.Dot(direction,FieldCamera.transform.right);return Mathf.Abs(x)<.05f?0:Mathf.Sign(x);}
        private void Face(Pose pose,float side){if(side!=0)pose.Mirrored=side>0;}

        private void KickPoses(BattleEvent e,Vector3 actorAt,Vector3 targetAt,float duration)
        {
            // side: where the motion leans or falls; face: which way the ally turns first.
            void Kick(string id,EventKind kind,float side,float face)
            {var pose=id==null?null:PoseOf(id);if(pose==null)return;pose.Kick=kind;pose.KickAt=0;pose.KickLength=duration;pose.KickSide=side;Face(pose,face);}
            var side=ScreenSide(targetAt-actorAt);
            // The striker leans into the target; the target faces the striker and is knocked away from it.
            if(e.Kind==EventKind.Damage || e.Kind==EventKind.Counter){Kick(e.Actor,EventKind.Damage,side,side);Kick(e.Target,EventKind.Counter,side,-side);}
            if(e.Kind==EventKind.Protect)Kick(e.Actor,EventKind.Protect,side,side);
            if(e.Kind==EventKind.Heal){Kick(e.Actor,EventKind.Action,side,side);if(e.Target!=e.Actor)Kick(e.Target,EventKind.Heal,0,0);}
            if(e.Kind==EventKind.Death){var pose=PoseOf(e.Target);Kick(e.Target,EventKind.Death,side!=0?side:pose!=null && pose.Mirrored?-1:1,0);}
        }

        private void AnimatePoses()
        {
            if(FieldCamera==null)return;
            var dt=Paused?0:Time.unscaledDeltaTime;var now=Time.unscaledTime;
            foreach(var pair in pawns)
            {
                var pose=PoseOf(pair.Key);if(pose==null || pose.Skin==null)continue;
                // A fallen ally is hidden by the projection; whenever it is shown again it starts upright.
                if(!pair.Value.gameObject.activeInHierarchy){if(pose.Kick==EventKind.Death)pose.KickAt=-1;pose.Placed=false;continue;}
                var at=pair.Value.position;var delta=at-pose.Last;pose.Last=at;
                // A reset or retry teleports pawns; only a short per-frame step counts as walking.
                var walked=pose.Placed && delta.magnitude<Cell*.6f?new Vector3(delta.x,0,delta.z).magnitude:0;pose.Placed=true;
                if(walked>.002f){Face(pose,ScreenSide(delta));pose.Stride+=walked*3.1f;}
                pose.Stepping=Mathf.MoveTowards(pose.Stepping,walked>.002f?1:0,Time.unscaledDeltaTime*(walked>.002f?12:5));
                var facing=pose.Mirrored?1:-1;
                // Breath: a slow, slightly offset swell per ally, always from the feet.
                var squash=.012f*Mathf.Sin(now*2.3f+pair.Key[0]*1.7f);var lean=0f;var lift=0f;var tilt=0f;
                // Steps: a small bounce per cell-third with a forward lean into the walk.
                var bounce=Mathf.Abs(Mathf.Sin(pose.Stride));
                lift+=pose.Stepping*bounce*.035f;squash+=pose.Stepping*(.045f-bounce*.075f);lean+=pose.Stepping*facing*.07f;
                if(pose.KickAt>=0)
                {
                    pose.KickAt+=dt;var t=Mathf.Clamp01(pose.KickAt/Mathf.Max(.01f,pose.KickLength));var s=pose.KickSide;
                    if(pose.Kick==EventKind.Damage)
                    {
                        // Wind up away from the target, then strike into it and settle.
                        var wind=t<.3f?Mathf.Sin(t/.3f*Mathf.PI*.5f):Mathf.Max(0,1-(t-.3f)/.12f);var strike=t<.3f?0:Mathf.Sin(Mathf.Clamp01((t-.3f)/.7f)*Mathf.PI);
                        lean+=s*(strike*.24f-wind*.10f);squash+=wind*.06f-strike*.07f;
                    }
                    if(pose.Kick==EventKind.Counter)
                    {
                        // Hit: knocked back at the impact, recovering with a short wobble.
                        var u=Mathf.Clamp01((t-.35f)/.65f);var hit=t<.35f?0:(1-u)*(1-u);
                        lean+=s*hit*(.17f+.05f*Mathf.Sin(u*Mathf.PI*5));squash+=hit*.09f;
                    }
                    if(pose.Kick==EventKind.Protect){var p=Mathf.Sin(t*Mathf.PI);lift+=p*.04f;squash-=p*.06f;lean+=s*p*.06f;}
                    if(pose.Kick==EventKind.Action){var p=Mathf.Sin(t*Mathf.PI);squash-=p*.05f;lift+=p*.02f;}
                    if(pose.Kick==EventKind.Heal)lift+=Mathf.Abs(Mathf.Sin(t*Mathf.PI*2))*.05f;
                    if(pose.Kick==EventKind.Death){var e=t*t;tilt=-s*1.2f*e;squash+=.10f*e;}
                    if(t>=1 && pose.Kick!=EventKind.Death)pose.KickAt=-1;
                }
                pose.Skin.SetVector(PoseId,new Vector4(pose.Mirrored?1:0,lean,squash,lift));pose.Skin.SetFloat(TiltId,tilt);
            }
        }
    }
}
