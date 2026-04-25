using Godot;

namespace HarvestManor.UI;

public partial class HudController : CanvasLayer
{
    [Export]
    public Label? ClockLabel { get; set; }

    [Export]
    public Label? DayLabel { get; set; }

    [Export]
    public Label? GoldLabel { get; set; }

    [Export]
    public Label? StaminaLabel { get; set; }

    [Export]
    public Label? GrowthLabel { get; set; }

    [Export]
    public Control? TopBar { get; set; }

    public override void _Ready()
    {
        ClockLabel ??= GetNodeOrNull<Label>("TopBar/Margin/Content/StatColumn/ClockBadge/ClockLabel");
        DayLabel ??= GetNodeOrNull<Label>("TopBar/Margin/Content/StatColumn/DayBadge/DayLabel");
        GoldLabel ??= GetNodeOrNull<Label>("TopBar/Margin/Content/StatColumn/GoldBadge/GoldLabel");
        StaminaLabel ??= GetNodeOrNull<Label>("TopBar/Margin/Content/StatColumn/StaminaBadge/StaminaLabel");
        GrowthLabel ??= GetNodeOrNull<Label>("TopBar/Margin/Content/StatColumn/GrowthBadge/GrowthLabel");
        TopBar ??= GetNodeOrNull<Control>("TopBar");
    }

    public void SetClock(string text)
    {
        if (ClockLabel is not null)
        {
            ClockLabel.Text = text;
        }
    }

    public void SetTopBarVisible(bool isVisible)
    {
        if (TopBar is not null)
        {
            TopBar.Visible = isVisible;
        }
    }

    public void SetDay(string text)
    {
        if (DayLabel is not null)
        {
            DayLabel.Text = text;
        }
    }

    public void SetGold(int gold)
    {
        if (GoldLabel is not null)
        {
            GoldLabel.Text = $"Gold: {gold}";
        }
    }

    public void SetStamina(int current, int maximum)
    {
        if (StaminaLabel is not null)
        {
            StaminaLabel.Text = $"Stamina: {current}/{maximum}";
        }
    }

    public void SetGrowth(string text)
    {
        if (GrowthLabel is not null)
        {
            GrowthLabel.Text = text;
        }
    }
}
