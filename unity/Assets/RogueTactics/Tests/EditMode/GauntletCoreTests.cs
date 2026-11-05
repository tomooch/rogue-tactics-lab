using System.Linq;
using NUnit.Framework;
using RogueTactics.Core;
using RogueTactics.Core.Exploration;
using RogueTactics.Core.G1;

namespace RogueTactics.Tests
{
    public class GauntletCoreTests
    {
        private static Replay Play(int encounter,Preference preference,bool correctAtFirstCrisis)
        {
            var replay=new Replay(Fixture.Encounter(encounter),preference);
            while(replay.State.Outcome==Outcome.Active)
            {
                if(correctAtFirstCrisis && replay.CorrectionsLeft>0 && Rules.Threatened(replay.State,new Command(replay.Preference)).Count>0)
                    replay.Correct(replay.Preference==Preference.ProtectAnchor?Preference.DirectAttack:Preference.ProtectAnchor);
                replay.Advance();
            }
            return replay;
        }
        private static int Survivors(Replay r)=>r.State.Units.Count(u=>!u.Enemy && u.Alive);

        [Test]
        public void EncounterZeroIsTheFrozenG1FixtureAndHashes()
        {
            Assert.That(Fixture.Encounter(0).Serialize(),Is.EqualTo(Fixture.Baseline().Serialize()));
            Assert.That(Play(0,Preference.ProtectAnchor,false).Hash(),Is.EqualTo("e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d"));
            Assert.That(Play(0,Preference.DirectAttack,false).Hash(),Is.EqualTo("85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e"));
        }
        [Test]
        public void SecondEncounterPunishesOnlyProtectingAndAllowsACrisisRecovery()
        {
            Assert.That(Survivors(Play(1,Preference.ProtectAnchor,false)),Is.EqualTo(3),"Protecting alone lets Shiro fall");
            Assert.That(Survivors(Play(1,Preference.DirectAttack,false)),Is.EqualTo(4));
            var recovered=Play(1,Preference.ProtectAnchor,true);
            Assert.That(recovered.State.Outcome,Is.EqualTo(Outcome.Victory));Assert.That(Survivors(recovered),Is.EqualTo(4));
        }
        [Test]
        public void ThirdEncounterNeedsTheRightCorrectionAtTheCrisis()
        {
            Assert.That(Survivors(Play(2,Preference.ProtectAnchor,false)),Is.LessThan(4));
            Assert.That(Survivors(Play(2,Preference.DirectAttack,false)),Is.LessThan(4));
            Assert.That(Survivors(Play(2,Preference.ProtectAnchor,true)),Is.LessThan(4),"The opposite correction does not save the party");
            var saved=Play(2,Preference.DirectAttack,true);
            Assert.That(saved.State.Outcome,Is.EqualTo(Outcome.Victory));Assert.That(Survivors(saved),Is.EqualTo(4));
        }
        [Test]
        public void CorrectionIsSingleRecordedAndDeterministic()
        {
            var a=Play(2,Preference.DirectAttack,true);var b=Play(2,Preference.DirectAttack,true);
            Assert.That(a.Hash(),Is.EqualTo(b.Hash()));Assert.That(a.CorrectionsLeft,Is.EqualTo(0));
            Assert.That(a.Serialize(),Does.Contain(";0;"),"Commands after the correction record the new preference");
            Assert.Throws<System.InvalidOperationException>(()=>{var r=new Replay(Fixture.Encounter(2),Preference.DirectAttack);r.Correct(Preference.ProtectAnchor);r.Correct(Preference.DirectAttack);});
            Assert.That(Play(2,Preference.DirectAttack,false).Hash(),Is.Not.EqualTo(a.Hash()));
        }
        [Test]
        public void IntentsAndThreatsAreReadOnlyQueries()
        {
            var state=Fixture.Encounter(2);var before=state.Serialize();
            var intents=Rules.Intents(state,new Command(Preference.DirectAttack));
            Assert.That(intents.Select(p=>p.Actor),Is.EqualTo(state.Initiative.ToArray()),"Every living actor, in acting order");
            Assert.That(intents[0].Serialize(),Is.EqualTo(Rules.Preview(state,new Command(Preference.DirectAttack)).Serialize()));
            Rules.Threatened(state,new Command(Preference.DirectAttack));
            Assert.That(state.Serialize(),Is.EqualTo(before));
        }
        [Test]
        public void TargetOnceValidatesLivingAllyAndEnemyAndConsumesEncounterBudget()
        {
            var replay=new Replay(Fixture.Baseline(),Preference.DirectAttack);
            Assert.Throws<System.ArgumentException>(()=>replay.TargetOnce("X","A"));
            Assert.Throws<System.ArgumentException>(()=>replay.TargetOnce("A","missing"));
            replay.TargetOnce("B","Y");
            Assert.That(replay.CorrectionsLeft,Is.Zero);
            Assert.Throws<System.InvalidOperationException>(()=>replay.TargetOnce("A","X"));
        }
        [Test]
        public void TargetOnceOverridesOnlySelectedAlliesNextActionAndThenExpires()
        {
            var replay=new Replay(Fixture.Baseline(),Preference.DirectAttack);
            replay.TargetOnce("B","Y");
            Assert.That(replay.Preview().Actor,Is.EqualTo("B"));
            Assert.That(replay.Preview().Target,Is.EqualTo("Y"));
            Assert.That(replay.Preview().Kind,Is.EqualTo(ActionKind.Maintain),"A occupies the only distance-reducing cell; an instruction cannot move through an ally");
            var intents=replay.Intents();
            Assert.That(intents.Single(p=>p.Actor=="B").Target,Is.EqualTo("Y"));
            replay.Advance();
            Assert.That(replay.PendingTargetActor,Is.Null);
            Assert.That(replay.Preview().Actor,Is.EqualTo("X"));
        }
        [Test]
        public void TargetOnceWaitsForChosenAllyRatherThanChangingEarlierActors()
        {
            var replay=new Replay(Fixture.Baseline(),Preference.DirectAttack);
            replay.TargetOnce("A","Y");
            Assert.That(replay.Preview().Actor,Is.EqualTo("B"));
            replay.Advance();
            Assert.That(replay.PendingTargetActor,Is.EqualTo("A"));
            Assert.That(replay.Preview().Actor,Is.EqualTo("X"));
            replay.Advance();
            Assert.That(replay.Preview().Actor,Is.EqualTo("A"));
            Assert.That(replay.Preview().Target,Is.EqualTo("Y"));
            replay.Advance();
            Assert.That(replay.PendingTargetActor,Is.Null);
        }
        [Test]
        public void TargetOnceIsRecordedAndDeterministicAndDoesNotChangeFrozenNoInterventionHashes()
        {
            var a=new Replay(Fixture.Baseline(),Preference.DirectAttack); var b=new Replay(Fixture.Baseline(),Preference.DirectAttack);
            a.TargetOnce("B","Y"); b.TargetOnce("B","Y");
            Assert.That(a.Serialize(),Does.Contain("target:B,Y"));
            Assert.That(a.Serialize(),Does.Contain("pending-target:B,Y"));
            while(a.State.Outcome==Outcome.Active)a.Advance(); while(b.State.Outcome==Outcome.Active)b.Advance();
            Assert.That(a.Hash(),Is.EqualTo(b.Hash()));
            Assert.That(Play(0,Preference.ProtectAnchor,false).Hash(),Is.EqualTo("e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d"));
            Assert.That(Play(0,Preference.DirectAttack,false).Hash(),Is.EqualTo("85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e"));
        }
        [Test]
        public void MeasureVoluntaryInstructionConsequences()
        {
            var rows=new System.Collections.Generic.List<string>{"encounter,preference,step,actor,target,survivors,hp,actions,outcome"};
            for(var encounter=0;encounter<3;encounter++)
            foreach(var preference in new[]{Preference.ProtectAnchor,Preference.DirectAttack})
            {
                var baseline=Play(encounter,preference,false);
                rows.Add($"{encounter},{preference},-1,none,none,{Survivors(baseline)},{baseline.State.Units.Where(u=>!u.Enemy).Sum(u=>u.Hp)},{baseline.State.Step},{baseline.State.Outcome}");
                for(var at=0;at<baseline.State.Step;at++)
                {
                    var before=new Replay(Fixture.Encounter(encounter),preference);
                    for(var i=0;i<at;i++)before.Advance();
                    foreach(var ally in before.State.Units.Where(u=>!u.Enemy && u.Alive))
                    foreach(var enemy in before.State.Units.Where(u=>u.Enemy && u.Alive))
                    {
                        var trial=new Replay(Fixture.Encounter(encounter),preference);
                        for(var i=0;i<at;i++)trial.Advance();
                        trial.TargetOnce(ally.Id,enemy.Id);
                        while(trial.State.Outcome==Outcome.Active)trial.Advance();
                        Assert.That(trial.State.Outcome,Is.Not.EqualTo(Outcome.Active));
                        rows.Add($"{encounter},{preference},{at},{ally.Id},{enemy.Id},{Survivors(trial)},{trial.State.Units.Where(u=>!u.Enemy).Sum(u=>u.Hp)},{trial.State.Step},{trial.State.Outcome}");
                    }
                }
            }
            System.IO.Directory.CreateDirectory("Logs/hq");
            System.IO.File.WriteAllLines("Logs/hq/instruction-consequences.csv",rows);
        }
        [Test]
        public void GauntletBoardsSitInsideTheirFloorRooms()
        {
            var map=new FloorMap();
            for(var i=0;i<Fixture.EncounterCount;i++)
            {
                var origin=GauntletRooms.Origin(map,i);
                foreach(var u in Fixture.Encounter(i).Units)Assert.That(map.Walkable(u.Position.Offset(origin.X,origin.Y)),Is.True,"Encounter "+i+" "+u.Id);
                if(i>0)Assert.That(Fixture.Encounter(i).Units.All(u=>map.RoomAt(u.Position.Offset(origin.X,origin.Y))==GauntletRooms.Room[i]),Is.True);
            }
        }
    }
}
