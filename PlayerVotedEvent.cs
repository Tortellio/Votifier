using fr34kyn01535.Votifier.Config;
using Rocket.API.Eventing;
using Rocket.API.Player;
using Rocket.Core.Player.Events;

namespace fr34kyn01535.Votifier
{
    public class PlayerVotedEvent : PlayerEvent, ICancellableEvent
    {
        public ServiceDefinition Service { get; }
        public PlayerVotedEvent(IPlayer player, ServiceDefinition service) : base(player)
        {
            Service = service;
        }

        public PlayerVotedEvent(IPlayer player, ServiceDefinition service, bool global = true) : base(player, global)
        {
            Service = service;
        }

        public PlayerVotedEvent(IPlayer player, ServiceDefinition service, EventExecutionTargetContext executionTarget = EventExecutionTargetContext.Sync, bool global = true) : base(player, executionTarget, global)
        {
            Service = service;
        }

        public bool IsCancelled { get; set; }
    }
}