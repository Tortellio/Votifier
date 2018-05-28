using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using fr34kyn01535.Votifier.Config;
using Rocket.API.DependencyInjection;
using Rocket.API.Eventing;
using Rocket.API.Scheduler;
using Rocket.API.User;
using Rocket.Core.I18N;
using Rocket.Core.Player.Events;

namespace fr34kyn01535.Votifier
{
    public class Votifier : Plugin<VotifierConfiguration>, IEventListener<PlayerConnectedEvent>
    {
        private readonly IUserManager _proxiedUserManager;
        private readonly ITaskScheduler _scheduler;
        private readonly Queue<VoteResult> _queue;

        private ITask _updateTask;

        public Votifier(IDependencyContainer container, IUserManager proxiedUserManager, ITaskScheduler scheduler) : base("Votifier", container)
        {
            _proxiedUserManager = proxiedUserManager;
            _scheduler = scheduler;
            _queue = new Queue<VoteResult>();
        }

        protected override void OnLoad(bool isFromReload)
        {
            base.OnLoad(isFromReload);
            EventManager.AddEventListener(this, this);
            _updateTask = _scheduler.ScheduleEveryAsyncFrame(this, UpdateTask);
        }

        protected override void OnUnload()
        {
            _updateTask?.Cancel();
        }

        void Votifier_OnPlayerVoted(UnturnedPlayer player, ServiceDefinition definition)
        {
            int propabilysum = ConfigurationInstance.RewardBundles.Sum(p => p.Probability);

            RewardBundle bundle = new RewardBundle();

            if (propabilysum != 0)
            {
                Random r = new Random();

                int i = 0, diceRoll = r.Next(0, propabilysum);

                foreach (RewardBundle b in ConfigurationInstance.RewardBundles)
                {
                    if (diceRoll > i && diceRoll <= i + b.Probability)
                    {
                        bundle = b;
                        break;
                    }
                    i = i + b.Probability;
                }
            }
            else
            {
                Logger.LogWarning(Translations.Get("no_rewards_found"));
                return;
            }

            foreach (Reward reward in bundle.Rewards)
            {

                if (!player.GiveItem(reward.ItemId, reward.Amount))
                {
                    Logger.LogError(Translations.Get("vote_give_error_message", player.CharacterName, reward.ItemId, reward.Amount));
                }
            }

            _proxiedUserManager.BroadcastLocalized(Translations, "vote_success_message", player.CharacterName, definition.Name, bundle.Name);
        }

        public override Dictionary<string, string> DefaultTranslations => new Dictionary<string, string>
        {
            {"no_apikeys_message","No apikeys supplied."},
            {"api_unknown_message", "The API for {0} is unknown"},
            {"api_down_message","Can't reach {0}, is it down?!"},
            {"not_yet_voted","You have not yet voted for this server on: {0}"},
            {"no_rewards_found","Failed finding any rewardbundles"},
            {"vote_give_error_message","Failed giving a item to {0} ({1},{2})"},
            {"vote_success_message","{0} voted on {1} and received the \"{2}\" bundle"},
            {"vote_pending_message","You have an outstanding reward for your vote on {0}"},
            {"vote_due_message","You have already voted for this server on {0}, Thanks!"}
        };

        public void Vote(UnturnedPlayer caller, bool giveItemDirectly = true)
        {
            try
            {
                if (ConfigurationInstance.Services.FirstOrDefault(s => !String.IsNullOrEmpty(s.APIKey)) == null)
                {
                    Logger.LogError(Translations.Get("no_apikeys_message")); return;
                }

                List<Service> services = ConfigurationInstance.Services.Where(s => !String.IsNullOrEmpty(s.APIKey)).ToList();


                foreach (Service service in services)
                {
                    ServiceDefinition apiDefinition = ConfigurationInstance.ServiceDefinitions.FirstOrDefault(s => s.Name == service.Name);
                    if (apiDefinition == null)
                    {
                        Logger.LogWarning(Translations.Get("api_unknown_message", service.Name)); return;
                    }
                    try
                    {
                        VotifierWebclient wc = new VotifierWebclient();
                        wc.DownloadStringCompleted += (sender, e) => OnDownloadStringCompleted(e, caller, service, apiDefinition, giveItemDirectly);
                        wc.DownloadStringAsync(new Uri(String.Format(apiDefinition.CheckHasVoted, service.APIKey, caller.ToString())));
                    }
                    catch (TimeoutException)
                    {
                        Logger.LogError(Translations.Get("api_down_message", service.Name));
                        caller.User.SendLocalizedMessage(Translations, "api_down_message", service.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(null, ex);
            }
        }

        private void OnDownloadStringCompleted(System.Net.DownloadStringCompletedEventArgs e, UnturnedPlayer caller, Service service, ServiceDefinition apidefinition, bool giveItemDirectly)
        {
            VoteResult v = new VoteResult
            {
                Caller = caller,
                Result = e.Result,
                ApiDefinition = apidefinition,
                Service = service,
                GiveItemDirectly = giveItemDirectly
            };

            lock (_queue)
            {
                _queue.Enqueue(v);
            }
        }

        private void UpdateTask()
        {
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    VoteResult v = _queue.Dequeue();
                    HandleVote(v);
                }
            }
        }

        private void HandleVote(VoteResult result)
        {
            switch (result.Result)
            {
                case "0":
                    result.Caller.User.SendLocalizedMessage(Translations, "not_yet_voted", result.Service.Name);
                    break;
                case "1":
                    if (result.GiveItemDirectly)
                    {
                        if (!ConfigurationInstance.EnableRewardBundles)
                            return;

                        EventManager.Emit(this, new PlayerVotedEvent(result.Caller, result.ApiDefinition), (e) =>
                        {
                            var @event = (PlayerVotedEvent) e;
                            if (@event.IsCancelled)
                                return;

                            Votifier_OnPlayerVoted((UnturnedPlayer)@event.Player, @event.Service);
                        });

                        new VotifierWebclient().DownloadStringAsync(new Uri(String.Format(result.ApiDefinition.ReportSuccess, result.Service.APIKey, result.Caller.ToString())));
                        return;
                    }
                    else
                    {
                        result.Caller.User.SendLocalizedMessage(Translations, "vote_pending_message", result.Service.Name);
                        return;
                    }
                case "2":
                    result.Caller.User.SendLocalizedMessage(Translations, "vote_due_message", result.Service.Name);
                    break;
            }
        }

        public void HandleEvent(IEventEmitter emitter, PlayerConnectedEvent @event)
        {
            if (@event.Player is UnturnedPlayer player)
                Vote(player, false);
        }
    }
}
