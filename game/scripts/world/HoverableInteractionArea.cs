using Godot;

namespace HarvestManor.World;

public abstract partial class HoverableInteractionArea : Area2D
{
    [Export]
    public Polygon2D? HotspotVisual { get; set; }

    private Color _baseHotspotColor = Colors.White;

    public override void _Ready()
    {
        HotspotVisual ??= GetNodeOrNull<Polygon2D>("HotspotVisual");
        if (HotspotVisual is not null)
        {
            _baseHotspotColor = HotspotVisual.Color;
        }

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        ApplyHoverState(isHovered: false);
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
        if (HotspotVisual is not null)
        {
            HotspotVisual.Color = InteractionHoverStyle.ResolveColor(_baseHotspotColor, isHovered);
        }
    }
}
