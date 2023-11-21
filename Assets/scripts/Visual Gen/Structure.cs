using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static Queue<VoxelMod> GenerateMajorFlora(int index, Vector3 position, int minTrunkHeight, int maxTrunkHeight)
    {
        switch (index)
        {
            case 0:
                return MakeTree(position, minTrunkHeight, maxTrunkHeight);

            case 1:
                return MakeCacti(position, minTrunkHeight, maxTrunkHeight);
        }

        return new Queue<VoxelMod>();
    }

    public static Queue<VoxelMod> MakeTree(Vector3 position, int minTrunkHeight, int maxTrunkHeight)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 250, 3));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        for (int i = 1; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 6));
        }

        for (int y = -1; y < 4; y++)
        {
            for (int x = -3 + y; x < 4 - y; x++)
            {
                for (int z = -3 + y; z < 4 - y; z++)
                {
                    if (!(y == -1 && x == 0 && z == 0))
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), 4));
                }
            }
        }
        return queue;
    }

    public static Queue<VoxelMod> MakeCacti(Vector3 position, int minTrunkHeight, int maxTrunkHeight)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 23456, 2));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        for (int i = 1; i <= height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 20));

        return queue;
    }
}