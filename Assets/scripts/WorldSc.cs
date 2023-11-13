using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;

public class WorldSc : MonoBehaviour
{
    public Settings settings;

    [Header("World Generation Values")]
    public BiomeAttributes[] biomes;

    [Range(0, 1)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockType;

    Chunk[,] chunks = new Chunk[voxelData.WorldSizeInChunks, voxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifications = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;

    public GameObject debugScreen;

    Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();

    private void Start()
    {
        Debug.Log("Generating new world using seed " + voxelData.seed);
        // string jsonExport = JsonUtility.ToJson(settings);

        // File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        Random.InitState(voxelData.seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", voxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", voxelData.maxLightLevel);

        if (settings.enableThreading)
        {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }

        SetGlobalLightValue();
        spawnPosition = new Vector3 (voxelData.WorldCenter, voxelData.chunkHeight - 50, voxelData.WorldCenter);
        player.position = spawnPosition;
        GenerateWorld();
        playerLastChunkCoord = getChunkCoordFromVector3(player.position);
    }

    public void SetGlobalLightValue()
    {
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }

    private void Update()
    {
        playerChunkCoord = getChunkCoordFromVector3(player.position);

        //Only Update The Chunks If The Player Has Moved From The Chunk They Were Previously On.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }

        if (chunksToCreate.Count > 0)
            createChunk();

        if (chunksToDraw.Count > 0)
        {
            if (chunksToDraw.Peek().isEditable)
                chunksToDraw.Dequeue().createMesh();
        }

        if (!settings.enableThreading)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
    }

    void GenerateWorld()
    {
        for (int x = (voxelData.WorldSizeInChunks / 2) - settings.viewDistance; x < (voxelData.WorldSizeInChunks / 2) + settings.viewDistance; x++)
        {
            for (int z = (voxelData.WorldSizeInChunks / 2) - settings.viewDistance; z < (voxelData.WorldSizeInChunks / 2) + settings.viewDistance; z++)
            {
                ChunkCoord newChunk = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(newChunk, this);
                chunksToCreate.Add(newChunk);
            }
        }

        player.position = spawnPosition;
        CheckViewDistance();
    }

    void createChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x, c.z].Init();
    }

