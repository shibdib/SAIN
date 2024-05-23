using EFT;
using UnityEngine.UIElements;
using System;
using System.Linq;
using EFT.Interactive;
using Comfort.Common;
using SAIN.SAINComponent.Classes;

namespace SAIN.Layers
{
    internal class ExtractLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName<ExtractLayer>();

        public ExtractLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public override Action GetNextAction()
        {
            return new Action(typeof(ExtractAction), "Extract");
        }

        public override bool IsActive()
        {
            if (Bot == null) return false;
            //if (SAIN.SAINEnabled == false) return false;

            if (!Bot.Info.FileSettings.Mind.EnableExtracts || !Bot.Info.GlobalSettings.Extract.EnableExtractsGlobal)
            {
                return false;
            }

            if (!Components.BotController.BotExtractManager.IsBotAllowedToExfil(Bot))
            {
                return false;
            }

            if (!ExtractFromTime() && !ExtractFromInjury() && !ExtractFromLoot() && !ExtractFromExternal())
            {
                return false;
            }

            if (Bot.Memory.Extract.ExfilPosition == null)
            {
                BotController.BotExtractManager.TryFindExfilForBot(Bot);
                return false;
            }

            // If the bot can no longer use its selected extract and isn't already in the extract area, select another one. This typically happens if
            // the bot selects a VEX but the car leaves before the bot reaches it.
            if (!BotController.BotExtractManager.CanBotsUseExtract(Bot.Memory.Extract.ExfilPoint) && !IsInExtractArea())
            {
                Bot.Memory.Extract.ExfilPoint = null;
                Bot.Memory.Extract.ExfilPosition = null;

                return false;
            }

            return true;
        }

        private bool IsInExtractArea()
        {
            float distance = (BotOwner.Position - Bot.Memory.Extract.ExfilPosition.Value).sqrMagnitude;
            return distance < ExtractAction.MinDistanceToStartExtract;
        }

        private bool ExtractFromTime()
        {
            if (ModDetection.QuestingBotsLoaded)
            {
                return false;
            }
            float percentageLeft = BotController.BotExtractManager.PercentageRemaining;
            if (percentageLeft <= Bot.Info.PercentageBeforeExtract)
            {
                if (!Logged)
                {
                    Logged = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract with [{percentageLeft}] of the raid remaining.");
                }
                if (Bot.Enemy == null || BotController.BotExtractManager.TimeRemaining < 120)
                {
                    Bot.Memory.Extract.ExtractReason = EExtractReason.Time;
                    return true;
                }
            }
            return false;
        }

        private bool ExtractFromInjury()
        {
            if (Bot.Memory.Health.Dying && !BotOwner.Medecine.FirstAid.HaveSmth2Use)
            {
                if (!Logged)
                {
                    Logged = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because of heavy injury and lack of healing items.");
                }
                if (Bot.Enemy == null || Bot.Enemy.TimeSinceSeen > 30f)
                {
                    Bot.Memory.Extract.ExtractReason = EExtractReason.Injured;
                    return true;
                }
            }
            return false;
        }

        // Looting Bots Integration
        private bool ExtractFromLoot()
        {
            // If extract from loot is disabled, or no Looting Bots interop, not active
            if (SAINPlugin.LoadedPreset.GlobalSettings.LootingBots.ExtractFromLoot == false  || !LootingBots.LootingBotsInterop.Init())
            {
                return false;
            }

            // No integration setup yet, set it up
            if (SAINLootingBotsIntegration == null)
            {
                SAINLootingBotsIntegration = new SAINLootingBotsIntegration(BotOwner, Bot);
            }

            SAINLootingBotsIntegration?.Update();

            if (FullOnLoot && HasActiveThreat() == false)
            {
                if (!_loggedExtractLoot)
                {
                    _loggedExtractLoot = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because of Loot found in raid. Net Loot Value: [{SAINLootingBotsIntegration?.NetLootValue}]");
                }
                Bot.Memory.Extract.ExtractReason = EExtractReason.Loot;
                return true;
            }
            return false;
        }

        private bool _loggedExtractLoot;

        private bool FullOnLoot => SAINLootingBotsIntegration?.FullOnLoot == true;

        private SAINLootingBotsIntegration SAINLootingBotsIntegration;

        private bool HasActiveThreat()
        {
            if (Bot.Enemy == null || Bot.Enemy.TimeSinceSeen > 30f)
            {
                return false;
            }
            return true;
        }

        private bool ExtractFromExternal()
        {
            if (Bot.Info.ForceExtract)
            {
                if (!_loggedExtractExternal)
                {
                    _loggedExtractExternal = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because of external call.");
                }
                Bot.Memory.Extract.ExtractReason = EExtractReason.External;
            }
            return Bot.Info.ForceExtract;
        }

        private bool _loggedExtractExternal;
        private bool Logged = false;

        public override bool IsCurrentActionEnding()
        {
            return false;
        }
    }
}