using System.Collections.Generic;
using System.Linq;
using RogueTactics.Core;
using RogueTactics.Core.Exploration;
using UnityEngine;
using UnityEngine.Rendering;

namespace RogueTactics.Presentation
{
    // Reuses the locally authored stone/plant surfaces, not an overview bitmap.
    public sealed class HqFloorScenery : MonoBehaviour
    {
        private readonly Dictionary<int,GameObject> rooms=new Dictionary<int,GameObject>();
        private readonly Dictionary<GridPosition,GameObject> bridges=new Dictionary<GridPosition,GameObject>();
        private readonly List<Mesh> ownedMeshes=new List<Mesh>();
        private readonly List<Material> ownedMaterials=new List<Material>();
        public static HqFloorScenery Build(FloorMap map,Mesh[] authored,Material template,GameObject startingRuins)
        {
            var owner=new GameObject("Connected sky ruins floor").AddComponent<HqFloorScenery>();
            owner.JoinStartingGround(startingRuins);
            var court=authored.First(m=>m.name=="Courtyard");
            var roomSurfaces=authored.Where(m=>m.name=="Courtyard" || m.name=="Cliff" || m.name=="Foliage" || m.name=="Flowers").Select(mesh=>
            {
                if(mesh.name=="Courtyard")return mesh;
                // Source Cliff also contains upper terrace columns, and the plant
                // meshes include an elevated arch garden. Their supports are not
                // copied into these rooms: keep only the grounded lower geometry.
                var lower=Instantiate(mesh);lower.name=mesh.name;
                var verts=lower.vertices;var triangles=lower.triangles;var keep=new List<int>();var ceiling=mesh.name=="Cliff"?-.24f:.7f;
                for(var i=0;i<triangles.Length;i+=3)
                    if(verts[triangles[i]].y<=ceiling && verts[triangles[i+1]].y<=ceiling && verts[triangles[i+2]].y<=ceiling)
                    {keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);}
                lower.SetTriangles(keep,0);lower.RecalculateBounds();owner.ownedMeshes.Add(lower);return lower;
            }).ToArray();
            foreach(var room in map.Rooms.Where(r=>r.Id!=0))
            {
                var root=new GameObject("Exploration room "+room.Id);root.transform.SetParent(owner.transform,false);
                root.transform.position=new Vector3((room.X+(room.Width-1)*.5f)*2.5f,0,(room.Y+(room.Height-1)*.5f)*2.5f);
                root.transform.localScale=new Vector3(room.Width*2.5f/court.bounds.size.x,1,room.Height*2.5f/court.bounds.size.z);
                foreach(var mesh in roomSurfaces)
                {
                    var mat=owner.Material(template,mesh.name=="Courtyard");var child=new GameObject(mesh.name);child.transform.SetParent(root.transform,false);
                    child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=mat;
                }
                owner.rooms.Add(room.Id,root);root.SetActive(false);
            }
            var tiles=new Mesh[8];for(var i=0;i<tiles.Length;i++)
            {tiles[i]=ClipTile(court,-5+(i%4)*2.5f,-8+(i/4)*8);owner.ownedMeshes.Add(tiles[i]);}
            var stone=owner.Material(template,true);
            // The original island cap is smaller than a complete5x5 tile rectangle.
            // Door aprons cover its transition to the two real passage entrances.
            foreach(var p in map.Cells.Where(p=>map.RoomAt(p)<0 || (map.RoomAt(p)==0 &&
                (p.X<=0 || p.Y<=0 || (p.Y==4 && (p.X==2 || p.X==3)) || (p.X==4 && (p.Y==2 || p.Y==3))))))
            {
                var root=new GameObject("Stone passage "+p.X+","+p.Y);root.transform.SetParent(owner.transform,false);
                root.transform.position=new Vector3(p.X*2.5f,0,p.Y*2.5f);
                root.AddComponent<MeshFilter>().sharedMesh=tiles[((p.X*3+p.Y*5)%tiles.Length+tiles.Length)%tiles.Length];root.AddComponent<MeshRenderer>().sharedMaterial=stone;
                var support=GameObject.CreatePrimitive(PrimitiveType.Cube);support.name="Continuous bridge support";support.transform.SetParent(root.transform,false);
                support.transform.localPosition=new Vector3(0,-.82f,0);support.transform.localScale=new Vector3(2.5f,1.1f,2.5f);
                var lit=owner.ownedMaterials.FirstOrDefault(m=>m.name=="Bridge support stone");
                if(lit==null){lit=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Bridge support stone"};lit.color=new Color(.43f,.46f,.40f);owner.ownedMaterials.Add(lit);}
                support.GetComponent<Renderer>().sharedMaterial=lit;Destroy(support.GetComponent<Collider>());
                owner.bridges.Add(p,root);root.SetActive(false);
            }
            return owner;
        }
        private Material Material(Material template,bool court)
        {
            var material=new Material(template);material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_SrgbPalette",court?1:0);material.SetFloat("_WeatheredStone",court?1:0);
            material.SetFloat("_Cull",0);ownedMaterials.Add(material);return material;
        }
        public void Reveal(FloorMap map,ISet<GridPosition> known)
        {
            foreach(var pair in rooms)pair.Value.SetActive(known.Any(p=>map.RoomAt(p)==pair.Key));
            foreach(var pair in bridges)pair.Value.SetActive(known.Contains(pair.Key));
        }
        private struct Vertex
        {
            public Vector3 P,N;public Vector2 U;public Color C;
            public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{P=Vector3.Lerp(a.P,b.P,t),N=Vector3.Lerp(a.N,b.N,t),U=Vector2.Lerp(a.U,b.U,t),C=Color.Lerp(a.C,b.C,t)};
        }
        // South/west used to be the edge of a separate island. In this
        // exploration variant those edges are interior, so remove their low
        // rim/water geometry and join them with real ground, not a fake wall.
        // Imported assets and the standalone G1 scene stay unchanged.
        private void JoinStartingGround(GameObject ruins)
        {
            foreach(var filter in ruins.GetComponentsInChildren<MeshFilter>())
            {
                var name=filter.sharedMesh.name;
                if(name.StartsWith("HeroLedge")){filter.gameObject.SetActive(false);continue;}
                if(!new[]{"Courtyard","Cliff","Limestone","Detail","Foliage","Flowers","Water"}.Contains(name))continue;
                var source=filter.sharedMesh;var verts=source.vertices;var normals=source.normals;var colours=source.colors;var uv=source.uv;var input=source.triangles;
                var output=new List<Vertex>();var triangles=new List<int>();
                for(var i=0;i<input.Length;i+=3)
                {
                    var polygon=new List<Vertex>();
                    for(var j=0;j<3;j++){var n=input[i+j];polygon.Add(new Vertex{P=verts[n],N=normals[n],U=uv[n],C=colours[n]});}
                    if(name=="Courtyard" || polygon.All(v=>v.P.y<=.35f))
                    {polygon=Clip(polygon,0,-3.75f);polygon=Clip(polygon,2,-7.5f);}
                    if(polygon.Count<3)continue;var start=output.Count;output.AddRange(polygon);
                    for(var j=1;j<polygon.Count-1;j++){triangles.Add(start);triangles.Add(start+j);triangles.Add(start+j+1);}
                }
                var mesh=new Mesh{name=name+" joined interior"};
                mesh.SetVertices(output.Select(v=>v.P).ToList());mesh.SetNormals(output.Select(v=>v.N).ToList());
                mesh.SetUVs(0,output.Select(v=>v.U).ToList());mesh.SetColors(output.Select(v=>v.C).ToList());mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
                filter.sharedMesh=mesh;ownedMeshes.Add(mesh);
            }
        }
        private static Mesh ClipTile(Mesh source,float x,float z)
        {
            var vertices=source.vertices;var normals=source.normals;var colours=source.colors;var uv=source.uv;var input=source.triangles;
            var output=new List<Vertex>();var triangles=new List<int>();
            for(var i=0;i<input.Length;i+=3)
            {
                var polygon=new List<Vertex>();for(var j=0;j<3;j++){var n=input[i+j];polygon.Add(new Vertex{P=vertices[n],N=normals[n],U=uv[n],C=colours[n]});}
                polygon=Clip(polygon,0,x);polygon=Clip(polygon,1,x+2.5f);polygon=Clip(polygon,2,z);polygon=Clip(polygon,3,z+8);
                if(polygon.Count<3)continue;var start=output.Count;output.AddRange(polygon);
                for(var j=1;j<polygon.Count-1;j++){triangles.Add(start);triangles.Add(start+j);triangles.Add(start+j+1);}
            }
            var mesh=new Mesh{name="Authored passage stone"};
            mesh.SetVertices(output.Select(v=>new Vector3(v.P.x-x-1.25f,v.P.y,(v.P.z-z-4)*.3125f)).ToList());
            mesh.SetNormals(output.Select(v=>v.N).ToList());mesh.SetUVs(0,output.Select(v=>v.U).ToList());mesh.SetColors(output.Select(v=>v.C).ToList());mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        private static List<Vertex> Clip(List<Vertex> points,int plane,float bound)
        {
            var result=new List<Vertex>();if(points.Count==0)return result;
            float Distance(Vertex v)=>plane==0?v.P.x-bound:plane==1?bound-v.P.x:plane==2?v.P.z-bound:bound-v.P.z;
            var a=points[points.Count-1];var da=Distance(a);
            foreach(var b in points){var db=Distance(b);if((da>=0)!=(db>=0))result.Add(Vertex.Lerp(a,b,da/(da-db)));if(db>=0)result.Add(b);a=b;da=db;}return result;
        }
        private void OnDestroy(){foreach(var m in ownedMeshes)Destroy(m);foreach(var m in ownedMaterials)Destroy(m);}
    }
}
