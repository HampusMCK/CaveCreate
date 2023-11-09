using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "MineCraftTutorial/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;
    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;

    [Header("Trees")]
    public float treeZoneScale = 0.13f;
    [Range(0.1f, 1)]
    public float treeZoneThreshold = 0.6f;
    public float treePlacementScale = 15;
    [Range(0.1f, 1)]
    public float treePlacementThreshold = 0.8f;

    public int maxTreeHeight = 12;
    public int minTreeHeight = 5;


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