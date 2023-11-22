using UnityEngine;

public static class voxelData
{
    public static readonly int chunkWidth = 16;
    public static readonly int chunkHeight = 128;
    public static readonly int WorldSizeInChunks = 100;
    public static readonly int seaLevel = 49;

    // Lighting Values
    public static float minLightLevel = 0.15f;
    public static float maxLightLevel = 0.8f;

    public static float unitOfLight
    {
        get { return 1 / 16; }
    }

    public static float tickLength = 1;

    public static int seed;

    public static int WorldCenter
    {
        get { return (WorldSizeInChunks * chunkWidth) / 2; }
    }

    public static int WorldSizeInVoxels
    {
        get { return WorldSizeInChunks * chunkWidth; }
    }

    public static readonly int TextureAtlasSizeInBlocks = 20;

    public static float NormalizeBlockTextureSize
    {
        get { return 1f / TextureAtlasSizeInBlocks; }
    }

    public static readonly Vector3[] voxelVerts = new Vector3[8]{
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    public static readonly Vector3Int[] faceCheck = new Vector3Int[6]
    {
        new Vector3Int(0, 0, -1), //Back
        new Vector3Int(0, 0, 1), //Front
        new Vector3Int(0, 1, 0), //Top
        new Vector3Int(0, -1, 0), //Bottom
        new Vector3Int(-1, 0, 0), //Left
        new Vector3Int(1, 0, 0) //Right
    };

    public static readonly int[] revFaceCheckIndex = new int[6] { 1, 0, 3, 2, 5, 4 };

    public static readonly int[,] voxelTris = new int[6, 4]
    {
        {0, 3, 1, 2},
        {5, 6, 4, 7},
        {3, 7, 2, 6},
        {1, 5, 0, 4},
        {4, 7, 0, 3},
        {1, 2, 5, 6}
    };

    public static readonly Vector2[] voxelUvs = new Vector2[4]
    {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f),
    };
}