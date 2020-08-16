using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;

namespace fr34kyn01535.Votifier
{
    public class Config : IRocketPluginConfiguration
    {
        public string MessageColor;
        public string MessageFailedIconUrl;
        public string MessageSuccessIconUrl;
        [XmlArray("Services"), XmlArrayItem(ElementName = "Service")]
        public List<Service> Services;
        [XmlArray("ServiceDefinitions"), XmlArrayItem(ElementName = "ServiceDefinition")]
        public List<ServiceDefinition> ServiceDefinitions;
        public bool EnableRewardBundles;
        public bool GlobalRewardAnnouncement;
        [XmlArray("RewardBundles"), XmlArrayItem(ElementName = "RewardBundle")]
        public List<RewardBundle> RewardBundles;

        public void LoadDefaults()
        {
            MessageColor = "Cyan";
            MessageFailedIconUrl = "https://i.imgur.com/FeIvao9.png";
            MessageSuccessIconUrl = "https://i.imgur.com/IYONga6.png";
            EnableRewardBundles = true;
            GlobalRewardAnnouncement = true;
            RewardBundles = new List<RewardBundle>()
            {
                new RewardBundle
                {
                    Name = "Hehe",
                    Probability = 66,
                    OneTimeOnly = false,
                    Experiences = 100,
                    Items = new List<ItemReward>
                    {
                        new ItemReward{ ItemID = 363, Amount = 1 },
                        new ItemReward{ ItemID = 132, Amount = 1 },
                    },
                    Vehicles = new List<VehicleReward>
                    {
                        new VehicleReward{ VehicleID = 2, Amount = 1 },
                        new VehicleReward{ VehicleID = 1, Amount = 1 },
                    },
                    Commands = new List<string>
                    {
                        "pay {playerid} 1000",
                        "Broadcast {playername} hehe"
                    }
                },
                new RewardBundle
                {
                    Name = "Hoho",
                    Probability = 33,
                    OneTimeOnly = false,
                    Experiences = 100,
                    Items = new List<ItemReward>
                    {
                        new ItemReward{ ItemID = 363, Amount = 1 },
                        new ItemReward{ ItemID = 132, Amount = 1 },
                    },
                    Vehicles = new List<VehicleReward>
                    {
                        new VehicleReward{ VehicleID = 2, Amount = 1 },
                        new VehicleReward{ VehicleID = 1, Amount = 1 },
                    },
                    Commands = new List<string>
                    {
                        "pay {playerid} 1000",
                        "Broadcast {playername} hehe"
                    }
                }
            };
            Services = new List<Service>()
            {
                new Service
                {
                    Name = "unturned-servers.net",
                    APIKey = "apikey"
                },
                new Service
                {
                    Name = "unturnedsl.com",
                    APIKey = "apikey"
                }
            };
            ServiceDefinitions = new List<ServiceDefinition>()
            {
                new ServiceDefinition() {
                    Name = "unturned-servers.net",
                    CheckHasVoted = "https://unturned-servers.net/api/?object=votes&element=claim&key={0}&steamid={1}",
                    ReportSuccess ="https://unturned-servers.net/api/?action=post&object=votes&element=claim&key={0}&steamid={1}"
                },
                new ServiceDefinition() {
                    Name = "unturnedsl.com",
                    CheckHasVoted = "http://unturnedsl.com/api/dedicated/{0}/{1}",
                    ReportSuccess = "http://unturnedsl.com/api/dedicated/post/{0}/{1}"
                }
            };
        }
    }
    public class Service
    {
        public Service() { }
        public Service(string name)
        {
            Name = name;
        }
        [XmlAttribute("Name")]
        public string Name = "";
        [XmlAttribute("APIKey")]
        public string APIKey = "";
    }
    public class ServiceDefinition
    {
        [XmlAttribute("Name")]
        public string Name;
        [XmlElement]
        public string CheckHasVoted;
        [XmlElement]
        public string ReportSuccess;
    }
    public class RewardBundle
    {
        [XmlAttribute("Name")]
        public string Name;
        [XmlAttribute("Probability")]
        public int Probability;
        [XmlAttribute("OneTimeOnly")]
        public bool OneTimeOnly;
        public uint Experiences;
        [XmlArrayItem(ElementName = "Item")]
        public List<ItemReward> Items;
        [XmlArrayItem(ElementName = "Vehicle")]
        public List<VehicleReward> Vehicles;
        [XmlArrayItem(ElementName = "Command")]
        public List<string> Commands;
        public RewardBundle() { }
    }
    public class ItemReward
    {
        [XmlAttribute("ItemID")]
        public ushort ItemID;
        [XmlAttribute("Amount")]
        public byte Amount;
        public ItemReward() { }
    }
    public class VehicleReward
    {
        [XmlAttribute("VehicleID")]
        public ushort VehicleID;
        [XmlAttribute("Amount")]
        public byte Amount;
        public VehicleReward() { }
    }
}
