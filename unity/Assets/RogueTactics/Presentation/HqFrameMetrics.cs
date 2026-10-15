using System;
using System.Collections.Generic;
using UnityEngine;
namespace RogueTactics.Presentation
{
    [Serializable]
    public sealed class HqFrameReport
    {
        public string runtime, unity, graphics;
        public int samples;
        public float p50_ms,p95_ms;
        public bool reduced_effects;
        public string limitation="Local frame intervals; includes capture/runtime overhead; not proof of physical iPhone performance";
    }
    public sealed partial class MobileG1View
    {
        private readonly List<float> frameIntervals=new List<float>(4096);
        public HqFrameReport LastFrameReport {get;private set;}
        private void SampleFrame()
        {if(Session.Started && !terminalSeen && Busy && !Paused && frameIntervals.Count<4096)frameIntervals.Add(Time.unscaledDeltaTime*1000);}
        private void FinishFrameReport()
        {
            if(frameIntervals.Count==0)return;
            var sorted=frameIntervals.ToArray();Array.Sort(sorted);
            LastFrameReport=new HqFrameReport{runtime=Application.isEditor?"Unity Editor":"Unity native player",unity=Application.unityVersion,graphics=SystemInfo.graphicsDeviceType.ToString(),samples=sorted.Length,p50_ms=sorted[(sorted.Length-1)/2],p95_ms=sorted[Mathf.FloorToInt((sorted.Length-1)*.95f)],reduced_effects=reducedEffects};
            Debug.Log("HQ_FRAME_METRICS "+JsonUtility.ToJson(LastFrameReport));
        }
    }
}
