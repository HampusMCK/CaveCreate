using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldData
{
   public string worldName = "Testing";
   public int seed;

   [System.NonSerialized]
   public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

   [System.NonSerialized]
   public List<ChunkData> modifiedChunks = new List<ChunkData>();

   public void AddToModifiedChunkList(ChunkData chunk)
   {
      if (!modifiedChunks.Contains(chunk))
         modifiedChunks.Add(chunk);
   }

   public WorldData(string _worldname, int _seed)
   {
      worldName = _worldname;
      seed = _seed;
   }

   public WorldData(WorldData wD)
   {
      worldName = wD.worldName;
      seed = wD.seed;
   }

   public ChunkData RequestChunk(Vector2Int coord, bool create)
   {
      ChunkData c;

      lock (WorldSc.Instance.ChunkListThreadlock)
      {
         if (chunks.ContainsKey(coord))
            c = chunks[coord];
         else if (!create)
            c = null;
         else
         {
            LoadChunk(coord);
            c = chunks[coord];
         }
      }

      return c;
   }

   public void LoadChunk(Vector2Int coord)
   {
      if (chunks.ContainsKey(coord))
         return;

      ChunkData chunk = SaveSystem.LoadChunk(worldName, coord);
      if (chunk != null)
      {
         chunks.Add(coord, chunk);
         return;
      }

      chunks.Add(coord, new ChunkData(coord));
      chunks[coord].Populate();
   }

   bool isVoxelInWorld(Vector3 pos)
   {
      if (pos.x >= 0 && pos.x < voxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < voxelData.chunkHeight && pos.z >= 0 && pos.z < voxelData.WorldSizeInVoxels)
         return true;
      else
         return false;
   }

   public void SetVoxel(Vector3 pos, byte value, int direction)
   {
      //Voxel outside world ? return:Continue
      if (!isVoxelInWorld(pos))
         return;

      //Find coord value of voxels chunk
      int x = Mathf.FloorToInt(pos.x / voxelData.chunkWidth);
      int z = Mathf.FloorToInt(pos.z / voxelData.chunkWidth);

      //Get pos of chunk by reversing
      x *= voxelData.chunkWidth;
      z *= voxelData.chunkWidth;

      //Does chunk Exist ? ignore:Create new
      ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

      //Create pos of voxel in chunk
      Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

      //set Voxel
      chunk.ModifyVoxel(voxel, value, direction);
   }

   public VoxelState GetVoxel(Vector3 pos)
   {
      //Voxel outside world ? return:Continue
      if (!isVoxelInWorld(pos))
         return null;

      //Find coord value of voxels chunk
      int x = Mathf.FloorToInt(pos.x / voxelData.chunkWidth);
      int z = Mathf.FloorToInt(pos.z / voxelData.chunkWidth);

      //Get pos of chunk by reversing
      x *= voxelData.chunkWidth;
      z *= voxelData.chunkWidth;

      //Does chunk Exist ? ignore:Create new
      ChunkData chunk = RequestChunk(new Vector2Int(x, z), false);

      if (chunk == null)
         return null;

      //Create pos of voxel in chunk
      Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

      //set Voxel
      return chunk.map[voxel.x, voxel.y, voxel.z];
   }
}
