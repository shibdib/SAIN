using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SelfActionDecisionClass : SAINBase, ISAINClass
    {
        public SelfActionDecisionClass(BotComponent sain) : base(sain)
        {
        }

        private static readonly float StartFirstAid_Injury_SeenRecentTime = 8f;
        private static readonly float StartFirstAid_HeavyInjury_SeenRecentTime = 6f;
        private static readonly float StartFirstAid_FatalInjury_SeenRecentTime = 4f;
        private static readonly float StartReload_LowAmmo_SeenRecentTime = 5f;

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
        private EPathDistance EnemyDistance => Bot.Decision.EnemyDistance;

        public bool GetDecision(out SelfDecision Decision)
        {
            if (Bot.Enemy == null &&
                BotOwner?.Medecine?.Using == false &&
                LowOnAmmo(0.75f))
            {
                Bot.SelfActions.TryReload();
                Decision = SelfDecision.None;
                return false;
            }

            if (CheckContinueSelfAction(out Decision))
            {
                return true;
            }

            if (StartBotReload())
            {
                Decision = SelfDecision.Reload;
                return true;
            }

            if (_nextCheckHealTime < Time.time)
            {
                _nextCheckHealTime = Time.time + 1f;
                if (StartUseStims())
                {
                    Decision = SelfDecision.Stims;
                    return true;
                }
                if (StartFirstAid())
                {
                    Decision = SelfDecision.FirstAid;
                    return true;
                }
                if (Bot.Medical.Surgery.AreaClearForSurgery)
                {
                    Decision = SelfDecision.Surgery;
                    return true;
                }
            }

            return false;
        }

        private float _nextCheckHealTime;

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
            float timeSinceChange = _timeSinceChangeDecision;

            if (CurrentSelfAction == SelfDecision.Reload)
            {
                bool reloading = BotOwner.WeaponManager.Reload?.Reloading == true;
                if (!reloading &&
                    !StartBotReload())
                {
                    Decision = SelfDecision.None;
                    return false;
                }

                if (reloading)
                {
                    if (timeSinceChange < 5f)
                    {
                        Decision = SelfDecision.Reload;
                        return true;
                    }
                    else
                    {
                        Bot.SelfActions.BotCancelReload();
                        Decision = SelfDecision.None;
                        return false;
                    }
                }
                Decision = SelfDecision.Reload;
                return true;
            }

            if (BotOwner?.Medecine == null &&
                CurrentSelfAction != SelfDecision.Reload)
            {
                Decision = SelfDecision.None;
                return false;
            }
            if (CurrentSelfAction != SelfDecision.None)
            {
                if (CurrentSelfAction == SelfDecision.Surgery)
                {
                    if (Bot.Medical.Surgery.AreaClearForSurgery)
                    {
                        if (checkDecisionTooLong())
                        {
                            Bot.Medical.TryCancelHeal();
                            Decision = SelfDecision.None;
                            return false;
                        }
                        Decision = CurrentSelfAction;
                        return true;
                    }
                    else
                    {
                        Bot.Medical.TryCancelHeal();
                        Decision = SelfDecision.None;
                        return false;
                    }
                }
                else if (CurrentSelfAction != SelfDecision.Reload &&
                    timeSinceChange > 5f)
                {
                    Bot.Medical.TryCancelHeal();
                    Decision = SelfDecision.None;
                    TryFixBusyHands();
                    return false;
                }
                else if (timeSinceChange > 10f)
                {
                    Bot.SelfActions.BotCancelReload();
                    Decision = SelfDecision.None;
                    return false;
                }
            }
            bool continueAction = UsingMeds || ContinueReload;
            Decision = continueAction ? CurrentSelfAction : SelfDecision.None;
            return continueAction;
        }

        private float _timeSinceChangeDecision => Time.time - Bot.Decision.ChangeDecisionTime;

        private bool checkDecisionTooLong()
        {
            return Time.time - Bot.Decision.ChangeDecisionTime > 60f;
        }

        public bool UsingMeds => BotOwner.Medecine?.Using == true && CurrentSelfAction != SelfDecision.None;

        private bool ContinueReload => BotOwner.WeaponManager.Reload?.Reloading == true && CurrentSelfAction == SelfDecision.Reload; //  && !StartCancelReload()

        public bool CanUseStims
        {
            get
            {
                var stims = BotOwner.Medecine?.Stimulators;
                return stims?.HaveSmt == true && Time.time - stims.LastEndUseTime > 3f && stims?.CanUseNow() == true && !Bot.Memory.Health.Healthy;
            }
        }

        public bool CanUseFirstAid => BotOwner.Medecine?.FirstAid?.ShallStartUse() == true;

        public bool CanUseSurgery => BotOwner.Medecine?.SurgicalKit?.ShallStartUse() == true && BotOwner.Medecine?.FirstAid?.IsBleeding == false;

        public bool CanReload => BotOwner.WeaponManager?.IsReady == true && BotOwner.WeaponManager?.HaveBullets == false;

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
                    else if (pathStatus == EPathDistance.Far || pathStatus == EPathDistance.VeryFar)
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
                        if (!enemy.InLineOfSight && !SeenRecent && pathStatus != EPathDistance.VeryClose && pathStatus != EPathDistance.Close)
                        {
                            useFirstAid = true;
                        }
                    }
                    else if (status.Memory.Health.BadlyInjured)
                    {
                        if (!enemy.InLineOfSight && pathStatus != EPathDistance.VeryClose && enemy.TimeSinceSeen < StartFirstAid_HeavyInjury_SeenRecentTime)
                        {
                            useFirstAid = true;
                        }

                        if (pathStatus == EPathDistance.VeryFar)
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
                        if (pathStatus == EPathDistance.VeryFar || pathStatus == EPathDistance.Far)
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
                if (pathStatus == EPathDistance.VeryClose)
                {
                    return true;
                }
                if (BotOwner.WeaponManager.Reload.BulletCount > 1 && pathStatus == EPathDistance.Close)
                {
                    return true;
                }
            }

            return false;
        }

        private bool StartBotReload()
        {
            if (BotOwner.WeaponManager?.Reload.Reloading == true)
            {
                return true;
            }
            // Only allow reloading every 1 seconds to avoid spamming reload when the weapon data is bad
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
                    else if (EnemyDistance != EPathDistance.VeryClose && !enemy.IsVisible)
                    {
                        needToReload = true;
                    }
                }
            }

            if (needToReload)
            {
                NextReloadTime = Time.time + 1;
            }

            return needToReload;
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