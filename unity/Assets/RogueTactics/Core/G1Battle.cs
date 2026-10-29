using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RogueTactics.Core.G1
{
    // Anonymous experiment roles. These are NOT the approved named character roster.
    public enum Role { Anchor, Protector, Flanker, Support, Enemy }
    public enum Preference { ProtectAnchor, DirectAttack }
    public enum ActionKind { Maintain, Move, Attack, Protect, Heal }
    public enum EventKind { Action, Move, Protect, Damage, Heal, Counter, Death, End }
    public enum Outcome { Active, Victory, Defeat, Limit }

    public sealed class Unit
    {
        public string Id { get; }
        public Role Role { get; }
        public bool Enemy => Role == Role.Enemy;
        public int MaxHp { get; }
        public int Attack { get; }
        public int Hp { get; internal set; }
        public GridPosition Position { get; internal set; }
        public bool Alive => Hp > 0;
        public Unit(string id, Role role, int hp, int attack, GridPosition position)
        { Id = id; Role = role; Hp = MaxHp = hp; Attack = attack; Position = position; }
        internal Unit Copy() => new Unit(Id, Role, MaxHp, Attack, Position) { Hp = Hp };
    }

    public sealed class EncounterState
    {
        private readonly Unit[] units;
        public IReadOnlyList<Unit> Units => Array.AsReadOnly(units);
        public int Width { get; }
        public int Height { get; }
        public int Step { get; internal set; }
        public int Cursor { get; internal set; }
        public int HealCharges { get; internal set; } = 2;
        public bool Guard { get; internal set; }
        public Outcome Outcome { get; internal set; }
        private readonly string[] initiative;
        public IReadOnlyList<string> Initiative => Array.AsReadOnly(initiative);
        public EncounterState(int width, int height, IEnumerable<Unit> source) : this(width, height, source, Fixture.Initiative) {}
        public EncounterState(int width, int height, IEnumerable<Unit> source, IEnumerable<string> order)
        {
            Width = width; Height = height; units = source.Select(u => u.Copy()).ToArray(); initiative = order.ToArray();
            if (width < 1 || height < 1 || units.Select(u => u.Id).Distinct().Count() != units.Length ||
                units.Any(u => u.Hp <= 0 || u.Attack < 0 || !Inside(u.Position)) ||
                units.Select(u => u.Position).Distinct().Count() != units.Length ||
                // A supplied order must name exactly these actors; the implied G1 order keeps its old leniency.
                (!ReferenceEquals(order, Fixture.Initiative) && (initiative.Length != units.Length ||
                 !initiative.OrderBy(i => i, StringComparer.Ordinal).SequenceEqual(units.Select(u => u.Id).OrderBy(i => i, StringComparer.Ordinal)))))
                throw new ArgumentException("Invalid encounter fixture");
        }
        public Unit Find(string id) => units.FirstOrDefault(u => u.Id == id);
        public bool Inside(GridPosition p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
        public bool Empty(GridPosition p) => Inside(p) && !units.Any(u => u.Alive && u.Position.Equals(p));
        private EncounterState(EncounterState source)
        { Width = source.Width; Height = source.Height; units = source.units.Select(u => u.Copy()).ToArray(); initiative = source.initiative;
          Step = source.Step; Cursor = source.Cursor; HealCharges = source.HealCharges; Guard = source.Guard; Outcome = source.Outcome; }
        public EncounterState Copy() => new EncounterState(this);
        public string Serialize()
        {
            var b = new StringBuilder();
            b.AppendFormat(CultureInfo.InvariantCulture, "g1-v1|{0},{1}|{2},{3},{4},{5},{6}", Width, Height, Step, Cursor, HealCharges, Guard ? 1 : 0, (int)Outcome);
            foreach (var u in units)
                b.AppendFormat(CultureInfo.InvariantCulture, "|{0},{1},{2},{3},{4},{5},{6}", u.Id, (int)u.Role, u.MaxHp, u.Hp, u.Attack, u.Position.X, u.Position.Y);
            // The frozen G1 order is implied, which keeps existing G1 serializations and hashes byte-identical.
            if (!initiative.SequenceEqual(Fixture.Initiative)) b.Append("|order:").Append(string.Join(",", initiative));
            return b.ToString();
        }
    }

    public readonly struct Command
    {
        // One explicitly requested Core action. Chunk/horizon belong to adapters, not game UX defaults.
        public Preference Preference { get; }
        public Command(Preference preference)
        { if (!Enum.IsDefined(typeof(Preference), preference)) throw new ArgumentOutOfRangeException(nameof(preference)); Preference = preference; }
    }
    public sealed class Plan
    {
        public string Actor { get; }
        public string Target { get; }
        public ActionKind Kind { get; }
        public GridPosition Destination { get; }
        public string Reason { get; }
        internal Plan(Unit actor, ActionKind kind, Unit target, GridPosition destination, string reason)
        { Actor = actor.Id; Kind = kind; Target = target?.Id ?? ""; Destination = destination; Reason = reason; }
        public string Serialize() => string.Join(",", Actor, (int)Kind, Target, Destination.X.ToString(CultureInfo.InvariantCulture), Destination.Y.ToString(CultureInfo.InvariantCulture), Reason);
    }
    public sealed class BattleEvent
    {
        public int Step { get; }
        public EventKind Kind { get; }
        public string Actor { get; }
        public string Target { get; }
        public int Amount { get; }
        public int HpAfter { get; }
        public GridPosition Position { get; }
        public string Detail { get; }
        internal BattleEvent(int step, EventKind kind, string actor, string target, int amount, int hp, GridPosition position, string detail)
        { Step = step; Kind = kind; Actor = actor; Target = target; Amount = amount; HpAfter = hp; Position = position; Detail = detail; }
        public string Serialize() => string.Join(",", Step.ToString(CultureInfo.InvariantCulture), (int)Kind, Actor, Target,
            Amount.ToString(CultureInfo.InvariantCulture), HpAfter.ToString(CultureInfo.InvariantCulture), Position.X.ToString(CultureInfo.InvariantCulture), Position.Y.ToString(CultureInfo.InvariantCulture), Detail);
    }
    public sealed class ResolvedStep
    {
        public EncounterState State { get; }
        public Plan Plan { get; }
        public IReadOnlyList<BattleEvent> Events { get; }
        internal ResolvedStep(EncounterState state, Plan plan, List<BattleEvent> events)
        { State = state; Plan = plan; Events = events.AsReadOnly(); }
    }

    public static class Fixture
    {
        internal static readonly string[] Initiative = { "B", "X", "A", "C", "Y", "D" };
        public static EncounterState Baseline() => new EncounterState(5, 5, new[] {
            new Unit("A", Role.Anchor, 18, 5, new GridPosition(2,2)),
            new Unit("B", Role.Protector, 16, 4, new GridPosition(1,2)),
            new Unit("C", Role.Flanker, 12, 4, new GridPosition(3,3)),
            new Unit("D", Role.Support, 10, 2, new GridPosition(1,1)),
            new Unit("X", Role.Enemy, 18, 6, new GridPosition(2,3)),
            new Unit("Y", Role.Enemy, 14, 5, new GridPosition(3,2)) });
        // Gauntlet experiment: the same four allies meet differently placed guardians.
        // Encounter 0 is the frozen G1 fixture. Later layouts were chosen so that neither
        // preference is always right (see VERIFICATION). Allies always start at full HP.
        public static EncounterState Encounter(int index)
        {
            switch (index)
            {
                case 0: return Baseline();
                case 1: return new EncounterState(5, 5, new[] {
                    new Unit("A", Role.Anchor, 18, 5, new GridPosition(4,4)),
                    new Unit("B", Role.Protector, 16, 4, new GridPosition(2,1)),
                    new Unit("C", Role.Flanker, 12, 4, new GridPosition(1,0)),
                    new Unit("D", Role.Support, 10, 2, new GridPosition(0,4)),
                    new Unit("X", Role.Enemy, 15, 5, new GridPosition(1,3)),
                    new Unit("Y", Role.Enemy, 11, 4, new GridPosition(2,3)) }, new[] { "B", "D", "Y", "X", "A", "C" });
                case 2: return new EncounterState(5, 5, new[] {
                    new Unit("A", Role.Anchor, 18, 5, new GridPosition(2,3)),
                    new Unit("B", Role.Protector, 16, 4, new GridPosition(3,0)),
                    new Unit("C", Role.Flanker, 12, 4, new GridPosition(4,3)),
                    new Unit("D", Role.Support, 10, 2, new GridPosition(1,4)),
                    new Unit("X", Role.Enemy, 18, 5, new GridPosition(2,1)),
                    new Unit("Y", Role.Enemy, 22, 7, new GridPosition(0,2)) }, new[] { "Y", "D", "A", "X", "C", "B" });
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
        public const int EncounterCount = 3;
    }

    public static class Rules
    {
        public const int ActionLimit = 120; // fixture stop condition only
        public static int Distance(GridPosition a, GridPosition b) => Math.Abs(a.X-b.X) + Math.Abs(a.Y-b.Y);
        private static Unit Next(EncounterState s)
        {
            var order = s.Initiative;
            for (var i = 0; i < order.Count; i++)
            { var u = s.Find(order[(s.Cursor+i) % order.Count]); if (u != null && u.Alive) return u; }
            return null;
        }
        public static Plan Preview(EncounterState s, Command command)
        {
            if (s.Outcome != Outcome.Active) return null;
            var actor = Next(s);
            return actor == null ? null : PlanFor(s, actor, command);
        }
        internal static Plan PreviewTarget(EncounterState s, Command command, string actorId, string targetId)
        {
            var actor = s.Find(actorId); var target = s.Find(targetId);
            if (actor == null || !actor.Alive || target == null || !target.Alive || actor.Enemy || !target.Enemy)
                return Preview(s, command);
            if (Distance(actor.Position, target.Position) == 1)
                return new Plan(actor, ActionKind.Attack, target, actor.Position, "Intervention: attack selected enemy");
            return MoveOrMaintain(s, actor, target, "Intervention: approach selected enemy");
        }
        public static Plan Preview(EncounterState s, Command command, string actorId, string targetId) => PreviewTarget(s, command, actorId, targetId);
        public static IReadOnlyList<Plan> Intents(EncounterState s, Command command, string actorId, string targetId)
        {
            var intents = Intents(s, command).ToList();
            var index = intents.FindIndex(p => p.Actor == actorId);
            if (index >= 0) intents[index] = PreviewTarget(s, command, actorId, targetId);
            return intents.AsReadOnly();
        }
        // Each living actor's present intention, in the order they will act from now.
        // Every plan reads the current state only: it is what they mean to do now, not a simulated future.
        public static IReadOnlyList<Plan> Intents(EncounterState s, Command command)
        {
            if (s.Outcome != Outcome.Active) return Array.Empty<Plan>();
            var order = s.Initiative; var plans = new List<Plan>();
            for (var i = 0; i < order.Count; i++)
            { var u = s.Find(order[(s.Cursor+i) % order.Count]); if (u != null && u.Alive) plans.Add(PlanFor(s, u, command)); }
            return plans.AsReadOnly();
        }
        // Allies whose current HP would be exhausted by the attacks enemies intend right now.
        public static IReadOnlyList<string> Threatened(EncounterState s, Command command)
        {
            var incoming = new Dictionary<string,int>();
            foreach (var p in Intents(s, command).Where(p => p.Kind == ActionKind.Attack))
            {
                var actor = s.Find(p.Actor); var target = s.Find(p.Target);
                if (!actor.Enemy || target.Enemy) continue;
                var protector = s.Find("B");
                var guarded = target.Id == "A" && s.Guard && protector != null && protector.Alive && Distance(protector.Position, target.Position) == 1;
                incoming[target.Id] = (incoming.TryGetValue(target.Id, out var sum) ? sum : 0) + Math.Max(0, actor.Attack - (guarded ? 3 : 0));
            }
            return s.Units.Where(u => !u.Enemy && u.Alive && incoming.TryGetValue(u.Id, out var n) && n >= u.Hp).Select(u => u.Id).ToList().AsReadOnly();
        }
        private static Plan PlanFor(EncounterState s, Unit actor, Command command)
        {
            var a = s.Find("A");
            if (actor.Role == Role.Protector && command.Preference == Preference.ProtectAnchor && a != null && a.Alive)
            {
                if (Distance(actor.Position, a.Position) == 1)
                    return new Plan(actor, ActionKind.Protect, a, actor.Position, "Protect preference; adjacent living A");
                return MoveOrMaintain(s, actor, a, "Approach living A to protect");
            }
            if (actor.Role == Role.Support && s.HealCharges > 0)
            {
                var wounded = s.Units.Where(u => !u.Enemy && u.Alive && u.Hp <= u.MaxHp-4)
                    .OrderBy(u => u.Hp).ThenBy(u => u.Id, StringComparer.Ordinal).FirstOrDefault();
                if (wounded != null)
                {
                    if (Distance(actor.Position, wounded.Position) <= 2)
                        return new Plan(actor, ActionKind.Heal, wounded, actor.Position, "Lowest HP ally in heal range; charge available");
                    return MoveOrMaintain(s, actor, wounded, "Approach wounded ally");
                }
            }
            // Opponents prefer living A; deterministic fallback uses distance, then stable ID.
            var target = actor.Enemy && a != null && a.Alive ? a : s.Units.Where(u => u.Alive && u.Enemy != actor.Enemy)
                .OrderBy(u => Distance(actor.Position, u.Position)).ThenBy(u => u.Id, StringComparer.Ordinal).FirstOrDefault();
            if (target == null) return new Plan(actor, ActionKind.Maintain, null, actor.Position, "No living opponent");
            if (Distance(actor.Position, target.Position) == 1)
                return new Plan(actor, ActionKind.Attack, target, actor.Position, Flank(s, actor, target) ? "Adjacent attack; complementary flank +2" : "Adjacent living opponent");
            return MoveOrMaintain(s, actor, target, "Approach opponent by legal unoccupied step");
        }
        private static Plan MoveOrMaintain(EncounterState s, Unit actor, Unit target, string reason)
        {
            // Stable N/E/S/W tie break; no routing/product traversal semantics are implied.
            foreach (var p in new[] { actor.Position.Offset(0,1), actor.Position.Offset(1,0), actor.Position.Offset(0,-1), actor.Position.Offset(-1,0) })
                if (s.Empty(p) && Distance(p, target.Position) < Distance(actor.Position, target.Position))
                    return new Plan(actor, ActionKind.Move, target, p, reason);
            return new Plan(actor, ActionKind.Maintain, target, actor.Position, "No empty step reduces distance");
        }
        private static bool Flank(EncounterState s, Unit actor, Unit target)
        {
            var a = s.Find("A");
            return actor.Role == Role.Flanker && a != null && a.Alive && Distance(a.Position, target.Position) == 1 &&
                Distance(actor.Position, target.Position) == 1 && !actor.Position.Equals(a.Position);
        }
        public static bool IsLegal(EncounterState s, Plan p)
        {
            if (p == null || s.Outcome != Outcome.Active) return false;
            var u = s.Find(p.Actor); var t = s.Find(p.Target);
            if (u == null || !u.Alive || Next(s)?.Id != u.Id) return false;
            switch (p.Kind)
            {
                case ActionKind.Move: return Distance(u.Position, p.Destination) == 1 && s.Empty(p.Destination);
                case ActionKind.Attack: return t != null && t.Alive && u.Enemy != t.Enemy && Distance(u.Position,t.Position) == 1;
                case ActionKind.Protect: return u.Role == Role.Protector && t?.Id == "A" && t.Alive && Distance(u.Position,t.Position) == 1;
                case ActionKind.Heal: return u.Role == Role.Support && t != null && t.Alive && !t.Enemy && t.Hp < t.MaxHp && s.HealCharges > 0 && Distance(u.Position,t.Position) <= 2;
                case ActionKind.Maintain: return true;
                default: return false;
            }
        }
        public static ResolvedStep Resolve(EncounterState source, Command command)
        {
            var plan = Preview(source, command);
            return Resolve(source, command, plan);
        }
        internal static ResolvedStep Resolve(EncounterState source, Command command, Plan plan)
        {
            if (!IsLegal(source, plan)) throw new InvalidOperationException("No legal action in terminal/invalid state");
            var s = source.Copy(); s.Step++;
            var u = s.Find(plan.Actor); var t = s.Find(plan.Target);
            var events = new List<BattleEvent>();
            Action<EventKind,string,string,int,int,GridPosition,string> emit = (k,actor,target,n,hp,p,d) =>
                events.Add(new BattleEvent(s.Step,k,actor,target,n,hp,p,d));
            if (u.Role == Role.Protector) s.Guard = false;
            emit(EventKind.Action,u.Id,plan.Target,0,u.Hp,u.Position,plan.Kind + ": " + plan.Reason);
            switch (plan.Kind)
            {
                case ActionKind.Move:
                    u.Position = plan.Destination; emit(EventKind.Move,u.Id,u.Id,0,u.Hp,u.Position,"Legal step"); break;
                case ActionKind.Protect:
                    s.Guard = true; emit(EventKind.Protect,u.Id,t.Id,3,t.Hp,t.Position,"Reduce incoming damage by 3 until B acts"); break;
                case ActionKind.Heal:
                    var healed = Math.Min(4,t.MaxHp-t.Hp); t.Hp += healed; s.HealCharges--;
                    emit(EventKind.Heal,u.Id,t.Id,healed,t.Hp,t.Position,"Heal charge spent"); break;
                case ActionKind.Attack:
                    var damage = u.Attack + (Flank(s,u,t) ? 2 : 0);
                    var protector = s.Find("B");
                    var guarded = t.Id == "A" && s.Guard && protector != null && protector.Alive && Distance(protector.Position,t.Position) == 1;
                    if (guarded) damage = Math.Max(0,damage-3);
                    Hit(s,events,u,t,damage,EventKind.Damage,guarded ? "Guard reduced damage by 3" : (Flank(s,u,t) ? "Flank +2" : "Attack"));
                    if (t.Alive && t.Role == Role.Anchor && u.Alive && damage > 0)
                        Hit(s,events,t,u,2,EventKind.Counter,"Living A counters a surviving adjacent hit");
                    break;
            }
            s.Cursor = (s.Initiative.ToList().IndexOf(u.Id)+1) % s.Initiative.Count;
            if (!s.Units.Any(x=>x.Enemy && x.Alive)) s.Outcome = Outcome.Victory;
            else if (!s.Units.Any(x=>!x.Enemy && x.Alive)) s.Outcome = Outcome.Defeat;
            else if (s.Step >= ActionLimit) s.Outcome = Outcome.Limit;
            if (s.Outcome != Outcome.Active) emit(EventKind.End,"","",0,0,new GridPosition(0,0),s.Outcome.ToString());
            return new ResolvedStep(s,plan,events);
        }
        private static void Hit(EncounterState s, List<BattleEvent> events, Unit actor, Unit target, int amount, EventKind kind, string detail)
        {
            var actual = Math.Min(target.Hp, amount); target.Hp -= actual;
            events.Add(new BattleEvent(s.Step,kind,actor.Id,target.Id,actual,target.Hp,target.Position,detail));
            if (!target.Alive) events.Add(new BattleEvent(s.Step,EventKind.Death,actor.Id,target.Id,0,0,target.Position,"Lethal resolution"));
        }
    }

    public sealed class Replay
    {
        public EncounterState State { get; private set; }
        public Preference Preference { get; private set; }
        public Preference InitialPreference { get; }
        public int CorrectionsLeft { get; private set; } = 1;
        public string PendingTargetActor { get; private set; }
        public string PendingTarget { get; private set; }
        private readonly List<BattleEvent> events = new List<BattleEvent>();
        private readonly List<string> commands = new List<string>();
        private readonly string baseline;
        public IReadOnlyList<BattleEvent> Events => events.AsReadOnly();
        public Replay(EncounterState initial, Preference preference)
        { State = initial.Copy(); baseline = initial.Serialize(); InitialPreference = Preference = new Command(preference).Preference; }
        // The encounter's single correction: B's preference for every following step. Explicit, recorded, never automatic.
        public void Correct(Preference preference)
        {
            if (CorrectionsLeft < 1) throw new InvalidOperationException("Correction already used in this encounter");
            if (State.Outcome != Outcome.Active) throw new InvalidOperationException("Encounter already resolved");
            if (preference == Preference) throw new ArgumentException("Correction must change the preference");
            Preference = new Command(preference).Preference; CorrectionsLeft--;
        }
        public void TargetOnce(string actorId, string targetId)
        {
            if (CorrectionsLeft < 1) throw new InvalidOperationException("Correction already used in this encounter");
            if (State.Outcome != Outcome.Active) throw new InvalidOperationException("Encounter already resolved");
            var actor = State.Find(actorId); var target = State.Find(targetId);
            if (actor == null || !actor.Alive || actor.Enemy) throw new ArgumentException("Actor must be a living ally", nameof(actorId));
            if (target == null || !target.Alive || !target.Enemy) throw new ArgumentException("Target must be a living enemy", nameof(targetId));
            PendingTargetActor = actorId; PendingTarget = targetId; CorrectionsLeft--;
            commands.Add("target:" + actorId + "," + targetId);
        }
        public Plan Preview()
        {
            var normal = Rules.Preview(State, new Command(Preference));
            return normal != null && normal.Actor == PendingTargetActor ?
                Rules.Preview(State, new Command(Preference), PendingTargetActor, PendingTarget) : normal;
        }
        public IReadOnlyList<Plan> Intents() => PendingTargetActor == null ? Rules.Intents(State, new Command(Preference)) :
            Rules.Intents(State, new Command(Preference), PendingTargetActor, PendingTarget);
        public ResolvedStep Advance()
        {
            var plan = Preview();
            if (!Rules.IsLegal(State, plan)) throw new InvalidOperationException("No legal action in terminal/invalid state");
            var step = Rules.Resolve(State,new Command(Preference), plan); State = step.State;
            if (PendingTargetActor == plan.Actor) { PendingTargetActor = null; PendingTarget = null; }
            else if (PendingTargetActor != null && (!State.Find(PendingTargetActor).Alive || !State.Find(PendingTarget).Alive))
            { PendingTargetActor = null; PendingTarget = null; }
            commands.Add(((int)Preference).ToString(CultureInfo.InvariantCulture)); events.AddRange(step.Events); return step;
        }
        public string Serialize()
        {
            var pending = PendingTargetActor == null ? "" : "\npending-target:" + PendingTargetActor + "," + PendingTarget;
            return baseline + "\ncommands:" + string.Join(";", commands) + pending + "\n" +
                string.Join("\n",events.Select(e=>e.Serialize())) + "\nfinal:" + State.Serialize();
        }
        public string Hash()
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Serialize()))).Replace("-","").ToLowerInvariant(); }
    }
}
