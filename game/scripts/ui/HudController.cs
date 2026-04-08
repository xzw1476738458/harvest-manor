using Godot;

namespace HarvestManor.UI;

public partial class HudController : CanvasLayer
{
    [Export]
    public Label? DayLabel { get; set; }

    [Export]
    public Label? GoldLabel { get; set; }

    [Export]
    public Label? StaminaLabel { get; set; }

    public override void _Ready()
    {
        DayLabel ??= GetNodeOrNull<Label>("Panel/Rows/DayLabel");
        GoldLabel ??= GetNodeOrNull<Label>("Panel/Rows/GoldLabel");
        StaminaLabel ??= GetNodeOrNull<Label>("Panel/Rows/StaminaLabel");
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
}
