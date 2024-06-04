using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Components.BotController;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static SAIN.Helpers.EnumValues;

namespace SAIN
{
    public class SAINEnableClass
    {
        static SAINEnableClass()
        {
            GameWorld.OnDispose += clear;
        }

        public static bool IsSAINDisabledForBot(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return true;
            }

            if (_excludedBots.Contains(botOwner.name))
                return true;

            if (_enabledBots.Contains(botOwner.name))
                return false;

            return shallExclude(botOwner);
        }

        private static bool shallExclude(BotOwner botOwner)
        {
            if (botOwner != null)
            {
                botOwner.GetPlayer.OnIPlayerDeadOrUnspawn += clearBot;
            }
            if (isBotExcluded(botOwner))
            {
                _excludedBots.Add(botOwner.name);
                return true;
            }
            _enabledBots.Add(botOwner.name);
            return false;
        }

        private static readonly List<string> _excludedBots = new List<string>();

        private static readonly List<string> _enabledBots = new List<string>();

        private static void clear()
        {
            if (_excludedBots.Count > 0)
                _excludedBots.Clear();

            if (_enabledBots.Count > 0)
                _enabledBots.Clear();
        }

        private static void clearBot(IPlayer player)
        {
            if (player != null)
            {
                player.OnIPlayerDeadOrUnspawn -= clearBot;
                string id = player.ProfileId;
                _excludedBots.Remove(id);
                _enabledBots.Remove(id);
            }
        }

        public static bool isBotExcluded(BotOwner botOwner)
        {
            WildSpawnType wildSpawnType = botOwner.Profile.Info.Settings.Role;
            if (BotSpawnController.StrictExclusionList.Contains(wildSpawnType))
            {
                return true;
            }
            if (isAlwaysEnabled(wildSpawnType, botOwner))
            {
                return false;
            }

            return shallExcludeBotByType(wildSpawnType, botOwner);
        }

        private static bool shallExcludeBotByType(WildSpawnType wildSpawnType, BotOwner botOwner)
        {
            return
                excludeOthers(wildSpawnType) ||
                excludeScav(wildSpawnType, botOwner) ||
                excludeBoss(wildSpawnType) ||
                excludeFollower(wildSpawnType) ||
                excludeGoons(wildSpawnType);
        }

        private static bool isAlwaysEnabled(WildSpawnType wildSpawnType, BotOwner botOwner)
        {
            return
                WildSpawn.IsPMC(wildSpawnType) ||
                SAINPlugin.BotController?.Bots?.ContainsKey(botOwner.name) == true;
        }

        private static bool excludeBoss(WildSpawnType wildSpawnType)
        {
            return SAINEnabled.VanillaBosses
            && !WildSpawn.IsGoons(wildSpawnType)
            && WildSpawn.IsBoss(wildSpawnType);
        }

        private static bool excludeGoons(WildSpawnType wildSpawnType)
        {
            return SAINEnabled.VanillaGoons
            && WildSpawn.IsGoons(wildSpawnType);
        }

        private static bool excludeFollower(WildSpawnType wildSpawnType)
        {
            return SAINEnabled.VanillaFollowers
            && !WildSpawn.IsGoons(wildSpawnType)
            && WildSpawn.IsFollower(wildSpawnType);
        }

        private static bool excludeScav(WildSpawnType wildSpawnType, BotOwner botOwner)
        {
            return SAINEnabled.VanillaScavs
            && WildSpawn.IsScav(wildSpawnType) && 
            !isPlayerScav(botOwner.Profile.Nickname);
        }

        private static bool excludeOthers(WildSpawnType wildSpawnType)
        {
            if (SAINEnabled.VanillaCultists &&
                WildSpawn.IsCultist(wildSpawnType))
            {
                return true;
            }
            if (SAINEnabled.VanillaRogues &&
                wildSpawnType == WildSpawnType.exUsec)
            {
                return true;
            }
            // Raiders have the same brain type as PMCs, so I'll need a new solution to have them excluded
            //if (SAINEnabled.VanillaRaiders &&
            //    wildSpawnType == WildSpawnType.pmcBot)
            //{
            //    return true;
            //}
            if (SAINEnabled.VanillaBloodHounds)
            {
                if (wildSpawnType == WildSpawnType.arenaFighter || 
                    wildSpawnType == WildSpawnType.arenaFighterEvent)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isPlayerScav(string nickname)
        {
            // Pattern: xxx (xxx)
            string pattern = "\\w+.[(]\\w+[)]";
            Regex regex = new Regex(pattern);
            if (regex.Matches(nickname).Count > 0)
            {
                return true;
            }
            return false;
        }

        public static bool GetSAIN(BotOwner botOwner, out BotComponent sain, string patchName)
        {
            sain = null;
            if (IsSAINDisabledForBot(botOwner))
            {
                return false;
            }
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{patchName}]");
                return false;
            }
            return SAINPlugin.BotController.GetSAIN(botOwner, out sain);
        }

        public static bool GetSAIN(Player player, out BotComponent sain, string patchName)
        {
            return GetSAIN(player?.AIData?.BotOwner, out sain, patchName);
        }

        private static DisableSAINSettings SAINEnabled => SAINPlugin.LoadedPreset.GlobalSettings.SAINDisabled;
    }
}
