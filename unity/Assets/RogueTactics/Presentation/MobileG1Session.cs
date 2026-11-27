using System;
using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core;
using RogueTactics.Core.G1;
using RogueTactics.Core.Exploration;

namespace RogueTactics.Presentation
{
    // Presentation session over the unchanged G1 Core. No new combat rules.
    public sealed class MobileG1Session
    {
        public Replay Replay { get; private set; }
        public Preference Preference { get; private set; }
        public bool Started { get; private set; }
        public string Prediction { get; private set; } = "";
        public int EncounterIndex { get; private set; }
        public int EncounterCount { get; }
        public int FloorNumber { get; }
        private static readonly string[] SecondFloorRooms={"R01","R04","R09"};
        private EncounterState entryState;
        private IReadOnlyDictionary<string,int> entryHealth;
        private BattleInput correction;
        public Dictionary<Preference, MobileTrial> Trials { get; } = new Dictionary<Preference, MobileTrial>();
        public MobileG1Session(int floorNumber=1)
        {
            if(floorNumber!=1 && floorNumber!=2)throw new ArgumentOutOfRangeException(nameof(floorNumber));
            FloorNumber=floorNumber;
            EncounterCount=floorNumber==1?Fixture.EncounterCount:SecondFloorRooms.Length;
            entryState=Room(0);
            Reset();
        }

        public MobileG1Session(EncounterState encounter)
        {
            FloorNumber=1;
            EncounterCount=1;
            entryState=encounter ?? throw new ArgumentNullException(nameof(encounter));
            Reset();
        }

        public void Select(Preference preference)
        { if(Started) throw new InvalidOperationException("Reset before changing preference"); Preference=preference; Reset(); }
        public void Reset()
        {
            Replay=new Replay(entryState,Preference);
            Started=false;
            Prediction="";
            correction=null;
        }
        private EncounterState Room(int index)
            => FloorNumber==1?Fixture.Encounter(index):FloorTwoRooms.Build(SecondFloorRooms[index]);

        public void LoadEncounter(int index) => LoadEncounter(index,null);
        public void LoadEncounter(int index,IReadOnlyDictionary<string,int> partyHp)
        {
            if(index<0 || index>=EncounterCount) throw new ArgumentOutOfRangeException(nameof(index));
            var room=EncounterCount==1?entryState:Room(index);
            var next=partyHp==null?room:EncounterBuilder.Build(room,partyHp);
            entryHealth=partyHp==null?null:next.Units.Where(unit=>!unit.Enemy).ToDictionary(unit=>unit.Id,unit=>unit.Hp);
            entryState=next;
            EncounterIndex=index;
            Trials.Clear();
            Reset();
        }
        // The encounter's one correction changes B's preference for the remaining steps.
        public void Correct(Preference preference)
        {
            if(!Started)throw new InvalidOperationException("Start first");
            Replay.Correct(preference);
            Preference=preference;
            correction=new BattleInput(Replay.State.Step,preference);
        }

        public void TargetOnce(string actor,string target)
        {
            if(!Started)throw new InvalidOperationException("Start first");
            Replay.TargetOnce(actor,target);
            correction=new BattleInput(Replay.State.Step,actor,target);
        }

        public BattleCheckpoint CaptureBattle(FloorCheckpoint floor=null)
        {
            if(!Started && FloorNumber==1)throw new InvalidOperationException("Start first");
            return new BattleCheckpoint(EncounterIndex,Replay.InitialPreference,Replay.State.Step,Prediction,correction,floor,entryHealth,FloorNumber,Started);
        }

