using Rocket.API;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fr34kyn01535.Votifier
{
    public class CommandReward : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, params string[] command)
        {
            Votifier.Instance.Vote((UnturnedPlayer)caller);
        }
        public string Help
        {
            get { return "Get rewards for voting"; }
        }
        public string Name
        {
            get { return "reward"; }
        }
        public string Syntax
        {
            get { return ""; }
        }
        public List<string> Aliases {
            get { return new List<string>() { "vote" }; }
        }
        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "votifier.reward" };
            }
        }
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }
    }
}
