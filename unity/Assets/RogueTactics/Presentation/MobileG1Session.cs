using System;
using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core;
using RogueTactics.Core.G1;

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
        private EncounterState entryState;
        public Dictionary<Preference, MobileTrial> Trials { get; } = new Dictionary<Preference, MobileTrial>();
        public MobileG1Session()
        {
            EncounterCount=Fixture.EncounterCount;
            entryState=Fixture.Encounter(0);
            Reset();
        }

        public MobileG1Session(EncounterState encounter)
        {
            EncounterCount=1;
            entryState=encounter ?? throw new ArgumentNullException(nameof(encounter));
            Reset();
        }

        public void Select(Preference preference)
        { if(Started) throw new InvalidOperationException("Reset before changing preference"); Preference=preference; Reset(); }
        public void Reset() { Replay=new Replay(entryState,Preference); Started=false; Prediction=""; }
        public void LoadEncounter(int index) => LoadEncounter(index,null);
        public void LoadEncounter(int index,IReadOnlyDictionary<string,int> partyHp)
        {
            if(index<0 || index>=EncounterCount) throw new ArgumentOutOfRangeException(nameof(index));
            var room=EncounterCount==1?entryState:Fixture.Encounter(index);
            var next=partyHp==null?room:EncounterBuilder.Build(room,partyHp);
            entryState=next;
            EncounterIndex=index;
            Trials.Clear();
            Reset();
        }
        // The encounter's one correction changes B's preference for the remaining steps.
        public void Correct(Preference preference)
        { if(!Started) throw new InvalidOperationException("Start first"); Replay.Correct(preference); Preference=preference; }
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
