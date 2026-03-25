using Dalamud.Game.Text.SeStringHandling;
using System;
using Dalamud.Plugin.Services;
using Dalamud.Game.Gui.ContextMenu;

namespace ItemVault.Handlers;

public sealed class ContextMenuHandler : IDisposable
{
    private readonly Plugin _plugin;
    private uint _lastHoveredItem;

    public ContextMenuHandler(Plugin plugin)
    {
        _plugin = plugin;
        Plugin.ContextMenu.OnMenuOpened += OnMenuOpened;
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var currentHover = (uint)Plugin.GameGui.HoveredItem;
        if (currentHover != 0)
        {
            _lastHoveredItem = currentHover;
        }
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        // The FINAL VIP List. Now with InventoryBuddy!
        if (args.AddonName is not ("Inventory" or "InventoryLarge" or "InventoryExpansion"
            or "InventoryGrid" or "ArmouryBoard" or "MiragePrismMiragePlate"
            or "RecipeNote" or "RetainerTask" or "RetainerTaskAsk" or "ItemSearch" 
            or "GatheringNote" or "ShopExchangeItem" or "Shop" 
            or "GrandCompanySupplyList" or "GrandCompanyExchange" or "FreeCompanyChest"
            or "InventoryRetainer" or "InventoryRetainerLarge" 
            or "RetainerTaskResult" or "RetainerList" 
            or "ChatLog" or "ItemDetail" or "RetainerTaskSupply" 
            or "RetainerTaskDetail" or "RetainerTaskList"
            or "InventoryBuddy"))
            return;

        var itemId = GetItemIdFromArgs(args);
        if (itemId == 0) return;

        _plugin.Configuration.LockedItems.TryGetValue(itemId, out var currentEntry);
        var currentCategory = currentEntry?.ListName;

        void AddCategoryButton(string categoryName)
        {
            var isCurrentCategory = currentCategory == categoryName;
            
            args.AddMenuItem(new MenuItem
            {
                PrefixChar = 'V',
                Name = isCurrentCategory ? $"Clear {categoryName}" : $"Mark: {categoryName}",
                OnClicked = _ => 
                {
                    if (isCurrentCategory) ClearCategory(itemId);
                    else SetCategory(itemId, categoryName);
                }
            });
        }

        AddCategoryButton("Keep");
        AddCategoryButton("Keep For Now");
        AddCategoryButton("Sell");
    }

    private uint GetItemIdFromArgs(IMenuOpenedArgs args)
    {
        uint id = 0;
        
        if (args.Target is MenuTargetInventory invTarget)
        {
            id = invTarget.TargetItem?.ItemId ?? 0;
        }
        
        if (id == 0)
        {
            id = _lastHoveredItem;
        }
        
        return id > 1_000_000 ? id - 1_000_000 : id;
    }

    private void SetCategory(uint itemId, string categoryName)
    {
        _plugin.Configuration.LockedItems[itemId] = new LockEntry { ListName = categoryName };
        _plugin.Configuration.Save();
        Plugin.Log.Information($"[ItemVault] Marked item {itemId} as '{categoryName}'");
    }

    private void ClearCategory(uint itemId)
    {
        _plugin.Configuration.LockedItems.Remove(itemId);
        _plugin.Configuration.Save();
        Plugin.Log.Information($"[ItemVault] Cleared category for item {itemId}");
    }

    public void Dispose()
    {
        Plugin.ContextMenu.OnMenuOpened -= OnMenuOpened;
        Plugin.Framework.Update -= OnFrameworkUpdate;
    }
}