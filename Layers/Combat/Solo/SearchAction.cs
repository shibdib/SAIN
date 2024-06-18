using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Search;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo
{
    internal class SearchAction : SAINAction
    {
        public SearchAction(BotOwner bot) : base(bot, nameof(SearchAction))
        {
        }

        public override void Start()
        {
        }

        private Vector3 TargetPosition => Search.FinalDestination;

        public override void Stop()
        {
            BotOwner.Mover.MovementResume();
            Search.Reset();
            HaveTalked = false;
        }

        private float CheckMagTimer;
        private float CheckChamberTimer;
        private float NextCheckTimer;
        private float ReloadTimer;

        public override void Update()
        {
            // Scavs will speak out and be more vocal
            if (!HaveTalked &&
                Bot.Info.Profile.IsScav &&
                (BotOwner.Position - TargetPosition).sqrMagnitude < 50f * 50f)
            {
                HaveTalked = true;
                if (EFTMath.RandomBool(60))
                {
                    Bot.Talk.Say(EPhraseTrigger.OnMutter, ETagStatus.Aware, true);
                }
            }

            bool isBeingStealthy = Bot.Enemy?.EnemyHeardFromPeace == true;
            if (isBeingStealthy)
            {
                SprintEnabled = false;
            }
            else
            {
                CheckShouldSprint();
            }

            Search.Search(SprintEnabled);
            Steer();

            if (!SprintEnabled)
            {
                Shoot.Update();
                if (!isBeingStealthy)
                    CheckWeapon();
            }
        }

        private void CheckWeapon()
        {
            if (Bot.Enemy != null && Bot.Enemy.TimeSinceLastKnownUpdated > 10f)
            {
                if (ReloadTimer < Time.time && Bot.Decision.SelfActionDecisions.LowOnAmmo(0.7f))
                {
                    ReloadTimer = Time.time + 3f * Random.Range(0.5f, 1.5f);
                    Bot.SelfActions.TryReload();
                }
                else if (CheckMagTimer < Time.time && NextCheckTimer < Time.time)
                {
                    NextCheckTimer = Time.time + 3f * Random.Range(0.5f, 1.5f);
                    if (EFTMath.RandomBool())
                    {
                        Bot.Player.HandsController.FirearmsAnimator.CheckAmmo();
                        CheckMagTimer = Time.time + 240f * Random.Range(0.5f, 1.5f);
                    }
                }
                else if (CheckChamberTimer < Time.time && NextCheckTimer < Time.time)
                {
                    NextCheckTimer = Time.time + 3f * Random.Range(0.5f, 1.5f);
                    if (EFTMath.RandomBool())
                    {
                        Bot.Player.HandsController.FirearmsAnimator.CheckChamber();
                        CheckChamberTimer = Time.time + 240f * Random.Range(0.5f, 1.5f);
                    }
                }
            }
        }

        private bool HaveTalked = false;

        private SAINSearchClass Search => Bot.Search;

        private void CheckShouldSprint()
        {
            if (Search.CurrentState == ESearchMove.MoveToEndPeek || Search.CurrentState == ESearchMove.Wait || Search.CurrentState == ESearchMove.MoveToDangerPoint)
            {
                SprintEnabled = false;
                return;
            }

            if (Bot.Enemy?.IsVisible == true || Bot.Enemy?.InLineOfSight == true)
            {
                SprintEnabled = false;
                return;
            }

            if (Bot.Decision.CurrentSquadDecision == SquadDecision.Help)
            {
                SprintEnabled = true;
                return;
            }

            var persSettings = Bot.Info.PersonalitySettings;
            float chance = persSettings.Search.SprintWhileSearchChance;
            if (RandomSprintTimer < Time.time && chance > 0)
            {
                float myPower = Bot.Info.PowerLevel;
                if (Bot.Enemy?.EnemyPlayer != null && Bot.Enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f)
                {
                    chance = 100f;
                }

                SprintEnabled = EFTMath.RandomBool(chance);
                float timeAdd;
                if (SprintEnabled)
                {
                    timeAdd = 2f * Random.Range(0.75f, 4.00f);
                }
                else
                {
                    timeAdd = 4f * Random.Range(0.5f, 1.5f);
                }
                RandomSprintTimer = Time.time + timeAdd;
            }
        }

        private bool SprintEnabled = false;
        private float RandomSprintTimer = 0f;

        private void Steer()
        {
            if (!SteerByPriority(false)
                && !Bot.Steering.LookToLastKnownEnemyPosition(Bot.Enemy))
            {
                LookToMovingDirection();
            }
        }

        private bool CanSeeDangerOrCorner(out Vector3 point)
        {
            point = Vector3.zero;

            if (Search.SearchMovePoint == null || Search.CurrentState == ESearchMove.MoveToDangerPoint)
            {
                LookPoint = Vector3.zero;
                return false;
            }

            if (CheckSeeTimer < Time.time)
            {
                LookPoint = Vector3.zero;
                CheckSeeTimer = Time.time + 0.5f;
                var headPosition = Bot.Transform.HeadPosition;

                var canSeePoint = !Vector.Raycast(headPosition,
                    Search.SearchMovePoint.DangerPoint,
                    LayerMaskClass.HighPolyWithTerrainMaskAI);

                if (canSeePoint)
                {
                    LookPoint = Search.SearchMovePoint.DangerPoint + Vector3.up;
                }
                else
                {
                    canSeePoint = !Vector.Raycast(headPosition,
                        Search.SearchMovePoint.Corner,
                        LayerMaskClass.HighPolyWithTerrainMaskAI);
                    if (canSeePoint)
                    {
                        LookPoint = Search.SearchMovePoint.Corner + Vector3.up;
                    }
                }

                if (LookPoint != Vector3.zero)
                {
                    //LookPoint.y = 0;
                    //LookPoint += headPosition;
                }
            }

            point = LookPoint;
            return point != Vector3.zero;
        }

        private Vector3 LookPoint;
        private float CheckSeeTimer;

        private bool SteerByPriority(bool value) => Bot.Steering.SteerByPriority(value);

        private void LookToMovingDirection() => Bot.Steering.LookToMovingDirection();

        public NavMeshPath Path = new NavMeshPath();
    }
}