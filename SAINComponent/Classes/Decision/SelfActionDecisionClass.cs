using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SelfActionDecisionClass : SAINBase, ISAINClass
    {
        public SelfActionDecisionClass(Bot sain) : base(sain)
        {
        }

        private static readonly float StartFirstAid_Injury_SeenRecentTime = 8f;
        private static readonly float StartFirstAid_HeavyInjury_SeenRecentTime = 6f;
        private static readonly float StartFirstAid_FatalInjury_SeenRecentTime = 4f;
        private static readonly float StartReload_LowAmmo_SeenRecentTime = 5f;
        private static readonly float StartSurgery_SeenRecentTime = 90f;

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public SelfDecision CurrentSelfAction => Bot.Decision.CurrentSelfDecision;
        private EnemyPathDistance EnemyDistance => Bot.Decision.EnemyDistance;

        public bool GetDecision(out SelfDecision Decision)
        {
            if ( Bot.Enemy == null && !BotOwner.Medecine.Using && LowOnAmmo(0.75f) )
            {
                Bot.SelfActions.TryReload();
                Decision = SelfDecision.None;
                return false;
            }

            if (!CheckContinueSelfAction(out Decision))
            {
                if (StartRunGrenade())
                {
                    Decision = SelfDecision.RunAwayGrenade;
                }
                else if (StartBotReload())
                {
                    Decision = SelfDecision.Reload;
                }
                else
                {
                    if (LastHealCheckTime < Time.time && !Bot.Memory.Health.Healthy)
                    {
                        LastHealCheckTime = Time.time + 1f;
                        if (StartUseStims())
                        {
                            Decision = SelfDecision.Stims;
                        }
                        else if (StartFirstAid())
                        {
                            Decision = SelfDecision.FirstAid;
                        }
                        else if (Bot.Medical.Surgery.ShallTrySurgery())
                        {
                            Decision = SelfDecision.Surgery;
                        }
                    }
                }
            }

            return Decision != SelfDecision.None;
        }

        private float LastHealCheckTime;

        private bool StartRunGrenade()
        {
            var grenadePos = Bot.Grenade.GrenadeDangerPoint; 
            if (grenadePos != null)
            {
                Vector3 headPos = Bot.Transform.HeadPosition;
                Vector3 direction = grenadePos.Value - headPos;
                if (!Physics.Raycast(headPos, direction.normalized, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    return true;
                }
            }
            return false;
        }

        private void TryFixBusyHands()
        {
            if (BusyHandsTimer > Time.time)
            {
                return;
            }
            BusyHandsTimer = Time.time + 1f;

            var selector = BotOwner.WeaponManager?.Selector;
            if (selector == null)
            {
                return;
            }
            if (selector.TryChangeWeapon(true))
            {
                return;
            }
            if (selector.TakePrevWeapon())
            {
                return;
            }
            if (selector.TryChangeToMain())
            {
                return;
            }
            if (selector.CanChangeToSecondWeapons)
            {
                selector.ChangeToSecond();
                return;
            }
        }

        private float BusyHandsTimer;
        private float NextReloadTime;

        private bool CheckContinueSelfAction(out SelfDecision Decision)
        {
            if (CurrentSelfAction != SelfDecision.None)
            {
                if (CurrentSelfAction == SelfDecision.Surgery 
                    && Bot.Medical.Surgery.ShallTrySurgery())
                {
                    Decision = CurrentSelfAction;
                    return true;
                }
                else if (Time.time - Bot.Decision.ChangeDecisionTime > 5f)
                {
                    if (CurrentSelfAction != SelfDecision.Surgery)
                    {
                        Decision = SelfDecision.None;
                        TryFixBusyHands();
                        return false;
                    }
                    else if (Time.time - Bot.Decision.ChangeDecisionTime > 30f)
                    {
                        Decision = SelfDecision.None;
                        TryFixBusyHands();
                        return false;
                    }
                }
            }
            bool continueAction = UsingMeds || ContinueReload || ContinueRunGrenade;
            Decision = continueAction ? CurrentSelfAction : SelfDecision.None;
            return continueAction;
        }

        private bool ContinueRunGrenade => CurrentSelfAction == SelfDecision.RunAwayGrenade && Bot.Grenade.GrenadeDangerPoint != null;
        public bool UsingMeds => BotOwner.Medecine.Using;
        private bool ContinueReload => BotOwner.WeaponManager.Reload?.Reloading == true; //  && !StartCancelReload()
        public bool CanUseStims
        {
            get
            {
                var stims = BotOwner.Medecine.Stimulators;
                return stims.HaveSmt && Time.time - stims.LastEndUseTime > 3f && stims.CanUseNow() && !Bot.Memory.Health.Healthy;
            }
        }
        public bool CanUseFirstAid => BotOwner.Medecine.FirstAid.ShallStartUse();
        public bool CanUseSurgery => BotOwner.Medecine.SurgicalKit.ShallStartUse() && !BotOwner.Medecine.FirstAid.IsBleeding;
        public bool CanReload => BotOwner.WeaponManager.IsReady && !BotOwner.WeaponManager.HaveBullets;

        private bool StartUseStims()
        {
            bool takeStims = false;
            if (CanUseStims)
            {
                var enemy = Bot.Enemy;
                if (enemy == null)
                {
                    if (Bot.Memory.Health.Dying || Bot.Memory.Health.BadlyInjured)
                    {
                        takeStims = true;
                    }
                }
                else
                {
                    var pathStatus = EnemyDistance;
                    bool SeenRecent = enemy.TimeSinceSeen < 3f;
                    if (!enemy.InLineOfSight && !SeenRecent)
                    {
                        takeStims = true;
                    }
                    else if (pathStatus == EnemyPathDistance.Far || pathStatus == EnemyPathDistance.VeryFar)
                    {
                        takeStims = true;
                    }
                }
            }
            return takeStims;
        }


        private bool StartFirstAid()
        {
            bool useFirstAid = false;
            if (CanUseFirstAid)
            {
                var enemy = Bot.Enemy;
                if (enemy == null)
                {
                    useFirstAid = true;
                }
                else
                {
                    var pathStatus = EnemyDistance;
                    bool SeenRecent = enemy.TimeSinceSeen < StartFirstAid_Injury_SeenRecentTime;
                    var status = Bot;
                    if (status.Memory.Health.Injured)
                    {
                        if (!enemy.InLineOfSight && !SeenRecent && pathStatus != EnemyPathDistance.VeryClose && pathStatus != EnemyPathDistance.Close)
                        {
                            useFirstAid = true;
                        }
                    }
                    else if (status.Memory.Health.BadlyInjured)
                    {
                        if (!enemy.InLineOfSight && pathStatus != EnemyPathDistance.VeryClose && enemy.TimeSinceSeen < StartFirstAid_HeavyInjury_SeenRecentTime)
                        {
                            useFirstAid = true;
                        }

                        if (pathStatus == EnemyPathDistance.VeryFar)
                        {
                            useFirstAid = true;
                        }
                    }
                    else if (status.Memory.Health.Dying)
                    {
                        if (!enemy.InLineOfSight && enemy.TimeSinceSeen < StartFirstAid_FatalInjury_SeenRecentTime)
                        {
                            useFirstAid = true;
                        }
                        if (pathStatus == EnemyPathDistance.VeryFar || pathStatus == EnemyPathDistance.Far)
                        {
                            useFirstAid = true;
                        }
                    }
                }
            }

            return useFirstAid;
        }

        public bool StartCancelReload()
        {
            if (!BotOwner.WeaponManager?.IsReady == true || BotOwner.WeaponManager.Reload.BulletCount == 0 || BotOwner.WeaponManager.CurrentWeapon.ReloadMode == EFT.InventoryLogic.Weapon.EReloadMode.ExternalMagazine)
            {
                return false;
            }

            var enemy = Bot.Enemy;
            if (enemy != null && BotOwner.WeaponManager.Reload.Reloading && Bot.Enemy != null)
            {
                var pathStatus = enemy.CheckPathDistance();
                bool SeenRecent = Time.time - enemy.TimeSinceSeen > 3f;

                if (SeenRecent && Vector3.Distance(BotOwner.Position, enemy.EnemyIPlayer.Position) < 8f)
                {
                    return true;
                }

                if (!LowOnAmmo(0.15f) && enemy.IsVisible)
                {
                    return true;
                }
                if (pathStatus == EnemyPathDistance.VeryClose)
                {
                    return true;
                }
                if (BotOwner.WeaponManager.Reload.BulletCount > 1 && pathStatus == EnemyPathDistance.Close)
                {
                    return true;
                }
            }

            return false;
        }


        private bool StartBotReload()
        {
            // Only allow reloading every 5 seconds to avoid spamming reload when the weapon data is bad
            if (NextReloadTime > Time.time)
            {
                return false;
            }

            bool needToReload = false;
            if (CanReload)
            {
                if (BotOwner.WeaponManager.Reload.BulletCount == 0)
                {
                    needToReload = true;
                }
                else if (LowOnAmmo())
                {
                    var enemy = Bot.Enemy;
                    if (enemy == null)
                    {
                        needToReload = true;
                    }
                    else if (enemy.TimeSinceSeen > StartReload_LowAmmo_SeenRecentTime)
                    {
                        needToReload = true;
                    }
                    else if (EnemyDistance != EnemyPathDistance.VeryClose && !enemy.IsVisible)
                    {
                        needToReload = true;
                    }
                }
            }

            if (needToReload)
            {
                NextReloadTime = Time.time + 5;
            }

            return needToReload;
        }


        private bool StartSurgery()
        {
            const float useSurgDist = 50f;
            bool useSurgery = false;

            if (CanUseSurgery)
            {
                var enemy = Bot.Enemy;
                if (enemy == null)
                {
                    if (Bot.CurrentTargetPosition == null)
                    {
                        useSurgery = true;
                    }
                    else if ((Bot.CurrentTargetPosition.Value - Bot.Position).sqrMagnitude > useSurgDist * useSurgDist)
                    {
                        useSurgery = true;
                    }
                }
                else
                {
                    var pathStatus = enemy.CheckPathDistance();
                    bool SeenRecent = enemy.TimeSinceSeen < StartSurgery_SeenRecentTime;

                    if (!SeenRecent && pathStatus != EnemyPathDistance.Far && pathStatus != EnemyPathDistance.Close && pathStatus != EnemyPathDistance.VeryClose)
                    {
                        useSurgery = true;
                    }
                }

                if (Bot.HasEnemy)
                {
                    foreach (var enemyInfo in Bot.EnemyController.Enemies)
                    {
                        if (enemyInfo.Value.InLineOfSight)
                        {
                            useSurgery = false;
                        }
                        else if (enemyInfo.Value.TimeSinceSeen < StartSurgery_SeenRecentTime)
                        {
                            useSurgery = false;
                        }
                    }
                }
            }

            return useSurgery;
        }

        public bool LowOnAmmo(float ratio = 0.3f)
        {
            return AmmoRatio < ratio;
        }

        public float AmmoRatio
        {
            get
            {
                try
                {
                    int currentAmmo = BotOwner.WeaponManager.Reload.BulletCount;
                    int maxAmmo = BotOwner.WeaponManager.Reload.MaxBulletCount;
                    return (float)currentAmmo / maxAmmo;
                }
                catch
                {
                    // I HATE THIS STUPID BUG
                }
                return 1f;
            }
        }
    }
}
