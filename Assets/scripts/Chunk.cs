using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    GameObject chunkObject;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;


    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 position;

    private bool _isActive;

    ChunkData chunkData;

    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;
    }

    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = WorldSc.Instance.material;
        materials[1] = WorldSc.Instance.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(WorldSc.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * voxelData.chunkWidth, 0f, coord.z * voxelData.chunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        position = chunkObject.transform.position;

        chunkData = WorldSc.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);

        lock (WorldSc.Instance.ChunkUpdateThreadLock)
            WorldSc.Instance.chunksToUpdate.Add(this);
    }

    public void UpdateChunk()
    {
        ClearMeshData();

        CalculateLight();

        for (int y = 0; y < voxelData.chunkHeight; y++)
        {
            for (int x = 0; x < voxelData.chunkWidth; x++)
            {
                for (int z = 0; z < voxelData.chunkWidth; z++)
                {
                    if (WorldSc.Instance.blockType[chunkData.map[x, y, z].id].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }

        WorldSc.Instance.chunksToDraw.Enqueue(this);
    }

    void CalculateLight()
    {

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();
        for (int x = 0; x < voxelData.chunkWidth; x++)
        {
            for (int z = 0; z < voxelData.chunkWidth; z++)
            {
                float lightRay = 1;
                for (int y = voxelData.chunkHeight - 1; y >= 0; y--)
                {
                    VoxelState thisVoxel = chunkData.map[x, y, z];

                    if (thisVoxel.id > 0 && WorldSc.Instance.blockType[thisVoxel.id].transparancy < lightRay)
                        lightRay = WorldSc.Instance.blockType[thisVoxel.id].transparancy;

                    thisVoxel.globalLightPrecent = lightRay;

                    chunkData.map[x, y, z] = thisVoxel;

                    if (lightRay > voxelData.lightFalloff)
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                }
            }
        }

        while (litVoxels.Count > 0)
        {
            Vector3Int v = litVoxels.Dequeue();
            for (int p = 0; p < 6; p++)
            {
                Vector3 currentVoxel = v + voxelData.faceCheck[p];
                Vector3Int neighbour = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if (IsVoxelInChunk(neighbour.x, neighbour.y, neighbour.z))
                {
                    if (chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPrecent < chunkData.map[v.x, v.y, v.z].globalLightPrecent - voxelData.lightFalloff)
                    {
                        chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPrecent = chunkData.map[v.x, v.y, v.z].globalLightPrecent - voxelData.lightFalloff;

                        if (chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPrecent > voxelData.lightFalloff)
                            litVoxels.Enqueue(neighbour);
                    }
                }
            }
        }
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    public bool isActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            if (chunkObject != null)
                chunkObject.SetActive(value);
        }
    }

    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > voxelData.chunkWidth - 1 || y < 0 || y > voxelData.chunkHeight - 1 || z < 0 || z > voxelData.chunkWidth - 1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void EditVoxel(Vector3 pos, byte newID)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.map[xCheck, yCheck, zCheck].id = newID;
        WorldSc.Instance.worldData.AddToModifiedChunkList(chunkData);

        lock (WorldSc.Instance.ChunkUpdateThreadLock)
        {
            WorldSc.Instance.chunksToUpdate.Insert(0, this);
            UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
        }

    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + voxelData.faceCheck[p];

            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                WorldSc.Instance.chunksToUpdate.Insert(0, WorldSc.Instance.getChunkFromVector3(thisVoxel + position));
            }
        }
    }

    VoxelState CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            return WorldSc.Instance.GetVoxelState(pos + position);
        }

        return chunkData.map[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);
        return chunkData.map[xCheck, yCheck, zCheck];
    }

    void UpdateMeshData(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        byte blockId = chunkData.map[x, y, z].id;
        //bool isTransparent = WorldSc.Instance.blockType[blockId].renderNeighbourFaces;

        for (int p = 0; p < 6; p++)
        {

            VoxelState neighbour = CheckVoxel(pos + voxelData.faceCheck[p]);

            if (neighbour != null && WorldSc.Instance.blockType[neighbour.id].renderNeighbourFaces)
            {
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 0]]);
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 1]]);
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 2]]);
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 3]]);

                for (int i = 0; i < 4; i++)
                    normals.Add(voxelData.faceCheck[p]);

                AddTexture(WorldSc.Instance.blockType[blockId].GetTextureId(p));

                float lightLevel = neighbour.globalLightPrecent;



                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                if (!WorldSc.Instance.blockType[neighbour.id].renderNeighbourFaces)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;
            }
        }
    }

    public void createMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        //mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();
        // mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int TextureId)
    {
        float y = TextureId / voxelData.TextureAtlasSizeInBlocks;
        float x = TextureId - (y * voxelData.TextureAtlasSizeInBlocks);

        x *= voxelData.NormalizeBlockTextureSize;
        y *= voxelData.NormalizeBlockTextureSize;

        y = 1f - y - voxelData.NormalizeBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + voxelData.NormalizeBlockTextureSize));
        uvs.Add(new Vector2(x + voxelData.NormalizeBlockTextureSize, y));
        uvs.Add(new Vector2(x + voxelData.NormalizeBlockTextureSize, y + voxelData.NormalizeBlockTextureSize));
    }
}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / voxelData.chunkWidth;
        z = zCheck / voxelData.chunkWidth;
    }

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public bool Equals(ChunkCoord other)
    {
        if (other == null)
        {
            return false;
        }
        else if (other.x == x && other.z == z)
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
public class VoxelState
{
    public byte id;
    public float globalLightPrecent;

    public VoxelState()
    {
        id = 0;
        globalLightPrecent = 0;
    }

    public VoxelState(byte _id)
    {
        id = _id;
        globalLightPrecent = 0;
    }
}
