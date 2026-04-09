using Godot;

namespace HarvestManor.World;

public partial class StorageInteraction : HoverableInteractionArea
{
    [Signal]
    public delegate void StorageRequestedEventHandler();

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.StorageRequested);
        }
    }
}
