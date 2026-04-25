using Godot;

namespace HarvestManor.Core.Time;

public enum TimeOfDayPhase
{
    Dawn,
    Morning,
    Midday,
    Afternoon,
    Dusk,
    Evening,
    Night
}

public static class TimeOfDayController
{
    public const int DayStartMinute = 6 * 60;     // 06:00
    public const int NoonMinute = 12 * 60;        // 12:00
    public const int DuskStartMinute = 17 * 60;   // 17:00
    public const int EveningStartMinute = 18 * 60; // 18:00 (sleep allowed)
    public const int NightStartMinute = 20 * 60;  // 20:00
    public const int DayEndMinute = 26 * 60;      // 02:00 next day (forced sleep)
    public const int SunsetMinute = 19 * 60;      // 19:00 (sun fully below horizon)

    private static readonly Color SkyDawn = new(0.86f, 0.62f, 0.50f);
    private static readonly Color SkyMorning = new(0.66f, 0.84f, 0.96f);
    private static readonly Color SkyMidday = new(0.55f, 0.78f, 0.96f);
    private static readonly Color SkyAfternoon = new(0.74f, 0.78f, 0.92f);
    private static readonly Color SkyDusk = new(0.92f, 0.55f, 0.36f);
    private static readonly Color SkyEvening = new(0.42f, 0.32f, 0.50f);
    private static readonly Color SkyNight = new(0.10f, 0.12f, 0.24f);

    public static TimeOfDayPhase GetPhase(int minuteOfDay) => minuteOfDay switch
    {
        < 7 * 60 => TimeOfDayPhase.Dawn,
        < 11 * 60 => TimeOfDayPhase.Morning,
        < 14 * 60 => TimeOfDayPhase.Midday,
        < 17 * 60 => TimeOfDayPhase.Afternoon,
        < 19 * 60 => TimeOfDayPhase.Dusk,
        < 21 * 60 => TimeOfDayPhase.Evening,
        _ => TimeOfDayPhase.Night,
    };

    public static bool IsSleepAllowed(int minuteOfDay) => minuteOfDay >= EveningStartMinute;

    public static bool IsDayEnded(int minuteOfDay) => minuteOfDay >= DayEndMinute - 1;

    public static string FormatClock(int minuteOfDay)
    {
        var hours = (minuteOfDay / 60) % 24;
        var minutes = minuteOfDay % 60;
        return $"{hours:D2}:{minutes:D2}";
    }

    public static Color GetSkyColor(int minuteOfDay)
    {
        if (minuteOfDay < 7 * 60)
        {
            return Lerp(SkyDawn, SkyMorning, Inverse(minuteOfDay, 6 * 60, 7 * 60));
        }
        if (minuteOfDay < 11 * 60)
        {
            return Lerp(SkyMorning, SkyMidday, Inverse(minuteOfDay, 7 * 60, 11 * 60));
        }
        if (minuteOfDay < 14 * 60)
        {
            return SkyMidday;
        }
        if (minuteOfDay < 17 * 60)
        {
            return Lerp(SkyMidday, SkyAfternoon, Inverse(minuteOfDay, 14 * 60, 17 * 60));
        }
        if (minuteOfDay < 19 * 60)
        {
            return Lerp(SkyAfternoon, SkyDusk, Inverse(minuteOfDay, 17 * 60, 19 * 60));
        }
        if (minuteOfDay < 21 * 60)
        {
            return Lerp(SkyDusk, SkyEvening, Inverse(minuteOfDay, 19 * 60, 21 * 60));
        }
        if (minuteOfDay < 23 * 60)
        {
            return Lerp(SkyEvening, SkyNight, Inverse(minuteOfDay, 21 * 60, 23 * 60));
        }
        return SkyNight;
    }

    public static float GetNightOverlayAlpha(int minuteOfDay)
    {
        if (minuteOfDay < 17 * 60)
        {
            return 0f;
        }
        if (minuteOfDay < 19 * 60)
        {
            return Mathf.Lerp(0f, 0.18f, Inverse(minuteOfDay, 17 * 60, 19 * 60));
        }
        if (minuteOfDay < 21 * 60)
        {
            return Mathf.Lerp(0.18f, 0.45f, Inverse(minuteOfDay, 19 * 60, 21 * 60));
        }
        if (minuteOfDay < 23 * 60)
        {
            return Mathf.Lerp(0.45f, 0.62f, Inverse(minuteOfDay, 21 * 60, 23 * 60));
        }
        return 0.62f;
    }

    public static (Vector2 position, float alpha) GetSunTransform(int minuteOfDay, Vector2 horizonLeft, Vector2 horizonRight, float arcHeight)
    {
        if (minuteOfDay < DayStartMinute - 30 || minuteOfDay > SunsetMinute)
        {
            return (Vector2.Zero, 0f);
        }

        var t = Inverse(minuteOfDay, DayStartMinute, SunsetMinute);
        t = Mathf.Clamp(t, 0f, 1f);

        var x = Mathf.Lerp(horizonLeft.X, horizonRight.X, t);
        var horizonY = Mathf.Lerp(horizonLeft.Y, horizonRight.Y, t);
        var y = horizonY - Mathf.Sin(t * Mathf.Pi) * arcHeight;

        var alpha = 1f;
        if (t < 0.05f)
        {
            alpha = Mathf.Lerp(0.2f, 1f, t / 0.05f);
        }
        else if (t > 0.95f)
        {
            alpha = Mathf.Lerp(1f, 0.1f, (t - 0.95f) / 0.05f);
        }

        return (new Vector2(x, y), alpha);
    }

    public static (Vector2 position, float alpha) GetMoonTransform(int minuteOfDay, Vector2 horizonLeft, Vector2 horizonRight, float arcHeight)
    {
        if (minuteOfDay < SunsetMinute && minuteOfDay > DayStartMinute)
        {
            return (Vector2.Zero, 0f);
        }

        float t;
        if (minuteOfDay >= SunsetMinute)
        {
            t = (minuteOfDay - SunsetMinute) / (float)(DayEndMinute - SunsetMinute);
        }
        else
        {
            return (Vector2.Zero, 0f);
        }

        t = Mathf.Clamp(t, 0f, 1f);
        var x = Mathf.Lerp(horizonLeft.X, horizonRight.X, t);
        var horizonY = Mathf.Lerp(horizonLeft.Y, horizonRight.Y, t);
        var y = horizonY - Mathf.Sin(t * Mathf.Pi) * arcHeight;

        var alpha = 1f;
        if (t < 0.1f)
        {
            alpha = Mathf.Lerp(0f, 1f, t / 0.1f);
        }
        else if (t > 0.9f)
        {
            alpha = Mathf.Lerp(1f, 0.3f, (t - 0.9f) / 0.1f);
        }

        return (new Vector2(x, y), alpha);
    }

    private static float Inverse(int value, int from, int to)
    {
        if (to == from)
        {
            return 0f;
        }
        return (value - from) / (float)(to - from);
    }

    private static Color Lerp(Color a, Color b, float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return new Color(
            Mathf.Lerp(a.R, b.R, t),
            Mathf.Lerp(a.G, b.G, t),
            Mathf.Lerp(a.B, b.B, t),
            Mathf.Lerp(a.A, b.A, t));
    }
}
