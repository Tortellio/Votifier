using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using Random = System.Random;

namespace fr34kyn01535.Votifier
{
    public class Votifier : RocketPlugin<Config>
    {
        public static Votifier Instance;
        public delegate void PlayerVotedEvent(UnturnedPlayer player, ServiceDefinition definition);
        public event PlayerVotedEvent OnPlayerVoted;
        //private readonly List<VoteResult> voteResult = new List<VoteResult>();
        private readonly Queue<VoteResult> queue = new Queue<VoteResult>(); 
        public Dictionary<CSteamID, List<string>> PlayerReward;
        public const string VFILEPATH = "Plugins/Votifier/data.xml";
        internal Color MsgColor;
        protected override void Load()
        {
            Instance = this;
            MsgColor = UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor, Color.green);
            U.Events.OnPlayerConnected += OnPlayerConnected;
            if (Configuration.Instance.EnableRewardBundles)
            {
                OnPlayerVoted += Votifier_OnPlayerVoted;
            }
            StartCoroutine((IEnumerator)CheckQueue());

            if (!File.Exists(VFILEPATH))
            {
                Logger.Log("Datafile not found, creating one now...");
                File.Create(VFILEPATH).Dispose();
                PlayerReward = new Dictionary<CSteamID, List<string>>();
            }
            else
            {
                try
                {
                    PlayerReward = DeserializePlayerDict();
                }
                catch (XmlException)
                {
                    Logger.Log(" Failed to deserialize datafile. This is normal for a first run.");
                    Logger.Log(" Delete data.xml in plugin folder if this keeps on happening.");
                    Logger.Log(" MAKE SURE THAT THE PLUGIN IS PROPERLY UNLOADED.");
                    PlayerReward = new Dictionary<CSteamID, List<string>>();
                }
            }

        }
        protected override void Unload()
        {
            Instance = null;
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            if (Configuration.Instance.EnableRewardBundles)
            {
                OnPlayerVoted -= Votifier_OnPlayerVoted;
            }
            StopCoroutine((IEnumerator)CheckQueue());

            Logger.LogWarning("Saving player reward data to a file...");
            SerializePlayerDict(PlayerReward);
            Logger.LogWarning("Done!");
        }
        public static void SerializePlayerDict(Dictionary<CSteamID, List<string>> dict)
        {
            List<VPlayer> players = new List<VPlayer>();
            foreach (KeyValuePair<CSteamID, List<string>> kv in dict)
            {
                players.Add(new VPlayer(kv.Key.m_SteamID, kv.Value));
            }
            XmlSerializer serializer = new XmlSerializer(typeof(List<VPlayer>));
            using (TextWriter stream = new StreamWriter(VFILEPATH, false))
            {
                serializer.Serialize(stream, players);
            }
        }
        public static Dictionary<CSteamID, List<string>> DeserializePlayerDict()
        {
            Dictionary<CSteamID, List<string>> dict = new Dictionary<CSteamID, List<string>>();
            XmlSerializer serializer = new XmlSerializer(typeof(List<VPlayer>));
            List<VPlayer> players;
            using (TextReader stream = new StreamReader(VFILEPATH))
            {
                players = (List<VPlayer>)serializer.Deserialize(stream);
            }
            foreach (VPlayer player in players)
            {
                dict[new CSteamID(player.SteamID)] = player.OneTimeReward;
            }
            return dict;
        }
        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList() {
                    {"no_apikeys_message","No apikeys supplied."},
                    {"api_unknown_message", "The API for {0} is unknown"},
                    {"api_down_message","Can't reach {0}, website is down!"},
                    {"not_yet_voted","You have not yet voted for this server on: {0}"},
                    {"no_rewards_found","Failed finding any reward bundles"},
                    {"vote_give_error_message","Failed giving a {3} to {0} ({1},{2})"},
                    {"vote_success_message","{0} voted on {1} and received the \"{2}\" bundle! Vote now!"},
                    {"vote_success_onetime_message","{0} voted on {1} and received the \"{2}\" bundle (One-Time Only)! Vote now!"},
                    {"vote_pending_message","You have an outstanding reward for your vote on {0}! Type /reward"},
                    {"vote_due_message","You have already voted for this server on {0}, Thank you!"}
                };
            }
        }
        private void OnPlayerConnected(UnturnedPlayer player)
        {
            //Trigger Vote
            Vote(player, false);
        }
        private void Votifier_OnPlayerVoted(UnturnedPlayer player, ServiceDefinition definition)
        {
            int probSum = Instance.Configuration.Instance.RewardBundles.Sum(p => p.Probability);
            RewardBundle bundle = new RewardBundle();
            if (probSum != 0)
            {
                Random r = new Random();
                int i = 0, diceRoll = r.Next(0, probSum);
                foreach (RewardBundle b in Instance.Configuration.Instance.RewardBundles)
                {
                    if (diceRoll > i && diceRoll <= i + b.Probability)
                    {
                        if (PlayerReward.TryGetValue(player.CSteamID, out var reward))
                        {
                            if (reward.Contains(b.Name))
                            {
                                i += b.Probability;
                                continue;
                            }
                            else
                            {
                                bundle = b;
                                if (bundle.OneTimeOnly)
                                {
                                    PlayerReward[player.CSteamID].Add(bundle.Name);
                                }
                                break;
                            }
                        }
                        else
                        {
                            bundle = b;
                            if (bundle.OneTimeOnly)
                            {
                                PlayerReward.Add(player.CSteamID, new List<string> { bundle.Name });
                            }
                            break;
                        }
                    }
                    i += b.Probability;
                }
            }
            else
            {
                Logger.Log(Instance.Translations.Instance.Translate("no_rewards_found"));
                return;
            }

            // Experience Reward
            player.Experience += bundle.Experiences;

            // Item Reward
            foreach (var item in bundle.Items)
            {
                if (!player.GiveItem(item.ItemID, item.Amount))
                {
                    Logger.Log(Instance.Translations.Instance.Translate("vote_give_error_message", player.CharacterName, item.ItemID, item.Amount, "item"));
                }
            }

            // Vehicle Reward
            foreach (var vehicle in bundle.Vehicles)
            {
                for (int i = 0; i < vehicle.Amount; i++)
                {
                    InteractableVehicle status = null;
                    Vector3 vector = player.Player.transform.position + player.Player.transform.forward * 6f;
                    Physics.Raycast(vector + Vector3.up * 16f, Vector3.down, out RaycastHit raycastHit, 32f, RayMasks.BLOCK_VEHICLE);
                    if (raycastHit.collider != null)
                    {
                        vector.y = raycastHit.point.y + 16f;
                    }
                    status = VehicleManager.spawnLockedVehicleForPlayerV2(vehicle.VehicleID, vector, player.Player.transform.rotation, player.Player);
                    if (status == null)
                    {
                        Logger.Log(Instance.Translations.Instance.Translate("vote_give_error_message", player.CharacterName, vehicle.VehicleID, vehicle.Amount, "vehicle"));
                    }
                }
            }

            // Command Reward
            foreach (var command in bundle.Commands)
            {
                bool yes = true;
                CommandWindow.onCommandWindowInputted(command.Replace("{playerid}", player.CSteamID.m_SteamID.ToString().Replace("{playername}", player.CharacterName)), ref yes);
            }
            if (bundle.OneTimeOnly)
            {
                if (Configuration.Instance.GlobalRewardAnnouncement) Say(Translations.Instance.Translate("vote_success_onetime_message", player.CharacterName, definition.Name, bundle.Name), MsgColor, Configuration.Instance.MessageSuccessIconUrl);
                else Say(player, Instance.Translations.Instance.Translate("vote_success_onetime_message", player.CharacterName, definition.Name, bundle.Name), MsgColor, Configuration.Instance.MessageSuccessIconUrl);
            }
            else
            {
                if (Configuration.Instance.GlobalRewardAnnouncement) Say(Translations.Instance.Translate("vote_success_message", player.CharacterName, definition.Name, bundle.Name), MsgColor, Configuration.Instance.MessageSuccessIconUrl);
                else Say(player, Instance.Translations.Instance.Translate("vote_success_message", player.CharacterName, definition.Name, bundle.Name), MsgColor, Configuration.Instance.MessageSuccessIconUrl);
            }
        }
        internal void ForceReward(UnturnedPlayer player, string reward = "$$random$$")
        {
            if (reward == "$$random$$")
            {
                Votifier_OnPlayerVoted(player, new ServiceDefinition { Name = "Admin" });
            }
            else
            {
                RewardBundle bundle = null;
                if (Configuration.Instance.RewardBundles.Count != 0)
                {
                    bundle = Configuration.Instance.RewardBundles.FirstOrDefault(r => r.Name == reward);
                }
                else
                {
                    Logger.Log(Instance.Translations.Instance.Translate("no_rewards_found"));
                    return;
                }

                // Experience Reward
                if (bundle.Experiences != 0) player.Experience += bundle.Experiences;

                // Item Reward
                foreach (var item in bundle.Items)
                {
                    if (!player.GiveItem(item.ItemID, item.Amount))
                    {
                        Logger.Log(Instance.Translations.Instance.Translate("vote_give_error_message", player.CharacterName, item.ItemID, item.Amount, "item"));
                    }
                }

                // Vehicle Reward
                foreach (var vehicle in bundle.Vehicles)
                {
                    for (int i = 0; i < vehicle.Amount; i++)
                    {
                        InteractableVehicle status = null;
                        Vector3 vector = player.Player.transform.position + player.Player.transform.forward * 6f;
                        Physics.Raycast(vector + Vector3.up * 16f, Vector3.down, out RaycastHit raycastHit, 32f, RayMasks.BLOCK_VEHICLE);
                        if (raycastHit.collider != null)
                        {
                            vector.y = raycastHit.point.y + 16f;
                        }
                        status = VehicleManager.spawnLockedVehicleForPlayerV2(vehicle.VehicleID, vector, player.Player.transform.rotation, player.Player);
                        if (status == null)
                        {
                            Logger.Log(Instance.Translations.Instance.Translate("vote_give_error_message", player.CharacterName, vehicle.VehicleID, vehicle.Amount, "vehicle"));
                        }
                    }
                }

                // Command Reward
                foreach (var command in bundle.Commands)
                {
                    bool yes = true;
                    CommandWindow.onCommandWindowInputted(command.Replace("{playerid}", player.CSteamID.m_SteamID.ToString().Replace("{playername}", player.CharacterName)), ref yes);
                }
            }
        }
        internal void Vote(UnturnedPlayer caller, bool giveItemDirectly = true)
        {
            try
            {
                if (Configuration.Instance.Services.Where(s => !string.IsNullOrEmpty(s.APIKey)).FirstOrDefault() == null)
                {
                    Logger.Log(Translations.Instance.Translate("no_apikeys_message")); return;
                }

                List<Service> services = Configuration.Instance.Services.Where(s => !string.IsNullOrEmpty(s.APIKey)).ToList();
                foreach (Service service in services)
                {
                    ServiceDefinition apidefinition = Configuration.Instance.ServiceDefinitions.Where(s => s.Name == service.Name).FirstOrDefault();
                    if (apidefinition == null) { Logger.Log(Translations.Instance.Translate("api_unknown_message", service.Name)); return; }
                    try
                    {
                        VotifierWebclient wc = new VotifierWebclient();
                        wc.DownloadStringCompleted += (sender, e) => Wc_DownloadStringCompleted(e, caller, service,apidefinition, giveItemDirectly);
                        wc.DownloadStringAsync(new Uri(string.Format(apidefinition.CheckHasVoted, service.APIKey, caller.ToString())));
                    }
                    catch (TimeoutException)
                    {
                        Logger.Log(Translations.Instance.Translate("api_down_message", service.Name));
                        Say(caller, Translations.Instance.Translate("api_down_message", service.Name), MsgColor, Configuration.Instance.MessageFailedIconUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
        private void Wc_DownloadStringCompleted(System.Net.DownloadStringCompletedEventArgs e, UnturnedPlayer _caller, Service _service,ServiceDefinition _apidefinition, bool _giveItemDirectly)
        {
            VoteResult v = new VoteResult() { caller = _caller, result = e.Result, apidefinition = _apidefinition, service = _service, giveItemDirectly = _giveItemDirectly };
            lock (queue)
            {
                queue.Enqueue(v);
            }
        }
        private void HandleVote(VoteResult result) {
            UnturnedPlayer p = result.caller;

#if DEBUG
            Console.WriteLine("Webserver returns: " +result.result);
#endif

            switch (result.result)
            {
                case "0":
                    Say(result.caller, Translations.Instance.Translate("not_yet_voted", result.service.Name), MsgColor, Configuration.Instance.MessageFailedIconUrl);
                    break;
                case "1":
                    if (result.giveItemDirectly)
                    {
                        OnPlayerVoted?.Invoke(result.caller, result.apidefinition);
                        new VotifierWebclient().DownloadStringAsync(new Uri(string.Format(result.apidefinition.ReportSuccess, result.service.APIKey, result.caller.CSteamID.m_SteamID.ToString())));
                        return;
                    }
                    else
                    {
                        Say(result.caller, Translations.Instance.Translate("vote_pending_message", result.service.Name), MsgColor, Configuration.Instance.MessageFailedIconUrl);
                        return;
                    }
                case "2":
                    Say(result.caller, Instance.Translations.Instance.Translate("vote_due_message", result.service.Name), MsgColor, Configuration.Instance.MessageFailedIconUrl);
                    break;
            }
        }
        private IEnumerator<WaitForSeconds> CheckQueue()
        {
            while (Instance != null){
                if (queue.Count > 0)
                {
                    VoteResult v = queue.Dequeue();
                    HandleVote(v);
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
        public List<string> WrapMessage(string text)
        {
            if (text.Length == 0) return new List<string>();
            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = "";
            var maxLength = 250;
            foreach (var currentWord in words)
            {

                if ((currentLine.Length > maxLength) ||
                    ((currentLine.Length + currentWord.Length) > maxLength))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                if (currentLine.Length > 0)
                    currentLine += " " + currentWord;
                else
                    currentLine += currentWord;

            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }
            return lines;
        }
        public void Say(CSteamID CSteamID, string message, Color color, string imageURL)
        {
            if (CSteamID.ToString() == "0")
            {
                Logger.Log(message, ConsoleColor.Gray);
                return;
            }
            SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(CSteamID);
            foreach (string text in UnturnedChat.wrapMessage(message))
            {
                ChatManager.serverSendMessage(text, color, null, steamPlayer, EChatMode.SAY, imageURL, true);
            }
        }
        public void Say(IRocketPlayer player, string message, Color color, string imageURL)
        {
            if (player is ConsolePlayer)
            {
                Logger.Log(message, ConsoleColor.Gray);
                return;
            }
            Say(new CSteamID(ulong.Parse(player.Id)), message, color, imageURL);
        }
        public void Say(string message, Color color, string imageURL)
        {
            foreach (var m in WrapMessage(message))
            {
                ChatManager.serverSendMessage(m, color, fromPlayer: null, toPlayer: null, mode: EChatMode.GLOBAL, iconURL: imageURL, useRichTextFormatting: true);
            }
        }
    }
    public class VoteResult
    {
        public UnturnedPlayer caller;
        public Service service;
        public ServiceDefinition apidefinition;
        public bool giveItemDirectly;
        public string result;
        public VoteResult() { }
    }
    public class VPlayer
    {
        [XmlAttribute("SteamID")]
        public ulong SteamID;
        [XmlArray("OneTimeReward"), XmlArrayItem(ElementName = "Reward")]
        public List<string> OneTimeReward;
        public VPlayer() { }
        public VPlayer(ulong steamId, List<string> oneTimeReward)
        {
            SteamID = steamId;
            OneTimeReward = oneTimeReward;
        }
    }
}
