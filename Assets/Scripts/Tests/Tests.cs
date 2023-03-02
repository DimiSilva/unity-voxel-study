using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tests : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        Application.targetFrameRate = 60;

        MeshRenderer mr = this.gameObject.AddComponent<MeshRenderer>();
        MeshFilter mf = this.gameObject.AddComponent<MeshFilter>();

        Vector3 p0 = new Vector3(-0.5f, -0.5f, 0.5f);
        Vector3 p1 = new Vector3(0.5f, -0.5f, 0.5f);
        Vector3 p2 = new Vector3(0.5f, -0.5f, -0.5f);
        Vector3 p3 = new Vector3(-0.5f, -0.5f, -0.5f);
        Vector3 p4 = new Vector3(-0.5f, 0.5f, 0.5f);
        Vector3 p5 = new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 p6 = new Vector3(0.5f, 0.5f, -0.5f);
        Vector3 p7 = new Vector3(-0.5f, 0.5f, -0.5f);

        List<int> triangles = new List<int>();
        int currentFaceIndex = 0;
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            if (faceIndex == 2) continue;
            triangles.Add(3 + currentFaceIndex * 4);
            triangles.Add(1 + currentFaceIndex * 4);
            triangles.Add(0 + currentFaceIndex * 4);
            triangles.Add(3 + currentFaceIndex * 4);
            triangles.Add(2 + currentFaceIndex * 4);
            triangles.Add(1 + currentFaceIndex * 4);
            currentFaceIndex++;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            p4, p5, p1, p0,
            p6, p7, p3, p2,
            // p7, p4, p0, p3,
            p5, p6, p2, p1,
            p7, p6, p5, p4,
            p0, p1, p2, p3
        };
        mesh.normals = new Vector3[] {
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            // Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
        };
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
