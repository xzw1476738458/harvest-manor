namespace HarvestManor.Core.Farming;

public sealed class FarmGrid
{
    private readonly int _width;
    private readonly int _height;
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

        _width = width;
        _height = height;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                _plots[(x, y)] = PlotState.Wild(x, y);
            }
        }
    }

    public PlotState GetPlot(int x, int y)
    {
        EnsureInBounds(x, y);
        return _plots[(x, y)];
    }

    public void SetPlot(PlotState plot)
    {
        ArgumentNullException.ThrowIfNull(plot);
        EnsureInBounds(plot.X, plot.Y);
        _plots[(plot.X, plot.Y)] = plot;
    }

    public int Width => _width;

    public int Height => _height;

    public IReadOnlyCollection<PlotState> AllPlots => _plots.Values;

    private void EnsureInBounds(int x, int y)
    {
        if (x < 0 || x >= _width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"X must be in range [0, {_width - 1}].");
        }

        if (y < 0 || y >= _height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Y must be in range [0, {_height - 1}].");
        }
    }
}
