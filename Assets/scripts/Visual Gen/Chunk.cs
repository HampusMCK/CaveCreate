using System.Collections.Generic;
using UnityEditor.iOS;
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
    List<int> waterTriangles = new List<int>();

    Material[] materials = new Material[3];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 position;

    private bool _isActive;

    ChunkData chunkData;

    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = WorldSc.Instance.material;
        materials[1] = WorldSc.Instance.transparentMaterial;
        materials[2] = WorldSc.Instance.waterMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(WorldSc.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * voxelData.chunkWidth, 0f, coord.z * voxelData.chunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        position = chunkObject.transform.position;

        chunkData = WorldSc.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);
        chunkData.chunk = this;

        WorldSc.Instance.AddChunkToUpdate(this);
    }

    public void TickUpdate()
    {

    }

    public void UpdateChunk()
    {
        ClearMeshData();

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

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        waterTriangles.Clear();
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

    public void EditVoxel(Vector3 pos, byte newID)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.ModifyVoxel(new Vector3Int(xCheck, yCheck, zCheck), newID, WorldSc.Instance._player.orientation);

        UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

        VoxelState voxel = chunkData.map[xCheck, yCheck, zCheck];
        for (int i = 0; i < 6; i++)
        {
            BlockBehaviour.Active(voxel.neighbours[i]);
        }
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + voxelData.faceCheck[p];

            if (!chunkData.IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                WorldSc.Instance.AddChunkToUpdate(WorldSc.Instance.getChunkFromVector3(thisVoxel + position), true);
            }
        }
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

        VoxelState voxel = chunkData.map[x, y, z];

        float rot = 0;
        switch (voxel.orientation)
        {
            case 0:
                rot = 180;
                break;
            case 1:
                rot = 0;
                break;
            case 4:
                rot = 270;
                break;
            default:
                rot = 90;
                break;
        }

        for (int p = 0; p < 6; p++)
        {
            int translatedP = p;

            if (voxel.orientation != 1)
            {
                if (voxel.orientation == 0)
                {
                    if (p == 0) translatedP = 1;
                    else if (p == 1) translatedP = 0;
                    else if (p == 4) translatedP = 5;
                    else if (p == 5) translatedP = 4;
                }
                else if (voxel.orientation == 5)
                {
                    if (p == 0) translatedP = 5;
                    else if (p == 1) translatedP = 4;
                    else if (p == 4) translatedP = 0;
                    else if (p == 5) translatedP = 1;
                }
                else if (voxel.orientation == 4)
                {
                    if (p == 0) translatedP = 4;
                    else if (p == 1) translatedP = 5;
                    else if (p == 4) translatedP = 1;
                    else if (p == 5) translatedP = 0;
                }
            }

            VoxelState neighbour = chunkData.map[x, y, z].neighbours[translatedP];

            if (neighbour != null && neighbour.properties.renderNeighbourFaces && !(voxel.properties.isWater && chunkData.map[x, y + 1, z].properties.isWater))
            {
                float lightLevel = neighbour.lightAsFloat;
                int faceVertCount = 0;

                for (int i = 0; i < voxel.properties.MeshData.faces[p].vertData.Length; i++)
                {
                    VertData vertData = voxel.properties.MeshData.faces[p].GetVertData(i);
                    vertices.Add(pos + vertData.GetRotatedPosition(new Vector3(0, rot, 0)));
                    normals.Add(voxelData.faceCheck[p]);
                    colors.Add(new Color(0, 0, 0, lightLevel));
                    if (voxel.properties.isWater)
                        uvs.Add(voxel.properties.MeshData.faces[p].vertData[i].uv);
                    else
                        AddTexture(voxel.properties.GetTextureId(p), vertData.uv);
                    faceVertCount++;
                }

                if (!voxel.properties.renderNeighbourFaces)
                {
                    for (int i = 0; i < voxel.properties.MeshData.faces[p].triangles.Length; i++)
                        triangles.Add(vertexIndex + voxel.properties.MeshData.faces[p].triangles[i]);
                }
                else
                {
                    if (voxel.properties.isWater)
                    {
                        for (int i = 0; i < voxel.properties.MeshData.faces[p].triangles.Length; i++)
                            waterTriangles.Add(vertexIndex + voxel.properties.MeshData.faces[p].triangles[i]);
                    }
                    else
                    {
                        for (int i = 0; i < voxel.properties.MeshData.faces[p].triangles.Length; i++)
                            transparentTriangles.Add(vertexIndex + voxel.properties.MeshData.faces[p].triangles[i]);
                    }
                }

                vertexIndex += faceVertCount;
            }
        }
    }

    public void createMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 3;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.SetTriangles(waterTriangles.ToArray(), 2);
        //mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();
        // mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int TextureId, Vector2 uv)
    {
        float y = TextureId / voxelData.TextureAtlasSizeInBlocks;
        float x = TextureId - (y * voxelData.TextureAtlasSizeInBlocks);

        x *= voxelData.NormalizeBlockTextureSize;
        y *= voxelData.NormalizeBlockTextureSize;

        y = 1f - y - voxelData.NormalizeBlockTextureSize;

        x += voxelData.NormalizeBlockTextureSize * uv.x;
        y += voxelData.NormalizeBlockTextureSize * uv.y;

        uvs.Add(new Vector2(x, y));
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