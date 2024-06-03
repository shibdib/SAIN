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
    public enum ESAINLayer
    {
        None = 0,
        Combat = 1,
        Squad = 2,
        Extract = 3,
        Run = 4,
        AvoidThreat = 5,
    }

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

            GameWorld.OnDispose += Dispose;

            Person = person;
            BotOwner = person.BotOwner;
            ProfileId = person.ProfileId;
            Player = person.Player;

            Player.OnPlayerDeadOrUnspawn += botKilled;

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
                CurrentTarget = new CurrentTargetClass(this);
                ManualShoot = new ManualShootClass(this);

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

            if (!verifyBrain(person))
            {
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

        private bool verifyBrain(SAINPersonClass person)
        {
            if (Info.Profile.IsPMC &&
                person.BotOwner.Brain.BaseBrain.ShortName() != Brain.PMC.ToString())
            {
                Logger.LogAndNotifyError($"{BotOwner.name} is a PMC but does not have [PMC] Base Brain! Current Brain Assignment: [{person.BotOwner.Brain.BaseBrain.ShortName()}] : SAIN Server mod is either missing or another mod is overwriting it. Destroying SAIN for this bot...");
                return false;
            }
            return true;
        }


        private void OnDisable()
        {
            Decision.ResetDecisions(false);
            StopAllCoroutines();
        }

        public bool IsSpeedHacker { get; private set; }

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }

        public bool SAINLayersActive => ActiveLayer != ESAINLayer.None;

        public ESAINLayer ActiveLayer { get; set; }


        private void Update()
        {
            if (Player == null || BotOwner == null || IsDead || BotOwner.BotState == EBotState.Disposed)
            {
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
            CurrentTarget.Update();
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
            ManualShoot.Update();

            handleDumbShit();
        }

        private void handleDumbShit()
        {
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

        public SAINDoorOpener DoorOpener { get; private set; }
        public bool PatrolDataPaused { get; private set; }
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

        public float DistanceToAimTarget
        {
            get
            {
                if (BotOwner.AimingData != null)
                {
                    return BotOwner.AimingData.LastDist2Target;
                }
                return CurrentTarget.CurrentTargetDistance;
            }
        }

        private void botKilled(Player player)
        {
            if (player != null)
            {
                player.OnPlayerDeadOrUnspawn -= botKilled;
            }
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                //Logger.LogWarning($"SAIN Disposed");

                GameWorld.OnDispose -= Dispose;
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

        public Vector3? CurrentTargetPosition => CurrentTarget.CurrentTargetPosition;

        public Vector3? CurrentTargetDirection => CurrentTarget.CurrentTargetDirection;

        public float CurrentTargetDistance => CurrentTarget.CurrentTargetDistance;

        public ManualShootClass ManualShoot { get; private set; }
        public CurrentTargetClass CurrentTarget { get; private set; }
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
        public bool HasEnemy => EnemyController.HasEnemy;
        public bool HasLastEnemy => EnemyController.HasLastEnemy;
        public BotOwner BotOwner { get; private set; }
        public string ProfileId { get; private set; }
        public Player Player { get; private set; }
    }
}