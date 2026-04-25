using Godot;

namespace HarvestManor.World;

public partial class RequestBoardInteraction : HoverableInteractionArea
{
    [Signal]
    public delegate void RequestBoardRequestedEventHandler();

    public override void _Ready()
    {
        base._Ready();
        InteractionTriggered += () => EmitSignal(SignalName.RequestBoardRequested);
    }
}
