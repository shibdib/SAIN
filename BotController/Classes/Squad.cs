using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.BotController.Classes
{
    public class Squad
    {
        public Squad()
        {
            CheckSquadTimer = Time.time + 10f;
        }

        public string GetId()
        {
            if (Id.IsNullOrEmpty())
            {
                return GUID;
            }
            else
            {
                return Id;
            }
        }

        public List<PlaceForCheck> PlacesForCheck => EFTBotGroup?.PlacesForCheck;

        public bool IsPointTooCloseToLastPlaceForCheck(Vector3 position)
        {
            PlaceForCheck mostRecentPlace = null;
            if (PlacesForCheck != null && PlacesForCheck.Count > 0)
            {
                mostRecentPlace = PlacesForCheck[PlacesForCheck.Count - 1];

                if (mostRecentPlace != null && (position - mostRecentPlace.Position).sqrMagnitude < 2)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddPointToSearch(Vector3 position, float soundPower, BotOwner botOwner, AISoundType soundType, Vector3 originalPosition, IPlayer player)
        {
            PlaceForCheck mostRecentPlace = null;
            bool isDanger = soundType == AISoundType.step ? false : true;
            PlaceForCheckType checkType = isDanger ? PlaceForCheckType.danger : PlaceForCheckType.simple;

            // Compare the most recent place for check with this new one we're adding.
            // If they are close by, just update that one instead of creating a new instance
            if (PlacesForCheck != null && PlacesForCheck.Count > 0)
            {
                mostRecentPlace = PlacesForCheck[PlacesForCheck.Count - 1];

                if (mostRecentPlace != null && (position - mostRecentPlace.Position).sqrMagnitude < 2)
                {
                    if (isDanger)
                    {
                        mostRecentPlace.Type = PlaceForCheckType.danger;
                    }
                    mostRecentPlace.BasePoint = position;
                    return;
                }
            }
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, -1))
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(botOwner.Position, hit.position, -1, path))
                {
                    position = hit.position;

                    try
                    {
                        PlaceForCheck placeForCheck = new PlaceForCheck(position, checkType);
                        PlacesForCheck.Add(placeForCheck);

                        if (!botOwner.Memory.GoalTarget.HavePlaceTarget() && botOwner.Memory.GoalEnemy == null)
                        {
                            botOwner.BotsGroup.CalcGoalForBot(botOwner);
                        }
                    }
                    catch { }
                }

                if (player != null)
                {
                    foreach (var member in Members)
                    {
                        SAINEnemy enemy = member.Value?.EnemyController?.CheckAddEnemy(player); 
                        enemy?.SetHeardStatus(true, position);
                    }
                }
            }
        }

        private BotsGroup EFTBotGroup;

        public string Id { get; private set; } = string.Empty;

        public readonly string GUID = Guid.NewGuid().ToString("N");

        public bool SquadReady { get; private set; }

        public Action<IPlayer, DamageInfo, float> LeaderKilled { get; set; }
        public Action<IPlayer, DamageInfo, float> MemberKilled { get; set; }

        public Action<SAINComponentClass, float> NewLeaderFound { get; set; }

        public bool LeaderIsDeadorNull => LeaderComponent?.Player == null || LeaderComponent?.Player?.HealthController.IsAlive == false;

        public float TimeThatLeaderDied { get; private set; }

        public const float FindLeaderAfterKilledCooldown = 60f;

        public SAINComponentClass LeaderComponent { get; private set; }
        public string LeaderId { get; private set; }

        public float LeaderPowerLevel { get; private set; }

        public bool MemberIsFallingBack { get; private set; }

        public readonly List<SoloDecision> SquadSoloDecisions = new List<SoloDecision>();

        public readonly List<SquadDecision> SquadDecisions = new List<SquadDecision>();

        public readonly List<Vector3> SquadLocations = new List<Vector3>();

        public float SquadPowerLevel
        {
            get
            {
                float result = 0f;
                foreach (var memberInfo in MemberInfos.Values)
                {
                    if (memberInfo.SAIN != null && memberInfo.SAIN.IsDead == false)
                    {
                        result += memberInfo.PowerLevel;
                    }
                }
                return result;
            }
        }

        public void Update()
        {
            // After 10 seconds since squad is originally created,
            // find a squad leader and activate the squad to give time for all bots to spawn in
            // since it can be staggered over a few seconds.
            if (!SquadReady && CheckSquadTimer < Time.time && Members.Count > 0)
            {
                FindSquadLeader(); 
                SquadReady = true;
                // Timer before starting to recheck
                RecheckSquadTimer = Time.time + 10f;
            }

            // Check happens once the squad is originally "activated" and created
            // Wait until all members are out of combat to find a squad leader, or 60 seconds have passed to find a new squad leader is they are KIA
            if (SquadReady)
            {
                if (RecheckSquadTimer < Time.time && LeaderIsDeadorNull)
                {
                    RecheckSquadTimer = Time.time + 3f;

                    if (TimeThatLeaderDied < Time.time + FindLeaderAfterKilledCooldown)
                    {
                        FindSquadLeader();
                    }
                    else
                    {
                        bool outOfCombat = true;
                        foreach (var member in MemberInfos.Values)
                        {
                            if (member.HasEnemy == true)
                            {
                                outOfCombat = false;
                                break;
                            }
                        }
                        if (outOfCombat)
                        {
                            FindSquadLeader();
                        }
                    }
                }
            }
        }

        public void CheckMembers()
        {
            SquadLocations.Clear();
            SquadSoloDecisions.Clear();
            SquadDecisions.Clear();

            foreach (var memberInfo in MemberInfos.Values)
            {
                var sain = memberInfo.SAIN;
                if (sain != null)
                {
                    SquadSoloDecisions.Add(sain.Memory.Decisions.Main.Current);
                    SquadDecisions.Add(sain.Decision.CurrentSquadDecision);
                    SquadLocations.Add(sain.Position);
                }
            }

            MemberIsFallingBack = SquadSoloDecisions.Contains(SoloDecision.Retreat) 
                || SquadSoloDecisions.Contains(SoloDecision.RunToCover) 
                || SquadSoloDecisions.Contains(SoloDecision.RunAway);
        }

        private float RecheckSquadTimer;
        private float CheckSquadTimer;

        private void MemberWasKilled(Player player, IPlayer lastAggressor, DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
        {
            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $"Member [{player?.Profile.Nickname}] " +
                    $"was killed for Squad: [{Id}] " +
                    $"by [{lastAggressor?.Profile.Nickname}] " +
                    $"at Time: [{Time.time}] " +
                    $"by damage type: [{lastDamageInfo.DamageType}] " +
                    $"to Body part: [{lastBodyPart}]"
                    );
            }

            MemberKilled?.Invoke(lastAggressor, lastDamageInfo, Time.time);

            if (MemberInfos.TryGetValue(player?.ProfileId, out var member) 
                && member != null)
            {
                // If this killed Member is the squad leader then
                if (member.ProfileId == LeaderId)
                {
                    if (SAINPlugin.DebugMode)
                        Logger.LogInfo($"Leader [{player?.Profile.Nickname}] was killed for Squad: [{Id}]");

                    LeaderKilled?.Invoke(lastAggressor, lastDamageInfo, Time.time);
                    TimeThatLeaderDied = Time.time;
                    LeaderComponent = null;
                }
            }

            RemoveMember(player?.ProfileId);
        }

        public void MemberExtracted(SAINComponentClass sain)
        {
            if (SAINPlugin.DebugMode)
                Logger.LogInfo($"Leader [{sain?.Player?.Profile.Nickname}] Extracted for Squad: [{Id}]");
            RemoveMember(sain?.ProfileId);
        }

        private void FindSquadLeader()
        {
            float power = 0f;
            SAINComponentClass leadComponent = null;

            // Iterate through each memberInfo memberInfo in friendly group to see who has the highest power level or if any are bosses
            foreach (var memberInfo in MemberInfos.Values)
            {
                if (memberInfo.SAIN == null || memberInfo.SAIN.IsDead) continue;

                // If this memberInfo is a boss type, they are the squad leader
                bool isBoss = memberInfo.SAIN.Info.Profile.IsBoss;
                // or If this memberInfo has a higher power level than the last one we checked, they are the squad leader
                if (isBoss || memberInfo.PowerLevel > power)
                {
                    power = memberInfo.PowerLevel;
                    leadComponent = memberInfo.SAIN;

                    if (isBoss)
                    {
                        break;
                    }
                }
            }

            if (leadComponent != null)
            {
                AssignSquadLeader(leadComponent);
            }
        }

        private void AssignSquadLeader(SAINComponentClass sain)
        {
            if (sain?.Player == null)
            {
                Logger.LogError($"Tried to Assign Null SAIN Component or Player for Squad [{Id}], skipping");
                return;
            }

            LeaderComponent = sain;
            LeaderPowerLevel = sain.Info.Profile.PowerLevel;
            LeaderId = sain.Player?.ProfileId;

            NewLeaderFound?.Invoke(sain, Time.time);

            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $" Found New Leader. Name [{sain.BotOwner?.Profile?.Nickname}]" +
                    $" for Squad: [{Id}]" +
                    $" at Time: [{Time.time}]" +
                    $" Group Size: [{Members.Count}]"
                    );
            }
        }

        public void AddMember(SAINComponentClass sain)
        {
            // Make sure nothing is null as a safety check.
            if (sain?.Player != null && sain.BotOwner != null)
            {
                // Make sure this profile ID doesn't already exist for whatever reason
                if (!Members.ContainsKey(sain.ProfileId))
                {
                    // If this is the first member, add their side to the start of their ID for easier identifcation during debug
                    if (Members.Count == 0)
                    {
                        EFTBotGroup = sain.BotOwner.BotsGroup;
                        Id = sain.Player.Profile.Side.ToString() + "_" + GUID;
                    }

                    var memberInfo = new MemberInfo(sain);
                    MemberInfos.Add(sain.ProfileId, memberInfo);
                    Members.Add(sain.ProfileId, sain);

                    // if this new member is a boss, set them to leader automatically
                    if (sain.Info.Profile.IsBoss)
                    {
                        AssignSquadLeader(sain);
                    }
                    // If this new memberInfo has a higher power level than the existing squad leader, set them as the new squad leader if they aren't a boss type
                    else if (LeaderComponent != null && sain.Info.Profile.PowerLevel > LeaderPowerLevel && !LeaderComponent.Info.Profile.IsBoss)
                    {
                        AssignSquadLeader(sain);
                    }

                    // Subscribe when this member is killed
                    sain.Player.OnPlayerDead += MemberWasKilled;
                }
            }
        }

        public void RemoveMember(SAINComponentClass sain)
        {
            RemoveMember(sain?.ProfileId);
        }

        public void RemoveMember(string id)
        {
            if (Members.ContainsKey(id))
            {
                Members.Remove(id);
            }
            if (MemberInfos.TryGetValue(id, out var memberInfo))
            {
                Player player = memberInfo.SAIN?.Player;
                if (player != null)
                {
                    player.OnPlayerDead -= MemberWasKilled;
                }
                memberInfo.Dispose();
                MemberInfos.Remove(id);
            }
        }

        public readonly Dictionary<string, SAINComponentClass> Members = new Dictionary<string, SAINComponentClass>();
        public readonly Dictionary<string, MemberInfo> MemberInfos = new Dictionary<string, MemberInfo>();
    }
}
