using System.Collections.Generic;
using System.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Rendering;
using Unity.Collections;
using UnityEngine;
using System;

[BurstCompile]
struct BuildChunkDataJob : IJobParallelFor
{
    public NativeArray<MeshUtils.VoxelTypesEnum> chunkData;
    public int width;
    public int height;
    public int3 location;
    [ReadOnly] public NativeArray<Unity.Mathematics.Random> randoms;
    public PerlinSettings surfaceSettings;
    public PerlinSettings stoneSettings;
    public PerlinSettings diamondTopSettings;
    public PerlinSettings diamondBottomSettings;
    public PerlinSettings bedrockSettings;
    public PerlinSettings cavesSettings;

    public void Execute(int i)
    {
        int x = i % width + location.x;
        int y = (i / width) % height + location.y;
        int z = i / (width * height) + location.z;

        Unity.Mathematics.Random random = randoms[i];

        int surfaceLayer = (int)MeshUtils.fBM(x, z, surfaceSettings.scale, surfaceSettings.heightScale, surfaceSettings.heightOffset, surfaceSettings.octaves);
        int stoneLayer = (int)MeshUtils.fBM(x, z, stoneSettings.scale, stoneSettings.heightScale, stoneSettings.heightOffset, stoneSettings.octaves);
        int diamondTopLayer = (int)MeshUtils.fBM(x, z, diamondTopSettings.scale, diamondTopSettings.heightScale, diamondTopSettings.heightOffset, diamondTopSettings.octaves);
        int diamondBottomLayer = (int)MeshUtils.fBM(x, z, diamondBottomSettings.scale, diamondBottomSettings.heightScale, diamondBottomSettings.heightOffset, diamondBottomSettings.octaves);
        int bedrockLayer = (int)MeshUtils.fBM(x, z, bedrockSettings.scale, bedrockSettings.heightScale, bedrockSettings.heightOffset, bedrockSettings.octaves);
        int digCave = (int)MeshUtils.fBM3D(x, y, z, cavesSettings.scale, cavesSettings.heightScale, cavesSettings.heightOffset, cavesSettings.octaves);

        if (y == bedrockLayer) chunkData[i] = MeshUtils.VoxelTypesEnum.OBSIDIAN;
        else if (y < bedrockLayer) chunkData[i] = MeshUtils.VoxelTypesEnum.AIR;
        else if (digCave < cavesSettings.probability) chunkData[i] = MeshUtils.VoxelTypesEnum.AIR;
        else if (y == surfaceLayer) chunkData[i] = MeshUtils.VoxelTypesEnum.GRASS;
        else if (y < diamondTopLayer && y > diamondBottomLayer && random.NextFloat(1) <= diamondTopSettings.probability)
            chunkData[i] = MeshUtils.VoxelTypesEnum.DIAMOND;
        else if (y < stoneLayer) chunkData[i] = MeshUtils.VoxelTypesEnum.STONE;
        else if (y < surfaceLayer) chunkData[i] = MeshUtils.VoxelTypesEnum.DIRT;
        else chunkData[i] = MeshUtils.VoxelTypesEnum.AIR;
    }
}

[BurstCompile]
struct ProcessMeshDataJob : IJobParallelFor
{
    [ReadOnly] public Mesh.MeshDataArray meshData;
    public Mesh.MeshData outputMesh;
    public NativeArray<int> verticesStarters;
    public NativeArray<int> trianglesStarters;

