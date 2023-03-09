using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

public struct PerlinSettings
{
    public float heightScale;
    public float scale;
    public int octaves;
    public float heightOffset;
    public float probability;
    public PerlinSettings(float heightScale, float scale, int octaves, float heightOffset, float probability)
    {
        this.heightScale = heightScale;
        this.scale = scale;
        this.octaves = octaves;
        this.heightOffset = heightOffset;
        this.probability = probability;
    }
}

public class World : MonoBehaviour
{
    public static Vector3Int worldDimensions = new Vector3Int(3, 3, 3);
    public static Vector3Int extraWorldDimensions = new Vector3Int(5, 3, 5);
    public static Vector3Int chunkDimensions = new Vector3Int(10, 10, 10);
    public GameObject chunkPrefab;
    public GameObject mainCamera;
    public GameObject fpc;
    public Slider loadingBar;

    public int drawRadius = 8;

    public static PerlinSettings surfaceSettings;
    public PerlinGrapher surface;
    public static PerlinSettings stoneSettings;
    public PerlinGrapher stone;
    public static PerlinSettings diamondTopSettings;
    public PerlinGrapher diamondTop;
    public static PerlinSettings diamondBottomSettings;
    public PerlinGrapher diamondBottom;
    public static PerlinSettings bedrockSettings;
    public PerlinGrapher bedrock;
    public static PerlinSettings cavesSettings;
    public PerlingGrapher3D caves;

    MeshUtils.VoxelTypesEnum selectedBuildBlockType;

    HashSet<Vector3Int> chunkBeingCreatedChecker = new HashSet<Vector3Int>();
    HashSet<Vector3Int> chunkChecker = new HashSet<Vector3Int>();
    HashSet<Vector2Int> chunkColumns = new HashSet<Vector2Int>();
    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    Vector3Int lastBuildPosition;

