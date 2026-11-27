using System;
using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core.G1;

namespace RogueTactics.Core.Exploration
{
    public sealed class FloorRoom
    {
        public int Id { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public FloorRoom(int id,int x,int y,int width,int height)
        {Id=id;X=x;Y=y;Width=width;Height=height;}
        public bool Contains(GridPosition p)=>p.X>=X && p.Y>=Y && p.X<X+Width && p.Y<Y+Height;
        public GridPosition Center=>new GridPosition(X+Width/2,Y+Height/2);
    }
    // Where each gauntlet encounter's 5x5 board sits on the floor. Encounter 0 is the G1 board at the origin.
    public static class GauntletRooms
    {
        public static readonly int[] Room={0,1,2};
        public static readonly GridPosition Spring=new GridPosition(22,4);
        public static GridPosition Origin(FloorMap map,int encounter)
        {var r=map.Rooms[Room[encounter]];return map.Number==1 && encounter==0?new GridPosition(0,0):new GridPosition(r.X+(r.Width-5)/2,r.Y+(r.Height-5)/2);}
    }

    // A deterministic exploration experiment, independent of the frozen G1 fixture.
    public sealed class FloorMap
    {
        private readonly HashSet<GridPosition> cells=new HashSet<GridPosition>();
        private readonly FloorRoom[] rooms;
        public IReadOnlyList<FloorRoom> Rooms=>Array.AsReadOnly(rooms);
        public IEnumerable<GridPosition> Cells=>cells.OrderBy(p=>p.Y).ThenBy(p=>p.X);
        public int Width=>cells.Max(p=>p.X)-cells.Min(p=>p.X)+1;
        public int Height=>cells.Max(p=>p.Y)-cells.Min(p=>p.Y)+1;
        private static readonly GridPosition[] Directions={new GridPosition(1,0),new GridPosition(0,1),new GridPosition(-1,0),new GridPosition(0,-1)};
        public int Number { get; }
        public GridPosition Spring => Number==1?GauntletRooms.Spring:rooms[3].Center;
        public GridPosition Stairs => rooms[Number==1?3:4].Center;

        public FloorMap(int number=1)
        {
            Number=number;
            if(number==3)
            {
                rooms=new[]{new FloorRoom(0,-1,-1,7,7),new FloorRoom(1,11,0,7,7),
                    new FloorRoom(2,11,13,7,7),new FloorRoom(3,23,0,7,7),new FloorRoom(4,23,13,7,7)};
                foreach(var r in rooms)Add(r.X,r.Y,r.Width,r.Height);
                Add(5,2,7,2);Add(13,6,2,8);Add(17,2,7,2);Add(17,15,7,2);
                return;
            }
            if(number==2)
            {
                rooms=new[]{new FloorRoom(0,-1,-1,7,7),new FloorRoom(1,0,11,7,7),
                    new FloorRoom(2,13,11,7,7),new FloorRoom(3,0,23,7,7),new FloorRoom(4,13,23,7,7)};
                foreach(var r in rooms)Add(r.X,r.Y,r.Width,r.Height);
                Add(2,5,2,7);Add(6,13,8,2);Add(2,17,2,7);Add(15,17,2,7);
                return;
            }
            if(number!=1)throw new ArgumentOutOfRangeException(nameof(number));
            // Exploration ground surrounds the unchanged G1 battle fixture. G1
            // still owns its original 5x5 legal combat board and initial actors.
            rooms=new[]{new FloorRoom(0,-3,-3,8,8),new FloorRoom(1,10,0,7,7),new FloorRoom(2,23,2,7,7),
                new FloorRoom(3,22,17,8,7),new FloorRoom(4,9,19,7,7),new FloorRoom(5,0,12,6,7),
                new FloorRoom(6,-12,-1,7,7),new FloorRoom(7,-1,-13,7,7)};
            foreach(var r in rooms)Add(r.X,r.Y,r.Width,r.Height);
            Add(4,2,7,2);Add(16,4,8,2);Add(26,8,2,10);Add(15,20,8,2);
            Add(3,17,2,5);Add(3,20,7,2);Add(2,4,2,9);
            // South/west are real walkable connections, independent of G1 combat.
            Add(-6,2,7,2);Add(2,-7,2,8);
            Add(-10,-10,2,10);Add(-9,-11,9,2);
        }
        private void Add(int x,int y,int w,int h)
        {for(var j=y;j<y+h;j++)for(var i=x;i<x+w;i++)cells.Add(new GridPosition(i,j));}
        public bool Walkable(GridPosition p)=>cells.Contains(p);
        public int RoomAt(GridPosition p)=>rooms.FirstOrDefault(r=>r.Contains(p))?.Id??-1;
        public IEnumerable<GridPosition> Neighbours(GridPosition p)=>Directions.Select(d=>p.Offset(d.X,d.Y)).Where(Walkable);
        public IReadOnlyList<GridPosition> Path(GridPosition start,GridPosition end,ISet<GridPosition> blocked=null)
        {
            if(!Walkable(start) || !Walkable(end) || (blocked!=null && blocked.Contains(end)))return Array.Empty<GridPosition>();
            var previous=new Dictionary<GridPosition,GridPosition>();var seen=new HashSet<GridPosition>{start};var queue=new Queue<GridPosition>();queue.Enqueue(start);
            while(queue.Count>0)
            {
                var p=queue.Dequeue();if(p.Equals(end))
                {var path=new List<GridPosition>{p};while(!p.Equals(start)){p=previous[p];path.Add(p);}path.Reverse();return path.AsReadOnly();}
                foreach(var n in Neighbours(p))if((blocked==null || !blocked.Contains(n)) && seen.Add(n)){previous[n]=p;queue.Enqueue(n);}
            }
            return Array.Empty<GridPosition>();
        }
    }

