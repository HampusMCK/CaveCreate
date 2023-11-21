using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelState
{
    public byte id;
    public int orientation;
    [System.NonSerialized] private byte _light;

    [System.NonSerialized] public ChunkData chunkData;

    [System.NonSerialized] public VoxelNeighbours neighbours;

    [System.NonSerialized] public Vector3Int position;

    public byte light
    {
        get { return _light; }
        set
        {
            if (value != _light)
            {
                byte oldLightValue = _light;
                byte oldCastValue = castLight;

                _light = value;

                if (_light < oldLightValue)
                {
                    List<int> neighboursToDarken = new List<int>();

                    for (int p = 0; p < 6; p++)
                    {
                        if (neighbours[p] != null)
                        {
                            if (neighbours[p].light <= oldCastValue)
                                neighboursToDarken.Add(p);
                            else
                            {
                                neighbours[p].PropogateLight();
                            }
                        }
                    }

                    foreach (int i in neighboursToDarken)
                    {
                        neighbours[i].light = 0;
                    }

                    if (chunkData.chunk != null)
                        WorldSc.Instance.AddChunkToUpdate(chunkData.chunk);
                }
                else if (_light > 1)
                    PropogateLight();
            }
        }
    }

    public VoxelState(byte _id, ChunkData _chunkData, Vector3Int _position)
    {
        id = _id;
        orientation = 1;
        chunkData = _chunkData;
        neighbours = new VoxelNeighbours(this);
        position = _position;
        light = 0;
    }

    public Vector3Int globalPosition
    {
        get
        {
            return new Vector3Int(position.x + chunkData.position.x, position.y, position.z + chunkData.position.y);
        }
    }

    public float lightAsFloat
    {
        get { return (float)light * voxelData.unitOfLight; }
    }

    public byte castLight
    {
        get
        {
            /*
            Get the amount of Light this voxel is spreading, Bytes rap around so we
            need to do this with an int so we can make sure it doesn't get below 0.
            */
            int lightLevel = _light - properties.opacity - 1;
            if (lightLevel < 0)
                lightLevel = 0;
            return (byte)lightLevel;
        }
    }

    public void PropogateLight()
    {
        //If we somehow added a null voxel or one that isn't bright enough to propogate, return.
        if (light < 2)
            return;

        //Loop through each neighbour of this voxel.
        for (int p = 0; p < 6; p++)
        {
            //we can only propogate to voxels that exist so check there is one first
            if (neighbours[p] != null)
            {
                /*
                We can ONLY propogate light in one direction (lighter to darker). If
                we work in both directions, we will get recurssive loops.
                So any neighbours who are not darker than this voxel's lightCast value,
                we leave alone
                */
                if (neighbours[p].light < castLight)
                    neighbours[p].light = castLight;
            }

            if (chunkData.chunk != null)
                WorldSc.Instance.AddChunkToUpdate(chunkData.chunk);
        }
    }

    public BlockType properties
    {
        get { return WorldSc.Instance.blockType[id]; }
    }
}

public class VoxelNeighbours
{
    public readonly VoxelState parent;
    public VoxelNeighbours(VoxelState _parent)
    {
        parent = _parent;
    }

    private VoxelState[] _neighbours = new VoxelState[6];

    public int Length { get { return _neighbours.Length; } }

    public VoxelState this[int index]
    {
        get
        {
            if (_neighbours[index] == null)
            {
                _neighbours[index] = WorldSc.Instance.worldData.GetVoxel(parent.globalPosition + voxelData.faceCheck[index]);
                ReturnNeighbour(index);
            }

            return _neighbours[index];
        }
        set
        {
            _neighbours[index] = value;
            ReturnNeighbour(index);
        }
    }

    void ReturnNeighbour(int index)
    {
        if (_neighbours[index] == null)
            return;

        if (_neighbours[index].neighbours[voxelData.revFaceCheckIndex[index]] != parent)
            _neighbours[index].neighbours[voxelData.revFaceCheckIndex[index]] = parent;
    }
}