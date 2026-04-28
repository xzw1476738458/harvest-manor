using System.IO;
using System.Linq;
using Godot;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Gathering;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap
{
    private void LoadScene(string sceneType, Vector2 spawnPosition)
    {
        if (_activeScene is not null)
        {
            UnwireActiveScene();
            _activeScene.QueueFree();
            _activeScene = null;
        }

        var scenePath = ResolveScenePath(sceneType);
        var instance = GD.Load<PackedScene>(scenePath).Instantiate<Node2D>();
        AddChild(instance);
        MoveChild(instance, 0);
        _activeScene = instance;
        _activeSceneType = sceneType;

        switch (sceneType)
        {
            case TownSceneType:
                WireTownScene(instance);
                break;
            case CottageSceneType:
                WireCottageScene(instance);
                break;
            case ShopInteriorSceneType:
                WireShopInteriorScene(instance);
                break;
            case BarnInteriorSceneType:
                WireBarnInteriorScene(instance);
                break;
            case GatheringSceneType:
                WireGatheringScene(instance);
                RenderGatheringNodes();
                break;
            default:
                WireFarmScene(instance);
                RenderFarmPlots();
                break;
        }

        WireSceneGates(instance);

        if (_player is not null)
        {
            _player.Position = spawnPosition;
            _player.Velocity = Vector2.Zero;
        }

        ApplyCameraBoundsForScene(instance);
    }

    private void ApplyCameraBoundsForScene(Node sceneRoot)
    {
        if (_player is null)
        {
            return;
        }

        var camera = _player.GetNodeOrNull<Camera2D>("Camera");
        if (camera is null)
        {
            return;
        }

        var bounds = sceneRoot.GetNodeOrNull<CameraBounds>("CameraBounds");
        if (bounds is null)
        {
            camera.LimitLeft = int.MinValue;
            camera.LimitTop = int.MinValue;
            camera.LimitRight = int.MaxValue;
            camera.LimitBottom = int.MaxValue;
            camera.ResetSmoothing();
            return;
        }

        camera.LimitLeft = bounds.Left;
        camera.LimitTop = bounds.Top;
        camera.LimitRight = bounds.Right;
        camera.LimitBottom = bounds.Bottom;
        camera.ResetSmoothing();
    }

    private static string ResolveScenePath(string sceneType) => sceneType switch
    {
        TownSceneType => "res://scenes/world/TownScene.tscn",
        CottageSceneType => "res://scenes/world/CottageInterior.tscn",
        ShopInteriorSceneType => "res://scenes/world/ShopInterior.tscn",
        BarnInteriorSceneType => "res://scenes/world/BarnInterior.tscn",
        GatheringSceneType => "res://scenes/world/GatheringScene.tscn",
        _ => "res://scenes/world/FarmScene.tscn",
    };

    private void UnwireActiveScene()
    {
        if (_activeScene is null)
        {
            return;
        }

        _farmPlotNodes.Clear();
        _farmStatusLabel = null;
        _requestStatusLabel = null;
        _farmStatusPanel = null;
        _requestStatusPanel = null;
        _farmStatusTimer?.Stop();
        _requestStatusTimer?.Stop();
    }

    private void WireSceneGates(Node sceneRoot)
    {
        foreach (var child in sceneRoot.GetChildren())
        {
            if (child is SceneGate gate)
            {
                gate.GateEntered += OnGateEntered;
            }
        }
    }

    private void OnGateEntered(string targetScene)
    {
        if (string.Equals(_activeSceneType, targetScene, StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(targetScene, ShopInteriorSceneType, StringComparison.Ordinal)
            && _clock is not null
            && !TimeOfDayController.IsShopOpen(_clock.CurrentMinuteOfDay))
        {
            SetFarmStatus(StatusMessageBuilder.BuildShopClosedAttemptStatusMessage());
            return;
        }

        var spawn = ResolveSpawnForTransition(_activeSceneType, targetScene);
        CallDeferred(nameof(DeferredLoadScene), targetScene, spawn);
    }

    private static Vector2 ResolveSpawnForTransition(string sourceScene, string targetScene) =>
        (targetScene, sourceScene) switch
        {
            (FarmSceneType, TownSceneType) => FarmFromTownSpawn,
            (FarmSceneType, CottageSceneType) => FarmFromCottageSpawn,
            (TownSceneType, ShopInteriorSceneType) => TownFromShopSpawn,
            (TownSceneType, BarnInteriorSceneType) => TownFromBarnSpawn,
            (TownSceneType, FarmSceneType) => TownFromFarmSpawn,
            (TownSceneType, GatheringSceneType) => TownFromGatheringSpawn,
            (GatheringSceneType, TownSceneType) => GatheringFromTownSpawn,
            (CottageSceneType, _) => CottageEntrySpawn,
            (ShopInteriorSceneType, _) => ShopInteriorEntrySpawn,
            (BarnInteriorSceneType, _) => BarnInteriorEntrySpawn,
            (FarmSceneType, _) => FarmDefaultSpawn,
            _ => FarmDefaultSpawn,
        };

    private void DeferredLoadScene(string sceneType, Vector2 spawn)
    {
        LoadScene(sceneType, spawn);
        switch (sceneType)
        {
            case FarmSceneType:
                RefreshFarmStatusAfterSwitch();
                break;
            case TownSceneType:
                RefreshRequestBoardStatus();
                break;
            case GatheringSceneType:
                RefreshFarmStatusAfterSwitch();
                break;
        }
    }

    private void WireCottageScene(Node cottageScene)
    {
        var bed = cottageScene.GetNodeOrNull<BedInteraction>("Bed");
        if (bed is null)
        {
            GD.PushWarning("Cottage interior is missing a BedInteraction node named 'Bed'.");
            return;
        }

        bed.DayEndRequested += OnDayEndRequested;
    }

    private void WireShopInteriorScene(Node shopScene)
    {
        var counter = shopScene.GetNodeOrNull<ShopInteraction>("Counter");
        if (counter is null)
        {
            GD.PushWarning("Shop interior is missing a ShopInteraction node named 'Counter'.");
            return;
        }

        counter.ShopRequested += OnShopRequested;
    }

    private void WireBarnInteriorScene(Node barnScene)
    {
        var chest = barnScene.GetNodeOrNull<StorageInteraction>("Chest");
        if (chest is null)
        {
            GD.PushWarning("Barn interior is missing a StorageInteraction node named 'Chest'.");
            return;
        }

        chest.StorageRequested += OnStorageRequested;
    }

    private void WireGatheringScene(Node gatheringScene)
    {
        _farmStatusLabel = gatheringScene.GetNodeOrNull<Label>("SceneOverlay/FarmStatusPanel/Margin/Content/FarmStatusLabel");
        _farmStatusPanel = gatheringScene.GetNodeOrNull<Control>("SceneOverlay/FarmStatusPanel");

        _resourceNodes.Clear();
        _resourceNodes.AddRange(gatheringScene.GetChildren().OfType<ResourceNode>());

        if (_resourceNodes.Count == 0)
        {
            GD.PushWarning("Gathering scene is missing ResourceNode children.");
            return;
        }

        foreach (var node in _resourceNodes)
        {
            var capturedNode = node;
            capturedNode.ResourceNodeInteracted += OnResourceNodeInteracted;
            capturedNode.MouseEntered += () =>
            {
                var harvested = _gatheringService?.State.IsHarvested(capturedNode.NodeId) ?? false;
                OnWorldInteractionHovered(
                    ResolveItemDisplayName(capturedNode.ItemId).ToLowerInvariant(),
                    harvested ? "already gathered today" : "click to gather");
            };
            capturedNode.MouseExited += OnWorldInteractionHoverEnded;
        }
    }

    private void RenderGatheringNodes()
    {
        if (_gatheringService is null)
        {
            return;
        }

        foreach (var node in _resourceNodes)
        {
            var harvested = _gatheringService.State.IsHarvested(node.NodeId);
            node.Render(harvested, ResolveItemDisplayName(node.ItemId));
        }
    }

    private string ResolveItemDisplayName(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return "Resource";
        }

        return _itemCatalog.TryGetValue(itemId, out var def) ? def.DisplayName : itemId;
    }

    private void OnResourceNodeInteracted(string nodeId)
    {
        if (BlocksWorldInteractions(_activePanelMode))
        {
            TryNotifyBlockedWorldInteraction();
            return;
        }

        if (_gatheringService is null || _inventory is null)
        {
            return;
        }

        var result = _gatheringService.TryHarvest(nodeId, _inventory);
        var message = StatusMessageBuilder.BuildGatheringStatusMessage(
            result,
            result.ItemId is null ? null : ResolveItemDisplayName(result.ItemId));
        SetFarmStatus(message);

        if (result.Outcome == GatheringHarvestOutcome.Success)
        {
            RefreshHud();
            RenderPanels();
            RenderGatheringNodes();
            Autosave();
        }
    }

    private void RefreshFarmStatusAfterSwitch()
    {
        if (_farmStatusLabel is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_persistedFarmStatusMessage))
        {
            _farmStatusLabel.Text = _persistedFarmStatusMessage;
        }
    }

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

        _farmStatusLabel = farmScene.GetNodeOrNull<Label>("SceneOverlay/FarmStatusPanel/Margin/Content/FarmStatusLabel");
        _farmStatusPanel = farmScene.GetNodeOrNull<Control>("SceneOverlay/FarmStatusPanel");

        var porchDoor = farmScene.GetNodeOrNull<EnterBuildingInteraction>("Bed");
        if (porchDoor is null)
        {
            GD.PushWarning("Farm scene is missing an EnterBuildingInteraction node named 'Bed'.");
            return;
        }

        porchDoor.EnterBuildingRequested += OnGateEntered;
        porchDoor.MouseEntered += () => OnWorldInteractionHovered("cottage", "step inside to rest");
        porchDoor.MouseExited += OnWorldInteractionHoverEnded;
    }

    private void WireTownScene(Node townScene)
    {
        _requestStatusLabel = townScene.GetNodeOrNull<Label>("SceneOverlay/RequestStatusPanel/Margin/Content/RequestStatusLabel");
        _requestStatusPanel = townScene.GetNodeOrNull<Control>("SceneOverlay/RequestStatusPanel");
        _farmStatusLabel = townScene.GetNodeOrNull<Label>("SceneOverlay/FarmStatusPanel/Margin/Content/FarmStatusLabel");
        _farmStatusPanel = townScene.GetNodeOrNull<Control>("SceneOverlay/FarmStatusPanel");

        var shopDoor = townScene.GetNodeOrNull<EnterBuildingInteraction>("Shop");
        if (shopDoor is null)
        {
            GD.PushWarning("Town scene is missing an EnterBuildingInteraction node named 'Shop'.");
        }
        else
        {
            shopDoor.EnterBuildingRequested += OnGateEntered;
            shopDoor.MouseEntered += OnShopDoorHovered;
            shopDoor.MouseExited += OnWorldInteractionHoverEnded;
        }

        var barnDoor = townScene.GetNodeOrNull<EnterBuildingInteraction>("Storage");
        if (barnDoor is null)
        {
            GD.PushWarning("Town scene is missing an EnterBuildingInteraction node named 'Storage'.");
        }
        else
        {
            barnDoor.EnterBuildingRequested += OnGateEntered;
            barnDoor.MouseEntered += () => OnWorldInteractionHovered("barn", "step inside to manage chest");
            barnDoor.MouseExited += OnWorldInteractionHoverEnded;
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
            requestBoard.MouseExited += OnRequestBoardHoverEnded;
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
        if (_farmGrid is null || ShouldSilenceHoverPreview(_activePanelMode))
        {
            return;
        }

        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? StatusMessageBuilder.BuildBlockedWorldInteractionMessage(_activePanelMode)
                : StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(_farmGrid.GetPlot(gridX, gridY), _cropCatalog, _inventory, _wallet?.Gold, _clock?.Date.Season, _expansionTiers.GetUnlockCost));
    }

    private void OnWorldInteractionHovered(string interactionName, string actionDescription, PanelMode requestedMode = PanelMode.None)
    {
        if (ShouldSilenceHoverPreview(_activePanelMode))
        {
            return;
        }

        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? StatusMessageBuilder.BuildBlockedWorldInteractionMessage(_activePanelMode, requestedMode)
                : StatusMessageBuilder.BuildInteractionHoverStatusMessage(interactionName, actionDescription));
    }

    private void OnShopDoorHovered()
    {
        if (ShouldSilenceHoverPreview(_activePanelMode))
        {
            return;
        }

        if (BlocksWorldInteractions(_activePanelMode))
        {
            PreviewFarmStatus(StatusMessageBuilder.BuildBlockedWorldInteractionMessage(_activePanelMode));
            return;
        }

        if (_clock is not null && !TimeOfDayController.IsShopOpen(_clock.CurrentMinuteOfDay))
        {
            PreviewFarmStatus(StatusMessageBuilder.BuildShopClosedHoverStatusMessage());
            return;
        }

        PreviewFarmStatus(StatusMessageBuilder.BuildInteractionHoverStatusMessage("general store", "step inside to trade"));
    }

    private void OnRequestBoardHovered()
    {
        if (ShouldSilenceHoverPreview(_activePanelMode))
        {
            return;
        }

        var message = BlocksWorldInteractions(_activePanelMode)
            ? StatusMessageBuilder.BuildBlockedWorldInteractionMessage(_activePanelMode)
            : _inventory is null
                ? StatusMessageBuilder.BuildInteractionHoverStatusMessage("request board", "turn in crops")
                : StatusMessageBuilder.BuildRequestBoardHoverStatusMessage(_requests, _completedRequestIds, _inventory, _itemCatalog);

        RefreshRequestBoardStatus(overrideMessage: message);
    }

    private void OnRequestBoardHoverEnded()
    {
        if (ShouldSilenceHoverPreview(_activePanelMode))
        {
            return;
        }

        RefreshRequestBoardStatus();
    }

    private void OnWorldInteractionHoverEnded()
    {
        if (ShouldSilenceHoverPreview(_activePanelMode))
        {
            return;
        }

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

        if (CanTriggerQuickExpansionShortcut(_activePanelMode, keyEvent.Keycode))
        {
            _ = TryPurchaseCheapestLockedPlot();
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
        string message;
        bool changed;

        if (plot.IsLocked)
        {
            changed = TryHandleLockedPlotInteraction(_expansionService, _expansionTiers, _unlockState, _wallet, gridX, gridY, out message);
            if (changed)
            {
                SyncFarmGridLocksFromUnlockState(_farmGrid, _unlockState);
                RefreshHud();
            }
        }
        else
        {
            changed = TryHandleFarmPlotInteraction(_farmGrid, _inventory, _cropCatalog, gridX, gridY, out message, _stamina, _clock?.Date.Season);
            if (changed)
            {
                RefreshHud();
            }
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

        RenderPanels();
        SetActivePanelMode(nextMode);
        if (nextMode == PanelMode.Shop)
        {
            SetPanelContextFarmStatus(StatusMessageBuilder.BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
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
            SetPanelContextFarmStatus(StatusMessageBuilder.BuildStorageBrowseStatusMessage(_inventory, _storage, _itemCatalog));
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
        SetFarmStatus(StatusMessageBuilder.BuildRequestBoardActionStatusMessage(message, _requests, _completedRequestIds, _inventory, _itemCatalog));
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

        var actionMessage = StatusMessageBuilder.BuildShopPurchaseStatusMessage(offer, _inventory, _wallet, changed, _itemCatalog);
        SetPanelContextFarmStatus(StatusMessageBuilder.BuildShopActionStatusMessage(actionMessage, _shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
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

        var actionMessage = StatusMessageBuilder.BuildShopSellStatusMessage(offer, _inventory, changed, _itemCatalog);
        SetPanelContextFarmStatus(StatusMessageBuilder.BuildShopActionStatusMessage(actionMessage, _shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
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
            SetPanelContextFarmStatus(StatusMessageBuilder.BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
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
            SetPanelContextFarmStatus(StatusMessageBuilder.BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
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

        var actionMessage = StatusMessageBuilder.BuildStorageTransferStatusMessage(itemId, changed, intoStorage: true, _inventory, _storage, _itemCatalog);
        SetPanelContextFarmStatus(StatusMessageBuilder.BuildStorageActionStatusMessage(actionMessage, _inventory, _storage, _itemCatalog));
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

        var actionMessage = StatusMessageBuilder.BuildStorageTransferStatusMessage(itemId, changed, intoStorage: false, _storage, _inventory, _itemCatalog);
        SetPanelContextFarmStatus(StatusMessageBuilder.BuildStorageActionStatusMessage(actionMessage, _inventory, _storage, _itemCatalog));
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

        if (!_expansionService.TryUnlockPlot(_unlockState, plotKey, requiredGold, _wallet))
        {
            return false;
        }

        if (_farmGrid is not null)
        {
            SyncFarmGridLocksFromUnlockState(_farmGrid, _unlockState);
        }

        RefreshHud();
        RenderFarmPlots();
        Autosave();
        return true;
    }

    private bool TryPurchaseCheapestLockedPlot()
    {
        if (_wallet is null)
        {
            return false;
        }

        var failureMessage = BuildQuickExpansionShortcutFailureMessage(_expansionTiers, _unlockState, _wallet.Gold);
        if (failureMessage is not null)
        {
            SetFarmStatus(failureMessage);
            return false;
        }

        var activeTier = _expansionTiers.GetActiveTier(_unlockState);
        if (activeTier is null)
        {
            return false;
        }

        foreach (var plotKey in activeTier.PlotKeys)
        {
            if (_unlockState.Contains(plotKey))
            {
                continue;
            }

            if (TryPurchaseExpansion(plotKey, activeTier.UnlockCost))
            {
                SetFarmStatus($"Unlocked plot {plotKey} for {activeTier.UnlockCost}g.");
                return true;
            }
        }

        return false;
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

        if (!HarvestManor.Core.Time.TimeOfDayController.IsSleepAllowed(_clock.CurrentMinuteOfDay))
        {
            SetFarmStatus($"It's only {HarvestManor.Core.Time.TimeOfDayController.FormatClock(_clock.CurrentMinuteOfDay)}. Come back to bed after 18:00.");
            return;
        }

        var minutesToAdvance = HarvestManor.Core.Time.TimeOfDayController.DayEndMinute - _clock.CurrentMinuteOfDay;
        var result = ProcessDayEnd(_clock, _stamina, _growth, _farmGrid, _cropCatalog, minutesToAdvance);
        if (!result.DayRolled)
        {
            return;
        }

        if (result.SeasonChanged)
        {
            _shopOffers = BuildSeasonShopOffers(_allShopOffers, _cropCatalog, result.CurrentSeason, _itemCatalog);
            _selectedShopOfferIndex = 0;
            RenderPanels();
        }

        _gatheringService?.ResetForNewDay();

        RefreshHud();
        RenderFarmPlots();
        RenderGatheringNodes();
        SetFarmStatus(StatusMessageBuilder.BuildDayStartFarmStatusMessage(
            _farmGrid, _requests, _completedRequestIds, _inventory, _itemCatalog,
            result.SeasonChanged ? result.CurrentSeason : null,
            result.CropsWithered));
        UpdateTimeOfDayVisuals();
        _realTimeAccumulator = 0.0;
        Autosave();
    }

    private void SetActivePanelMode(PanelMode mode)
    {
        var previousMode = _activePanelMode;
        _activePanelMode = mode;
        ApplyPanelVisibility();

        if (mode == PanelMode.None && previousMode != PanelMode.None)
        {
            SetFarmStatus(StatusMessageBuilder.BuildPanelCloseStatusMessage(previousMode, _latestPanelContextFarmStatusMessage));
            _latestPanelContextFarmStatusMessage = string.Empty;
            return;
        }

        if (previousMode == PanelMode.None && mode != PanelMode.None)
        {
            _latestPanelContextFarmStatusMessage = string.Empty;
        }

        var statusMessage = StatusMessageBuilder.BuildPanelModeStatusMessage(previousMode, mode);
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

        _hud?.SetTopBarVisible(_activePanelMode == PanelMode.None);

        var movementEnabled = !BlocksWorldInteractions(_activePanelMode);
        foreach (var node in GetTree().GetNodesInGroup(PlayerController.PlayerGroup))
        {
            if (node is PlayerController player)
            {
                player.MovementEnabled = movementEnabled;
            }
        }
    }

    private void RenderFarmPlots()
    {
        if (_farmGrid is null)
        {
            return;
        }

        var activeTier = _expansionTiers.GetActiveTier(_unlockState);
        var activeTierKeys = activeTier is null
            ? null
            : new HashSet<string>(activeTier.PlotKeys, StringComparer.Ordinal);

        foreach (var plotNode in _farmPlotNodes)
        {
            var plot = _farmGrid.GetPlot(plotNode.GridX, plotNode.GridY);
            var plotKey = BuildPlotKey(plot.X, plot.Y);
            var isInActiveTier = activeTierKeys is not null && activeTierKeys.Contains(plotKey);
            var cropDefinition = ResolveCropDefinition(plot);
            plotNode.Render(
                plot,
                cropDefinition?.DisplayName,
                isInActiveTier ? GetLockedPlotHint(plot.X, plot.Y, _expansionTiers) : null,
                isInActiveTier,
                cropDefinition);
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

        _hud.SetClock(HarvestManor.Core.Time.TimeOfDayController.FormatClock(_clock.CurrentMinuteOfDay));
        _hud.SetDay($"Day {_clock.Date.Day} ({_clock.Date.Season})");
        _hud.SetGold(_wallet.Gold);
        _hud.SetStamina(_stamina.Current, _stamina.Maximum);
        _hud.SetGrowth($"Plots: {_unlockState.UnlockedPlotKeys.Count}");
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
            ShowRequestStatusPanel();
            return;
        }

        _requestStatusLabel.Text = StatusMessageBuilder.BuildRequestBoardStatusText(_requests, _completedRequestIds, _inventory, _itemCatalog);
        ShowRequestStatusPanel();
    }

    private void ShowFarmStatusPanel()
    {
        if (_farmStatusPanel is null)
        {
            return;
        }
        _farmStatusPanel.Visible = true;
        _farmStatusTimer?.Start();
        SuppressRequestStatusPanel();
    }

    private void HideFarmStatusPanel()
    {
        var wasVisible = _farmStatusPanel?.Visible ?? false;
        if (_farmStatusPanel is not null)
        {
            _farmStatusPanel.Visible = false;
        }

        if (wasVisible && _activeSceneType == TownSceneType && _requestStatusLabel is not null)
        {
            RefreshRequestBoardStatus();
        }
    }

    private void ShowRequestStatusPanel()
    {
        if (_requestStatusPanel is null)
        {
            return;
        }
        _requestStatusPanel.Visible = true;
        _requestStatusTimer?.Start();
        SuppressFarmStatusPanel();
    }

    private void HideRequestStatusPanel()
    {
        if (_requestStatusPanel is not null)
        {
            _requestStatusPanel.Visible = false;
        }
    }

    private void SuppressFarmStatusPanel()
    {
        if (_farmStatusPanel is null || ReferenceEquals(_farmStatusPanel, _requestStatusPanel))
        {
            return;
        }

        _farmStatusPanel.Visible = false;
        _farmStatusTimer?.Stop();
    }

    private void SuppressRequestStatusPanel()
    {
        if (_requestStatusPanel is null || ReferenceEquals(_requestStatusPanel, _farmStatusPanel))
        {
            return;
        }

        _requestStatusPanel.Visible = false;
        _requestStatusTimer?.Stop();
    }

    private void SetFarmStatus(string message)
    {
        _persistedFarmStatusMessage = message;
        if (_farmStatusLabel is not null)
        {
            _farmStatusLabel.Text = message;
        }
        if (!string.IsNullOrWhiteSpace(message))
        {
            ShowFarmStatusPanel();
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
        ShowFarmStatusPanel();
    }

    private void RestoreFarmStatus()
    {
        if (_farmStatusLabel is null || string.IsNullOrWhiteSpace(_persistedFarmStatusMessage))
        {
            return;
        }

        _farmStatusLabel.Text = _persistedFarmStatusMessage;
        ShowFarmStatusPanel();
    }

    private bool TryNotifyBlockedWorldInteraction(PanelMode requestedMode = PanelMode.None)
    {
        var message = StatusMessageBuilder.BuildBlockedWorldInteractionMessage(_activePanelMode, requestedMode);
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

    private HarvestManor.Core.Content.CropDefinition? ResolveCropDefinition(PlotState plot)
    {
        if (plot.Crop is null)
        {
            return null;
        }

        return _cropCatalog.TryGetValue(plot.Crop.CropId, out var crop) ? crop : null;
    }
}
