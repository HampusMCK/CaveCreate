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
                break;
        }
    }
}