using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "MineCraftTutorial/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    [Header("Biome Data")]
    public string biomeName;
    public int offset;
    public float scale;

    public int terrainHeight;
    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Major Flora")]
    public int majorFloraIndex;
    public float majorFloraZoneScale = 0.13f;
    [Range(0.1f, 1)]
    public float majorFloraZoneThreshold = 0.6f;
    public float majorFloraPlacementScale = 15;
    [Range(0.1f, 1)]
    public float majorFloraPlacementThreshold = 0.8f;
    public bool placeMajorFlora = true;

    public int maxHeight = 12;
    public int minHeight = 5;


    public Lode[] Lodes;
}

[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}