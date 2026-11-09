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
            Assert.That(a.Events.Any(e=>e.Kind==EventKind.Counter),Is.True);
            Assert.That(a.Events.Any(e=>e.Detail=="Flank +2"),Is.True);
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
            Assert.That(step.State.Find("B").Position,Is.EqualTo(s.Find("B").Position));
            Assert.That(()=>new EncounterState(1,1,new[] {new Unit("A",Role.Anchor,1,1,new GridPosition(2,2))}),Throws.ArgumentException);
            Assert.That(()=>new Command((Preference)99),Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
