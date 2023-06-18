﻿using BepInEx.Logging;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.Classes;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;
using static SAIN.UserSettings.VisionConfig;

namespace SAIN.Components
{
    public class SAINComponent : MonoBehaviour
    {
        public List<Player> VisiblePlayers = new List<Player>();
        private void Awake()
        {
            BotOwner = GetComponent<BotOwner>();
            Init(BotOwner);
        }

        public List<Vector3> ExitsToLocation { get; private set; } = new List<Vector3>();

        public void UpdateExitsToLoc(Vector3[] exits)
        {
            ExitsToLocation.Clear();
            ExitsToLocation.AddRange(exits);
        }

        public string ProfileId => BotOwner.ProfileId;
        public string SquadId => Squad.SquadID;
        public Player Player => BotOwner.GetPlayer;

        private void Init(BotOwner bot)
        {
            BotColor = RandomColor;

            // Must be first, other classes use it
            Squad = new SquadClass(bot);
            Equipment = new BotEquipmentClass(bot);

            Info = new BotInfoClass(bot);
            AILimit = new AILimitClass(bot);
            BotStuck = new BotUnstuckClass(bot);
            Hearing = new HearingSensorClass(bot);
            Talk = new BotTalkClass(bot);
            Lean = bot.GetOrAddComponent<LeanComponent>();
            Decision = new DecisionClass(bot);
            Cover = new CoverClass(bot);
            FlashLight = bot.GetPlayer.gameObject.AddComponent<FlashLightComponent>();
            SelfActions = new SelfActionClass(bot);
            Steering = new SteeringClass(bot);
            Grenade = new BotGrenadeClass(bot);
            Mover = new MoverClass(bot);
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
        }

        private float DebugPathTimer = 0f;
        public bool NoBushESPActive { get; private set; }

        private void Update()
        {
            UpdatePatrolData();

            if (BotActive && !GameIsEnding)
            {
                if (VisiblePlayers.Count > 0 && DebugVision.Value)
                {
                    foreach (var player in VisiblePlayers)
                    {
                        DebugGizmos.SingleObjects.Line(HeadPosition, player.MainParts[BodyPartType.body].Position, Color.blue, 0.025f, true, 0.1f, true);
                    }
                }

                NoBushESPActive = NoBushESP(Enemy, HeadPosition);
                if (NoBushESPActive)
                {
                    BotOwner.ShootData?.EndShoot();
                    BotOwner.AimingData?.LoseTarget();
                }

                UpdateHealth();
                UpdateEnemy();
                Equipment.Update();
                Grenade.Update();
                Mover.Update();
                Squad.Update();
                Info.Update();
                BotStuck.Update();
                Decision.Update();
                Cover.Update();
                Talk.Update();
                SelfActions.Update();
                Steering.Update();
            }
        }

        private void UpdatePatrolData()
        {
            if (CheckActiveLayer)
            {
                BotOwner.PatrollingData.Pause();
            }
            else
            {
                if (Enemy == null)
                {
                    BotOwner.PatrollingData.Unpause();
                }
            }
        }

        public bool SAINLayersActive => BigBrainSAIN.IsBotUsingSAINLayer(BotOwner);
        public bool CheckActiveLayer
        {
            get
            {
                if (RecheckTimer < Time.time)
                {
                    if (SAINLayersActive)
                    {
                        RecheckTimer = Time.time + 1f;
                        Active = true;
                    }
                    else
                    {
                        RecheckTimer = Time.time + 0.05f;
                        Active = false;
                    }
                }
                return Active;
            }
        }

        private bool Active;
        private float RecheckTimer = 0f;
        private bool PatrolPaused { get; set; }
        private float DebugPatrolTimer = 0f;

