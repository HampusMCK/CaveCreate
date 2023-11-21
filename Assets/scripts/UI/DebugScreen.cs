using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    WorldSc world;
    TMP_Text text;
    float framRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    void Start()
    {
        world = GameObject.Find("World").GetComponent<WorldSc>();
        text = GetComponent<TMP_Text>();

        halfWorldSizeInVoxels = voxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = voxelData.WorldSizeInChunks / 2;
    }

    void Update()
    {
        string debugText = "Hampus' CaveCreate";
        debugText += "\n";
        debugText += framRate + " fps";
        debugText += "\n\n";
        debugText += "XYZ: " + (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + " / " + Mathf.FloorToInt(world.player.transform.position.y) + " / " + (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels);
        debugText += "\n";
        debugText += "Chunk: " + (world.playerChunkCoord.x - halfWorldSizeInChunks) + " / " + (world.playerChunkCoord.z - halfWorldSizeInChunks);

        string direction = "";
        switch (world._player.orientation)
        {
            case 0:
                direction = "South";
                break;
            case 5:
                direction = "East";
                break;
            case 1:
                direction = "North";
                break;
            default:
                direction = "West";
                break;
        }

        debugText += "\n";
        debugText += "Facing: " + direction;

        text.text = debugText;

        if (timer > 1)
        {
            framRate = (int)(1 / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }
}
