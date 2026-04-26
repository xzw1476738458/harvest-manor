using HarvestManor.Core.Content;
using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Content;

public sealed class CropDefinitionTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(6, 1)]
    [InlineData(7, 2)]
    [InlineData(11, 2)]
    [InlineData(12, 2)]
    [InlineData(99, 2)]
    public void GetStageIndex_ReturnsExpectedStageForGivenDay(int daysGrown, int expectedStage)
    {
        var crop = BuildCrop(stageDays: new[] { 3, 4, 5 });

        Assert.Equal(expectedStage, crop.GetStageIndex(daysGrown));
    }

    [Fact]
    public void GetStageIndex_NeverNegative_WhenDaysGrownIsNegative()
    {
        var crop = BuildCrop(stageDays: new[] { 1, 2, 3 });

        Assert.Equal(0, crop.GetStageIndex(-5));
    }

    [Fact]
    public void GetStageIndex_ReturnsLastStage_WhenDaysGrownExceedsTotal()
    {
        var crop = BuildCrop(stageDays: new[] { 2, 2 });

        Assert.Equal(1, crop.GetStageIndex(50));
    }

    [Fact]
    public void StageCount_MatchesGrowthStageDaysCount()
    {
        var crop = BuildCrop(stageDays: new[] { 1, 2, 3, 4 });

        Assert.Equal(4, crop.StageCount);
    }

    private static CropDefinition BuildCrop(int[] stageDays)
    {
        var total = 0;
        foreach (var d in stageDays)
        {
            total += d;
        }

        var crop = new CropDefinition(
            Id: "demo",
            DisplayName: "Demo",
            Season: Season.Spring,
            SeedItemId: "demo_seed",
            HarvestItemId: "demo_crop",
            PurchasePrice: 10,
            SellPrice: 20,
            TotalGrowthDays: total,
            GrowthStageDays: stageDays);

        crop.Validate();
        return crop;
    }
}
