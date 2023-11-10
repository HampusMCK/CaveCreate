using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreativeInventory : MonoBehaviour
{
    public GameObject slotPrefab;
    WorldSc world;

    List<ItemSlot> slots = new List<ItemSlot>();

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<WorldSc>();

        for (int i = 1; i < world.blockType.Length; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, transform);

            ItemStack stack = new ItemStack((byte)i, Random.Range(1, 65));
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), stack);
            slot.isCreative = true;
        }
    }
}
