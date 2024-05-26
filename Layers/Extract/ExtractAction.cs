using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using Systems.Effects;
using EFT.Interactive;
using System.Linq;
using SAIN.Components.BotController;
using UnityEngine.AI;
using SAIN.Helpers;
using SAIN.Components;

namespace SAIN.Layers
{
    internal class ExtractAction : SAINAction
    {
        public static float MinDistanceToStartExtract { get; } = 6f;

        private static readonly string Name = typeof(ExtractAction).Name;
        public ExtractAction(BotOwner bot) : base(bot, Name)
        {
        }

        private Vector3? Exfil => SAINBot.Memory.Extract.ExfilPosition;

        public override void Start()
        {
            SAINBot.Extracting = true;
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            SAINBot.Extracting = false;
            BotOwner.PatrollingData.Unpause();
            BotOwner.Mover.MovementResume();
        }

        public override void Update()
        {
            if (SAINBot?.Player == null) return;

            float stamina = SAINBot.Player.Physical.Stamina.NormalValue;
            bool fightingEnemy = isFightingEnemy();
            // Environment id of 0 means a bot is outside.
            if (SAINBot.Player.AIData.EnvironmentId != 0)
            {
                shallSprint = false;
            }
            else if (fightingEnemy)
            {
                shallSprint = false;
            }
            else if (stamina > 0.75f)
            {
                shallSprint = true;
            }
            else if (stamina < 0.2f)
            {
                shallSprint = false;
            }

            if (!BotOwner.GetPlayer.MovementContext.CanSprint)
            {
                shallSprint = false;
            }

            if (!Exfil.HasValue)
            {
                return;
            }

            Vector3 point = Exfil.Value;
            float distance = (point - BotOwner.Position).sqrMagnitude;

            if (distance < 8f)
            {
                shallSprint = false;
            }

            if (ExtractStarted)
            {
                SAINBot.Memory.Extract.ExtractStatus = EExtractStatus.ExtractingNow;
                StartExtract(point);
                SAINBot.Mover.SetTargetPose(0f);
                SAINBot.Mover.SetTargetMoveSpeed(0f);
                if (_sayExitLocatedTime < Time.time)
                {
                    _sayExitLocatedTime = Time.time + 10;
                    SAINBot.Talk.GroupSay(EPhraseTrigger.ExitLocated, null, true, 70);
                }

            }
            else
            {
                if (fightingEnemy)
                {
                    SAINBot.Memory.Extract.ExtractStatus = EExtractStatus.Fighting;
                }
                else
                {
                    SAINBot.Memory.Extract.ExtractStatus = EExtractStatus.MovingTo;
                }
                MoveToExtract(distance, point);
                SAINBot.Mover.SetTargetPose(1f);
                SAINBot.Mover.SetTargetMoveSpeed(1f);
            }

            if (BotOwner.BotState == EBotState.Active)
            {
                SAINBot.Steering.SteerByPriority();
                Shoot.Update();
            }
        }

        private float _sayExitLocatedTime;

        private bool isFightingEnemy()
        {
            return SAINBot.Enemy != null 
                && SAINBot.Enemy.Seen 
                && (SAINBot.Enemy.Path.PathDistance < 50f || SAINBot.Enemy.InLineOfSight);
        }

        private bool shallSprint;

