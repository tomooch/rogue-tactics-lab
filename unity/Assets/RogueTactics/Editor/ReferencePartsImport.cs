using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RogueTactics.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RogueTactics.Editor
{
    // Private editable SVG polygons select UV regions of the unchanged local original.
    // Triangulation is geometric clipping; it neither paints nor regenerates source RGB.
    internal static class ReferencePartsImport
    {
        public static void CaptureSourcePreview()
        {
            AssetDatabase.Refresh();
            var parts=Load("Assets/RogueTactics/Art/MobileG1/Private/ReferenceParts/",out var original);
            if(parts.Length==0)throw new InvalidDataException("No private reference parts to preview");
            AssetDatabase.SaveAssets();
            var root=new GameObject("Private source preview only");
            var material=new Material(Shader.Find("RogueTactics/StaticSkin")){mainTexture=original};
            material.SetColor("_Color",Color.white);
            var offset=-2.5f;
            foreach(var part in parts.Reverse())
            {
                var child=new GameObject(part.Mesh.name);child.layer=30;child.transform.SetParent(root.transform,false);
                child.transform.localPosition=new Vector3(offset-part.Mesh.bounds.min.x,0,0);
                offset+=part.Mesh.bounds.size.x+.35f;
                var previewMesh=UnityEngine.Object.Instantiate(part.Mesh);
                child.AddComponent<MeshFilter>().sharedMesh=previewMesh;
                child.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            var cameraObject=new GameObject("Private source preview camera");
            var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.8f;
            camera.transform.position=new Vector3(0,2.4f,-10);camera.aspect=1;camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.29f,.31f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var target=new RenderTexture(640,640,24){antiAliasing=4};target.Create();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            var previous=RenderTexture.active;RenderTexture.active=target;
            var capture=new Texture2D(640,640,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,640,640),0,0);capture.Apply();
            var folder=Path.GetFullPath("Logs/hq/private");Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,"reference-parts-source.png"),capture.EncodeToPNG());
            // Inspect the actual original region beside the clipped result, without raster edits.
            root.SetActive(false);
            var region=new GameObject("Original source region only");region.layer=30;
            var regionMesh=new Mesh();var pixels=new[]{new Vector2(200,240),new Vector2(520,240),new Vector2(520,660),new Vector2(200,660)};
            regionMesh.SetVertices(pixels.Select(p=>new Vector3((p.x-416)*.0127f,(637-p.y)*.0127f,0)).ToList());
            regionMesh.SetUVs(0,pixels.Select(p=>new Vector2(p.x/original.width,1-p.y/original.height)).ToList());
            regionMesh.SetTriangles(new[]{0,1,2,0,2,3},0);regionMesh.RecalculateBounds();
            region.AddComponent<MeshFilter>().sharedMesh=regionMesh;region.AddComponent<MeshRenderer>().sharedMaterial=material;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,640,640),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(folder,"reference-source-region.png"),capture.EncodeToPNG());
            RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(capture);
            target.Release();UnityEngine.Object.DestroyImmediate(target);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(region);UnityEngine.Object.DestroyImmediate(regionMesh);
            AssetDatabase.SaveAssets();Debug.Log("PRIVATE_REFERENCE_SOURCE_PREVIEW_COMPLETE");
        }

        internal static ReferenceEnvironmentPart[] Load(string folder, out Texture2D texture)
        {
            texture=null;var source=folder+"parts.svg";
            if(!File.Exists(source))return Array.Empty<ReferenceEnvironmentPart>();
            var path=folder+"reference.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidDataException("Private reference texture missing");
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var doc=XDocument.Load(source);var root=doc.Root;
            float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
            var width=F(root.Attribute("width").Value);var height=F(root.Attribute("height").Value);
            if(texture.width!=width || texture.height!=height)throw new InvalidDataException("Reference dimensions changed");
            var parts=new List<ReferenceEnvironmentPart>();
            foreach(var polygon in doc.Descendants().Where(e=>e.Name.LocalName=="polygon"))
            {
                var points=polygon.Attribute("points").Value.Split(new[]{' ','\n','\r'},StringSplitOptions.RemoveEmptyEntries)
                    .Select(s=>s.Split(',')).Select(p=>new Vector2(F(p[0]),F(p[1]))).ToList();
                if(points.Any(p=>p.x<0 || p.y<0 || p.x>width || p.y>height))throw new InvalidDataException("UV polygon outside original");
                var name=polygon.Attribute("id").Value;
                if(name.Any(c=>!char.IsLetterOrDigit(c) && c!='-'))throw new InvalidDataException("Invalid part name");
                var scale=F(polygon.Attribute("data-scale").Value);
                var pivot=polygon.Attribute("data-pivot").Value.Split(',');
                var origin=new Vector2(F(pivot[0]),F(pivot[1]));
                var mesh=new Mesh{name="Reference "+name};
                mesh.SetVertices(points.Select(p=>new Vector3((p.x-origin.x)*scale,(origin.y-p.y)*scale,0)).ToList());
                mesh.SetUVs(0,points.Select(p=>new Vector2(p.x/width,1-p.y/height)).ToList());
                mesh.SetTriangles(Triangulate(points),0);mesh.RecalculateBounds();
                var assetPath=folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if(existing==null)AssetDatabase.CreateAsset(mesh,assetPath);
                else
                {
                    // Set through Mesh APIs so repeated contour edits also invalidate GPU buffers.
                    existing.Clear();existing.SetVertices(mesh.vertices);existing.SetUVs(0,mesh.uv.ToList());
                    existing.SetTriangles(mesh.triangles,0);existing.RecalculateBounds();existing.UploadMeshData(false);
                    UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);mesh=existing;
                }
                if(mesh.vertexCount!=points.Count)throw new InvalidDataException("Imported contour vertex count mismatch");
                var position=polygon.Attribute("data-position").Value.Split(',');
                parts.Add(new ReferenceEnvironmentPart{Mesh=mesh,Position=new Vector3(F(position[0]),F(position[1]),F(position[2]))});
                Debug.Log("PRIVATE_REFERENCE_PART "+name+" vertices="+points.Count);
            }
            return parts.ToArray();
        }

        private static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        private static List<int> Triangulate(List<Vector2> points)
        {
            if(points.Count<3)throw new InvalidDataException("Part needs a contour");
            var area=0f;for(var i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];area+=a.x*b.y-b.x*a.y;}
            if(Mathf.Abs(area)<.01f)throw new InvalidDataException("Empty contour");
            var sign=Mathf.Sign(area);var remaining=Enumerable.Range(0,points.Count).ToList();var result=new List<int>();
            while(remaining.Count>3)
            {
                var found=false;
                for(var i=0;i<remaining.Count;i++)
                {
                    var a=remaining[(i+remaining.Count-1)%remaining.Count];var b=remaining[i];var c=remaining[(i+1)%remaining.Count];
                    if(Cross(points[a],points[b],points[c])*sign<=.001f)continue;
                    if(remaining.Any(j=>j!=a && j!=b && j!=c &&
                        Cross(points[a],points[b],points[j])*sign>=0 &&
                        Cross(points[b],points[c],points[j])*sign>=0 &&
                        Cross(points[c],points[a],points[j])*sign>=0))continue;
                    result.AddRange(new[]{a,b,c});remaining.RemoveAt(i);found=true;break;
                }
                if(!found)throw new InvalidDataException("Self-intersecting or degenerate reference contour");
            }
            result.AddRange(remaining);
            var sum=0f;for(var i=0;i<result.Count;i+=3)sum+=Mathf.Abs(Cross(points[result[i]],points[result[i+1]],points[result[i+2]]));
            if(Mathf.Abs(sum-Mathf.Abs(area))>.1f)throw new InvalidDataException("Clipped mesh does not cover exactly the source contour");
            return result;
        }
    }
}
