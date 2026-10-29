using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RogueTactics.Presentation
{
    [Serializable]
    public struct ReferenceEnvironmentPart
    {
        public Mesh Mesh;
        public Vector3 Position;
        public float SurfaceOffset;
    }

    // Decorative, independently positioned cutout meshes. No board/rule ownership.
    public sealed class HqReferenceParts : MonoBehaviour
    {
        private Camera facing;
        private Material material;
        private ReferenceEnvironmentPart[] source;

        public static void Build(Camera camera, Vector3 center, Material template,
            Texture2D original, ReferenceEnvironmentPart[] parts)
        {
            if(original==null || parts==null || parts.Length==0)return;
            var root=new GameObject("Private reference environment parts");
            root.transform.position=center;
            var owner=root.AddComponent<HqReferenceParts>();owner.facing=camera;
            owner.source=parts;
            owner.material=new Material(template){name="Private reference RGB sampling"};
            owner.material.mainTexture=original;owner.material.SetColor("_Color",Color.white);
            foreach(var part in parts)
            {
                if(part.Mesh==null)throw new InvalidOperationException("Missing private reference part mesh");
                var child=new GameObject(part.Mesh.name);child.transform.SetParent(root.transform,false);
                child.transform.localPosition=part.Position;child.transform.rotation=camera.transform.rotation;
                child.AddComponent<MeshFilter>().sharedMesh=part.Mesh;
                var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=owner.material;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                // These rim decorations are behind the living cast, never an overlay on faces.
                renderer.sortingOrder=-10;
            }
        }

        private void LateUpdate()
        {
            if(facing==null)return;
            for(var i=0;i<source.Length;i++)
            {
                var part=transform.GetChild(i);part.rotation=facing.transform.rotation;
                // A facade lies in front of its opaque support while retaining
                // its projected attachment point. It still depth-tests normally.
                part.localPosition=source[i].Position-facing.transform.forward*source[i].SurfaceOffset;
            }
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