    void Start()
    {
        loadingBar.maxValue = worldDimensions.x * worldDimensions.z;

        surfaceSettings = new PerlinSettings(surface.heightScale, surface.scale, surface.octaves, surface.heightOffset, surface.probability);
        stoneSettings = new PerlinSettings(stone.heightScale, stone.scale, stone.octaves, stone.heightOffset, stone.probability);
        diamondTopSettings = new PerlinSettings(diamondTop.heightScale, diamondTop.scale, diamondTop.octaves, diamondTop.heightOffset, diamondTop.probability);
        diamondBottomSettings = new PerlinSettings(diamondBottom.heightScale, diamondBottom.scale, diamondBottom.octaves, diamondBottom.heightOffset, diamondBottom.probability);
        cavesSettings = new PerlinSettings(caves.heightScale, caves.scale, caves.octaves, caves.heightOffset, caves.drawCutOff);
        bedrockSettings = new PerlinSettings(bedrock.heightScale, bedrock.scale, bedrock.octaves, bedrock.heightOffset, bedrock.probability);

        StartCoroutine(BuildWorld());
        // StartCoroutine(BuildExtraWorld());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10))
            {
                Vector3 hitBlock = Vector3.zero;
                if (Input.GetMouseButton(0))
                    hitBlock = hit.point - hit.normal / 2;
                else
                    hitBlock = hit.point + hit.normal / 2;
                Chunk chunk = hit.collider.gameObject.GetComponent<Chunk>();
                int hitBlockTreatedX = (int)(Mathf.RoundToInt(hitBlock.x)) - chunk.location.x;
                int hitBlockTreatedY = (int)(Mathf.RoundToInt(hitBlock.y)) - chunk.location.y;
                int hitBlockTreatedZ = (int)(Mathf.RoundToInt(hitBlock.z)) - chunk.location.z;

                Debug.Log("Hit Block Location: " + hitBlockTreatedX + "_" + hitBlockTreatedY + "_" + hitBlockTreatedZ);
                if (Input.GetMouseButton(0))
                    chunk.chunkData[hitBlockTreatedX + chunk.width * (hitBlockTreatedY + chunk.height * hitBlockTreatedZ)] = MeshUtils.VoxelTypesEnum.AIR;
                else
                {
                    int3 neighbour = chunk.location;

                    if (hitBlockTreatedX == chunk.width) neighbour = chunk.location + new int3(chunk.width, 0, 0);
                    if (hitBlockTreatedX == -1) neighbour = chunk.location + new int3(-chunk.width, 0, 0);
                    if (hitBlockTreatedY == chunk.width) neighbour = chunk.location + new int3(0, chunk.height, 0);
                    if (hitBlockTreatedY == -1) neighbour = chunk.location + new int3(0, -chunk.height, 0);
                    if (hitBlockTreatedZ == chunk.width) neighbour = chunk.location + new int3(0, 0, chunk.depth);
                    if (hitBlockTreatedZ == -1) neighbour = chunk.location + new int3(0, 0, -chunk.depth);

                    if (!neighbour.Equals(chunk.location))
                    {
                        chunk = chunks[new Vector3Int(neighbour.x, neighbour.y, neighbour.z)];
                        hitBlockTreatedX = (int)(Mathf.RoundToInt(hitBlock.x)) - chunk.location.x;
                        hitBlockTreatedY = (int)(Mathf.RoundToInt(hitBlock.y)) - chunk.location.y;
                        hitBlockTreatedZ = (int)(Mathf.RoundToInt(hitBlock.z)) - chunk.location.z;
                    }
                    chunk.chunkData[hitBlockTreatedX + chunk.width * (hitBlockTreatedY + chunk.height * hitBlockTreatedZ)] = selectedBuildBlockType;
                }

                StartCoroutine(chunk.RecreateChunk());
            }
        }
    }

    IEnumerator BuildWorld()
    {
        int worldDimensionsZ = worldDimensions.z;
        int worldDimensionsX = worldDimensions.x;
        int chunkDimensionsZ = chunkDimensions.z;
        int chunkDimensionsX = chunkDimensions.x;
        for (int z = 0; z < worldDimensionsZ; z++)
            for (int x = 0; x < worldDimensionsX; x++)
            {
                yield return StartCoroutine(BuildChunkColumn(x * chunkDimensionsX, z * chunkDimensionsZ));
                loadingBar.value++;
            }

        mainCamera.SetActive(false);
        loadingBar.gameObject.SetActive(false);

        int middleXPosition = worldDimensionsX * chunkDimensionsX / 2;
        int middleZPosition = worldDimensionsZ * chunkDimensionsZ / 2;
        int middleYPosition = (int)MeshUtils.fBM(middleXPosition, middleZPosition, surfaceSettings.scale, surfaceSettings.heightScale, surfaceSettings.heightOffset, surfaceSettings.octaves) + 5;
        Transform fpcTransform = fpc.GetComponent<Transform>();
        fpcTransform.position = new Vector3Int(middleXPosition, middleYPosition, middleZPosition);
        fpc.SetActive(true);
        lastBuildPosition = Vector3Int.CeilToInt(fpc.transform.position);
        StartCoroutine(UpdateWorld());
    }


    IEnumerator BuildChunkColumn(int x, int z, bool meshEnabler = true)
    {
        for (int y = 0; y < worldDimensions.y; y++)
        {
            Vector3Int position = new Vector3Int(x, y * chunkDimensions.y, z);
            if (chunkBeingCreatedChecker.Contains(position)) yield return new WaitUntil(() => chunkChecker.Contains(position));
            if (!chunkChecker.Contains(position))
            {
                GameObject chunkInstance = Instantiate(chunkPrefab);
                chunkInstance.name = "Chunk_" + position.x + "_" + position.y + "_" + position.z;
                Chunk chunk = chunkInstance.GetComponent<Chunk>();
                chunkBeingCreatedChecker.Add(position);
                yield return StartCoroutine(chunk.CreateChunk(chunkDimensions, position));
                chunkBeingCreatedChecker.RemoveWhere((Vector3Int checkerPosition) => checkerPosition == position);
                chunkChecker.Add(position);
                chunks.Add(position, chunk);
            }
            chunks[position].meshRenderer.enabled = meshEnabler;
        }
        chunkColumns.Add(new Vector2Int(x, z));
    }

    IEnumerator BuildExtraWorld()
    {
        int zEnd = worldDimensions.z + extraWorldDimensions.z;
        int zStart = worldDimensions.z - 1;
        int xEnd = worldDimensions.x + extraWorldDimensions.x;
        int xStart = worldDimensions.x - 1;

        for (int z = zStart; z < zEnd; z++)
            for (int x = 0; x < xEnd; x++)
                yield return StartCoroutine(BuildChunkColumn(x * chunkDimensions.x, z * chunkDimensions.z, false));
        for (int z = 0; z < zEnd; z++)
            for (int x = xStart; x < xEnd; x++)
                yield return StartCoroutine(BuildChunkColumn(x * chunkDimensions.x, z * chunkDimensions.z, false));
    }

    WaitForSeconds waitForSeconds = new WaitForSeconds(0.5f);
    IEnumerator UpdateWorld()
    {
        while (true)
        {
            bool fpcMovedToAnotherChunkX = (lastBuildPosition - fpc.transform.position).magnitude > chunkDimensions.x;
            bool fpcMovedToAnotherChunkZ = (lastBuildPosition - fpc.transform.position).magnitude > chunkDimensions.z;

            if (fpcMovedToAnotherChunkX || fpcMovedToAnotherChunkZ)
            {
                lastBuildPosition = Vector3Int.CeilToInt(fpc.transform.position);

                int positionX = (int)(fpc.transform.position.x / chunkDimensions.x) * chunkDimensions.x;
                int positionZ = (int)(fpc.transform.position.z / chunkDimensions.z) * chunkDimensions.z;
                List<Vector2Int> currentChunksPositions = BuildCurrentChunksPositions(positionX, positionZ);

                int requiredCreatedColumnsNumberToYieldReturn = 2;
                int createdColumnsSinceLastYeildReturn = 0;
                foreach (Vector2Int chunkPosition in currentChunksPositions)
                {
                    StartCoroutine(BuildChunkColumn(chunkPosition.x, chunkPosition.y));
                    createdColumnsSinceLastYeildReturn++;
                    if (createdColumnsSinceLastYeildReturn == requiredCreatedColumnsNumberToYieldReturn) { yield return null; createdColumnsSinceLastYeildReturn = 0; };
                }
                foreach (Vector2Int chunkColumn in chunkColumns)
                    if (!currentChunksPositions.Contains(chunkColumn)) HideChunkColumn(chunkColumn.x, chunkColumn.y);
            }
            yield return waitForSeconds;
        }
    }

    public void HideChunkColumn(int x, int z)
    {
        for (int y = 0; y < worldDimensions.y; y++)
        {
            Vector3Int position = new Vector3Int(x, y * chunkDimensions.y, z);
            if (chunkChecker.Contains(position) && chunks[position].meshRenderer.enabled)
                chunks[position].meshRenderer.enabled = false;
        }
    }

    List<Vector2Int> BuildCurrentChunksPositions(int x, int z)
    {
        List<Vector2Int> currentChunksPositions = new List<Vector2Int>();
        List<Vector2Int> zChunksPositions = new List<Vector2Int>();

        zChunksPositions.Add(new Vector2Int(x, z));


        for (int i = 0; i <= drawRadius; i++)
        {
            zChunksPositions.Add(new Vector2Int(x, z + (i * chunkDimensions.z)));
            zChunksPositions.Add(new Vector2Int(x, z - (i * chunkDimensions.z)));
        }


        for (int i = 0; i < zChunksPositions.Count; i++)
        {
            Vector2Int zChunkPosition = zChunksPositions[i];

            currentChunksPositions.Add(zChunkPosition);
            for (int j = 0; j <= drawRadius; j++)
            {
                currentChunksPositions.Add(new Vector2Int(zChunkPosition.x + (j * chunkDimensions.x), zChunkPosition.y));
                currentChunksPositions.Add(new Vector2Int(zChunkPosition.x - (j * chunkDimensions.x), zChunkPosition.y));
            }
        }

        return currentChunksPositions;
    }

    public void SetSelectedBuildBlockType(int buildBlockType)
    {
        selectedBuildBlockType = (MeshUtils.VoxelTypesEnum)buildBlockType;
    }
}
