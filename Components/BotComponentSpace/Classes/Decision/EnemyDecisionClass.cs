using EFT;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Decision.Reasons;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Search;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class EnemyDecisionClass : BotBase, IBotClass
    {
        private static readonly float RushEnemyMaxPathDistance = 10f;
        private static readonly float RushEnemyMaxPathDistanceSprint = 20f;
        private static readonly float RushEnemyLowAmmoRatio = 0.4f;
        private static readonly float RunToCoverTime = 1.5f;
        private static readonly float RunToCoverTimeRandomMin = 0.66f;
        private static readonly float RunToCoverTimeRandomMax = 1.33f;
        public SearchReasonsStruct DebugSearchReasons { get; private set; }
        public float FreezeFor { get; private set; }
        public float UnFreezeTime { get; private set; }
        public StringBuilder FailedDecisionReasons { get; } = new StringBuilder();
        public bool ShiftCoverComplete { get; set; }
        public bool? DebugShallSearch { get; set; }
        public bool ShotInCover { get; set; }

        private Dictionary<int, IBotDecisionClass> _decisions { get; } = new Dictionary<int, IBotDecisionClass>();

        public EnemyDecisionClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public BotDecision<ECombatDecision>? GetDecision()
        {
            FailedDecisionReasons.Clear();
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                return null;
            }
            string reason = string.Empty;
            if (shallDogFight(enemy, out reason))
            {
                return new BotDecision<ECombatDecision>(ECombatDecision.DogFight, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.DogFight} {reason}");

            if (shallStandAndShoot(enemy, out reason))
            {
                if (Bot.Decision.CurrentCombatDecision != ECombatDecision.StandAndShoot)
                    Bot.Info.CalcHoldGroundDelay();

                return new BotDecision<ECombatDecision>(ECombatDecision.StandAndShoot, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.StandAndShoot} {reason}");

            if (shallShootDistantEnemy(enemy, out reason))
            {
                return new BotDecision<ECombatDecision>(ECombatDecision.ShootDistantEnemy, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.ShootDistantEnemy} {reason}");

            if (shallRushEnemy(enemy, out reason))
            {
                return new BotDecision<ECombatDecision>(ECombatDecision.RushEnemy, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.RushEnemy} {reason}");

            if (shallThrowGrenade(enemy, out reason))
            {
                return new BotDecision<ECombatDecision>(ECombatDecision.ThrowGrenade, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.ThrowGrenade} {reason}");

            if (shallSearch(enemy, out reason))
            {
                if (Bot.Decision.CurrentCombatDecision != ECombatDecision.Search)
                {
                    enemy.Status.NumberOfSearchesStarted++;
                }
                return new BotDecision<ECombatDecision>(ECombatDecision.Search, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.Search} {reason}");

            if (shallFreezeAndWait(enemy, out reason))
            {
                return new BotDecision<ECombatDecision>(ECombatDecision.Freeze, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.Freeze} {reason}");

            if (shallShiftCover(enemy, out reason))
            {
                return new BotDecision<ECombatDecision>(ECombatDecision.ShiftCover, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.ShiftCover} {reason}");

            if (shallMoveToCover(out reason))
            {
                if (shallRunForCover(enemy, out reason))
                {
                    return new BotDecision<ECombatDecision>(ECombatDecision.RunToCover, reason);
                }
                FailedDecisionReasons.AppendLine($"{ECombatDecision.RunToCover} {reason}");

                return new BotDecision<ECombatDecision>(ECombatDecision.MoveToCover, reason);
            }
            FailedDecisionReasons.AppendLine($"{ECombatDecision.MoveToCover} {reason}");

            return new BotDecision<ECombatDecision>(ECombatDecision.HoldInCover, reason);
        }

        public bool GetDecision(out ECombatDecision Decision)
        {
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                Decision = ECombatDecision.None;
                return false;
            }

            if (shallDogFight(enemy, out _))
            {
                Decision = ECombatDecision.DogFight;
            }
            else if (shallStandAndShoot(enemy, out _))
            {
                if (Bot.Decision.CurrentCombatDecision != ECombatDecision.StandAndShoot)
                {
                    Bot.Info.CalcHoldGroundDelay();
                }
                Decision = ECombatDecision.StandAndShoot;
            }
            else if (shallShootDistantEnemy(enemy, out _))
            {
                Decision = ECombatDecision.ShootDistantEnemy;
            }
            else if (shallRushEnemy(enemy, out _))
            {
                Decision = ECombatDecision.RushEnemy;
            }
            else if (shallThrowGrenade(enemy, out _))
            {
                Decision = ECombatDecision.ThrowGrenade;
            }
            else if (shallSearch(enemy, out _))
            {
                if (Bot.Decision.CurrentCombatDecision != ECombatDecision.Search)
                {
                    enemy.Status.NumberOfSearchesStarted++;
                }
                Decision = ECombatDecision.Search;
            }
            else if (shallFreezeAndWait(enemy, out _))
            {
                Decision = ECombatDecision.Freeze;
            }
            //else if (shallMoveToEngage(enemy))
            //{
            //    Decision = SoloDecision.MoveToEngage;
            //}
            else if (shallShiftCover(enemy, out _))
            {
                Decision = ECombatDecision.ShiftCover;
            }
            else if (shallMoveToCover(out _))
            {
                Decision = ECombatDecision.MoveToCover;

                if (shallRunForCover(enemy, out _))
                {
                    Decision = ECombatDecision.RunToCover;
                }
            }
            else if (shallHoldInCover(out _))
            {
                Decision = ECombatDecision.HoldInCover;
            }
            else
            {
                Decision = ECombatDecision.DebugNoDecision;
            }

            if (Decision != ECombatDecision.MoveToCover &&
                Decision != ECombatDecision.RunToCover)
            {
                StartRunCoverTimer = 0f;
            }

            return true;
        }

        private void checkFreezeTime()
        {
            if (Bot.Decision.CurrentCombatDecision != ECombatDecision.Freeze)
            {
                FreezeFor = UnityEngine.Random.Range(10f, 120f);
                UnFreezeTime = Time.time + FreezeFor;
            }
        }

        private bool shallFreezeAndWait(Enemy enemy, out string reason)
        {
            if (Bot.Info.PersonalitySettings.Search.HeardFromPeaceBehavior != EHeardFromPeaceBehavior.Freeze)
            {
                reason = "wontFreeze";
                return false;
            }
            if (!enemy.Hearing.EnemyHeardFromPeace)
            {
                reason = "notHeardFromPeace";
                return false;
            }
            if (!Bot.Memory.Location.IsIndoors)
            {
                reason = "outside";
                return false;
            }
            if (!Bot.Memory.Location.IsIndoors)
            {
                reason = "outside";
                return false;
            }
            if (enemy.Seen && enemy.TimeSinceSeen < 240f)
            {
                reason = "seenRecent";
                return false;
            }
            if (enemy.TimeSinceLastKnownUpdated > 90f)
            {
                reason = "haventHeard";
                return false;
            }

            checkFreezeTime();

            if (UnFreezeTime < Time.time)
            {
                reason = "frozenTooLong";
                return false;
            }
            reason = "timeForFreeze";
            return true;
        }

        private bool shallThrowGrenade(Enemy enemy, out string reason)
        {
            return Bot.Grenade.GrenadeThrowDecider.GetDecision(enemy, out reason);
        }

        private bool shallRushEnemy(Enemy enemy, out string reason)
        {
            var health = Bot.Memory.Health.HealthStatus;
            if (health == ETagStatus.Dying)
            {
                reason = "imDying";
                return false;
            }
            if (enemy.Hearing.EnemyHeardFromPeace &&
                Bot.Info.PersonalitySettings.Search.HeardFromPeaceBehavior == EHeardFromPeaceBehavior.Charge)
            {
                reason = "heardFromPeaceCharge";
                return true;
            }
            if (!Bot.Info.PersonalitySettings.Rush.CanRushEnemyReloadHeal)
            {
                reason = "cantRush";
                return false;
            }
            if (Bot.Decision.SelfActionDecisions.LowOnAmmo(RushEnemyLowAmmoRatio))
            {
                reason = "lowAmmo";
                return false;
            }

            if (!checkInRangeForRush(enemy))
            {
                reason = "outOfRange";
                return false;
            }
            if (enemy.Status.VulnerableAction != EEnemyAction.None)
            {
                reason = "enemyVulnerable";
                return true;
            }
            ETagStatus enemyHealth = enemy.EnemyPlayer.HealthStatus;
            if (enemyHealth == ETagStatus.Dying)
            {
                reason = "enemyHurtBad";
                return true;
            }
            if (enemyHealth == ETagStatus.BadlyInjured && 
                enemy.EnemyPlayer.IsInPronePose)
            {
                reason = "enemyHurtAndProne";
                return true;
            }
            reason = "notGoodTimeTo";
            return false;
        }

        private bool checkInRangeForRush(Enemy enemy)
        {
            EEnemyAction vulnerableAction = enemy.Status.VulnerableAction;
            float modifier = vulnerableAction == EEnemyAction.UsingSurgery ? 2f : 1f;
            if (enemy.Path.PathDistance < RushEnemyMaxPathDistance * modifier)
            {
                return true;
            }
            if (enemy.Path.PathDistance < RushEnemyMaxPathDistanceSprint * modifier && 
                BotOwner.CanSprintPlayer)
            {
                return true;
            }
            return false;
        }


        private bool shallShiftCover(Enemy enemy, out string reason)
        {
            if (Bot.Info.PersonalitySettings.Cover.CanShiftCoverPosition == false)
            {
                reason = "cantShift";
                return false;
            }
            if (Bot.Suppression.IsSuppressed)
            {
                reason = "suppressed";
                return false;
            }

            if (ContinueShiftCover())
            {
                reason = "continueShift";
                return true;
            }

            var CurrentDecision = Bot.Decision.CurrentCombatDecision;

            if (CurrentDecision == ECombatDecision.HoldInCover && Bot.Info.PersonalitySettings.Cover.CanShiftCoverPosition)
            {
                if (Bot.Decision.TimeSinceChangeDecision > ShiftCoverChangeDecisionTime && TimeForNewShift < Time.time)
                {
                    if (enemy != null)
                    {
                        if (enemy.Seen && !enemy.IsVisible && enemy.TimeSinceSeen > ShiftCoverTimeSinceSeen)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            reason = "enemyNotSeen";
                            return true;
                        }
                        if (!enemy.Seen && enemy.KnownPlaces.TimeSinceLastKnownUpdated > ShiftCoverTimeSinceEnemyCreated)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            reason = "lastKnownNotUpdated";
                            return true;
                        }
                    }
                    if (enemy == null && Bot.Decision.TimeSinceChangeDecision > ShiftCoverNoEnemyResetTime)
                    {
                        TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                        ShiftResetTimer = Time.time + ShiftCoverResetTime;
                        reason = "timeDecisionMade";
                        return true;
                    }
                }
            }

            reason = "dontWantTo";
            ShiftResetTimer = -1f;
            return false;
        }

        private bool ContinueShiftCover()
        {
            var CurrentDecision = Bot.Decision.CurrentCombatDecision;
            if (CurrentDecision == ECombatDecision.ShiftCover)
            {
                if (ShiftResetTimer > 0f && ShiftResetTimer < Time.time)
                {
                    ShiftResetTimer = -1f;
                    return false;
                }
                if (!BotOwner.Mover.IsMoving && !Bot.Mover.SprintController.Running)
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


        private bool shallDogFight(Enemy enemy, out string reason)
        {
            if (Bot.Decision.CurrentSelfDecision != ESelfDecision.None || BotOwner.WeaponManager.Reload.Reloading)
            {
                reason = "selfDecisionOrReloading";
                return false;
            }

            if (Bot.Decision.CurrentCombatDecision == ECombatDecision.RushEnemy)
            {
                reason = "rushingEnemy";
                return false;
            }

            //var currentSolo = Bot.Decision.CurrentSoloDecision;
            //if (Time.time - Bot.Cover.LastHitTime < 2f
            //    && currentSolo != SoloDecision.RunAway
            //    && currentSolo != SoloDecision.RunToCover
            //    && currentSolo != SoloDecision.Retreat
            //    && currentSolo != SoloDecision.MoveToCover)
            //{
            //    reason = "shotInCover";
            //    ShotInCover = true;
            //    return true;
            //}

            if (Bot.Cover.SpottedInCover == true)
            {
                reason = "coverSpotted";
                return true;
            }
            if ((enemy.EPathDistance == EPathDistance.VeryClose && Bot.Enemy.IsVisible))
            {
                reason = "enemyClose";
                return true;
            }

            reason = string.Empty;
            return false;
        }


        private bool shallMoveToEngage(Enemy enemy)
        {
            if (Bot.Suppression.IsSuppressed)
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
            var decision = Bot.Decision.CurrentCombatDecision;
            if (BotOwner.Memory.IsUnderFire && decision != ECombatDecision.MoveToEngage)
            {
                return false;
            }
            if (decision == ECombatDecision.Retreat || decision == ECombatDecision.MoveToCover || decision == ECombatDecision.RunToCover)
            {
                return false;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance
                && decision != ECombatDecision.MoveToEngage)
            {
                return true;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance * 0.66f
                && decision == ECombatDecision.MoveToEngage)
            {
                return true;
            }
            return false;
        }

        private bool shallShootDistantEnemy(Enemy enemy, out string reason)
        {
            if (_endShootDistTargetTime > Time.time
                && Bot.Decision.CurrentCombatDecision == ECombatDecision.ShootDistantEnemy
                && Bot.Memory.Health.HealthStatus != ETagStatus.Dying)
            {
                reason = "shootingDistantEnemy";
                return true;
            }
            if (_nextShootDistTargetTime < Time.time
                && enemy.RealDistance > Bot.Info.FileSettings.Shoot.MaxPointFireDistance
                && enemy.IsVisible
                && enemy.CanShoot
                && (Bot.Memory.Health.HealthStatus == ETagStatus.Healthy || Bot.Memory.Health.HealthStatus == ETagStatus.Injured))
            {
                float timeAdd = 6f * UnityEngine.Random.Range(0.75f, 1.25f);
                _nextShootDistTargetTime = Time.time + timeAdd;
                _endShootDistTargetTime = Time.time + timeAdd / 3f;
                reason = "shootingDistantEnemy";
                return true;
            }
            reason = string.Empty;
            return false;
        }

        private bool shallRunForCover(Enemy enemy, out string reason)
        {
            if (!BotOwner.CanSprintPlayer)
            {
                reason = "cantSprint";
                return false;
            }

            if (Bot.Cover.CoverPoints.Count == 0)
            {
                reason = "noCoverPoints";
                return false;
            }

            if (!enemy.IsVisible &&
                (!enemy.Seen || enemy.TimeSinceSeen > 3f))
            {
                reason = "runNow cantSeeEnemy";
                return true;
            }

            if (StartRunCoverTimer < Time.time)
            {
                reason = "timeToRun";
                return true;
            }

            reason = "dontRunYet";
            return false;
        }

        private bool shallMoveToCover(out string reason)
        {
            if (Bot.Cover.InCover)
            {
                reason = "inCover";
                return false;
            }

            var CurrentDecision = Bot.Decision.CurrentCombatDecision;
            if (CurrentDecision != ECombatDecision.MoveToCover && CurrentDecision != ECombatDecision.RunToCover)
            {
                StartRunCoverTimer = Time.time + RunToCoverTime * UnityEngine.Random.Range(RunToCoverTimeRandomMin, RunToCoverTimeRandomMax);
            }

            reason = "notInCover";
            return true;
        }

        private bool shallSearch(Enemy enemy, out string reason)
        {
            bool shallSearch = Bot.Search.SearchDecider.ShallStartSearch(enemy, out SearchReasonsStruct reasons);
            DebugSearchReasons = reasons;
            DebugShallSearch = shallSearch;
            if (shallSearch)
            {
                reason = "wantToSearch";
            }
            else
            {
                reason = "cantSearch";
            }
            return shallSearch;
        }

        public bool shallHoldInCover(out string reason)
        {
            if (Bot.Cover.InCover)
            {
                reason = "inCover";
                return true;
            }
            reason = "notInCover";
            return false;
        }

        private bool shallStandAndShoot(Enemy enemy, out string reason)
        {
            if (!enemy.IsVisible)
            {
                reason = "cantSeeEnemy";
                return false;
            }
            if (!enemy.CanShoot)
            {
                reason = "cantShootEnemy";
                return false;
            }
            if (BotOwner.WeaponManager?.HaveBullets == false)
            {
                reason = "noBullets";
                return false;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance * 1.25f)
            {
                reason = "outOfRange";
                return false;
            }

            float holdGround = Bot.Info.HoldGroundDelay;
            if (holdGround <= 0f)
            {
                reason = "wontHoldGround";
                return false;
            }

            if (!enemy.EnemyLookingAtMe)
            {
                reason = "enemyNotLooking";
                return true;
            }

            float visibleFor = Time.time - enemy.Vision.VisibleStartTime;
            if (visibleFor > holdGround)
            {
                reason = "visibleTooLong";
                return false;
            }

            if (visibleFor < holdGround / 1.5f)
            {
                reason = "holdingFromTime";
                return true;
            }
            else if (Bot.Cover.CheckLimbsForCover())
            {
                reason = "holdingHaveSomeCover";
                return true;
            }
            reason = "outOfTime";
            return false;
        }

        private CoverSettings CoverSettings => SAINPlugin.LoadedPreset.GlobalSettings.Cover;
        private float ShiftCoverChangeDecisionTime => CoverSettings.ShiftCoverChangeDecisionTime;
        private float ShiftCoverTimeSinceSeen => CoverSettings.ShiftCoverTimeSinceSeen;
        private float ShiftCoverTimeSinceEnemyCreated => CoverSettings.ShiftCoverTimeSinceEnemyCreated;
        private float ShiftCoverNoEnemyResetTime => CoverSettings.ShiftCoverNoEnemyResetTime;
        private float ShiftCoverNewCoverTime => CoverSettings.ShiftCoverNewCoverTime;
        private float ShiftCoverResetTime => CoverSettings.ShiftCoverResetTime;

        private float StartRunCoverTimer;
        private float _nextShootDistTargetTime;
        private float _endShootDistTargetTime;
        private float TimeForNewShift;
        private float ShiftResetTimer;
    }
}