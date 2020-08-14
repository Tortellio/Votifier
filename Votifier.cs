using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Cysharp.Threading.Tasks;
using OpenMod.Unturned.Plugins;
using OpenMod.API.Plugins;
using System.Net;
using OpenMod.Core.Eventing;
using SDG.Unturned;
using OpenMod.API.Eventing;
using OpenMod.Core.Users.Events;
using OpenMod.API.Prioritization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using OpenMod.API.Commands;
using UnityEngine;
using Steamworks;
using System.Collections;
using OpenMod.API.Ioc;
using Microsoft.Extensions.DependencyInjection;

[assembly: PluginMetadata("Tortellio.Votifier", DisplayName = "Votifier")]
namespace Tortellio.Votifier
{
    public class Votifier : OpenModUnturnedPlugin
    {
        internal readonly IConfiguration m_Configuration;
        internal readonly IStringLocalizer m_StringLocalizer;
        private readonly ILogger<Votifier> m_Logger;
        public delegate void PlayerVotedEvent(Player player, ServiceDefinition definition);
        public event PlayerVotedEvent OnPlayerVoted;
        internal Queue<VoteResult> queue = new Queue<VoteResult>();
        internal Color MsgColor;
        public Votifier(
            IConfiguration configuration, 
            IStringLocalizer stringLocalizer,
            ILogger<Votifier> logger, 
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Configuration = configuration;
            m_StringLocalizer = stringLocalizer;
            m_Logger = logger;
        }
        protected override async UniTask OnLoadAsync()
        {
            await UniTask.SwitchToMainThread();

            MsgColor = GetColorFromName(m_Configuration["MessageColor"], Color.green);

            if (m_Configuration.GetSection("EnableRewardBundles").Get<bool>())
            {
                OnPlayerVoted += Votifier_OnPlayerVoted;
            }
            Provider.onEnemyConnected += OnPlayerConnected;

            await UniTask.SwitchToThreadPool();

            m_Logger.LogInformation("Votifier by Tortellio has been unloaded.");
        }

        protected override UniTask OnUnloadAsync()
        {
            if (m_Configuration.GetSection("EnableRewardBundles").Get<bool>())
            {
                OnPlayerVoted -= Votifier_OnPlayerVoted;
            }
            Provider.onEnemyConnected -= OnPlayerConnected;

            m_Logger.LogInformation("Votifier by Tortellio has been unloaded.");

            return UniTask.CompletedTask;
        }
        private async void OnPlayerConnected(SteamPlayer splayer)
        {
            await UniTask.SwitchToMainThread();

            Vote(PlayerTool.getPlayer(splayer.playerID.steamID), false);

            await UniTask.SwitchToThreadPool();
        }
        private void Wc_DownloadStringCompleted(DownloadStringCompletedEventArgs e, Player _caller, Service _service, ServiceDefinition _apidefinition, bool _giveItemDirectly)
        {
            VoteResult v = new VoteResult() { caller = _caller, result = e.Result, apidefinition = _apidefinition, service = _service, giveItemDirectly = _giveItemDirectly };
            lock (queue)
            {
                queue.Enqueue(v);
            }
        }
        internal void HandleVote(VoteResult result)
        {
            Player p = result.caller;

#if DEBUG
            Console.WriteLine("Webserver returns: " + result.result);
#endif

            switch (result.result)
            {
                case "0":
                    Say(result.caller, m_StringLocalizer["not_yet_voted", result.service.Name], MsgColor, m_Configuration["MessageFailedIconUrl"]);
                    break;
                case "1":
                    if (result.giveItemDirectly)
                    {
                        OnPlayerVoted?.Invoke(result.caller, result.apidefinition);
                        new VotifierWebclient().DownloadStringAsync(new Uri(string.Format(result.apidefinition.ReportSuccess, result.service.APIKey, result.caller.channel.owner.playerID.steamID.m_SteamID.ToString())));
                        return;
                    }
                    else
                    {
                        Say(result.caller, m_StringLocalizer["vote_pending_message", result.service.Name], MsgColor, m_Configuration["MessageFailedIconUrl"]);
                        return;
                    }
                case "2":
                    Say(result.caller, m_StringLocalizer["vote_due_message", result.service.Name], MsgColor, m_Configuration["MessageFailedIconUrl"]);
                    break;
            }
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
                        bundle = b;
                        break;
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
            player.Experience += bundle.Reward.Experiences;

            // Item Reward
            foreach (var item in bundle.Reward.Items)
            {
                if (!player.GiveItem(item.ItemID, item.Amount))
                {
                    Logger.Log(Instance.Translations.Instance.Translate("vote_give_error_message", player.CharacterName, item.ItemID, item.Amount, "item"));
                }
            }

            // Vehicle Reward
            foreach (var vehicle in bundle.Reward.Vehicles)
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
            foreach (var command in bundle.Reward.Commands)
            {
                bool yes = true;
                CommandWindow.onCommandWindowInputted(command.Replace("{playerid}", player.CSteamID.m_SteamID.ToString().Replace("{playername}", player.CharacterName)), ref yes);
            }

            if (Configuration.Instance.GlobalRewardAnnouncement) Say(Translations.Instance.Translate("vote_success_message", player.CharacterName, definition.Name, bundle.Name), MsgColor, Configuration.Instance.MessageSuccessIconUrl);
            else Say(player, Instance.Translations.Instance.Translate("vote_success_message", player.CharacterName, definition.Name, bundle.Name), MsgColor, Configuration.Instance.MessageSuccessIconUrl);
        }
        internal void ForceReward(Player player, string reward = "$$random$$")
        {
            if (reward == "$$random$$")
            {
                Votifier_OnPlayerVoted(player, new ServiceDefinition { Name = "Admin" });
            }
            else
            {
                RewardBundle bundle = null;
                if (m_Configuration.GetSection("RewardBundles").Get<List<RewardBundle>>().Count != 0)
                {
                    bundle = m_Configuration.GetSection("RewardBundles").Get<List<RewardBundle>>().FirstOrDefault(r => r.Name == reward);
                }
                else
                {
                    Logger.Log(Instance.Translations.Instance.Translate("no_rewards_found"));
                    return;
                }

                // Experience Reward
                player.Experience += bundle.Reward.Experiences;

                // Item Reward
                foreach (var item in bundle.Reward.Items)
                {
                    if (!player.GiveItem(item.ItemID, item.Amount))
                    {
                        m_Logger.LogInformation(m_StringLocalizer["FAIL:VOTE_GIVE_ERROR_MESSAGE", player.CharacterName, item.ItemID, item.Amount, "item"]);
                    }
                }

                // Vehicle Reward
                foreach (var vehicle in bundle.Reward.Vehicles)
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
                        status = VehicleManager.spawnLockedVehicleForPlayer(vehicle.VehicleID, vector, player.Player.transform.rotation, player.Player);
                        if (status == null)
                        {
                            m_Logger.LogInformation(m_StringLocalizer["FAIL:VOTE_GIVE_ERROR_MESSAGE", player.CharacterName, vehicle.VehicleID, vehicle.Amount, "vehicle"]);
                        }
                    }
                }

