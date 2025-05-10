using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi;
using Microsoft.Extensions.Logging;

namespace IksAdmTarget
{
    public class IksAdmTarget : BasePlugin, IPluginConfig<IksAdmTargetConfig>
    {
        public override string ModuleName => "[IKS] Admin Target";
        public override string ModuleDescription => "";
        public override string ModuleAuthor => "E!N";
        public override string ModuleVersion => "v1.0.0";

        private const string AdminPermission = "other.admtarget";

        public IksAdmTargetConfig Config { get; set; } = new();

        public void OnConfigParsed(IksAdmTargetConfig config)
        {
            Config = config;
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            AddCommand("css_target", "", OnCommandTarget);
            AddCommand("css_tar", "", OnCommandTarget);

            AdminModule.Api.MenuOpenPre += OnMenuOpenPre;
            AdminModule.Api.RegisterPermission(AdminPermission, Config.AdminPermissionFlags);

            foreach (KeyValuePair<string, CommandsInfo> entry in Config.Commands.Where(e => !string.IsNullOrEmpty(e.Value.Command)))
            {
                AdminModule.Api.RegisterPermission(entry.Value.CommandID, entry.Value.CommandFlag);
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveCommand("css_target", OnCommandTarget);
            RemoveCommand("css_tar", OnCommandTarget);
            AdminModule.Api.MenuOpenPre -= OnMenuOpenPre;
        }

        private HookResult OnMenuOpenPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu)
        {
            if (menu.Id != "iksadmin:menu:main" || player.PawnIsAlive)
            {
                return HookResult.Continue;
            }

            menu.AddMenuOption("admtarget", Localizer["MenuOption.admtarget"],
                (_, _) => OpenTargetMenu(player, menu),
                viewFlags: AdminUtils.GetCurrentPermissionFlags(AdminPermission));

            return HookResult.Continue;
        }

        private void OpenTargetMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null!)
        {
            IDynamicMenu menu = AdminModule.Api.CreateMenu("admtarget.main", Localizer["MenuOption.TargetSelect"], backMenu: backMenu);
            CCSPlayerController? target = GetTarget(caller);

            menu.AddMenuOption("admtarget.reload", Localizer["MenuOption.ReloadTarget"], (_, _) => OpenTargetMenu(caller, backMenu));

            if (target is not null)
            {
                menu.AddMenuOption("admtarget.target", Localizer["MenuOption.Target", target.PlayerName],
                    (_, _) => OpenControlMenu(caller, target, menu));
            }

            menu.Open(caller);
        }

        private void OpenControlMenu(CCSPlayerController caller, CCSPlayerController target, IDynamicMenu? backMenu = null!)
        {
            IDynamicMenu menu = AdminModule.Api.CreateMenu("admtarget.control", Localizer["MenuOption.Target", target.PlayerName], backMenu: backMenu);
            List<KeyValuePair<string, CommandsInfo>> validCommands = [.. Config.Commands.Where(e => !string.IsNullOrEmpty(e.Value.Command))];

            for (int i = 0; i < validCommands.Count; i++)
            {
                (string keyword, CommandsInfo info) = validCommands[i];
                string finalCommand = ReplacePlaceholders(info.Command, target);
                string commandId = string.IsNullOrEmpty(info.CommandID) ? $"admtarget.cmd_{i}" : info.CommandID;

                menu.AddMenuOption(commandId, keyword, (_, _) =>
                {
                    Server.ExecuteCommand(finalCommand);
                    Logger.LogInformation($"Admin {caller.AuthorizedSteamID?.SteamId64} used {commandId} on {target.AuthorizedSteamID?.SteamId64}");
                }, viewFlags: AdminUtils.GetCurrentPermissionFlags(commandId));
            }

            menu.Open(caller);
        }

        private static string ReplacePlaceholders(string command, CCSPlayerController target)
        {
            return command
                .Replace("{steamid64}", target.AuthorizedSteamID?.SteamId64.ToString())
                .Replace("{steamid3}", target.AuthorizedSteamID?.SteamId3.ToString())
                .Replace("{steamid2}", target.AuthorizedSteamID?.SteamId2.ToString())
                .Replace("{ip}", target.IpAddress)
                .Replace("{uid}", target.UserId.ToString())
                .Replace("{name}", target.PlayerName);
        }

        public static CCSPlayerController? GetTarget(CCSPlayerController caller)
        {
            CCSPlayerController? observerTarget = caller.Pawn.Value?.ObserverServices?.ObserverTarget?.Value?
                .As<CCSPlayerPawn>().OriginalController.Value;

            return observerTarget?.IsValid == true ? observerTarget : null;
        }

        public void OnCommandTarget(CCSPlayerController? player, CommandInfo command)
        {
            if (player is { PawnIsAlive: false } && AdminUtils.HasPermissions(player, Config.AdminPermissionFlags))
            {
                OpenTargetMenu(player);
            }
        }
    }
}