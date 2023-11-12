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

    public VoxelState[,,] voxelMap = new VoxelState[voxelData.chunkWidth, voxelData.chunkHeight, voxelData.chunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    WorldSc world;

    private bool _isActive;
    private bool isVoxelMapPopulated = false;

    public Chunk(ChunkCoord _coord, WorldSc _world)
    {
        coord = _coord;
        world = _world;
    }

    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * voxelData.chunkWidth, 0f, coord.z * voxelData.chunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        position = chunkObject.transform.position;

        PopulateVoxelMap();
    }

    void PopulateVoxelMap()
    {
        for (int y = 0; y < voxelData.chunkHeight; y++)
        {
            for (int x = 0; x < voxelData.chunkWidth; x++)
            {
                for (int z = 0; z < voxelData.chunkWidth; z++)
                {
                    voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + position));
                }
            }
        }

        isVoxelMapPopulated = true;

        lock (world.ChunkUpdateThreadLock)
        {
            world.chunksToUpdate.Add(this);
        }
    }

    public void UpdateChunk()
    {

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position -= position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = v.id;
        }

        ClearMeshData();

        CalculateLight();

        for (int y = 0; y < voxelData.chunkHeight; y++)
        {
            for (int x = 0; x < voxelData.chunkWidth; x++)
            {
                for (int z = 0; z < voxelData.chunkWidth; z++)
                {
                    if (world.blockType[voxelMap[x, y, z].id].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }

        world.chunksToDraw.Enqueue(this);
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
                    VoxelState thisVoxel = voxelMap[x, y, z];

                    if (thisVoxel.id > 0 && world.blockType[thisVoxel.id].transparancy < lightRay)
                        lightRay = world.blockType[thisVoxel.id].transparancy;

                    thisVoxel.globalLightPrecent = lightRay;

                    voxelMap[x, y, z] = thisVoxel;

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
                    if (voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPrecent < voxelMap[v.x, v.y, v.z].globalLightPrecent - voxelData.lightFalloff)
                    {
                        voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPrecent = voxelMap[v.x, v.y, v.z].globalLightPrecent - voxelData.lightFalloff;

                        if (voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPrecent > voxelData.lightFalloff)
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

    public bool isEditable
    {
        get
        {
            if (!isVoxelMapPopulated)
                return false;
            else
                return true;
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

        voxelMap[xCheck, yCheck, zCheck].id = newID;

        lock (world.ChunkUpdateThreadLock)
        {
            world.chunksToUpdate.Insert(0, this);
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
                world.chunksToUpdate.Insert(0, world.getChunkFromVector3(thisVoxel + position));
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
            return world.GetVoxelState(pos + position);
        }

        return voxelMap[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);
        return voxelMap[xCheck, yCheck, zCheck];
    }

    void UpdateMeshData(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        byte blockId = voxelMap[x, y, z].id;
        //bool isTransparent = world.blockType[blockId].renderNeighbourFaces;

        for (int p = 0; p < 6; p++)
        {

            VoxelState neighbour = CheckVoxel(pos + voxelData.faceCheck[p]);

            if (neighbour != null && world.blockType[neighbour.id].renderNeighbourFaces)
            {
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 0]]);
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 1]]);
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 2]]);
                vertices.Add(pos + voxelData.voxelVerts[voxelData.voxelTris[p, 3]]);

                for (int i = 0; i < 4; i++)
                    normals.Add(voxelData.faceCheck[p]);

                AddTexture(world.blockType[blockId].GetTextureId(p));

                float lightLevel = neighbour.globalLightPrecent;



                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                if (!world.blockType[neighbour.id].renderNeighbourFaces)
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
