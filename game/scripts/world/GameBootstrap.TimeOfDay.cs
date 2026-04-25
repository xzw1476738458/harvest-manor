using Godot;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap
{
    private static readonly Vector2 SunArcLeft = new(160, 280);
    private static readonly Vector2 SunArcRight = new(1120, 280);
    private const float SunArcHeight = 150f;

    public override void _Process(double delta)
    {
        if (_clock is null)
        {
            return;
        }

        UpdateTimeOfDayVisuals();

        if (_activePanelMode != PanelMode.None)
        {
            return;
        }

        var maxAllowedMinute = TimeOfDayController.DayEndMinute - 1;
        if (_clock.CurrentMinuteOfDay >= maxAllowedMinute)
        {
            return;
        }

        _realTimeAccumulator += delta * MinutesPerRealSecond;
        if (_realTimeAccumulator < 1.0)
        {
            return;
        }

        var minutesToAdd = (int)_realTimeAccumulator;
        _realTimeAccumulator -= minutesToAdd;

        var safeAdvance = Math.Min(minutesToAdd, maxAllowedMinute - _clock.CurrentMinuteOfDay);
        if (safeAdvance <= 0)
        {
            return;
        }

        _clock.AdvanceMinutes(safeAdvance);

        _hud?.SetClock(TimeOfDayController.FormatClock(_clock.CurrentMinuteOfDay));

        if (_clock.CurrentMinuteOfDay >= maxAllowedMinute)
        {
            SetFarmStatus("It's getting very late. Walk home and rest.");
        }
    }

    private void UpdateTimeOfDayVisuals()
    {
        if (_clock is null || _activeScene is null)
        {
            return;
        }

        var minute = _clock.CurrentMinuteOfDay;

        UpdateOutdoorCelestials(_activeScene, minute);

        if (_activeSceneType == CottageSceneType)
        {
            UpdateCottageWindow(_activeScene, minute);
        }
    }

    private static void UpdateOutdoorCelestials(Node2D scene, int minute)
    {
        var sun = scene.GetNodeOrNull<Polygon2D>("Sun");
        var sunGlow = scene.GetNodeOrNull<Polygon2D>("SunGlow");
        var moon = scene.GetNodeOrNull<Polygon2D>("Moon");
        var moonGlow = scene.GetNodeOrNull<Polygon2D>("MoonGlow");
        var stars = scene.GetNodeOrNull<Node2D>("Stars");
        var nightOverlay = scene.GetNodeOrNull<Polygon2D>("NightOverlay");
        var sky = scene.GetNodeOrNull<Polygon2D>("SkyBackdrop");

        var (sunPos, sunAlpha) = TimeOfDayController.GetSunTransform(minute, SunArcLeft, SunArcRight, SunArcHeight);
        if (sun is not null)
        {
            sun.Position = sunPos;
            sun.Modulate = new Color(1, 1, 1, sunAlpha);
        }
        if (sunGlow is not null)
        {
            sunGlow.Position = sunPos;
            sunGlow.Modulate = new Color(1, 1, 1, sunAlpha);
        }

        var (moonPos, moonAlpha) = TimeOfDayController.GetMoonTransform(minute, SunArcLeft, SunArcRight, SunArcHeight);
        if (moon is not null)
        {
            moon.Position = moonPos;
            moon.Modulate = new Color(1, 1, 1, moonAlpha);
        }
        if (moonGlow is not null)
        {
            moonGlow.Position = moonPos;
            moonGlow.Modulate = new Color(1, 1, 1, moonAlpha);
        }

        if (stars is not null)
        {
            var starsAlpha = TimeOfDayController.GetNightOverlayAlpha(minute) / 0.62f;
            stars.Modulate = new Color(1, 1, 1, Mathf.Clamp(starsAlpha, 0f, 1f));
        }

        if (nightOverlay is not null)
        {
            var nightColor = nightOverlay.Color;
            nightColor.A = TimeOfDayController.GetNightOverlayAlpha(minute);
            nightOverlay.Color = nightColor;
        }

        if (sky is not null)
        {
            sky.Color = TimeOfDayController.GetSkyColor(minute);
        }
    }

    private static void UpdateCottageWindow(Node2D scene, int minute)
    {
        var winSun = scene.GetNodeOrNull<Polygon2D>("WindowOutdoorSun");
        var winMoon = scene.GetNodeOrNull<Polygon2D>("WindowOutdoorMoon");
        var winStars = scene.GetNodeOrNull<Node2D>("WindowOutdoorStars");
        var winSky = scene.GetNodeOrNull<Polygon2D>("WindowOutdoorSky");
        var winHill = scene.GetNodeOrNull<Polygon2D>("WindowOutdoorHill");
        var winTree = scene.GetNodeOrNull<Polygon2D>("WindowOutdoorTree");

        var nightStrength = Mathf.Clamp(TimeOfDayController.GetNightOverlayAlpha(minute) / 0.62f, 0f, 1f);
        var dayStrength = 1f - nightStrength;

        if (winSun is not null)
        {
            var c = winSun.Color;
            c.A = dayStrength;
            winSun.Color = c;
        }
        if (winMoon is not null)
        {
            var c = winMoon.Color;
            c.A = nightStrength;
            winMoon.Color = c;
        }
        if (winStars is not null)
        {
            winStars.Modulate = new Color(1, 1, 1, nightStrength);
        }
        if (winSky is not null)
        {
            var daySky = new Color(0.55f, 0.78f, 0.94f, 0.95f);
            var nightSky = new Color(0.10f, 0.13f, 0.26f, 0.95f);
            winSky.Color = daySky.Lerp(nightSky, nightStrength);
        }
        if (winHill is not null)
        {
            var dayHill = new Color(0.42f, 0.62f, 0.40f, 0.92f);
            var nightHill = new Color(0.18f, 0.26f, 0.22f, 0.92f);
            winHill.Color = dayHill.Lerp(nightHill, nightStrength);
        }
        if (winTree is not null)
        {
            var dayTree = new Color(0.32f, 0.52f, 0.30f);
            var nightTree = new Color(0.14f, 0.22f, 0.16f);
            winTree.Color = dayTree.Lerp(nightTree, nightStrength);
        }
    }
}
