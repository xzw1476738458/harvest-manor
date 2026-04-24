namespace HarvestManor.Core.Farming;

public sealed record CropInstance
{
    public CropInstance(string cropId, int daysGrown)
    {
        if (string.IsNullOrWhiteSpace(cropId))
        {
            throw new ArgumentException("Crop id cannot be blank.", nameof(cropId));
        }

        if (daysGrown < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(daysGrown), daysGrown, "Days grown cannot be negative.");
        }

        CropId = cropId;
        DaysGrown = daysGrown;
    }

    public string CropId { get; init; }

    public int DaysGrown { get; init; }
}
