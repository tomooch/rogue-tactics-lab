import {simulate,CONDITIONS} from './core.mjs';
import {project,beatDuration,playbackEnd} from './timeline.mjs';
const $=id=>document.getElementById(id);
let run=simulate(),time=0,playing=false,speed=1,last=0,details=false;
const A={x:180,y:302},B={x:180,y:472};
const colors={a:'#71a5cf',enemy:'#f88d90',counter:'#e9bf64',life:'#50d9c6',heal:'#68e9b1'};
const num=v=>(Math.max(0,v)/100).toFixed(1).replace(/\.0$/,'');
function pos(id,state){
  if(!state.enemies[id].active)return [{x:270,y:66},{x:326,y:66}][id-2];
  if(run.tactic==='split')return {x:124+(id%2)*112,y:166};
  return [{x:62,y:170},{x:136,y:124},{x:224,y:124},{x:298,y:170}][id];
}
function line(a,b,color,opacity=1,width=2){return `<path d="M${a.x} ${a.y} Q${(a.x+b.x)/2+10} ${(a.y+b.y)/2} ${b.x} ${b.y}" fill="none" stroke="${color}" stroke-width="${width}" opacity="${opacity}" stroke-linecap="round"/>`;}
function flow(a,b,color,p){
  p=Math.max(0,Math.min(1,p));const tail=Math.max(0,p-.25);
  return line({x:a.x+(b.x-a.x)*tail,y:a.y+(b.y-a.y)*tail},{x:a.x+(b.x-a.x)*p,y:a.y+(b.y-a.y)*p},color,1,3)+`<circle cx="${a.x+(b.x-a.x)*p}" cy="${a.y+(b.y-a.y)*p}" r="3" fill="${color}"/>`;
}
function pawn(p,label,color,dead=false,flash=0){return `<g transform="translate(${p.x} ${p.y})"><ellipse cy="28" rx="23" ry="7" fill="#07141d" opacity=".3"/><path d="M-13 -1 Q-19 10 -20 29 Q0 35 20 29 Q19 10 13 -1Z" fill="${color}"/><circle cy="-12" r="13" fill="${color}"/><path d="M-10 -19 Q0 -26 10 -19" stroke="#ffffff25" fill="none" stroke-width="3"/><text y="20" text-anchor="middle" fill="${dead?'#263b4b':'white'}" font-size="22" font-weight="650">${dead?'×':label}</text>${flash?`<circle r="31" fill="none" stroke="${colors.enemy}" opacity="${flash}"/>`:''}</g>`;}
function render(){
 const f=project(run,time),s=f.state,e=f.event,p=f.phase,ended=time>=playbackEnd(run);
 let svg=`<g fill="none" stroke="#597284" opacity=".14" stroke-dasharray="4 7"><circle cx="180" cy="302" r="108"/><circle cx="180" cy="302" r="162"/></g>`;
 if(s.enemies.some(x=>!x.active))svg+=`<path d="M226 24 L226 97 Q226 116 247 116 L347 116" stroke="#7c94a7" stroke-dasharray="5 6" opacity=".6" fill="none"/><text x="284" y="105" fill="#9fb2c2" font-size="12" text-anchor="middle">待機</text>`;
 // Minimal stationary geometry, exactly the approved mock vocabulary.
 for(const enemy of s.enemies){const at=pos(enemy.id,s);let opacity=enemy.hp<=0?.13:enemy.active?1:.62;
  let offset=0;const next=run.events[f.index+1];if(next?.attackers.includes(enemy.id)){const gap=next.timestamp-(f.index>=0?e.timestamp:0);const pre=Math.min(120,gap*.25);if(time>=next.timestamp-pre)offset=6*(time-next.timestamp+pre)/pre;}
  svg+=`<g transform="translate(${at.x} ${at.y+offset})" opacity="${opacity}"><path d="M0 -12 L12 0 L0 12 L-12 0Z" fill="${colors.enemy}"/><path d="M0 -12 L12 0 L0 -5 L-12 0Z" fill="#ffb2b1" opacity=".6"/><rect x="-18" y="21" width="36" height="5" rx="1" fill="#10212e" stroke="#8091a0" stroke-width=".8"/><rect x="-17" y="22" width="${34*Math.max(0,enemy.hp)/3500}" height="3" fill="${colors.enemy}"/></g>`;
 }
 if(e&&p<1){
  if(p<.19){for(const attack of e.attacks)svg+=line(attack.source==='A'?A:B,pos(attack.target,s),attack.source==='A'?colors.a:colors.heal,1-p/.19,2);for(const id of e.attackers)svg+=line(pos(id,s),A,colors.enemy,1-p/.19,2.5);}
  if(e.counterTriggered&&p>=.19&&p<.42){for(const id of e.counterTargets)svg+=line(A,pos(id,s),colors.counter,1-(p-.19)/.23,2.5);}
  if(e.lifestealAmount>0&&p>=.42&&p<.73){for(const id of e.counterTargets)svg+=flow(pos(id,s),A,colors.life,(p-.42)/.31);}
  if(e.supportHealAmount>0&&p>=.73){svg+=flow(B,A,colors.heal,(p-.73)/.27);}
 }
 svg+=pawn(A,'A',f.dead?'#91a0ac':colors.a,f.dead,e?.totalDamage>0&&p<.19?1-p/.19:0);
 for(let i=0;i<4;i++){const x=A.x+(i%2?34:-34),y=A.y+(i<2?-13:26),lit=!f.dead&&i<s.gauge;svg+=`<path d="M${x} ${y-7} l7 7 -7 7 -7 -7Z" fill="${lit?'#e9bf6440':'#213746'}" stroke="${lit?colors.counter:'#647d90'}" stroke-width="1.6"/>`;}
 const hp=Math.max(0,s.hp),barX=126,barY=348,barW=108;
 svg+=`<rect x="${barX-1}" y="${barY-1}" width="110" height="11" rx="2" fill="#10212e" stroke="#8396a8"/>`;
 if(f.trail){const l=Math.max(0,f.trail.from)/s.maxHp*barW,r=Math.min(s.maxHp,f.trail.to)/s.maxHp*barW;svg+=`<rect x="${barX+l}" y="${barY}" width="${Math.max(0,r-l)}" height="9" fill="#e8969b" opacity="${f.trail.opacity}"/>`;}
 svg+=`<rect x="${barX}" y="${barY}" width="${barW*Math.min(s.maxHp,hp)/s.maxHp}" height="9" rx="1" fill="#65e991"/>`;
 svg+=pawn(B,run.config.support,run.config.support==='B'?'#50ceb8':'#a39bd1');
 if(run.config.support==='B')for(let i=0;i<3;i++)svg+=`<circle cx="${156+i*24}" cy="524" r="5" fill="${i<3-s.healUses?colors.heal:'#192a39'}" stroke="#9fb9c9" stroke-width="1.2"/>`;
 $('scene').innerHTML=svg;
 $('endBadge').hidden=!(ended||f.dead&&p>=.6);
 $('status').textContent=ended?'戦闘終了':time===0?'待機':playing?'交戦中':'停止中';
 $('play').setAttribute('aria-label',playing?'一時停止':ended?'最初から再生':'再生');
 $('play').innerHTML=playing?'<svg viewBox="0 0 20 20" fill="currentColor"><rect x="4" y="2" width="4" height="16" rx="1"/><rect x="12" y="2" width="4" height="16" rx="1"/></svg>':'<svg viewBox="0 0 20 20" fill="currentColor"><path d="M5 2 L18 10 L5 18Z"/></svg>';
 $('replay').hidden=playing||time===0;$('expanded').hidden=playing;document.body.classList.toggle('playing',playing);
 $('seek').max=playbackEnd(run);$('seek').value=time;$('clock').value=`${(Math.min(time,run.duration)/1000).toFixed(1)}秒`;
 $('speed').textContent=`${speed}×`;$('inspection').hidden=playing||!details;
 if(details&&!playing){$('inspection').innerHTML=`<h3>${run.config.label} · ${run.tactic==='focus'?'集中':'分割'}</h3><dl><dt>A HP</dt><dd>${num(s.hp)} / ${num(s.maxHp)}</dd><dt>反撃ゲージ</dt><dd>${s.gauge}</dd><dt>回復消費</dt><dd>${run.config.support==='B'?s.healUses+' / 3':'なし'}</dd>${e?`<dt>イベント時刻</dt><dd>${e.timestamp/1000}秒</dd><dt>被害</dt><dd>${num(e.totalDamage)}</dd><dt>吸血</dt><dd>${num(e.lifestealAmount)}</dd><dt>B回復</dt><dd>${num(e.supportHealAmount)}</dd>`:''}${ended?`<dt>結果</dt><dd>${run.result.survived?'生存':'死亡'}</dd><dt>戦闘終了時刻</dt><dd>${run.result.time}秒</dd>`:''}</dl>`;}
}
function pause(){playing=false;render();}
$('play').onclick=()=>{if(time>=playbackEnd(run))time=0;playing=!playing;details=false;last=performance.now();render();};
$('replay').onclick=()=>{time=0;playing=true;details=false;last=performance.now();render();};
$('speed').onclick=()=>{speed=speed===1?.5:1;last=performance.now();render();};
$('seek').oninput=e=>{playing=false;time=Number(e.target.value);render();};
$('details').onclick=()=>{details=!details;render();};
$('scene').onclick=()=>{if(!playing){details=!details;render();}};
$('configure').onclick=()=>{pause();$('configForm').elements.condition.value=run.condition;$('configForm').elements.tactic.value=run.tactic;$('config').showModal();};
$('closeConfig').onclick=()=>$('config').close();
$('configForm').onsubmit=e=>{e.preventDefault();const data=new FormData(e.currentTarget);run=simulate(data.get('condition'),data.get('tactic'));time=0;playing=true;details=false;last=performance.now();$('config').close();render();};
document.addEventListener('visibilitychange',()=>{if(document.hidden)pause();last=performance.now();});
function tick(now){if(playing){time=Math.min(playbackEnd(run),time+Math.min(now-last,100)*speed);if(time>=playbackEnd(run))playing=false;render();}last=now;requestAnimationFrame(tick);}
render();requestAnimationFrame(tick);
