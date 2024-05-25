using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace SAIN.Layers
{
    public static class DebugOverlay
    {
        public static void Update()
        {
        }

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
                var decisions = sain.Memory.Decisions;
                stringBuilder.AppendLine($"Name: [{sain.Person.Name}] Nickname: [{sain.Player.Profile.Nickname}] Personality: [{info.Personality}] Type: [{info.Profile.WildSpawnType}]");
                stringBuilder.AppendLine($"Suppression Status: Num: [{sain.Suppression?.SuppressionNumber}] IsSuppressed: [{sain.Suppression?.IsSuppressed}] IsHeavySuppressed: [{sain.Suppression?.IsHeavySuppressed}]");
                stringBuilder.AppendLine($"Steering: [{sain.Steering.CurrentSteerPriority}]");
                stringBuilder.AppendLine($"AI Limited: [{sain.CurrentAILimit}]");
                stringBuilder.AppendLine($"Cover Points Count: [{sain.Cover.CoverPoints.Count}]");
                stringBuilder.AppendLine($"Cover Finder State: [{sain.Cover.CurrentCoverFinderState}]");
                stringBuilder.AppendLine($"Cover Finder Status: [{sain.Cover.CoverFinder.CurrentStatus}] Limited? [{sain.Cover.CoverFinder.ProcessingLimited}] ");
                var members = sain.Squad.SquadInfo?.Members;
                if (members != null && members.Count > 1)
                {
                    stringBuilder.AppendLine($"Squad Personality: [{sain.Squad.SquadInfo.SquadPersonality}]");
                }

                stringBuilder.AppendLine($"Indoors? {sain.Memory.Location.IsIndoors} EnvironmentID: {sain.Player?.AIData.EnvironmentId}");

                if (decisions.Main.Current != SoloDecision.None)
                {
                    stringBuilder.AppendLabeledValue("Main Decision", $"Current: {decisions.Main.Current} Last: {decisions.Main.Last}", Color.white, Color.yellow, true);
                }
                if (decisions.Squad.Current != SquadDecision.None)
                {
                    stringBuilder.AppendLabeledValue("Squad Decision", $"Current: {decisions.Squad.Current} Last: {decisions.Squad.Last}", Color.white, Color.yellow, true);
                }
                if (decisions.Self.Current != SelfDecision.None)
                {
                    stringBuilder.AppendLabeledValue("Self Decision", $"Current: {decisions.Self.Current} Last: {decisions.Self.Last}", Color.white, Color.yellow, true);
                }

                var state = sain.Search.CurrentState;
                if (state != SAINComponent.Classes.ESearchMove.None)
                {
                    stringBuilder.AppendLabeledValue("Searching", $"Current State: {sain.Search.CurrentState} Next: {sain.Search.NextState} Last: {sain.Search.LastState}", Color.white, Color.yellow, true);
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine("SAIN Info");
                stringBuilder.AppendLabeledValue("Personality and Type", $"{info.Personality} {info.Profile.WildSpawnType}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Power of Equipment", $"{info.Profile.PowerLevel}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Start Search + Hold Ground Time", $"{info.TimeBeforeSearch} + {info.HoldGroundDelay}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Difficulty + Modifier", $"{info.Profile.BotDifficulty} + {info.Profile.DifficultyModifier}", Color.white, Color.yellow, true);

                if (sain.HasEnemy)
                {
                    stringBuilder.AppendLine("Active Enemy Info");
                    CreateEnemyInfo(stringBuilder, sain.Enemy);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private static void CreateEnemyInfo(StringBuilder stringBuilder, SAINEnemy enemy)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Enemy Info");

            stringBuilder.AppendLabeledValue("Enemy Name", $"{enemy.EnemyPlayer?.Profile.Nickname}", Color.white, Color.red, true);
            stringBuilder.AppendLabeledValue("Enemy Power Level", $"{enemy.EnemyIPlayer?.AIData?.PowerOfEquipment}", Color.white, Color.red, true);

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
                stringBuilder.AppendLabeledValue("Heard Recently?", $"{enemy.HeardRecently}", Color.white, Color.yellow, true);
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