using Microsoft.Extensions.Localization;
using NuGet.Protocol.Plugins;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Core.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Command = OpenMod.Core.Commands.Command;

namespace Tortellio.Votifier.Commands
{
    [Command("reward", Priority = Priority.Normal)]
    [CommandDescription("Claim vote reward.")]
    [CommandSyntax("")]
    public class CommandReward : Command
    {
        private readonly IPluginAccessor<Votifier> m_PluginAccessor;
        public CommandReward(IServiceProvider serviceProvider, IPluginAccessor<Votifier> pluginAccessor) : base(
             serviceProvider)
        {
            m_PluginAccessor = pluginAccessor;
        }
        protected override Task OnExecuteAsync()
        {
            if (Context.Actor.Type == KnownActorTypes.Console && Context.Parameters.Length != 0)
                throw new CommandWrongUsageException(Context);

            if (Context.Parameters.Length == 0)
            {
                m_PluginAccessor.Instance.Vote(PlayerTool.getPlayer(new Steamworks.CSteamID(ulong.Parse(Context.Actor.Id))));
            }

            return Task.CompletedTask;
        }
    }
}
