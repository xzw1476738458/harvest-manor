using HarvestManor.Core.Gathering;
using Xunit;

namespace HarvestManor.Game.Tests.Gathering;

public sealed class GatheringStateTests
{
    [Fact]
    public void NewState_HasNoHarvestedNodes()
    {
        var state = new GatheringState();

        Assert.Empty(state.HarvestedNodeIds);
        Assert.False(state.IsHarvested("forest_tree_1"));
    }

    [Fact]
    public void TryMarkHarvested_AddsTheNodeAndReturnsTrueOnFirstCall()
    {
        var state = new GatheringState();

        Assert.True(state.TryMarkHarvested("forest_tree_1"));
        Assert.True(state.IsHarvested("forest_tree_1"));
        Assert.Contains("forest_tree_1", state.HarvestedNodeIds);
    }

    [Fact]
    public void TryMarkHarvested_ReturnsFalseWhenAlreadyHarvested()
    {
        var state = new GatheringState();
        state.TryMarkHarvested("forest_tree_1");

        Assert.False(state.TryMarkHarvested("forest_tree_1"));
        Assert.Single(state.HarvestedNodeIds);
    }

    [Fact]
    public void ResetForNewDay_RestoresEveryHarvestedNode()
    {
        var state = new GatheringState();
        state.TryMarkHarvested("forest_tree_1");
        state.TryMarkHarvested("quarry_rock_2");

        state.ResetForNewDay();

        Assert.Empty(state.HarvestedNodeIds);
        Assert.False(state.IsHarvested("forest_tree_1"));
        Assert.False(state.IsHarvested("quarry_rock_2"));
    }

    [Fact]
    public void Reset_ReplacesHarvestedSetWithProvidedIds()
    {
        var state = new GatheringState();
        state.TryMarkHarvested("forest_tree_1");

        state.Reset(new[] { "quarry_rock_1", "quarry_rock_1", "quarry_rock_2" });

        Assert.Equal(2, state.HarvestedNodeIds.Count);
        Assert.False(state.IsHarvested("forest_tree_1"));
        Assert.True(state.IsHarvested("quarry_rock_1"));
        Assert.True(state.IsHarvested("quarry_rock_2"));
    }

    [Fact]
    public void ConstructWithSeed_HydratesHarvestedNodes()
    {
        var state = new GatheringState(new[] { "forest_tree_1", "forest_tree_1", "quarry_rock_2" });

        Assert.Equal(2, state.HarvestedNodeIds.Count);
        Assert.True(state.IsHarvested("forest_tree_1"));
        Assert.True(state.IsHarvested("quarry_rock_2"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryMarkHarvested_RejectsNullOrWhitespaceNodeId(string? nodeId)
    {
        var state = new GatheringState();

        Assert.ThrowsAny<ArgumentException>(() => state.TryMarkHarvested(nodeId!));
    }
}
