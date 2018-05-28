using Rocket.Core.Configuration;

namespace fr34kyn01535.Votifier.Config
{
    public class RewardBundle
    {
        public int Probability { get; set; }

        public string Name { get; set; }

        [ConfigArray]
        public Reward[] Rewards { get; set; }
    }
}