using UnityEngine;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes
{
    public enum EEnemyAction
    {
        None = 0,
        Reloading = 1,
        HasGrenade = 2,
        Healing = 3,
        UsingSurgery = 4,
        TryingToExtract = 5,
        Looting = 6,
    }

    public class SAINEnemyStatus : EnemyBase
    {
        public EEnemyAction EnemyAction
        {
            get
            {
                if (EnemyIsReloading)
                {
                    return EEnemyAction.Reloading;
                }
                else if (EnemyHasGrenadeOut)
                {
                    return EEnemyAction.HasGrenade;
                }
                else if (EnemyIsHealing)
                {
                    return EEnemyAction.Healing;
                }
                else
                {
                    return EEnemyAction.None;
                }
            }
            set
            {
                switch (value)
                {
                    case EEnemyAction.None:
                        break;

                    case EEnemyAction.Reloading:
                        EnemyIsReloading = true;
                        break;

                    case EEnemyAction.HasGrenade:
                        EnemyHasGrenadeOut = true;
                        break;

                    case EEnemyAction.Healing:
                        EnemyIsHealing = true;
                        break;
                }
            }
        }


        public SAINEnemyStatus(SAINEnemy enemy) : base(enemy)
        {
        }

        public bool EnemyLookingAtMe
        {
            get
            {
                Vector3 directionToBot = (SAIN.Position - EnemyPosition).normalized;
                Vector3 enemyLookDirection = EnemyPerson.Transform.LookDirection.normalized;
                float dot = Vector3.Dot(directionToBot, enemyLookDirection);
                return dot >= 0.9f;
            }
        }

        public bool ShotAtMeRecently
        {
            get
            {
                return _enemyShotAtMe.Value;
            }
            set
            {
                _enemyShotAtMe.Value = value;
            }
        }

        private readonly ExpirableBool _enemyShotAtMe = new ExpirableBool(30f, 0.75f, 1.25f);

        public bool EnemyIsReloading
        {
            get
            {
                return _enemyIsReloading.Value;
            }
            set
            {
                _enemyIsReloading.Value = value;
            }
        }

        private readonly ExpirableBool _enemyIsHealing = new ExpirableBool(4f, 0.75f, 1.25f);

        public bool EnemyHasGrenadeOut
        {
            get
            {
                return _enemyHasGrenade.Value;
            }
            set
            {
                _enemyHasGrenade.Value = value;
            }
        }

        private readonly ExpirableBool _enemyHasGrenade = new ExpirableBool(4f, 0.75f, 1.25f);

        public bool EnemyIsHealing
        {
            get
            {
                return _enemyIsHealing.Value;
            }
            set
            {
                _enemyIsHealing.Value = value;
            }
        }

        private readonly ExpirableBool _enemyIsReloading = new ExpirableBool(4f, 0.75f, 1.25f);
    }

    public class ExpirableBool
    {
        public ExpirableBool(float expireTime, float randomMin, float randomMax)
        {
            _expireTime = expireTime;
            _randomMin = randomMin;
            _randomMax = randomMax;
        }

        public bool Value
        {
            get
            {
                if (_value && _resetTime < Time.time)
                {
                    _value = false;
                }
                return _value;
            }
            set
            {
                if (value == true)
                {
                    _resetTime = Time.time + _expireTime * Random.Range(_randomMin, _randomMax);
                }
                _value = value;
            }
        }

        private bool _value;
        private float _resetTime;
        private readonly float _expireTime;
        private readonly float _randomMin;
        private readonly float _randomMax;
    }
}