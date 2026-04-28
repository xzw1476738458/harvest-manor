using Godot;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap
{
    private static readonly Vector2 SunArcLeft = new(160, 200);
    private static readonly Vector2 SunArcRight = new(1120, 200);
    private const float SunArcHeight = 130f;

    private readonly struct InteriorWindowConfig
    {
        public required string Prefix { get; init; }
        public required Color DaySky { get; init; }
        public required Color NightSky { get; init; }
        public required Color DayHill { get; init; }
        public required Color NightHill { get; init; }
        public Color? DayTree { get; init; }
        public Color? NightTree { get; init; }
        public string[]? DayOnlyExtras { get; init; }
    }

    private static readonly InteriorWindowConfig CottageWindowConfig = new()
    {
        Prefix = "WindowOutdoor",
        DaySky = new Color(0.55f, 0.78f, 0.94f, 0.95f),
        NightSky = new Color(0.10f, 0.13f, 0.26f, 0.95f),
        DayHill = new Color(0.42f, 0.62f, 0.40f, 0.92f),
        NightHill = new Color(0.18f, 0.26f, 0.22f, 0.92f),
        DayTree = new Color(0.32f, 0.52f, 0.30f),
        NightTree = new Color(0.14f, 0.22f, 0.16f),
    };

    private static readonly InteriorWindowConfig ShopWindowConfig = new()
    {
        Prefix = "Window",
        DaySky = new Color(0.78f, 0.88f, 0.96f, 0.95f),
        NightSky = new Color(0.12f, 0.16f, 0.30f, 0.95f),
        DayHill = new Color(0.46f, 0.62f, 0.36f, 1f),
        NightHill = new Color(0.18f, 0.26f, 0.20f, 1f),
        DayOnlyExtras = new[] { "WindowCloud" },
    };

    private static readonly InteriorWindowConfig BarnWindowConfig = new()
    {
        Prefix = "Window",
        DaySky = new Color(0.78f, 0.88f, 0.96f, 0.95f),
        NightSky = new Color(0.12f, 0.16f, 0.30f, 0.95f),
        DayHill = new Color(0.46f, 0.62f, 0.36f, 1f),
        NightHill = new Color(0.18f, 0.26f, 0.20f, 1f),
        DayOnlyExtras = new[] { "WindowSunRay1", "WindowSunRay2" },
    };

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

        switch (_activeSceneType)
        {
            case CottageSceneType:
                UpdateInteriorWindow(_activeScene, minute, CottageWindowConfig);
                break;
            case ShopInteriorSceneType:
                UpdateInteriorWindow(_activeScene, minute, ShopWindowConfig);
                break;
            case BarnInteriorSceneType:
                UpdateInteriorWindow(_activeScene, minute, BarnWindowConfig);
                break;
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

        var backdropSky = scene.GetNodeOrNull<Polygon2D>("BackdropSky");
        if (backdropSky is not null)
        {
            backdropSky.Color = TimeOfDayController.GetSkyColor(minute);
        }

        var skyGradientTop = scene.GetNodeOrNull<Polygon2D>("SkyGradientTop");
        if (skyGradientTop is not null)
        {
            var dayStrength = 1f - Mathf.Clamp(TimeOfDayController.GetNightOverlayAlpha(minute) / 0.62f, 0f, 1f);
            var skyColor = TimeOfDayController.GetSkyColor(minute);
            skyGradientTop.Color = new Color(
                Mathf.Min(1f, skyColor.R + 0.10f),
                Mathf.Min(1f, skyColor.G + 0.08f),
                Mathf.Min(1f, skyColor.B + 0.04f),
                0.85f * dayStrength);
        }
    }

    private static void UpdateInteriorWindow(Node2D scene, int minute, InteriorWindowConfig config)
    {
        var prefix = config.Prefix;
        var winSun = scene.GetNodeOrNull<Polygon2D>($"{prefix}Sun");
        var winMoon = scene.GetNodeOrNull<Polygon2D>($"{prefix}Moon");
        var winStars = scene.GetNodeOrNull<Node2D>($"{prefix}Stars");
        var winSky = scene.GetNodeOrNull<Polygon2D>($"{prefix}Sky");
        var winHill = scene.GetNodeOrNull<Polygon2D>($"{prefix}Hill");
        var winTree = scene.GetNodeOrNull<Polygon2D>($"{prefix}Tree");

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
            winSky.Color = config.DaySky.Lerp(config.NightSky, nightStrength);
        }
        if (winHill is not null)
        {
            winHill.Color = config.DayHill.Lerp(config.NightHill, nightStrength);
        }
        if (winTree is not null && config.DayTree is { } dayTree && config.NightTree is { } nightTree)
        {
            winTree.Color = dayTree.Lerp(nightTree, nightStrength);
        }
        if (config.DayOnlyExtras is { } extras)
        {
            foreach (var name in extras)
            {
                var extra = scene.GetNodeOrNull<Polygon2D>(name);
                if (extra is null)
                {
                    continue;
                }
                var c = extra.Color;
                c.A = dayStrength;
                extra.Color = c;
            }
        }
    }
}
