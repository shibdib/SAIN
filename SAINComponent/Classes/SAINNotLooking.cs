using Comfort.Common;
using EFT;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

namespace SAIN.SAINComponent.Classes
{
    public class SAINNotLooking
    {
        private static GeneralSettings Settings => SAINPlugin.LoadedPreset.GlobalSettings.General;

        public static float GetSpreadIncrease(IPlayer person, BotOwner botOwner)
        {
            if (Settings.NotLookingToggle && CheckIfPlayerNotLooking(person, botOwner))
            {
                return Settings.NotLookingAccuracyAmount;
            }
            return 0f;
        }

        public static float GetVisionSpeedDecrease(EnemyInfo enemyInfo)
        {
            if (CheckIfPlayerNotLooking(enemyInfo))
            {
                return Settings.NotLookingVisionSpeedModifier;
            }
            return 1f;
        }

        private static bool CheckIfPlayerNotLooking(IPlayer player, BotOwner botOwner)
        {
            if (player == null || botOwner == null)
            {
                return false;
            }
            if (botOwner.EnemiesController.EnemyInfos.TryGetValue(player, out EnemyInfo enemyInfo))
            {
                return CheckIfPlayerNotLooking(enemyInfo);
            }
            return false;
        }

        private static bool CheckIfPlayerNotLooking(EnemyInfo enemyInfo)
        {
            if (enemyInfo == null || enemyInfo.Owner == null)
            {
                return false;
            }
            IPlayer player = enemyInfo.Person;
            if (player == null)
            {
                return false;
            }

            if (!enemyInfo.HaveSeenPersonal
                || Time.time - enemyInfo.PersonalSeenTime <= Settings.NotLookingTimeLimit
                || !enemyInfo.IsVisible)
            {
                Vector3 lookDir = player.LookDirection.normalized;
                Vector3 playerPos = player.Position;
                Vector3 botPos = enemyInfo.Owner.Position;
                Vector3 botDir = (botPos - playerPos).normalized;
                float angle = Vector3.Angle(botDir, lookDir);
                return angle >= Settings.NotLookingAngle;
            }

            return false;
        }

        private static float DebugTimer;
    }
}
