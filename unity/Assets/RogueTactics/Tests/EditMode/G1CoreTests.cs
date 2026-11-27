using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core;
using RogueTactics.Core.G1;

namespace RogueTactics.Tests
{
    public class G1CoreTests
    {
        [Test]
        public void WatchLanternProtectsAtRangeTwoAndChangesThreatDetection()
        {
            var room=new EncounterState(4,4,new[]{
                new Unit("A",Role.Anchor,5,3,new GridPosition(1,1)),
                new Unit("B",Role.Protector,8,2,new GridPosition(3,1)),
                new Unit("X",Role.Enemy,10,6,new GridPosition(1,2))
            },new[]{"B","X","A"},BattleRules.WithRelic(BattleRules.WatchLantern));
            var command=new Command(Preference.ProtectAnchor);
            Assert.That(Rules.Threatened(room,command),Does.Contain("A"));
            var prepared=Rules.Resolve(room,command);
            Assert.That(prepared.Plan.Kind,Is.EqualTo(ActionKind.Protect));
            Assert.That(prepared.Events.Single(e=>e.Kind==EventKind.Protect).Relic,Is.EqualTo(BattleRules.WatchLantern));
            Assert.That(Rules.Threatened(prepared.State,command),Does.Not.Contain("A"));
            var hit=Rules.Resolve(prepared.State,command).Events.Single(e=>e.Kind==EventKind.Damage);
            Assert.That(hit.Amount,Is.EqualTo(3));
            Assert.That(hit.Relic,Is.EqualTo(BattleRules.WatchLantern));
        }

        [Test]
        public void ThornCharmStrengthensARealCounterAfterCarryingHealth()
        {
            var room=new EncounterState(3,3,new[]{
                new Unit("A",Role.Anchor,8,3,new GridPosition(1,1)),
                new Unit("X",Role.Enemy,10,2,new GridPosition(1,2))
            },new[]{"X","A"},BattleRules.WithRelic(BattleRules.ThornCharm));
            var entry=EncounterBuilder.Build(room,new System.Collections.Generic.Dictionary<string,int>{{"A",6}});
            var replay=new Replay(entry,Preference.DirectAttack);
            var counter=replay.Advance().Events.Single(e=>e.Kind==EventKind.Counter);
            Assert.That(counter.Amount,Is.EqualTo(3));
            Assert.That(counter.Relic,Is.EqualTo(BattleRules.ThornCharm));
            Assert.That(replay.State.Find("A").Hp,Is.EqualTo(4));
            Assert.That(replay.State.Find("X").Hp,Is.EqualTo(7));
        }

