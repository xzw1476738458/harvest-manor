using Godot;

namespace HarvestManor.World;

public partial class StorageInteraction : HoverableInteractionArea
{
    [Signal]
    public delegate void StorageRequestedEventHandler();

    public override void _Ready()
    {
        base._Ready();
        InteractionTriggered += () => EmitSignal(SignalName.StorageRequested);
    }
}
