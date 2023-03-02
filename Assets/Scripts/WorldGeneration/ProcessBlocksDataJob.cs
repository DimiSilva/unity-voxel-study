using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System;

[BurstCompile]
struct ProcessBlocksDataJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int3> offsets;
    [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<MeshUtils.VoxelTypesEnum> chunkVoxeltypes;
    public int3 chunkLocation;
    public NativeMultiHashMap<int3, float3>.ParallelWriter verticesParallelWriter;
    public NativeMultiHashMap<int3, float3>.ParallelWriter normalsParallelWriter;
    public NativeMultiHashMap<int3, float2>.ParallelWriter uvParallelWriter;
    public NativeMultiHashMap<int3, int>.ParallelWriter trianglesParallelWriter;
    [ReadOnly] public NativeMultiHashMap<int, float2> voxelFacesUVs;
    [ReadOnly] public NativeMultiHashMap<int, MeshUtils.VoxelFaceTypesEnum> voxelTypes;
    [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<MeshUtils.VoxelInteractionTypesEnum> voxelTypesInteractionTypes;
    public int chunkWidth;
    public int chunkHeight;
    public int chunkDepth;

    public void Dispose()
    {
        offsets.Dispose();
        chunkVoxeltypes.Dispose();
        voxelFacesUVs.Dispose();
        voxelTypes.Dispose();
        voxelTypesInteractionTypes.Dispose();
    }

    public void Execute(int index)
    {

        MeshUtils.VoxelTypesEnum voxelType = chunkVoxeltypes[index];
        MeshUtils.VoxelInteractionTypesEnum voxelInteractionType = voxelTypesInteractionTypes[(int)voxelType];

        if (voxelTypesInteractionTypes[(int)voxelType] == MeshUtils.VoxelInteractionTypesEnum.EMPTY) return;

        int3 offset = offsets[index];
        CreateBlockMeshData(offset, voxelType);
    }

    public void CreateBlockMeshData(int3 offset, MeshUtils.VoxelTypesEnum voxelType)
    {
        int3 globalOffset = offset + chunkLocation;

        float3 p0 = new float3(-0.5f, -0.5f, 0.5f) + globalOffset;
        float3 p1 = new float3(0.5f, -0.5f, 0.5f) + globalOffset;
        float3 p2 = new float3(0.5f, -0.5f, -0.5f) + globalOffset;
        float3 p3 = new float3(-0.5f, -0.5f, -0.5f) + globalOffset;
        float3 p4 = new float3(-0.5f, 0.5f, 0.5f) + globalOffset;
        float3 p5 = new float3(0.5f, 0.5f, 0.5f) + globalOffset;
        float3 p6 = new float3(0.5f, 0.5f, -0.5f) + globalOffset;
        float3 p7 = new float3(-0.5f, 0.5f, -0.5f) + globalOffset;

        NativeList<int> voxelFacesToHide = new NativeList<int>(Allocator.Temp);
        if (CheckIfVoxelHaveNeighbour(offset.x, offset.y, offset.z + 1)) voxelFacesToHide.Add((int)MeshUtils.VoxelFaceSidesEnum.FORWARD);
        if (CheckIfVoxelHaveNeighbour(offset.x, offset.y, offset.z - 1)) voxelFacesToHide.Add((int)MeshUtils.VoxelFaceSidesEnum.BACK);
        if (CheckIfVoxelHaveNeighbour(offset.x - 1, offset.y, offset.z)) voxelFacesToHide.Add((int)MeshUtils.VoxelFaceSidesEnum.LEFT);
        if (CheckIfVoxelHaveNeighbour(offset.x + 1, offset.y, offset.z)) voxelFacesToHide.Add((int)MeshUtils.VoxelFaceSidesEnum.RIGHT);
        if (CheckIfVoxelHaveNeighbour(offset.x, offset.y + 1, offset.z)) voxelFacesToHide.Add((int)MeshUtils.VoxelFaceSidesEnum.UP);
        if (CheckIfVoxelHaveNeighbour(offset.x, offset.y - 1, offset.z)) voxelFacesToHide.Add((int)MeshUtils.VoxelFaceSidesEnum.DOWN);

        if (voxelFacesToHide.Length == 6) return;

        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.FORWARD)) { verticesParallelWriter.Add(offset, p4); verticesParallelWriter.Add(offset, p5); verticesParallelWriter.Add(offset, p1); verticesParallelWriter.Add(offset, p0); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.BACK)) { verticesParallelWriter.Add(offset, p6); verticesParallelWriter.Add(offset, p7); verticesParallelWriter.Add(offset, p3); verticesParallelWriter.Add(offset, p2); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.LEFT)) { verticesParallelWriter.Add(offset, p7); verticesParallelWriter.Add(offset, p4); verticesParallelWriter.Add(offset, p0); verticesParallelWriter.Add(offset, p3); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.RIGHT)) { verticesParallelWriter.Add(offset, p5); verticesParallelWriter.Add(offset, p6); verticesParallelWriter.Add(offset, p2); verticesParallelWriter.Add(offset, p1); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.UP)) { verticesParallelWriter.Add(offset, p7); verticesParallelWriter.Add(offset, p6); verticesParallelWriter.Add(offset, p5); verticesParallelWriter.Add(offset, p4); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.DOWN)) { verticesParallelWriter.Add(offset, p0); verticesParallelWriter.Add(offset, p1); verticesParallelWriter.Add(offset, p2); verticesParallelWriter.Add(offset, p3); }

        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.FORWARD)) { normalsParallelWriter.Add(offset, Vector3.forward); normalsParallelWriter.Add(offset, Vector3.forward); normalsParallelWriter.Add(offset, Vector3.forward); normalsParallelWriter.Add(offset, Vector3.forward); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.BACK)) { normalsParallelWriter.Add(offset, Vector3.back); normalsParallelWriter.Add(offset, Vector3.back); normalsParallelWriter.Add(offset, Vector3.back); normalsParallelWriter.Add(offset, Vector3.back); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.LEFT)) { normalsParallelWriter.Add(offset, Vector3.left); normalsParallelWriter.Add(offset, Vector3.left); normalsParallelWriter.Add(offset, Vector3.left); normalsParallelWriter.Add(offset, Vector3.left); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.RIGHT)) { normalsParallelWriter.Add(offset, Vector3.right); normalsParallelWriter.Add(offset, Vector3.right); normalsParallelWriter.Add(offset, Vector3.right); normalsParallelWriter.Add(offset, Vector3.right); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.UP)) { normalsParallelWriter.Add(offset, Vector3.up); normalsParallelWriter.Add(offset, Vector3.up); normalsParallelWriter.Add(offset, Vector3.up); normalsParallelWriter.Add(offset, Vector3.up); }
        if (!voxelFacesToHide.Contains((int)MeshUtils.VoxelFaceSidesEnum.DOWN)) { normalsParallelWriter.Add(offset, Vector3.down); normalsParallelWriter.Add(offset, Vector3.down); normalsParallelWriter.Add(offset, Vector3.down); normalsParallelWriter.Add(offset, Vector3.down); }

        NativeList<MeshUtils.VoxelFaceTypesEnum> voxelTypeFaces = new NativeList<MeshUtils.VoxelFaceTypesEnum>(Allocator.Temp);
        NativeMultiHashMap<int, MeshUtils.VoxelFaceTypesEnum>.Enumerator voxelTypeFacesEnumerator = voxelTypes.GetValuesForKey((int)voxelType);
        while (voxelTypeFacesEnumerator.MoveNext())
            voxelTypeFaces.Add(voxelTypeFacesEnumerator.Current);

        voxelTypeFacesEnumerator.Dispose();

        int currentTriangleMultiplier = 0;

        NativeList<float2> blockUV = new NativeList<float2>(Allocator.Temp);

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            if (voxelFacesToHide.Contains(faceIndex)) continue;

            MeshUtils.VoxelFaceTypesEnum voxelfaceType = voxelTypeFaces[faceIndex];
            NativeMultiHashMap<int, float2>.Enumerator blockUVEnumerator = voxelFacesUVs.GetValuesForKey((int)voxelfaceType);
            while (blockUVEnumerator.MoveNext())
                blockUV.Add(blockUVEnumerator.Current);

            float2 uv00 = blockUV[0];
            float2 uv10 = blockUV[1];
            float2 uv01 = blockUV[2];
            float2 uv11 = blockUV[3];

            uvParallelWriter.Add(offset, uv11);
            uvParallelWriter.Add(offset, uv01);
            uvParallelWriter.Add(offset, uv00);
            uvParallelWriter.Add(offset, uv10);


            trianglesParallelWriter.Add(offset, 3 + currentTriangleMultiplier * 4);
            trianglesParallelWriter.Add(offset, 1 + currentTriangleMultiplier * 4);
            trianglesParallelWriter.Add(offset, 0 + currentTriangleMultiplier * 4);
            trianglesParallelWriter.Add(offset, 3 + currentTriangleMultiplier * 4);
            trianglesParallelWriter.Add(offset, 2 + currentTriangleMultiplier * 4);
            trianglesParallelWriter.Add(offset, 1 + currentTriangleMultiplier * 4);
            currentTriangleMultiplier++;
            blockUV.Clear();
        }

        blockUV.Dispose();
        voxelTypeFaces.Dispose();
        voxelFacesToHide.Dispose();
    }

    bool CheckIfVoxelHaveNeighbour(int x, int y, int z)
    {
        bool xOutOfChunkLimit = x < 0 || x >= chunkWidth;
        bool yOutOfChunkLimit = y < 0 || y >= chunkHeight;
        bool zOutOfChunkLimit = z < 0 || z >= chunkDepth;

        if (xOutOfChunkLimit || yOutOfChunkLimit || zOutOfChunkLimit) return false;

        MeshUtils.VoxelTypesEnum neighbourVoxelType = chunkVoxeltypes[x + chunkWidth * (y + chunkHeight * z)];
        MeshUtils.VoxelInteractionTypesEnum neighbourVoxelInteractionType = voxelTypesInteractionTypes[(int)neighbourVoxelType];

        if (neighbourVoxelInteractionType == MeshUtils.VoxelInteractionTypesEnum.EMPTY || neighbourVoxelInteractionType == MeshUtils.VoxelInteractionTypesEnum.LIQUID) return false;

        return true;
    }
}