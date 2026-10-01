using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Top.Contracts.Tables.Items
{
    public class ItemEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("iconPath")] public string IconPath;
        [JsonProperty("type")] public int Type;
        [JsonProperty("equipLinks")] public int[] EquipLinks;
        [JsonProperty("modelPaths")] public string[] ModelPaths;
        [JsonProperty("tradeable")] public bool Tradeable;
        [JsonProperty("pickable")] public bool Pickable;
        [JsonProperty("discardable")] public bool Discardable;
        [JsonProperty("deletable")] public bool Deletable;
        [JsonProperty("stackLimit")] public int StackLimit;
        [JsonProperty("price")] public int Price;
        [JsonProperty("requiredLevel")] public int RequiredLevel;
        [JsonProperty("jobs")] public int[] Jobs = new int[0];
        [JsonProperty("bodies")] public int[] Bodies = new int[0];
        [JsonProperty("needLinks")] public int[] NeedLinks = new int[0];
        [JsonProperty("stats")] public Dictionary<CharacterStat, ItemStatBonus> Stats =
            new Dictionary<CharacterStat, ItemStatBonus>();
        [JsonProperty("durability")] public ItemRange Durability = new ItemRange();
        [JsonProperty("energy")] public ItemRange Energy = new ItemRange();
        [JsonProperty("sockets")] public int Sockets;
        [JsonProperty("forgeLevel")] public int ForgeLevel;
        [JsonProperty("forgeCap")] public int ForgeCap;
        [JsonProperty("scriptEffect")] public string ScriptEffect;
        [JsonProperty("description")] public string Description;
        [JsonProperty("cooldown")] public float Cooldown;

        public string ModelPathFor(int bodyType)
        {
            return ModelPaths != null && bodyType >= 0 && bodyType < ModelPaths.Length ? ModelPaths[bodyType] : null;
        }

        public bool CanEquipAt(int link)
        {
            return EquipLinks != null && Array.IndexOf(EquipLinks, link) >= 0;
        }
    }
}
