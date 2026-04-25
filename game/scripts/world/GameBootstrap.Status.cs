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
            PanelMode.Storage => new PanelVisibility(true, false, true),
            _ => new PanelVisibility(false, false, false)
        };
    }

    public static bool BlocksWorldInteractions(PanelMode mode)
    {
        return mode != PanelMode.None;
    }

    public static PanelMode ResolvePanelModeAfterUnhandledKey(PanelMode currentMode, Key keycode)
    {
        return keycode == Key.Escape && currentMode != PanelMode.None
            ? PanelMode.None
            : currentMode;
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
