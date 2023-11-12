using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        position.x += (offset + voxelData.seed + 0.1f);
        position.y += (offset + voxelData.seed + 0.1f);

        return Mathf.PerlinNoise(position.x / voxelData.chunkWidth * scale, position.y / voxelData.chunkWidth * scale);
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        float x = (position.x + offset + voxelData.seed + 0.1f) * scale;
        float y = (position.y + offset + voxelData.seed + 0.1f) * scale;
        float z = (position.z + offset + voxelData.seed + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        return (AB + BC + AC + BA + CB + CA) / 6f > threshold ? true : false;
        // if ((AB + BC + AC + BA + CB + CA) / 6f > threshold)
        // {
        //     return true;
        // }
        // else 
        // {
        //     return false;
        // }
    }
}
