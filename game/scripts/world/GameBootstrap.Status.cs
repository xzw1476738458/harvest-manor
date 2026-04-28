using Godot;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap
{
    public static PanelVisibility ResolvePanelVisibility(PanelMode mode)
    {
        return mode switch
        {
            PanelMode.Shop => new PanelVisibility(false, true, false),
            PanelMode.Storage => new PanelVisibility(false, false, true),
            PanelMode.Inventory => new PanelVisibility(true, false, false),
            _ => new PanelVisibility(false, false, false)
        };
    }

    public static bool BlocksWorldInteractions(PanelMode mode)
    {
        return mode != PanelMode.None;
    }

    /// <summary>
    /// When the inventory panel is open the player is reading their own ledger,
    /// so hovering the world should not steal focus from the panel by rewriting
    /// the field-notes label. Other panels (shop/storage) still surface
    /// "Close the X panel..." hover hints because the player typically interacts
    /// with their UI buttons rather than the world while those panels are up.
    /// </summary>
    public static bool ShouldSilenceHoverPreview(PanelMode mode)
    {
        return mode == PanelMode.Inventory;
    }

    public static PanelMode ResolvePanelModeAfterUnhandledKey(PanelMode currentMode, Key keycode)
    {
        if (keycode == Key.Escape && currentMode != PanelMode.None)
        {
            return PanelMode.None;
        }

        if (keycode == Key.Tab)
        {
            return currentMode switch
            {
                PanelMode.None => PanelMode.Inventory,
                PanelMode.Inventory => PanelMode.None,
                _ => currentMode
            };
        }

        return currentMode;
    }

    public static bool CanHandlePanelInteractionRequest(PanelMode currentMode, PanelMode requestedMode)
    {
        return requestedMode != PanelMode.None
            && (currentMode == PanelMode.None || currentMode == requestedMode);
    }

    public static PanelMode ResolvePanelModeAfterInteractionRequest(PanelMode currentMode, PanelMode requestedMode)
    {
        return currentMode == requestedMode
            ? PanelMode.None
            : requestedMode;
    }

    public static bool CanTriggerQuickExpansionShortcut(PanelMode currentMode, Key keycode)
    {
        return keycode == Key.F7 && !BlocksWorldInteractions(currentMode);
    }
}
