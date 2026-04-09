using Godot;

namespace HarvestManor.World;

public static class InteractionHoverStyle
{
    public static Vector2 ResolveScale(bool isHovered)
    {
        return isHovered ? new Vector2(1.05f, 1.05f) : Vector2.One;
    }

    public static Color ResolveColor(Color baseColor, bool isHovered)
    {
        return isHovered ? baseColor.Lightened(0.18f) : baseColor;
    }
}
