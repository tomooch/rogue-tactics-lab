using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;

namespace RogueTactics.Tests
{
    public class MobileG1SessionTests
    {
        [TestCase(Preference.ProtectAnchor,"e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d")]
        [TestCase(Preference.DirectAttack,"85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e")]
        public void MobileAdapterPreservesFrozenG1Trace(Preference preference,string expected)
        {
            var session=new MobileG1Session();session.Select(preference);session.Start("自分の予想");
            while(session.Replay.State.Outcome==Outcome.Active)session.Advance();
            Assert.That(session.Replay.Hash(),Is.EqualTo(expected));Assert.That(session.Trials[preference].Prediction,Is.EqualTo("自分の予想"));
            session.Reset();Assert.That(session.Replay.State.Serialize(),Is.EqualTo(Fixture.Baseline().Serialize()));Assert.That(session.Trials.Count,Is.EqualTo(1));
        }
        [Test]
        public void CarriedHealthAndRoomGeometrySurviveAReplayReset()
        {
            var session=new MobileG1Session();
            session.Start("");
            while(session.Replay.State.Outcome==Outcome.Active)session.Advance();
            Assert.That(session.Replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            var partyHp=session.Replay.State.Units.Where(unit=>!unit.Enemy).ToDictionary(unit=>unit.Id,unit=>unit.Hp);
            Assert.That(partyHp["A"],Is.LessThan(Fixture.Baseline().Find("A").MaxHp));
            var room=Fixture.Encounter(1);
            var fullHp=room.Units.Where(unit=>!unit.Enemy).ToDictionary(unit=>unit.Id,unit=>unit.MaxHp);
            Assert.That(EncounterBuilder.Build(room,fullHp).Serialize(),Is.EqualTo(room.Serialize()));
            session.LoadEncounter(1,partyHp);
            var state=session.Replay.State;
            foreach(var unit in state.Units.Where(unit=>!unit.Enemy))
                Assert.That(unit.Hp,Is.EqualTo(partyHp[unit.Id]));
            Assert.That(state.Units.Select(unit=>unit.MaxHp),Is.EqualTo(room.Units.Select(unit=>unit.MaxHp)));
            Assert.That(state.Units.Select(unit=>unit.Position),Is.EqualTo(room.Units.Select(unit=>unit.Position)));
            Assert.That(state.Initiative,Is.EqualTo(room.Initiative));
            Assert.That(state.HealCharges,Is.EqualTo(2));
            Assert.That(session.Replay.CorrectionsLeft,Is.EqualTo(1));
            var entry=state.Serialize();
            session.Start("retry");
            session.Advance();
            session.Reset();
            Assert.That(session.Replay.State.Serialize(),Is.EqualTo(entry));
        }

        [Test]
        public void InvalidCarriedHealthDoesNotReplaceTheCurrentEncounter()
        {
            var session=new MobileG1Session();
            var replay=session.Replay;
            var hp=new Dictionary<string,int>{{"A",0},{"B",16},{"C",12},{"D",10}};
            Assert.That(()=>session.LoadEncounter(1,hp),Throws.TypeOf<System.ArgumentOutOfRangeException>());
            hp["A"]=19;
            Assert.That(()=>session.LoadEncounter(1,hp),Throws.TypeOf<System.ArgumentOutOfRangeException>());
            hp.Remove("A");
            Assert.That(()=>session.LoadEncounter(1,hp),Throws.TypeOf<System.ArgumentException>());
            Assert.That(session.EncounterIndex,Is.Zero);
            Assert.That(session.Replay,Is.SameAs(replay));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void RestoreImmediatelyAfterCorrectionPreservesInputAndTheRemainingReplay(bool targetOnce)
        {
            var original=new MobileG1Session();
            original.Start("戦いの前の予想");
            original.Advance();
            if(targetOnce)original.TargetOnce("C","Y");
            else original.Correct(Preference.DirectAttack);

            var restored=new MobileG1Session();
            restored.RestoreBattle(original.CaptureBattle());
            Assert.That(restored.Replay.CorrectionsLeft,Is.Zero);
            Assert.That(restored.Replay.PendingTarget,Is.EqualTo(original.Replay.PendingTarget));
            Assert.That(restored.Preference,Is.EqualTo(original.Preference));
            Assert.That(restored.Prediction,Is.EqualTo(original.Prediction));
            Assert.That(restored.Replay.State.Serialize(),Is.EqualTo(original.Replay.State.Serialize()));
            while(original.Replay.State.Outcome==Outcome.Active)original.Advance();
            while(restored.Replay.State.Outcome==Outcome.Active)restored.Advance();
            Assert.That(restored.Replay.Hash(),Is.EqualTo(original.Replay.Hash()));

            var terminal=new MobileG1Session();
            terminal.RestoreBattle(restored.CaptureBattle());
            Assert.That(terminal.Replay.State.Outcome,Is.EqualTo(Outcome.Victory));
            Assert.That(terminal.Trials[original.Replay.InitialPreference].Hash,Is.EqualTo(original.Replay.Hash()));
        }

        [Test]
        public void RestoreDefeatAndRejectCheckpointThatContinuesPastTheEnd()
        {
            var room=new EncounterState(3,3,new[]{
                new Unit("A",Role.Anchor,1,1,new GridPosition(1,1)),
                new Unit("X",Role.Enemy,20,6,new GridPosition(1,2))
            },new[]{"X","A"});
            var original=new MobileG1Session(room);
            original.Start("");
            original.Advance();
            Assert.That(original.Replay.State.Outcome,Is.EqualTo(Outcome.Defeat));

            var restored=new MobileG1Session(room);
            restored.RestoreBattle(original.CaptureBattle());
            Assert.That(restored.Replay.Hash(),Is.EqualTo(original.Replay.Hash()));
            Assert.That(restored.Replay.State.Outcome,Is.EqualTo(Outcome.Defeat));
            var before=restored.Replay;
            var invalid=new BattleCheckpoint(0,Preference.ProtectAnchor,2,"",null);
            Assert.That(()=>restored.RestoreBattle(invalid),Throws.InvalidOperationException);
            Assert.That(restored.Replay,Is.SameAs(before));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void DiskCheckpointOverwritesAndRestoresImmediatelyAfterCorrection(bool targetOnce)
        {
            var folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            var path=Path.Combine(folder,"battle.json");
            var store=new BattleCheckpointStore(path);
            try
            {
                Assert.That(store.Read(out _),Is.EqualTo(BattleCheckpointReadStatus.Missing));
                var session=new MobileG1Session();
                session.Start("任せる前の予想");
                session.Advance();
                Assert.That(store.TryWrite(session.CaptureBattle()),Is.True);
                Assert.That(store.Read(out var initial),Is.EqualTo(BattleCheckpointReadStatus.Loaded));
                Assert.That(initial.Input,Is.Null);
                if(targetOnce)session.TargetOnce("C","Y");
                else session.Correct(Preference.DirectAttack);
                Assert.That(store.TryWrite(session.CaptureBattle()),Is.True);
                Assert.That(Directory.GetFiles(folder),Is.EqualTo(new[]{path}));
                var reopened=new BattleCheckpointStore(path);
                Assert.That(reopened.Read(out var saved),Is.EqualTo(BattleCheckpointReadStatus.Loaded));
                var restored=new MobileG1Session();
                restored.RestoreBattle(saved);
                Assert.That(restored.Replay.CorrectionsLeft,Is.Zero);
                Assert.That(restored.Replay.PendingTarget,Is.EqualTo(session.Replay.PendingTarget));
                Assert.That(restored.Preference,Is.EqualTo(session.Preference));
                Assert.That(restored.Prediction,Is.EqualTo(session.Prediction));
                while(session.Replay.State.Outcome==Outcome.Active)session.Advance();
                while(restored.Replay.State.Outcome==Outcome.Active)restored.Advance();
                Assert.That(restored.Replay.Hash(),Is.EqualTo(session.Replay.Hash()));
            }
            finally
            {
                if(Directory.Exists(folder))Directory.Delete(folder,true);
            }
        }

        [TestCase("{")]
        [TestCase("{\"format\":2,\"rules\":1,\"room\":0,\"policy\":0,\"steps\":0,\"inputs\":[]}")]
        [TestCase("{\"format\":1,\"rules\":2,\"room\":0,\"policy\":0,\"steps\":0,\"inputs\":[]}")]
        [TestCase("{\"format\":1,\"rules\":1,\"room\":0,\"steps\":0,\"inputs\":[]}")]
        public void RejectedCheckpointIsPreservedOutsideTheActiveSlot(string json)
        {
            var folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            var path=Path.Combine(folder,"battle.json");
            Directory.CreateDirectory(folder);
            try
            {
                File.WriteAllText(path,json);
                var store=new BattleCheckpointStore(path);
                Assert.That(store.Read(out var checkpoint),Is.EqualTo(BattleCheckpointReadStatus.Rejected));
                Assert.That(checkpoint,Is.Null);
                Assert.That(File.Exists(path),Is.False);
                var rejected=Directory.GetFiles(folder,"battle.json.rejected.*").Single();
                Assert.That(File.ReadAllText(rejected),Is.EqualTo(json));
                Assert.That(store.Read(out _),Is.EqualTo(BattleCheckpointReadStatus.Missing));
            }
            finally
            {
                Directory.Delete(folder,true);
            }
        }
    }
}
