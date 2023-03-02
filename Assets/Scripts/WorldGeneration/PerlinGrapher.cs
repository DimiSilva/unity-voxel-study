using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PerlinGrapher : MonoBehaviour
{
    public LineRenderer lr;
    public float heightOffset = 0;
    public float heightScale = 2;
    public float scale = 0.5f;
    public int octaves = 1;
    [Range(0.0f, 1.0f)]
    public float probability = 1;
    void Start()
    {
        lr = this.GetComponent<LineRenderer>();
        lr.positionCount = 100;
        Graph();
    }

    void Graph()
    {
        lr = this.GetComponent<LineRenderer>();
        lr.positionCount = 100;
        int z = 11;
        Vector3[] positions = new Vector3[lr.positionCount];
        for (int x = 0; x < lr.positionCount; x++)
        {
            float y = MeshUtils.fBM(x, z, scale, heightScale, heightOffset, octaves);
            positions[x] = new Vector3(x, y, z);
        }

        lr.SetPositions(positions);
    }

    void OnValidate()
    {
        Graph();
    }

    void Update()
    {

    }
}
