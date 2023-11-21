using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    int x;
    int y;
    public Vector2Int position
    {
        get { return new Vector2Int(x, y); }
        set
        {
            x = value.x;
            y = value.y;
        }
    }

    Queue<VoxelState> lightToPropogate = new Queue<VoxelState>();
    public void AddLightForPropogation (VoxelState voxel)
    {
        lightToPropogate.Enqueue(voxel);
    }

    public ChunkData(Vector2Int pos) { position = pos; }
    public ChunkData(int _x, int _y) { x = _x; y = _y; }

    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[voxelData.chunkWidth, voxelData.chunkHeight, voxelData.chunkWidth];

    public void Populate()
    {
        for (int y = 0; y < voxelData.chunkHeight; y++)
        {
            for (int x = 0; x < voxelData.chunkWidth; x++)
            {
                for (int z = 0; z < voxelData.chunkWidth; z++)
                {
                    map[x, y, z] = new VoxelState(WorldSc.Instance.GetVoxel(new Vector3(x + position.x, y, z + position.y)), this);
                }
            }
        }

        Lighting.RecalculateNaturalLight(this);
        WorldSc.Instance.worldData.AddToModifiedChunkList(this);
    }
}
