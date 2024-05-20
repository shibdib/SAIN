using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public enum CoverFinderState
    {
        off = 0,
        on = 1,
        forceOff = 2,
        forceOn = 3,
    }

    public class SAINCoverClass : SAINBase, ISAINClass
    {
        public SAINCoverClass(SAINComponentClass sain) : base(sain)
        {
            CoverFinder = sain.GetOrAddComponent<CoverFinderComponent>();
            Player.HealthController.ApplyDamageEvent += OnBeingHit;
        }

        public CoverPoint FindPointInDirection(Vector3 direction, float dotThreshold = 0.33f, float minDistance = 8f)
        {
            Vector3 botPosition = SAIN.Position;
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                CoverPoint point = CoverPoints[i];
                if (point != null && !point.Spotted)
                {
                    Vector3 coverPosition = point.Position;
                    Vector3 directionToPoint = botPosition - coverPosition;

                    if (directionToPoint.sqrMagnitude > minDistance * minDistance 
                        && Vector3.Dot(directionToPoint.normalized, direction.normalized) > dotThreshold)
                    {
                        return point;
                    }
                }
            }
            return null;
        }

        public void Init()
        {
            CoverFinder.Init(SAIN);
        }

        public CoverFinderState CurrentCoverFinderState { get; private set; }

        public void Update()
        {
            if (!SAIN.BotActive || SAIN.GameIsEnding)
            {
                ActivateCoverFinder(false);
                return;
            }
            ActivateCoverFinder(SAIN.Decision.SAINActive);
        }

        public void Dispose()
        {
            try
            {
                Player.HealthController.ApplyDamageEvent -= OnBeingHit;
                CoverFinder?.Dispose();
            }
            catch { }
        }

        private void OnBeingHit(EBodyPart part, float unused, DamageInfo damage)
        {
            LastHitTime = Time.time;

            CoverPoint coverInUse = CoverInUse;
            if (coverInUse != null)
            {
                SAINEnemy enemy = SAIN.Enemy;
                bool HitInCoverKnown = enemy != null && damage.Player != null && enemy.EnemyPlayer.ProfileId == damage.Player.iPlayer.ProfileId;
                bool HitInCoverCantSee = enemy != null && enemy.IsVisible == false;

                if (HitInCoverCantSee)
                {
                    coverInUse.HitInCoverCantSeeCount++;
                }
                else if (HitInCoverKnown)
                {
                    coverInUse.HitInCoverCount++;
                }
                else
                {
                    coverInUse.HitInCoverUnknownCount++;
                }
            }
        }

        private void ActivateCoverFinder(bool value)
        {
            if (value)
            {
                CoverFinder?.LookForCover();
                CurrentCoverFinderState = CoverFinderState.on;
            }
            if (!value)
            {
                CoverFinder?.StopLooking();
                CurrentCoverFinderState = CoverFinderState.off;
            }
        }

        public void CheckResetCoverInUse()
        {
            SoloDecision decision = SAIN.Decision.CurrentSoloDecision;
            if (decision != SoloDecision.MoveToCover
                && decision != SoloDecision.RunToCover
                && decision != SoloDecision.Retreat
                && decision != SoloDecision.HoldInCover)
            {
                SAIN.Cover.CoverInUse = null;
            }
        }

        public CoverPoint ClosestPoint
        {
            get
            {
                // Call path length to trigger recalc if timer allows.
                foreach (var point in CoverPoints)
                {
                    float dist = point.PathLength;
                }

                SortPointsByPathDist();

                for (int i = 0; i < CoverPoints.Count; i++)
                {
                    CoverPoint point = CoverPoints[i];
                    if (point != null)
                    {
                        if (point != null && point.Spotted == false)
                        {
                            return point;
                        }
                    }
                }
                return null;
            }
        }

        public void SortPointsByPathDist()
        {
            CoverFinderComponent.OrderPointsByPathDist(CoverPoints, SAIN);
        }

        private bool GetPointToHideFrom(out Vector3? target)
        {
            target = SAIN.Grenade.GrenadeDangerPoint ?? SAIN.CurrentTargetPosition;
            return target != null;
        }

        public bool DuckInCover()
        {
            var point = CoverInUse;
            if (point != null)
            {
                var move = SAIN.Mover;
                var prone = move.Prone;
                bool shallProne = prone.ShallProneHide();

                if (shallProne && (SAIN.Decision.CurrentSelfDecision != SelfDecision.None || SAIN.Suppression.IsHeavySuppressed))
                {
                    prone.SetProne(true);
                    return true;
                }
                if (move.Pose.SetPoseToCover())
                {
                    return true;
                }
                if (shallProne && point.Collider.bounds.size.y < 1f)
                {
                    prone.SetProne(true);
                    return true;
                }
            }
            return false;
        }

        public bool CheckLimbsForCover()
        {
            var enemy = SAIN.Enemy;
            if (enemy?.IsVisible == true)
            {
                if (CheckLimbTimer < Time.time)
                {
                    CheckLimbTimer = Time.time + 0.1f;
                    bool cover = false;
                    var target = enemy.EnemyIPlayer.WeaponRoot.position;
                    const float rayDistance = 3f;
                    if (CheckLimbForCover(BodyPartType.leftLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.leftArm, target, rayDistance))
                    {
                        cover = true;
                    }
                    else if (CheckLimbForCover(BodyPartType.rightLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.rightArm, target, rayDistance))
                    {
                        cover = true;
                    }
                    HasLimbCover = cover;
                }
            }
            else
            {
                HasLimbCover = false;
            }
            return HasLimbCover;
        }

        private bool HasLimbCover;
        private float CheckLimbTimer = 0f;

        private bool CheckLimbForCover(BodyPartType bodyPartType, Vector3 target, float dist = 2f)
        {
            var position = BotOwner.MainParts[bodyPartType].Position;
            Vector3 direction = target - position;
            return Physics.Raycast(position, direction, dist, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool BotIsAtCoverPoint(CoverPoint coverPoint)
        {
            return coverPoint?.BotInThisCover() == true;
        }

        public bool BotIsAtCoverInUse()
        {
            return CoverInUse?.BotInThisCover() == true;
        }

        public CoverPoint CoverInUse { get; set; }

        public List<CoverPoint> CoverPoints => CoverFinder.CoverPoints;
        public CoverFinderComponent CoverFinder { get; private set; }
        public CoverPoint FallBackPoint => CoverFinder.FallBackPoint;

        public float LastHitTime { get; private set; }
    }
}