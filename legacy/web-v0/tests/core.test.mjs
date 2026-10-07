import test from 'node:test';
import assert from 'node:assert/strict';
import {simulate,CONDITIONS} from '../core.mjs';
import {project,beatDuration,playbackEnd} from '../timeline.mjs';
const expected={base:[[true,8,63.8,1],[true,11.2,62,1]],companion:[[true,8,38.8,0],[true,11.2,42.45,0]],growth:[[true,8,83.8,1],[true,11.2,82,1]],danger:[[false,5.4,0,1],[true,11.2,22,2]]};
for(const key of Object.keys(CONDITIONS))for(const [i,tactic] of ['focus','split'].entries()){
 test(`${key}/${tactic}: provided fixture and deterministic replay`,()=>{
  const r=simulate(key,tactic),v=r.result;assert.deepEqual([v.survived,v.time,v.hp,v.healUses],expected[key][i]);assert.deepEqual(r,simulate(key,tactic));
  for(const [j,e] of r.events.entries()){
   assert.equal(e.hpBefore-e.totalDamage,e.hpAfterDamage);
   assert.equal(e.hpAfterAll,e.hpAfterDamage+e.lifestealAmount+e.supportHealAmount);
   assert.ok(e.after.hp<=e.after.maxHp);assert.ok(e.after.healUses<=3);
   const afterAttacks=e.before.enemies.map(x=>x.hp);for(const a of e.attacks)afterAttacks[a.target]-=a.damage;
   e.counterTargets.forEach((id,k)=>assert.ok(e.counterDamage[k]<=afterAttacks[id]));
   if(e.died){assert.equal(e.counterTriggered,false);assert.equal(e.lifestealAmount,0);assert.equal(e.supportHealAmount,0);assert.equal(j,r.events.length-1);}
   if(r.events[j+1])assert.ok(beatDuration(r,j)<r.events[j+1].timestamp-e.timestamp);
   // Scrubbing is independent of playback history and never mutates the core.
   const t=e.timestamp+beatDuration(r,j)*.5,mid=project(r,t);project(r,0);project(r,playbackEnd(r));assert.deepEqual(project(r,t),mid);
  }
  assert.deepEqual(project(r,playbackEnd(r)).state,r.events.at(-1).after);
 });
}
test('waiting wave cannot attack in the beat it joins',()=>{
 const r=simulate('base','split');const e=r.events.find(e=>e.joined.length);
 assert.ok(e);assert.ok(e.joined.every(id=>!e.attackers.includes(id)&&!e.counterTargets.includes(id)));
});
test('fatal beat remains dead at every presentation phase',()=>{
 const r=simulate('danger','focus'),e=r.events.at(-1);
 for(const fraction of [0,.1,.2,.5,.8,1]){const f=project(r,e.timestamp+fraction*520);assert.equal(f.dead,true);assert.ok(f.state.hp<=0);}
});
test('same-timestamp ally kills remove enemy from attack bundle',()=>{
 // The battle never includes an enemy already removed by that beat's ally attacks.
 for(const key of Object.keys(CONDITIONS))for(const tactic of ['focus','split'])for(const e of simulate(key,tactic).events){
  const hp=e.before.enemies.map(x=>x.hp);for(const a of e.attacks)hp[a.target]-=a.damage;
  for(const id of e.attackers)assert.ok(hp[id]>0);
 }
});
