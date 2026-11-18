using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RogueTactics.Tests.PlayMode
{
    // S1-13 (design/ROADMAP.md, design/HANDOFF.md 3.1 #1): a blow must never wash the struck figure out in white.
    // On every Damage/Counter landing this watches the struck figure's face in the dungeon camera's own render, frame by
    // frame, and counts the share of its painted face pixels that are near-white, against the same face before the battle.
    // Limits and the measured baseline: design/CRAFT_HANDBOOK.md 5.4.1.
    // Set RT_WATCHDOG_CAPTURE=1 to also save each landing's whitest face crop under Logs/hq/private/watchdog for review.
    public class GfxReadabilityWatchdogTests
    {
        // A pixel is near-white when its darkest channel is at least this (sRGB, as displayed): a white core or flash lifts
        // all three channels together, while the faces' eyes, hair and outlines stay well below it.
        private const float NearWhite=.90f;
        // Limits, from the measured baseline (design/CRAFT_HANDBOOK.md 5.4.1). "Washed" is always the near-white share of
        // the face above that face's own share before the battle.
        // Every struck figure: washed over Washed (most of the face has lost its features) for no longer than the
        // handbook lets a white core live (0.15 s), plus one step of the effects' clock (.05 s) for frame granularity.
        private const float Washed=.60f,WashedSeconds=.20f;
        // A struck companion (the approved art): at its whitest, never more than CompanionPeak of the face, and over
        // the battle no more than CompanionMeanPeak on average. Spirit faces are not held to this: their bodies are pale
        // (darkest channel ~.85) and the real effects already whiten their eyes and mouth almost entirely for a few frames.
        // Tightened 2026-11-18 after the Human asked for a softer ram on companions (measured max .50, mean ~.32).
        private const float CompanionPeak=.60f,CompanionMeanPeak=.40f;
        // Spirits (S1-2): blows on them land on the lower body with a softer bloom and flash. Per blow they still vary
        // (a lunge's swoosh crosses some), so only their average is held: measured ~.47, ~.85 before the change.
        private const float SpiritMeanPeak=.60f;
        // How long after the landing is watched: the shown event holds .40 s (HqMotion.PresentEvent), then the next event
        // starts and its own light (a counter's charge on the struck figure, say) is not this blow's.
        private const float Window=.40f;

        private sealed class Landing
        {
            public BattleEvent Event;public bool BySpirit,OnSpirit;public float Age,Reference;public int Pending;
            // Per watched frame: how long it showed (the effects' own clock) and how washed the face was.
            public readonly List<(float Seconds,float Washed,int Samples)> Frames=new List<(float,float,int)>();
            public byte[] Worst;public int WorstWidth,WorstHeight;
            public float Peak=>Frames.Count==0?0:Frames.Max(f=>f.Washed);
            public float Over=>Frames.Where(f=>f.Washed>Washed).Sum(f=>f.Seconds);
        }

        [UnityTest]
        public IEnumerator ImpactsNeverWashTheStruckFigureOutInWhite()
        {
            var landings=new List<Landing>();
            // Encounter 1 (blunt from Momo and every spirit, Garu's blade, Shiro's magic) and encounter 3, where Myaru's
            // arrows land. The second is loaded on the first room, as RT_GFX_ENCOUNTER does: it is not a flow check.
            foreach(var encounter in new[]{0,2})yield return Watch(encounter,landings);

            var capture=System.Environment.GetEnvironmentVariable("RT_WATCHDOG_CAPTURE")=="1";var directory=Path.GetFullPath("Logs/hq/private/watchdog");
            if(capture){if(Directory.Exists(directory))Directory.Delete(directory,true);Directory.CreateDirectory(directory);}
            var i=0;
            foreach(var l in landings)
            {
                var trace=string.Join(" ",l.Frames.Select(f=>f.Washed.ToString("0.00")));
                Debug.Log($"WATCHDOG {l.Event.Kind} {l.Event.Actor}->{l.Event.Target} {l.Event.Detail} | reference {l.Reference:0.000} peak {l.Peak:0.000} over {l.Over:0.000}s frames {l.Frames.Count} ({l.Frames.Sum(f=>f.Seconds):0.000}s) | over .4/.5/.6/.7 {string.Join("/",new[]{.4f,.5f,.6f,.7f}.Select(t=>l.Frames.Where(f=>f.Washed>t).Sum(f=>f.Seconds).ToString("0.000")))} | {trace}");
                if(capture && l.Worst!=null)
                {
                    var image=new Texture2D(l.WorstWidth,l.WorstHeight,TextureFormat.RGBA32,false);image.LoadRawTextureData(l.Worst);image.Apply();
                    File.WriteAllBytes(Path.Combine(directory,$"{i++:00}_{l.Event.Kind}_{l.Event.Actor}{l.Event.Target}_peak{Mathf.RoundToInt(l.Peak*100):00}.png"),image.EncodeToPNG());Object.Destroy(image);
                }
            }
            var companions=landings.Where(l=>!l.OnSpirit).ToList();
            var meanPeak=companions.Count>0?companions.Average(l=>l.Peak):0;
            Debug.Log($"WATCHDOG summary: {landings.Count} landings, {companions.Count} on companions, companion peak mean {meanPeak:0.000} max {(companions.Count>0?companions.Max(l=>l.Peak):0):0.000}; longest washed {(landings.Count>0?landings.Max(l=>l.Over):0):0.000}s");
            Assert.That(landings.Count,Is.GreaterThanOrEqualTo(8),"Enough blows land to judge");
            Assert.That(companions.Count,Is.GreaterThanOrEqualTo(4),"Enough blows land on companions to judge");
            foreach(var striker in new[]{"A","B","C","D"})Assert.That(landings.Any(l=>l.Event.Actor==striker),Is.True,"A blow by "+striker+" is watched");
            Assert.That(landings.Any(l=>l.BySpirit),Is.True,"A spirit's blow is watched");
            foreach(var l in landings)
            {
                var what=$"{l.Event.Kind} {l.Event.Actor}->{l.Event.Target}";
                Assert.That(l.Frames.Count,Is.GreaterThanOrEqualTo(8),what+" is watched frame by frame");
                Assert.That(l.Frames.Min(f=>f.Samples),Is.GreaterThan(20),what+" face sampling sees the figure's own pixels");
                Assert.That(l.Over,Is.LessThanOrEqualTo(WashedSeconds),what+$" washes over {Washed:P0} of the struck face white for {l.Over:0.000}s, longer than {WashedSeconds}s (peak {l.Peak:P0})");
                if(!l.OnSpirit)Assert.That(l.Peak,Is.LessThanOrEqualTo(CompanionPeak),what+$" washes {l.Peak:P0} of the struck companion's face white at once");
            }
            var spirits=landings.Where(l=>l.OnSpirit).ToList();var spiritMean=spirits.Count>0?spirits.Average(l=>l.Peak):0;
            Debug.Log($"WATCHDOG spirits: {spirits.Count} landings, peak mean {spiritMean:0.000}");
            Assert.That(spiritMean,Is.LessThanOrEqualTo(SpiritMeanPeak),$"Blows wash {spiritMean:P0} of the struck spirit's face white at their brightest, on average");
            Assert.That(meanPeak,Is.LessThanOrEqualTo(CompanionMeanPeak),$"Blows wash {meanPeak:P0} of the struck companion's face white at their brightest, on average");
        }

        private static IEnumerator Watch(int encounter,List<Landing> landings)
        {
            yield return SceneManager.LoadSceneAsync("MobileG1");yield return null;yield return null;
            var view=Object.FindFirstObjectByType<MobileG1View>();
            var rt=new RenderTexture(390,844,24);Camera.main.targetTexture=rt;
            view.SetTestSafeArea(new Rect(0,34,390,763),390,844);yield return null;
            if(encounter>0)
            {
                view.Session.LoadEncounter(encounter);
                typeof(MobileG1View).GetMethod("ResetProjection",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
            }
            yield return new WaitForSecondsRealtime(.5f);

            // The dungeon camera renders multisampled; each watched frame is resolved into this copy and read back
            // asynchronously, so watching never stalls the frame (a stalled frame slows the effects, whose clock is capped
            // at .05 s a frame, and would make every blow look longer than it is).
            var source=view.FieldCamera.targetTexture;var descriptor=source.descriptor;descriptor.msaaSamples=1;descriptor.depthBufferBits=0;
            // Same sRGB encoding as shown, in a layout every platform can read back (the target itself is BGRA).
            descriptor.graphicsFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB;
            var copy=new RenderTexture(descriptor){name="Watchdog copy"};
            // Found while everyone stands: a fallen figure is inactive and GameObject.Find no longer sees it.
            var faces=view.Session.Replay.State.Units.ToDictionary(u=>u.Id,u=>new Face(view,u.Id));
            try
            {
                // Each face's own near-white share at rest, averaged over a few idle frames.
                var rest=faces.Keys.ToDictionary(id=>id,id=>new Landing{Reference=0});const int restFrames=8;
                for(var f=0;f<restFrames;f++){yield return null;Graphics.Blit(source,copy);foreach(var pair in faces)pair.Value.Request(copy,rest[pair.Key],1,false);}
                AsyncGPUReadback.WaitAllRequests();
                var reference=rest.ToDictionary(p=>p.Key,p=>p.Value.Frames.Average(f=>f.Washed));

                // The finished battle reports its frame times once; wait for it here so it never lands in the next test.
                LogAssert.Expect(LogType.Log,new System.Text.RegularExpressions.Regex("^HQ_FRAME_METRICS \\{"));
                view.Button("Attack").onClick.Invoke();view.Button("Start").onClick.Invoke();
                var seen=view.DisplayedEvents.Count;var open=new List<Landing>();var deadline=Time.realtimeSinceStartup+120;
                while((!view.DisplayedEvents.Any(e=>e.Kind==EventKind.End) || open.Count>0) && Time.realtimeSinceStartup<deadline)
                {
                    yield return null;
                    // The target texture now holds the frame just rendered; a landing seen last frame shows in it.
                    if(open.Count>0)Graphics.Blit(source,copy);
                    var seconds=Mathf.Min(Time.unscaledDeltaTime,.05f);
                    foreach(var l in open)
                    {
                        var face=faces[l.Event.Target];
                        if(face.Visible)face.Request(copy,l,seconds,true);
                        l.Age+=seconds;
                    }
                    open.RemoveAll(l=>l.Age>=Window || !faces[l.Event.Target].Visible);
                    for(;seen<view.DisplayedEvents.Count;seen++)
                    {
                        var e=view.DisplayedEvents[seen];
                        if((e.Kind!=EventKind.Damage && e.Kind!=EventKind.Counter) || !faces.ContainsKey(e.Target ?? ""))continue;
                        // One watch per figure: a counter landing on a figure still being watched takes over that watch.
                        open.RemoveAll(l=>l.Event.Target==e.Target);
                        var landing=new Landing{Event=e,BySpirit=view.Session.Replay.State.Units.Any(u=>u.Id==e.Actor && u.Enemy),OnSpirit=view.Session.Replay.State.Units.Any(u=>u.Id==e.Target && u.Enemy),Reference=reference[e.Target]};
                        landings.Add(landing);open.Add(landing);
                    }
                }
                AsyncGPUReadback.WaitAllRequests();
                var settle=Time.realtimeSinceStartup+10;while(view.LastFrameReport==null && Time.realtimeSinceStartup<settle)yield return null;
                Assert.That(view.DisplayedEvents.Any(e=>e.Kind==EventKind.End),Is.True,"Encounter "+encounter+" is shown to its end");
                Assert.That(landings.Sum(l=>l.Pending),Is.Zero,"Every read-back came back");
            }
            finally{Camera.main.targetTexture=null;Object.Destroy(rt);Object.Destroy(copy);}
        }

        // One figure's face as the dungeon camera draws it. The projected board comes from the measured mesh (never
        // moved by presentation), the face inside it from the puppet rig's head bone (companions) or the eye and mouth
        // parts (spirits), as MobileG1InteractionTests.CheckActorReadability reads it. Only the figure's own painted
        // pixels are sampled, so the stone behind a transparent margin never counts as white.
        private sealed class Face
        {
            private readonly MobileG1View view;private readonly Renderer renderer;private readonly Transform root;
            private readonly Texture2D art;private readonly Rect companionFace;
            private static readonly Dictionary<Texture,Texture2D> readable=new Dictionary<Texture,Texture2D>();
            private const int Pad=24;

            public Face(MobileG1View view,string id)
            {
                this.view=view;root=GameObject.Find("Fixture "+id).transform;
                renderer=root.GetComponentsInChildren<Renderer>().First(x=>x.name=="Private approved static skin" || x.name=="Fixture slime");
                var m=renderer.sharedMaterial;
                art=renderer.name=="Private approved static skin" && m.mainTexture!=null?Readable(m.mainTexture):null;
                companionFace=CompanionFace(id);
            }
            public bool Visible=>renderer!=null && renderer.gameObject.activeInHierarchy;
            private bool Mirrored{get{var m=renderer.sharedMaterial;return m.HasProperty("_Mirror") && m.GetFloat("_Mirror")>.5f;}}
            private Camera Camera=>view.FieldCamera;
            private Vector2 Field=>new Vector2(view.Field.rect.width,view.Field.rect.height);

            // From the rig's head bone: just below the bone's lower end to its middle, 60% of the head radius to each side.
            private static Rect CompanionFace(string id)
            {
                var index=id=="A"?0:id=="B"?1:id=="C"?2:id=="D"?3:-1;if(index<0)return Rect.zero;
                var head=RogueTactics.Presentation.Gfx.HqPuppet.Authored(index).Bones[RogueTactics.Presentation.Gfx.HqPuppet.Head];
                var u=head.A.x/.75f+.5f;var half=head.Radius*.6f/.75f;
                return Rect.MinMaxRect(u-half,head.A.y-.05f,u+half,(head.A.y+head.B.y)/2);
            }
            // The approved originals are imported unreadable; copy each once through the GPU instead of touching its import settings.
            private static Texture2D Readable(Texture texture)
            {
                if(readable.TryGetValue(texture,out var copy) && copy!=null)return copy;
                var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);Graphics.Blit(texture,rt);
                var previous=RenderTexture.active;RenderTexture.active=rt;copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
                copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
                return readable[texture]=copy;
            }
            private Vector2 ToField(Vector3 world){var v=Camera.WorldToViewportPoint(world);return new Vector2(v.x*Field.x,(v.y-1)*Field.y);}
            private Rect Board()
            {
                var points=renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>ToField(renderer.transform.TransformPoint(v))).ToArray();
                return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
            }
            private List<Rect> Parts(Rect board)
            {
                if(art!=null)
                {
                    var f=companionFace;var mirrored=Mirrored;var u0=mirrored?1-f.xMax:f.xMin;var u1=mirrored?1-f.xMin:f.xMax;
                    return new List<Rect>{Rect.MinMaxRect(board.xMin+board.width*u0,board.yMin+board.height*f.yMin,board.xMin+board.width*u1,board.yMin+board.height*f.yMax)};
                }
                var parts=new List<Rect>();var face=root.Find("Spirit face");if(face==null)return parts;
                foreach(var part in face.GetComponentsInChildren<Renderer>().Where(x=>x.name=="Bright spirit eye" || x.name=="Smile"))
                {
                    var b=part.bounds;var points=new List<Vector2>();
                    for(var k=0;k<8;k++)points.Add(ToField(new Vector3((k&1)==0?b.min.x:b.max.x,(k&2)==0?b.min.y:b.max.y,(k&4)==0?b.min.z:b.max.z)));
                    parts.Add(Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y)));
                }
                return parts;
            }
            private bool Opaque(Rect board,Vector2 p)
            {
                if(!board.Contains(p))return false;
                // A spirit is a rounded solid body: its inscribed ellipse.
                if(art==null){var c=p-board.center;var e=new Vector2(c.x/(board.width*.5f),c.y/(board.height*.5f));return e.sqrMagnitude<=1;}
                var ray=Camera.ViewportPointToRay(new Vector3(p.x/Field.x,p.y/Field.y+1,0));var t=renderer.transform;
                if(!new Plane(t.forward,t.position).Raycast(ray,out var distance))return false;
                var local=t.InverseTransformPoint(ray.GetPoint(distance));if(Mathf.Abs(local.x)>.5f || Mathf.Abs(local.y)>.5f)return false;
                return art.GetPixelBilinear((Mirrored?-local.x:local.x)+.5f,local.y+.5f).a>.5f;
            }
            // Face sample points in the dungeon camera's target texture (pixels, origin bottom-left).
            private List<Vector2Int> Samples(int width,int height)
            {
                var board=Board();var points=new List<Vector2Int>();
                foreach(var part in Parts(board))
                    for(var i=0;i<12;i++)for(var j=0;j<12;j++)
                    {
                        var p=new Vector2(Mathf.Lerp(part.xMin,part.xMax,(i+.5f)/12),Mathf.Lerp(part.yMin,part.yMax,(j+.5f)/12));
                        if(!Opaque(board,p))continue;
                        points.Add(new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(p.x/Field.x*width),0,width-1),Mathf.Clamp(Mathf.FloorToInt((p.y/Field.y+1)*height),0,height-1)));
                    }
                return points;
            }
            // Reads the face (padded, for a person to look at) out of the resolved frame and adds one watched frame to the
            // landing when it comes back: the near-white share of the face's own pixels above the landing's reference.
            public void Request(RenderTexture frame,Landing into,float seconds,bool keepWorst)
            {
                var points=Samples(frame.width,frame.height);if(points.Count==0){into.Frames.Add((seconds,0,0));return;}
                var x0=Mathf.Max(0,points.Min(p=>p.x)-Pad);var y0=Mathf.Max(0,points.Min(p=>p.y)-Pad);
                var x1=Mathf.Min(frame.width,points.Max(p=>p.x)+Pad+1);var y1=Mathf.Min(frame.height,points.Max(p=>p.y)+Pad+1);
                var w=x1-x0;var h=y1-y0;var index=into.Frames.Count;into.Frames.Add((seconds,0,points.Count));into.Pending++;
                AsyncGPUReadback.Request(frame,0,x0,w,y0,h,0,1,frame.graphicsFormat,request=>
                {
                    into.Pending--;if(request.hasError){into.Frames[index]=(seconds,1,0);return;}
                    var data=request.GetData<byte>();var white=0;
                    foreach(var p in points)
                    {
                        var k=((p.y-y0)*w+(p.x-x0))*4;
                        if(Mathf.Min(data[k],Mathf.Min(data[k+1],data[k+2]))>=NearWhite*255)white++;
                    }
                    var washed=(float)white/points.Count-into.Reference;into.Frames[index]=(seconds,washed,points.Count);
                    if(keepWorst && into.Frames.All(f=>f.Washed<=washed)){into.Worst=data.ToArray();into.WorstWidth=w;into.WorstHeight=h;}
                });
            }
        }
    }
}
