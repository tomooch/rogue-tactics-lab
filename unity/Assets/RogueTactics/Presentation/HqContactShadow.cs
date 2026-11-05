using UnityEngine;
namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private Material contactShadow;
        private Texture2D contactTexture;
        private GameObject MakeContactShadow(Transform root,bool enemy)
        {
            if(contactShadow==null)
            {
                contactTexture=new Texture2D(64,64,TextureFormat.RGBA32,true){name="Local soft contact",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[4096];for(var y=0;y<64;y++)for(var x=0;x<64;x++){var p=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f);var fade=Mathf.Clamp01(1-p.sqrMagnitude);pixels[y*64+x]=new Color(.025f,.055f,.07f,fade*fade*.48f);}contactTexture.SetPixels(pixels);contactTexture.Apply(true,true);
                contactShadow=new Material(SpriteTemplate){name="Soft actor contact"};contactShadow.mainTexture=contactTexture;materials.Add(contactShadow);
            }
            var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Soft foot contact";Destroy(shadow.GetComponent<Collider>());shadow.transform.SetParent(root,false);shadow.transform.localPosition=new Vector3(0,.04f,enemy?.55f:0);shadow.transform.localRotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(1.25f,.85f,1);var r=shadow.GetComponent<Renderer>();r.sharedMaterial=contactShadow;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return shadow;
        }
    }
}
