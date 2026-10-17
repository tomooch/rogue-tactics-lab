using System.Collections.Generic;
using UnityEngine;
namespace RogueTactics.Presentation
{
    // Two locally authored garden spirits. Visuals only; their identity and HP stay X/Y in Core.
    public static class HqCreature
    {
        public static Renderer Build(Transform parent,Material template,bool violet)
        {
            var owner=parent.gameObject.AddComponent<AssetsLifetime>();
            Material Mat(Color c){var m=new Material(template);m.color=c;m.SetFloat("_Smoothness",.24f);owner.assets.Add(m);return m;}
            var skin=Mat(violet?new Color(.61f,.43f,.70f):new Color(.55f,.76f,.29f));
            var leaf=Mat(violet?new Color(.34f,.48f,.38f):new Color(.26f,.49f,.20f));var cream=Mat(new Color(1,.94f,.77f));var dark=Mat(new Color(.08f,.12f,.13f));var blush=Mat(new Color(.96f,.58f,.59f));
            GameObject Shape(string name,Vector3 at,Vector3 size,Material material){var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;Object.Destroy(obj.GetComponent<Collider>());return obj;}
            var mesh=new Mesh{name="Authored scalloped spirit body"};owner.assets.Add(mesh);var verts=new List<Vector3>();var tris=new List<int>();
            const int rings=13,sides=24;
            for(var j=0;j<=rings;j++)for(var i=0;i<=sides;i++)
            {
                var a=i*2*Mathf.PI/sides;var t=j*Mathf.PI/rings;var scallop=1+.055f*Mathf.Cos(a*5)*Mathf.Sin(t);var radius=Mathf.Sin(t)*scallop;
                verts.Add(new Vector3(Mathf.Cos(a)*radius*.68f,Mathf.Max(.11f,.46f+Mathf.Cos(t)*.40f),.55f+Mathf.Sin(a)*radius*.56f));
                if(j<rings && i<sides){var n=j*(sides+1)+i;tris.AddRange(new[]{n,n+sides+1,n+1,n+1,n+sides+1,n+sides+2});}
            }
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var body=new GameObject("Fixture slime",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(parent,false);body.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=body.GetComponent<Renderer>();renderer.sharedMaterial=skin;
            foreach(var dx in new[]{-.22f,.22f})
            {
                Shape("Bright spirit eye",new Vector3(dx,.59f,.045f),new Vector3(.11f,.16f,.055f),dark);
                Shape("Eye catchlight",new Vector3(dx-.018f,.63f,.016f),new Vector3(.035f,.045f,.025f),cream);
                Shape("Rosy cheek",new Vector3(dx*1.6f,.43f,.04f),new Vector3(.15f,.07f,.035f),blush);
            }
            Shape("Smile",new Vector3(0,.42f,-.012f),new Vector3(.065f,.035f,.025f),dark);
            for(var i=0;i<3;i++)
            {
                var l=Shape("Crown leaf",new Vector3((i-1)*.22f,.87f,.55f),new Vector3(.20f,.09f,.43f),leaf);l.transform.localRotation=Quaternion.Euler(-18,(i-1)*48,0);
            }
            for(var i=0;i<5;i++)
            {var a=i*Mathf.PI*2/5;Shape("Little crown petal",new Vector3(.30f+Mathf.Cos(a)*.09f,.88f,.45f+Mathf.Sin(a)*.09f),new Vector3(.14f,.055f,.14f),cream);}
            Shape("Pollen",new Vector3(.30f,.92f,.45f),new Vector3(.07f,.06f,.07f),blush);
            foreach(Transform child in parent)if(child.name!="Soft foot contact" && child.name!="Contact shadow" && !child.name.StartsWith("Known ")){child.localPosition*=1.3f;child.localScale*=1.3f;}
            return renderer;
        }
        private sealed class AssetsLifetime:MonoBehaviour
        {public readonly List<Object> assets=new List<Object>();private void OnDestroy(){foreach(var asset in assets)if(asset!=null)Object.Destroy(asset);}}
    }
}
