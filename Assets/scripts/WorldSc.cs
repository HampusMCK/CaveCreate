using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class WorldSc : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biome;

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
    List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifications = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;

    public GameObject debugScreen;

    private void Start()
    {
        Random.InitState(seed);
        spawnPosition = new Vector3((voxelData.WorldSizeInChunks * voxelData.chunkWidth) / 2f, voxelData.chunkHeight - 50, (voxelData.WorldSizeInChunks * voxelData.chunkWidth) / 2f);
        player.position = spawnPosition;

        GenerateWorld();
        playerLastChunkCoord = getChunkCoordFromVector3(player.position);
    }

    private void Update()
    {
        playerChunkCoord = getChunkCoordFromVector3(player.position);

        //Only Update The Chunks If The Player Has Moved From The Chunk They Were Previously On.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }

        if (!applyingModifications)
            ApplyModifications();

        if (chunksToCreate.Count > 0)
            createChunk();

        if (chunksToUpdate.Count > 0)
            UpdateChunks();

        if (chunksToDraw.Count > 0)
            lock (chunksToDraw)
            {
                if (chunksToDraw.Peek().isEditable)
                    chunksToDraw.Dequeue().createMesh();
            }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
    }

    void GenerateWorld()
    {
        for (int x = (voxelData.WorldSizeInChunks / 2) - voxelData.ViewDistanceInChunks; x < (voxelData.WorldSizeInChunks / 2) + voxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (voxelData.WorldSizeInChunks / 2) - voxelData.ViewDistanceInChunks; z < (voxelData.WorldSizeInChunks / 2) + voxelData.ViewDistanceInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));
            }
        }

        player.position = spawnPosition;
    }

    void createChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        activeChunks.Add(c);
        chunks[c.x, c.z].Init();
    }

    void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[index].isEditable)
            {
                chunksToUpdate[index].UpdateChunk();
                chunksToUpdate.RemoveAt(index);
                updated = true;
            }
            else
                index++;
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
                    chunks[c.x, c.z] = new Chunk(c, this, true);
                    activeChunks.Add(c);
                }

                chunks[c.x, c.z].modifications.Enqueue(v);

                if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
                    chunksToUpdate.Add(chunks[c.x, c.z]);
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

        for (int x = coord.x - voxelData.ViewDistanceInChunks; x < coord.x + voxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - voxelData.ViewDistanceInChunks; z < coord.z + voxelData.ViewDistanceInChunks; z++)
            {
                if (isChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunksToCreate.Add(new ChunkCoord(x, z));
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(new ChunkCoord(x, z));
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
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
            return blockType[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;

        return blockType[GetVoxel(pos)].isSolid;
    }

    public bool CheckIfVoxelTransparent(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!isChunkInWorld(thisChunk) || pos.y < 0 || pos.y > voxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return blockType[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;

        return blockType[GetVoxel(pos)].isTransparent;
    }

    public bool inUI
    {
        get { return _inUI; }

        set
        {
            _inUI = value;
            if (_inUI)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
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

        // Basic Terrain Pass
        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight;
        byte voxelValue = 0;

        /* 0 = Air, 1 = Bedrock, 2 = Stone, 3 = Dirt, 4 = Leaf, 5 = Grass, 6 = Wood, 7 = Sand, 8 = Glass, 9 = Diamond Ore, 10 = Gold Ore, 11 = Iron Ore, 12 = Coal Ore, 13 = Emerald Ore */

        if (yPos == terrainHeight)
        {
            voxelValue = 5;
        }
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
        {
            voxelValue = 3;
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

        if (yPos == terrainHeight)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    modifications.Enqueue(Structure.MakeTree(pos, biome.minTreeHeight, biome.maxTreeHeight));
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
    public bool isTransparent;
    public Sprite Icon;

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
