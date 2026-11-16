using System.Collections.Generic;
using UnityEngine;

namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// Display-only life for a garden spirit: jelly breathing, blinks, a swaying crown, and short clips for the
    /// Core events being shown. The body is posed in the shader around the foot pivot; only the small face and
    /// crown parts move as transforms, so nothing other code measures (body mesh, pawn transform) changes.
    /// </summary>
    public sealed class HqCreatureAnim:MonoBehaviour
    {
        public readonly List<Material> Materials=new List<Material>();
        public readonly List<Transform> Eyes=new List<Transform>();
        public readonly List<Transform> Leaves=new List<Transform>();
        public Transform Crown,Pivot;public Camera View;public float Phase;public Color Tint=Color.white;
        public PuppetClip Clip{get;private set;}

        private static readonly int PivotId=Shader.PropertyToID("_CreaturePivot"),PoseId=Shader.PropertyToID("_CreaturePose"),RightId=Shader.PropertyToID("_CreatureRight"),
            FlashId=Shader.PropertyToID("_Flash"),DissolveId=Shader.PropertyToID("_Dissolve");
        private readonly List<Vector3> eyeScale=new List<Vector3>();private readonly List<Quaternion> leafRest=new List<Quaternion>();
        private Vector3 crownRest;private float time,clipTime,clipLength=1,clipSide,flash,blinkAt=2,blink;

        public void Capture()
        {
            foreach(var eye in Eyes)eyeScale.Add(eye.localScale);foreach(var leaf in Leaves)leafRest.Add(leaf.localRotation);
            if(Crown!=null)crownRest=Crown.localPosition;
        }
        /// <param name="side">Screen side of the action: +1 right, -1 left, 0 straight up or down the screen.</param>
        public void Play(PuppetClip clip,float length,float side){Clip=clip;clipTime=0;clipLength=Mathf.Max(.05f,length);clipSide=side;}
        public void Flash(float amount=1){flash=amount;}
        public void ResetPose(){Clip=PuppetClip.None;clipTime=0;flash=0;Apply(0,0,0,0,0);}

        public void Tick(float dt)
        {
            dt=Mathf.Min(dt,.05f);time+=dt;var t=time+Phase;
            // Idle: a slow jelly breath with a quicker shiver riding on it.
            var squash=.035f*Mathf.Sin(t*2.6f)+.008f*Mathf.Sin(t*7.3f);var lean=.02f*Mathf.Sin(t*1.3f);var lift=0f;var wobble=0f;var dissolve=0f;var crownBob=0f;
            if(Clip!=PuppetClip.None)
            {
                clipTime+=dt;var u=clipTime/clipLength;var post=clipTime-clipLength;
                switch(Clip)
                {
                    case PuppetClip.Attack:
                    case PuppetClip.Cast:
                    {
                        // Flatten and pull back, then spring at the target and land as the blow lands.
                        var gather=u<.6f?EaseOut(u/.6f):Mathf.Max(0,1-(u-.6f)/.15f);
                        var leap=u<.6f?0:u<1?Mathf.Sin((u-.6f)/.4f*Mathf.PI):0;
                        var land=u>=1?Mathf.Exp(-post*9)*Mathf.Cos(post*24):0;
                        squash+=.26f*gather-.24f*leap+.16f*land;lean+=clipSide*(-.20f*gather+.42f*leap);lift+=.34f*leap;wobble=land;crownBob=-.05f*gather+.08f*leap;
                        if(post>.5f)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Hit:
                    {
                        // Jelly: squashed by the blow, then ringing back to shape.
                        var k=Mathf.Exp(-clipTime*6.5f);var ring=Mathf.Cos(clipTime*23);
                        squash+=.30f*k*ring;lean+=clipSide*.38f*k*(.5f+.5f*ring);wobble=k;crownBob=.06f*k*ring;
                        if(clipTime>.75f)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Guard:
                    {
                        var brace=u<1?EaseOut(u/.4f):Mathf.Max(0,1-post/.25f);squash+=.14f*brace;
                        if(post>.26f)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Cheer:
                    {
                        var hop=Mathf.Abs(Mathf.Sin(Mathf.Clamp01(u)*Mathf.PI*2));lift+=.22f*hop;squash-=.12f*hop;
                        if(u>=1)Clip=PuppetClip.None;
                        break;
                    }
                    case PuppetClip.Fall:
                    {
                        // Swell, then come apart from the top down. Stays gone until ResetPose.
                        var e=Mathf.Clamp01(u);squash+=-.18f*Mathf.Sin(Mathf.Min(1,e*2.2f)*Mathf.PI)+.45f*e*e;wobble=1-e;
                        dissolve=Mathf.SmoothStep(0,1,(e-.25f)/.75f);
                        break;
                    }
                }
            }
            flash=Mathf.MoveTowards(flash,0,dt*7f);
            Apply(squash,lean,lift,wobble,dissolve);

            // Blink: quick close, slower open, now and then twice.
            blinkAt-=dt;if(blinkAt<=0){blink=.16f;blinkAt=2.2f+Mathf.Abs(Mathf.Sin(t*3.7f))*3.2f;}
            var lid=1f;if(blink>0){blink-=dt;var b=1-blink/.16f;lid=b<.35f?1-b/.35f:(b-.35f)/.65f;lid=Mathf.Max(.08f,lid);}
            if(Clip==PuppetClip.Hit||Clip==PuppetClip.Fall)lid=.18f; // eyes squeezed shut
            for(var i=0;i<Eyes.Count;i++){var s=eyeScale[i];Eyes[i].localScale=new Vector3(s.x*(1+(1-lid)*.25f),s.y*lid,s.z);}
            for(var i=0;i<Leaves.Count;i++)
                Leaves[i].localRotation=leafRest[i]*Quaternion.Euler(0,0,(7*Mathf.Sin(t*2.1f+i*1.4f)+wobble*14*Mathf.Sin(time*30))*(i%2==0?1:-1));
            // The crown already follows the body through the shared shader pose; it only adds its own small bob.
            if(Crown!=null)Crown.localPosition=crownRest+Vector3.up*crownBob;
        }

        private void Apply(float squash,float lean,float lift,float wobble,float dissolve)
        {
            var pivot=Pivot!=null?Pivot.position:transform.position;var right=View!=null?View.transform.right:Vector3.right;
            var flashColor=new Color(1,1,1,Mathf.Clamp01(flash)*.6f);
            foreach(var m in Materials)
            {
                if(m==null)continue;
                m.SetVector(PivotId,pivot);m.SetVector(PoseId,new Vector4(squash,lean,lift,wobble*Mathf.Sin(time*34)));m.SetVector(RightId,right);
                m.SetColor(FlashId,flashColor);m.SetFloat(DissolveId,dissolve);
            }
        }
        private static float EaseOut(float x){x=1-Mathf.Clamp01(x);return 1-x*x*x;}
    }
}
