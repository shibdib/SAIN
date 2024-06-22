using BepInEx.Logging;
using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using EFT.HealthSystem;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotHitReaction : SAINBase, ISAINClass
    {
        public EHitReaction HitReaction { get; private set; }

        public SAINBotHitReaction(BotComponent sain) : base(sain)
        {
            HealthController = sain.Player.HealthController;
            addPart(EBodyPart.Head);
            addPart(EBodyPart.Chest);
            addPart(EBodyPart.LeftArm);
            addPart(EBodyPart.RightArm);
            addPart(EBodyPart.LeftLeg);
            addPart(EBodyPart.RightLeg);
            addPart(EBodyPart.Stomach);
        }

        public IHealthController HealthController { get; private set; }

        private void addPart(EBodyPart part)
        {
            BodyParts.Add(part, new BodyPartStatus(part, HealthController));
        }

        public void Init()
        {
            Player.BeingHitAction += GetHit;
            Bot.EnemyController.OnEnemyRemoved += clearEnemy;
        }

        private void clearEnemy(string profileId)
        {
            if (EnemyWhoLastShotMe != null && 
                EnemyWhoLastShotMe.EnemyProfileId == profileId)
            {
                EnemyWhoLastShotMe = null;
            }
        }

        public void Update()
        {
            if (_updateHealthTime < Time.time || injuryRegistered)
            {
                _updateHealthTime = Time.time + 1f;
                if (injuryRegistered)
                {
                    injuryRegistered = false;
                }
                LeftArmInjury = BodyParts[EBodyPart.LeftArm].InjurySeverity;
                RightArmInjury = BodyParts[EBodyPart.RightArm].InjurySeverity;
            }
        }

        private float _updateHealthTime;

        public EInjurySeverity LeftArmInjury { get; private set; }
        public EInjurySeverity RightArmInjury { get; private set; }

        public bool ArmsInjured => LeftArmInjury != EInjurySeverity.None || RightArmInjury != EInjurySeverity.None;

        public void Dispose()
        {
            if (Player != null)
            {
                Player.BeingHitAction -= GetHit;
            }
            Bot.EnemyController.OnEnemyRemoved -= clearEnemy;
        }

        private bool injuryRegistered;

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
            injuryRegistered = true;
            TimeLastShot = Time.time;
            switch (bodyPart)
            {
                case EBodyPart.Head:
                    GetHitInHead(damageInfo);
                    break;

                case EBodyPart.Chest:
                case EBodyPart.Stomach:
                    GetHitInCenter(damageInfo);
                    break;

                case EBodyPart.LeftLeg:
                case EBodyPart.RightLeg:
                    GetHitInLegs(damageInfo);
                    break;

                default:
                    GetHitInArms(damageInfo); 
                    break;
            }

            var player = damageInfo.Player?.iPlayer;
            if (player != null)
            {
                Enemy enemy = Bot.EnemyController.GetEnemy(player.ProfileId);
                if (enemy != null &&
                    enemy.IsValid)
                {
                    EnemyWhoLastShotMe = enemy;
                    enemy.EnemyStatus.RegisterShotByEnemy(damageInfo);
                }
            }
        }

        public float TimeLastShot { get; private set; }
        public float TimeSinceShot => Time.time - TimeLastShot;
        public Enemy EnemyWhoLastShotMe { get; private set; }

        private void GetHitInLegs(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Legs;
        }

        private void GetHitInArms(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Arms;
        }

        private void GetHitInCenter(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Center;
        }

        private void GetHitInHead(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Head;
        }

        const float StunDamageThreshold = 50;
        const float BaseStunTime = 3f;

        private float TimeStunHappened;
        private float StunTime;

        public bool IsStunned 
        { 
            get
            {
                if (_isStunned && StunTime < Time.time)
                {
                    _isStunned = false;
                }
                return _isStunned;
            }
            set
            {
                if (value)
                {
                    TimeStunHappened = Time.time;
                    StunTime = Time.time + BaseStunTime * UnityEngine.Random.Range(0.75f, 1.25f);
                }
                _isStunned = value;
            }
        }

        private bool _isStunned;

        private bool IsStunnedFromDamage(DamageInfo damageInfo)
        {
            return false;
        }

        public Dictionary<EBodyPart, BodyPartStatus> BodyParts = new Dictionary<EBodyPart, BodyPartStatus>();
    }

    public class BodyPartStatus
    {
        public BodyPartStatus(EBodyPart part, IHealthController healthController)
        {
            _bodyPart = part;
            _healthController = healthController;
        }

        private readonly IHealthController _healthController;
        private readonly EBodyPart _bodyPart;

        public EInjurySeverity InjurySeverity
        {
            get
            {
                float health = PartHealthNormalized;
                if (health > 0.75f)
                {
                    return EInjurySeverity.None;
                }
                if (health > 0.4f)
                {
                    return EInjurySeverity.Injury;
                }
                if (health > 0.01f)
                {
                    return EInjurySeverity.HeavyInjury;
                }
                return EInjurySeverity.Destroyed;
            }
        }

        public float PartHealth => _healthController.GetBodyPartHealth(_bodyPart, false).Current;
        public float PartHealthNormalized => _healthController.GetBodyPartHealth(_bodyPart, false).Normalized;
        public bool PartDestoyed => _healthController.IsBodyPartDestroyed(_bodyPart);

    }

    public enum EHitReaction
    {
        None,
        Head,
        Center,
        Legs,
        Arms,
    }
}