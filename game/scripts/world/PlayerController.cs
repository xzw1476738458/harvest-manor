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

    public override void _Ready()
    {
        EnsureDefaultMovementActions();
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector(MoveLeftAction, MoveRightAction, MoveUpAction, MoveDownAction);
        Velocity = direction * MoveSpeed;
        MoveAndSlide();
    }

    private static void EnsureDefaultMovementActions()
    {
        EnsureActionHasKey(MoveLeftAction, Key.A);
        EnsureActionHasKey(MoveLeftAction, Key.Left);
        EnsureActionHasKey(MoveRightAction, Key.D);
        EnsureActionHasKey(MoveRightAction, Key.Right);
        EnsureActionHasKey(MoveUpAction, Key.W);
        EnsureActionHasKey(MoveUpAction, Key.Up);
        EnsureActionHasKey(MoveDownAction, Key.S);
        EnsureActionHasKey(MoveDownAction, Key.Down);
    }

    private static void EnsureActionHasKey(StringName action, Key key)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        foreach (var inputEvent in InputMap.ActionGetEvents(action))
        {
            if (inputEvent is InputEventKey { Keycode: var keycode, PhysicalKeycode: var physicalKeycode } &&
                (keycode == key || physicalKeycode == key))
            {
                return;
            }
        }

        var keyEvent = new InputEventKey
        {
            Keycode = key,
            PhysicalKeycode = key
        };

        InputMap.ActionAddEvent(action, keyEvent);
    }
}
