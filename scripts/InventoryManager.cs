using Godot;
using System.Collections.Generic;

public partial class InventoryManager : Control
{
    private Slot currentHoveredSlot;
    private Item currentPickedItem;
    public bool isHoldingItem = false;

    // Grids for slots
    private GridContainer unusedSlotsGrid;
    private GridContainer activeSlotsGrid;



    public override void _Ready()
    {
        // Accessing grids via their node paths, you may want to do this dynamically

        // I made it work on 2 gridcontainers as a proof of concept, 
        // in case you want to have a main grid with all your items and a separate gridcontainer for your "equipped" items

        unusedSlotsGrid = GetNode<GridContainer>("../Main/Inventory/InventorySlots/UnusedSlotsGrid");
        activeSlotsGrid = GetNode<GridContainer>("../Main/Inventory/InventorySlots/ActiveSlotsGrid");


        // Checking if we actually accessed the grids correctly
        if(unusedSlotsGrid == null)
        {
            GD.Print("unusedSlotsGrid is null");
        }
        if(activeSlotsGrid == null)
        {
            GD.Print("actuveSlotsGrid is null");
        }

        // We want to connect the signals from the inventory slots and items one frame after we have successfully connected the grids
        CallDeferred(nameof(ConnectSignals));
    }

    public override void _Process(double delta)
    {
        GD.Print("Current FPS: " + Engine.GetFramesPerSecond());
    }


    // We connect the signals from the slots and items. 
    // Keep in mind, we add slots and items to their respective groups in code, no need to do it in the editor
    private void ConnectSignals()
    {
        // Connect signals from slots
        foreach (var slot in GetTree().GetNodesInGroup("inventory_slots"))
        {
            if (slot is Slot inventorySlot)
            {
                inventorySlot.SlotHovered += OnSlotHovered;
                inventorySlot.SlotExited += OnSlotExited;
                GD.Print("Connected slot signals");
            }
        }

        // Connect signals from items
        foreach (var itemNode in GetTree().GetNodesInGroup("items"))
        {
            if (itemNode is Item item)
            {
                item.ItemPickedUp += OnItemPickedUp;
                item.ItemDropped += OnItemDropped;
                GD.Print("Connected item signals");
            }
        }
    }

    // Retrieve the last hovered slot
    private void OnSlotHovered(Slot slot)
    {
        currentHoveredSlot = slot;
        slot.SelfModulate = slot.hoverColor;
    }

    private void OnSlotExited(Slot slot)
    {
        currentHoveredSlot = null;
        slot.SelfModulate = slot.normalColor;
    }


    // This is important!
    // When we pick up an item, we reparent it temporarily to the "DragLayer"
    // This is because I had some issues if we didn't
    // You can run the game, check the remote view, and if you pick up and hold an item, you can see it being reparented to the DragLayer.
    // When you drop it again, it gets parented to the correct slot
    private void OnItemPickedUp(Item item)
    {
        // Reparent the item to DragLayer
        var dragLayer = GetNode<Control>("../Main/DragLayer");
        if (dragLayer == null)
        {
            GD.PrintErr("DragLayer not found!");
            return;
        }

        // If the item is already in DragLayer, no need to reparent
        if (item.GetParent() == dragLayer)
        {
            GD.Print("Item is already in DragLayer");
            return;
        }

        // Store the item's global position
        Vector2 globalPosition = item.GlobalPosition;

        // Remove from current parent
        Node originalParent = item.GetParent();
        if (originalParent != null)
        {
            // Remove the item from its current parent
            originalParent.RemoveChild(item);

            // If the original parent was a Slot, update its occupyingItem
            if (originalParent is Slot slot)
            {
                slot.RemoveItem();
            }
        }

        // Add to DragLayer
        dragLayer.AddChild(item);

        // Restore global position
        item.GlobalPosition = globalPosition;
    }

private void OnItemDropped(Item item)
{
    if (currentHoveredSlot != null && currentHoveredSlot.CanAcceptItem(item))
    {
        // Remove item from its current parent
        if (item.GetParent() != null)
        {
            item.GetParent().RemoveChild(item);
        }

        // Update old slot's occupyingItem if necessary
        if (item.originalParent is Slot oldSlot && oldSlot != currentHoveredSlot)
        {
            oldSlot.ClearItemReference();
        }

        // Reparent the item to the hovered slot
        currentHoveredSlot.AcceptItem(item);
        item.Position = Vector2.Zero;
    }
    else
    {
        // Return item to its original parent
        if (item.GetParent() != null)
        {
            item.GetParent().RemoveChild(item);
        }

        // Reparent to original parent
        item.originalParent.AddChild(item);
        item.Position = item.originalPosition;

        // If original parent is a Slot, update its occupyingItem
        if (item.originalParent is Slot slot)
        {
            slot.AcceptItem(item);
        }
    }
}

public Slot GetHoveredSlot()
{
    return currentHoveredSlot;
}

}
