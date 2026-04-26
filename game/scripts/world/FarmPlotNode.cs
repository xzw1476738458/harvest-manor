using Godot;
using HarvestManor.Core.Content;

namespace HarvestManor.World;

public partial class FarmPlotNode : Area2D
{
    public readonly record struct PlotVisualState(string LabelText, Color FillColor, Color LabelColor);

    public readonly record struct CropSpriteVisual(bool Visible, CropVisualTheme.StageVisual StageVisual);

    [Export]
    public int GridX { get; set; }

    [Export]
    public int GridY { get; set; }

    [Export]
    public Label? PlotLabel { get; set; }

    [Export]
    public Polygon2D? PlotVisual { get; set; }

    [Export]
    public Label? PromptLabel { get; set; }

    [Export]
    public Polygon2D? CropSprite { get; set; }

    [Signal]
    public delegate void PlotInteractedEventHandler(int gridX, int gridY);

    private bool _isPlayerInRange;
    private bool _isMouseInside;

    public bool IsPlayerInRange => _isPlayerInRange;

    public override void _Ready()
    {
        PlotLabel ??= GetNodeOrNull<Label>("PlotLabel");
        PlotVisual ??= GetNodeOrNull<Polygon2D>("PlotVisual");
        PromptLabel ??= GetNodeOrNull<Label>("PromptLabel");
        CropSprite ??= GetNodeOrNull<Polygon2D>("CropSprite");

        EnsureCropSprite();

        if (PromptLabel is not null)
        {
            PromptLabel.Visible = false;
        }

        if (CropSprite is not null)
        {
            CropSprite.Visible = false;
        }

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;
        ApplyHoverState();
    }

    private void EnsureCropSprite()
    {
        if (CropSprite is not null)
        {
            return;
        }

        var sprite = new Polygon2D
        {
            Name = "CropSprite",
            Visible = false,
            ZIndex = 10,
        };
        AddChild(sprite);
        CropSprite = sprite;
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

        EmitSignal(SignalName.PlotInteracted, GridX, GridY);
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

        EmitSignal(SignalName.PlotInteracted, GridX, GridY);
        viewport.SetInputAsHandled();
    }

    public void Render(
        HarvestManor.Core.Farming.PlotState plot,
        string? cropDisplayName,
        string? lockedHint = null,
        bool isInActiveTier = true,
        CropDefinition? cropDefinition = null)
    {
        ArgumentNullException.ThrowIfNull(plot);

        var visualState = ResolveVisualState(plot, cropDisplayName, lockedHint, isInActiveTier);

        if (PlotLabel is not null)
        {
            PlotLabel.Text = visualState.LabelText;
            PlotLabel.Modulate = visualState.LabelColor;
        }

        if (PlotVisual is not null)
        {
            PlotVisual.Color = visualState.FillColor;
        }

        ApplyCropSpriteVisual(plot, cropDefinition);

        Modulate = Colors.White;
    }

    private void ApplyCropSpriteVisual(HarvestManor.Core.Farming.PlotState plot, CropDefinition? cropDefinition)
    {
        if (CropSprite is null)
        {
            return;
        }

        var spriteVisual = ResolveCropSpriteVisual(plot, cropDefinition);
        if (!spriteVisual.Visible)
        {
            CropSprite.Visible = false;
            return;
        }

        CropSprite.Polygon = CropVisualTheme.BuildShape(spriteVisual.StageVisual.Sides, spriteVisual.StageVisual.Radius);
        CropSprite.Color = spriteVisual.StageVisual.FillColor;
        CropSprite.Visible = true;
    }

    public static CropSpriteVisual ResolveCropSpriteVisual(
        HarvestManor.Core.Farming.PlotState plot,
        CropDefinition? cropDefinition)
    {
        ArgumentNullException.ThrowIfNull(plot);

        if (plot.IsLocked || !plot.IsTilled || plot.Crop is null || cropDefinition is null)
        {
            return new CropSpriteVisual(false, default);
        }

        var stageIndex = cropDefinition.GetStageIndex(plot.Crop.DaysGrown);
        var stageVisual = CropVisualTheme.GetStageVisual(
            plot.Crop.CropId,
            stageIndex,
            cropDefinition.StageCount,
            plot.IsHarvestReady);

        return new CropSpriteVisual(true, stageVisual);
    }

    public static PlotVisualState ResolveVisualState(
        HarvestManor.Core.Farming.PlotState plot,
        string? cropDisplayName,
        string? lockedHint = null,
        bool isInActiveTier = true)
    {
        ArgumentNullException.ThrowIfNull(plot);

        var displayName = string.IsNullOrWhiteSpace(cropDisplayName) ? "Planted Crop" : cropDisplayName;

        if (plot.IsLocked)
        {
            if (!isInActiveTier)
            {
                return new PlotVisualState(
                    string.Empty,
                    new Color(0.32f, 0.30f, 0.28f, 0.95f),
                    Colors.WhiteSmoke);
            }

            return new PlotVisualState(
                $"New Plot\n{lockedHint ?? "Locked"}",
                new Color(0.45f, 0.42f, 0.40f, 0.95f),
                Colors.WhiteSmoke);
        }

        if (!plot.IsTilled)
        {
            return new PlotVisualState(
                "Open Plot\nClick: till",
                new Color(0.42f, 0.62f, 0.31f, 0.95f),
                Colors.WhiteSmoke);
        }

        if (plot.Crop is null)
        {
            return new PlotVisualState(
                "Open Plot\nClick: plant",
                new Color(0.61f, 0.43f, 0.27f, 0.95f),
                Colors.WhiteSmoke);
        }

        if (plot.IsHarvestReady)
        {
            return new PlotVisualState(
                $"{displayName}\nReady: harvest",
                new Color(0.54f, 0.74f, 0.34f, 0.95f),
                new Color(0.12f, 0.18f, 0.10f, 1f));
        }

        if (plot.IsWateredToday)
        {
            return new PlotVisualState(
                $"{displayName}\nWatered today",
                new Color(0.34f, 0.56f, 0.72f, 0.95f),
                Colors.WhiteSmoke);
        }

        return new PlotVisualState(
            $"{displayName}\nNeeds: water",
            new Color(0.64f, 0.50f, 0.29f, 0.95f),
            Colors.WhiteSmoke);
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
    }
}
