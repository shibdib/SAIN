using BepInEx.Logging;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SquadDecisionClass : SAINBase, ISAINClass
    {
        public SquadDecisionClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        private SAINSquadClass Squad => SAIN.Squad;

        float SquaDDecision_DontDoSquadDecision_EnemySeenRecentTime = 3f;

        public bool GetDecision(out SquadDecision Decision)
        {
            Decision = SquadDecision.None;
            if (!Squad.BotInGroup || SAIN.Squad.SquadInfo?.LeaderComponent == null || Squad.LeaderComponent?.IsDead == true)
            {
                return false;
            }

            if (EnemyDecision(out Decision))
            {
                return true;
            }
            if (shallRegroup())
            {
                Decision = SquadDecision.Regroup;
                return true;
            }

            return false;
        }

        float SquaDecision_RadioCom_MaxDistSq = 1200f;
        float SquadDecision_MyEnemySeenRecentTime = 10f;

        private bool EnemyDecision(out SquadDecision Decision)
        {
            Decision = SquadDecision.None;
            SAINEnemy myEnemy = SAIN.Enemy;

            if (shallPushSuppressedEnemy(myEnemy))
            {
                Decision = SquadDecision.PushSuppressedEnemy;
                return true;
            }
            if (myEnemy != null)
            {
                if (myEnemy.IsVisible || myEnemy.TimeSinceSeen < SquadDecision_MyEnemySeenRecentTime)
                {
                    return false;
                }
            }
            if (shallGroupSearch())
            {
                Decision = SquadDecision.GroupSearch;
                return true;
            }

            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member == null || member.BotOwner == BotOwner || member.BotOwner.IsDead)
                {
                    continue;
                }
                if (!HasRadioComms && (SAIN.Transform.Position - member.Transform.Position).sqrMagnitude > SquaDecision_RadioCom_MaxDistSq)
                {
                    continue;
                }
                if (myEnemy != null 
                    && member.HasEnemy)
                {
                    if (myEnemy.EnemyIPlayer == member.Enemy.EnemyIPlayer)
                    {
                        if (shallSuppressEnemy(member))
                        {
                            Decision = SquadDecision.Suppress;
                            return true;
                        }
                        if (shallHelp(member))
                        {
                            Decision = SquadDecision.Help;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static readonly float PushSuppressedEnemyMaxPathDistance = 75f;
        private static readonly float PushSuppressedEnemyMaxPathDistanceSprint = 100f;
        private static readonly float PushSuppressedEnemyLowAmmoRatio = 0.5f;

        private bool shallPushSuppressedEnemy(SAINEnemy enemy)
        {
            if (enemy != null
                && !SAIN.Decision.SelfActionDecisions.LowOnAmmo(PushSuppressedEnemyLowAmmoRatio) 
                && SAIN.Info.PersonalitySettings.CanRushEnemyReloadHeal)
            {
                bool inRange = false;
                if (enemy.Path.PathDistance < PushSuppressedEnemyMaxPathDistanceSprint
                    && BotOwner?.CanSprintPlayer == true)
                {
                    inRange = true;
                }
                else if (enemy.Path.PathDistance < PushSuppressedEnemyMaxPathDistance)
                {
                    inRange = true;
                }

                if (inRange
                    && SAIN.Memory.HealthStatus == ETagStatus.Healthy
                    && SAIN.Squad.SquadInfo.SquadIsSuppressEnemy(enemy.EnemyPlayer.ProfileId, out var suppressingMember) 
                    && suppressingMember != SAIN)
                {
                    var enemyStatus = enemy.EnemyStatus;
                    if (enemyStatus.EnemyIsReloading || enemyStatus.EnemyIsHealing || enemyStatus.EnemyHasGrenadeOut)
                    {
                        return true;
                    }
                    ETagStatus enemyHealth = enemy.EnemyPlayer.HealthStatus;
                    if (enemyHealth == ETagStatus.Dying || enemyHealth == ETagStatus.BadlyInjured)
                    {
                        return true;
                    }
                    else if (enemy.EnemyPlayer.IsInPronePose)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HasRadioComms => SAIN.Equipment.HasEarPiece;

        float SquadDecision_SuppressFriendlyDistStart = 30f;
        float SquadDecision_SuppressFriendlyDistEnd = 50f;

        private bool shallSuppressEnemy(SAINComponentClass member)
        {
            if (SAIN.Enemy?.SuppressionTarget == null)
            {
                return false;
            }
            if (SAIN.Enemy?.IsVisible == true)
            {
                return false;
            }
            if (member.Memory.Decisions.Main.Current != SoloDecision.Retreat)
            {
                return false;
            }

            float memberDistance = (member.Transform.Position - BotOwner.Position).magnitude;
            float ammo = SAIN.Decision.SelfActionDecisions.AmmoRatio;

            if (SAIN.Memory.Decisions.Squad.Current == SquadDecision.Suppress)
            {
                return memberDistance <= SquadDecision_SuppressFriendlyDistEnd && ammo >= 0.1f;
            }
            return memberDistance <= SquadDecision_SuppressFriendlyDistStart && ammo >= 0.5f;
        }

        private bool shallGroupSearch(SAINComponentClass member)
        {
            bool squadSearching = member.Memory.Decisions.Main.Current == SoloDecision.Search || member.Decision.CurrentSquadDecision == SquadDecision.Search;
            if (squadSearching)
            {
                return true;
            }
            return false;
        }

        private bool shallGroupSearch()
        {
            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member.Memory.Decisions.Main.Current == SoloDecision.Search)
                {
                    if (SAIN.Enemy != null
                        && doesMemberShareEnemy(member))
                    {
                        return true;
                    }
                    if (SAIN.Enemy == null
                        && SAIN.CurrentTargetPosition != null
                        && doesMemberShareTarget(member, SAIN.CurrentTargetPosition.Value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool doesMemberShareTarget(SAINComponentClass member, Vector3 targetPosition, float maxDist = 20f)
        {
            if (member == null || member.ProfileId == SAIN.ProfileId || member.BotOwner?.IsDead == true)
            {
                return false;
            }

            return member.CurrentTargetPosition != null 
                && (member.CurrentTargetPosition.Value - targetPosition).sqrMagnitude < maxDist;
        }
        private bool doesMemberShareEnemy(SAINComponentClass member)
        {
            if (member == null || member.ProfileId == SAIN.ProfileId || member.BotOwner?.IsDead == true)
            {
                return false;
            }

            return member.Enemy != null
                && member.Enemy.EnemyPlayer.ProfileId == SAIN.Enemy.EnemyPlayer.ProfileId;
        }

        float SquadDecision_StartHelpFriendDist = 30f;
        float SquadDecision_EndHelpFriendDist = 45f;
        float SquadDecision_EndHelp_FriendsEnemySeenRecentTime = 8f;

        private bool shallHelp(SAINComponentClass member)
        {
            float distance = member.Enemy.Path.PathDistance;
            bool visible = member.Enemy.IsVisible;

            if (SAIN.Memory.Decisions.Squad.Current == SquadDecision.Help 
                && member.Enemy.Seen)
            {
                return distance < SquadDecision_EndHelpFriendDist
                    && member.Enemy.TimeSinceSeen < SquadDecision_EndHelp_FriendsEnemySeenRecentTime;
            }
            return distance < SquadDecision_StartHelpFriendDist && visible;
        }

        float SquadDecision_Regroup_NoEnemy_StartDist = 125f;
        float SquadDecision_Regroup_NoEnemy_EndDistance = 50f;
        float SquadDecision_Regroup_Enemy_StartDist = 50f;
        float SquadDecision_Regroup_Enemy_EndDistance = 15f;
        float SquadDecision_Regroup_EnemySeenRecentTime = 60f;

        public bool shallRegroup()
        {
            var squad = SAIN.Squad;
            if (squad.IAmLeader)
            {
                return false;
            }

            float maxDist = SquadDecision_Regroup_NoEnemy_StartDist;
            float minDist = SquadDecision_Regroup_NoEnemy_EndDistance;

            var enemy = SAIN.Enemy;
            if (enemy != null)
            {
                if (enemy.IsVisible || (enemy.Seen && enemy.TimeSinceSeen < SquadDecision_Regroup_EnemySeenRecentTime))
                {
                    return false;
                }
                maxDist = SquadDecision_Regroup_Enemy_StartDist;
                minDist = SquadDecision_Regroup_Enemy_EndDistance;
            }

            var lead = squad.LeaderComponent;
            if (lead != null)
            {
                Vector3 BotPos = BotOwner.Position;
                Vector3 leadPos = lead.Transform.Position;
                Vector3 directionToLead = leadPos - BotPos;
                float leadDistance = directionToLead.magnitude;
                if (enemy != null)
                {
                    Vector3 EnemyPos = enemy.EnemyPosition;
                    Vector3 directionToEnemy = EnemyPos - BotPos;
                    float EnemyDistance = directionToEnemy.magnitude;
                    if (EnemyDistance < leadDistance)
                    {
                        if (EnemyDistance < 30f && Vector3.Dot(directionToEnemy.normalized, directionToLead.normalized) > 0.25f)
                        {
                            return false;
                        }
                    }
                }
                if (SAIN.Memory.Decisions.Squad.Current == SquadDecision.Regroup)
                {
                    return leadDistance > minDist;
                }
                else
                {
                    return leadDistance > maxDist;
                }
            }
            return false;
        }
    }
}