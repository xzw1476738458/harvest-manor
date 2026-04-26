using Godot;

namespace HarvestManor.World;

public partial class ResourceNode : HoverableInteractionArea
{
    [Export]
    public string NodeId { get; set; } = string.Empty;

    [Export]
    public string ItemId { get; set; } = string.Empty;

    private Node2D? _activeRoot;
    private Node2D? _harvestedRoot;

    [Signal]
    public delegate void ResourceNodeInteractedEventHandler(string nodeId);

    public override void _Ready()
    {
        base._Ready();
        BuildVisualLayers();
        InteractionTriggered += () => EmitSignal(SignalName.ResourceNodeInteracted, NodeId);
    }

    private void BuildVisualLayers()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            return;
        }

        var theme = ResourceVisualTheme.Resolve(ItemId);
        _activeRoot = CreateLayerRoot("ActiveLayers", theme.Active);
        _harvestedRoot = CreateLayerRoot("HarvestedLayers", theme.Harvested);
        AddChild(_activeRoot);
        AddChild(_harvestedRoot);
        _harvestedRoot.Visible = false;
    }

    private static Node2D CreateLayerRoot(string name, ResourceVisualTheme.VisualLayer[] layers)
    {
        var root = new Node2D { Name = name };
        foreach (var layer in layers)
        {
            var poly = new Polygon2D
            {
                Polygon = layer.Polygon,
                Color = layer.FillColor,
                ZIndex = layer.ZOffset,
                ZAsRelative = true,
            };
            root.AddChild(poly);
        }
        return root;
    }

    public void Render(bool isHarvested, string itemDisplayName)
    {
        var state = ResolveVisualState(isHarvested, itemDisplayName, ItemId);

        if (_activeRoot is not null)
        {
            _activeRoot.Visible = state.ActiveVisible;
        }

        if (_harvestedRoot is not null)
        {
            _harvestedRoot.Visible = state.HarvestedVisible;
        }

        PromptText = state.PromptText;
        if (PromptLabel is not null)
        {
            PromptLabel.Text = state.PromptText;
        }
    }

    public readonly record struct ResourceVisualState(string PromptText, bool ActiveVisible, bool HarvestedVisible);

    public static ResourceVisualState ResolveVisualState(bool isHarvested, string itemDisplayName, string itemId = "")
    {
        var name = string.IsNullOrWhiteSpace(itemDisplayName) ? "Resource" : itemDisplayName;

        if (isHarvested)
        {
            return new ResourceVisualState($"{name} returns tomorrow", false, true);
        }

        return new ResourceVisualState(ResolveActivePrompt(itemId), true, false);
    }

    private static string ResolveActivePrompt(string itemId) => itemId switch
    {
        "wood" => "[E] chop",
        "stone" => "[E] mine",
        _ => "[E] gather",
    };
}