    public void Execute(int index)
    {
        var data = meshData[index];
        var vertexCount = data.vertexCount;
        var vertexStart = verticesStarters[index];

        var vertices = new NativeArray<float3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        data.GetVertices(vertices.Reinterpret<Vector3>());

        var normals = new NativeArray<float3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        data.GetNormals(normals.Reinterpret<Vector3>());

        var uvs = new NativeArray<float3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        data.GetUVs(0, uvs.Reinterpret<Vector3>());

        var outputVertices = outputMesh.GetVertexData<Vector3>(stream: 0);
        var outputNormals = outputMesh.GetVertexData<Vector3>(stream: 1);
        var outputUVs = outputMesh.GetVertexData<Vector3>(stream: 2);

        for (int i = 0; i < vertexCount; i++)
        {
            outputVertices[i + vertexStart] = vertices[i];
            outputNormals[i + vertexStart] = normals[i];
            outputUVs[i + vertexStart] = uvs[i];
        }

        vertices.Dispose();
        normals.Dispose();
        uvs.Dispose();

        var triangleStart = trianglesStarters[index];
        var triangleCount = data.GetSubMesh(0).indexCount;
        var outputTriangles = outputMesh.GetIndexData<int>();

        if (data.indexFormat == IndexFormat.UInt16)
        {
            var triangles = data.GetIndexData<ushort>();
            for (int i = 0; i < triangleCount; i++)
            {
                int triangle = triangles[i];
                outputTriangles[i + triangleStart] = vertexStart + triangle;
            }
        }
        else
        {
            var triangles = data.GetIndexData<int>();
            for (int i = 0; i < triangleCount; i++)
            {
                int triangle = triangles[i];
                outputTriangles[i + triangleStart] = vertexStart + triangle;
            }
        }
    }
}

public class Chunk : MonoBehaviour
{
    public Material atlas;
    public Material transparentAtlas;
    public int width = 2;
    public int height = 2;
    public int depth = 0;
    public int3 location;
    public Block[,,] blocks;
    public List<Mesh> meshes;
    public MeshUtils.VoxelTypesEnum[] chunkData;
    public MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    void BuildChunk()
    {
        int blockCount = width * depth * height;
        chunkData = new MeshUtils.VoxelTypesEnum[blockCount];
        NativeArray<MeshUtils.VoxelTypesEnum> nativeChunkData = new NativeArray<MeshUtils.VoxelTypesEnum>(blockCount, Allocator.Persistent);
        Unity.Mathematics.Random[] randomArray = new Unity.Mathematics.Random[blockCount];
        System.Random seed = new System.Random();

        for (int i = 0; i < blockCount; i++)
            randomArray[i] = new Unity.Mathematics.Random((uint)seed.Next());

        NativeArray<Unity.Mathematics.Random> randomNativeArray = new NativeArray<Unity.Mathematics.Random>(randomArray, Allocator.Persistent);

        BuildChunkDataJob buildChunkDataJob = new BuildChunkDataJob()
        {
            chunkData = nativeChunkData,
            height = height,
            location = location,
            width = width,
            randoms = randomNativeArray,
            surfaceSettings = World.surfaceSettings,
            stoneSettings = World.stoneSettings,
            diamondBottomSettings = World.diamondBottomSettings,
            diamondTopSettings = World.diamondTopSettings,
            bedrockSettings = World.bedrockSettings,
            cavesSettings = World.cavesSettings
        };
        JobHandle buildChunkDataJobHandle = buildChunkDataJob.Schedule(blockCount, 64);
        buildChunkDataJobHandle.Complete();
        buildChunkDataJob.chunkData.CopyTo(chunkData);

        nativeChunkData.Dispose();
        randomNativeArray.Dispose();
    }

    public MeshUtils.VoxelTypesEnum GetBlockChunkData(int x, int y, int z)
    {
        return chunkData[x + width * (y + depth * z)];
    }

    public IEnumerator CreateChunk(Vector3 dimensions, Vector3 position)
    {
        location = new int3((int)position.x, (int)position.y, (int)position.z);
        width = (int)dimensions.x;
        height = (int)dimensions.y;
        depth = (int)dimensions.z;

        int blocksCount = depth * height * width;

        meshFilter = this.gameObject.AddComponent<MeshFilter>();
        meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        meshRenderer.material = atlas;

        blocks = new Block[width, height, depth];
        meshes = new List<Mesh>();

        BuildChunk();

        CreateBlocksAndMeshes();

        CreateChunkMesh();

        yield return null;
    }

