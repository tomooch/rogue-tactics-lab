using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RogueTactics.Presentation
{
    // Original offline-authored geometry is imported as project assets. Runtime only composes it.
    public static class HqSkyRuins
    {
        public static GameObject Build(Material template,Vector3 center,Mesh[] meshes)
        {
            if(template==null || meshes==null || meshes.Length==0)throw new InvalidOperationException("HQ authored environment assets are required");
            var root=new GameObject("HQ Sky Ruins Environment");root.transform.position=center;
            var lifetime=root.AddComponent<OwnedMaterials>();
            foreach(var mesh in meshes)
            {
                var material=new Material(template){name="Authored ruins "+mesh.name};
                material.SetColor("_BaseColor",Color.white);
                material.SetFloat("_Leaves",mesh.name.Contains("Foliage") || mesh.name.Contains("Flowers")?1:0);
                material.SetFloat("_Emission",mesh.name.Contains("Warm")?1.2f:0);
                if(mesh.name.Contains("Blue") || mesh.name.Contains("Water") || mesh.name.Contains("Foliage") || mesh.name.Contains("Flowers"))material.SetFloat("_Cull",0);
                if(mesh.name.Contains("Warm")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(1.4f,.65f,.18f));}
                lifetime.Materials.Add(material);
                var child=new GameObject(mesh.name);child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=mesh.name.Contains("Distant")?ShadowCastingMode.Off:ShadowCastingMode.On;
            }
            return root;
        }
        private sealed class OwnedMaterials:MonoBehaviour
        {
            public readonly System.Collections.Generic.List<Material> Materials=new System.Collections.Generic.List<Material>();
            private void OnDestroy(){foreach(var material in Materials)Destroy(material);}
        }
    }
}
