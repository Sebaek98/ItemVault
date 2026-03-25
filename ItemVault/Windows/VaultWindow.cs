using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Linq;
using System.Numerics;
using Lumina.Excel.Sheets;

namespace ItemVault.Windows;

public sealed class VaultWindow : Window, IDisposable
{
    private readonly Plugin _plugin;

    private string _newListName = string.Empty;
    private string _searchFilter = string.Empty;
    private string? _selectedList = null; // null = show all

    public VaultWindow(Plugin plugin) : base(
        "ItemVault###ItemVaultWindow",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(800, 600)
        };
    }

    public override void Draw()
    {
        var config = _plugin.Configuration;

        // --- Left panel: list selector ---
        ImGui.BeginChild("##lists", new Vector2(140, 0), true);

        if (ImGui.Selectable("All Items", _selectedList == null))
            _selectedList = null;

        ImGui.Separator();

        foreach (var list in config.KnownLists)
        {
            var count = config.LockedItems.Values.Count(e => e.ListName == list);
            if (ImGui.Selectable($"{list} ({count})##list_{list}", _selectedList == list))
                _selectedList = list;
        }

        ImGui.Separator();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##newlist", "New list...", ref _newListName, 64);
        if (ImGui.Button("+ Add List", new Vector2(-1, 0)) && !string.IsNullOrWhiteSpace(_newListName))
        {
            if (!config.KnownLists.Contains(_newListName))
            {
                config.KnownLists.Add(_newListName);
                config.Save();
            }
            _newListName = string.Empty;
        }

        ImGui.EndChild();
        ImGui.SameLine();

        // --- Right panel: items ---
        ImGui.BeginChild("##items", new Vector2(0, 0), true);

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Search items...", ref _searchFilter, 128);
        ImGui.Separator();

        var itemsToShow = config.LockedItems
            .Where(kvp => _selectedList == null || kvp.Value.ListName == _selectedList)
            .ToList();

        if (!string.IsNullOrWhiteSpace(_searchFilter))
        {
            var filter = _searchFilter.ToLowerInvariant();
            itemsToShow = itemsToShow
                .Where(kvp => GetItemName(kvp.Key).ToLowerInvariant().Contains(filter))
                .ToList();
        }

        if (itemsToShow.Count == 0)
        {
            ImGui.TextDisabled("No locked items here. Right-click an inventory item to lock it.");
        }

        uint? toRemove = null;

        foreach (var (itemId, entry) in itemsToShow)
        {
            var itemName = GetItemName(itemId);
            ImGui.PushID((int)itemId);

            ImGui.Text("🔒");
            ImGui.SameLine();
            ImGui.TextUnformatted(itemName);
            ImGui.SameLine();
            ImGui.TextDisabled($"(ID: {itemId})");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 160);
            ImGui.SetNextItemWidth(110);
            if (ImGui.BeginCombo($"##list_{itemId}", entry.ListName))
            {
                foreach (var listName in config.KnownLists)
                {
                    if (ImGui.Selectable(listName, entry.ListName == listName))
                    {
                        entry.ListName = listName;
                        config.Save();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("🗑"))
                toRemove = itemId;

            var note = entry.Note;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint($"##note_{itemId}", "Add a note...", ref note, 128))
            {
                entry.Note = note;
                config.Save();
            }

            ImGui.Separator();
            ImGui.PopID();
        }

        if (toRemove.HasValue)
        {
            config.LockedItems.Remove(toRemove.Value);
            config.Save();
        }

        ImGui.EndChild();
    }

    private string GetItemName(uint itemId)
    {
        try
        {
            var sheet = Plugin.DataManager.GetExcelSheet<Item>();
            var row = sheet?.GetRow(itemId);
            return row?.Name.ToString() ?? $"Unknown Item ({itemId})";
        }
        catch
        {
            return $"Item {itemId}";
        }
    }

    public void Dispose() { }
}
