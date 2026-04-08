namespace HarvestManor.Core.Farming;

public sealed class FarmGrid
{
    private readonly Dictionary<(int X, int Y), PlotState> _plots = new();

    public FarmGrid(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                _plots[(x, y)] = PlotState.Wild(x, y);
            }
        }
    }

    public PlotState GetPlot(int x, int y) => _plots[(x, y)];

    public void SetPlot(PlotState plot)
    {
        _plots[(plot.X, plot.Y)] = plot;
    }

    public IReadOnlyCollection<PlotState> AllPlots => _plots.Values;
}
