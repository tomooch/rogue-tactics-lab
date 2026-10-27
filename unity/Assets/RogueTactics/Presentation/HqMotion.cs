using System.Collections;
using RogueTactics.Core.G1;
using UnityEngine;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private IEnumerator PresentEvent(BattleEvent e)
        {
            while(Paused)yield return null;
            if(!EventMotion){Display(e);Refresh();PopEventNumber(e);yield return PresentationDelay(e.Kind==EventKind.Action?.22f:.42f);yield break;}
            // Every motion is caused by this queued Core event; visible state changes at the impact/end boundary.
            if(e.Kind==EventKind.Action)
            {
                Display(e);Refresh();
                actionLink.gameObject.SetActive(false);ghost.gameObject.SetActive(false);
                var target=executingPlan.Kind==ActionKind.Move?Shown(executingPlan.Destination):executingPlan.Target!=""?positions[executingPlan.Target]:positions[e.Actor];
                if(executingPlan.Kind==ActionKind.Move)
                {actionLink.gameObject.SetActive(true);Link(actionLink,positions[e.Actor],target);}
                var route=actionLink.GetComponent<MobileRouteGraphic>();route.Progress=0;
                var elapsed=0f;while(elapsed<.38f){if(!Paused){elapsed+=Time.unscaledDeltaTime;route.Progress=Mathf.Clamp01(elapsed/.38f);}yield return null;}
                route.Progress=1;yield break;
            }
            var hasActor=pawns.TryGetValue(e.Actor,out var actor);var hasTarget=pawns.TryGetValue(e.Target,out var targetRoot);
            var original=hasActor?actor.position:Vector3.zero;
            var targetPosition=hasTarget?targetRoot.position:Vector3.zero;
            var duration=e.Kind==EventKind.Move?.52f:e.Kind==EventKind.Death?.36f:.46f;
            KickPoses(e,hasActor && hasTarget?original:targetPosition,targetPosition,duration);
            var elapsedMotion=0f;visualTweenActive=true;
            if(e.Kind==EventKind.Counter){reaction=HeroName(e.Actor)+" 反撃（割込）";RefreshProductHud();}
            while(elapsedMotion<duration)
            {
                if(!Paused)
                {
                    elapsedMotion+=Time.unscaledDeltaTime;var t=Mathf.Clamp01(elapsedMotion/duration);var pulse=Mathf.Sin(t*Mathf.PI);
                    if(e.Kind==EventKind.Move && hasActor)
                    {var destination=World(Shown(e.Position));actor.position=Vector3.Lerp(original,destination,t*t*(3-2*t))+Vector3.up*(pulse*.16f);}
                    if((e.Kind==EventKind.Damage || e.Kind==EventKind.Counter) && hasActor && hasTarget)
                    {
                        actor.position=original+(targetPosition-original).normalized*(pulse*.12f);
                        targetRoot.position=targetPosition+FieldCamera.transform.right*(Mathf.Sin(t*Mathf.PI*4)*.055f);
                    }
                    if(e.Kind==EventKind.Protect && hasActor)actor.position=original+Vector3.up*(pulse*.16f);
                    if(e.Kind==EventKind.Heal && hasTarget)targetRoot.localScale=Vector3.one*(1+pulse*.055f);
                    if(e.Kind==EventKind.Death && hasTarget)targetRoot.localScale=Vector3.one*(1-t*.65f);
                }
                // Geometry follows the visible pose while pause/resume retains the tween's exact phase.
                bodyRects.Clear();
                foreach(var unit in Session.Replay.State.Units)if(!dead.Contains(unit.Id))
                {bodyRects[unit.Id]=ProjectedBody(bodies[unit.Id]);}
                LayoutFootBadges();RefreshRelationCues();
                if(guardLink.gameObject.activeSelf)Link(guardLink,positions["B"],positions["A"]);
                if(actionLink.gameObject.activeSelf && hasActor && hasTarget)Link(actionLink,positions[e.Actor],positions[e.Target]);
                yield return null;
            }
            visualTweenActive=false;
            if(hasActor)actor.position=original;if(hasTarget){targetRoot.position=targetPosition;targetRoot.localScale=Vector3.one;}
            Display(e);Refresh();PopEventNumber(e);
            if(e.Kind==EventKind.Damage || e.Kind==EventKind.Counter || e.Kind==EventKind.Heal || e.Kind==EventKind.Protect)
            {
                var targetId=e.Target;var renderer=bodies[targetId];var material=renderer.sharedMaterial;var before=material.color;
                var tint=e.Kind==EventKind.Heal?new Color(.45f,1,.76f):e.Kind==EventKind.Protect?new Color(.5f,.85f,1):new Color(1,.68f,.57f);
                // Multiplicative light flash only. The approved source texture RGB is never rewritten.
                var elapsed=0f;while(elapsed<.22f){if(!Paused){elapsed+=Time.unscaledDeltaTime;material.color=Color.Lerp(tint,before,elapsed/.22f);}yield return null;}material.color=before;
            }
            yield return PresentationDelay(.14f);
        }
        private IEnumerator PresentationDelay(float duration)
        {var t=0f;while(t<duration){if(!Paused)t+=Time.unscaledDeltaTime;yield return null;}}
    }
}
