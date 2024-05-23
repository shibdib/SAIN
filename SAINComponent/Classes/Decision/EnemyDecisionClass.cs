using BepInEx.Logging;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class EnemyDecisionClass : SAINBase, ISAINClass
    {
        public EnemyDecisionClass(Bot sain) : base(sain)
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

        public bool GetDecision(out SoloDecision Decision)
        {
            if (BotOwner.Memory.IsUnderFire)
            {

            }

            SAINEnemy enemy = SAIN.Enemy;
            if (enemy == null)
            {
                Decision = SoloDecision.None;
                return false;
            }

            SAIN.Decision.GoalTargetDecisions.IgnorePlaceTarget = false;

            var CurrentDecision = SAIN.Memory.Decisions.Main.Current;

            if (StartDogFightAction(enemy))
            {
                Decision = SoloDecision.DogFight;
            }
            else if (StartThrowGrenade(enemy))
            {
                Decision = SoloDecision.ThrowGrenade;
            }
            else if (StartMoveToEngage(enemy))
            {
                Decision = SoloDecision.MoveToEngage;
            }
            else if (StartStandAndShoot(enemy))
            {
                if (CurrentDecision != SoloDecision.StandAndShoot)
                {
                    SAIN.Info.CalcHoldGroundDelay();
                }
                Decision = SoloDecision.StandAndShoot;
            }
            else if (shallShootDistantEnemy(enemy))
            {
                Decision = SoloDecision.ShootDistantEnemy;
            }
            else if (StartRushEnemy(enemy))
            {
                Decision = SoloDecision.RushEnemy;
            }
            else if (startSearch())
            {
                if (SAIN.Decision.CurrentSoloDecision != SoloDecision.Search)
                {
                    enemy.EnemyStatus.NumberOfSearchesStarted++;
                }
                if (!enemy.EnemyStatus.SearchStarted)
                {
                    enemy.EnemyStatus.SearchStarted = true;
                }
                Decision = SoloDecision.Search;
            }
            else if (StartShiftCover(enemy))
            {
                Decision = SoloDecision.ShiftCover;
            }
            else if (StartMoveToCover())
            {
                Decision = SoloDecision.MoveToCover;

                if (StartRunForCover())
                {
                    Decision = SoloDecision.RunToCover;
                }
            }
            else if (StartHoldInCover())
            {
                Decision = SoloDecision.HoldInCover;
            }
            else
            {
                Decision = SoloDecision.DebugNoDecision;
            }

            if (Decision != SoloDecision.MoveToCover && Decision != SoloDecision.RunToCover)
            {
                StartRunCoverTimer = 0f;
            }

            return true;
        }

        private bool StayInCoverToSelfCare()
        {
            SelfDecision currentSelf = SAIN.Memory.Decisions.Self.Current;
            SoloDecision currentMain = SAIN.Memory.Decisions.Main.Current;

            if (currentMain == SoloDecision.HoldInCover)
            {

            }
            if (currentSelf != SelfDecision.None && StartHoldInCover())
            {

            }
            return false;
        }

        private static readonly float GrenadeMaxEnemyDistance = 100f;

        private bool StartRunUnknownShooter()
        {

            return false;
        }

        private bool StartThrowGrenade(SAINEnemy enemy)
        {
            if (!GlobalSettings.General.BotsUseGrenades)
            {
                var core = BotOwner.Settings.FileSettings.Core;
                if (core.CanGrenade)
                {
                    core.CanGrenade = false;
                }
                return false;
            }

            var grenades = BotOwner.WeaponManager.Grenades;
            if (!grenades.HaveGrenade)
            {
                return false;
            }
            if (_nextGrenadeCheckTime < Time.time 
                && enemy.LastKnownPosition != null
                && !enemy.IsVisible 
                && enemy.TimeSinceSeen > SAIN.Info.FileSettings.Grenade.TimeSinceSeenBeforeThrow
                && enemy.TimeSinceLastKnownUpdated < 60f
                && (enemy.LastKnownPosition.Value - SAIN.Position).sqrMagnitude < GrenadeMaxEnemyDistance * GrenadeMaxEnemyDistance)
            {
                _nextGrenadeCheckTime = Time.time + 0.5f;
                if (grenades.ReadyToThrow && grenades.AIGreanageThrowData.IsUpToDate())
                {
                    grenades.DoThrow();
                    return true;
                }
                grenades.CanThrowGrenade(enemy.LastKnownPosition.Value + Vector3.up * 0.5f);
            }
            return false;
        }

        private float _nextGrenadeCheckTime;

        private static readonly float RushEnemyMaxPathDistance = 10f;
        private static readonly float RushEnemyMaxPathDistanceSprint = 25f;
        private static readonly float RushEnemyLowAmmoRatio = 0.5f;

        private bool StartRushEnemy(SAINEnemy enemy)
        {
            if (SAIN.Info.PersonalitySettings?.CanRushEnemyReloadHeal == true)
            {
                if (enemy != null 
                    && !SAIN.Decision.SelfActionDecisions.LowOnAmmo(RushEnemyLowAmmoRatio))
                {
                    bool inRange = false;
                    if (enemy.Path.PathDistance < RushEnemyMaxPathDistanceSprint
                        && BotOwner?.CanSprintPlayer == true)
                    {
                        inRange = true;
                    }
                    else if (enemy.Path.PathDistance < RushEnemyMaxPathDistance)
                    {
                        inRange = true;
                    }

                    if (inRange
                        && SAIN.Memory.HealthStatus != ETagStatus.Dying)
                    {
                        var enemyStatus = enemy.EnemyStatus;
                        if (enemyStatus.EnemyIsReloading || enemyStatus.EnemyIsHealing || enemyStatus.EnemyHasGrenadeOut)
                        {
                            return true;
                        }
                        ETagStatus enemyHealth = enemy.EnemyPlayer.HealthStatus;
                        if (enemyHealth == ETagStatus.Dying)
                        {
                            return true;
                        }
                        else if (enemyHealth == ETagStatus.BadlyInjured && enemy.EnemyPlayer.IsInPronePose)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private CoverSettings CoverSettings => SAINPlugin.LoadedPreset.GlobalSettings.Cover;
        private float ShiftCoverChangeDecisionTime => CoverSettings.ShiftCoverChangeDecisionTime;
        private float ShiftCoverTimeSinceSeen => CoverSettings.ShiftCoverTimeSinceSeen;
        private float ShiftCoverTimeSinceEnemyCreated => CoverSettings.ShiftCoverTimeSinceEnemyCreated;
        private float ShiftCoverNoEnemyResetTime => CoverSettings.ShiftCoverNoEnemyResetTime;
        private float ShiftCoverNewCoverTime => CoverSettings.ShiftCoverNewCoverTime;
        private float ShiftCoverResetTime => CoverSettings.ShiftCoverResetTime;

        private bool StartShiftCover(SAINEnemy enemy)
        {
            if (SAIN.Info.PersonalitySettings.CanShiftCoverPosition == false)
            {
                return false;
            }
            if (SAIN.Suppression.IsSuppressed)
            {
                return false;
            }

            if (ContinueShiftCover())
            {
                return true;
            }

            var CurrentDecision = SAIN.Memory.Decisions.Main.Current;

            if (CurrentDecision == SoloDecision.HoldInCover && SAIN.Info.PersonalitySettings.CanShiftCoverPosition)
            {
                if (SAIN.Decision.TimeSinceChangeDecision > ShiftCoverChangeDecisionTime && TimeForNewShift < Time.time)
                {
                    if (enemy != null)
                    {
                        if (enemy.Seen && !enemy.IsVisible && enemy.TimeSinceSeen > ShiftCoverTimeSinceSeen)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            return true;
                        }
                        if (!enemy.Seen && enemy.TimeSinceEnemyCreated > ShiftCoverTimeSinceEnemyCreated)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            return true;
                        }
                    }
                    if (enemy == null && SAIN.Decision.TimeSinceChangeDecision > ShiftCoverNoEnemyResetTime)
                    {
                        TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                        ShiftResetTimer = Time.time + ShiftCoverResetTime;
                        return true;
                    }
                }
            }

            ShiftResetTimer = -1f;
            return false;
        }

        private bool ContinueShiftCover()
        {
            var CurrentDecision = SAIN.Memory.Decisions.Main.Current;
            if (CurrentDecision == SoloDecision.ShiftCover)
            {
                if (ShiftResetTimer > 0f && ShiftResetTimer < Time.time)
                {
                    ShiftResetTimer = -1f;
                    return false;
                }
                if (!BotOwner.Mover.IsMoving)
                {
                    return false;
                }
                if (!ShiftCoverComplete)
                {
                    return true;
                }
            }
            return false;
        }

        private float TimeForNewShift;

        private float ShiftResetTimer;
        public bool ShiftCoverComplete { get; set; }

        private bool StartDogFightAction(SAINEnemy enemy)
        {
            if (SAIN.Decision.CurrentSelfDecision != SelfDecision.None || BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }
            var currentSolo = SAIN.Decision.CurrentSoloDecision;
            if (Time.time - SAIN.Cover.LastHitTime < 2f 
                && currentSolo != SoloDecision.RunAway 
                && currentSolo != SoloDecision.RunToCover
                && currentSolo != SoloDecision.Retreat
                && currentSolo != SoloDecision.MoveToCover)
            {
                return true;
            }

            var pathStatus = enemy.CheckPathDistance();
            return (pathStatus == EnemyPathDistance.VeryClose && SAIN.Enemy.IsVisible) || SAIN.Cover.CoverInUse?.Spotted == true;
        }

        private bool StartMoveToEngage(SAINEnemy enemy)
        {
            if (SAIN.Suppression.IsSuppressed)
            {
                return false;
            }
            if (!enemy.Seen || enemy.TimeSinceSeen < 8f)
            {
                return false;
            }
            if (enemy.IsVisible && enemy.EnemyLookingAtMe)
            {
                return false;
            }
            var decision = SAIN.Decision.CurrentSoloDecision;
            if (BotOwner.Memory.IsUnderFire && decision != SoloDecision.MoveToEngage)
            {
                return false;
            }
            if (decision == SoloDecision.Retreat || decision == SoloDecision.MoveToCover || decision == SoloDecision.RunToCover)
            {
                return false;
            }
            if (enemy.RealDistance > SAIN.Info.WeaponInfo.EffectiveWeaponDistance 
                && decision != SoloDecision.MoveToEngage)
            {
                return true;
            }
            if (enemy.RealDistance > SAIN.Info.WeaponInfo.EffectiveWeaponDistance * 0.66f 
                && decision == SoloDecision.MoveToEngage)
            {
                return true;
            }
            return false;
        }

        private bool shallShootDistantEnemy(SAINEnemy enemy)
        {
            if (_endShootDistTargetTime > Time.time 
                && SAIN.Decision.CurrentSoloDecision == SoloDecision.ShootDistantEnemy 
                && SAIN.Memory.HealthStatus != ETagStatus.Dying)
            {
                return true;
            }
            if (_nextShootDistTargetTime < Time.time 
                && enemy.RealDistance > SAIN.Info.FileSettings.Shoot.MaxPointFireDistance 
                && enemy.IsVisible 
                && enemy.CanShoot 
                && (SAIN.Memory.HealthStatus == ETagStatus.Healthy || SAIN.Memory.HealthStatus == ETagStatus.Injured))
            {
                float timeAdd = 3f * UnityEngine.Random.Range(0.75f, 1.25f); ;
                _nextShootDistTargetTime = Time.time + timeAdd;
                _endShootDistTargetTime = Time.time + timeAdd / 2f;
                return true;
            }
            return false;
        }

        private float _nextShootDistTargetTime;
        private float _endShootDistTargetTime;

        private float EndThrowTimer = 0f;

        private bool ContinueThrow()
        {
            return false;
            //if (SAIN.Grenade.EFTBotGrenade.AIGreanageThrowData == null || Time.time - EndThrowTimer > 3f)
            //{
            //    return false;
            //}
            //return CurrentDecision == SoloDecision.ThrowGrenadeAction && SAIN.Grenade.EFTBotGrenade.AIGreanageThrowData?.ThrowComplete == false;
        }

        private bool StartRunForCover()
        {
            if (!BotOwner.CanSprintPlayer)
            {
                return false;
            }

            bool runNow = SAIN.Enemy != null
                && !SAIN.Enemy.IsVisible
                && (!SAIN.Enemy.Seen || SAIN.Enemy.TimeSinceSeen > 3f || BotOwner.Memory.IsUnderFire)
                && SAIN.Cover.CoverPoints.Count > 0;

            if (StartRunCoverTimer < Time.time || runNow)
            {
                CoverPoint coverInUse = SAIN.Cover.CoverInUse;
                if (coverInUse != null)
                {
                    return (coverInUse.Position - SAIN.Position).sqrMagnitude >= 1f;
                }
                return true;
            }
            return false;
        }

        private static readonly float RunToCoverTime = 1.5f;
        private static readonly float RunToCoverTimeRandomMin = 0.66f;
        private static readonly float RunToCoverTimeRandomMax = 1.33f;

        private bool StartMoveToCover()
        {
            CoverPoint coverInUse = SAIN.Cover.CoverInUse;
            if (coverInUse == null || coverInUse.Status != CoverStatus.InCover || coverInUse.Spotted)
            {
                var CurrentDecision = SAIN.Memory.Decisions.Main.Current;
                if (CurrentDecision != SoloDecision.MoveToCover && CurrentDecision != SoloDecision.RunToCover)
                {
                    StartRunCoverTimer = Time.time + RunToCoverTime * UnityEngine.Random.Range(RunToCoverTimeRandomMin, RunToCoverTimeRandomMax);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool startSearch()
        {
            return SAIN.Search.ShallStartSearch(out _, true);
        }

        private float _nextRecalcSearchTime;
        private float TimeBeforeSearch => SAIN.Info.TimeBeforeSearch;

        private static readonly float HoldInCoverMaxCoverDist = 0.75f * 0.75f;

        public bool StartHoldInCover()
        {
            var cover = SAIN.Cover.CoverInUse;
            if (cover != null 
                && !cover.Spotted
                && cover.Status == CoverStatus.InCover)
            {
                return true;
            }
            return false;
        }

        private bool StartStandAndShoot(SAINEnemy enemy)
        {
            if (enemy.IsVisible && enemy.CanShoot)
            {
                if (enemy.RealDistance > SAIN.Info.WeaponInfo.EffectiveWeaponDistance * 1.25f)
                {
                    return false;
                }
                float holdGround = SAIN.Info.HoldGroundDelay;

                if (holdGround <= 0f)
                {
                    return false;
                }

                if (!enemy.EnemyLookingAtMe)
                {
                    CoverPoint closestPoint = SAIN.Cover.ClosestPoint;
                    if (!enemy.EnemyLookingAtMe && closestPoint != null && closestPoint.Status <= CoverStatus.CloseToCover)
                    {
                        return true;
                    }
                }

                float visibleFor = Time.time - enemy.VisibleStartTime;

                if (visibleFor < holdGround)
                {
                    if (visibleFor < holdGround / 1.5f)
                    {
                        return true;
                    }
                    else
                    {
                        return SAIN.Cover.CheckLimbsForCover();
                    }
                }
            }
            return false;
        }

        private float StartRunCoverTimer;
    }
}
