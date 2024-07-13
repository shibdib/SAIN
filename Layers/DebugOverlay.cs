using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Search;
using System;
using System.Collections.Generic;
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

            int count = 0;
            foreach (var name in _overlayNames) {
                _overlays.Add(count, name);
                count++;
            }
        }

        private static FieldInfo TimeToAim;
        private static FieldInfo timeAiming;

        private static readonly Dictionary<int, string> _overlays = new Dictionary<int, string>();

        private static readonly string[] _overlayNames =
        {
            "Bot Info",
            "Bot Properties",
            "Current Enemy Info",
            "All Enemies Info",
        };

        private static bool _changedOverlay;

        public static void UpdateSelectedOverlay()
        {
            if (_changedOverlay &&
                SAINPlugin.NextDebugOverlay.Value.IsUp() &&
                SAINPlugin.PreviousDebugOverlay.Value.IsUp()) {
                _changedOverlay = false;
            }
            if (_changedOverlay) {
                return;
            }

            if (SAINPlugin.NextDebugOverlay.Value.IsDown()) {
                changeOverlay(true);
                _changedOverlay = true;
                return;
            }

            if (SAINPlugin.PreviousDebugOverlay.Value.IsDown()) {
                changeOverlay(false);
                _changedOverlay = true;
                return;
            }
        }

        private static void changeOverlay(bool next)
        {
            int count = _overlays.Count - 1;
            if (next) {
                _selectedOverlay++;
                if (_selectedOverlay > count) {
                    _selectedOverlay = 0;
                }
            }
            else {
                _selectedOverlay--;
                if (_selectedOverlay < 0) {
                    _selectedOverlay = count;
                }
            }
            Logger.LogDebug($"{_selectedOverlay} : {next}");
        }

        private static int _selectedOverlay;

        public static void AddBaseInfo(BotComponent sain, BotOwner botOwner, StringBuilder stringBuilder)
        {
            UpdateSelectedOverlay();

            try {
                var debug = SAINPlugin.DebugSettings;

                var info = sain.Info;
                if (debug.Overlay_Info) {
                    stringBuilder.AppendLine($"Name: [{sain.Person.Name}] Nickname: [{sain.Player.Profile.Nickname}] Personality: [{info.Personality}] Type: [{info.Profile.WildSpawnType}] PowerLevel: [{info.Profile.PowerLevel}]");
                    stringBuilder.AppendLabeledValue("Active SAIN Layer", $"{sain.ActiveLayer}", Color.white, Color.yellow);
                    stringBuilder.AppendLabeledValue("Steering", $"{sain.Steering.CurrentSteerPriority} : {sain.Steering.EnemySteerDir}", Color.white, Color.yellow);
                    stringBuilder.AppendLabeledValue("AI Limited", $"{sain.CurrentAILimit}", Color.white, Color.yellow);
                    stringBuilder.AppendLabeledValue("Closest Human Distance", $"{sain.AILimit.ClosestPlayerDistanceSqr.Sqrt()}", Color.white, Color.yellow);

                    if (debug.Overlay_Info_Expanded) {
                        stringBuilder.AppendLine($"Cover Points Count: [{sain.Cover.CoverPoints.Count}]");
                        stringBuilder.AppendLabeledValue("Start Search + Hold Ground Time", $"{info.TimeBeforeSearch} + {info.HoldGroundDelay}", Color.white, Color.yellow, true);
                        stringBuilder.AppendLine($"Suppression Num: [{sain.Suppression?.SuppressionNumber}] IsSuppressed: [{sain.Suppression?.IsSuppressed}] IsHeavySuppressed: [{sain.Suppression?.IsHeavySuppressed}]");
                        stringBuilder.AppendLine($"Indoors? {sain.Memory.Location.IsIndoors} EnvironmentID: {sain.Player?.AIData.EnvironmentId} In Bunker? {sain.PlayerComponent.AIData.PlayerLocation.InBunker}");
                        var members = sain.Squad.SquadInfo?.Members;
                        if (members != null && members.Count > 1) {
                            stringBuilder.AppendLine($"Squad Personality: [{sain.Squad.SquadInfo.SquadPersonality}]");
                        }
                    }
                }

                if (debug.Overlay_Decisions) {
                    stringBuilder.AppendLine("Decisions");
                    stringBuilder.AppendLabeledValue("Main", $"Current: {sain.Decision.CurrentCombatDecision} Last: {sain.Decision.PreviousCombatDecision}", Color.white, Color.yellow, true);
                    stringBuilder.AppendLabeledValue("Squad", $"Current: {sain.Decision.CurrentSquadDecision} Last: {sain.Decision.PreviousSquadDecision}", Color.white, Color.yellow, true);
                    stringBuilder.AppendLabeledValue("Self", $"Current: {sain.Decision.CurrentSelfDecision} Last: {sain.Decision.PreviousSelfDecision}", Color.white, Color.yellow, true);
                }

                if (debug.Overlay_EnemyLists) {
                    var lists = sain.EnemyController.EnemyLists;
                    stringBuilder.AppendLine("Enemy Lists");
                    stringBuilder.AppendLine($"Known Enemies: [{lists.GetEnemyList(EEnemyListType.Known).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.Known).Humans}]");
                    stringBuilder.AppendLine($"Visible Enemies: [{lists.GetEnemyList(EEnemyListType.Visible).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.Visible).Humans}]");
                    stringBuilder.AppendLine($"InLineOfSight Enemies: [{lists.GetEnemyList(EEnemyListType.InLineOfSight).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.InLineOfSight).Humans}]");
                    stringBuilder.AppendLine($"ActiveThreats: [{lists.GetEnemyList(EEnemyListType.ActiveThreats).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.ActiveThreats).Humans}]");
                }

                if (debug.OverLay_AimInfo) {
                    if (sain.BotOwner.AimingData != null) {
                        stringBuilder.AppendLine("Aim Data");
                        stringBuilder.AppendLine($"Status {sain.Aim.AimStatus} Last Aim Time: {sain.Aim.LastAimTime}");
                        stringBuilder.AppendLine($"Aim Time {timeAiming.GetValue(sain.BotOwner.AimingData)} TimeToAim: {TimeToAim.GetValue(sain.BotOwner.AimingData)}");
                        stringBuilder.AppendLine($"Aim Offset Magnitude {(sain.BotOwner.AimingData.RealTargetPoint - sain.BotOwner.AimingData.EndTargetPoint).magnitude}");
                        stringBuilder.AppendLine($"Friendly Fire Status {sain.FriendlyFire.FriendlyFireStatus} No Bush ESP Status: {sain.NoBushESP.NoBushESPActive}");
                    }
                }

                if (debug.Overlay_EnemyInfo) {
                    Enemy mainPlayer = null;
                    if (debug.OverLay_AlwaysShowMainPlayerInfo)
                        foreach (var enemy in sain.EnemyController.Enemies.Values)
                            if (enemy?.EnemyPlayer.IsYourPlayer == true)
                                mainPlayer = enemy;

                    Enemy closestHuman = null;
                    if (debug.OverLay_AlwaysShowClosestHumanInfo) {
                        float closest = float.MaxValue;
                        foreach (var enemy in sain.EnemyController.Enemies.Values) {
                            if (enemy == null) continue;
                            if (enemy.IsAI) continue;
                            if (enemy.RealDistance < closest) {
                                closest = enemy.RealDistance;
                                closestHuman = enemy;
                            }
                        }
                    }

                    Enemy infoToShow = mainPlayer ?? closestHuman ?? sain.Enemy;
                    if (infoToShow != null) {
                        CreateEnemyInfo(stringBuilder, infoToShow);
                        stringBuilder.AppendLine();
                    }
                }

                if (debug.Overlay_Search) {
                    var enemyDecisions = sain.Decision.EnemyDecisions;
                    var shallSearch = enemyDecisions.DebugShallSearch;
                    if (shallSearch != null) {
                        if (shallSearch == true)
                            stringBuilder.AppendLabeledValue("Searching",
                                $"Current State: {sain.Search.CurrentState} " +
                                $"Next: {sain.Search.NextState} " +
                                $"Last: {sain.Search.LastState}",
                                Color.white, Color.yellow, true);

                        var reasons = enemyDecisions.DebugSearchReasons;
                        var wantReasons = reasons.WantSearchReasons;
                        stringBuilder.AppendLabeledValue("Want Search Reasons",
                            $"[WantToSearchReason : {wantReasons.WantToSearchReason}] " +
                            $"[NotWantToSearchReason: {wantReasons.NotWantToSearchReason}] " +
                            $"[CantStartReason: {wantReasons.CantStartReason}]",
                            Color.white, Color.yellow, true);

                        if (reasons.NotSearchReason != SearchReasonsStruct.ENotSearchReason.None)
                            stringBuilder.AppendLabeledValue("Not Search Reason",
                                $"{reasons.NotSearchReason}",
                                Color.white, Color.yellow, true);

                        if (!reasons.PathCalcFailReason.IsNullOrEmpty())
                            stringBuilder.AppendLabeledValue("CalcPath Fail Reason",
                                $"{reasons.PathCalcFailReason}",
                                Color.white, Color.yellow, true);
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
            }
        }

        private static bool _expandedEnemyInfo => SAINPlugin.DebugSettings.Overlay_EnemyInfo_Expanded;

        private static void CreateEnemyInfo(StringBuilder stringBuilder, Enemy enemy)
        {
            if (enemy == null) {
                return;
            }

            stringBuilder.AppendLine("Enemy Info");
            stringBuilder.AppendLabeledValue("Name", $"{enemy.EnemyPlayer?.Profile.Nickname}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Power Level ", $"{enemy.EnemyIPlayer?.AIData?.PowerOfEquipment}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Distance", $"{enemy.EnemyIPlayer?.AIData?.PowerOfEquipment}", Color.white, Color.yellow, true);

            addPlaceInfo(stringBuilder, enemy.KnownPlaces.LastKnownPlace, "Last Known Position");

            stringBuilder.AppendLabeledValue("Time To Spot", $"{enemy.Vision.LastGainSightResult.Round100()}", Color.white, Color.yellow, true);

            float highestPercent = getPercentSpotted(enemy, out var partType);
            if (highestPercent > 0)
                stringBuilder.AppendLabeledValue("Percent Spotted", $"{partType} : {highestPercent}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Visible", $"{enemy.IsVisible}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Seen", $"{enemy.Seen}", Color.white, enemy.Seen ? Color.red : Color.white, true);
            if (enemy.Seen) {
                if (_expandedEnemyInfo) {
                    addPlaceInfo(stringBuilder, enemy.KnownPlaces.LastSeenPlace, "Last Seen");
                }
            }
            if (_expandedEnemyInfo) {
                stringBuilder.AppendLabeledValue("Horizontal Angle", $"{enemy.Vision.Angles.AngleToEnemyHorizontalSigned.Round100()}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Vertical Angle", $"{enemy.Vision.Angles.AngleToEnemyVerticalSigned.Round100()}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Gain Sight Modifier", $"{enemy.Vision.GainSightCoef.Round100()}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Vision Distance", $"{enemy.Vision.VisionDistance.Round100()}", Color.white, Color.yellow, true);
            }

            stringBuilder.AppendLabeledValue("Can Shoot", $"{enemy.CanShoot}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("In Line of Sight", $"{enemy.InLineOfSight}", Color.white, Color.yellow, true);
            if (_expandedEnemyInfo) {
                var parts = enemy.Vision.VisionChecker.EnemyParts.Parts.Values;
                int visCount = 0;
                int partCount = 0;
                int notChecked = 0;
                foreach (var part in parts) {
                    if (part.TimeSinceLastVisionCheck > 2f) {
                        notChecked++;
                        continue;
                    }
                    partCount++;
                    if (part.LineOfSight) visCount++;
                }
                stringBuilder.AppendLabeledValue("Body Parts", $"In LOS: {visCount} : Checked: {partCount} : Not Checked: {notChecked}", Color.white, Color.yellow, true);
            }
            stringBuilder.AppendLabeledValue("Heard", $"{enemy.Heard}", Color.white, enemy.Seen ? Color.red : Color.white, true);
            if (enemy.Heard) {
                stringBuilder.AppendLabeledValue("Heard Recently?", $"{enemy.Status.HeardRecently}", Color.white, Color.yellow, true);
                if (_expandedEnemyInfo) {
                    addPlaceInfo(stringBuilder, enemy.KnownPlaces.LastHeardPlace, "Last Heard");
                }
            }
        }

        private static float getPercentSpotted(Enemy enemy, out BodyPartType partType)
        {
            float highestPercent = enemy.EnemyInfo.BodyData().Value?.PercentSpotted(out _) ?? 0f;
            partType = BodyPartType.body;
            foreach (var part in enemy.EnemyInfo.AllActiveParts) {
                float percent = part.Value.PercentSpotted(out _);
                if (percent > highestPercent) {
                    highestPercent = percent;
                    partType = part.Key.BodyPartType;
                }
            }

            highestPercent = Mathf.Clamp(highestPercent.Round100(), 0f, 100f);
            return highestPercent;
        }

        private static void addPlaceInfo(StringBuilder stringBuilder, EnemyPlace place, string name)
        {
            if (place != null) {
                stringBuilder.AppendLine($"{name} Data");
                stringBuilder.AppendLabeledValue("Time Since Updated", $"{place.TimeSincePositionUpdated.Round100()}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Enemy Distance", $"{place.DistanceToEnemyRealPosition.Round100()}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Bot Distance", $"{place.DistanceToBot.Round100()}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Searched", $"Personal: {place.HasArrivedPersonal} / Squad: {place.HasArrivedSquad}", Color.white, Color.yellow, true);
            }
        }
    }
}