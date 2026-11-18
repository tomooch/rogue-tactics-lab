using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using RogueTactics.Presentation.Gfx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using Object=UnityEngine.Object;

namespace RogueTactics.Tests.PlayMode
{
    // The five floor looks (specs/07 §8), each applied over the MobileG1 battle board. Checks that a look never moves a
    // measured object, never recolours or covers a figure, keeps the paving in floor 1's brightness band, and keeps
    // every face as separable from the paving as on the approved floor 1.
    // RT_GFX_CAPTURE=<n> also saves one full-screen frame per floor (and the field render alone) under
    // Logs/hq/private/floorlooks. Captures include approved art: they stay on this machine.
    public class GfxFloorLookGalleryTests
    {
        private static bool Capturing=>int.TryParse(Environment.GetEnvironmentVariable("RT_GFX_CAPTURE"),out var n) && n>0;
        private static string Gallery=>Path.GetFullPath("Logs/hq/private/floorlooks");
        private const float Cell=2.5f;
        // Frames rendered after a scene load before measuring, at a fixed capture rate so each floor is read at the
        // same moment of the idle animation.
        private const int Settle=24;

        [Test]
        public void EachFloorHasItsOwnPaletteAndFloorOneIsTheApprovedRuins()
        {
            var all=HqFloorLook.All;
            Assert.That(all.Count,Is.EqualTo(HqFloorLook.Floors));
            Assert.That(all.Select(l=>l.Floor),Is.EqualTo(Enumerable.Range(1,HqFloorLook.Floors)));
            Assert.That(all.Select(l=>l.Name).Distinct().Count(),Is.EqualTo(all.Count),"every floor has its own name");
            Assert.That(all.All(l=>!string.IsNullOrEmpty(l.Mood) && !string.IsNullOrEmpty(l.Enemies)),Is.True);
            // Floor 1 holds the shaders' own literals (the approved world), and has no props.
            var one=HqFloorLook.For(1);
            Assert.That(one.Props,Is.Null);
            Assert.That(one.SkyLow,Is.EqualTo(new Vector4(.80f,.90f,.97f,0)));Assert.That(one.SkyHigh,Is.EqualTo(new Vector4(.27f,.55f,.86f,0)));
            Assert.That(one.Stone,Is.EqualTo(new Vector4(.96f,.89f,.76f,0)));Assert.That(one.LightWarm,Is.EqualTo(new Vector4(1,.98f,.91f,0)));
            Assert.That(all.Skip(1).All(l=>l.Props!=null),Is.True,"floors 2-5 add their own props");
            Assert.Throws<ArgumentOutOfRangeException>(()=>HqFloorLook.For(0));Assert.Throws<ArgumentOutOfRangeException>(()=>HqFloorLook.For(6));
            for(var i=0;i<all.Count;i++)for(var j=i+1;j<all.Count;j++)
            {
                // Told apart at a glance by the sky: horizon and zenith together differ by more than a quarter (RGB distance).
                var d=Vector4.Distance(all[i].SkyLow,all[j].SkyLow)+Vector4.Distance(all[i].SkyHigh,all[j].SkyHigh);
                Debug.Log($"FLOORLOOK sky {i+1} vs {j+1} = {d:0.000}");
                Assert.That(d,Is.GreaterThan(.25f),$"floors {i+1} and {j+1} have different skies");
            }
            foreach(var l in all)
            {
                // The paving stays a pale, near-neutral stone: never the ally blue or the enemy red of the markers.
                var s=l.Stone;var max=Mathf.Max(s.x,s.y,s.z);var min=Mathf.Min(s.x,s.y,s.z);
                Assert.That((max-min)/max,Is.LessThan(.30f),$"floor {l.Floor} paving is near neutral");
                Assert.That(Luma(s),Is.InRange(.80f,.95f),$"floor {l.Floor} paving stays pale");
            }
        }

