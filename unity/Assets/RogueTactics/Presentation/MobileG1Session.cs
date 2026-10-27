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
        public Dictionary<Preference, MobileTrial> Trials { get; } = new Dictionary<Preference, MobileTrial>();
        public MobileG1Session() => Reset();
        public void Select(Preference preference)
        { if(Started) throw new InvalidOperationException("Reset before changing preference"); Preference=preference; Reset(); }
        public void Reset() { Replay=new Replay(Fixture.Encounter(EncounterIndex),Preference); Started=false; Prediction=""; }
        public void LoadEncounter(int index)
        {
            if(index<0 || index>=Fixture.EncounterCount) throw new ArgumentOutOfRangeException(nameof(index));
            EncounterIndex=index; Trials.Clear(); Reset();
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
