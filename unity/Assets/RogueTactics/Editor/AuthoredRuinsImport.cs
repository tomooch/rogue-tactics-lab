using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RogueTactics.Editor
{
    // Reads only our offline author's triangle OBJ dialect, including its explicit RGB extension.
    // Mesh assets and GUID metadata are created by the real Editor, never fabricated in text.
    internal static class AuthoredRuinsImport
    {
        internal static Mesh Import(string path)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            var sourceNormals=new List<Vector3>();
            float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
            foreach(var line in File.ReadLines(path))
            {
                var p=line.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(p.Length==0)continue;
                if(p[0]=="v"){if(p.Length!=7)throw new InvalidDataException("Expected authored XYZ+RGB vertex");vertices.Add(new Vector3(F(p[1]),F(p[2]),F(p[3])));colors.Add(new Color(F(p[4]),F(p[5]),F(p[6]),1));}
                if(p[0]=="vt")uv.Add(new Vector2(F(p[1]),F(p[2])));
                if(p[0]=="vn")sourceNormals.Add(new Vector3(F(p[1]),F(p[2]),F(p[3])));
                if(p[0]=="f")
                {
                    if(p.Length!=4)throw new InvalidDataException("Only authored triangles are supported");
                    for(var i=1;i<4;i++){var parts=p[i].Split('/');var index=int.Parse(parts[0],CultureInfo.InvariantCulture)-1;triangles.Add(index);while(normals.Count<=index)normals.Add(Vector3.up);normals[index]=sourceNormals[int.Parse(parts[2],CultureInfo.InvariantCulture)-1];}
                }
            }
            if(vertices.Count!=uv.Count || vertices.Count!=colors.Count || normals.Count!=vertices.Count)throw new InvalidDataException("Authored vertex payload mismatch");
            var mesh=new Mesh{name=Path.GetFileNameWithoutExtension(path)};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var assetPath=Path.ChangeExtension(path,"asset");var existing=AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if(existing==null)AssetDatabase.CreateAsset(mesh,assetPath);else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);mesh=existing;}
            Debug.Log("HQ_AUTHORED_MESH "+mesh.name+" vertices="+mesh.vertexCount+" colours="+mesh.colors.Length);return mesh;
        }
    }
}
