using Godot;

namespace HarvestManor.World;

public static class InteractionHoverStyle
{
    public static Vector2 ResolveScale(bool isHovered) => isHovered ? new Vector2(1.05f, 1.05f) : Vector2.One;

    public static Color ResolveColor(Color baseColor, bool isHovered)
    {
        if (!isHovered)
        {
            return baseColor;
        }

        return new Color(
            Mathf.Min(baseColor.R * 1.15f, 1f),
            Mathf.Min(baseColor.G * 1.15f, 1f),
            Mathf.Min(baseColor.B * 1.15f, 1f),
            baseColor.A);
    }
}