        public void RestoreBattle(BattleCheckpoint checkpoint)
        {
            if(checkpoint==null)throw new ArgumentNullException(nameof(checkpoint));
            if(checkpoint.FloorNumber!=FloorNumber || checkpoint.EncounterIndex!=EncounterIndex)throw new ArgumentException("Load the saved encounter before restoring its battle",nameof(checkpoint));

            var restored=new Replay(entryState,checkpoint.InitialPreference);
            for(var step=0;step<=checkpoint.Steps;step++)
            {
                if(checkpoint.Input?.At==step)checkpoint.Input.Apply(restored);
                if(step<checkpoint.Steps)restored.Advance();
            }

            Replay=restored;
            Preference=restored.Preference;
            Prediction=checkpoint.Prediction;
            correction=checkpoint.Input;
            Started=checkpoint.Started;
            if(restored.State.Outcome!=Outcome.Active)
                Trials[restored.InitialPreference]=new MobileTrial(restored,Prediction);
        }

        public void Start(string prediction)
        { if(Started) throw new InvalidOperationException("Already started"); Prediction=prediction??""; Started=true; }
        public ResolvedStep Advance()
        {
            if(!Started) throw new InvalidOperationException("Start first");
            var step=Replay.Advance();
            if(step.State.Outcome!=Outcome.Active) Trials[Replay.InitialPreference]=new MobileTrial(Replay,Prediction);
            return step;
        }
    }
    public sealed class BattleInput
    {
        public int At { get; }
        public Preference? Policy { get; }
        public string Actor { get; }
        public string Target { get; }

        public BattleInput(int at,Preference policy)
        {
            At=at;
            Policy=policy;
        }

        public BattleInput(int at,string actor,string target)
        {
            At=at;
            Actor=actor;
            Target=target;
        }

        internal void Apply(Replay replay)
        {
            if(Policy.HasValue)replay.Correct(Policy.Value);
            else replay.TargetOnce(Actor,Target);
        }
    }

    public sealed class BattleCheckpoint
    {
        public int EncounterIndex { get; }
        public Preference InitialPreference { get; }
        public int Steps { get; }
        public string Prediction { get; }
        public BattleInput Input { get; }
        public FloorCheckpoint Floor { get; }
        public IReadOnlyDictionary<string,int> EntryHealth { get; }
        public int FloorNumber { get; }
        public bool Started { get; }

        public BattleCheckpoint(int encounterIndex,Preference initialPreference,int steps,string prediction,BattleInput input,FloorCheckpoint floor=null,IReadOnlyDictionary<string,int> entryHealth=null,int floorNumber=1,bool started=true)
        {
            if(steps<0 || steps>Rules.ActionLimit)throw new ArgumentOutOfRangeException(nameof(steps));
            if(input!=null && (input.At<0 || input.At>steps))throw new ArgumentException("The correction must be within the saved battle",nameof(input));
            if(floorNumber!=1 && floorNumber!=2)throw new ArgumentOutOfRangeException(nameof(floorNumber));
            if(!started && (floorNumber==1 || steps!=0 || input!=null || floor!=null))
                throw new ArgumentException("Invalid battle preparation checkpoint");
            FloorNumber=floorNumber;
            Started=started;
            EncounterIndex=encounterIndex;
            InitialPreference=initialPreference;
            Steps=steps;
            Prediction=prediction??"";
            Input=input;
            Floor=floor;
            EntryHealth=entryHealth==null?null:new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(entryHealth.ToDictionary(entry=>entry.Key,entry=>entry.Value));
        }
    }

    public sealed class MobileTrial
    {
        public string Prediction { get; }
        public string Hash { get; }
        public Outcome Outcome { get; }
        public int Actions { get; }
        public int Heals { get; }
        public int Alive { get; }
        public string Hp { get; }
        public MobileTrial(Replay replay,string prediction)
        {
            Prediction=prediction; Hash=replay.Hash(); Outcome=replay.State.Outcome; Actions=replay.State.Step;
            Heals=replay.State.HealCharges; Alive=replay.State.Units.Count(u=>!u.Enemy && u.Alive);
            Hp=string.Join("  ",replay.State.Units.Where(u=>!u.Enemy).Select(u=>u.Id+":"+u.Hp));
        }
    }
}
