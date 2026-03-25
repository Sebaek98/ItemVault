using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ItemVault.Handlers;
using ItemVault.Windows;

namespace ItemVault;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
	[PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
	[PluginService] internal static IFramework Framework { get; private set; } = null!;

    private const string CommandName = "/ivault";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("ItemVault");
    private VaultWindow VaultWindow { get; init; }

    private TooltipHandler TooltipHandler { get; init; }
    private ContextMenuHandler ContextMenuHandler { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        VaultWindow = new VaultWindow(this);
        WindowSystem.AddWindow(VaultWindow);

        TooltipHandler = new TooltipHandler(this);
        ContextMenuHandler = new ContextMenuHandler(this);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the ItemVault window to manage locked items."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleVaultUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleVaultUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleVaultUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleVaultUi;

        WindowSystem.RemoveAllWindows();
        VaultWindow.Dispose();

        TooltipHandler.Dispose();
        ContextMenuHandler.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args) => VaultWindow.Toggle();
    public void ToggleVaultUi() => VaultWindow.Toggle();
}
