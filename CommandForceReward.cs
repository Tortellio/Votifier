using Rocket.API;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fr34kyn01535.Votifier
{
    public class CommandForceReward : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, params string[] command)
        {
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
