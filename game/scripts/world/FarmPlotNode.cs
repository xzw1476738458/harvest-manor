using Godot;

namespace HarvestManor.World;

public partial class FarmPlotNode : Area2D
{
    [Export]
    public int GridX { get; set; }

    [Export]
    public int GridY { get; set; }

    [Signal]
    public delegate void PlotInteractedEventHandler(int gridX, int gridY);

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.PlotInteracted, GridX, GridY);
        }
    }
}
