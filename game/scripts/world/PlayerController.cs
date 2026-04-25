using System.Linq;
using Godot;

namespace HarvestManor.World;

public partial class PlayerController : CharacterBody2D
{
    private static readonly StringName MoveLeftAction = "move_left";
    private static readonly StringName MoveRightAction = "move_right";
    private static readonly StringName MoveUpAction = "move_up";
    private static readonly StringName MoveDownAction = "move_down";
    private static readonly StringName InteractAction = "interact";

    public const string PlayerGroup = "player";

    [Export]
    public float MoveSpeed { get; set; } = 160.0f;

    [Export]
    public Area2D? Interactor { get; set; }

    public bool MovementEnabled { get; set; } = true;

    public override void _Ready()
    {
        Interactor ??= GetNodeOrNull<Area2D>("Interactor");
        if (Interactor is not null && !Interactor.IsInGroup("player_interactor"))
        {
            Interactor.AddToGroup("player_interactor");
        }

        if (!IsInGroup(PlayerGroup))
        {
            AddToGroup(PlayerGroup);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!MovementEnabled)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        var direction = Input.GetVector(MoveLeftAction, MoveRightAction, MoveUpAction, MoveDownAction);
        Velocity = direction * MoveSpeed;
        MoveAndSlide();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!MovementEnabled)
        {
            return;
        }

        if (!@event.IsActionPressed(InteractAction) || Interactor is null)
        {
            return;
        }

        var bestTarget = FindClosestInteractable();
        if (bestTarget is null)
        {
            return;
        }

        switch (bestTarget)
        {
            case HoverableInteractionArea hoverable:
                hoverable.TryInteractFromKey();
                GetViewport().SetInputAsHandled();
                break;
            case FarmPlotNode plot:
                plot.TryInteractFromKey();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private Node2D? FindClosestInteractable()
    {
        if (Interactor is null)
        {
            return null;
        }

        var overlaps = Interactor.GetOverlappingAreas();
        Node2D? best = null;
        var bestDistanceSq = float.MaxValue;
        var origin = GlobalPosition;

        foreach (var area in overlaps)
        {
            if (area is not Node2D candidate)
            {
                continue;
            }

            if (candidate is not HoverableInteractionArea && candidate is not FarmPlotNode)
            {
                continue;
            }

            var distSq = origin.DistanceSquaredTo(candidate.GlobalPosition);
            if (distSq < bestDistanceSq)
            {
                bestDistanceSq = distSq;
                best = candidate;
            }
        }

        return best;
    }
}
