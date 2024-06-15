using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using UnityEngine.AI;
using SAIN.Helpers;

namespace SAIN.Layers.Combat.Run
{
    internal class RunningAction : SAINAction
    {
        public RunningAction(BotOwner bot) : base(bot, nameof(RunningAction))
        {
        }

        public override void Update()
        {
            SAINBot.Mover.SetTargetPose(1f);
            SAINBot.Mover.SetTargetMoveSpeed(1f);

            if (nextRandomRunTime > Time.time && (_runDestination - SAINBot.Position).sqrMagnitude < 1f)
            {
                nextRandomRunTime = 0f;
            }

            if (!SAINBot.Mover.SprintController.Running
                && findRandomPlace(out var path) 
                && SAINBot.Mover.SprintController.RunToPoint(_runDestination, SAINComponent.Classes.Mover.ESprintUrgency.High))
            {
                nextRandomRunTime = Time.time + 20f;
            }

        }

        private Vector3 _runDestination;
        private float nextRandomRunTime;


        public override void Start()
        {
            
        }

        private bool findRandomPlace(out NavMeshPath path)
        {
            for (int i = 0; i < 10;  i++)
            {
                Vector3 random = UnityEngine.Random.onUnitSphere * 100f;
                if (NavMesh.SamplePosition(random + SAINBot.Position, out var hit, 10f, -1))
                {
                    path = new NavMeshPath();
                    if (NavMesh.CalculatePath(SAINBot.Position, hit.position, -1, path))
                    {
                        _runDestination = path.corners[path.corners.Length - 1];
                        return true;
                    }
                }
            }
            path = null;
            return false;
        }

        public override void Stop()
        {
            SAINBot.Mover.SprintController.CancelRun();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run Info");
            var cover = SAINBot.Cover;
            stringBuilder.AppendLabeledValue("Run State", $"{SAINBot.Mover.SprintController.CurrentRunStatus}", Color.white, Color.yellow, true);
        }
    }
}