// All logic times are integer milliseconds; damage/HP are in hundredths.
export const CONDITIONS = Object.freeze({
  base: { label: '基準', maxHp: 100, support: 'B', supportAttack: 4, enemyAttack: 9 },
  companion: { label: '仲間変更 B → C', maxHp: 100, support: 'C', supportAttack: 8, enemyAttack: 9 },
  growth: { label: 'A成長 HP 120', maxHp: 120, support: 'B', supportAttack: 4, enemyAttack: 9 },
  danger: { label: '敵変更 攻撃 17', maxHp: 100, support: 'B', supportAttack: 4, enemyAttack: 17 },
});
const clone = x => structuredClone(x);
const freeze = x => { if(x && typeof x==='object') { Object.values(x).forEach(freeze); Object.freeze(x); } return x; };
export function simulate(condition='base', tactic='focus') {
  if (!CONDITIONS[condition] || !['focus','split'].includes(tactic)) throw new Error('Unknown configuration');
  const config = CONDITIONS[condition];
  const state = {hp:config.maxHp*100,maxHp:config.maxHp*100,gauge:0,healUses:0,healReady:0,
    enemies:Array.from({length:4},(_,id)=>({id,hp:3500,active:tactic==='focus'||id<2}))};
  const initial=clone(state), events=[];
  let nextA=1400,nextB=2000,nextEnemy=1800,time=0,totalDamage=0;
  const living = () => state.enemies.filter(e=>e.active && e.hp>0);
  const hit = (enemy,amount) => { const actual=Math.min(enemy.hp,amount); enemy.hp-=actual;return actual; };
  while (state.hp>0 && state.enemies.some(e=>e.hp>0) && time<60000) {
    time=Math.min(nextA,nextB,nextEnemy);
    const before=clone(state), attacks=[], counterTargets=[], counterDamage=[];
    for(const [source,due,power] of [['A',nextA,1000],[config.support,nextB,config.supportAttack*100]]) {
      if(time===due) {
        const target=living().sort((a,b)=>a.hp-b.hp||a.id-b.id)[0];
        if(target) attacks.push({source,target:target.id,damage:hit(target,power)});
      }
    }
    if(time===nextA)nextA+=1400;
    if(time===nextB)nextB+=2000;
    const attackers=time===nextEnemy?living().map(e=>e.id):[];
    if(time===nextEnemy)nextEnemy+=1800;
    const damage=attackers.length*config.enemyAttack*80;
    const hpBefore=state.hp,gaugeBefore=state.gauge;
    state.hp-=damage;totalDamage+=damage;state.gauge+=attackers.length;
    const hpAfterDamage=state.hp,gaugeAfterDamage=state.gauge;
    const died=state.hp<=0;
    let counterTriggered=false,lifestealAmount=0,supportHealAmount=0,supportHealSource=null;
    if(!died && state.gauge>=4) {
      counterTriggered=true;state.gauge-=4;
      for(const enemy of living()) {counterTargets.push(enemy.id);counterDamage.push(hit(enemy,1100));}
      lifestealAmount=Math.min(state.maxHp-state.hp,Math.round(counterDamage.reduce((a,b)=>a+b,0)*.25));
      state.hp+=lifestealAmount;
    }
    const hpAfterLifesteal=state.hp;
    if(!died && config.support==='B' && state.hp*100<=state.maxHp*55 && state.healUses<3 && time>=state.healReady) {
      supportHealSource='B';supportHealAmount=Math.min(2400,state.maxHp-state.hp);state.hp+=supportHealAmount;
      state.healUses++;state.healReady=time+5000;
    }
    const joined=[];
    if(!died && living().length===0)for(const enemy of state.enemies)if(enemy.hp>0&&!enemy.active){enemy.active=true;joined.push(enemy.id);}
    events.push({type:attackers.length?'HitBeat':'ActionBeat',timestamp:time,before,attacks,attackers,
      hpBefore,totalDamage:damage,hpAfterDamage,gaugeBefore,gaugeDelta:attackers.length,gaugeAfterDamage,
      counterTriggered,counterTargets,counterDamage,lifestealAmount,hpAfterLifesteal,
      supportHealSource,supportHealAmount,hpAfterAll:state.hp,died,joined,after:clone(state)});
  }
  return freeze({condition,tactic,config,initial,events,duration:time,
    result:{survived:state.hp>0,time:time/1000,hp:Math.max(0,state.hp)/100,healUses:state.healUses,totalDamage:totalDamage/100}});
}
