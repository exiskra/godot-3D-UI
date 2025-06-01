using Godot;
using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// This script is responsible for handling 3D items in the player Inventory UI
/// </summary>

public partial class Item : Button
{
private InventoryManager inventoryManager;

[Signal] public delegate void ItemPickedUpEventHandler(Item item);
[Signal] public delegate void ItemDroppedEventHandler(Item item);

[Export] private float angleXMax = 15.0f; // Max angle we can rotate in X
[Export] private float angleYMax = 15.0f;   // Max angle we can rotate in Y
[Export] private float rotationLerpSpeed = 15.0f;    // Speed to rotation when hovering
[Export] private float hoverScaleFactor = 1.2f; // How much we scale up when hovering
[Export] private float moveTweenDuration = 0.3f;    // How fast to tween to position
[Export] private float scaleTweenDuration = 0.3f;   // How fast to tween to scale
[Export] private float dragTiltFactor = 0.001f;
[Export] private float dragTiltLerpSpeed = 15.0f;
[Export] private float mouseVelocityThreshold = 5.0f;   // How fast the mouse needs to move to activate item dragging effect

private SubViewport subViewport;
private Node3D modelContainer;
public Vector2 dragOffset;
private Vector3 initialModelPosition;
private Slot currentHoveredSlot;
private Tween currentTween;
private Vector2 targetPosition;
private Vector2 homePosition; // Item returns to this position if no other position is available
private bool isMoving = false;


private Vector3 targetRotation = Vector3.Zero;
private Vector3 currentRotation = Vector3.Zero;
private Vector3 dragTiltRotation = Vector3.Zero;
private Vector2 lastMousePosition;
private Vector2 mouseVelocity;

private bool isDragging = false;

public Node originalParent;
public Vector2 originalPosition;

#region Setup


public override void _Ready()
{
    inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");
    if(inventoryManager == null)
    {
        GD.Print("Failed to connect to root node InventoryManager");
    }

    SetupComponents();
    SetupSignals();

    CallDeferred(nameof(InitializeValues));
    ZIndex = 0;

    AddToGroup("items");
    MouseFilter = MouseFilterEnum.Stop; // Ensure we receive mouse events

}

private void SetupComponents()
{
    subViewport = GetNode<SubViewport>("SubViewport");
    modelContainer = subViewport.GetNode<Node3D>("ModelContainer");
    if (modelContainer == null)
    {
        GD.PrintErr($"ModelContainer not found in ItemSlot! {Name}");
    }
    else
    {
        initialModelPosition = modelContainer.Position;
    }
}

private void SetupSignals()
{
    MouseEntered += OnMouseEntered;
    MouseExited += OnMouseExited;
}

private void InitializeValues()
{
    angleXMax = Mathf.DegToRad(angleXMax);
    angleYMax = Mathf.DegToRad(angleYMax);

    if (GetParent() is Slot homeSlot)
    {
        // If the Item is in a slot
        homePosition = MiddlePointOfSlot(homeSlot);
        Position = Vector2.Zero;
    }
    else if (GetParent() is GridContainer gridContainer)
    {
        // If the Item is in the grid container
        homePosition = GlobalPosition;
    }
    else
    {
        // Fallback if it's neither in a slot nor in the grid container
        homePosition = GlobalPosition;
    }
}


#endregion

#region Processes

public override void _Process(double delta)
{
    UpdateHoverTilt((float)delta);

    if (isDragging)
    {
        FollowMouse((float)delta);
        UpdateDragTiltEffect((float)delta);
    }

    else if(isMoving)
    {
        MoveItem((float)delta);
    }
}

public override void _Input(InputEvent @event)
{
    if (@event is InputEventMouseButton mouseEvent)
    {
        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed && !isDragging && IsHovered())
            {
                OnButtonDown();
            }
            else if (!mouseEvent.Pressed && isDragging)
            {
                OnButtonUp();
            }
        }
    }
    if (@event is InputEventMouseMotion mouseMotion)
    {
        UpdateMouseVelocity(mouseMotion);
        if (IsHovered() && !isDragging)
        {
            UpdateTargetRotation(GetLocalMousePosition());
        }
    }
}


#endregion

#region On Hovering

private void OnMouseEntered()
{
    if (!isDragging && !inventoryManager.isHoldingItem)
    {
        ScaleTo(hoverScaleFactor);
        UpdateTargetRotation(GetLocalMousePosition());
        ZIndex = 10;
    }
}

