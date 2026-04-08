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
        var invalidPath = WriteTempJson(
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

    [Fact]
    public void LoadCropCatalog_ThrowsWhenGrowthStagesAreMissing()
    {
        var loader = new ContentCatalogLoader();
        var invalidPath = WriteTempJson(
            """
            [
              {
                "id": "missing_stages",
                "displayName": "Missing Stages",
                "season": "Spring",
                "seedItemId": "missing_stages_seed",
                "harvestItemId": "missing_stages_crop",
                "purchasePrice": 10,
                "sellPrice": 18,
                "totalGrowthDays": 5
              }
            ]
            """
        );

        var exception = Assert.Throws<InvalidDataException>(() => loader.LoadCropCatalog(invalidPath));
        Assert.Contains("missing_stages", exception.Message);
    }

    [Fact]
    public void LoadCropCatalog_ThrowsWhenGrowthStagesAreNull()
    {
        var loader = new ContentCatalogLoader();
        var invalidPath = WriteTempJson(
            """
            [
              {
                "id": "null_stages",
                "displayName": "Null Stages",
                "season": "Spring",
                "seedItemId": "null_stages_seed",
                "harvestItemId": "null_stages_crop",
                "purchasePrice": 10,
                "sellPrice": 18,
                "totalGrowthDays": 5,
                "growthStageDays": null
              }
            ]
            """
        );

        var exception = Assert.Throws<InvalidDataException>(() => loader.LoadCropCatalog(invalidPath));
        Assert.Contains("null_stages", exception.Message);
    }

    [Fact]
    public void LoadItemCatalog_ThrowsWhenItemDefinitionIsInvalid()
    {
        var loader = new ContentCatalogLoader();
        var invalidPath = WriteTempJson(
            """
            [
              {
                "id": "broken_item",
                "displayName": "Broken Item",
                "category": "Seed",
                "maxStack": 0
              }
            ]
            """
        );

        var exception = Assert.Throws<InvalidDataException>(() => loader.LoadItemCatalog(invalidPath));
        Assert.Contains("broken_item", exception.Message);
    }

    private static string WriteTempJson(string json)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);
        return path;
    }
}
