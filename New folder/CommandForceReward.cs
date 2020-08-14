using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Command = OpenMod.Core.Commands.Command;

namespace Tortellio.Votifier
{

    [Command("forcereward", Priority = Priority.Normal)]
    [CommandDescription("Give vote reward to player.")]
    [CommandSyntax("[player]")]
    public class CommandForceReward : Command
    {
        private readonly IPluginAccessor<Votifier> m_PluginAccessor;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserManager m_UserManager;
        public CommandForceReward(IServiceProvider serviceProvider, IPluginAccessor<Votifier> pluginAccessor, IStringLocalizer stringLocalizer, IUserManager userManager) : base(
             serviceProvider)
        {
            m_PluginAccessor = pluginAccessor;
            m_StringLocalizer = stringLocalizer;
            m_UserManager = userManager;
        }
        protected override async Task OnExecuteAsync()
        {
            if (Context.Actor.Type == KnownActorTypes.Console)
            {
                if (Context.Parameters.Length == 0)
                {
                    throw new CommandWrongUsageException(Context);
                }
                else if (Context.Parameters.Length == 1)
                {
                    var target = await Context.Parameters.GetAsync<string>(0);
                    var targetPlayer = await m_UserManager.FindUserAsync(KnownActorTypes.Player, target, UserSearchMode.NameOrId);

                    if (targetPlayer == null)
                        throw new UserFriendlyException(m_StringLocalizer["FAIL:USER_NOT_FOUND", new { target }]);
                    else
                        m_PluginAccessor.Instance.ForceReward(UnturnedPlayer.FromName(command[0]));
                }

            }

            if (Context.Parameters.Length == 0)
            {
                m_PluginAccessor.Instance.Vote(PlayerTool.getPlayer(new Steamworks.CSteamID(ulong.Parse(Context.Actor.Id))));
            }

            if (caller is ConsolePlayer)
            {
                if (command.Length == 0)
                {
                    return;
                }
                else if (command.Length == 1)
                {
                    if (UnturnedPlayer.FromName(command[0]) != null)
                    {
                        Votifier.Instance.ForceReward(UnturnedPlayer.FromName(command[0]));
                    }
                }
                else if (command.Length == 2)
                {
                    if (UnturnedPlayer.FromName(command[0]) != null)
                    {
                        Votifier.Instance.ForceReward(UnturnedPlayer.FromName(command[0]), command[1]);
                    }
                }
            }
            else
            {
                if (command.Length == 0) Votifier.Instance.ForceReward((UnturnedPlayer)caller);
                else if (command.Length == 1) Votifier.Instance.ForceReward((UnturnedPlayer)caller, command[0]);
            }

            return Task.CompletedTask;
        }

        public void Execute(IRocketPlayer caller, params string[] command)
        {
        }
        public string Help
        {
            get { return "Force reward to player"; }
        }
        public string Name
        {
            get { return "forcereward"; }
        }
        public string Syntax
        {
            get { return ""; }
        }
        public List<string> Aliases
        {
            get { return new List<string>() { "freward" }; }
        }
        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "votifier.forcereward" };
            }
        }
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }
    }
}
