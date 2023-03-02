using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PerlingGrapher3D : MonoBehaviour
{
    Vector3 dimensions = new Vector3(10, 10, 10);
    public float heightOffset = 0;
    public float heightScale = 2;
    public float scale = 0.03f;
    public int octaves = 2;
    [Range(0.0f, 10.0f)]
    public float drawCutOff = 1;

    void CreateCubes()
    {
        for (int z = 0; z < dimensions.z; z++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "perlin_cube";
                    cube.transform.parent = this.transform;
                    cube.transform.position = new Vector3(x, y, z);
                }
            }
        }
    }

    void Graph()
    {
        MeshRenderer[] cubes = this.GetComponentsInChildren<MeshRenderer>();
        if (cubes.Length == 0)
        {
            CreateCubes();
            return;
        }

        for (int z = 0; z < dimensions.z; z++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    float p3d = MeshUtils.fBM3D(x, y, z, scale, heightScale, heightOffset, octaves);
                    if (p3d < drawCutOff) cubes[x + (int)dimensions.x * (y + (int)dimensions.z * z)].enabled = false;
                    else cubes[x + (int)dimensions.x * (y + (int)dimensions.z * z)].enabled = true;
                }
            }
        }
    }

    void OnValidate()
    {
        Graph();
    }
}
