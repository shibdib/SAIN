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
using static UnityEngine.EventSystems.EventTrigger;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class EnemyDecisionClass : SAINBase, ISAINClass
    {
        public EnemyDecisionClass(BotComponent sain) : base(sain)
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
            SAINEnemy enemy = SAINBot.Enemy;
            if (enemy == null)
            {
                Decision = SoloDecision.None;
                return false;
            }

            SAINBot.Decision.GoalTargetDecisions.IgnorePlaceTarget = false;

            var CurrentDecision = SAINBot.Decision.CurrentSoloDecision;

            if (shallDogFight(enemy))
            {
                Decision = SoloDecision.DogFight;
            }
            else if (shallThrowGrenade(enemy))
            {
                Decision = SoloDecision.ThrowGrenade;
            }
            else if (shallStandAndShoot(enemy))
            {
                if (CurrentDecision != SoloDecision.StandAndShoot)
                {
                    SAINBot.Info.CalcHoldGroundDelay();
                }
                Decision = SoloDecision.StandAndShoot;
            }
            else if (shallShootDistantEnemy(enemy))
            {
                Decision = SoloDecision.ShootDistantEnemy;
            }
            else if (shallRushEnemy(enemy))
            {
                Decision = SoloDecision.RushEnemy;
            }
            else if (shallSearch())
            {
                if (SAINBot.Decision.CurrentSoloDecision != SoloDecision.Search)
                {
                    enemy.EnemyStatus.NumberOfSearchesStarted++;
                }
                if (!enemy.EnemyStatus.SearchStarted)
                {
                    enemy.EnemyStatus.SearchStarted = true;
                }
                Decision = SoloDecision.Search;
            }
            else if (shallFreezeAndWait(enemy))
            {
                Decision = SoloDecision.Freeze;
            }
            else if (shallMoveToEngage(enemy))
            {
                Decision = SoloDecision.MoveToEngage;
            }
            else if (shallShiftCover(enemy))
            {
                Decision = SoloDecision.ShiftCover;
            }
            else if (shallMoveToCover())
            {
                Decision = SoloDecision.MoveToCover;

                if (shallRunForCover())
                {
                    Decision = SoloDecision.RunToCover;
                }
            }
            else if (shallHoldInCover())
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

        private static readonly float GrenadeMaxEnemyDistance = 100f;

        private void checkFreezeTime()
        {
            if (SAINBot.Decision.CurrentSoloDecision != SoloDecision.Freeze)
            {
                FreezeFor = UnityEngine.Random.Range(10f, 120f);
                UnFreezeTime = Time.time + FreezeFor;
            }
        }

        private bool shallFreezeAndWait(SAINEnemy enemy)
        {
            if (enemy.EnemyHeardFromPeace && 
                SAINBot.Memory.Location.IsIndoors &&
                (!enemy.Seen || enemy.TimeSinceSeen > 240f) && 
                enemy.TimeSinceLastKnownUpdated < 90f)
            {
                checkFreezeTime();

                if (UnFreezeTime > Time.time)
                {
                    return true;
                }
            }

            if (enemy.EnemyHeardFromPeace)
                enemy.EnemyHeardFromPeace = false;

            return false;
        }

        public float FreezeFor { get; private set; }
        public float UnFreezeTime { get; private set; }

        private bool checkIndoors()
        {
            var gameWorld = SAINGameworldComponent.Instance;
            if (gameWorld != null)
            {
                if (gameWorld.Location == ELocation.Factory)
                {
                    return true;
                }
                if (gameWorld.Location == ELocation.Labs)
                {
                    return true;
                }
            }
            return SAINBot.Memory.Location.IsIndoors;
        }

        private bool shallThrowGrenade(SAINEnemy enemy)
        {
            if (!GlobalSettings.General.BotsUseGrenades)
            {
                //var core = BotOwner.Settings.FileSettings.Core;
                //if (core.CanGrenade)
                //{
                //    core.CanGrenade = false;
                //}
                return false;
            }

            var grenades = BotOwner.WeaponManager.Grenades;
            if (!grenades.HaveGrenade)
            {
                if (_nextSayNeedGrenadeTime < Time.time)
                {
                    _nextSayNeedGrenadeTime = Time.time + 10;
                    SAINBot.Talk.GroupSay(EPhraseTrigger.NeedFrag, null, true, 5);
                }
                return false;
            }
            if (tryThrowGrenade())
            {
                SAINBot.Talk.GroupSay(EPhraseTrigger.OnGrenade, null, true, 75);
                return true;
            }
            if (_nextGrenadeCheckTime < Time.time && 
                canTryThrow(enemy))
            {
                _nextGrenadeCheckTime = Time.time + 0.25f;
                if (findThrowTarget(enemy))
                {
                    return true;
                }
            }
            return false;
        }

        private float _nextSayNeedGrenadeTime;

        private bool canTryThrow(SAINEnemy enemy)
        {
            return !enemy.IsVisible
                && enemy.TimeSinceSeen > SAINBot.Info.FileSettings.Grenade.TimeSinceSeenBeforeThrow
                && enemy.TimeSinceLastKnownUpdated < 120f;
        }

        int throwTarget = 0;

        private bool findThrowTarget(SAINEnemy enemy)
        {
            throwTarget++;
            if (throwTarget == 1 && 
                enemy.LastCornerToEnemy != null &&
                enemy.CanSeeLastCornerToEnemy &&
                tryThrowToPos(enemy.LastCornerToEnemy.Value, "LastCornerToEnemy"))
            {
                return true;
            }
            if (throwTarget == 2 && 
                enemy.Path.BlindCornerToEnemy != null &&
                enemy.LastKnownPosition != null &&
                (enemy.Path.BlindCornerToEnemy.Value - enemy.LastKnownPosition.Value).sqrMagnitude < 5f * 5f &&
                tryThrowToPos(enemy.Path.BlindCornerToEnemy.Value, "BlindCornerToEnemy"))
            {
                return true;
            }
            if (throwTarget == 3 && 
                enemy.LastKnownPosition != null &&
                tryThrowToPos(enemy.LastKnownPosition.Value, "LastKnownPosition"))
            {
                return true;
            }

            if (throwTarget >= 3)
                throwTarget = 0;

            return false;
        }

        private bool tryThrowToPos(Vector3 pos, string posString)
        {
            pos += Vector3.up * 0.25f;
            if (checkCanThrowFromPos(pos))
            {
                Logger.LogDebug($"{posString} Can Throw to pos");
                return true;
            }
            if (checkCanThrowFromPos(pos + (pos - SAINBot.Position).normalized))
            {
                Logger.LogDebug($"{posString} Can Throw to pos + (pos - SAINBot.Position).normalized");
                return true;
            }
            if (checkCanThrowFromPos(pos + Vector3.up))
            {
                Logger.LogDebug($"{posString} Can Throw to pos + vector3.up");
                return true;
            }
            if (checkCanThrowFromPos(pos + Vector3.up + UnityEngine.Random.onUnitSphere))
            {
                Logger.LogDebug($"{posString} Can Throw to pos + Vector3.up + UnityEngine.Random.onUnitSphere");
                return true;
            }
            return false;
        }

        private bool tryThrowGrenade()
        {
            var grenades = BotOwner.WeaponManager.Grenades;
            if (grenades.ReadyToThrow && grenades.AIGreanageThrowData.IsUpToDate())
            {
                SAINBot.Steering.LookToDirection(grenades.AIGreanageThrowData.Direction, false);
                grenades.DoThrow();
                return true;
            }
            return false;
        }

        private bool checkCanThrowFromPos(Vector3? pos)
        {
            return pos != null && checkCanThrowFromPos(pos.Value);
        }

        private bool checkCanThrowFromPos(Vector3 pos)
        {
            if ((pos - SAINBot.Position).sqrMagnitude < GrenadeMaxEnemyDistance * GrenadeMaxEnemyDistance)
            {
                return BotOwner.WeaponManager.Grenades.CanThrowGrenade(pos);
            }
            return false;
        }

        private float _nextGrenadeCheckTime;

        private static readonly float RushEnemyMaxPathDistance = 10f;
        private static readonly float RushEnemyMaxPathDistanceSprint = 20f;
        private static readonly float RushEnemyLowAmmoRatio = 0.4f;

        private bool shallRushEnemy(SAINEnemy enemy)
        {
            if (SAINBot.Info.PersonalitySettings?.Rush.CanRushEnemyReloadHeal == true && enemy != null)
            {
                if (!SAINBot.Decision.SelfActionDecisions.LowOnAmmo(RushEnemyLowAmmoRatio))
                {
                    bool inRange = false;
                    EEnemyAction vulnerableAction = enemy.EnemyStatus.VulnerableAction;
                    float modifier = vulnerableAction == EEnemyAction.UsingSurgery ? 2f : 1f;
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
                        && SAINBot.Memory.Health.HealthStatus != ETagStatus.Dying
                        && SAINBot.Memory.Health.HealthStatus != ETagStatus.BadlyInjured)
                    {
                        if (vulnerableAction != EEnemyAction.None)
                        {
                            return true;
                        }
                        ETagStatus enemyHealth = enemy.EnemyPlayer.HealthStatus;
                        if (enemyHealth == ETagStatus.Dying || enemyHealth == ETagStatus.BadlyInjured)
                        {
                            return true;
                        }
                        else if (enemyHealth == ETagStatus.Injured && enemy.EnemyPlayer.IsInPronePose)
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

        private bool shallShiftCover(SAINEnemy enemy)
        {
            if (SAINBot.Info.PersonalitySettings.Cover.CanShiftCoverPosition == false)
            {
                return false;
            }
            if (SAINBot.Suppression.IsSuppressed)
            {
                return false;
            }

            if (ContinueShiftCover())
            {
                return true;
            }

            var CurrentDecision = SAINBot.Decision.CurrentSoloDecision;

            if (CurrentDecision == SoloDecision.HoldInCover && SAINBot.Info.PersonalitySettings.Cover.CanShiftCoverPosition)
            {
                if (SAINBot.Decision.TimeSinceChangeDecision > ShiftCoverChangeDecisionTime && TimeForNewShift < Time.time)
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
                    if (enemy == null && SAINBot.Decision.TimeSinceChangeDecision > ShiftCoverNoEnemyResetTime)
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
            var CurrentDecision = SAINBot.Decision.CurrentSoloDecision;
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

        private bool shallDogFight(SAINEnemy enemy)
        {
            if (SAINBot.Decision.CurrentSelfDecision != SelfDecision.None || BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }

            if (SAINBot.Decision.CurrentSoloDecision == SoloDecision.RushEnemy)
            {
                return false;
            }

            var currentSolo = SAINBot.Decision.CurrentSoloDecision;
            if (Time.time - SAINBot.Cover.LastHitTime < 2f 
                && currentSolo != SoloDecision.RunAway 
                && currentSolo != SoloDecision.RunToCover
                && currentSolo != SoloDecision.Retreat
                && currentSolo != SoloDecision.MoveToCover)
            {
                return true;
            }

            var pathStatus = enemy.CheckPathDistance();
            return (pathStatus == EnemyPathDistance.VeryClose && SAINBot.Enemy.IsVisible) || SAINBot.Cover.CoverInUse?.Spotted == true;
        }

        private bool shallMoveToEngage(SAINEnemy enemy)
        {
            if (SAINBot.Suppression.IsSuppressed)
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
            var decision = SAINBot.Decision.CurrentSoloDecision;
            if (BotOwner.Memory.IsUnderFire && decision != SoloDecision.MoveToEngage)
            {
                return false;
            }
            if (decision == SoloDecision.Retreat || decision == SoloDecision.MoveToCover || decision == SoloDecision.RunToCover)
            {
                return false;
            }
            if (enemy.RealDistance > SAINBot.Info.WeaponInfo.EffectiveWeaponDistance 
                && decision != SoloDecision.MoveToEngage)
            {
                return true;
            }
            if (enemy.RealDistance > SAINBot.Info.WeaponInfo.EffectiveWeaponDistance * 0.66f 
                && decision == SoloDecision.MoveToEngage)
            {
                return true;
            }
            return false;
        }

        private bool shallShootDistantEnemy(SAINEnemy enemy)
        {
            if (_endShootDistTargetTime > Time.time 
                && SAINBot.Decision.CurrentSoloDecision == SoloDecision.ShootDistantEnemy 
                && SAINBot.Memory.Health.HealthStatus != ETagStatus.Dying)
            {
                return true;
            }
            if (_nextShootDistTargetTime < Time.time 
                && enemy.RealDistance > SAINBot.Info.FileSettings.Shoot.MaxPointFireDistance 
                && enemy.IsVisible 
                && enemy.CanShoot 
                && (SAINBot.Memory.Health.HealthStatus == ETagStatus.Healthy || SAINBot.Memory.Health.HealthStatus == ETagStatus.Injured))
            {
                float timeAdd = 4f * UnityEngine.Random.Range(0.75f, 1.25f); ;
                _nextShootDistTargetTime = Time.time + timeAdd;
                _endShootDistTargetTime = Time.time + timeAdd / 3f;
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

        private bool shallRunForCover()
        {
            if (!BotOwner.CanSprintPlayer)
            {
                return false;
            }

            bool runNow = SAINBot.Enemy != null
                && !SAINBot.Enemy.IsVisible
                && (!SAINBot.Enemy.Seen || SAINBot.Enemy.TimeSinceSeen > 3f || BotOwner.Memory.IsUnderFire)
                && SAINBot.Cover.CoverPoints.Count > 0;

            if (StartRunCoverTimer < Time.time || runNow)
            {
                CoverPoint coverInUse = SAINBot.Cover.CoverInUse;
                if (coverInUse != null)
                {
                    return (coverInUse.Position - SAINBot.Position).sqrMagnitude >= 1f;
                }
                return true;
            }
            return false;
        }

        private static readonly float RunToCoverTime = 1.5f;
        private static readonly float RunToCoverTimeRandomMin = 0.66f;
        private static readonly float RunToCoverTimeRandomMax = 1.33f;

        private bool shallMoveToCover()
        {
            CoverPoint coverInUse = SAINBot.Cover.CoverInUse;
            if (coverInUse == null || coverInUse.Status != CoverStatus.InCover || coverInUse.Spotted)
            {
                var CurrentDecision = SAINBot.Decision.CurrentSoloDecision;
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

        private bool shallSearch()
        {
            return SAINBot.Search.ShallStartSearch(out _, true);
        }

        public bool shallHoldInCover()
        {
            var cover = SAINBot.Cover.CoverInUse;
            if (cover != null 
                && !cover.Spotted
                && cover.Status == CoverStatus.InCover)
            {
                return true;
            }
            return false;
        }

        private bool shallStandAndShoot(SAINEnemy enemy)
        {
            if (enemy.IsVisible && 
                enemy.CanShoot && 
                BotOwner.WeaponManager?.HaveBullets == true)
            {
                if (enemy.RealDistance > SAINBot.Info.WeaponInfo.EffectiveWeaponDistance * 1.25f)
                {
                    return false;
                }
                float holdGround = SAINBot.Info.HoldGroundDelay;

                if (holdGround <= 0f)
                {
                    return false;
                }

                if (!enemy.EnemyLookingAtMe)
                {
                    return true;
                    //CoverPoint closestPoint = SAINBot.Cover.ClosestPoint;
                    //if (!enemy.EnemyLookingAtMe && closestPoint != null && closestPoint.Status <= CoverStatus.CloseToCover)
                    //{
                    //    return true;
                    //}
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
                        return SAINBot.Cover.CheckLimbsForCover();
                    }
                }
            }
            return false;
        }

        private float StartRunCoverTimer;
    }
}
