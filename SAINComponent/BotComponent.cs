using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.BaseClasses;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Debug;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent.Classes.Memory;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes.Search;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.SubComponents;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SAIN.SAINComponent
{
    public class BotComponent : MonoBehaviour, IBotComponent
    {
        public static bool TryAddBotComponent(BotOwner botOwner, out BotComponent sainComponent)
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
                sainComponent = gameObject.AddComponent<BotComponent>();
                if (sainComponent?.Init(new SAINPersonClass(player)) == true)
                {
                    return true;
                }
            }
            sainComponent = null;
            return false;
        }

        public Action<string, BotOwner> OnSAINDisposed { get; set; }
        public SAINPersonClass Person { get; private set; }
        public SAINMedical Medical { get; private set; }

        public float NextCheckVisiblePlayerTime;

        public bool Init(SAINPersonClass person)
        {
            if (person == null)
            {
                Logger.LogAndNotifyError("Person is Null in SAINComponent Init");
                return false;
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
                BotLight = new BotLightController(this);
                BackpackDropper = new BotBackpackDropClass(this);

                BotOwner.OnBotStateChange += resetBot;
            }
            catch (Exception ex)
            {
                Logger.LogError("Init SAIN ERROR, Disposing...");
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
            BotLight.Init();
            BackpackDropper.Init();

            TimeBotCreated = Time.time;

            if (Info.Profile.IsPMC && 
                person.BotOwner.Brain.BaseBrain.ShortName() != Brain.PMC.ToString())
            {
                Logger.LogAndNotifyError($"{BotOwner.name} is a PMC but does not have [PMC] Base Brain! Current Brain Assignment: [{person.BotOwner.Brain.BaseBrain.ShortName()}] : SAIN Server mod is either missing or another mod is overwriting it. Destroying SAIN for this bot...");
                Logger.LogError("Init SAIN ERROR, Disposing...");
                Dispose();
                return false;
            }

            if (EFTMath.RandomBool(1) && 
                SAINPlugin.LoadedPreset.GlobalSettings.General.RandomSpeedHacker)
            {
                IsSpeedHacker = true;
            }

            return true;
        }

        private void OnDisable()
        {
            Decision.ResetDecisions(false);
        }

        public bool IsSpeedHacker { get; private set; }

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }

        public float TimeBotCreated { get; private set; }

        public bool SAINLayersActive => SAINSoloActive || SAINSquadActive || SAINAvoidActive || SAINExtractActive || SAINRunActive;

        public bool SAINSoloActive { get; set; }
        public bool SAINSquadActive { get; set; }
        public bool SAINAvoidActive { get; set; }
        public bool SAINExtractActive { get; set; }
        public bool SAINRunActive { get; set; }

        private void Update()
        {
            if (Player == null)
            {
                //Logger.LogWarning("Dispose SAIN Player == null");
                Dispose();
                return;
            }
            if (BotOwner == null)
            {
                //Logger.LogWarning("Dispose SAIN BotOwner == null");
                Dispose();
                return;
            }
            if (IsDead)
            {
                //Logger.LogWarning("Dispose SAIN IsDead");
                Dispose();
                return;
            }
            if (Singleton<GameWorld>.Instance == null)
            {
                //Logger.LogWarning("Dispose SAIN Singleton<GameWorld>.Instance == null");
                Dispose();
                return;
            }

            if (BotOwner.BotState == EBotState.Disposed)
            {
                Logger.LogWarning("Bot Disposed!");
                Dispose();
                return;
            }

            if (GameIsEnding || !BotActive)
            {
                StopAllCoroutines();
                Cover.ActivateCoverFinder(false);
                return;
            }

            handlePatrolData();
            EnemyController.Update();

            AILimit.UpdateAILimit();
            if (AILimit.LimitAIThisFrame)
            {
                //return;
            }

            Decision.Update();
            Info.Update();
            DoorOpener.Update();
            Search.Update();
            Memory.Update();
            FriendlyFireClass.Update();
            Vision.Update();
            Equipment.Update();
            Mover.Update();
            BotStuck.Update();
            Hearing.Update();
            Talk.Update();
            Cover.Update();
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
            BotLight.Update();

            UpdateGoalTarget();

            if (ManualShootReason != EShootReason.None && (!BotOwner.WeaponManager.HaveBullets || _timeStartManualShoot + 1f < Time.time))
            {
                Shoot(false, Vector3.zero);
            }

            if (IsSpeedHacker)
            {
                if (defaultMoveSpeed == 0)
                {
                    defaultMoveSpeed = Player.MovementContext.MaxSpeed;
                    defaultSprintSpeed = Player.MovementContext.SprintSpeed;
                }
                Player.Grounder.enabled = Enemy == null;
                if (Enemy != null)
                {
                    Player.MovementContext.SetCharacterMovementSpeed(350, true);
                    Player.MovementContext.SprintSpeed = 50f;
                    Player.ChangeSpeed(100f);
                    Player.UpdateSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    Player.MovementContext.ChangeSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    BotOwner.SetTargetMoveSpeed(100f);
                }
                else
                {
                    Player.MovementContext.SetCharacterMovementSpeed(defaultMoveSpeed, false);
                    Player.MovementContext.SprintSpeed = defaultSprintSpeed;
                }
            }
        }

        private float defaultMoveSpeed;
        private float defaultSprintSpeed;

        private float _nextCheckReloadTime;
        public SAINDoorOpener DoorOpener { get; private set; }
        public bool PatrolDataPaused { get; private set; }

        public bool IsHumanActiveEnemy
        {
            get
            {
                foreach (var enemy in EnemyController.ActiveEnemies)
                {
                    if (enemy != null && !enemy.IsAI)
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
            if (!SAINLayersActive && 
                PatrolDataPaused)
            {
                PatrolDataPaused = false;

                if (!BrainManager.IsCustomLayerActive(BotOwner))
                    BotOwner.PatrollingData?.Unpause();

                if (!_speedReset)
                {
                    _speedReset = true; 
                    resetSpeed();
                }
            }
            if (SAINLayersActive && 
                !PatrolDataPaused)
            {
                PatrolDataPaused = true;
                BotOwner.PatrollingData?.Pause();
                if (_speedReset)
                {
                    _speedReset = false;
                }
            }
        }

        private void resetSpeed()
        {
            BotOwner.SetTargetMoveSpeed(1f);
            BotOwner.Mover.SetPose(1f);
            Mover.SetTargetMoveSpeed(1f);
            Mover.SetTargetPose(1f);
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
                    BotOwner.ShootData.EndShoot();
                    ManualShootReason = EShootReason.None;
                    return false;
                }
            }
            BotOwner.ShootData.EndShoot();
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

        public void Dispose()
        {
            try
            {
                //Logger.LogWarning($"SAIN Disposed");

                OnSAINDisposed?.Invoke(ProfileId, BotOwner);

                StopAllCoroutines();

                if (BotOwner != null)
                {
                    BotOwner.OnBotStateChange -= resetBot;
                }

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
                BotLight?.Dispose();
                BackpackDropper?.Dispose();

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

        private void OnDestroy()
        {
            StopAllCoroutines();
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
                        DirectionToCurrentTarget = _currentTarget.Value - Position;
                        if (hadNoTarget)
                        {
                            TimeTargetFound = Time.time;
                        }
                        CurrentTargetDistance = (_currentTarget.Value - Position).magnitude;
                    }
                    else
                    {
                        DirectionToCurrentTarget = Vector3.zero;
                        CurrentTargetDistance = 0f;
                    }
                }
                return _currentTarget;
            }
        }

        public Vector3 DirectionToCurrentTarget { get; private set; }

        public float TimeTargetFound { get; private set; }
        public float TimeSinceTargetFound => Time.time - TimeTargetFound;

        public float CurrentTargetDistance { get; private set; }

        private float _nextGetTargetTime;
        private Vector3? _currentTarget;

        private Vector3? getTarget()
        {
            if (HasEnemy)
            {
                if (Enemy.IsVisible || (Enemy.Seen && Enemy.TimeSinceSeen < 1f))
                {
                    return Enemy.EnemyPosition;
                }
                var lastKnownPlace = Enemy.KnownPlaces.LastKnownPlace;
                if (lastKnownPlace != null)
                {
                    return lastKnownPlace.Position;
                }
                if (Enemy.KnownPlaces.LastSeenPlace != null)
                {
                    return Enemy.KnownPlaces.LastSeenPlace.Position;
                }
                if (Enemy.KnownPlaces.LastHeardPlace != null)
                {
                    return Enemy.KnownPlaces.LastHeardPlace.Position;
                }
                return Enemy.EnemyPosition;
            }
            return null;
        }

        
        public BotBackpackDropClass BackpackDropper { get; private set; }
        public BotLightController BotLight { get; private set; }
        public SAINBotSpaceAwareness SpaceAwareness { get; private set; }
        public SAINBotHitReaction BotHitReaction { get; private set; }
        public AimDownSightsController AimDownSightsController { get; private set; }
        public SAINAILimit AILimit { get; private set; }
        public SAINBotSuppressClass Suppression { get; private set; }
        public SAINVaultClass Vault { get; private set; }
        public SAINSearchClass Search { get; private set; }
        public SAINEnemy Enemy => HasEnemy ? EnemyController.ActiveEnemy : null;
        public SAINEnemy LastEnemy => HasLastEnemy ? EnemyController.LastEnemy : null;
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
            && BotOwner.isActiveAndEnabled
            && Player.gameObject.activeInHierarchy
            && BotOwner.gameObject.activeInHierarchy
            && BotOwner.BotState == EBotState.Active
            && BotOwner.StandBy.StandByType == BotStandByType.active;

        //&& BotOwner.StandBy.StandByType == BotStandByType.active
        //&& BotOwner.isActiveAndEnabled ;

        public bool GameIsEnding =>
            Singleton<IBotGame>.Instance == null
            || Singleton<IBotGame>.Instance.Status == GameStatus.Stopping;

        public Vector3 Position => Person.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public BotOwner BotOwner => Person?.BotOwner;
        public string ProfileId => Person?.ProfileId;
        public Player Player => Person?.Player;
        public bool HasEnemy => EnemyController.HasEnemy;
        public bool HasLastEnemy => EnemyController.HasLastEnemy;
    }
}