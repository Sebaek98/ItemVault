using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Memory;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;

namespace ItemVault.Handlers;

public sealed class TooltipHandler : IDisposable
{
    private readonly Plugin _plugin;

    public TooltipHandler(Plugin plugin)
    {
        _plugin = plugin;
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ItemDetail", OnItemTooltip);
    }

    private unsafe void OnItemTooltip(AddonEvent type, AddonArgs args)
    {
        var itemId = (uint)Plugin.GameGui.HoveredItem;
        var baseItemId = itemId > 1_000_000 ? itemId - 1_000_000 : itemId;

        if (!_plugin.Configuration.LockedItems.TryGetValue(baseItemId, out var entry))
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;

        var node = addon->GetTextNodeById(34);
        if (node == null) return;

        var seString = MemoryHelper.ReadSeStringNullTerminated((nint)(byte*)node->NodeText.StringPtr);

        var categoryText = $"[{entry.ListName}] ";
        
        if (seString.TextValue.Contains(categoryText)) return;

        // 1. Inject the color payload (0 for default, or try 3 for the Patch text color)
        seString.Payloads.Insert(0, new UIForegroundPayload(3));
        
        // 2. Inject your actual category text
        seString.Payloads.Insert(1, new TextPayload(categoryText));
        
		seString.Payloads.Insert(2, new UIForegroundPayload(0));

        var encodedBytes = seString.Encode();
        fixed (byte* ptr = encodedBytes)
        {
            node->NodeText.SetString(ptr);
        }
    }

    public void Dispose()
    {
        Plugin.AddonLifecycle.UnregisterListener(OnItemTooltip);
    }
}