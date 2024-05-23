using EFT;
using EFT.InventoryLogic;
using SAIN.SAINComponent.Classes.Enemy;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class ShootClass : BaseNodeClass
    {
        public ShootClass(BotOwner owner)
            : base(owner)
        {
            SAIN = owner.GetComponent<Bot>();
            Shoot = new BotShoot(owner);
        }

        private readonly BotShoot Shoot;
        private BotOwner BotOwner => botOwner_0;

        private readonly Bot SAIN;

        private float changeAimTimer;

        public override void Update()
        {
            if (BotOwner == null || BotOwner.GetPlayer == null)
            {
                return;
            }
            if (SAIN.Player.IsSprintEnabled)
            {
                BotOwner.AimingData?.LoseTarget();
                return;
            }

            if (BotOwner.WeaponManager.Selector.EquipmentSlot == EquipmentSlot.Holster
                && !BotOwner.WeaponManager.HaveBullets
                && !BotOwner.WeaponManager.Selector.TryChangeToMain())
            {
                selectWeapon();
            }

            if (changeAimTimer < Time.time)
            {
                changeAimTimer = Time.time + 0.5f;
                SAIN.AimDownSightsController.UpdateADSstatus();
            }

            if (SAIN.Enemy != null 
                && SAIN.Enemy.RealDistance > SAIN.Info.FileSettings.Shoot.MaxPointFireDistance 
                && !BotOwner.ShootData.ShootController.IsAiming)
            {
                BotOwner.AimingData?.LoseTarget();
                return;
            }

            aimAtEnemy();
        }

        private void selectWeapon()
        {
            if (WeaponInfo == null)
            {
                WeaponInfo = SAINGearInfoHandler.GetGearInfo(BotOwner.GetPlayer);
            }
            if (WeaponInfo != null)
            {
                EquipmentSlot optimalSlot = findOptimalWeaponForDistance(WeaponInfo, getDistance());
                if (currentSlot != optimalSlot)
                {
                    tryChangeWeapon(optimalSlot);
                }
            }
        }

        private EquipmentSlot currentSlot => BotOwner.WeaponManager.Selector.EquipmentSlot;

        private void tryChangeWeapon(EquipmentSlot slot)
        {
            if (_nextChangeWeaponTime < Time.time)
            {
                var selector = BotOwner?.WeaponManager?.Selector;
                if (selector != null)
                {
                    _nextChangeWeaponTime = Time.time + 3f;
                    switch (slot)
                    {
                        case EquipmentSlot.FirstPrimaryWeapon:
                            selector.TryChangeToMain();
                            break;

                        case EquipmentSlot.SecondPrimaryWeapon:
                            selector.ChangeToSecond();
                            break;

                        case EquipmentSlot.Holster:
                            selector.TryChangeWeapon(true);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private float _nextChangeWeaponTime;

        private float getDistance()
        {
            if (_nextGetDistTime < Time.time)
            {
                _nextGetDistTime = Time.time + 0.5f;
                Vector3? target = SAIN.CurrentTargetPosition;
                if (target != null)
                {
                    _lastDistance = (target.Value - SAIN.Position).magnitude;
                }
            }
            return _lastDistance;
        }

        private float _lastDistance;
        private float _nextGetDistTime;

        private EquipmentSlot findOptimalWeaponForDistance(GearInfoContainer weaponInfo, float distance)
        {
            if (_nextCheckOptimalTime < Time.time)
            {
                _nextCheckOptimalTime = Time.time + 1f;

                float? primaryEngageDist = weaponInfo.Primary?.EngagementDistance();
                float? secondaryEngageDist = weaponInfo.Secondary?.EngagementDistance();
                float? holsterEngageDist = weaponInfo.Holster?.EngagementDistance();

                float minDifference = Mathf.Abs(distance - primaryEngageDist ?? 0);
                optimalSlot = EquipmentSlot.FirstPrimaryWeapon;

                float difference = Mathf.Abs(distance - secondaryEngageDist ?? 0);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    optimalSlot = EquipmentSlot.SecondPrimaryWeapon;
                }

                if (!BotOwner.WeaponManager.HaveBullets)
                {
                    difference = Mathf.Abs(distance - holsterEngageDist ?? 0);
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        optimalSlot = EquipmentSlot.Holster;
                    }
                }
            }
            return optimalSlot;
        }

        private EquipmentSlot optimalSlot;
        private float _nextCheckOptimalTime;

        private GearInfoContainer WeaponInfo;

        private void aimAtEnemy()
        {
            if (!BotOwner.WeaponManager.HaveBullets)
            {
                SAIN.SelfActions.TryReload();
                return;
            }
            var enemy = SAIN.Enemy;
            if (enemy != null)
            {
                if (enemy.IsVisible && enemy.RealDistance < 40f)
                {
                    BotOwner.BotLight?.TurnOn(true);
                }
                else if (!enemy.IsVisible && enemy.Seen)
                {
                    if (enemy.TimeSinceSeen > 3f)
                    {
                        BotOwner.BotLight?.TurnOff(false);
                    }
                }
                else
                {
                    BotOwner.BotLight?.TurnOff(false);
                }

                Vector3? pointToShoot = GetPointToShoot(enemy);
                if (pointToShoot != null)
                {
                    Target = pointToShoot.Value;
                    if (BotOwner.AimingData.IsReady 
                        && !SAIN.NoBushESP.NoBushESPActive 
                        && FriendlyFire.ClearShot)
                    {
                        ReadyToShoot();
                        Shoot.Update();
                    }
                }
                else
                {
                    BotOwner.AimingData.LoseTarget();
                }
            }
            else if (SAIN.CurrentTargetPosition != null)
            {
                float lightOnVisDist = BotOwner.Settings.FileSettings.Look.LightOnVisionDistance;
                float sqrMagnitude = (SAIN.Position - SAIN.CurrentTargetPosition.Value).sqrMagnitude;
                if (sqrMagnitude < lightOnVisDist * lightOnVisDist)
                {
                    BotOwner.BotLight?.TurnOn(true);
                }
                else
                {
                    BotOwner.BotLight?.TurnOff(false);
                }
            }
        }

        private Vector3? blindShootTarget(SAINEnemy enemy)
        {
            Vector3? result = null;
            if (!enemy.IsVisible
                    && enemy.HeardRecently
                    && enemy.InLineOfSight)
            {
                EnemyPlace lastKnown = enemy.KnownPlaces.LastKnownPlace;
                if (lastKnown != null && lastKnown.PersonalClearLineOfSight(BotOwner.LookSensor._headPoint, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    result = lastKnown.Position + Vector3.up + UnityEngine.Random.onUnitSphere;
                }
            }
            return result;
        }

        protected virtual void ReadyToShoot()
        {
        }

        protected virtual Vector3? GetTarget()
        {
            Vector3? target = getAimTarget(SAIN.Enemy);

            if (target == null)
            {
                target = getAimTarget(SAIN.LastEnemy);
            }
            return target;
        }

        private Vector3? getAimTarget(SAINEnemy enemy)
        {
            Vector3? target = null;
            if (enemy != null && enemy.IsVisible && enemy.CanShoot)
            {
                if (!enemy.IsAI && SAINPlugin.LoadedPreset.GlobalSettings.General.HeadShotProtection)
                {
                    target = enemy.CenterMass;
                }
                if (target == null)
                {
                    target = getEnemyPartToShoot(enemy.EnemyInfo);
                }
            }
            return target;
        }

        private Vector3? getEnemyPartToShoot(EnemyInfo enemy)
        {
            if (enemy != null)
            {
                Vector3 value;
                if (enemy.Distance < 6f)
                {
                    value = enemy.GetCenterPart();
                }
                else
                {
                    value = enemy.GetPartToShoot();
                }
                return new Vector3?(value);
            }
            return null;
        }

        protected virtual Vector3? GetPointToShoot(SAINEnemy enemy)
        {
            Vector3? target = GetTarget();
            SAIN.Steering.SetAimTarget(target);
            if (target != null)
            {
                Target = target.Value;
            }
            return target;
        }

        protected Vector3 Target;

        public SAINFriendlyFireClass FriendlyFire => SAIN.FriendlyFireClass;
    }

    public class BotShoot : BaseNodeClass
    {
        public BotShoot(BotOwner bot)
            : base(bot)
        {
        }

        public override void Update()
        {
            if (!this.botOwner_0.WeaponManager.HaveBullets)
            {
                return;
            }
            Vector3 position = this.botOwner_0.GetPlayer.PlayerBones.WeaponRoot.position;
            Vector3 realTargetPoint = this.botOwner_0.AimingData.RealTargetPoint;
            if (this.botOwner_0.ShootData.CheckFriendlyFire(position, realTargetPoint))
            {
                return;
            }
            if (this.botOwner_0.RecoilData.RecoilOffset.sqrMagnitude > 3f)
            {
                return;
            }
            if (this.botOwner_0.ShootData.Shoot())
            {
                if (this.int_0 > this.botOwner_0.WeaponManager.Reload.BulletCount)
                {
                    this.int_0 = this.botOwner_0.WeaponManager.Reload.BulletCount;
                }
                this.int_0 = this.botOwner_0.WeaponManager.Reload.BulletCount;

                this.botOwner_0.Memory.GoalEnemy?.SetLastShootTime();
            }
        }

        private int int_0;
    }
}