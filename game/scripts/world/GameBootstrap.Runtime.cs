using System.IO;
using System.Linq;
using Godot;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap
{
    private void WireFarmScene(Node farmScene)
    {
        _farmPlotNodes.Clear();
        _farmPlotNodes.AddRange(farmScene.GetChildren().OfType<FarmPlotNode>());
        if (_farmPlotNodes.Count == 0)
        {
            GD.PushWarning("Farm scene is missing FarmPlotNode children.");
        }
        else
        {
            foreach (var plotNode in _farmPlotNodes)
            {
                plotNode.PlotInteracted += OnFarmPlotInteracted;
                plotNode.MouseEntered += () => OnFarmPlotHovered(plotNode.GridX, plotNode.GridY);
                plotNode.MouseExited += OnWorldInteractionHoverEnded;
            }
        }

        _farmStatusLabel = farmScene.GetNodeOrNull<Label>("FarmStatusLabel");

        var bed = farmScene.GetNodeOrNull<BedInteraction>("Bed");
        if (bed is null)
        {
            GD.PushWarning("Farm scene is missing a BedInteraction node named 'Bed'.");
            return;
        }

        bed.DayEndRequested += OnDayEndRequested;
        bed.MouseEntered += () => OnWorldInteractionHovered("bed", "click to end day");
        bed.MouseExited += OnWorldInteractionHoverEnded;
    }

    private void WireTownScene(Node townScene)
    {
        _requestStatusLabel = townScene.GetNodeOrNull<Label>("RequestStatusLabel");

        var shop = townScene.GetNodeOrNull<ShopInteraction>("Shop");
        if (shop is null)
        {
            GD.PushWarning("Town scene is missing a ShopInteraction node named 'Shop'.");
        }
        else
        {
            shop.ShopRequested += OnShopRequested;
            shop.MouseEntered += () => OnWorldInteractionHovered("shop", "buy or sell items", PanelMode.Shop);
            shop.MouseExited += OnWorldInteractionHoverEnded;
        }

        var storage = townScene.GetNodeOrNull<StorageInteraction>("Storage");
        if (storage is null)
        {
            GD.PushWarning("Town scene is missing a StorageInteraction node named 'Storage'.");
        }
        else
        {
            storage.StorageRequested += OnStorageRequested;
            storage.MouseEntered += () => OnWorldInteractionHovered("storage", "move items", PanelMode.Storage);
            storage.MouseExited += OnWorldInteractionHoverEnded;
        }

        var requestBoard = townScene.GetNodeOrNull<RequestBoardInteraction>("RequestBoard");
        if (requestBoard is null)
        {
            GD.PushWarning("Town scene is missing a RequestBoardInteraction node named 'RequestBoard'.");
        }
        else
        {
            requestBoard.RequestBoardRequested += OnRequestBoardRequested;
            requestBoard.MouseEntered += OnRequestBoardHovered;
            requestBoard.MouseExited += OnWorldInteractionHoverEnded;
        }
    }

    private void WireUiPanels()
    {
        if (_shopPanel is not null)
        {
            _shopPanel.BuyRequested += OnShopBuyRequested;
            _shopPanel.SellRequested += OnShopSellRequested;
            _shopPanel.NextOfferRequested += OnShopNextOfferRequested;
            _shopPanel.PreviousOfferRequested += OnShopPreviousOfferRequested;
            _shopPanel.CloseRequested += OnShopCloseRequested;
        }

        if (_storagePanel is not null)
        {
            _storagePanel.StoreRequested += OnStorageStoreRequested;
            _storagePanel.WithdrawRequested += OnStorageWithdrawRequested;
            _storagePanel.CloseRequested += OnStorageCloseRequested;
        }
    }

    private void OnDayEndRequested()
    {
        EndDay();
    }

    private void OnFarmPlotHovered(int gridX, int gridY)
    {
        if (_farmGrid is null)
        {
            return;
        }

        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? BuildBlockedWorldInteractionMessage(_activePanelMode)
                : BuildFarmPlotHoverStatusMessage(_farmGrid.GetPlot(gridX, gridY), _cropCatalog, _inventory, _wallet?.Gold));
    }

    private void OnWorldInteractionHovered(string interactionName, string actionDescription, PanelMode requestedMode = PanelMode.None)
    {
        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? BuildBlockedWorldInteractionMessage(_activePanelMode, requestedMode)
                : BuildInteractionHoverStatusMessage(interactionName, actionDescription));
    }

    private void OnRequestBoardHovered()
    {
        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? BuildBlockedWorldInteractionMessage(_activePanelMode)
                : _inventory is null
                    ? BuildInteractionHoverStatusMessage("request board", "turn in crops")
                    : BuildRequestBoardHoverStatusMessage(_requests, _completedRequestIds, _inventory, _itemCatalog));
    }

    private void OnWorldInteractionHoverEnded()
    {
        RestoreFarmStatus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        var nextPanelMode = ResolvePanelModeAfterUnhandledKey(_activePanelMode, keyEvent.Keycode);
        if (nextPanelMode != _activePanelMode)
        {
            SetActivePanelMode(nextPanelMode);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (CanTriggerDemoExpansionShortcut(_activePanelMode, keyEvent.Keycode))
        {
            _ = TryPurchaseExpansion(DemoExpansionPlotKey, requiredGold: DemoExpansionCost);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == Key.F7 && TryNotifyBlockedWorldInteraction())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnFarmPlotInteracted(int gridX, int gridY)
    {
        if (BlocksWorldInteractions(_activePanelMode))
        {
            TryNotifyBlockedWorldInteraction();
            return;
        }

        if (_farmGrid is null || _inventory is null || _wallet is null || _cropCatalog.Count == 0)
        {
            return;
        }

        var plot = _farmGrid.GetPlot(gridX, gridY);
        var updatedGold = _wallet.Gold;
        string message;
        bool changed;

        if (plot.IsLocked)
        {
            changed = TryHandleLockedPlotInteraction(_expansionService, _unlockState, _wallet.Gold, gridX, gridY, out updatedGold, out message);
        }
        else
        {
            changed = TryHandleFarmPlotInteraction(_farmGrid, _inventory, _cropCatalog, gridX, gridY, out message);
        }

        if (plot.IsLocked && changed)
        {
            _wallet = new Wallet(updatedGold);
            SyncFarmGridLocksFromUnlockState(_farmGrid, _unlockState);
            RefreshHud();
        }

        SetFarmStatus(message);
        RenderFarmPlots();
        RenderPanels();
        RefreshRequestBoardStatus();

        if (changed)
        {
            Autosave();
        }
    }

    private void OnShopRequested()
    {
        if (!CanHandlePanelInteractionRequest(_activePanelMode, PanelMode.Shop))
        {
            TryNotifyBlockedWorldInteraction(PanelMode.Shop);
            return;
        }

        if (_inventory is null || _wallet is null)
        {
            return;
        }

        var nextMode = ResolvePanelModeAfterInteractionRequest(_activePanelMode, PanelMode.Shop);
        if (nextMode == PanelMode.Shop)
        {
            _ = TryApplyShopOpenSideEffects(_inventory, _wallet, _shopOffers);
        }

        RenderPanels();
        SetActivePanelMode(nextMode);
        if (nextMode == PanelMode.Shop)
        {
            SetPanelContextFarmStatus(BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        }
    }

    private void OnStorageRequested()
    {
        if (!CanHandlePanelInteractionRequest(_activePanelMode, PanelMode.Storage))
        {
            TryNotifyBlockedWorldInteraction(PanelMode.Storage);
            return;
        }

        var nextMode = ResolvePanelModeAfterInteractionRequest(_activePanelMode, PanelMode.Storage);
        RenderPanels();
        SetActivePanelMode(nextMode);
        if (nextMode == PanelMode.Storage && _inventory is not null && _storage is not null)
        {
            SetPanelContextFarmStatus(BuildStorageBrowseStatusMessage(_inventory, _storage, _itemCatalog));
        }
    }

    private void OnRequestBoardRequested()
    {
        if (BlocksWorldInteractions(_activePanelMode))
        {
            TryNotifyBlockedWorldInteraction();
            return;
        }

        if (_inventory is null || _wallet is null)
        {
            return;
        }

        var changed = TryCompleteNextRequest(_requests, _requestBoardService, _inventory, _completedRequestIds, _wallet, _itemCatalog, out var message);
        RefreshRequestBoardStatus();
        SetFarmStatus(BuildRequestBoardActionStatusMessage(message, _requests, _completedRequestIds, _inventory, _itemCatalog));
        RenderPanels();

        if (changed)
        {
            RefreshHud();
            Autosave();
        }
    }

    private void OnShopBuyRequested()
    {
        if (_inventory is null || _wallet is null || !TryGetSelectedShopOffer(out var offer) || offer is null)
        {
            return;
        }

        var changed = _shopService.TryPurchase(_inventory, _wallet, offer, 1);
        if (changed)
        {
            RefreshHud();
            Autosave();
        }

        var actionMessage = BuildShopPurchaseStatusMessage(offer, _inventory, _wallet, changed, _itemCatalog);
        SetPanelContextFarmStatus(BuildShopActionStatusMessage(actionMessage, _shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnShopSellRequested()
    {
        if (_inventory is null || _wallet is null || !TryGetSelectedShopOffer(out var offer) || offer is null)
        {
            return;
        }

        var changed = _shopService.TrySell(_inventory, _wallet, offer, 1);
        if (changed)
        {
            RefreshHud();
            Autosave();
        }

        var actionMessage = BuildShopSellStatusMessage(offer, _inventory, changed, _itemCatalog);
        SetPanelContextFarmStatus(BuildShopActionStatusMessage(actionMessage, _shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnShopPreviousOfferRequested()
    {
        if (_shopOffers.Count == 0)
        {
            return;
        }

        _selectedShopOfferIndex = (_selectedShopOfferIndex - 1 + _shopOffers.Count) % _shopOffers.Count;
        RenderPanels();
        if (_inventory is not null && _wallet is not null)
        {
            SetPanelContextFarmStatus(BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        }
    }

    private void OnShopNextOfferRequested()
    {
        if (_shopOffers.Count == 0)
        {
            return;
        }

        _selectedShopOfferIndex = (_selectedShopOfferIndex + 1) % _shopOffers.Count;
        RenderPanels();
        if (_inventory is not null && _wallet is not null)
        {
            SetPanelContextFarmStatus(BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        }
    }

    private void OnShopCloseRequested()
    {
        SetActivePanelMode(PanelMode.None);
    }

    private void OnStorageStoreRequested(string itemId)
    {
        if (_inventory is null || _storage is null)
        {
            return;
        }

        var changed = TryTransferItem(_inventory, _storage, itemId, 1);
        if (changed)
        {
            Autosave();
        }

        var actionMessage = BuildStorageTransferStatusMessage(itemId, changed, intoStorage: true, _inventory, _storage, _itemCatalog);
        SetPanelContextFarmStatus(BuildStorageActionStatusMessage(actionMessage, _inventory, _storage, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnStorageWithdrawRequested(string itemId)
    {
        if (_inventory is null || _storage is null)
        {
            return;
        }

        var changed = TryTransferItem(_storage, _inventory, itemId, 1);
        if (changed)
        {
            Autosave();
        }

        var actionMessage = BuildStorageTransferStatusMessage(itemId, changed, intoStorage: false, _storage, _inventory, _itemCatalog);
        SetPanelContextFarmStatus(BuildStorageActionStatusMessage(actionMessage, _inventory, _storage, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnStorageCloseRequested()
    {
        SetActivePanelMode(PanelMode.None);
    }

    private bool TryPurchaseExpansion(string plotKey, int requiredGold)
    {
        if (_wallet is null)
        {
            return false;
        }

        if (!_expansionService.TryUnlockPlot(_unlockState, plotKey, requiredGold, _wallet.Gold, out var updatedGold))
        {
            return false;
        }

        _wallet = new Wallet(updatedGold);
        if (_farmGrid is not null)
        {
            SyncFarmGridLocksFromUnlockState(_farmGrid, _unlockState);
        }

        RefreshHud();
        RenderFarmPlots();
        Autosave();
        return true;
    }

    private bool TryGetSelectedShopOffer(out ShopOffer? offer)
    {
        offer = null;
        if (_shopOffers.Count == 0)
        {
            return false;
        }

        if (_selectedShopOfferIndex < 0 || _selectedShopOfferIndex >= _shopOffers.Count)
        {
            _selectedShopOfferIndex = 0;
        }

        offer = _shopOffers[_selectedShopOfferIndex];
        return true;
    }

    private void EndDay()
    {
        if (BlocksWorldInteractions(_activePanelMode))
        {
            TryNotifyBlockedWorldInteraction();
            return;
        }

        if (_clock is null || _stamina is null || _growth is null || _farmGrid is null)
        {
            return;
        }

        var rolled = ProcessDayEnd(_clock, _stamina, _growth, _farmGrid);
        if (rolled)
        {
            RefreshHud();
            RenderFarmPlots();
            SetFarmStatus(BuildDayStartFarmStatusMessage(_farmGrid, _requests, _completedRequestIds, _inventory, _itemCatalog));
            Autosave();
        }
    }

    private void SetActivePanelMode(PanelMode mode)
    {
        var previousMode = _activePanelMode;
        _activePanelMode = mode;
        ApplyPanelVisibility();

        if (mode == PanelMode.None && previousMode != PanelMode.None)
        {
            SetFarmStatus(BuildPanelCloseStatusMessage(previousMode, _latestPanelContextFarmStatusMessage));
            _latestPanelContextFarmStatusMessage = string.Empty;
            return;
        }

        if (previousMode == PanelMode.None && mode != PanelMode.None)
        {
            _latestPanelContextFarmStatusMessage = string.Empty;
        }

        var statusMessage = BuildPanelModeStatusMessage(previousMode, mode);
        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            SetFarmStatus(statusMessage);
        }
    }

    private void ApplyPanelVisibility()
    {
        var visibility = ResolvePanelVisibility(_activePanelMode);

        if (_inventoryPanel is not null)
        {
            _inventoryPanel.Visible = visibility.InventoryVisible;
        }

        if (_shopPanel is not null)
        {
            _shopPanel.Visible = visibility.ShopVisible;
        }

        if (_storagePanel is not null)
        {
            _storagePanel.Visible = visibility.StorageVisible;
        }
    }

    private void RenderFarmPlots()
    {
        if (_farmGrid is null)
        {
            return;
        }

        foreach (var plotNode in _farmPlotNodes)
        {
            var plot = _farmGrid.GetPlot(plotNode.GridX, plotNode.GridY);
            plotNode.Render(plot, ResolveCropDisplayName(plot), GetLockedPlotHint(plot.X, plot.Y));
        }
    }

    private void RenderPanels()
    {
        if (_inventory is not null)
        {
            _inventoryPanel?.Render(_inventory, _itemCatalog);
        }

        _shopPanel?.Render(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog);

        if (_inventory is not null && _storage is not null)
        {
            _storagePanel?.Render(_inventory, _storage, _itemCatalog);
        }
    }

    private void RefreshHud()
    {
        if (_clock is null || _stamina is null || _wallet is null || _hud is null)
        {
            return;
        }

        _hud.SetDay($"Day {_clock.Date.Day} ({_clock.Date.Season})");
        _hud.SetGold(_wallet.Gold);
        _hud.SetStamina(_stamina.Current, _stamina.Maximum);
        _hud.SetGrowth($"Unlocked plots: {_unlockState.UnlockedPlotKeys.Count}");
    }

    private void RefreshRequestBoardStatus(string? overrideMessage = null)
    {
        if (_requestStatusLabel is null || _inventory is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(overrideMessage))
        {
            _requestStatusLabel.Text = overrideMessage;
            return;
        }

        _requestStatusLabel.Text = BuildRequestBoardStatusText(_requests, _completedRequestIds, _inventory, _itemCatalog);
    }

    private void SetFarmStatus(string message)
    {
        _persistedFarmStatusMessage = message;
        if (_farmStatusLabel is not null)
        {
            _farmStatusLabel.Text = message;
        }
    }

    private void SetPanelContextFarmStatus(string message)
    {
        _latestPanelContextFarmStatusMessage = message;
        SetFarmStatus(message);
    }

    private void PreviewFarmStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || _farmStatusLabel is null)
        {
            return;
        }

        _farmStatusLabel.Text = message;
    }

    private void RestoreFarmStatus()
    {
        if (_farmStatusLabel is null || string.IsNullOrWhiteSpace(_persistedFarmStatusMessage))
        {
            return;
        }

        _farmStatusLabel.Text = _persistedFarmStatusMessage;
    }

    private bool TryNotifyBlockedWorldInteraction(PanelMode requestedMode = PanelMode.None)
    {
        var message = BuildBlockedWorldInteractionMessage(_activePanelMode, requestedMode);
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        SetFarmStatus(message);
        return true;
    }

    private string? ResolveCropDisplayName(PlotState plot)
    {
        if (plot.Crop is null)
        {
            return null;
        }

        return _cropCatalog.TryGetValue(plot.Crop.CropId, out var crop) ? crop.DisplayName : plot.Crop.CropId;
    }
}