        [UnityTest]
        public IEnumerator EachFloorKeepsTheBoardReadable()
        {
            var faces=new Dictionary<int,Dictionary<string,float>>();var faceColours=new Dictionary<int,Dictionary<string,Color>>();
            var tiles=new Dictionary<int,float>();var paving=new Dictionary<int,Color>();var failures=new List<string>();
            var screen=new RenderTexture(780,1688,24);
            try
            {
                for(var floor=1;floor<=HqFloorLook.Floors;floor++)
                {
                    Time.captureFramerate=30;
                    yield return SceneManager.LoadSceneAsync("MobileG1");
                    var view=Object.FindFirstObjectByType<MobileG1View>();
                    Camera.main.targetTexture=screen;view.SetTestSafeArea(new Rect(0,68,780,1526),780,1688);
                    for(var k=0;k<Settle;k++)yield return null;
                    var before=Measured();var materialsBefore=Materials();
                    var owner=HqFloorLook.Apply(floor,view.FieldCamera);
                    if(floor==1)
                    {
                        Assert.That(owner,Is.Null,"floor 1 is the world as built: nothing is created");
                        Assert.That(Materials(),Is.EqualTo(materialsBefore),"floor 1 keeps every original material");
                        // Pixel-identical after a round trip: render the approved floor, show the night floor, come back
                        // to floor 1, and render again, all within one frame (nothing animates in between). Two renders
                        // of the untouched world already differ in a few hundred sky pixels (thin lines on the procedural
                        // sky's waterfall streaks and edges: GPU derivative noise, before and after this change alike), so
                        // the round trip may differ from the pristine frame by no more than that noise, and by little.
                        var pristine=Render(view.FieldCamera);var again=Render(view.FieldCamera);
                        HqFloorLook.Apply(4,view.FieldCamera);var night=Render(view.FieldCamera);HqFloorLook.Apply(1,view.FieldCamera);
                        var restored=Render(view.FieldCamera);
                        var noise=Differing(pristine,again);var differ=Differing(pristine,restored);var changed=Differing(pristine,night);
                        var total=pristine.width*pristine.height;
                        Debug.Log($"FLOORLOOK floor 1 round trip: {differ} of {total} pixels differ; two untouched renders differ in {noise}; floor 4 changes {changed}");
                        Assert.That(changed,Is.GreaterThan(total/2),"the night floor really changes the frame (the check can fail)");
                        Assert.That(differ,Is.LessThanOrEqualTo(Mathf.Max(2*noise,total/2000)),"floor 1 after Apply(4) then Apply(1) matches the world as built up to render noise");
                        Assert.That(Materials(),Is.EqualTo(materialsBefore),"and every original material is back");
                        Object.Destroy(again);
                        Object.Destroy(pristine);Object.Destroy(night);Object.Destroy(restored);
                    }
                    else
                    {
                        Assert.That(owner.Floor,Is.EqualTo(floor));
                        Assert.That(owner.PropRoot.GetComponentsInChildren<Renderer>().Length,Is.GreaterThan(8),$"floor {floor} adds props");
                        var sky=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(r=>r.name=="Procedural sky haze");
                        Assert.That(sky.sharedMaterial.IsKeywordEnabled("_FLOOR_LOOK"),Is.True,"the sky takes the floor's palette");
                    }
                    yield return null;yield return null;yield return null;
                    var after=Measured();
                    Assert.That(after.Keys,Is.EquivalentTo(before.Keys),$"floor {floor}: the same measured objects exist");
                    foreach(var pair in before)if(after[pair.Key]!=pair.Value)failures.Add($"floor {floor}: {pair.Key} moved {pair.Value} -> {after[pair.Key]}");

                    var field=Read(view.FieldCamera.targetTexture);
                    faces[floor]=new Dictionary<string,float>();faceColours[floor]=new Dictionary<string,Color>();
                    foreach(var u in Fixture.Baseline().Units)
                    {
                        var c=FaceColour(view,u.Id,field);faceColours[floor][u.Id]=c;faces[floor][u.Id]=Luma(c);
                    }
                    paving[floor]=Paving(view,field);tiles[floor]=Luma(paving[floor]);
                    Debug.Log($"FLOORLOOK floor {floor} paving={tiles[floor]:0.000} faces "+string.Join(" ",faces[floor].Select(p=>$"{p.Key}={p.Value:0.000}")));
                    if(Capturing){Save(field,$"floor{floor}_field");var full=Read(screen);Save(full,$"floor{floor}");Object.Destroy(full);}
                    Object.Destroy(field);

                    if(floor==HqFloorLook.Floors)
                    {
                        // Reversible: back to floor 1 restores every original material and removes every prop; then again.
                        HqFloorLook.Apply(1,view.FieldCamera);yield return null;
                        Assert.That(Materials(),Is.EqualTo(materialsBefore),"Apply(1) restores the approved materials");
                        Assert.That(owner.PropRoot,Is.Null);
                        Assert.That(GameObject.Find("Floor "+floor+" props"),Is.Null);
                        HqFloorLook.Apply(3,view.FieldCamera);HqFloorLook.Apply(3,view.FieldCamera);yield return null;
                        Assert.That(Object.FindObjectsByType<HqFloorLook>(FindObjectsSortMode.None).Length,Is.EqualTo(1),"one look at a time");
                        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Floor 3 props"),Is.EqualTo(1));
                    }
                    Camera.main.targetTexture=null;
                }
            }
            finally{Time.captureFramerate=0;if(Camera.main!=null)Camera.main.targetTexture=null;Object.Destroy(screen);}

            foreach(var floor in tiles.Keys.Where(f=>f>1))
            {
                // The paving stays in floor 1's brightness band: the foot HP bars, the blue/red position markers and the
                // contact shadows were tuned on that stone. -25%..+10% (night floors are allowed a moonlit dimming).
                var ratio=tiles[floor]/tiles[1];
                if(ratio<.75f || ratio>1.10f)failures.Add($"floor {floor}: paving luminance {tiles[floor]:0.000} is {ratio:0.00}x floor 1");
                foreach(var id in faces[1].Keys)
                {
                    // A figure is never tinted or covered by a look: its face reads the same colour as on floor 1.
                    var a=faceColours[1][id];var b=faceColours[floor][id];
                    var shift=Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
                    if(shift>.09f)failures.Add($"floor {floor}: {id}'s face changed colour by {shift:0.000}");
                    // Separation from the paving. Luminance alone is not it: the pale wardens differ from the cream stone by
                    // about 0.01 in luminance on the approved floor 1 and still read, by hue. So the measure is the colour
                    // distance (sRGB, Euclidean) between the face and the open paving, and no floor may give any figure
                    // less than 90% of what it has on floor 1, the human-approved board (10% covers frame-to-frame noise
                    // of the idle animation, measured under 0.01).
                    var c1=Distance(faceColours[1][id],paving[1]);var cn=Distance(b,paving[floor]);
                    var l1=Mathf.Abs(faces[1][id]-tiles[1]);var ln=Mathf.Abs(faces[floor][id]-tiles[floor]);
                    Debug.Log($"FLOORLOOK contrast floor {floor} {id}: colour {cn:0.000} (floor 1 {c1:0.000}) luminance {ln:0.000} (floor 1 {l1:0.000})");
                    if(cn<.9f*c1)failures.Add($"floor {floor}: {id}'s face separates from the paving by {cn:0.000} (floor 1 {c1:0.000})");
                }
            }
            Assert.That(failures,Is.Empty);
        }

