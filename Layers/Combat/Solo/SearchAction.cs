using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes;
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
            Search.Reset();
        }

        private float CheckMagTimer;
        private float CheckChamberTimer;
        private float NextCheckTimer;
        private float ReloadTimer;

        public override void Update()
        {
            tryTalk();
            CheckShouldSprint();
            Search.Search(SprintEnabled);
            Steer();

            if (!SprintEnabled)
            {
                Shoot.Update();
                CheckWeapon();
            }
        }

        private void CheckWeapon()
        {
            if (SAIN.Enemy != null)
            {
                if (SAIN.Enemy.Seen && SAIN.Enemy.TimeSinceSeen > 10f || !SAIN.Enemy.Seen && SAIN.Enemy.TimeSinceEnemyCreated > 10f)
                {
                    if (ReloadTimer < Time.time && SAIN.Decision.SelfActionDecisions.LowOnAmmo(0.5f))
                    {
                        ReloadTimer = Time.time + 3f * Random.Range(0.5f, 1.5f);
                        SAIN.SelfActions.TryReload();
                    }
                    else if (CheckMagTimer < Time.time && NextCheckTimer < Time.time)
                    {
                        NextCheckTimer = Time.time + 3f * Random.Range(0.5f, 1.5f);
                        if (EFTMath.RandomBool())
                        {
                            SAIN.Player.HandsController.FirearmsAnimator.CheckAmmo();
                            CheckMagTimer = Time.time + 240f * Random.Range(0.5f, 1.5f);
                        }
                    }
                    else if (CheckChamberTimer < Time.time && NextCheckTimer < Time.time)
                    {
                        NextCheckTimer = Time.time + 3f * Random.Range(0.5f, 1.5f);
                        if (EFTMath.RandomBool())
                        {
                            SAIN.Player.HandsController.FirearmsAnimator.CheckChamber();
                            CheckChamberTimer = Time.time + 240f * Random.Range(0.5f, 1.5f);
                        }
                    }
                }
            }
        }

        private bool HaveTalked = false;

        private void MoveToEnemy()
        {
            if (SAIN.Enemy == null)
            {
                float targetDistSqr = (BotOwner.Position - TargetPosition).sqrMagnitude;
                if (targetDistSqr < 2f)
                {
                    //SAIN.Decision.ResetDecisions();
                    //return;
                }

                // Scavs will speak out and be more vocal
                if (!HaveTalked 
                    && SAIN.Info.WildSpawnType == WildSpawnType.assault 
                    && targetDistSqr < 30f * 30f)
                {
                    HaveTalked = true;
                    if (EFTMath.RandomBool(40))
                    {
                        SAIN.Talk.Say(EPhraseTrigger.OnMutter, ETagStatus.Aware, true);
                    }
                }
            }
            if (SAIN.Enemy == null && (BotOwner.Position - TargetPosition).sqrMagnitude < 30f * 30f)
            {
                //SAIN.Decision.ResetDecisions();
                //return;
            }
            CheckShouldSprint();
            Search.Search(SprintEnabled);
            Steer();
        }

        private void tryTalk()
        {
            // Scavs will speak out and be more vocal
            if (!HaveTalked
                && SAIN.Info.WildSpawnType == WildSpawnType.assault
                && (BotOwner.Position - TargetPosition).sqrMagnitude < 30f * 30f)
            {
                HaveTalked = true;
                if (EFTMath.RandomBool(60))
                {
                    SAIN.Talk.Say(EPhraseTrigger.OnMutter, ETagStatus.Aware, true);
                }
            }
        }

        private SAINSearchClass Search => SAIN.Search;

        private void CheckShouldSprint()
        {
            if (Search.CurrentState == ESearchMove.MoveToEndPeak || Search.CurrentState == ESearchMove.Wait || Search.CurrentState == ESearchMove.MoveToDangerPoint || Search.CurrentState == ESearchMove.DirectMovePeek)
            {
                SprintEnabled = false;
                return;
            }

            var persSettings = SAIN.Info.PersonalitySettings;
            if (RandomSprintTimer < Time.time && persSettings.SprintWhileSearch)
            {
                float chance = persSettings.FrequentSprintWhileSearch ? 40f : 10f;
                SprintEnabled = EFTMath.RandomBool(chance);
                float timeAdd;
                if (SprintEnabled)
                {
                    timeAdd = 1f * Random.Range(0.75f, 4.00f);
                }
                else
                {
                    timeAdd = 3f * Random.Range(0.5f, 1.25f);
                }
                RandomSprintTimer = Time.time + timeAdd;
            }
        }

        private bool SprintEnabled = false;
        private float RandomSprintTimer = 0f;

        private void Steer()
        {
            if (BotOwner.Memory.IsUnderFire)
            {
                SAIN.Mover.Sprint(false);
                SteerByPriority(false);
                return;
            }
            if (SprintEnabled)
            {
                LookToMovingDirection();
                return;
            }
            if (Search.CurrentState == ESearchMove.MoveToDangerPoint)
            {
                float distance = (Search.SearchMovePoint.DangerPoint - SAIN.Position).sqrMagnitude;
                if (distance > 2f)
                {
                    LookToMovingDirection();
                    return;
                }
            }
            if (SteerByPriority(false) == false)
            {
                if (CanSeeDangerOrCorner(out Vector3 point) && SAIN.CurrentTargetPosition != null)
                {
                    Vector3 direction = point - SAIN.Position;
                    Vector3 targetDirection = SAIN.CurrentTargetPosition.Value - SAIN.Position;
                    float dot = Vector3.Dot(direction, targetDirection);
                    if (dot > 0.25f && (point - SAIN.Position).sqrMagnitude > 0.5f)
                    {
                        SAIN.Steering.LookToPoint(point);
                    }
                    else
                    {
                        SAIN.Steering.LookToPoint(SAIN.CurrentTargetPosition.Value);
                    }
                }
                else
                {
                    LookToMovingDirection();
                }
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
                CheckSeeTimer = Time.time + 0.5f * Random.Range(0.66f, 1.33f);
                var headPosition = SAIN.Transform.Head;

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

        private bool SteerByPriority(bool value) => SAIN.Steering.SteerByPriority(value);

        private void LookToMovingDirection() => SAIN.Steering.LookToMovingDirection();

        public NavMeshPath Path = new NavMeshPath();
    }
}