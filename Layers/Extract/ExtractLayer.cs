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
            if (SAINBot == null) return false;
            //if (SAIN.SAINEnabled == false) return false;

            if (!SAINBot.Info.FileSettings.Mind.EnableExtracts || !SAINBot.Info.GlobalSettings.Extract.EnableExtractsGlobal)
            {
                return false;
            }

            if (!Components.BotController.BotExtractManager.IsBotAllowedToExfil(SAINBot))
            {
                return false;
            }

            if (!ExtractFromTime() && !ExtractFromInjury() && !ExtractFromLoot() && !ExtractFromExternal())
            {
                return false;
            }

            if (SAINBot.Memory.Extract.ExfilPosition == null)
            {
                BotController.BotExtractManager.TryFindExfilForBot(SAINBot);
                return false;
            }

            // If the bot can no longer use its selected extract and isn't already in the extract area, select another one. This typically happens if
            // the bot selects a VEX but the car leaves before the bot reaches it.
            if (!BotController.BotExtractManager.CanBotsUseExtract(SAINBot.Memory.Extract.ExfilPoint) && !IsInExtractArea())
            {
                SAINBot.Memory.Extract.ExfilPoint = null;
                SAINBot.Memory.Extract.ExfilPosition = null;

                return false;
            }

            return true;
        }

        private bool IsInExtractArea()
        {
            float distance = (BotOwner.Position - SAINBot.Memory.Extract.ExfilPosition.Value).sqrMagnitude;
            return distance < ExtractAction.MinDistanceToStartExtract;
        }

        private bool ExtractFromTime()
        {
            if (ModDetection.QuestingBotsLoaded)
            {
                return false;
            }
            float percentageLeft = BotController.BotExtractManager.PercentageRemaining;
            if (percentageLeft <= SAINBot.Info.PercentageBeforeExtract)
            {
                if (!Logged)
                {
                    Logged = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract with [{percentageLeft}] of the raid remaining.");
                }
                if (SAINBot.Enemy == null || BotController.BotExtractManager.TimeRemaining < 120)
                {
                    SAINBot.Memory.Extract.ExtractReason = EExtractReason.Time;
                    return true;
                }
            }
            return false;
        }

        private bool ExtractFromInjury()
        {
            if (SAINBot.Memory.Health.Dying && !BotOwner.Medecine.FirstAid.HaveSmth2Use)
            {
                if (!Logged)
                {
                    Logged = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because of heavy injury and lack of healing items.");
                }
                if (SAINBot.Enemy == null || SAINBot.Enemy.TimeSinceSeen > 30f)
                {
                    SAINBot.Memory.Extract.ExtractReason = EExtractReason.Injured;
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
                SAINLootingBotsIntegration = new SAINLootingBotsIntegration(BotOwner, SAINBot);
            }

            SAINLootingBotsIntegration?.Update();

            if (FullOnLoot && HasActiveThreat() == false)
            {
                if (!_loggedExtractLoot)
                {
                    _loggedExtractLoot = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because of Loot found in raid. Net Loot Value: [{SAINLootingBotsIntegration?.NetLootValue}]");
                }
                SAINBot.Memory.Extract.ExtractReason = EExtractReason.Loot;
                return true;
            }
            return false;
        }

        private bool _loggedExtractLoot;

        private bool FullOnLoot => SAINLootingBotsIntegration?.FullOnLoot == true;

        private SAINLootingBotsIntegration SAINLootingBotsIntegration;

        private bool HasActiveThreat()
        {
            if (SAINBot.Enemy == null || SAINBot.Enemy.TimeSinceSeen > 30f)
            {
                return false;
            }
            return true;
        }

        private bool ExtractFromExternal()
        {
            if (SAINBot.Info.ForceExtract)
            {
                if (!_loggedExtractExternal)
                {
                    _loggedExtractExternal = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because of external call.");
                }
                SAINBot.Memory.Extract.ExtractReason = EExtractReason.External;
            }
            return SAINBot.Info.ForceExtract;
        }

        private bool _loggedExtractExternal;
        private bool Logged = false;

        public override bool IsCurrentActionEnding()
        {
            return false;
        }
    }
}