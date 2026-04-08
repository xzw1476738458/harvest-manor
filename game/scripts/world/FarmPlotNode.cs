using Godot;

namespace HarvestManor.World;

public partial class FarmPlotNode : Area2D
{
    [Export]
    public int GridX { get; set; }

    [Export]
    public int GridY { get; set; }

    [Export]
    public Label? PlotLabel { get; set; }

    [Signal]
    public delegate void PlotInteractedEventHandler(int gridX, int gridY);

    public override void _Ready()
    {
        PlotLabel ??= GetNodeOrNull<Label>("PlotLabel");
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.PlotInteracted, GridX, GridY);
        }
    }

    public void Render(HarvestManor.Core.Farming.PlotState plot, string? cropDisplayName, string? lockedHint = null)
    {
        ArgumentNullException.ThrowIfNull(plot);

        if (PlotLabel is null)
        {
            return;
        }

        var text = plot.IsLocked
            ? $"Plot ({GridX},{GridY})\n{lockedHint ?? "Locked"}"
            : !plot.IsTilled
                ? $"Plot ({GridX},{GridY})\nClick: till"
                : plot.Crop is null
                    ? $"Plot ({GridX},{GridY})\nClick: plant"
                    : plot.IsHarvestReady
                        ? $"{cropDisplayName}\nReady: harvest"
                        : plot.IsWateredToday
                            ? $"{cropDisplayName}\nWatered today"
                            : $"{cropDisplayName}\nClick: water";

        PlotLabel.Text = text;
        Modulate = plot.IsLocked
            ? new Color(0.55f, 0.55f, 0.55f)
            : plot.IsHarvestReady
                ? new Color(0.72f, 1f, 0.72f)
                : plot.Crop is null
                    ? new Color(0.89f, 0.77f, 0.58f)
                    : plot.IsWateredToday
                        ? new Color(0.58f, 0.78f, 1f)
                        : Colors.White;
    }
}
