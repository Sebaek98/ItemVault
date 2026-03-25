using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace ItemVault;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // Key: item ID, Value: lock entry
    public Dictionary<uint, LockEntry> LockedItems { get; set; } = new();

    // All list names the user has ever used, for autocomplete
    public List<string> KnownLists { get; set; } = new() { "Locked", "Crafting", "Don't Sell", "Glamour" };

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