        private void UpdateEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null)
            {
                SAINEnemy sainEnemy;
                string profile = goalEnemy.Person.ProfileId;
                if (Enemies.ContainsKey(profile))
                {
                    sainEnemy = Enemies[profile];
                }
                else
                {
                    sainEnemy = new SAINEnemy(BotOwner, goalEnemy.Person, DifficultyModifier);
                    Enemies.Add(profile, sainEnemy);
                }
                Enemy = sainEnemy;
                Enemy?.Update();
            }
            else
            {
                Enemy = null;
                Enemies.Clear();
            }
        }

        private void UpdateHealth()
        {
            if (UpdateHealthTimer < Time.time)
            {
                UpdateHealthTimer = Time.time + 0.25f;
                HealthStatus = BotOwner.GetPlayer.HealthStatus;
            }
        }

        public FriendlyFireStatus FriendlyFireStatus { get; private set; }

        private float UpdateHealthTimer = 0f;
        public float DifficultyModifier => Info.DifficultyModifier;

        public bool HasEnemy => Enemy != null;

        public SAINEnemy Enemy { get; private set; }
        public Dictionary<string, SAINEnemy> Enemies { get; private set; } = new Dictionary<string, SAINEnemy>();

        public void Dispose()
        {
            StopAllCoroutines();

            Cover?.Dispose();
            Hearing?.Dispose();
            Lean?.Dispose();
            Cover?.CoverFinder?.Dispose();
            FlashLight?.Dispose();

            Destroy(this);
        }

        public bool NoDecisions => CurrentDecision == SAINSoloDecision.None && Decision.SquadDecision == SAINSquadDecision.None && Decision.SelfDecision == SAINSelfDecision.None;
        public SAINSoloDecision CurrentDecision => Decision.MainDecision;
        public float DistanceToMainPlayer => (SAINBotController.MainPlayerPosition - BotOwner.Position).magnitude;
        public Vector3 Position => BotOwner.Position;
        public Vector3 WeaponRoot => BotOwner.WeaponRoot.position;
        public Vector3 HeadPosition => BotOwner.LookSensor._headPoint;
        public Vector3 BodyPosition => BotOwner.MainParts[BodyPartType.body].Position;

        public static bool NoBushESP(SAINEnemy enemy, Vector3 botHead)
        {
            if (enemy != null && enemy.IsVisible)
            {
                if (enemy.Player.IsYourPlayer)
                {
                    Vector3 direction = enemy.EnemyChestPosition - botHead;
                    if (Physics.Raycast(botHead, direction, out var hitInfo, direction.magnitude, NoBushMask))
                    {
                        string ObjectName = hitInfo.transform.parent?.gameObject?.name;
                        foreach (string exclusion in ExclusionList)
                        {
                            if (ObjectName.ToLower().Contains(exclusion))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private static LayerMask NoBushMask => LayerMaskClass.HighPolyWithTerrainMaskAI;
        public static List<string> ExclusionList = new List<string> { "filbert", "fibert", "tree", "pine", "plant", "birch",
        "timber", "spruce", "bush", "wood"};


        public Vector3? CurrentTargetPosition
        {
            get
            {
                if (Enemy != null)
                {
                    return Enemy.Position;
                }
                if (Time.time - BotOwner.Memory.LastTimeHit < 120f && !BotOwner.Memory.IsPeace)
                {
                    return BotOwner.Memory.LastHitPos;
                }
                var sound = BotOwner.BotsGroup.YoungestPlace(BotOwner, 500f, true);
                if (sound != null && !sound.IsCome)
                {
                    return sound.Position;
                }
                var Target = BotOwner.Memory.GoalTarget?.GoalTarget;
                if (Target != null)
                {
                    return Target.Position;
                }
                return null;
            }
        }

        public bool HasGoalTarget => BotOwner.Memory.GoalTarget?.GoalTarget != null;
        public bool HasGoalEnemy => BotOwner.Memory.GoalEnemy != null;
        public Vector3? GoalTargetPos => BotOwner.Memory.GoalTarget?.GoalTarget?.Position;
        public Vector3? GoalEnemyPos => BotOwner.Memory.GoalEnemy?.CurrPosition;

        public bool BotHasStamina => BotOwner.GetPlayer.Physical.Stamina.NormalValue > 0f;

        public Vector3 UnderFireFromPosition { get; set; }

        public bool HasEnemyAndCanShoot => Enemy?.IsVisible == true;

        public BotEquipmentClass Equipment { get; private set; }

        public MoverClass Mover { get; private set; }

        public AILimitClass AILimit { get; private set; }

        public BotUnstuckClass BotStuck { get; private set; }

        public FlashLightComponent FlashLight { get; private set; }

        public HearingSensorClass Hearing { get; private set; }

        public BotTalkClass Talk { get; private set; }

        public DecisionClass Decision { get; private set; }

        public CoverClass Cover { get; private set; }

        public BotInfoClass Info { get; private set; }

        public SquadClass Squad { get; private set; }

        public SelfActionClass SelfActions { get; private set; }

        public LeanComponent Lean { get; private set; }

        public BotGrenadeClass Grenade { get; private set; }

        public SteeringClass Steering { get; private set; }

        public bool IsDead => BotOwner?.IsDead == true;
        public bool BotActive => BotOwner.BotState == EBotState.Active && !IsDead && BotOwner?.GetPlayer?.enabled == true;
        public bool GameIsEnding => GameHasEnded || Singleton<IBotGame>.Instance?.Status == GameStatus.Stopping;
        public bool GameHasEnded => Singleton<IBotGame>.Instance == null;

        public bool Healthy => HealthStatus == ETagStatus.Healthy;
        public bool Injured => HealthStatus == ETagStatus.Injured;
        public bool BadlyInjured => HealthStatus == ETagStatus.BadlyInjured;
        public bool Dying => HealthStatus == ETagStatus.Dying;

        public ETagStatus HealthStatus { get; private set; }

        public LastHeardSound LastHeardSound => Hearing.LastHeardSound;

        public Color BotColor { get; private set; }

        public BotOwner BotOwner { get; private set; }

        private static Color RandomColor => new Color(Random.value, Random.value, Random.value);

        private ManualLogSource Logger;
    }
}