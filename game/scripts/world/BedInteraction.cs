using Godot;

namespace HarvestManor.World;

public partial class BedInteraction : HoverableInteractionArea
{
    [Signal]
    public delegate void DayEndRequestedEventHandler();

    public override void _Ready()
    {
        base._Ready();
        InteractionTriggered += () => EmitSignal(SignalName.DayEndRequested);
    }
}
