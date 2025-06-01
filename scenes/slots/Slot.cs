using Godot;
using System;

public partial class Slot : PanelContainer
{
    [Signal] public delegate void SlotHoveredEventHandler(Slot slot);
    [Signal] public delegate void SlotExitedEventHandler(Slot slot);

    [Export] public bool IsActivatingSlot { get; set; }
    
    private Item occupyingItem;

    [Export] public Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    [Export] public Color hoverColor = new Color(5f, 0.7f, 0.7f);


    public override void _Ready()
    {
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        AddToGroup("inventory_slots");

        if(GetParent<GridContainer>().Name == "ActiveSlotsGrid")
        {
            IsActivatingSlot = true;
            AddToGroup("activeSlots");
        }
        else if(GetParent<GridContainer>().Name == "UnusedSlotsGrid")
        {
            IsActivatingSlot = false;
            AddToGroup("unusedSlots");
        }
        
        ZIndex = 0; // Ensure the Slot is above the Item when not dragging
        MouseFilter = MouseFilterEnum.Stop; // Ensure the slot can receive mouse events

    }



    private void OnMouseEntered()
    {
        EmitSignal(SignalName.SlotHovered, this);
        GD.Print("Entered slot," + " Slot Occupied: " + occupyingItem);
    }

    private void OnMouseExited()
    {
        EmitSignal(SignalName.SlotExited, this);
        GD.Print("Exited Slot");
    }

    public bool CanAcceptItem(Item item)
    {
        return occupyingItem == null;
    }

    public void AcceptItem(Item item)
    {
        // Remove existing item if necessary
        if (occupyingItem != null && occupyingItem != item)
        {
            RemoveItem();
        }

        // Remove item from its current parent
        if (item.GetParent() != null)
        {
            item.GetParent().RemoveChild(item);
        }

        // Add item to this slot
        AddChild(item);
        item.Position = Vector2.Zero;
        occupyingItem = item;
    }


    private void SetItem(Item item)
    {
        if (item.GetParent() != null)
        {
            item.GetParent().RemoveChild(item);
        }

        occupyingItem = item;
        AddChild(item);
        item.Position = Vector2.Zero;
        item.ZIndex = 5;
    }

public void RemoveItem()
{
    if (occupyingItem != null)
    {
        // Check if the occupyingItem's parent is this Slot
        if (occupyingItem.GetParent() == this)
        {
            RemoveChild(occupyingItem);
        }
        occupyingItem = null;
    }
}

    public void ClearItemReference()
    {
        occupyingItem = null;
    }
}