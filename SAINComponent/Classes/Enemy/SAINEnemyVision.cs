using EFT;
using EFT.InventoryLogic;
using SAIN.Helpers;
using SAIN.Patches.Vision;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.SubComponents;
using UnityEngine;
using static MultiFlareLight;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemyVision : EnemyBase
    {
        public SAINEnemyVision(SAINEnemy enemy) : base(enemy)
        {
        }

        public void Update(bool isCurrentEnemy)
        {
            UpdateVisible(false);
            UpdateCanShoot(false);
        }

        public bool FirstContactOccured { get; private set; }
        public bool ShallReportRepeatContact { get; set; }
        public bool ShallReportLostVisual { get; set; }

        private const float _repeatContactMinSeenTime = 12f;
        private const float _lostContactMinSeenTime = 12f;

        private float _realLostVisionTime;

        public void UpdateVisible(bool forceOff)
        {
            bool wasVisible = IsVisible;
            if (forceOff)
            {
                IsVisible = false;
            }
            else
            {
                IsVisible = EnemyInfo?.IsVisible == true && InLineOfSight;
            }

            if (IsVisible)
            {
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                    if (Seen && TimeSinceSeen >= _repeatContactMinSeenTime)
                    {
                        ShallReportRepeatContact = true;
                    }
                }
                if (!Seen)
                {
                    FirstContactOccured = true;
                    TimeFirstSeen = Time.time;
                    Seen = true;
                }
                _realLostVisionTime = Time.time;
                TimeLastSeen = Time.time;
                LastSeenPosition = EnemyPerson.Position;
            }

            if (Time.time - _realLostVisionTime < 1f)
            {
                Enemy.UpdateSeenPosition(EnemyPerson.Position);
            }

            if (!IsVisible)
            {
                if (Seen
                    && TimeSinceSeen > _lostContactMinSeenTime
                    && _nextReportLostVisualTime < Time.time)
                {
                    _nextReportLostVisualTime = Time.time + 20f;
                    ShallReportLostVisual = true;
                }
                VisibleStartTime = -1f;
            }

            if (IsVisible != wasVisible)
            {
                LastChangeVisionTime = Time.time;
            }
        }

        private float _nextReportLostVisualTime;

        public void UpdateCanShoot(bool forceOff)
        {
            if (forceOff)
            {
                CanShoot = false;
                return;
            }
            CanShoot = EnemyInfo?.CanShoot == true;
        }

        public bool InLineOfSight { get; set; }
        public bool IsVisible { get; private set; }
        public bool CanShoot { get; private set; }
        public Vector3? LastSeenPosition { get; set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1f;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }

        public float GainSightModifier
        {
            get
            {
                calcVisionModifiers();
                return _gainSightModifier;
            }
        }

        public float VisionDistanceModifier
        {
            get
            {
                calcVisionModifiers();
                return _visionDistanceModifier;
            }
        }

        private void calcVisionModifiers()
        {
            if (_nextCheckVisTime < Time.time)
            {
                _nextCheckVisTime = Time.time + 0.2f;
                _gainSightModifier = GetGainSightModifier(EnemyInfo, SAIN);
                _visionDistanceModifier = 0f;
            }
        }

        private float _gainSightModifier;
        private float _visionDistanceModifier;
        private float _nextCheckVisTime;

        public static float GetGainSightModifier(EnemyInfo enemyInfo, Bot sain)
        {
            float result = 1f;
            float dist = (enemyInfo.Owner.Position - enemyInfo.CurrPosition).magnitude;
            float weatherModifier = SAINPlugin.BotController.WeatherVision.VisibilityNum;
            float inverseWeatherModifier = Mathf.Sqrt(2f - weatherModifier);

            WildSpawnType wildSpawnType = enemyInfo.Owner.Profile.Info.Settings.Role;
            if (PresetHandler.LoadedPreset.BotSettings.SAINSettings.TryGetValue(wildSpawnType, out var sainSettings))
            {
                BotDifficulty diff = enemyInfo.Owner.Profile.Info.Settings.BotDifficulty;
                result *= Math.CalcVisSpeed(dist, sainSettings.Settings[diff]);
            }

            var person = enemyInfo.Person;
            if (person != null)
            {
                Player player = EFTInfo.GetPlayer(enemyInfo.Person.ProfileId);
                if (player != null)
                {
                    var gearInfo = SAINGearInfoHandler.GetGearInfo(player);
                    if (gearInfo != null)
                    {
                        result *= gearInfo.GetStealthModifier(enemyInfo.Distance);
                    }

                    bool flare =  person.AIData.GetFlare;
                    bool suppressedFlare = 
                        flare 
                        && player.HandsController.Item is Weapon weapon 
                        && gearInfo?.GetWeaponInfo(weapon)?.HasSuppressor == true;

                    // Only apply vision speed debuff from weather if their enemy has not shot an unsuppressed weapon
                    if (!flare || suppressedFlare)
                    {
                        result *= inverseWeatherModifier;
                    }

                    LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
                    if (player.IsSprintEnabled)
                    {
                        result *= Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, Mathf.InverseLerp(0, 5f, player.Velocity.magnitude)); // 5f is the observed max sprinting speed with gameplays (with Realism, which gives faster sprinting)
                    }

                    Vector3 botEyeToPlayerBody = enemyInfo.Person.MainParts[BodyPartType.body].Position - enemyInfo.Owner.MainParts[BodyPartType.head].Position;
                    var visionAngleDeviation = Vector3.Angle(new Vector3(botEyeToPlayerBody.x, 0f, botEyeToPlayerBody.z), botEyeToPlayerBody);

                    if (botEyeToPlayerBody.y >= 0)
                    {
                        float angleFactor = Mathf.InverseLerp(0, globalLookSettings.HighElevationMaxAngle, visionAngleDeviation);
                        result *= Mathf.Lerp(1f, globalLookSettings.HighElevationVisionModifier, angleFactor);
                    }
                    else
                    {
                        float angleFactor = Mathf.InverseLerp(0, globalLookSettings.LowElevationMaxAngle, visionAngleDeviation);
                        result *= Mathf.Lerp(1f, globalLookSettings.LowElevationVisionModifier, angleFactor);
                    }

                    if (!player.IsAI)
                    {
                        result *= SAINNotLooking.GetVisionSpeedDecrease(enemyInfo);
                    }

                    SAINEnemy sainEnemy = sain?.EnemyController.GetEnemy(player.ProfileId);
                    if (sainEnemy?.EnemyStatus.PositionalFlareEnabled == true
                        && sainEnemy.Heard
                        && sainEnemy.TimeSinceHeard < 300f)
                    {
                        result *= 0.9f;
                    }
                }
            }

            return result;
        }
    }
}