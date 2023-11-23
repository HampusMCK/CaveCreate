using System.Collections.Generic;
using UnityEngine;

public static class BlockBehaviour
{
    public static bool Active(VoxelState voxel)
    {
        switch (voxel.id)
        {
            case 5: //Grass
                if ((voxel.neighbours[0] != null && voxel.neighbours[0].id == 3) ||
                    (voxel.neighbours[1] != null && voxel.neighbours[1].id == 3) ||
                    (voxel.neighbours[4] != null && voxel.neighbours[4].id == 3) ||
                    (voxel.neighbours[5] != null && voxel.neighbours[5].id == 3))
                    {
                        return true;
                    }

                    break;
        }

        return false;
    }

    public static void Behave(VoxelState voxel)
    {
        switch (voxel.id)
        {
            case 5: //Grass
            if (voxel.neighbours[2] != null && voxel.neighbours[2].id != 0)
            {
                voxel.chunkData.chunk.RemoveActiveVoxel(voxel);
                voxel.chunkData.ModifyVoxel(voxel.position, 3, 0);
                return;
            }

            List<VoxelState> neighbour = new List<VoxelState>();
            if ((voxel.neighbours[0] != null && voxel.neighbours[0].id == 3)) neighbour.Add(voxel.neighbours[0]);
            if ((voxel.neighbours[1] != null && voxel.neighbours[1].id == 3)) neighbour.Add(voxel.neighbours[1]);
            if ((voxel.neighbours[4] != null && voxel.neighbours[4].id == 3)) neighbour.Add(voxel.neighbours[4]);
            if ((voxel.neighbours[5] != null && voxel.neighbours[5].id == 3)) neighbour.Add(voxel.neighbours[5]);

            if (neighbour.Count == 0)
                return;
            
            int index = Random.Range(0, neighbour.Count);
            neighbour[index].chunkData.ModifyVoxel(neighbour[index].position, 5, 0);
                break;
        }
    }
}