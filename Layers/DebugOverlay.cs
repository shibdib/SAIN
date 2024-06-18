using EFT;
using HarmonyLib;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SAIN.Layers
{
    public static class DebugOverlay
    {
        static DebugOverlay()
        {
            TimeToAim = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "float_7");
            timeAiming = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "float_5");
        }

        private static FieldInfo TimeToAim;
        private static FieldInfo timeAiming;

        private static void DisplayPropertyAndFieldValues(object obj, StringBuilder stringBuilder)
        {
            if (obj == null)
            {
                stringBuilder.AppendLine($"null");
                return;
            }

            Type type = obj.GetType();

            string name;
            object resultValue;
            int count = 0;

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                name = field.Name;
                resultValue = field.GetValue(obj);
                string stringValue = null;
                if (resultValue != null)
                {
                    count++;
                    stringValue = resultValue.ToString();
                    stringBuilder.AppendLabeledValue($"{count}. {name}", stringValue, Color.white, Color.yellow, true);
                }
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                name = property.Name;
                resultValue = property.GetValue(obj);
                string stringValue = null;
                if (resultValue != null)
                {
                    count++;
                    stringValue = resultValue.ToString();
                    stringBuilder.AppendLabeledValue($"{count}. {name}", stringValue, Color.white, Color.yellow, true);
                }
            }
        }

        public static void AddBaseInfo(BotComponent sain, BotOwner botOwner, StringBuilder stringBuilder)
        {
            try
            {
                var info = sain.Info;
                stringBuilder.AppendLine($"Name: [{sain.Person.Name}] Nickname: [{sain.Player.Profile.Nickname}] Personality: [{info.Personality}] Type: [{info.Profile.WildSpawnType}] PowerLevel: [{info.Profile.PowerLevel}]");
                stringBuilder.AppendLabeledValue("Start Search + Hold Ground Time", $"{info.TimeBeforeSearch} + {info.HoldGroundDelay}", Color.white, Color.yellow, true);
                stringBuilder.AppendLine($"Suppression Num: [{sain.Suppression?.SuppressionNumber}] IsSuppressed: [{sain.Suppression?.IsSuppressed}] IsHeavySuppressed: [{sain.Suppression?.IsHeavySuppressed}]");
                stringBuilder.AppendLine($"Steering: [{sain.Steering.CurrentSteerPriority}] : AI Limited: [{sain.CurrentAILimit}] : Cover Points Count: [{sain.Cover.CoverPoints.Count}]");
                stringBuilder.AppendLine($"Indoors? {sain.Memory.Location.IsIndoors} EnvironmentID: {sain.Player?.AIData.EnvironmentId} In Bunker? {sain.PlayerComponent.AIData.PlayerLocation.InBunker}");
                var members = sain.Squad.SquadInfo?.Members;
                if (members != null && members.Count > 1)
                {
                    stringBuilder.AppendLine($"Squad Personality: [{sain.Squad.SquadInfo.SquadPersonality}]");
                }

                stringBuilder.AppendLine();

                stringBuilder.AppendLabeledValue("Main Decision", $"Current: {sain.Decision.CurrentSoloDecision} Last: {sain.Decision.OldSoloDecision}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Squad Decision", $"Current: {sain.Decision.CurrentSquadDecision} Last: {sain.Decision.OldSquadDecision}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Self Decision", $"Current: {sain.Decision.CurrentSelfDecision} Last: {sain.Decision.OldSelfDecision}", Color.white, Color.yellow, true);

                stringBuilder.AppendLine();

                if (sain.BotOwner.AimingData != null)
                {
                    stringBuilder.AppendLine($"Aim Status {sain.Steering.AimStatus} Last Aim Time: {sain.LastAimTime}");
                    stringBuilder.AppendLine($"Aim Time {timeAiming.GetValue(sain.BotOwner.AimingData)} TimeToAim: {TimeToAim.GetValue(sain.BotOwner.AimingData)}");
                    stringBuilder.AppendLine($"Aim Offset Magnitude {(sain.BotOwner.AimingData.RealTargetPoint - sain.BotOwner.AimingData.EndTargetPoint).magnitude}");
                    stringBuilder.AppendLine($"Friendly Fire Status {sain.FriendlyFireClass.FriendlyFireStatus} No Bush ESP Status: {sain.NoBushESP.NoBushESPActive}");

                }

                if (sain.HasEnemy)
                {
                    stringBuilder.AppendLine();
                    CreateEnemyInfo(stringBuilder, sain.Enemy);
                }
                stringBuilder.AppendLine();

                if (sain.Decision.CurrentSoloDecision == SoloDecision.Search)
                {
                    stringBuilder.AppendLabeledValue("Searching", $"Current State: {sain.Search.CurrentState} Next: {sain.Search.NextState} Last: {sain.Search.LastState}", Color.white, Color.yellow, true);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private static void CreateEnemyInfo(StringBuilder stringBuilder, SAINEnemy enemy)
        {
            stringBuilder.AppendLine("Enemy Info");

            stringBuilder.AppendLabeledValue("Enemy Name", $"{enemy.EnemyPlayer?.Profile.Nickname}", Color.white, Color.red, true);
            stringBuilder.AppendLabeledValue("Enemy Power Level ", $"{enemy.EnemyIPlayer?.AIData?.PowerOfEquipment}", Color.white, Color.red, true);

            stringBuilder.AppendLabeledValue("Last GainSight Result", $"{enemy.Vision.LastGainSightResult}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("SeenCoef", $"{enemy.EnemyInfo.SeenCoef}", Color.white, Color.yellow, true);
            //stringBuilder.AppendLabeledValue("PercentSpotted", $"{enemy.EnemyInfo.BodyData().Value?.PercentSpotted(out _)}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Seen?", $"{enemy.Seen}", Color.white, enemy.Seen ? Color.red : Color.white, true);
            if (enemy.Seen)
            {
                stringBuilder.AppendLabeledValue("Time Since Seen", $"{enemy.TimeSinceSeen}", Color.white, Color.yellow, true);
            }
            stringBuilder.AppendLabeledValue("Currently Visible", $"{enemy.IsVisible}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Can Shoot", $"{enemy.CanShoot}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("In Line of Sight", $"{enemy.InLineOfSight}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Heard?", $"{enemy.Heard}", Color.white, enemy.Seen ? Color.red : Color.white, true);
            if (enemy.Heard)
            {
                stringBuilder.AppendLabeledValue("Time Since Heard", $"{enemy.TimeSinceHeard}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Heard Recently?", $"{enemy.EnemyStatus.HeardRecently}", Color.white, Color.yellow, true);
            }

            var lastKnown = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnown != null)
            {
                stringBuilder.AppendLabeledValue("Time Since Last Known Position Updated", $"{lastKnown.TimeSincePositionUpdated}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Enemy Distance from Last Known Position", $"{(enemy.EnemyPosition - lastKnown.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Has Arrived?", $"Personal: {lastKnown.HasArrivedPersonal} / Squad: {lastKnown.HasArrivedSquad}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Has Seen?", $"Personal: {lastKnown.HasSeenPersonal} / Squad: {lastKnown.HasSeenSquad}", Color.white, Color.yellow, true);
            }


        }

        public static void AddAimData(BotOwner BotOwner, StringBuilder stringBuilder)
        {
            var aimData = BotOwner.AimingData;
            if (aimData != null)
            {
                stringBuilder.AppendLine(nameof(BotOwner.AimingData));
                stringBuilder.AppendLabeledValue("Aim: AimComplete?", $"{aimData.IsReady}", Color.red, Color.yellow, true);
                var shoot = BotOwner.ShootData;
                stringBuilder.AppendLabeledValue("Shoot: CanShootByState", $"{shoot?.CanShootByState}", Color.red, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Shoot: Shooting", $"{shoot?.Shooting}", Color.red, Color.yellow, true);
                DisplayPropertyAndFieldValues(BotOwner.AimingData, stringBuilder);
            }
        }
    }
}