        // ---- Helpers ----------------------------------------------------------------------------------

        private static float Luma(Vector4 c)=>c.x*.299f+c.y*.587f+c.z*.114f;
        private static float Luma(Color c)=>c.r*.299f+c.g*.587f+c.b*.114f;
        private static float Distance(Color a,Color b)=>Mathf.Sqrt((a.r-b.r)*(a.r-b.r)+(a.g-b.g)*(a.g-b.g)+(a.b-b.b)*(a.b-b.b));

        // Position, rotation, scale and bounds of everything the HUD and the tests measure.
        private static Dictionary<string,string> Measured()
        {
            var result=new Dictionary<string,string>();
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
            {
                var name=t.name;
                if(!(name.StartsWith("Fixture ") || name=="Private approved static skin" || name=="Courtyard"))continue;
                var path=name;for(var p=t.parent;p!=null;p=p.parent)path=p.name+"/"+path;
                var r=t.GetComponent<Renderer>();
                var key=path;var n=2;while(result.ContainsKey(key))key=path+"#"+n++;
                result[key]=$"{t.position:F4} {t.rotation.eulerAngles:F3} {t.lossyScale:F4}"+(r!=null?$" {r.bounds.center:F4} {r.bounds.size:F4}":"");
            }
            return result;
        }
        private static Dictionary<int,int> Materials()=>Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(r=>!r.name.StartsWith("Floor ") && r.sharedMaterial!=null).ToDictionary(r=>r.GetInstanceID(),r=>r.sharedMaterial.GetInstanceID());

