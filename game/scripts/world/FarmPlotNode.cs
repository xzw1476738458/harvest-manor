using Godot;

namespace HarvestManor.World;

public partial class FarmPlotNode : Area2D
{
    public readonly record struct PlotVisualState(string LabelText, Color FillColor, Color LabelColor);

    [Export]
    public int GridX { get; set; }

    [Export]
    public int GridY { get; set; }

    [Export]
    public Label? PlotLabel { get; set; }

    [Export]
    public Polygon2D? PlotVisual { get; set; }

    [Signal]
    public delegate void PlotInteractedEventHandler(int gridX, int gridY);

    public override void _Ready()
    {
        PlotLabel ??= GetNodeOrNull<Label>("PlotLabel");
        PlotVisual ??= GetNodeOrNull<Polygon2D>("PlotVisual");
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        ApplyHoverState(isHovered: false);
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

        var visualState = ResolveVisualState(plot, cropDisplayName, lockedHint);

        if (PlotLabel is not null)
        {
            PlotLabel.Text = visualState.LabelText;
            PlotLabel.Modulate = visualState.LabelColor;
        }

        if (PlotVisual is not null)
        {
            PlotVisual.Color = visualState.FillColor;
        }

        Modulate = Colors.White;
    }

    public static PlotVisualState ResolveVisualState(
        HarvestManor.Core.Farming.PlotState plot,
        string? cropDisplayName,
        string? lockedHint = null)
    {
        ArgumentNullException.ThrowIfNull(plot);

        var displayName = string.IsNullOrWhiteSpace(cropDisplayName) ? "Planted Crop" : cropDisplayName;

        if (plot.IsLocked)
        {
            return new PlotVisualState(
                $"New Plot\n{lockedHint ?? "Locked"}",
                new Color(0.45f, 0.42f, 0.40f, 0.95f),
                Colors.WhiteSmoke);
        }

        if (!plot.IsTilled)
        {
            return new PlotVisualState(
                "Open Plot\nClick: till",
                new Color(0.42f, 0.62f, 0.31f, 0.95f),
                Colors.WhiteSmoke);
        }

        if (plot.Crop is null)
        {
            return new PlotVisualState(
                "Open Plot\nClick: plant",
                new Color(0.61f, 0.43f, 0.27f, 0.95f),
                Colors.WhiteSmoke);
        }

        if (plot.IsHarvestReady)
        {
            return new PlotVisualState(
                $"{displayName}\nReady: harvest",
                new Color(0.54f, 0.74f, 0.34f, 0.95f),
                new Color(0.12f, 0.18f, 0.10f, 1f));
        }

        if (plot.IsWateredToday)
        {
            return new PlotVisualState(
                $"{displayName}\nWatered today",
                new Color(0.34f, 0.56f, 0.72f, 0.95f),
                Colors.WhiteSmoke);
        }

        return new PlotVisualState(
            $"{displayName}\nClick: water",
            new Color(0.64f, 0.50f, 0.29f, 0.95f),
            Colors.WhiteSmoke);
    }

    private void OnMouseEntered()
    {
        ApplyHoverState(isHovered: true);
    }

    private void OnMouseExited()
    {
        ApplyHoverState(isHovered: false);
    }

    private void ApplyHoverState(bool isHovered)
    {
        Scale = InteractionHoverStyle.ResolveScale(isHovered);
    }
}
