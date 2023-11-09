using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toolbar : MonoBehaviour
{
    WorldSc world;
    public Player player;

    public RectTransform highlight;
    public ItemSlot[] itemSlots;

    int slotIndex = 0;

    private void Start() 
    {
        world = GameObject.Find("World").GetComponent<WorldSc>();

        foreach (ItemSlot slot in itemSlots)
        {
         slot.Icon.sprite = world.blockType[slot.itemID].Icon; 
         slot.Icon.enabled = true;  
        }
    }

    private void Update() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (scroll > 0)
                slotIndex--;
            else
                slotIndex++;

            if (slotIndex > itemSlots.Length - 1)
                slotIndex = 0;
            if (slotIndex < 0)
                slotIndex = itemSlots.Length - 1;

            highlight.position = itemSlots[slotIndex].Icon.transform.position;
            player.selectedBlockIndex = itemSlots[slotIndex].itemID;
        }
    }
}

[System.Serializable]
public class ItemSlot
{
    public byte itemID;
    public Image Icon;
}
