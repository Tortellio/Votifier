using fr34kyn01535.Votifier.Config;
using Rocket.Unturned.Player;

namespace fr34kyn01535.Votifier
{
    public class VoteResult
    {
        public UnturnedPlayer Caller { get; set; }
        public Service Service { get; set; }
        public ServiceDefinition ApiDefinition { get; set; }
        public bool GiveItemDirectly { get; set; }
        public string Result { get; set; }
    }
}