    public readonly struct FloorMove
    {
        public string Actor { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public FloorMove(string actor,GridPosition from,GridPosition to){Actor=actor;From=from;To=to;}
    }
    public enum TravelStop { None, Arrived, NewRoom, Blocked, Cancelled }
    public sealed class FloorStep
    {
        public IReadOnlyList<FloorMove> Moves { get; }
        public TravelStop Stop { get; }
        internal FloorStep(List<FloorMove> moves,TravelStop stop){Moves=moves.AsReadOnly();Stop=stop;}
    }
    public sealed class FloorCheckpoint
    {
        public IReadOnlyDictionary<string,GridPosition> Party { get; }
        public IReadOnlyList<GridPosition> KnownCells { get; }
        public IReadOnlyList<int> DiscoveredRooms { get; }
        public bool VictoryRestApplied { get; }
        public bool SpringUsed { get; }

        public FloorCheckpoint(IEnumerable<KeyValuePair<string,GridPosition>> party,
            IEnumerable<GridPosition> knownCells,IEnumerable<int> discoveredRooms,bool victoryRestApplied=false,bool springUsed=false)
        {
            Party=new System.Collections.ObjectModel.ReadOnlyDictionary<string,GridPosition>(
                party.ToDictionary(entry=>entry.Key,entry=>entry.Value));
            KnownCells=Array.AsReadOnly(knownCells.ToArray());
            DiscoveredRooms=Array.AsReadOnly(discoveredRooms.ToArray());
            VictoryRestApplied=victoryRestApplied;
            SpringUsed=springUsed;
        }
    }
    public sealed class FloorTraveller
    {
        private readonly Dictionary<string,GridPosition> party=new Dictionary<string,GridPosition>();
        private readonly Dictionary<string,int> maxHealth=new Dictionary<string,int>();
        private readonly Dictionary<string,int> health=new Dictionary<string,int>();
        private readonly HashSet<int> rooms=new HashSet<int>{0};
        private readonly HashSet<GridPosition> known=new HashSet<GridPosition>();
        private string[] order=Array.Empty<string>();
        private GridPosition destination;
        public FloorMap Map { get; }
        public IReadOnlyDictionary<string,GridPosition> Party=>new System.Collections.ObjectModel.ReadOnlyDictionary<string,GridPosition>(party);
        public IReadOnlyDictionary<string,int> Health=>new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(health);
        public IEnumerable<GridPosition> KnownCells=>known.OrderBy(p=>p.Y).ThenBy(p=>p.X);
        public int DiscoveredRooms=>rooms.Count;
        public bool VictoryRestApplied { get; private set; }
        public bool SpringUsed { get; private set; }
        public bool Moving { get; private set; }
        public bool Paused { get; private set; }
        public TravelStop Stop { get; private set; }
        public FloorTraveller(FloorMap map,IEnumerable<Unit> survivors):this(map,survivors,new GridPosition(0,0)){}
        // origin places a 5x5 encounter board inside its floor room.
        public FloorTraveller(FloorMap map,IEnumerable<Unit> survivors,GridPosition origin)
        {
            Map=map??throw new ArgumentNullException(nameof(map));
            foreach(var unit in survivors.Where(u=>!u.Enemy && u.Alive).OrderBy(u=>u.Id,StringComparer.Ordinal))
            {var at=unit.Position.Offset(origin.X,origin.Y);if(!map.Walkable(at) || party.ContainsValue(at))throw new ArgumentException("Invalid floor handoff");party.Add(unit.Id,at);health.Add(unit.Id,unit.Hp);maxHealth.Add(unit.Id,unit.MaxHp);}
            if(party.Count==0)throw new ArgumentException("No living explorers");Reveal();
        }
        public static FloorTraveller AfterVictory(FloorMap map,EncounterState victory,GridPosition origin,int revivalPercent)
        {
            var recovered=PartyRecovery.AfterVictory(victory,revivalPercent);
            var traveller=new FloorTraveller(map,victory.Units,origin);
            foreach(var ally in victory.Units.Where(unit=>!unit.Enemy))
            {
                if(!ally.Alive)
                {
                    var cell=ally.Position.Offset(origin.X,origin.Y);
                    if(traveller.party.ContainsValue(cell))
                        cell=map.Cells.Where(position=>!traveller.party.ContainsValue(position))
                            .OrderBy(position=>Rules.Distance(position,cell)).First();
                    traveller.party.Add(ally.Id,cell);
                }
                traveller.health[ally.Id]=recovered[ally.Id];
                traveller.maxHealth[ally.Id]=ally.MaxHp;
            }
            traveller.VictoryRestApplied=true;
            traveller.Reveal();
            return traveller;
        }

        public FloorCheckpoint Capture()=>new FloorCheckpoint(party,KnownCells,rooms.OrderBy(room=>room),VictoryRestApplied,SpringUsed);

        public bool TryDrinkSpring()
        {
            if(!VictoryRestApplied || SpringUsed || !known.Contains(Map.Spring)
                || !party.Values.Any(position=>Rules.Distance(position,Map.Spring)<=1))return false;
            ApplySpring();
            return true;
        }

        public IReadOnlyDictionary<string,int> HealthAfterDescent()
        {
            if(!VictoryRestApplied || !known.Contains(Map.Stairs) ||
                !party.Values.Any(position=>Rules.Distance(position,Map.Stairs)<=1))
                throw new InvalidOperationException("The party must reach the stairs after battle");
            return health.ToDictionary(entry=>entry.Key,entry=>Recovery(entry.Key,entry.Value));
        }

        private int Recovery(string actor,int hp)
            => (int)Math.Min(maxHealth[actor],hp+((long)maxHealth[actor]+1)/2);

        private void ApplySpring()
        {
            foreach(var actor in party.Keys)
                health[actor]=Recovery(actor,health[actor]);
            SpringUsed=true;
        }

        public static FloorTraveller Restore(FloorMap map,EncounterState battle,GridPosition origin,FloorCheckpoint checkpoint)
        {
            if(checkpoint==null)throw new ArgumentNullException(nameof(checkpoint));
            var traveller=checkpoint.VictoryRestApplied?AfterVictory(map,battle,origin,25):
                new FloorTraveller(map,battle.Units,origin);
            var savedKnown=new HashSet<GridPosition>(checkpoint.KnownCells);
            var savedRooms=new HashSet<int>(checkpoint.DiscoveredRooms);
            if(!savedRooms.Contains(0) || !savedRooms.IsSubsetOf(map.Rooms.Select(room=>room.Id))
                || !new HashSet<string>(traveller.party.Keys).SetEquals(checkpoint.Party.Keys)
                || checkpoint.Party.Values.Distinct().Count()!=checkpoint.Party.Count
                || checkpoint.Party.Values.Any(position=>!savedKnown.Contains(position))
                || savedKnown.Any(position=>!map.Walkable(position))
                || map.Cells.Any(position=>savedRooms.Contains(map.RoomAt(position)) && !savedKnown.Contains(position)))
                throw new ArgumentException("Invalid exploration checkpoint",nameof(checkpoint));
            foreach(var entry in checkpoint.Party)traveller.party[entry.Key]=entry.Value;
            traveller.known.Clear();
            traveller.known.UnionWith(savedKnown);
            traveller.rooms.Clear();
            traveller.rooms.UnionWith(savedRooms);
            if(checkpoint.SpringUsed)
            {
                if(!checkpoint.VictoryRestApplied || !savedKnown.Contains(map.Spring))
                    throw new ArgumentException("Invalid used spring",nameof(checkpoint));
                traveller.ApplySpring();
            }
            return traveller;
        }

        private void Reveal()
        {
            foreach(var room in Map.Rooms)
                if(party.Values.Any(p=>Math.Max(room.X-p.X,Math.Max(0,p.X-(room.X+room.Width-1)))+Math.Max(room.Y-p.Y,Math.Max(0,p.Y-(room.Y+room.Height-1)))<=2))rooms.Add(room.Id);
            foreach(var cell in Map.Cells)
                if(rooms.Contains(Map.RoomAt(cell)) || party.Values.Any(p=>Rules.Distance(p,cell)<=2))known.Add(cell);
        }
        private GridPosition CorridorEnd(GridPosition requested)
        {
            if(Map.RoomAt(requested)>=0)return requested;
            // A known corridor entrance entrusts traversal to its next room. Unknown
            // geometry is used by Core only; no route through it is exposed to UI.
            var queue=new Queue<GridPosition>();var seen=new HashSet<GridPosition>{requested};queue.Enqueue(requested);
            while(queue.Count>0)
            {
                var p=queue.Dequeue();var room=Map.RoomAt(p);if(room>=0 && !rooms.Contains(room))return p;
                foreach(var n in Map.Neighbours(p))if(!rooms.Contains(Map.RoomAt(n)) && seen.Add(n))queue.Enqueue(n);
            }
            return requested;
        }
        public bool Entrust(GridPosition requested)
        {
            if(Moving || !known.Contains(requested) || !Map.Walkable(requested))return false;
            destination=CorridorEnd(requested);
            var candidates=party.Select(u=>new{u.Key,Path=Map.Path(u.Value,destination,new HashSet<GridPosition>(party.Where(v=>v.Key!=u.Key).Select(v=>v.Value)))})
                .Where(p=>p.Path.Count>0).OrderBy(p=>p.Path.Count).ThenBy(p=>p.Key,StringComparer.Ordinal).ToArray();
            if(candidates.Length==0){Stop=TravelStop.Blocked;return false;}
            var leader=candidates[0].Key;
            order=new[]{leader}.Concat(party.Keys.Where(k=>k!=leader).OrderBy(k=>Rules.Distance(party[k],party[leader])).ThenBy(k=>k,StringComparer.Ordinal)).ToArray();
            Moving=true;Paused=false;Stop=TravelStop.None;return true;
        }
        public void SetPaused(bool paused){Paused=paused;}
        public void Cancel(){Moving=false;Paused=false;Stop=TravelStop.Cancelled;}
        public FloorStep Advance()
        {
            var moves=new List<FloorMove>();if(!Moving || Paused)return new FloorStep(moves,Stop);
            var before=party.ToDictionary(p=>p.Key,p=>p.Value);var count=rooms.Count;
            for(var i=0;i<order.Length;i++)
            {
                var id=order[i];var goal=i==0?destination:before[order[i-1]];
                var blocked=new HashSet<GridPosition>(party.Where(p=>p.Key!=id).Select(p=>p.Value));var path=Map.Path(party[id],goal,blocked);
                if(path.Count>1){var from=party[id];party[id]=path[1];moves.Add(new FloorMove(id,from,path[1]));}
            }
            Reveal();
            Stop=rooms.Count>count?TravelStop.NewRoom:party[order[0]].Equals(destination)?TravelStop.Arrived:moves.Count==0?TravelStop.Blocked:TravelStop.None;
            if(Stop!=TravelStop.None)Moving=false;
            return new FloorStep(moves,Stop);
        }
    }
}