    void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        lock (ChunkUpdateThreadLock)
        {
            while (!updated && index < chunksToUpdate.Count - 1)
            {
                if (chunksToUpdate[index].isEditable)
                {
                    chunksToUpdate[index].UpdateChunk();
                    if (!activeChunks.Contains(chunksToUpdate[index].coord))
                        activeChunks.Add(chunksToUpdate[index].coord);
                    chunksToUpdate.RemoveAt(index);
                    updated = true;
                }
                else
                    index++;
            }
        }
    }

    void ThreadedUpdate()
    {
        while (true)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }
    }

    private void OnDisable()
    {
        if (settings.enableThreading)
        {
            ChunkUpdateThread.Abort();
        }
    }

    void ApplyModifications()
    {
        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {


                VoxelMod v = queue.Dequeue();

                ChunkCoord c = getChunkCoordFromVector3(v.position);

                if (chunks[c.x, c.z] == null)
                {
                    chunks[c.x, c.z] = new Chunk(c, this);
                    chunksToCreate.Add(c);
                }

                chunks[c.x, c.z].modifications.Enqueue(v);
            }
        }

        applyingModifications = false;
    }

    ChunkCoord getChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / voxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / voxelData.chunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk getChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / voxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / voxelData.chunkWidth);
        return chunks[x, z];
    }

    void CheckViewDistance()
    {
        ChunkCoord coord = getChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                if (isChunkInWorld(thisChunkCoord))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunkCoord, this);
                        chunksToCreate.Add(thisChunkCoord);
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(thisChunkCoord);
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }
        foreach (ChunkCoord c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].isActive = false;
        }
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!isChunkInWorld(thisChunk) || pos.y < 0 || pos.y > voxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return blockType[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos).id].isSolid;

        return blockType[GetVoxel(pos)].isSolid;
    }

    public VoxelState GetVoxelState(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!isChunkInWorld(thisChunk) || pos.y < 0 || pos.y > voxelData.chunkHeight)
            return null;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos);

        return new VoxelState(GetVoxel(pos));
    }

    public bool inUI
    {
        get { return _inUI; }

        set
        {
            _inUI = value;
            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public byte GetVoxel(Vector3 pos) // Get Block To Draw
    {
        /* 0 = Air, 1 = Bedrock, 2 = Stone, 3 = Dirt, 4 = Leaf, 5 = Grass, 6 = Wood, 7 = Sand, 8 = Glass, 9 = Diamond Ore, 10 = Gold Ore, 11 = Iron Ore, 12 = Coal Ore, 13 = Emerald Ore */

        int yPos = Mathf.FloorToInt(pos.y);
        //IMMUTABLE PASS 
        if (!isVoxelInWorld(pos)) //If outside world, return air
        {
            return 0;
        }

        if (yPos == 0) // If bottom return bedrock
        {
            return 1;
        }

        // BIOME SELECTION PASS
        int solidGroundHeight = 42;
        float sumOfHeights = 0;
        int count = 0;
        float strongestWeight = 0;
        int strongestBiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++)
        {
            float weight = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);

            // Keep Track Of Strongest Weight.
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            //Multiply weight by height.
            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;

            //If The Height > 0: Add All Heights.
            if (height > 0)
            {
                sumOfHeights += height;
                count++;
            }
        }

        // Set Biome To The One With The Strongest Weight
        BiomeAttributes biome = biomes[strongestBiomeIndex];

        // Get The Average Height.
        sumOfHeights /= count;

        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);
        // Basic Terrain Pass
        byte voxelValue = 0;

        /* 0 = Air, 1 = Bedrock, 2 = Stone, 3 = Dirt, 4 = Leaf, 5 = Grass, 6 = Wood, 7 = Sand, 8 = Glass, 9 = Diamond Ore, 10 = Gold Ore, 11 = Iron Ore, 12 = Coal Ore, 13 = Emerald Ore */

        if (yPos == terrainHeight)
        {
            voxelValue = biome.surfaceBlock;
        }
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
        {
            voxelValue = biome.subSurfaceBlock;
        }
        else if (yPos > terrainHeight)
        {
            return 0;
        }
        else
        {
            voxelValue = 2;
        }

        //Second Pass
        /* 0 = Air, 1 = Bedrock, 2 = Stone, 3 = Dirt, 4 = Leaf, 5 = Grass, 6 = Wood, 7 = Sand, 8 = Glass, 9 = Diamond Ore, 10 = Gold Ore, 11 = Iron Ore, 12 = Coal Ore, 13 = Emerald Ore */

        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.Lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                    {
                        voxelValue = lode.blockID;
                    }
                }
            }
        }

        //Tree Pass
        /* 0 = Air, 1 = Bedrock, 2 = Stone, 3 = Dirt, 4 = Leaf, 5 = Grass, 6 = Wood, 7 = Sand, 8 = Glass, 9 = Diamond Ore, 10 = Gold Ore, 11 = Iron Ore, 12 = Coal Ore, 13 = Emerald Ore */

        if (yPos == terrainHeight && biome.placeMajorFlora)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraZoneScale) > biome.majorFloraZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold)
                {
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex, pos, biome.minHeight, biome.maxHeight));
                }
            }
        }
        return voxelValue;
    }

    bool isChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < voxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < voxelData.WorldSizeInChunks - 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool isVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < voxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < voxelData.chunkHeight && pos.z >= 0 && pos.z < voxelData.WorldSizeInVoxels)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

[System.Serializable]
public class BlockType
{
    public string name;
    public bool isSolid;
    public bool renderNeighbourFaces;
    public float transparancy;
    public Sprite Icon;
    public int maxStackSize;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public int GetTextureId(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                return 0;
        }
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}

[System.Serializable]
public class Settings
{
    [Header("Game Data")]
    public string version = "0.0.0.01";

    [Header("Performance")]
    public int viewDistance = 5;
    public bool enableThreading = true;

    [Header("Game Play")]
    [Range(0.1f, 10)]
    public float mouseSensitivity = 3;
}