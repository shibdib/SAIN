using SAIN.Components;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SAIN.BotController.Classes
{
    public class BotSquads : SAINControl
    {
        public BotSquads(SAINBotController botController) : base(botController)
        {
        }

        public void Update()
        {
            int count = 0;
            foreach (var squad in Squads.Values)
            {
                if (squad != null)
                {
                    squad.Update();

                    if (SAINPlugin.DebugMode && DebugTimer < Time.time)
                    {
                        DebugTimer = Time.time + 60f;

                        string debugText = $"Squad [{count}]: " +
                            $"ID: [{squad.GetId()}] " +
                            $"Count: [{squad.Members.Count}] " +
                            $"Power: [{squad.SquadPowerLevel}] " +
                            $"Members:";

                        foreach (var member in squad.MemberInfos.Values)
                        {
                            debugText += $" [{member.Nickname}, {member.PowerLevel}]";
                        }

                        Logger.LogDebug(debugText);
                    }
                }
                count++;
            }
        }

        private float DebugTimer = 0f;

        public readonly Dictionary<string, Squad> Squads = new Dictionary<string, Squad>();

        public SquadCoverFinder SquadCoverFinder { get; private set; }

        public Squad GetSquad(BotComponent sain)
        {
            Squad result = null;
            var group = sain?.BotOwner?.BotsGroup;
            if (group != null)
            {
                int max = group.MembersCount;

                if (SAINPlugin.DebugMode)
                {
                    Logger.LogDebug($"Member Count: {max} Checking for existing squad object");
                }
                for (int i = 0; i < max; i++)
                {
                    var defaultMember = group.Member(i);
                    if (defaultMember != null)
                    {
                        if (BotController.GetSAIN(defaultMember, out var sainComponent))
                        {
                            if (SAINPlugin.DebugMode)
                                Logger.LogInfo($"Found SAIN Bot for squad");

                            if (sainComponent?.Squad.SquadInfo != null)
                            {
                                result = sainComponent.Squad.SquadInfo;
                                if (SAINPlugin.DebugMode)
                                    Logger.LogInfo($"Adding bot to squad [{result.GUID}]");
                                break;
                            }
                        }
                    }
                }
            }

            if (result == null)
            {
                result = new Squad();
                if (SAINPlugin.DebugMode)
                    Logger.LogWarning($"Created New Squad [{result.GUID}]");

                if (!Squads.ContainsKey(result.GUID))
                {
                    result.OnSquadEmpty += removeSquad;
                    Squads.Add(result.GUID, result);
                }
            }

            result.AddMember(sain);

            return result;
        }

        private void removeSquad(Squad squad)
        {
            if (squad != null)
            {
                squad.OnSquadEmpty -= removeSquad;
                squad.Dispose();
            }
        }
    }
}
