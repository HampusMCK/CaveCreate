using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] public UIItemSlot cursorSlot = null;
    public ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;

    public UIItemSlot pastClick;

    WorldSc world;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<WorldSc>();
        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        if (!world.inUI)
            return;

        cursorSlot.transform.position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            HandleSlotClick(CheckForSlot());
        }
        if (Input.GetMouseButtonDown(1))
            cursorItemSlot.EmptySlot();
    }

    public void returnClicked()
    {
        if (cursorSlot.HasItem)
            pastClick.itemSlot.InsetStack(cursorItemSlot.TakeAll());
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        pastClick = clickedSlot;
        if (clickedSlot == null)
            return;

        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;

        if (clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsetStack(clickedSlot.itemSlot.stack);
        }

        if (!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsetStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsetStack(cursorItemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack giveCursor = clickedSlot.itemSlot.TakeAll();
                ItemStack giveSlot = cursorItemSlot.TakeAll();

                cursorItemSlot.InsetStack(giveCursor);
                clickedSlot.itemSlot.InsetStack(giveSlot);
            }
            else
            {
                if (clickedSlot.itemSlot.stack.amount + cursorItemSlot.stack.amount <= 64)
                {
                    clickedSlot.itemSlot.stack.amount += cursorItemSlot.stack.amount;
                    cursorItemSlot.EmptySlot();
                    cursorSlot.UpdateSlot();
                    clickedSlot.UpdateSlot();
                }
                else if (clickedSlot.itemSlot.stack.amount + cursorItemSlot.stack.amount > 64)
                {
                    int diff = 64 - clickedSlot.itemSlot.stack.amount;
                    clickedSlot.itemSlot.stack.amount += diff;
                    cursorItemSlot.stack.amount -= diff;
                    cursorSlot.UpdateSlot();
                    clickedSlot.UpdateSlot();
                }
            }
        }
    }

    private UIItemSlot CheckForSlot()
    {
        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.tag == "UIItemSlot")
                return result.gameObject.GetComponent<UIItemSlot>();
        }

        return null;
    }
}