    private void CreateBlocksAndMeshes()
    {
        int blocksCount = depth * height * width;

        NativeArray<int3> offsets = new NativeArray<int3>(blocksCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeMultiHashMap<int, float2> voxelFacesUVs = new NativeMultiHashMap<int, float2>(MeshUtils.voxelFacesUVs.GetLength(0) * 4, Allocator.TempJob);
        NativeMultiHashMap<int, MeshUtils.VoxelFaceTypesEnum> voxelTypes = new NativeMultiHashMap<int, MeshUtils.VoxelFaceTypesEnum>(MeshUtils.voxelTypes.GetLength(0) * 6, Allocator.TempJob);
        NativeMultiHashMap<int3, float3> vertices = new NativeMultiHashMap<int3, float3>(4 * 6 * (blocksCount), Allocator.TempJob);
        NativeMultiHashMap<int3, float3> normals = new NativeMultiHashMap<int3, float3>(4 * 6 * (blocksCount), Allocator.TempJob);
        NativeMultiHashMap<int3, float2> uv = new NativeMultiHashMap<int3, float2>(4 * 6 * (blocksCount), Allocator.TempJob);
        NativeMultiHashMap<int3, int> triangles = new NativeMultiHashMap<int3, int>(6 * 6 * (blocksCount), Allocator.TempJob);

        for (int i = 0; i < MeshUtils.voxelFacesUVs.GetLength(0); i++)
            for (int j = 3; j >= 0; j--)
                voxelFacesUVs.Add(i, MeshUtils.voxelFacesUVs[i, j]);

        for (int i = 0; i < MeshUtils.voxelTypes.GetLength(0); i++)
            for (int j = 5; j >= 0; j--)
                voxelTypes.Add(i, MeshUtils.voxelTypes[i, j]);

        NativeArray<MeshUtils.VoxelTypesEnum> chunkVoxeltypes = new NativeArray<MeshUtils.VoxelTypesEnum>(chunkData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        chunkVoxeltypes.CopyFrom(chunkData);

        NativeArray<MeshUtils.VoxelInteractionTypesEnum> voxelTypesInteractionTypes = new NativeArray<MeshUtils.VoxelInteractionTypesEnum>(MeshUtils.voxelTypesInteractionTypes.Length, Allocator.TempJob);
        voxelTypesInteractionTypes.CopyFrom(MeshUtils.voxelTypesInteractionTypes);


        ProcessBlocksDataJob processBlocksDataJob = new ProcessBlocksDataJob()
        {
            verticesParallelWriter = vertices.AsParallelWriter(),
            normalsParallelWriter = normals.AsParallelWriter(),
            uvParallelWriter = uv.AsParallelWriter(),
            trianglesParallelWriter = triangles.AsParallelWriter(),
            voxelFacesUVs = voxelFacesUVs,
            voxelTypes = voxelTypes,
            voxelTypesInteractionTypes = voxelTypesInteractionTypes,
            chunkVoxeltypes = chunkVoxeltypes,
            chunkDepth = depth,
            chunkHeight = height,
            chunkLocation = location,
            chunkWidth = width,
            offsets = offsets,
        };

        int currentOffsetIndex = 0;

        for (int z = 0; z < depth; z++)
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    offsets[currentOffsetIndex] = new int3(x, y, z);
                    currentOffsetIndex++;
                }

        JobHandle processBlocksDataJobHandle = processBlocksDataJob.Schedule(blocksCount, 4);
        processBlocksDataJobHandle.Complete();

        List<Vector3> blockVertices = new List<Vector3>();
        List<Vector3> blockNormals = new List<Vector3>();
        List<Vector2> blockUV = new List<Vector2>();
        List<int> blockTriangles = new List<int>();

        foreach (int3 offset in offsets)
        {
            NativeMultiHashMap<int3, float3>.Enumerator verticesEnumerator = vertices.GetValuesForKey(offset);
            while (verticesEnumerator.MoveNext())
                blockVertices.Add(verticesEnumerator.Current);
            verticesEnumerator.Dispose();

            NativeMultiHashMap<int3, float3>.Enumerator normalsEnumerator = normals.GetValuesForKey(offset);
            while (normalsEnumerator.MoveNext())
                blockNormals.Add(normalsEnumerator.Current);
            normalsEnumerator.Dispose();

            NativeMultiHashMap<int3, float2>.Enumerator uvEnumerator = uv.GetValuesForKey(offset);
            while (uvEnumerator.MoveNext())
                blockUV.Add(uvEnumerator.Current);
            uvEnumerator.Dispose();

            NativeMultiHashMap<int3, int>.Enumerator trianglesEnumerator = triangles.GetValuesForKey(offset);
            while (trianglesEnumerator.MoveNext())
                blockTriangles.Add(trianglesEnumerator.Current);
            trianglesEnumerator.Dispose();

            if (blockVertices.Count > 0)
            {
                Block block = new Block(this, blockVertices.ToArray(), blockNormals.ToArray(), blockUV.ToArray(), blockTriangles.ToArray());
                blocks[offset.x, offset.y, offset.z] = block;
                meshes.Add(block.mesh);
            }

            vertices.Remove(offset);
            normals.Remove(offset);
            uv.Remove(offset);
            triangles.Remove(offset);

            blockVertices.Clear();
            blockNormals.Clear();
            blockUV.Clear();
            blockTriangles.Clear();
        }

        processBlocksDataJob.Dispose();
        vertices.Dispose();
        normals.Dispose();
        uv.Dispose();
        triangles.Dispose();
    }

    private void CreateChunkMesh()
    {
        int vertexStart = 0;
        int triangleStart = 0;

        ProcessMeshDataJob processMeshDataJob = new ProcessMeshDataJob();
        processMeshDataJob.verticesStarters = new NativeArray<int>(meshes.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        processMeshDataJob.trianglesStarters = new NativeArray<int>(meshes.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        for (int i = 0; i < meshes.Count; i++)
        {
            Mesh mesh = meshes[i];
            var vertexCount = mesh.vertexCount;
            var indexCount = (int)mesh.GetIndexCount(0);

            processMeshDataJob.verticesStarters[i] = vertexStart;
            processMeshDataJob.trianglesStarters[i] = triangleStart;
            vertexStart += vertexCount;
            triangleStart += indexCount;
        }

        processMeshDataJob.meshData = Mesh.AcquireReadOnlyMeshData(meshes);
        var outputMeshData = Mesh.AllocateWritableMeshData(1);
        processMeshDataJob.outputMesh = outputMeshData[0];
        processMeshDataJob.outputMesh.SetIndexBufferParams(triangleStart, IndexFormat.UInt32);
        processMeshDataJob.outputMesh.SetVertexBufferParams(
            vertexStart,
            new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2)
        );

        var processMeshDataJobHandle = processMeshDataJob.Schedule(meshes.Count, 12);
        var newMesh = new Mesh();
        newMesh.name = "chunk_x" + location.x + "_y" + location.y + "_z" + location.z;
        var subMesh = new SubMeshDescriptor(0, triangleStart, MeshTopology.Triangles);
        subMesh.firstVertex = 0;
        subMesh.vertexCount = vertexStart;

        processMeshDataJobHandle.Complete();

        processMeshDataJob.outputMesh.subMeshCount = 1;
        processMeshDataJob.outputMesh.SetSubMesh(0, subMesh);

        Mesh.ApplyAndDisposeWritableMeshData(outputMeshData, new[] { newMesh });
        processMeshDataJob.meshData.Dispose();
        processMeshDataJob.verticesStarters.Dispose();
        processMeshDataJob.trianglesStarters.Dispose();

        newMesh.RecalculateBounds();
        meshFilter.mesh = newMesh;
        MeshCollider collider = this.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = meshFilter.mesh;
    }
}
