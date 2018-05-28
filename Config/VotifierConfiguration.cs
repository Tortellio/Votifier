using Rocket.Core.Configuration;

namespace fr34kyn01535.Votifier.Config
{
    public class VotifierConfiguration
    {
        [ConfigArray]
        public Service[] Services { get; set; } =
        {
            new Service("unturned-servers.net"),
            new Service("unturnedsl.com"),
            new Service("obs.erve.me")
        };

        public bool EnableRewardBundles { get; set; } = true;

        [ConfigArray]
        public RewardBundle[] RewardBundles { get; set; } =
        {
            new RewardBundle { Name="Survival", Rewards = new[] { new Reward(245, 1), new Reward(81, 2), new Reward(16, 1) }, Probability = 33 },
            new RewardBundle { Name="Brute Force", Rewards = new[] { new Reward(112, 1), new Reward(113, 3), new Reward(254, 3) }, Probability = 33 },
            new RewardBundle { Name="Watcher", Rewards = new[] { new Reward(109, 1), new Reward(111, 3), new Reward(236, 1) }, Probability = 33 }
        };

        [ConfigArray]
        public ServiceDefinition[] ServiceDefinitions { get; set; } =
        {
            new ServiceDefinition
            {
                Name = "unturned-servers.net",
                CheckHasVoted = "https://unturned-servers.net/api/?object=votes&element=claim&key={0}&steamid={1}",
                ReportSuccess ="https://unturned-servers.net/api/?action=post&object=votes&element=claim&key={0}&steamid={1}"
            }
            , new ServiceDefinition
            {
                Name = "unturnedsl.com",
                CheckHasVoted = "http://unturnedsl.com/api/dedicated/{0}/{1}",
                ReportSuccess = "http://unturnedsl.com/api/dedicated/post/{0}/{1}"
            }
            , new ServiceDefinition
            {
                Name = "obs.erve.me",
                CheckHasVoted = "http://api.observatory.rocketmod.net/?server={0}&steamid={1}",
                ReportSuccess = "http://api.observatory.rocketmod.net/?server={0}&steamid={1}&claim"
            }
        };
    }
}
