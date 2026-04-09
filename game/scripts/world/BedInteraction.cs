using Godot;

namespace HarvestManor.World;

public partial class BedInteraction : HoverableInteractionArea
{
    [Signal]
    public delegate void DayEndRequestedEventHandler();

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.DayEndRequested);
        }
    }
}
