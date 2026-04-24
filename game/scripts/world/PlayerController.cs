using Godot;

namespace HarvestManor.World;

public partial class PlayerController : CharacterBody2D
{
    private static readonly StringName MoveLeftAction = "move_left";
    private static readonly StringName MoveRightAction = "move_right";
    private static readonly StringName MoveUpAction = "move_up";
    private static readonly StringName MoveDownAction = "move_down";

    [Export]
    public float MoveSpeed { get; set; } = 120.0f;

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector(MoveLeftAction, MoveRightAction, MoveUpAction, MoveDownAction);
        Velocity = direction * MoveSpeed;
        MoveAndSlide();
    }
}
