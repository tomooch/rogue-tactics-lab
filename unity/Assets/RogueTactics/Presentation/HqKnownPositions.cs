using UnityEngine;
namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private Texture2D knownPositionTexture;
        private Material friendlyPosition,hostilePosition;
        private void MakeKnownPositionMarker(Transform root,bool enemy)
        {
            if(knownPositionTexture==null)
            {
                knownPositionTexture=new Texture2D(64,64,TextureFormat.RGBA32,true){name="Known occupied position",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[4096];for(var y=0;y<64;y++)for(var x=0;x<64;x++){var a=Mathf.Abs((x-31.5f)/31.5f);var b=Mathf.Abs((y-31.5f)/31.5f);var edge=Mathf.Max(a,b);var alpha=edge>.97f || a+b>1.78f?0:edge>.86f?.62f:.10f;pixels[y*64+x]=new Color(1,1,1,alpha);}knownPositionTexture.SetPixels(pixels);knownPositionTexture.Apply(true,true);
                friendlyPosition=new Material(SpriteTemplate){name="Known friendly position"};hostilePosition=new Material(SpriteTemplate){name="Known enemy position"};
                friendlyPosition.mainTexture=hostilePosition.mainTexture=knownPositionTexture;friendlyPosition.color=new Color(.22f,.72f,1);hostilePosition.color=new Color(1,.36f,.32f);materials.Add(friendlyPosition);materials.Add(hostilePosition);
            }
            var marker=GameObject.CreatePrimitive(PrimitiveType.Quad);marker.name=enemy?"Known hostile cell":"Known friendly cell";Destroy(marker.GetComponent<Collider>());marker.transform.SetParent(root,false);marker.transform.localPosition=new Vector3(0,.025f,0);marker.transform.localRotation=Quaternion.Euler(90,0,0);marker.transform.localScale=new Vector3(1.9f,3.8f,1);var r=marker.GetComponent<Renderer>();r.sharedMaterial=enemy?hostilePosition:friendlyPosition;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
    }
}
