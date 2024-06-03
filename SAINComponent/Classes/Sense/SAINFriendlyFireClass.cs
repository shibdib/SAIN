using EFT;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINFriendlyFireClass : SAINBase, ISAINClass
    {
        public bool ClearShot => FriendlyFireStatus != FriendlyFireStatus.FriendlyBlock;

        public FriendlyFireStatus FriendlyFireStatus
        {
            get
            {
                if (!Bot.SAINLayersActive)
                {
                    _ffStatus = FriendlyFireStatus.None;
                }
                else if (_nextCheckFFTime < Time.time)
                {
                    _nextCheckFFTime = Time.time + 0.1f;
                    _ffStatus = CheckFriendlyFire();
                }
                return _ffStatus;
            }
        }

        public SAINFriendlyFireClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            AimSettings = BotOwner.Settings.FileSettings.Aiming;
        }

        public void Update()
        {
            if (FriendlyFireStatus == FriendlyFireStatus.FriendlyBlock)
            {
                StopShooting();
            }
        }

        public void Dispose()
        {
        }

        public FriendlyFireStatus CheckFriendlyFire()
        {
            if (!Bot.Squad.BotInGroup)
            {
                return FriendlyFireStatus.None;
            }

            setSphereCastSize();
            var aimData = BotOwner.AimingData;
            FriendlyFireStatus friendlyFire = checkFFToTarget(aimData?.RealTargetPoint);

            if (friendlyFire != FriendlyFireStatus.FriendlyBlock)
            {
                friendlyFire = checkFFToTarget(aimData?.EndTargetPoint);
            }

            return friendlyFire;
        }

        private FriendlyFireStatus checkFFToTarget(Vector3? target)
        {
            FriendlyFireStatus friendlyFire;
            if (target != null &&
                BotOwner.ShootData?.CheckFriendlyFire(BotOwner.WeaponRoot.position, target.Value) == true)
            {
                friendlyFire = FriendlyFireStatus.FriendlyBlock;
            }
            else
            {
                friendlyFire = FriendlyFireStatus.Clear;
            }
            return friendlyFire;
        }

        private void setSphereCastSize()
        {
            if (AimSettings == null)
            {
                Logger.LogError($"Aim Settings are null, cannot edit friendly fire settings!");
                return;
            }
            if (defaultSphereSize == 0f)
            {
                defaultSphereSize = currentSphereSize;
                shootingSphereSize = defaultSphereSize * 1.5f;
            }
            bool shooting = BotOwner.ShootData.Shooting;
            if (shooting && currentSphereSize != shootingSphereSize)
            {
                AimSettings.SHPERE_FRIENDY_FIRE_SIZE = shootingSphereSize;
            }
            else if (!shooting && currentSphereSize != defaultSphereSize)
            {
                AimSettings.SHPERE_FRIENDY_FIRE_SIZE = defaultSphereSize;
            }
        }

        private BotGlobalAimingSettings AimSettings;

        public void StopShooting()
        {
            BotOwner.ShootData?.EndShoot();
            BotOwner.AimingData?.LoseTarget();
        }

        private FriendlyFireStatus _ffStatus;
        private float defaultSphereSize = 0f;
        private float shootingSphereSize = 0f;
        private float currentSphereSize => AimSettings.SHPERE_FRIENDY_FIRE_SIZE;

        private float _nextCheckFFTime = 0f;
    }
}