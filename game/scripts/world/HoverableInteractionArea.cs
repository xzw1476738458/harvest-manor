using Godot;

namespace HarvestManor.World;

public abstract partial class HoverableInteractionArea : Area2D
{
    [Export]
    public Polygon2D? HotspotVisual { get; set; }

    [Export]
    public Label? PromptLabel { get; set; }

    [Export]
    public string PromptText { get; set; } = "[E] Interact";

    private Color _baseHotspotColor = Colors.White;
    private bool _isPlayerInRange;
    private bool _isMouseInside;

    [Signal]
    public delegate void InteractionTriggeredEventHandler();

    public bool IsPlayerInRange => _isPlayerInRange;

    public override void _Ready()
    {
        HotspotVisual ??= GetNodeOrNull<Polygon2D>("HotspotVisual");
        PromptLabel ??= GetNodeOrNull<Label>("PromptLabel");

        if (HotspotVisual is not null)
        {
            _baseHotspotColor = HotspotVisual.Color;
        }

        if (PromptLabel is not null)
        {
            PromptLabel.Visible = false;
            PromptLabel.Text = PromptText;
        }

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;
        ApplyHoverState();
    }

    public void SetPlayerInRange(bool inRange)
    {
        if (_isPlayerInRange == inRange)
        {
            return;
        }

        _isPlayerInRange = inRange;
        ApplyHoverState();
    }

    public void TryInteractFromKey()
    {
        if (!_isPlayerInRange)
        {
            return;
        }

        EmitSignal(SignalName.InteractionTriggered);
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            return;
        }

        if (!_isPlayerInRange)
        {
            return;
        }

        EmitSignal(SignalName.InteractionTriggered);
        viewport.SetInputAsHandled();
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area.IsInGroup("player_interactor"))
        {
            SetPlayerInRange(true);
        }
    }

    private void OnAreaExited(Area2D area)
    {
        if (area.IsInGroup("player_interactor"))
        {
            SetPlayerInRange(false);
        }
    }

    private void OnMouseEntered()
    {
        _isMouseInside = true;
        ApplyHoverState();
    }

    private void OnMouseExited()
    {
        _isMouseInside = false;
        ApplyHoverState();
    }

    private void ApplyHoverState()
    {
        var highlight = _isPlayerInRange || _isMouseInside;
        Scale = InteractionHoverStyle.ResolveScale(highlight);
        if (HotspotVisual is not null)
        {
            HotspotVisual.Color = InteractionHoverStyle.ResolveColor(_baseHotspotColor, highlight);
        }

        if (PromptLabel is not null)
        {
            PromptLabel.Visible = _isPlayerInRange;
        }
    }
}
