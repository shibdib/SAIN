using EFT;
using SAIN.Helpers;
using SAIN.Preset;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using GrenadeThrowChecker = GClass493;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class GrenadeThrowDecider : BotSubClass<BotGrenadeManager>, IBotDecisionClass
    {
        public GrenadeThrowDecider(BotGrenadeManager grenadeClass) : base(grenadeClass)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(UpdatePresetSettings);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public void UpdatePresetSettings(SAINPresetClass preset)
        {
            _grenadesEnabled = preset.GlobalSettings.General.BotsUseGrenades;

            var sainSettings = Bot.Info.FileSettings;
            _canThrowGrenades = sainSettings.Core.CanGrenade;
            _canThrowAtVisEnemies = sainSettings.Grenade.ThrowAtVisibleEnemies;
            _canThrowWhileSprint = sainSettings.Grenade.CanThrowWhileSprinting;
            _minEnemyDistToThrow = sainSettings.Grenade.MinEnemyDistance;
            _minFriendlyDistToThrow = sainSettings.Grenade.MinFriendlyDistance;
            _minFriendlyDistToThrow_SQR = _minFriendlyDistToThrow * _minFriendlyDistToThrow;
            _throwGrenadeFreq = sainSettings.Grenade.ThrowGrenadeFrequency;
            _minThrowDistPercent = sainSettings.Grenade.MIN_THROW_DIST_PERCENT_0_1;

            _blindCornerDistToThrow = 5f;
            _blindCornerDistToLastKnown_Max_SQR = _blindCornerDistToThrow * _blindCornerDistToThrow;
            _checkThrowPos_HeightOffset = 0.25f;
        }

        private bool _grenadesEnabled = true;
        private bool _canThrowGrenades = true;
        private bool _canThrowAtVisEnemies = false;
        private bool _canThrowWhileSprint = false;
        private float _timeSinceSeenBeforeThrow = 3f;
        private float _maxTimeSinceUpdatedCanThrow = 120f;
        private float _minEnemyDistToThrow = 8f;
        private float _throwGrenadeFreq = 3f;
        private float _minFriendlyDistToThrow = 8f;
        private float _minFriendlyDistToThrow_SQR = 64;
        private float _blindCornerDistToThrow = 5f;
        private float _blindCornerDistToLastKnown_Max_SQR = 25f;
        private float _checkThrowPos_HeightOffset = 0.25f;
        private float _maxEnemyDistToCheckThrow = 75f;
        private float _friendlyCloseRecheckTime = 1f;
        private float _announceThrowingNadeChance = 75f;
        private float _sayNeedGrenadeFreq = 10f;
        private float _sayNeedGrenadeChance = 5f;

        public bool GetDecision(Enemy enemy, out string reason)
        {
            if (!canCheckThrow(out reason))
            {
                return false;
            }
            if (!canThrowAtEnemy(enemy, out reason))
            {
                return false;
            }
            var grenades = BotOwner.WeaponManager.Grenades;
            if (!grenades.HaveGrenade)
            {
                _nextPosibleAttempt = Time.time + _throwGrenadeFreq;
                sayNeedNades();
                reason = "noNades";
                return false;
            }
            if (tryThrowGrenade() || (findThrowTarget(enemy) && tryThrowGrenade()))
            {
                _nextPosibleAttempt = Time.time + _throwGrenadeFreq;
                reason = "throwingNow";
                return true;
            }
            reason = "noGoodTarget";
            return false;
        }

        private GrenadeClass _currentGrenade;

        private bool canCheckThrow(out string reason)
        {
            if (!_grenadesEnabled)
            {
                reason = "grenadesDisabledGlobal";
                return false;
            }
            if (!_canThrowGrenades)
            {
                reason = "grenadesDisabled";
                return false;
            }
            if (this._nextPosibleAttempt > Time.time)
            {
                reason = "nextAttemptTime";
                return false;
            }
            if (!_canThrowWhileSprint &&
                (Player.IsSprintEnabled || Bot.Mover.SprintController.Running))
            {
                reason = "running";
                return false;
            }
            var weaponManager = BotOwner.WeaponManager;
            if (weaponManager != null)
            {
                if (weaponManager.Selector.IsChanging)
                {
                    reason = "changingWeapon";
                    return false;
                }
                if (weaponManager.Reload.Reloading)
                {
                    reason = "reloading";
                    return false;
                }
            }
            if (Player.HandsController.IsInInteractionStrictCheck())
            {
                reason = "handsController Busy";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private bool canThrowAtEnemy(Enemy enemy, out string reason)
        {
            if (!_canThrowAtVisEnemies)
            {
                if (enemy.IsVisible)
                {
                    reason = "enemyVisible";
                    return false;
                }
                if (enemy.TimeSinceSeen < _timeSinceSeenBeforeThrow)
                {
                    reason = "enemySeenRecent";
                    return false;
                }
            }
            if (enemy.TimeSinceLastKnownUpdated > _maxTimeSinceUpdatedCanThrow)
            {
                reason = "lastUpdatedTooLong";
                return false;
            }
            var lastKnown = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnown == null)
            {
                reason = "nullLastKnown";
                return false;
            }
            if (lastKnown.DistanceToBot > _maxEnemyDistToCheckThrow)
            {
                reason = "tooFar";
                return false;
            }
            if (lastKnown.DistanceToBot < _minEnemyDistToThrow)
            {
                reason = "tooClose";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private bool findThrowTarget(Enemy enemy)
        {
            EnemyPlace lastKnown = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnown != null)
            {
                Vector3 lastKnownPos = lastKnown.Position;
                if (!checkFriendlyDistances(lastKnownPos))
                {
                    return false;
                }
                if (tryThrowToPos(lastKnownPos, "LastKnownPosition"))
                {
                    return true;
                }
                if (checkCanThrowBlindCorner(enemy, lastKnownPos))
                {
                    return true;
                }
            }
            return false;
        }

        private bool checkCanThrowBlindCorner(Enemy enemy, Vector3 lastKnownPos)
        {
            Vector3? blindCorner = enemy.Path.EnemyCorners.GroundPosition(ECornerType.Blind);
            if (blindCorner == null)
            {
                return false;
            }
            Vector3 blindCornerPos = blindCorner.Value;
            if ((blindCornerPos - lastKnownPos).sqrMagnitude > _blindCornerDistToLastKnown_Max_SQR)
            {
                return false;
            }
            if (!checkFriendlyDistances(blindCornerPos))
            {
                return false;
            }
            if (tryThrowToPos(blindCornerPos, "BlindCornerToEnemy"))
            {
                return true;
            }
            return false;
        }

        private bool tryThrowToPos(Vector3 pos, string posString)
        {
            pos += Vector3.up * _checkThrowPos_HeightOffset;
            var weaponRoot = Bot.Transform.WeaponRoot;
            if (canThrowAGrenade(weaponRoot, pos))
            {
                //Logger.LogDebug($"{posString} Can Throw to pos");
                return true;
            }
            if (canThrowAGrenade(weaponRoot, pos + (pos - Bot.Position).normalized))
            {
                //Logger.LogDebug($"{posString} Can Throw to pos + dir");
                return true;
            }
            if (canThrowAGrenade(weaponRoot, pos + Vector3.up * 0.5f))
            {
                //Logger.LogDebug($"{posString} Can Throw to pos + vector3.up * 0.5f");
                return true;
            }
            return false;
        }

        private bool tryThrowGrenade()
        {
            var grenades = BotOwner.WeaponManager.Grenades;
            if (!grenades.ReadyToThrow)
            {
                return false;
            }
            if (!grenades.AIGreanageThrowData.IsUpToDate())
            {
                return false;
            }
            if (grenades.DoThrow())
            {
                //if (Player.HandsAnimator is FirearmsAnimator firearmAnimator)
                //{
                //    firearmAnimator.SetGrenadeFire(FirearmsAnimator.EGrenadeFire.Throw);
                //}
                //else
                //{
                //    Logger.LogWarning("fail");
                //}
                Bot.Talk.GroupSay(EPhraseTrigger.OnGrenade, null, false, _announceThrowingNadeChance);
                return true;
            }
            return false;
        }

        private bool canThrowAGrenade(Vector3 from, Vector3 trg)
        {
            if (_nextPosibleAttempt > Time.time)
            {
                return false;
            }
            if (Player.IsSprintEnabled || Bot.Mover.SprintController.Running)
            {
                return false;
            }
            if (!checkFriendlyDistances(trg))
            {
                _nextPosibleAttempt = 1f + Time.time;
                return false;
            }
            AIGreandeAng angle = getAngleToThrow();
            AIGreanageThrowData aigreanageThrowData = GrenadeThrowChecker.CanThrowGrenade2(from, trg, _maxPower, angle, -1f, _minThrowDistPercent);
            if (aigreanageThrowData.CanThrow)
            {
                if (Physics.Raycast(from, aigreanageThrowData.Direction, 1.5f, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    Logger.LogDebug($"blocked by object, cant throw");
                    return false;
                }
                BotOwner.WeaponManager.Grenades.SetThrowData(aigreanageThrowData);
                return true;
            }
            return false;
        }

        private AIGreandeAng getAngleToThrow()
        {
            var angles = Bot.Memory.Location.IsIndoors ? _indoorAngles : _outdoorAngles;
            AIGreandeAng angle = angles.PickRandom();
            return angle;
        }

        private bool checkFriendlyDistances(Vector3 trg)
        {
            var members = Bot.Squad.Members;
            if (members == null || members.Count <= 1)
                return true;

            foreach (var member in members.Values)
                if (member != null && (member.Position - trg).sqrMagnitude < _minFriendlyDistToThrow_SQR)
                {
                    _nextPosibleAttempt = Time.time + _friendlyCloseRecheckTime;
                    return false;
                }

            return true;
        }

        private void sayNeedNades()
        {
            if (_nextSayNeedGrenadeTime < Time.time)
            {
                _nextSayNeedGrenadeTime = Time.time + _sayNeedGrenadeFreq;
                Bot.Talk.GroupSay(EPhraseTrigger.NeedFrag, null, true, _sayNeedGrenadeChance);
            }
        }

        private float _nextSayNeedGrenadeTime;
        private float _minThrowDistPercent;
        private float _maxPower => BotOwner.WeaponManager.Grenades.MaxPower;
        private float _nextPosibleAttempt;
        private float _nextGrenadeCheckTime;

        private static AIGreandeAng[] _indoorAngles =
        {
            AIGreandeAng.ang5,
            AIGreandeAng.ang15,
            AIGreandeAng.ang25,
            //AIGreandeAng.ang35,
        };

        private static AIGreandeAng[] _outdoorAngles =
        {
            AIGreandeAng.ang15,
            AIGreandeAng.ang25,
            AIGreandeAng.ang35,
            AIGreandeAng.ang45,
            //AIGreandeAng.ang55,
            //AIGreandeAng.ang65,
        };

        static GrenadeThrowDecider()
        {

        }
    }
}