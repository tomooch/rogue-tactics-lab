using System;
using System.IO;
using RogueTactics.Core.G1;
using UnityEngine;

namespace RogueTactics.Presentation
{
    public enum BattleCheckpointReadStatus { Loaded, Missing, Rejected, Unavailable }

    public sealed class BattleCheckpointStore
    {
        private const int FormatVersion = 1;
        private const int RulesVersion = 1;
        private readonly string path;

        public BattleCheckpointStore(string path)
        {
            this.path = Path.GetFullPath(path);
        }

        public bool TryWrite(BattleCheckpoint checkpoint)
        {
            if(checkpoint==null)throw new ArgumentNullException(nameof(checkpoint));
            var record=new SavedBattle
            {
                format=FormatVersion,
                rules=RulesVersion,
                room=checkpoint.EncounterIndex,
                policy=(int)checkpoint.InitialPreference,
                steps=checkpoint.Steps,
                note=checkpoint.Prediction,
                inputs=checkpoint.Input==null?Array.Empty<SavedInput>():new[]{new SavedInput(checkpoint.Input)}
            };
            var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary,JsonUtility.ToJson(record));
                if(File.Exists(path))File.Replace(temporary,path,null);
                else File.Move(temporary,path);
                return true;
            }
            catch(Exception error) when(IsFileAccessError(error))
            {
                return false;
            }
            finally
            {
                try { File.Delete(temporary); }
                catch(Exception error) when(IsFileAccessError(error)) { }
            }
        }

        public BattleCheckpointReadStatus Read(out BattleCheckpoint checkpoint)
        {
            checkpoint=null;
            string json;
            try { json=File.ReadAllText(path); }
            catch(FileNotFoundException) { return BattleCheckpointReadStatus.Missing; }
            catch(DirectoryNotFoundException) { return BattleCheckpointReadStatus.Missing; }
            catch(Exception error) when(IsFileAccessError(error)) { return BattleCheckpointReadStatus.Unavailable; }

            try
            {
                var record=new SavedBattle();
                JsonUtility.FromJsonOverwrite(json,record);
                if(record.format!=FormatVersion || record.rules!=RulesVersion || record.room<0
                    || !Enum.IsDefined(typeof(Preference),record.policy) || record.inputs==null || record.inputs.Length>1)
                    throw new ArgumentException("Invalid or unsupported saved battle");
                var input=record.inputs.Length==0?null:record.inputs[0]?.ToBattleInput()
                    ?? throw new ArgumentException("Missing saved battle input");
                checkpoint=new BattleCheckpoint(record.room,(Preference)record.policy,record.steps,record.note,input);
                return BattleCheckpointReadStatus.Loaded;
            }
            catch(ArgumentException)
            {
                try { File.Move(path,path+".rejected."+Guid.NewGuid().ToString("N")); }
                catch(Exception error) when(IsFileAccessError(error)) { return BattleCheckpointReadStatus.Unavailable; }
                return BattleCheckpointReadStatus.Rejected;
            }
        }

        private static bool IsFileAccessError(Exception error)
            => error is IOException || error is UnauthorizedAccessException || error is NotSupportedException;

        [Serializable]
        private sealed class SavedBattle
        {
            public int format;
            public int rules;
            public int room=-1;
            public int policy=-1;
            public int steps=-1;
            public string note;
            public SavedInput[] inputs;
        }

        [Serializable]
        private sealed class SavedInput
        {
            public int at;
            public string kind;
            public int policy;
            public string actor;
            public string target;

            public SavedInput(BattleInput input)
            {
                at=input.At;
                kind=input.Policy.HasValue?"correct":"target";
                policy=input.Policy.HasValue?(int)input.Policy.Value:-1;
                actor=input.Actor;
                target=input.Target;
            }

            public BattleInput ToBattleInput()
            {
                if(kind=="correct" && Enum.IsDefined(typeof(Preference),policy))
                    return new BattleInput(at,(Preference)policy);
                if(kind=="target" && !string.IsNullOrEmpty(actor) && !string.IsNullOrEmpty(target))
                    return new BattleInput(at,actor,target);
                throw new ArgumentException("Invalid saved battle input");
            }
        }
    }
}