        private static Texture2D Read(RenderTexture rt)
        {
            var copy=RenderTexture.GetTemporary(rt.width,rt.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(rt,copy);
            var previous=RenderTexture.active;RenderTexture.active=copy;
            var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(copy);return t;
        }
        private static int Differing(Texture2D x,Texture2D y)
        {
            var a=x.GetPixels32();var b=y.GetPixels32();var n=0;
            for(var i=0;i<a.Length;i++)if(a[i].r!=b[i].r || a[i].g!=b[i].g || a[i].b!=b[i].b)n++;
            return n;
        }
        // Renders the board camera now, outside the frame loop, and reads it back.
        private static Texture2D Render(Camera camera){camera.Render();return Read(camera.targetTexture);}
        private static Vector2 Pixel(Camera camera,Texture2D frame,Vector3 world){var v=camera.WorldToViewportPoint(world);return new Vector2(v.x*frame.width,v.y*frame.height);}
        private static Color Mean(Texture2D frame,Rect area)
        {
            var sum=Color.clear;var n=0;
            for(var y=Mathf.Max(0,(int)area.yMin);y<Mathf.Min(frame.height,(int)area.yMax);y++)
                for(var x=Mathf.Max(0,(int)area.xMin);x<Mathf.Min(frame.width,(int)area.xMax);x++){sum+=frame.GetPixel(x,y);n++;}
            return n>0?sum/n:Color.clear;
        }

        // A figure's face, as the board camera draws it. Companions: the inner part of the puppet rig's head (the
        // approved art's face and hair). Spirits: the body around the eyes and mouth.
        private static Color FaceColour(MobileG1View view,string id,Texture2D frame)
        {
            var root=GameObject.Find("Fixture "+id).transform;var camera=view.FieldCamera;
            var skin=root.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Private approved static skin");
            Rect area;
            if(skin!=null)
            {
                var index=id[0]-'A';var head=HqPuppet.Authored(index).Bones[HqPuppet.Head];
                var u=head.A.x/.75f+.5f;var half=head.Radius*.6f/.75f;var v0=head.A.y-.05f;var v1=(head.A.y+head.B.y)/2;
                var mirrored=skin.sharedMaterial.HasProperty("_Mirror") && skin.sharedMaterial.GetFloat("_Mirror")>.5f;
                float U(float x)=>mirrored?1-x:x;
                var a=Pixel(camera,frame,skin.transform.TransformPoint(new Vector3(U(u-half)-.5f,v0-.5f,0)));
                var b=Pixel(camera,frame,skin.transform.TransformPoint(new Vector3(U(u+half)-.5f,v1-.5f,0)));
                area=Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
                area=Shrink(area,.6f);
            }
            else
            {
                var parts=root.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Bright spirit eye" || r.name=="Smile").ToArray();
                Assert.That(parts.Length,Is.GreaterThan(0),id+" has a face");
                var points=parts.SelectMany(r=>Corners(r.bounds)).Select(p=>Pixel(camera,frame,p)).ToArray();
                area=Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
                area=Shrink(area,1.4f);
            }
            return Mean(frame,area);
        }
        private static Rect Shrink(Rect r,float k){var c=r.center;var s=r.size*k;return new Rect(c-s/2,s);}
        private static IEnumerable<Vector3> Corners(Bounds b){for(var k=0;k<8;k++)yield return new Vector3((k&1)==0?b.min.x:b.max.x,(k&2)==0?b.min.y:b.max.y,(k&4)==0?b.min.z:b.max.z);}

        // Mean colour of open paving: the centres of board cells with no figure on them or in front of them
        // (a figure's art rises over the cell behind it).
        private static Color Paving(MobileG1View view,Texture2D frame)
        {
            var taken=new HashSet<Vector2Int>();
            foreach(var u in Fixture.Baseline().Units)
            {
                var p=GameObject.Find("Fixture "+u.Id).transform.position;var cell=new Vector2Int(Mathf.RoundToInt(p.x/Cell),Mathf.RoundToInt(p.z/Cell));
                Assert.That(new Vector2(p.x,p.z),Is.EqualTo(new Vector2(cell.x*Cell,cell.y*Cell)).Using(Vector2EqualityComparer.Instance),u.Id+" stands on a cell centre");
                taken.Add(cell);
            }
            var sum=Color.clear;var n=0;
            for(var x=0;x<5;x++)for(var y=0;y<5;y++)
            {
                var c=new Vector2Int(x,y);
                if(taken.Contains(c) || taken.Contains(c+Vector2Int.down) || taken.Contains(c+Vector2Int.down*2))continue;
                var at=Pixel(view.FieldCamera,frame,new Vector3(x*Cell,0,y*Cell));
                sum+=Mean(frame,new Rect(at.x-22,at.y-14,44,28));n++;
            }
            Assert.That(n,Is.GreaterThanOrEqualTo(4),"enough open paving to measure");
            return sum/n;
        }
        private static void Save(Texture2D t,string name){Directory.CreateDirectory(Gallery);File.WriteAllBytes(Path.Combine(Gallery,name+".jpg"),t.EncodeToJPG(92));}
    }
}
