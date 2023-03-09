using UnityEngine;
using Unity.Mathematics;

public class MeshUtils
{
    public enum VoxelFaceSidesEnum { FORWARD, BACK, LEFT, RIGHT, UP, DOWN };

    public enum VoxelFaceTypesEnum
    {
        GRASSTOP, GRASSSIDE, DIRT, WATER, STONE, SAND, GOLD, OBSIDIAN, REDSTONE, DIAMOND, NOCRACK,
        CRACK1, CRACK2, CRACK3, CRACK4, AIR
    };

    public static float2[,] voxelFacesUVs = {
        /*GRASSTOP*/  { new float2(0.125f, 0.375f), new float2(0.1875f,0.375f), new float2(0.125f, 0.4375f), new float2(0.1875f,0.4375f) },
        /*GRASSSIDE*/ { new float2(0.1875f, 0.9375f), new float2(0.25f, 0.9375f), new float2(0.1875f, 1.0f), new float2(0.25f, 1.0f) },
        /*DIRT*/      { new float2(0.125f, 0.9375f), new float2(0.1875f, 0.9375f), new float2(0.125f, 1.0f), new float2(0.1875f, 1.0f) },
        /*WATER*/     { new float2(0.875f,0.125f), new float2(0.9375f,0.125f), new float2(0.875f,0.1875f), new float2(0.9375f,0.1875f) },
        /*STONE*/     { new float2(0, 0.875f), new float2(0.0625f, 0.875f), new float2(0, 0.9375f), new float2(0.0625f, 0.9375f) },
        /*SAND*/      { new float2(0.125f,0.875f), new float2(0.1875f,0.875f), new float2(0.125f,0.9375f), new float2(0.1875f,0.9375f) },
        /*GOLD*/      { new float2(0f,0.8125f), new float2(0.0625f,0.8125f), new float2(0f,0.875f), new float2(0.0625f,0.875f) },
        /*OBSIDIAN*/  { new float2(0.3125f, 0.8125f), new float2(0.375f, 0.8125f), new float2(0.3125f, 0.875f), new float2(0.375f, 0.875f) },
        /*REDSTONE*/  { new float2(0.1875f, 0.75f), new float2(0.25f, 0.75f), new float2(0.1875f, 0.8125f), new float2(0.25f, 0.8125f) },
        /*DIAMOND*/   { new float2(0.125f, 0.75f), new float2(0.1875f, 0.75f), new float2(0.125f, 0.8125f), new float2(0.1875f, 0.8125f) },
        /*NOCRACK*/   { new float2(0.6875f, 0f), new float2(0.75f, 0f), new float2(0.6875f, 0.0625f), new float2(0.75f, 0.0625f) },
        /*CRACK1*/    { new float2(0f,0f), new float2(0.0625f,0f), new float2(0f,0.0625f), new float2(0.0625f,0.0625f) },
        /*CRACK2*/    { new float2(0.0625f,0f), new float2(0.125f,0f), new float2(0.0625f,0.0625f), new float2(0.125f,0.0625f) },
        /*CRACK3*/    { new float2(0.125f,0f), new float2(0.1875f,0f), new float2(0.125f,0.0625f), new float2(0.1875f,0.0625f) },
        /*CRACK4*/    { new float2(0.1875f,0f), new float2(0.25f,0f), new float2(0.1875f,0.0625f), new float2(0.25f,0.0625f) }
    };

    public enum VoxelTypesEnum
    {
        GRASS, DIRT, WATER, STONE, SAND, GOLD, OBSIDIAN, REDSTONE, DIAMOND, AIR
    };
    public enum VoxelInteractionTypesEnum
    {
        EMPTY, SOLID, LIQUID, DECORATIVE
    };
    public static VoxelInteractionTypesEnum[] voxelTypesInteractionTypes = {
        /*GRASS*/     VoxelInteractionTypesEnum.SOLID,
        /*DIRT*/      VoxelInteractionTypesEnum.SOLID,
        /*WATER*/     VoxelInteractionTypesEnum.LIQUID,
        /*STONE*/     VoxelInteractionTypesEnum.SOLID,
        /*SAND*/      VoxelInteractionTypesEnum.SOLID,
        /*GOLD*/      VoxelInteractionTypesEnum.SOLID,
        /*OBSIDIAN*/  VoxelInteractionTypesEnum.SOLID,
        /*REDSTONE*/  VoxelInteractionTypesEnum.SOLID,
        /*DIAMOND*/   VoxelInteractionTypesEnum.SOLID,
        /*AIR*/       VoxelInteractionTypesEnum.EMPTY,
    };

