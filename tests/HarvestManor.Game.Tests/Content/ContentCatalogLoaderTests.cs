using HarvestManor.Core.Content;
using Xunit;

namespace HarvestManor.Game.Tests.Content;

public sealed class ContentCatalogLoaderTests
{
    [Fact]
    public void LoadCropCatalog_ReturnsExpectedCropById()
    {
        var loader = new ContentCatalogLoader();
        var cropPath = Path.Combine(AppContext.BaseDirectory, "game-data", "crops", "spring.json");

        var crops = loader.LoadCropCatalog(cropPath);

        var parsnip = Assert.Single(crops, crop => crop.Id == "parsnip");
        Assert.Equal("Parsnip", parsnip.DisplayName);
        Assert.Equal(4, parsnip.TotalGrowthDays);
        Assert.Equal("parsnip_seed", parsnip.SeedItemId);
        Assert.Equal("parsnip_crop", parsnip.HarvestItemId);
    }

    [Fact]
    public void LoadCropCatalog_ThrowsWhenGrowthStagesDoNotMatchTotalDays()
    {
        var loader = new ContentCatalogLoader();
        var invalidPath = Path.GetTempFileName();

        File.WriteAllText(
            invalidPath,
            """
            [
              {
                "id": "bad_turnip",
                "displayName": "Bad Turnip",
                "season": "Spring",
                "seedItemId": "bad_turnip_seed",
                "harvestItemId": "bad_turnip_crop",
                "purchasePrice": 10,
                "sellPrice": 18,
                "totalGrowthDays": 5,
                "growthStageDays": [2, 2]
              }
            ]
            """
        );

        var exception = Assert.Throws<InvalidDataException>(() => loader.LoadCropCatalog(invalidPath));
        Assert.Contains("bad_turnip", exception.Message);
    }
}
