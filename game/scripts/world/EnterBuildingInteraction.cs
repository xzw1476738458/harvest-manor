using Godot;

namespace HarvestManor.World;

public partial class EnterBuildingInteraction : HoverableInteractionArea
{
    [Export]
    public string TargetScene { get; set; } = string.Empty;

    [Signal]
    public delegate void EnterBuildingRequestedEventHandler(string targetScene);

    public override void _Ready()
    {
        base._Ready();
        InteractionTriggered += () => EmitSignal(SignalName.EnterBuildingRequested, TargetScene);
    }
}