                // Command Reward
                foreach (var command in bundle.Reward.Commands)
                {
                    bool yes = true;
                    CommandWindow.onCommandWindowInputted(command.Replace("{playerid}", player.CSteamID.m_SteamID.ToString().Replace("{playername}", player.CharacterName)), ref yes);
                }
            }
        }
        internal void Vote(Player caller, bool giveItemDirectly = true)
        {
            try
            {
                if (m_Configuration.GetSection("Services").Get<List<Service>>().Where(s => !string.IsNullOrEmpty(s.APIKey)).FirstOrDefault() == null)
                {
                    m_Logger.LogInformation(m_StringLocalizer["FAIL:NO_API_MESSAGE"]); 
                    return;
                }

                List<Service> services = m_Configuration.GetSection("Services").Get<List<Service>>().Where(s => !string.IsNullOrEmpty(s.APIKey)).ToList();
                foreach (Service service in services)
                {
                    ServiceDefinition apidefinition = m_Configuration.GetSection("Services").Get<List<ServiceDefinition>>().Where(s => s.Name == service.Name).FirstOrDefault();
                    if (apidefinition == null) { m_Logger.LogInformation(m_StringLocalizer["FAIL:API_UNKNOWN_MESSAGE", service.Name]); return; }
                    try
                    {
                        VotifierWebclient wc = new VotifierWebclient();
                        wc.DownloadStringCompleted += (sender, e) => Wc_DownloadStringCompleted(e, caller, service, apidefinition, giveItemDirectly);
                        wc.DownloadStringAsync(new Uri(string.Format(apidefinition.CheckHasVoted, service.APIKey, caller.ToString())));
                    }
                    catch (TimeoutException)
                    {
                        m_Logger.LogInformation(m_StringLocalizer["FAIL:API_DOWN_MESSAGE", service.Name]);
                        Say(caller.channel.owner.playerID.steamID, m_StringLocalizer["FAIL:API_DOWN_MESSAGE", service.Name], MsgColor, m_Configuration["MessageFailedIconUrl"]);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(ex.Message);
            }
        }
        #region Utilities
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
            SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(CSteamID);
            foreach (string text in WrapMessage(message))
            {
                ChatManager.serverSendMessage(text, color, null, steamPlayer, EChatMode.SAY, imageURL, true);
            }
        }
        public void Say(Player player, string message, Color color, string imageURL)
        {
            SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(player.channel.owner.playerID.steamID);
            foreach (string text in WrapMessage(message))
            {
                ChatManager.serverSendMessage(text, color, null, steamPlayer, EChatMode.SAY, imageURL, true);
            }
        }
        public void Say(string message, Color color, string imageURL)
        {
            foreach (var m in WrapMessage(message))
            {
                ChatManager.serverSendMessage(m, color, fromPlayer: null, toPlayer: null, mode: EChatMode.GLOBAL, iconURL: imageURL, useRichTextFormatting: true);
            }
        }
        public Color GetColorFromName(string colorName, Color fallback)
        {
            switch (colorName.Trim().ToLower())
            {
                case "black": return Color.black;
                case "blue": return Color.blue;
                case "clear": return Color.clear;
                case "cyan": return Color.cyan;
                case "gray": return Color.gray;
                case "green": return Color.green;
                case "grey": return Color.grey;
                case "magenta": return Color.magenta;
                case "red": return Color.red;
                case "white": return Color.white;
                case "yellow": return Color.yellow;
                case "rocket": return GetColorFromRGB(90, 206, 205);
            }

            Color? color = GetColorFromHex(colorName);
            if (color.HasValue) return color.Value;

            return fallback;
        }
        public Color? GetColorFromHex(string hexString)
        {
            hexString = hexString.Replace("#", "");
            if (hexString.Length == 3)
            { // #99f
                hexString = hexString.Insert(1, Convert.ToString(hexString[0])); // #999f
                hexString = hexString.Insert(3, Convert.ToString(hexString[2])); // #9999f
                hexString = hexString.Insert(5, Convert.ToString(hexString[4])); // #9999ff
            }
            if (hexString.Length != 6 || !int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out int argb))
            {
                return null;
            }
            byte r = (byte)((argb >> 16) & 0xff);
            byte g = (byte)((argb >> 8) & 0xff);
            byte b = (byte)(argb & 0xff);
            return GetColorFromRGB(r, g, b);
        }
        public Color GetColorFromRGB(byte R, byte G, byte B)
        {
            return GetColorFromRGB(R, G, B, 100);
        }
        public Color GetColorFromRGB(byte R, byte G, byte B, short A)
        {
            return new Color((1f / 255f) * R, (1f / 255f) * G, (1f / 255f) * B, (1f / 100f) * A);
        }
        #endregion
    }

    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class UpdateTask : MonoBehaviour
    {
        private readonly IPluginAccessor<Votifier> VotifierPlugin;
        public UpdateTask(IPluginAccessor<Votifier> votifierPlugin)
        {
            VotifierPlugin = votifierPlugin;
        }
        bool Loaded = false;
        protected void Load()
        {
            Loaded = true;
            StartCoroutine((IEnumerator)CheckQueue());
        }
        protected void Unload()
        {
            Loaded = false;
            StopCoroutine((IEnumerator)CheckQueue());
        }
        private IEnumerator<WaitForSeconds> CheckQueue()
        {
            while (Loaded)
            {
                if (VotifierPlugin.Instance.queue.Count > 0)
                {
                    VoteResult v = VotifierPlugin.Instance.queue.Dequeue();
                    VotifierPlugin.Instance.HandleVote(v);
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    public class VoteResult
    {
        public Player caller;
        public Service service;
        public ServiceDefinition apidefinition;
        public bool giveItemDirectly;
        public string result;
        public VoteResult() { }
    }
    public class VotifierWebclient : WebClient
    {
        public int Timeout { get; private set; }
        public VotifierWebclient(int timeout = 5000)
        {
            Timeout = timeout;
        }
        protected override WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);
            result.Timeout = Timeout;
            return result;
        }
    }
    public class Service
    {
        public string Name;
        public string APIKey;
        public Service() { }
    }
    public class ServiceDefinition
    {
        public string Name;
        public string CheckHasVoted;
        public string ReportSuccess;
        public ServiceDefinition() { }
    }
    public class RewardBundle
    {
        public string Name;
        public int Probability;
        public Reward Reward;
        public RewardBundle() { }
    }
    public class Reward
    {
        public uint Experiences;
        public List<Item> Items;
        public List<Vehicle> Vehicles;
        public List<string> Commands;
        public Reward() { }
    }
    public class Item
    {
        public ushort ItemID;
        public byte Amount;
        public Item() { }
    }
    public class Vehicle
    {
        public ushort VehicleID;
        public byte Amount;
        public Vehicle() { }
    }
}
