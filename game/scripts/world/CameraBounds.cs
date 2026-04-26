using Godot;

namespace HarvestManor.World;

public partial class CameraBounds : Node
{
    [Export]
    public int Left { get; set; }

    [Export]
    public int Top { get; set; }

    [Export]
    public int Right { get; set; } = 1280;

    [Export]
    public int Bottom { get; set; } = 720;
}