    public static VoxelFaceTypesEnum[,] voxelTypes = {
        /*GRASS*/     { VoxelFaceTypesEnum.GRASSSIDE, VoxelFaceTypesEnum.GRASSSIDE, VoxelFaceTypesEnum.GRASSSIDE, VoxelFaceTypesEnum.GRASSSIDE, VoxelFaceTypesEnum.GRASSTOP, VoxelFaceTypesEnum.DIRT },
        /*DIRT*/      { VoxelFaceTypesEnum.DIRT, VoxelFaceTypesEnum.DIRT, VoxelFaceTypesEnum.DIRT, VoxelFaceTypesEnum.DIRT, VoxelFaceTypesEnum.DIRT, VoxelFaceTypesEnum.DIRT },
        /*WATER*/     { VoxelFaceTypesEnum.WATER, VoxelFaceTypesEnum.WATER, VoxelFaceTypesEnum.WATER, VoxelFaceTypesEnum.WATER, VoxelFaceTypesEnum.WATER, VoxelFaceTypesEnum.WATER },
        /*STONE*/     { VoxelFaceTypesEnum.STONE, VoxelFaceTypesEnum.STONE, VoxelFaceTypesEnum.STONE, VoxelFaceTypesEnum.STONE, VoxelFaceTypesEnum.STONE, VoxelFaceTypesEnum.STONE },
        /*SAND*/      { VoxelFaceTypesEnum.SAND, VoxelFaceTypesEnum.SAND, VoxelFaceTypesEnum.SAND, VoxelFaceTypesEnum.SAND, VoxelFaceTypesEnum.SAND, VoxelFaceTypesEnum.SAND },
        /*GOLD*/      { VoxelFaceTypesEnum.GOLD, VoxelFaceTypesEnum.GOLD, VoxelFaceTypesEnum.GOLD, VoxelFaceTypesEnum.GOLD, VoxelFaceTypesEnum.GOLD, VoxelFaceTypesEnum.GOLD },
        /*OBSIDIAN*/  { VoxelFaceTypesEnum.OBSIDIAN, VoxelFaceTypesEnum.OBSIDIAN, VoxelFaceTypesEnum.OBSIDIAN, VoxelFaceTypesEnum.OBSIDIAN, VoxelFaceTypesEnum.OBSIDIAN, VoxelFaceTypesEnum.OBSIDIAN },
        /*REDSTONE*/  { VoxelFaceTypesEnum.REDSTONE, VoxelFaceTypesEnum.REDSTONE, VoxelFaceTypesEnum.REDSTONE, VoxelFaceTypesEnum.REDSTONE, VoxelFaceTypesEnum.REDSTONE, VoxelFaceTypesEnum.REDSTONE },
        /*DIAMOND*/   { VoxelFaceTypesEnum.DIAMOND, VoxelFaceTypesEnum.DIAMOND, VoxelFaceTypesEnum.DIAMOND, VoxelFaceTypesEnum.DIAMOND, VoxelFaceTypesEnum.DIAMOND, VoxelFaceTypesEnum.DIAMOND },
        /*AIR*/       { VoxelFaceTypesEnum.AIR, VoxelFaceTypesEnum.AIR, VoxelFaceTypesEnum.AIR, VoxelFaceTypesEnum.AIR, VoxelFaceTypesEnum.AIR, VoxelFaceTypesEnum.AIR }
    };

    public static int[] voxelTypesHealth = {
        /*GRASS*/     1,
        /*DIRT*/      1,
        /*WATER*/     1,
        /*STONE*/     2,
        /*SAND*/      1,
        /*GOLD*/      3,
        /*OBSIDIAN*/  8,
        /*REDSTONE*/  3,
        /*DIAMOND*/   4,
        /*AIR*/       -1
    };

    public static float fBM(float x, float z, float scale, float heightScale, float heightOffset, int octaves)
    {
        float total = 0;
        float frequency = 1;
        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * scale * frequency, z * scale * frequency) * heightScale;
            frequency *= 2;
        }
        return total + heightOffset;
    }

    public static float fBM3D(float x, float y, float z, float scale, float heightScale, float heightOffset, int octaves)
    {
        float XY = fBM(x, y, scale, heightScale, heightOffset, octaves);
        float YZ = fBM(y, z, scale, heightScale, heightOffset, octaves);
        float XZ = fBM(x, z, scale, heightScale, heightOffset, octaves);
        float YX = fBM(y, x, scale, heightScale, heightOffset, octaves);
        float ZY = fBM(z, y, scale, heightScale, heightOffset, octaves);
        float ZX = fBM(z, x, scale, heightScale, heightOffset, octaves);

        return (XY + YZ + XZ + YX + ZY + ZX) / 6.0f;
    }
}
