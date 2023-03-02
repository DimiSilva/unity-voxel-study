using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Block
{
    public Mesh mesh;
    public Chunk parentChunk;
    public Block(Chunk chunk, Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles)
    {
        parentChunk = chunk;

        if (vertices.Length == 0) return;

        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
    }
}
