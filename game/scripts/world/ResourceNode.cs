using Godot;

namespace HarvestManor.World;

public partial class ResourceNode : HoverableInteractionArea
{
    [Export]
    public string NodeId { get; set; } = string.Empty;

    [Export]
    public string ItemId { get; set; } = string.Empty;

    [Export]
    public Polygon2D? ResourceVisual { get; set; }

    [Export]
    public Label? ResourceLabel { get; set; }

    [Signal]
    public delegate void ResourceNodeInteractedEventHandler(string nodeId);

    public override void _Ready()
    {
        base._Ready();
        ResourceVisual ??= GetNodeOrNull<Polygon2D>("ResourceVisual");
        ResourceLabel ??= GetNodeOrNull<Label>("ResourceLabel");
        ApplyResourceTheme();
        InteractionTriggered += () => EmitSignal(SignalName.ResourceNodeInteracted, NodeId);
    }

    private void ApplyResourceTheme()
    {
        if (ResourceVisual is null || string.IsNullOrWhiteSpace(ItemId))
        {
            return;
        }

        var theme = ResourceVisualTheme.Resolve(ItemId);
        ResourceVisual.Polygon = theme.Polygon;
        ResourceVisual.Color = theme.FillColor;
    }

    public void Render(bool isHarvested, string itemDisplayName)
    {
        var visual = ResolveVisualState(isHarvested, itemDisplayName);

        if (ResourceLabel is not null)
        {
            ResourceLabel.Text = visual.LabelText;
            ResourceLabel.Modulate = visual.LabelColor;
        }

        if (ResourceVisual is not null)
        {
            ResourceVisual.Modulate = visual.VisualModulate;
        }
    }

    public readonly record struct ResourceVisualState(string LabelText, Color LabelColor, Color VisualModulate);

    public static ResourceVisualState ResolveVisualState(bool isHarvested, string itemDisplayName)
    {
        var name = string.IsNullOrWhiteSpace(itemDisplayName) ? "Resource" : itemDisplayName;

        if (isHarvested)
        {
            return new ResourceVisualState(
                $"{name}\nReturns tomorrow",
                new Color(0.62f, 0.58f, 0.50f, 1f),
                new Color(1f, 1f, 1f, 0.25f));
        }

        return new ResourceVisualState(
            $"{name}\nClick: gather",
            Colors.WhiteSmoke,
            Colors.White);
    }
}
