using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent.BaseClasses;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Debug;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.SubComponents;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent
{
    public class SAINComponentClass : MonoBehaviour, IBotComponent
    {
        public static bool TryAddSAINToBot(BotOwner botOwner, out SAINComponentClass sainComponent)
        {
            Player player = EFTInfo.GetPlayer(botOwner?.ProfileId);
            GameObject gameObject = botOwner?.gameObject;

            if (gameObject != null && player != null)
            {
                // If Somehow this bot already has SAIN attached, destroy it.
                if (gameObject.TryGetComponent(out sainComponent))
                {
                    sainComponent.Dispose();
                }

                // Create a new Component
                sainComponent = gameObject.AddComponent<SAINComponentClass>();

                if (SAINPersonComponent.TryAddSAINPersonToPlayer(player, out SAINPersonComponent personComponent)
                    && sainComponent?.Init(personComponent?.SAINPerson) == true)
                {
                    return true;
                }
            }
            sainComponent = null;
            return false;
        }

        private void PlayerKilled(EDamageType damageType)
        {
            if (damageType == EDamageType.Bullet)
            {
                IFirearmHandsController firearmHandsController = Player?.HandsController as IFirearmHandsController;
                if (firearmHandsController != null)
                {
                    // firearmHandsController.SetTriggerPressed(true);
                }
            }
        }

        public Action<string, BotOwner> OnSAINDisposed { get; set; }
        public SAINPersonClass Person { get; private set; }
        public SAINMedical Medical { get; private set; }

        public bool Init(SAINPersonClass person)
        {
            if (person == null)
            {
                Logger.LogAndNotifyError("Person is Null in SAINComponent Init");
                return false;
            }

            if (SAINPlugin.LoadedPreset.GlobalSettings.PowerCalc.CalcPower(person.Player, out float power))
            {
                _powerCalcd = true;
            }

            Person = person;

            try
            {
                NoBushESP = this.GetOrAddComponent<SAINNoBushESP>();
                NoBushESP.Init(person.BotOwner, this);

                FlashLight = person.Player.gameObject.AddComponent<SAINFlashLightComponent>();

                // Must be first, other classes use it
                Squad = new SAINSquadClass(this);
                Equipment = new SAINBotEquipmentClass(this);
                Info = new SAINBotInfoClass(this);
                Memory = new SAINMemoryClass(this);
                BotStuck = new SAINBotUnstuckClass(this);
                Hearing = new SAINHearingSensorClass(this);
                Talk = new SAINBotTalkClass(this);
                Decision = new SAINDecisionClass(this);
                Cover = new SAINCoverClass(this);
                SelfActions = new SAINSelfActionClass(this);
                Steering = new SAINSteeringClass(this);
                Grenade = new SAINBotGrenadeClass(this);
                Mover = new SAINMoverClass(this);
                EnemyController = new SAINEnemyController(this);
                FriendlyFireClass = new SAINFriendlyFireClass(this);
                Vision = new SAINVisionClass(this);
                Search = new SAINSearchClass(this);
                Vault = new SAINVaultClass(this);
                Suppression = new SAINBotSuppressClass(this);
                AILimit = new SAINAILimit(this);
                AimDownSightsController = new AimDownSightsController(this);
                BotHitReaction = new SAINBotHitReaction(this);
                SpaceAwareness = new SAINBotSpaceAwareness(this);
                DoorOpener = new SAINDoorOpener(this, person.BotOwner);
                Medical = new SAINMedical(this);

                NavMeshAgent = this.GetComponent<NavMeshAgent>();
                if (NavMeshAgent == null)
                {
                    Logger.LogError("Agent Null");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Init SAIN ERROR, Disposing.");
                Logger.LogError(ex);
                Dispose();
                return false;
            }

            Search.Init();
            Memory.Init();
            EnemyController.Init();
            FriendlyFireClass.Init();
            Vision.Init();
            Equipment.Init();
            Mover.Init();
            BotStuck.Init();
            Hearing.Init();
            Talk.Init();
            Decision.Init();
            Cover.Init();
            Info.Init();
            Squad.Init();
            SelfActions.Init();
            Grenade.Init();
            Steering.Init();
            Vault.Init();
            Suppression.Init();
            AILimit.Init();
            AimDownSightsController.Init();
            BotHitReaction.Init();
            SpaceAwareness.Init();
            Medical.Init();

            try
            {
                Player.HealthController.DiedEvent += PlayerKilled;
            }
            catch
            {

            }

            TimeBotCreated = Time.time;

            return true;
        }

        public NavMeshAgent NavMeshAgent { get; private set; }

        public float TimeBotCreated { get; private set; }


        private void Update()
        {
            if (BotOwner == null || this == null || Player == null)
            {
                Dispose();
                return;
            }
            if (IsDead || Singleton<GameWorld>.Instance == null)
            {
                Dispose();
                return;
            }

            if (GameIsEnding)
            {
                return;
            }

            if (BotActive)
            {
                checkLayerActive();
                handlePatrolData();

                AILimit.UpdateAILimit();
                if (AILimit.LimitAIThisFrame)
                {
                    return;
                }

                if (BotOwner.Mover.IsMoving)
                {
                    DoorOpener.Update();
                }

                if (!_powerCalcd && BotOwner?.WeaponManager?.IsWeaponReady == true)
                {
                    if (SAINPlugin.LoadedPreset.GlobalSettings.PowerCalc.CalcPower(Player, out float power))
                    {
                        Info.CalcPersonality();
                        _powerCalcd = true;
                    }
                }

                Decision.Update();
                Search.Update();
                Memory.Update();
                EnemyController.Update();
                FriendlyFireClass.Update();
                Vision.Update();
                Equipment.Update();
                Mover.Update();
                BotStuck.Update();
                Hearing.Update();
                Talk.Update();
                Cover.Update();
                Info.Update();
                Squad.Update();
                SelfActions.Update();
                Grenade.Update();
                Steering.Update();
                Vault.Update();
                Suppression.Update();
                AimDownSightsController.Update();
                BotHitReaction.Update();
                SpaceAwareness.Update();
                Medical.Update();

                //BotOwner.DoorOpener.Update(); 
                UpdateGoalTarget();

                if (_nextCheckReloadTime < Time.time)
                {
                    _nextCheckReloadTime = Time.time + 0.5f;
                    if (!BotOwner.WeaponManager.HaveBullets)
                    {
                        SelfActions.TryReload();
                    }
                }

                if (ManualShootReason != EShootReason.None && (!BotOwner.WeaponManager.HaveBullets || _timeStartManualShoot + 1f < Time.time))
                {
                    Shoot(false, Vector3.zero);
                }
            }
        }

        private float _nextCheckFlashlightTime;
        private GearInfoContainer GearInfoContainer;
        private float _nextCheckReloadTime;
        private bool _powerCalcd;
        public SAINDoorOpener DoorOpener { get; private set; }
        public bool PatrolDataPaused { get; private set; }
        public bool IsHumanACareEnemy
        {
            get
            {
                if (HasEnemy && !Enemy.IsAI)
                {
                    return true;
                }
                var closestHeard = EnemyController.ClosestHeardEnemy;
                if (closestHeard != null && !closestHeard.IsAI)
                {
                    return true;
                }

                foreach (var player in Memory.VisiblePlayers)
                {
                    if (!player.IsAI)
                    {
                        return true;
                    }
                }
                
                return false;
            }
        }
        public bool Extracting { get; set; }

        private void handlePatrolData()
        {
            if (CurrentTargetPosition == null 
                && !Extracting)
            {
                PatrolDataPaused = false;
                BotOwner.PatrollingData?.Unpause();
                if (!_speedReset)
                {
                    _speedReset = true;
                    BotOwner.SetTargetMoveSpeed(1f);
                    BotOwner.Mover.SetPose(1f);
                }
                return;
            }

            PatrolDataPaused = true;
            BotOwner.PatrollingData?.Pause();
            if (_speedReset)
            {
                _speedReset = false;
            }
        }

        private bool _speedReset;
        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;
        public bool BotIsAlive => Player?.HealthController?.IsAlive == true;
        public float DistanceToAimTarget
        {
            get
            {
                if (BotOwner.AimingData != null)
                {
                    return BotOwner.AimingData.LastDist2Target;
                }
                if (Enemy != null)
                {
                    return Enemy.RealDistance;
                }
                else if (CurrentTargetPosition != null)
                {
                    return (CurrentTargetPosition.Value - Position).magnitude;
                }
                return 200f;
            }
        }

        public bool Shoot(bool value, Vector3 targetPos, bool checkFF = true, EShootReason reason = EShootReason.None)
        {
            ManualShootTargetPosition = targetPos;
            ManualShootReason = value ? reason : EShootReason.None;

            if (value)
            {
                if (checkFF && !FriendlyFireClass.ClearShot)
                {
                    ManualShootReason = EShootReason.None;
                    BotOwner.ShootData.EndShoot();
                    return false;
                }
                else if (BotOwner.ShootData.Shoot())
                {
                    _timeStartManualShoot = Time.time;
                    ManualShootReason = reason;
                    return true;
                }
                else
                {
                    ManualShootReason = EShootReason.None;
                    return false;
                }
            }
            ManualShootReason = EShootReason.None;
            return false;
        }

        private float _timeStartManualShoot;

        public Vector3 ManualShootTargetPosition { get; private set; }

        public EShootReason ManualShootReason { get; private set; }

        public enum EShootReason
        {
            None = 0,
            SquadSuppressing = 1,
            Blindfire = 2,
            WalkToCoverSuppress = 3,
        }

        private bool SAINActive => BigBrainHandler.IsBotUsingSAINLayer(BotOwner);

        public void checkLayerActive()
        {
            if (RecheckTimer < Time.time)
            {
                CombatLayersActive = BigBrainHandler.IsBotUsingSAINCombatLayer(BotOwner);
                if (SAINActive)
                {
                    RecheckTimer = Time.time + 0.5f;
                }
                else
                {
                    RecheckTimer = Time.time + 0.05f;
                }
            }
        }
        public bool CombatLayersActive { get; private set; }

        private float RecheckTimer = 0f;

        public void Dispose()
        {
            try
            {
                OnSAINDisposed?.Invoke(ProfileId, BotOwner);

                try
                {
                    Player.HealthController.DiedEvent -= PlayerKilled;
                }
                catch { }

                StopAllCoroutines();

                Search.Dispose();
                Memory.Dispose();
                EnemyController.Dispose();
                FriendlyFireClass.Dispose();
                Vision.Dispose();
                Equipment.Dispose();
                Mover.Dispose();
                BotStuck.Dispose();
                Hearing.Dispose();
                Talk.Dispose();
                Decision.Dispose();
                Cover.Dispose();
                Info.Dispose();
                Squad.Dispose();
                SelfActions.Dispose();
                Grenade.Dispose();
                Steering.Dispose();
                Enemy?.Dispose();
                Vault?.Dispose();
                Suppression?.Dispose();
                AILimit?.Dispose();
                AimDownSightsController?.Dispose();
                BotHitReaction?.Dispose();
                SpaceAwareness?.Dispose();
                Medical?.Dispose();

                try
                {
                    GameObject.Destroy(NoBushESP);
                    GameObject.Destroy(FlashLight);
                }
                catch { }

                Destroy(this);
            }
            catch { }
        }

        private void UpdateGoalTarget()
        {
            if (updateGoalTargetTimer < Time.time)
            {
                updateGoalTargetTimer = Time.time + 0.5f;
                var Target = GoalTargetPosition;
                if (Target != null)
                {
                    if ((Target.Value - Position).sqrMagnitude < 2f)
                    {
                        Talk.GroupSay(EPhraseTrigger.Clear, null, true, 40);
                        BotOwner.Memory.GoalTarget.Clear();
                        BotOwner.CalcGoal();
                    }
                    else if (BotOwner.Memory.GoalTarget.CreatedTime > 120f 
                        && Enemy == null
                        && Decision.CurrentSoloDecision != SoloDecision.Search)
                    {
                        BotOwner.Memory.GoalTarget.Clear();
                        BotOwner.CalcGoal();
                    }
                }
            }
        }

        private float updateGoalTargetTimer;

        public Vector3? GoalTargetPosition => BotOwner.Memory.GoalTarget.Position;

        public Vector3? CurrentTargetPosition
        {
            get
            {
                if (_nextGetTargetTime < Time.time)
                {
                    bool hadNoTarget = _currentTarget == null;
                    _nextGetTargetTime = Time.time + 0.05f;
                    _currentTarget = getTarget();

                    if (_currentTarget != null)
                    {
                        if (hadNoTarget)
                        {
                            TimeTargetFound = Time.time;
                        }
                        CurrentTargetDistance = (_currentTarget.Value - Position).magnitude;
                    }
                    else
                    {
                        CurrentTargetDistance = 0f;
                    }
                }
                return _currentTarget;
            }
        }

        public float TimeTargetFound { get; private set; }
        public float TimeSinceTargetFound => Time.time - TimeTargetFound;

        public float CurrentTargetDistance { get; private set; }

        private float _nextGetTargetTime;
        private Vector3? _currentTarget;

        private Vector3? getTarget()
        {
            if (HasEnemy && Enemy.IsVisible)
            {
                return Enemy.EnemyPosition;
            }
            if (BotOwner.Memory.IsUnderFire)
            {
                return Memory.UnderFireFromPosition;
            }

            if (HasEnemy)
            {
                var lastKnownPlace = Enemy.KnownPlaces.LastKnownPlace;
                if (lastKnownPlace != null)
                {
                    return lastKnownPlace.Position;
                }

                Vector3? lastPlaceHaventSeen = Enemy.KnownPlaces.GetPlaceHaventSeen()?.Position;
                if (lastPlaceHaventSeen != null)
                {
                    return lastPlaceHaventSeen;
                }

                lastPlaceHaventSeen = Enemy.KnownPlaces.GetPlaceHaventArrived()?.Position;
                if (lastPlaceHaventSeen != null)
                {
                    return lastPlaceHaventSeen;
                }

                return Enemy.EnemyPosition;
            }
            var placeForCheck = BotOwner.Memory.GoalTarget?.GoalTarget;
            if (placeForCheck != null)
            {
                return placeForCheck.Position;
            }
            return null;
        }

        public SAINBotSpaceAwareness SpaceAwareness { get; private set; }
        public SAINBotHitReaction BotHitReaction { get; private set; }
        public AimDownSightsController AimDownSightsController { get; private set; }
        public SAINAILimit AILimit { get; private set; }
        public SAINBotSuppressClass Suppression { get; private set; }
        public SAINVaultClass Vault { get; private set; }
        public SAINSearchClass Search { get; private set; }
        public SAINEnemy Enemy => HasEnemy ? EnemyController.ActiveEnemy : null;
        public SAINPersonTransformClass Transform => Person.Transform;
        public SAINMemoryClass Memory { get; private set; }
        public SAINEnemyController EnemyController { get; private set; }
        public SAINNoBushESP NoBushESP { get; private set; }
        public SAINFriendlyFireClass FriendlyFireClass { get; private set; }
        public SAINVisionClass Vision { get; private set; }
        public SAINBotEquipmentClass Equipment { get; private set; }
        public SAINMoverClass Mover { get; private set; }
        public SAINBotUnstuckClass BotStuck { get; private set; }
        public SAINFlashLightComponent FlashLight { get; private set; }
        public SAINHearingSensorClass Hearing { get; private set; }
        public SAINBotTalkClass Talk { get; private set; }
        public SAINDecisionClass Decision { get; private set; }
        public SAINCoverClass Cover { get; private set; }
        public SAINBotInfoClass Info { get; private set; }
        public SAINSquadClass Squad { get; private set; }
        public SAINSelfActionClass SelfActions { get; private set; }
        public SAINBotGrenadeClass Grenade { get; private set; }
        public SAINSteeringClass Steering { get; private set; }

        public bool IsDead => 
            BotOwner == null 
            || BotOwner.IsDead == true 
            || Player == null 
            || Player.HealthController.IsAlive == false;

        public bool BotActive => 
            IsDead == false 
            && BotOwner.enabled 
            && Player.enabled && BotOwner.BotState == EBotState.Active 
            && BotOwner.StandBy.StandByType == BotStandByType.active 
            && BotOwner.isActiveAndEnabled 
            && Player.isActiveAndEnabled 
            && isActiveAndEnabled;

        public bool GameIsEnding => 
            Singleton<IBotGame>.Instance == null 
            || Singleton<IBotGame>.Instance.Status == GameStatus.Stopping;

        public Vector3 Position => Person.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public BotOwner BotOwner => Person?.BotOwner;
        public string ProfileId => Person?.ProfileId;
        public Player Player => Person?.Player;
        public bool HasEnemy => EnemyController.HasEnemy;
    }
}