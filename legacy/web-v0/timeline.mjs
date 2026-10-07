// A pure projection of confirmed events. No battle decisions are made here.
export function beatDuration(run,index) {
  const next=run.events[index+1];
  return next?Math.min(520,(next.timestamp-run.events[index].timestamp)*.7):520;
}
export function playbackEnd(run) {return run.duration+beatDuration(run,run.events.length-1);}
const mix=(a,b,p)=>a+(b-a)*Math.max(0,Math.min(1,p));
export function project(run,time) {
  let index=-1;
  for(let i=0;i<run.events.length&&run.events[i].timestamp<=time;i++)index=i;
  if(index<0)return {state:structuredClone(run.initial),event:null,index,phase:1,trail:null,dead:false};
  const event=run.events[index],elapsed=time-event.timestamp,phase=Math.min(1,elapsed/beatDuration(run,index));
  if(phase>=1)return {state:structuredClone(event.after),event,index,phase,trail:null,dead:event.died};
  const state=structuredClone(event.before);
  for(const attack of event.attacks)state.enemies[attack.target].hp-=attack.damage;
  state.hp=Math.max(0,event.hpAfterDamage);state.gauge=event.gaugeAfterDamage;
  if(phase>=.19 && event.counterTriggered) {
    event.counterTargets.forEach((id,i)=>state.enemies[id].hp-=event.counterDamage[i]);
    state.gauge=event.after.gauge;
  }
  if(phase>=.42)state.hp=mix(Math.max(0,event.hpAfterDamage),Math.max(0,event.hpAfterLifesteal),(phase-.42)/.31);
  if(phase>=.73) {
    state.hp=mix(Math.max(0,event.hpAfterLifesteal),Math.max(0,event.hpAfterAll),(phase-.73)/.27);
    state.healUses=event.after.healUses;
  }
  const trail=event.totalDamage>0&&elapsed<Math.min(300,beatDuration(run,index))?
    {from:Math.max(0,event.hpAfterDamage),to:event.hpBefore,opacity:1-elapsed/Math.min(300,beatDuration(run,index))}:null;
  return {state,event,index,phase,trail,dead:event.died};
}
