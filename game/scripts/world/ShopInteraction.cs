using Godot;

namespace HarvestManor.World;

public partial class ShopInteraction : HoverableInteractionArea
{
    [Signal]
    public delegate void ShopRequestedEventHandler();

    public override void _Ready()
    {
        base._Ready();
        InteractionTriggered += () => EmitSignal(SignalName.ShopRequested);
    }
}
