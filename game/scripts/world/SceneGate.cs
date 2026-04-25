using Godot;

namespace HarvestManor.World;

public partial class SceneGate : Area2D
{
    [Export]
    public string TargetScene { get; set; } = string.Empty;

    [Signal]
    public delegate void GateEnteredEventHandler(string targetScene);

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerController)
        {
            return;
        }

        EmitSignal(SignalName.GateEntered, TargetScene);
    }
}
