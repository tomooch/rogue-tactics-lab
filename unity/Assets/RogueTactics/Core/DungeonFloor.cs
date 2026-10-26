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
        public FloorMap()
        {
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
    public sealed class FloorTraveller
    {
        private readonly Dictionary<string,GridPosition> party=new Dictionary<string,GridPosition>();
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
        public bool Moving { get; private set; }
        public bool Paused { get; private set; }
        public TravelStop Stop { get; private set; }
        public FloorTraveller(FloorMap map,IEnumerable<Unit> survivors)
        {
            Map=map??throw new ArgumentNullException(nameof(map));
            foreach(var unit in survivors.Where(u=>!u.Enemy && u.Alive).OrderBy(u=>u.Id,StringComparer.Ordinal))
            {if(!map.Walkable(unit.Position) || party.ContainsValue(unit.Position))throw new ArgumentException("Invalid floor handoff");party.Add(unit.Id,unit.Position);health.Add(unit.Id,unit.Hp);}
            if(party.Count==0)throw new ArgumentException("No living explorers");Reveal();
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
