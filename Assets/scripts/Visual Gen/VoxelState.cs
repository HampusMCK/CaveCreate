[System.Serializable]
public class VoxelState
{
    public byte id;
    [System.NonSerialized] private byte _light;

    [System.NonSerialized] ChunkData chunkData;

    public byte light
    {
        get { return _light; }
        set { _light = value; }
    }

    public VoxelState(byte _id, ChunkData _chunkData)
    {
        id = _id;
        chunkData = _chunkData;
        light = 0;
    }

    public float lightAsFloat
    {
        get { return (float)light * voxelData.unitOfLight; }
    }

    public BlockType properties
    {
        get { return WorldSc.Instance.blockType[id]; }
    }
}