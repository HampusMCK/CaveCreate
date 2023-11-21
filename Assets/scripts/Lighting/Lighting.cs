using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Lighting
{
    public static void RecalculateNaturalLight(ChunkData chunkData)
    {
        for (int x = 0; x < voxelData.chunkWidth; x++)
            for (int z = 0; z < voxelData.chunkWidth; z++)
            {
                {
                    CastNaturalLight(chunkData, x, z, voxelData.chunkHeight - 1);
                }
            }
    }

    // Propogates natural light straight down from at the given x,z coord starting from the startY value.
    public static void CastNaturalLight(ChunkData chunkData, int x, int z, int startY)
    {
        //Check so we dont start above world
        if (startY > voxelData.chunkHeight - 1)
        {
            startY = voxelData.chunkHeight - 1;
        }

        bool obstructed = false;

        for (int y = startY; y > -1; y--)
        {
            VoxelState voxel = chunkData.map[x, y, z];

            if (obstructed)
            {
                voxel.light = 0;
            }
            else if (voxel.properties.opacity > 0)
            {
                voxel.light = 0;
                obstructed = true;
            }
            else
                voxel.light = 15;
        }
    }
}
