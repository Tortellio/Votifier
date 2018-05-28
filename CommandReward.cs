using Rocket.API;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Rocket.API.Commands;
using Rocket.API.Plugins;

namespace fr34kyn01535.Votifier
{
    public class CommandReward : ICommand
    {
        public string Name => "reward";
        public string Summary => "Gets the rewards for voting";
        public string Description => null;
        public string Permission => null;
        public string Syntax => "";
        public IChildCommand[] ChildCommands => null;
        public string[] Aliases => new[] { "vote" };

        private readonly Votifier _votifier;

        public CommandReward(IPlugin plugin)
        {
            _votifier = (Votifier) plugin;
        }
        public bool SupportsUser(Type user)
        {
            return typeof(UnturnedUser).IsAssignableFrom(user);
        }

        public void Execute(ICommandContext context)
        {
            _votifier.Vote(((UnturnedUser)context.User).Player);
        }
    }
}
