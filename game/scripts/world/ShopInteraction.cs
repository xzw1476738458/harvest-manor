using Godot;

namespace HarvestManor.World;

public partial class ShopInteraction : Area2D
{
    [Signal]
    public delegate void ShopRequestedEventHandler();

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.ShopRequested);
        }
    }
}
