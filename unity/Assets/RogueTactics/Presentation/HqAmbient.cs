using System.Collections.Generic;
using UnityEngine;
namespace RogueTactics.Presentation
{
    // Decorative motion never reads or writes Core. Fixed geometry/material allocations stay out of Update.
    public sealed class HqAmbient:MonoBehaviour
    {
        private Transform water,cloth,foliage;
        public bool Reduced {get;private set;}
        private float phase;
        private readonly Dictionary<MeshRenderer,UnityEngine.Rendering.ShadowCastingMode> originalShadows=new Dictionary<MeshRenderer,UnityEngine.Rendering.ShadowCastingMode>();
        private void Start()
        {
            water=transform.Find("Water");cloth=transform.Find("Blue");foliage=transform.Find("Foliage");
            phase=0;
        }
        public void SetReduced(bool reduced)
        {
            Reduced=reduced;
            if(water!=null)water.localPosition=Vector3.zero;
            if(cloth!=null)cloth.localRotation=Quaternion.identity;
            if(foliage!=null)foliage.localRotation=Quaternion.identity;
            foreach(var r in GetComponentsInChildren<MeshRenderer>())
            {
                if(!originalShadows.TryGetValue(r,out var original)){original=r.shadowCastingMode;originalShadows.Add(r,original);}
                r.shadowCastingMode=reduced?UnityEngine.Rendering.ShadowCastingMode.Off:original;
                if(r.sharedMaterial.HasProperty("_FlowSpeed"))r.sharedMaterial.SetFloat("_FlowSpeed",reduced?0:1);
            }
        }
        private void Update()
        {
            if(Reduced)return;phase+=Time.unscaledDeltaTime;
            if(water!=null)water.localPosition=new Vector3(0,Mathf.Sin(phase*1.6f)*.035f,Mathf.Sin(phase*1.2f)*.012f);
            if(cloth!=null)cloth.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase*1.1f)*.25f);
            if(foliage!=null)foliage.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase*.7f)*.16f);
        }
    }
}
