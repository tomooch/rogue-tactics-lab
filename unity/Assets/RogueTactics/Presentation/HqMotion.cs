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
            // Long enough to read the wind-up; the event still lands exactly at the end of this tween.
            var duration=e.Kind==EventKind.Move?.52f:e.Kind==EventKind.Death?.72f:e.Kind==EventKind.Damage?.62f:e.Kind==EventKind.Heal?.60f:e.Kind==EventKind.End?.2f:.50f;
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
                    // Strikes, guards, mending and falls are posed by the puppets and spirits (HqPose); pawns stay on their cells.
                }
                // Geometry follows the visible pose while pause/resume retains the tween's exact phase.
                bodyRects.Clear();
                foreach(var unit in Session.Replay.State.Units)if(!dead.Contains(unit.Id))
                {bodyRects[unit.Id]=ProjectedBody(bodies[unit.Id]);}
                LayoutFootBadges();RefreshRelationCues();
                RefreshShownGuard();
                if(actionLink.gameObject.activeSelf && hasActor && hasTarget)Link(actionLink,positions[e.Actor],positions[e.Target]);
                yield return null;
            }
            visualTweenActive=false;
            if(hasActor)actor.position=original;if(hasTarget){targetRoot.position=targetPosition;targetRoot.localScale=Vector3.one;}
            Display(e);Refresh();PopEventNumber(e);
            LandPoses(e,hasActor && hasTarget?original:targetPosition,targetPosition);
            // Leave the reaction (recoil, flash, burst) on screen before the next event starts.
            if(e.Kind==EventKind.Damage || e.Kind==EventKind.Counter || e.Kind==EventKind.Heal || e.Kind==EventKind.Protect)yield return PresentationDelay(.26f);
            yield return PresentationDelay(.14f);
        }
        private IEnumerator PresentationDelay(float duration)
        {var t=0f;while(t<duration){if(!Paused)t+=Time.unscaledDeltaTime;yield return null;}}
    }
}
