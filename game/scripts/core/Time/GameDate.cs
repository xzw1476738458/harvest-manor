namespace HarvestManor.Core.Time;

public readonly record struct GameDate(Season Season, int Day)
{
    public GameDate NextDay(int daysPerSeason = 28)
    {
        if (Day < daysPerSeason)
        {
            return this with { Day = Day + 1 };
        }

        var nextSeason = Season switch
        {
            Season.Spring => Season.Summer,
            Season.Summer => Season.Autumn,
            Season.Autumn => Season.Winter,
            _ => Season.Spring
        };

        return new GameDate(nextSeason, 1);
    }
}
