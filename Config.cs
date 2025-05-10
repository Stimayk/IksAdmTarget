using CounterStrikeSharp.API.Core;

namespace IksAdmTarget
{
    public class CommandsInfo
    {
        public string Command { get; set; } = "";
        public string CommandID { get; set; } = "";
        public string CommandFlag { get; set; } = "";
    }

    public class IksAdmTargetConfig : BasePluginConfig
    {
        public Dictionary<string, CommandsInfo> Commands { get; set; } = new()
        {
            ["Who"] = new() { Command = "css_who #{steamid64}", CommandID = "cmd_identify", CommandFlag = "b" },
            ["Kill"] = new() { Command = "css_slay #{steamid64}", CommandID = "cmd_slay", CommandFlag = "k" },
            ["Ban (forever)"] = new() { Command = "css_ban #{steamid64} 0 test", CommandID = "cmd_ban", CommandFlag = "b" },
            ["Kick"] = new() { Command = "css_kick #{steamid64} test", CommandID = "cmd_kick", CommandFlag = "k" }
        };

        public string AdminPermissionFlags { get; set; } = "z";
    }
}