        [TestCase("R07",BattleRules.WatchLantern,Preference.ProtectAnchor,17,6,16,12,10)]
        [TestCase("R02","",Preference.ProtectAnchor,22,6,16,8,10)]
        [TestCase("R05",BattleRules.ThornCharm,Preference.DirectAttack,32,2,8,12,10)]
        public void ThirdFloorRoomAndRelicMatchPython(string room,string relic,Preference policy,
            int steps,int a,int b,int c,int d)
        {
            var replay=new Replay(FloorThreeRooms.Build(room,BattleRules.WithRelic(relic)),policy);
            while(replay.State.Outcome==Outcome.Active)replay.Advance();
            Assert.That(replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(replay.State.Step,Is.EqualTo(steps));
            Assert.That(replay.State.Units.Where(unit=>!unit.Enemy).Select(unit=>unit.Hp),
                Is.EqualTo(new[]{a,b,c,d}));
        }

        private static Replay Complete(Preference p)
        { var r = new Replay(Fixture.Baseline(),p); while(r.State.Outcome == Outcome.Active) r.Advance(); return r; }
        [TestCase(Preference.ProtectAnchor)] [TestCase(Preference.DirectAttack)]
        public void ReplayIsByteIdenticalAndIndependentOfCulture(Preference p)
        {
            var before = CultureInfo.CurrentCulture;
            try {
                var a = Complete(p); CultureInfo.CurrentCulture = new CultureInfo("ar-SA"); var b = Complete(p);
                Assert.That(a.Serialize(),Is.EqualTo(b.Serialize())); Assert.That(a.Hash(),Is.EqualTo(b.Hash()));
            } finally { CultureInfo.CurrentCulture = before; }
        }
        [TestCase("R01",Preference.ProtectAnchor,20,18,11,5,10)]
        [TestCase("R01",Preference.DirectAttack,20,18,16,0,10)]
        [TestCase("R01",Preference.Balanced,20,18,16,0,10)]
        [TestCase("R04",Preference.ProtectAnchor,22,8,16,11,0)]
        [TestCase("R04",Preference.DirectAttack,18,8,16,11,4)]
        [TestCase("R04",Preference.Balanced,22,8,16,11,0)]
        [TestCase("R09",Preference.ProtectAnchor,25,12,16,12,0)]
        [TestCase("R09",Preference.DirectAttack,23,18,0,12,10)]
        [TestCase("R09",Preference.Balanced,23,18,0,12,10)]
        public void FloorTwoRoomsMatchThePythonSimulation(string room,Preference preference,int steps,int a,int b,int c,int d)
        {
            var replay=new Replay(FloorTwoRooms.Build(room),preference);
            while(replay.State.Outcome==Outcome.Active)replay.Advance();
            Assert.That(replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(replay.State.Step,Is.EqualTo(steps));
            Assert.That(replay.State.Units.Where(unit=>!unit.Enemy).Select(unit=>unit.Hp),Is.EqualTo(new[]{a,b,c,d}));
        }

        [TestCase(EnemyKind.Hunter,"A")]
        [TestCase(EnemyKind.Moss,"B")]
        [TestCase(EnemyKind.Bloom,"D")]
        public void OpponentsSelectTheirDistinctTargetFromCurrentState(EnemyKind kind,string expected)
        {
            var room=new EncounterState(5,5,new[]{
                new Unit("A",Role.Anchor,18,5,new GridPosition(4,4)),
                new Unit("B",Role.Protector,16,4,new GridPosition(1,2)),
                new Unit("C",Role.Flanker,12,4,new GridPosition(2,1)),
                new Unit("D",Role.Support,10,2,new GridPosition(0,0)),
                new Unit("X",Role.Enemy,18,6,new GridPosition(1,1),kind)
            },new[]{"X","A","B","C","D"});
            var replay=new Replay(room,Preference.DirectAttack);
            var next=replay.Preview();
            Assert.That(next.Target,Is.EqualTo(expected));
            Assert.That(Rules.IsLegal(room,next),Is.True);
            Assert.That(replay.State.Find("X").Kind,Is.EqualTo(kind));
            Assert.That(room.Serialize().Contains("|kinds:"),Is.EqualTo(kind!=EnemyKind.Hunter));
        }

        [TestCase(10,Preference.DirectAttack)]
        [TestCase(9,Preference.ProtectAnchor)]
        [TestCase(8,Preference.ProtectAnchor)]
        public void BalancedDefendsAtHalfHealthAndBelow(int anchorHp,Preference equivalent)
        {
            var room=Fixture.Baseline();
            var hp=room.Units.Where(unit=>!unit.Enemy).ToDictionary(unit=>unit.Id,unit=>unit.Hp);
            hp["A"]=anchorHp;
            var state=EncounterBuilder.Build(room,hp);
            var intents=Rules.Intents(state,new Command(Preference.Balanced)).Select(plan=>plan.Serialize());
            var expected=Rules.Intents(state,new Command(equivalent)).Select(plan=>plan.Serialize());
            Assert.That(intents,Is.EqualTo(expected));
        }

        [Test]
        public void BalancedThirdEncounterMatchesThePythonSimulation()
        {
            var replay=new Replay(Fixture.Encounter(2),Preference.Balanced);
            while(replay.State.Outcome==Outcome.Active)replay.Advance();
            var allies=replay.State.Units.Where(unit=>!unit.Enemy).ToArray();
            Assert.That(replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(replay.State.Step,Is.EqualTo(29));
            Assert.That(allies.All(unit=>unit.Alive),Is.True);
            Assert.That(allies.Sum(unit=>unit.Hp),Is.EqualTo(39));
        }

        [Test]
        public void ReasonsDistinguishProtectionAndAPlayerDirectedAttack()
        {
            var replay=new Replay(Fixture.Baseline(),Preference.ProtectAnchor);
            Assert.That(replay.Preview().Why,Is.EqualTo(IntentReason.Protect));
            replay.TargetOnce("C","Y");
            Assert.That(replay.Intents().Single(plan=>plan.Actor=="C").Why,Is.EqualTo(IntentReason.Ordered));
        }
        [Test] public void SinglePreferenceForkChangesRealPlanAndOutcomeMeasures()
        {
            var baseline = Fixture.Baseline().Serialize(); var a = Complete(Preference.ProtectAnchor); var b = Complete(Preference.DirectAttack);
            Assert.That(Fixture.Baseline().Serialize(),Is.EqualTo(baseline));
            Assert.That(a.Events.First().Detail,Does.StartWith("Protect")); Assert.That(b.Events.First().Detail,Does.StartWith("Move"));
            Assert.That(a.Hash(),Is.Not.EqualTo(b.Hash()));
            Assert.That(a.State.Units.Select(u=>u.Hp).SequenceEqual(b.State.Units.Select(u=>u.Hp)) && a.State.Step == b.State.Step,Is.False);
            Assert.That(a.State.Outcome,Is.EqualTo(Outcome.Victory)); Assert.That(b.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(a.Events.Any(e=>e.Kind==EventKind.Protect),Is.True);
            Assert.That(a.Events.Any(e=>e.Kind==EventKind.Heal),Is.True);
            Assert.That(a.Events.Any(e=>e.Kind==EventKind.Counter && e.Tags.HasFlag(EventTags.Counter)),Is.True);
            var guarded = a.Events.First(e=>e.Kind==EventKind.Damage && e.Tags.HasFlag(EventTags.Guarded));
            Assert.That(guarded.Guardian,Is.EqualTo("B"));
            Assert.That(guarded.Prevented,Is.EqualTo(3));
            Assert.That(a.Events.Last().Result,Is.EqualTo(Outcome.Victory));
            Assert.That(a.Events.Any(e=>e.Detail=="Flank +2" && e.Tags.HasFlag(EventTags.Flanked)),Is.True);
        }
        [TestCase(Preference.ProtectAnchor)] [TestCase(Preference.DirectAttack)]
        public void EveryActionLegalAndNoDeadActorOrTargetCanActOrHeal(Preference p)
        {
            var r = new Replay(Fixture.Baseline(),p);
            while(r.State.Outcome==Outcome.Active)
            {
                var before = r.State.Serialize(); var next = Rules.Preview(r.State,new Command(p));
                Assert.That(Rules.IsLegal(r.State,next),Is.True);
                var hp = r.State.Units.ToDictionary(u=>u.Id,u=>u.Hp);
                var result = r.Advance();
                Assert.That(before,Is.Not.EqualTo(r.State.Serialize()));
                foreach(var e in result.Events)
                {
                    if(e.Kind==EventKind.Action || e.Kind==EventKind.Heal || e.Kind==EventKind.Damage || e.Kind==EventKind.Counter)
                        Assert.That(hp[e.Actor],Is.GreaterThan(0),e.Serialize());
                    if(e.Kind==EventKind.Heal || e.Kind==EventKind.Damage || e.Kind==EventKind.Counter)
                    { Assert.That(hp[e.Target],Is.GreaterThan(0),e.Serialize()); hp[e.Target]=e.HpAfter; }
                    Assert.That(e.Amount,Is.GreaterThanOrEqualTo(0));
                }
                foreach(var u in r.State.Units) Assert.That(u.Hp,Is.InRange(0,u.MaxHp));
                Assert.That(r.State.Units.Where(u=>u.Alive).Select(u=>u.Position).Distinct().Count(),Is.EqualTo(r.State.Units.Count(u=>u.Alive)));
            }
            Assert.That(()=>r.Advance(),Throws.InvalidOperationException);
            Assert.That(Rules.Preview(r.State,new Command(p)),Is.Null);
        }
        [Test] public void LethalHitSuppressesAnchorCounter()
        {
            var s=new EncounterState(4,4,new[] {
                new Unit("A",Role.Anchor,1,5,new GridPosition(2,2)),new Unit("B",Role.Protector,10,4,new GridPosition(0,0)),
                new Unit("X",Role.Enemy,20,6,new GridPosition(2,3)) });
            var first=Rules.Resolve(s,new Command(Preference.DirectAttack));
            var hit=Rules.Resolve(first.State,new Command(Preference.DirectAttack));
            var damage = hit.Events.Single(e=>e.Kind==EventKind.Damage);
            Assert.That(damage.Tags,Is.EqualTo(EventTags.Lethal | EventTags.Overkill));
            Assert.That(hit.Events.Any(e=>e.Kind==EventKind.Death && e.Target=="A"),Is.True);
            Assert.That(hit.Events.Any(e=>e.Kind==EventKind.Counter),Is.False);
            Assert.That(s.Find("A").Hp,Is.EqualTo(1),"source snapshot must not mutate");
        }
        [Test] public void BlockedMovementMaintainsAndIllegalFixtureRejected()
        {
            var s=new EncounterState(3,3,new[] {new Unit("A",Role.Anchor,10,5,new GridPosition(0,1)),
                new Unit("B",Role.Protector,10,4,new GridPosition(1,1)),new Unit("X",Role.Enemy,10,6,new GridPosition(2,2)),
                new Unit("C",Role.Flanker,10,4,new GridPosition(1,2)),new Unit("D",Role.Support,10,2,new GridPosition(2,1))});
            var step=Rules.Resolve(s,new Command(Preference.DirectAttack));
            Assert.That(step.Plan.Kind,Is.EqualTo(ActionKind.Maintain));
            Assert.That(step.Plan.Why,Is.EqualTo(IntentReason.Blocked));
            Assert.That(step.State.Find("B").Position,Is.EqualTo(s.Find("B").Position));
            Assert.That(()=>new EncounterState(1,1,new[] {new Unit("A",Role.Anchor,1,1,new GridPosition(2,2))}),Throws.ArgumentException);
            Assert.That(()=>new Command((Preference)99),Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
