using HarvestManor.Core.Economy;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.World;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class RewardFlowTests
{
    [Fact]
    public void CompletingRequest_AddsRewardToWallet()
    {
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 5));

        var wallet = new Wallet(0);
        var board = new RequestBoardService();
        var completedRequestIds = new HashSet<string>();
        var request = new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120);

        var completed = board.TryComplete(request, inventory, completedRequestIds, out var reward);
        if (completed)
        {
            wallet.Earn(reward);
        }

        Assert.True(completed);
        Assert.Equal(120, wallet.Gold);
        Assert.Equal(0, inventory.GetQuantity("parsnip_crop"));
        Assert.Contains("ship_5_parsnips", completedRequestIds);
    }

    [Fact]
    public void ShouldAutosaveAfterBootstrap_IsFalseForFreshSeededState()
    {
        var shouldAutosave = GameBootstrap.ShouldAutosaveAfterBootstrap(
            saveFileExists: false,
            loadedExistingSave: false,
            hasMeaningfulStateChanges: false);

        Assert.False(shouldAutosave);
    }

}