private void OnMouseExited()
{
    if (!isDragging)
    {
        ScaleTo(1.0f);
        targetRotation = Vector3.Zero;
        ZIndex = 5;
    }
}

private void ScaleTo(float targetScale)
{
    if (currentTween != null && currentTween.IsValid())
    {
        currentTween.Kill();
    }

    currentTween = CreateTween()
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Elastic);

    currentTween.TweenProperty(modelContainer, "scale", Vector3.One * targetScale, scaleTweenDuration);
}


#endregion

#region On Clicking

private void OnButtonDown()
{
    isDragging = true;
    dragOffset = GetLocalMousePosition();

    ScaleTo(hoverScaleFactor);

    lastMousePosition = GetGlobalMousePosition();
    dragTiltRotation = Vector3.Zero;

    // Store the original parent and position
    originalParent = GetParent();
    originalPosition = Position;


    // Adjust ZIndex and MouseFilter
    ZIndex = 100;
    //MouseFilter = MouseFilterEnum.Ignore; // So it doesn't block input to slots

    // Emit signal when item is picked up
    EmitSignal(nameof(ItemPickedUp), this);
}


private void OnButtonUp()
{
    ZIndex = 5;
    isDragging = false;
    dragTiltRotation = Vector3.Zero;
    ResetItem();


    MouseFilter = MouseFilterEnum.Pass;

    // Emit signal when item is dropped
    EmitSignal(nameof(ItemDropped), this);
}

#endregion

private void MoveItem(float delta)
{
    GlobalPosition = GlobalPosition.Lerp(targetPosition, 10 * delta);
    if (GlobalPosition.DistanceTo(targetPosition) < 1)
    {
        GlobalPosition = targetPosition;
        isMoving = false;
    }
}

private void UpdateMouseVelocity(InputEventMouseMotion mouseMotion)
{
    mouseVelocity = mouseMotion.Velocity;
}

private void FollowMouse(float delta)
{
    Vector2 targetPosition = GetGlobalMousePosition() - dragOffset;
    GlobalPosition = GlobalPosition.Lerp(targetPosition, 15 * delta);
}

private void TweenToPosition(Vector2 targetPosition)
{
    if (currentTween != null && currentTween.IsValid())
    {
        currentTween.Kill();
    }

    currentTween = CreateTween()
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Sine);

    currentTween.TweenProperty(this, "global_position", targetPosition, moveTweenDuration);
}

private void ResetItem()
{
    targetRotation = Vector3.Zero;
    ScaleTo(1.0f);
}

#region Helper Methods

private Vector2 MiddlePointOfSlot(Slot slot)
{
    return slot.GlobalPosition + (slot.Size / 2) - (Size / 2);
}

#endregion


#region Visual effects

private void UpdateHoverTilt(float delta)
{
    if (isDragging)
    {
        currentRotation = currentRotation.Lerp(dragTiltRotation, rotationLerpSpeed * delta);
    }
    else
    {
        currentRotation = currentRotation.Lerp(targetRotation, rotationLerpSpeed * delta);
    }
    modelContainer.Rotation = currentRotation;
}

private void UpdateDragTiltEffect(float delta)
{
    if (mouseVelocity.Length() > mouseVelocityThreshold)
    {
        float tiltX = Mathf.Clamp(mouseVelocity.X * dragTiltFactor, -0.3f, 0.3f);
        float tiltY = Mathf.Clamp(-mouseVelocity.Y* dragTiltFactor, -0.3f, 0.3f);
        dragTiltRotation = dragTiltRotation.Lerp(new Vector3(tiltX, tiltY, 0), dragTiltLerpSpeed * delta);
    }
    else
    {
        dragTiltRotation = dragTiltRotation.Lerp(Vector3.Zero, dragTiltLerpSpeed * delta);
    }
}

private void UpdateTargetRotation(Vector2 localMousePosition)
{
    Vector2 size = GetRect().Size;
    float relativeX = (localMousePosition.X / size.X) - 0.5f;
    float relativeY = (localMousePosition.Y / size.Y) - 0.5f;

    float rotX = -relativeY * angleYMax * 2;
    float rotY = -relativeX * angleXMax * 2;

    targetRotation = new Vector3(0, rotY, -rotX);
}

#endregion

}