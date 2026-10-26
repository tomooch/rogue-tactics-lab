using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core;
using RogueTactics.Core.G1;
using RogueTactics.Core.Exploration;

namespace RogueTactics.Tests
{
    public class FloorExplorationTests
    {
        private static EncounterState Finish(Preference preference)
        {var replay=new Replay(Fixture.Baseline(),preference);while(replay.State.Outcome==Outcome.Active)replay.Advance();return replay.State;}
        [Test]
        public void AllEightRoomsConnectAcrossAllFourSidesAndGapsRemainWalls()
        {
            var map=new FloorMap();Assert.That(map.Rooms.Count,Is.EqualTo(8));Assert.That(map.Cells.Count(),Is.GreaterThan(300));
            foreach(var room in map.Rooms)Assert.That(map.Path(map.Rooms[0].Center,room.Center).Count,Is.GreaterThan(0));
            Assert.That(map.Cells.Any(p=>p.X<0),Is.True);Assert.That(map.Cells.Any(p=>p.Y<0),Is.True);
            foreach(var p in new[]{new GridPosition(-1,2),new GridPosition(2,-1),new GridPosition(5,2),new GridPosition(2,5)})Assert.That(map.Walkable(p),Is.True);
            Assert.That(map.Walkable(new GridPosition(8,10)),Is.False);
            Assert.That(map.Path(new GridPosition(2,2),new GridPosition(8,10)),Is.Empty);
        }
        [TestCase(Preference.ProtectAnchor,4)] [TestCase(Preference.DirectAttack,3)]
        public void HandoffPreservesExactPositionsHealthAndDeaths(Preference preference,int alive)
        {
            var result=Finish(preference);var floor=new FloorTraveller(new FloorMap(),result.Units);
            Assert.That(floor.Party.Count,Is.EqualTo(alive));
            foreach(var unit in result.Units.Where(u=>!u.Enemy))
                if(unit.Alive){Assert.That(floor.Party[unit.Id],Is.EqualTo(unit.Position));Assert.That(floor.Health[unit.Id],Is.EqualTo(unit.Hp));}
                else Assert.That(floor.Party.ContainsKey(unit.Id),Is.False);
        }
        [Test]
        public void UnknownDestinationIsRejectedAndInitialKnowledgeDoesNotContainFarRooms()
        {
            var floor=new FloorTraveller(new FloorMap(),Fixture.Baseline().Units);
            Assert.That(floor.DiscoveredRooms,Is.EqualTo(1));Assert.That(floor.KnownCells.Contains(new GridPosition(26,20)),Is.False);
            Assert.That(floor.KnownCells.Contains(new GridPosition(-9,2)),Is.False);Assert.That(floor.Entrust(new GridPosition(-9,2)),Is.False);
            Assert.That(floor.KnownCells.Contains(new GridPosition(2,-10)),Is.False);Assert.That(floor.Entrust(new GridPosition(2,-10)),Is.False);
            var before=floor.Party.ToArray();Assert.That(floor.Entrust(new GridPosition(26,20)),Is.False);CollectionAssert.AreEqual(before,floor.Party.ToArray());
        }
        [Test]
        public void CorridorEntrustmentMovesPartyLegallyAndStopsAtDiscoveryWithoutHealing()
        {
            var state=Finish(Preference.ProtectAnchor);var floor=new FloorTraveller(new FloorMap(),state.Units);
            var target=floor.KnownCells.Where(p=>floor.Map.RoomAt(p)<0 && p.X>=5).OrderByDescending(p=>p.X).First();
            Assert.That(floor.Entrust(target),Is.True);var steps=0;
            while(floor.Moving && steps++<100)
            {
                var step=floor.Advance();foreach(var move in step.Moves){Assert.That(Rules.Distance(move.From,move.To),Is.EqualTo(1));Assert.That(floor.Map.Walkable(move.To),Is.True);}
                Assert.That(floor.Party.Values.Distinct().Count(),Is.EqualTo(floor.Party.Count));
            }
            Assert.That(steps,Is.LessThan(100));Assert.That(floor.Stop,Is.EqualTo(TravelStop.NewRoom));Assert.That(floor.DiscoveredRooms,Is.EqualTo(2));
            foreach(var unit in state.Units.Where(u=>!u.Enemy && u.Alive))Assert.That(floor.Health[unit.Id],Is.EqualTo(unit.Hp));
        }
        [Test]
        public void PauseAndCancellationDoNotAdvanceOrReassignTheParty()
        {
            var floor=new FloorTraveller(new FloorMap(),Fixture.Baseline().Units);
            Assert.That(floor.Entrust(new GridPosition(5,3)),Is.True);floor.Advance();floor.SetPaused(true);var before=floor.Party.ToArray();
            Assert.That(floor.Advance().Moves,Is.Empty);CollectionAssert.AreEqual(before,floor.Party.ToArray());
            floor.Cancel();Assert.That(floor.Advance().Moves,Is.Empty);CollectionAssert.AreEqual(before,floor.Party.ToArray());
        }
        [Test]
        public void TravelIsDeterministicAndTheFrozenG1HashesAreUnaffected()
        {
            string Run(){var f=new FloorTraveller(new FloorMap(),Fixture.Baseline().Units);Assert.That(f.Entrust(new GridPosition(5,3)),Is.True);var moves=new List<string>();var count=0;
                while(f.Moving && count++<100)foreach(var m in f.Advance().Moves)moves.Add(m.Actor+":"+m.From.X+","+m.From.Y+">"+m.To.X+","+m.To.Y);Assert.That(count,Is.LessThan(100));return string.Join("|",moves);}
            Assert.That(Run(),Is.EqualTo(Run()));
            foreach(var preference in new[]{Preference.ProtectAnchor,Preference.DirectAttack})
            {var replay=new Replay(Fixture.Baseline(),preference);while(replay.State.Outcome==Outcome.Active)replay.Advance();Assert.That(replay.Hash(),Is.EqualTo(preference==Preference.ProtectAnchor?"e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d":"85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e"));}
        }
    }
}
