using Comfort.Common;
using EFT;
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
using System;
using UnityEngine;

namespace SAIN.SAINComponent
{
    public class BotComponent : MonoBehaviour, IBotComponent
    {
        public static bool TryAddBotComponent(BotOwner botOwner, out BotComponent sainComponent)
        {
            GameObject gameObject = botOwner?.gameObject;
            if (gameObject != null)
            {
                // If Somehow this bot already has SAIN attached, destroy it.
                if (gameObject.TryGetComponent(out sainComponent))
                {
                    sainComponent.Dispose();
                }

                PlayerComponent playerComponent = GameWorldHandler.SAINGameWorld.PlayerTracker.GetPlayerComponent(botOwner.ProfileId);
                if (playerComponent != null)
                {
                    playerComponent.InitBot(botOwner);
                    // Create a new Component
                    sainComponent = gameObject.AddComponent<BotComponent>();
                    if (sainComponent?.Init(playerComponent) == true)
                    {
                        return true;
                    }
                }
            }
            sainComponent = null;
            return false;
        }

        public PlayerComponent PlayerComponent { get; private set; }
        public bool BotActive { get; private set; }
        public string ProfileId { get; private set; }

        public Vector3? CurrentTargetPosition => CurrentTarget.CurrentTargetPosition;
        public Vector3? CurrentTargetDirection => CurrentTarget.CurrentTargetDirection;
        public float CurrentTargetDistance => CurrentTarget.CurrentTargetDistance;
        public Vector3 Position => Person.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public bool HasEnemy => EnemyController.HasEnemy;
        public bool HasLastEnemy => EnemyController.HasLastEnemy;
        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;
        public BotOwner BotOwner => PlayerComponent.BotOwner;
        public Player Player => PlayerComponent.Player;
        public SAINPersonClass Person => PlayerComponent.Person;
        public SAINEnemy Enemy => HasEnemy ? EnemyController.ActiveEnemy : null;
        public SAINEnemy LastEnemy => HasLastEnemy ? EnemyController.LastEnemy : null;
        public SAINPersonTransformClass Transform => Person.Transform;

        public SAINMedical Medical { get; private set; }
        public SAINActivationClass SAINActivation { get; private set; }
        public SAINDoorOpener DoorOpener { get; private set; }
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
        public SAINMemoryClass Memory { get; private set; }
        public SAINEnemyController EnemyController { get; private set; }
        public SAINNoBushESP NoBushESP { get; private set; }
        public SAINFriendlyFireClass FriendlyFireClass { get; private set; }
        public SAINVisionClass Vision { get; private set; }
        public SAINBotEquipmentClass Equipment { get; private set; }
        public SAINMoverClass Mover { get; private set; }
        public SAINBotUnstuckClass BotStuck { get; private set; }
        public FlashLightComponent FlashLight { get; private set; }
        public SAINHearingSensorClass Hearing { get; private set; }
        public SAINBotTalkClass Talk { get; private set; }
        public SAINDecisionClass Decision { get; private set; }
        public SAINCoverClass Cover { get; private set; }
        public SAINBotInfoClass Info { get; private set; }
        public SAINSquadClass Squad { get; private set; }
        public SAINSelfActionClass SelfActions { get; private set; }
        public SAINBotGrenadeClass Grenade { get; private set; }
        public SAINSteeringClass Steering { get; private set; }

        public Action<string, BotOwner> OnSAINDisposed { get; set; }

        public bool IsDead =>
            PlayerComponent == null ||
            BotOwner == null
            || BotOwner.IsDead == true
            || Player == null
            || Player.HealthController.IsAlive == false;


        public bool GameIsEnding =>
            Singleton<IBotGame>.Instance == null
            || Singleton<IBotGame>.Instance.Status == GameStatus.Stopping;

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

        public float NextCheckVisiblePlayerTime;

        public bool Init(PlayerComponent playerComponent)
        {
            PlayerComponent = playerComponent;
            ProfileId = playerComponent.ProfileId;

            try
            {
                NoBushESP = this.GetOrAddComponent<SAINNoBushESP>();
                NoBushESP.Init(playerComponent.BotOwner, this);
                FlashLight = playerComponent.Player.GetOrAddComponent<FlashLightComponent>();

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
                DoorOpener = new SAINDoorOpener(this, playerComponent.BotOwner);
                Medical = new SAINMedical(this);
                BotLight = new BotLightController(this);
                BackpackDropper = new BotBackpackDropClass(this);
                CurrentTarget = new CurrentTargetClass(this);
                ManualShoot = new ManualShootClass(this);
                SAINActivation = new SAINActivationClass(this);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error When Creating Classes, Disposing...");
                Logger.LogError(ex);
                Dispose();
                return false;
            }

            try
            {
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
            }
            catch (Exception ex)
            {
                Logger.LogError("Error When Initializing Classes, Disposing...");
                Logger.LogError(ex);
                Dispose();
                return false;
            }

            try
            {
                if (!verifyBrain(Person))
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

                BotOwner.OnBotStateChange += resetBot;
                GameWorld.OnDispose += Dispose;
                Player.OnPlayerDeadOrUnspawn += botKilled;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error When Finishing Bot Initialization, Disposing...");
                Logger.LogError(ex);
                Dispose();
                return false;
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
            BotActive = false;
            StopAllCoroutines();
            Decision?.ResetDecisions(false);
            Cover?.ActivateCoverFinder(false);
        }

        public bool IsSpeedHacker { get; private set; }
        public bool SAINLayersActive => SAINActivation.SAINLayersActive;

        public ESAINLayer ActiveLayer
        {
            get
            {
                return SAINActivation.ActiveLayer;
            }
            set
            {
                SAINActivation.SetActiveLayer(value);
            }
        }

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }


        private void Update()
        {
            if (BotActive)
            {
                EnemyController.Update();
                AILimit.Update();
                CurrentTarget.Update();
                Decision.Update();
                Info.Update();
                SAINActivation.Update();

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
        }

        private void LateUpdate()
        {
            if (IsDead || BotOwner.BotState == EBotState.Disposed)
            {
                Dispose();
                return;
            }

            BotActive =
                !GameIsEnding &&
                BotOwner.isActiveAndEnabled &&
                Player.gameObject.activeInHierarchy &&
                BotOwner.gameObject.activeInHierarchy &&
                BotOwner.BotState == EBotState.Active &&
                BotOwner.StandBy.StandByType == BotStandByType.active;

            if (!BotActive)
            {
                OnDisable();
            }
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
            OnDisable();

            try
            {
                Search?.Dispose();
                Memory?.Dispose();
                EnemyController?.Dispose();
                FriendlyFireClass?.Dispose();
                Vision?.Dispose();
                Equipment?.Dispose();
                Mover?.Dispose();
                BotStuck?.Dispose();
                Hearing?.Dispose();
                Talk?.Dispose();
                Decision?.Dispose();
                Cover?.Dispose();
                Info?.Dispose();
                Squad?.Dispose();
                SelfActions?.Dispose();
                Grenade?.Dispose();
                Steering?.Dispose();
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
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Classes Error: {ex}");
            }

            try
            {
                GameObject.Destroy(NoBushESP);
                GameObject.Destroy(FlashLight);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Components Error: {ex}");
            }

            if (BotOwner != null)
            {
                BotOwner.OnBotStateChange -= resetBot;
            }
            GameWorld.OnDispose -= Dispose;
            OnSAINDisposed?.Invoke(ProfileId, BotOwner);

            Destroy(this);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

    }
}