        private void MoveToExtract(float distance, Vector3 point)
        {
            if (BotOwner.Mover == null)
            {
                return;
            }

            SAINBot.Mover.Sprint(shallSprint);

            if (distance > MinDistanceToStartExtract * 2)
            {
                ExtractStarted = false;
            }
            if (distance < MinDistanceToStartExtract)
            {
                ExtractStarted = true;
            }

            if (ExtractStarted)
            {
                return;
            }

            if (ReCalcPathTimer < Time.time)
            {
                ExtractTimer = -1f;
                ReCalcPathTimer = Time.time + 4f;

                SAINBot.Memory.Extract.ExtractStatus = EExtractStatus.MovingTo;
                NavMeshPathStatus pathStatus = BotOwner.Mover.GoToPoint(point, true, 0.5f, false, false);
                var pathController = HelpersGClass.GetPathControllerClass(BotOwner.Mover);
                if (pathController?.CurPath != null)
                {
                    float distanceToEndOfPath = Vector3.Distance(BotOwner.Position, pathController.CurPath.LastCorner());
                    bool reachedEndOfIncompletePath = (pathStatus == NavMeshPathStatus.PathPartial) && (distanceToEndOfPath < BotExtractManager.MinDistanceToExtract);

                    // If the path to the extract is invalid or the path is incomplete and the bot reached the end of it, select a new extract
                    if ((pathStatus == NavMeshPathStatus.PathInvalid) || reachedEndOfIncompletePath)
                    {
                        // Need to reset the search timer to prevent the bot from immediately selecting (possibly) the same extract
                        BotController.BotExtractManager.ResetExfilSearchTime(SAINBot);

                        SAINBot.Memory.Extract.ExfilPoint = null;
                        SAINBot.Memory.Extract.ExfilPosition = null;
                    }
                }
            }
        }

        private void StartExtract(Vector3 point)
        {
            SAINBot.Memory.Extract.ExtractStatus = EExtractStatus.ExtractingNow;
            if (ExtractTimer == -1f)
            {
                ExtractTimer = BotController.BotExtractManager.GetExfilTime(SAINBot.Memory.Extract.ExfilPoint);
                
                // Needed to get car extracts working
                activateExfil(SAINBot.Memory.Extract.ExfilPoint);

                float timeRemaining = ExtractTimer - Time.time;
                Logger.LogInfo($"{BotOwner.name} Starting Extract Timer of {timeRemaining}");
                BotOwner.Mover.MovementPause(timeRemaining);
            }

            if (ExtractTimer < Time.time)
            {
                Logger.LogInfo($"{BotOwner.name} Extracted at {point} for extract {SAINBot.Memory.Extract.ExfilPoint.Settings.Name} at {System.DateTime.UtcNow}");
                SAINBotController.Instance?.BotExtractManager?.LogExtractionOfBot(BotOwner, point, SAINBot.Memory.Extract.ExtractReason.ToString(), SAINBot.Memory.Extract.ExfilPoint);

                var botgame = Singleton<IBotGame>.Instance;
                Player player = SAINBot.Player;
                Singleton<Effects>.Instance.EffectsCommutator.StopBleedingForPlayer(player);
                BotOwner.Deactivate();
                BotOwner.Dispose();
                botgame.BotsController.BotDied(BotOwner);
                botgame.BotsController.DestroyInfo(player);
                Object.DestroyImmediate(BotOwner.gameObject);
                Object.Destroy(BotOwner);
            }
        }

        private void activateExfil(ExfiltrationPoint exfil)
        {
            // Needed to start the car extract
            exfil.OnItemTransferred(SAINBot.Player);

            // Copied from the end of ExfiltrationPoint.Proceed()
            if (exfil.Status == EExfiltrationStatus.UncompleteRequirements)
            {
                switch (exfil.Settings.ExfiltrationType)
                {
                    case EExfiltrationType.Individual:
                        exfil.SetStatusLogged(EExfiltrationStatus.RegularMode, "Proceed-3");
                        break;
                    case EExfiltrationType.SharedTimer:
                        exfil.SetStatusLogged(EExfiltrationStatus.Countdown, "Proceed-1");

                        if (SAINPlugin.DebugMode)
                        {
                            Logger.LogInfo($"bot {SAINBot.name} has started the VEX exfil");
                        }

                        break;
                    case EExfiltrationType.Manual:
                        exfil.SetStatusLogged(EExfiltrationStatus.AwaitsManualActivation, "Proceed-2");
                        break;
                }
            }
        }

        private bool ExtractStarted = false;
        private float ReCalcPathTimer = 0f;
        private float ExtractTimer = -1f;
    }
}