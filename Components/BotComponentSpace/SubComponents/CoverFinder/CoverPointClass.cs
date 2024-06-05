using EFT;
using EFT.Game.Spawning;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverPoint
    {
        public CoverPoint(BotComponent sain, Vector3 point, Collider collider, NavMeshPath pathToPoint)
        {
            SAIN = sain;
            TimeCreated = Time.time;
            Collider = collider;
            Vector3 size = collider.bounds.size;
            CoverHeight = size.y;
            CoverValue = (size.x + size.y + size.z).Round10();
            Position = point;
            PathToPoint = pathToPoint;
        }

        private BotComponent SAIN;

        public bool BotIsUsingThis => SAIN.Cover.CoverInUse == this;

        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                setPosition(value);
            }
        }

        private void setPosition(Vector3 value)
        {
            _position = value;
            calcDirection(value);
            TimeLastUpdated = Time.time;
        }

        private void calcDirection(Vector3 value)
        {
            Vector3 dir = ColliderPosition - value;
            DirectionToCollider = dir;
            DirectionToColliderNormal = dir.normalized;
        }

        private Vector3 _position;

        public Vector3 DirectionToCollider { get; private set; }
        public Vector3 DirectionToColliderNormal { get; private set; }

        public Vector3 ColliderPosition => Collider.transform.position;

        public float TimeLastUpdated;
        public float TimeLastUsed = 0f;

        const float coverUpdateFreq = 0.5f;
        public bool ShallUpdate => Time.time - TimeLastUpdated > coverUpdateFreq;

        private float nextCalcPathTime = 0f;

        public int RoundedPathLength;

        public float PathLength
        {
            get
            {
                return _pathLength;
            }
            set
            {
                _pathLength = value;
            }
        }

        public void CalcPathLength()
        {
            if (nextCalcPathTime < Time.time)
            {
                nextCalcPathTime = Time.time + 1;
                PathToPoint.ClearCorners();
                if (NavMesh.CalculatePath(SAIN.Position, Position, -1, PathToPoint))
                {
                    _pathLength = PathToPoint.CalculatePathLength();
                }
                else
                {
                    _pathLength = float.MaxValue;
                }

                RoundedPathLength = Mathf.RoundToInt(_pathLength);
            }
        }

        private float _pathLength;

        private float nextPathSafetyTime = 0f;

        public CoverStatus Status
        {
            get
            {
                if (nextCheckStatusTime < Time.time)
                {
                    nextCheckStatusTime = Time.time + 0.1f;
                    SqrMagnitude = (SAIN.Position - Position).sqrMagnitude;

                    CoverStatus status;
                    if (_status == CoverStatus.InCover && SqrMagnitude <= InCoverStayDist * InCoverStayDist)
                    {
                        status = CoverStatus.InCover;
                    }
                    else if (SqrMagnitude <= InCoverDist * InCoverDist)
                    {
                        status = CoverStatus.InCover;
                    }
                    else if (SqrMagnitude <= CloseCoverDist * CloseCoverDist)
                    {
                        status = CoverStatus.CloseToCover;
                    }
                    else if (SqrMagnitude <= MidCoverDist * MidCoverDist)
                    {
                        status = CoverStatus.MidRangeToCover;
                    }
                    else
                    {
                        status = CoverStatus.FarFromCover;
                    }

                    LastStatus = _status;
                    _status = status;
                }
                return _status;
            }
        }

        private CoverStatus _status;

        public CoverStatus LastStatus = CoverStatus.None;

        public bool PointIsVisible 
        { 
            get
            {
                if (nextCheckVisTime < Time.time)
                {
                    nextCheckVisTime = Time.time + 0.5f;

                    _pointIsVisible = false;
                    if (SAIN.Enemy != null)
                    {
                        Vector3 coverPos = Position;
                        coverPos += Vector3.up * 0.5f;
                        Vector3 start = SAIN.Enemy.EnemyHeadPosition;
                        Vector3 direction = coverPos - start;
                        _pointIsVisible = !Physics.Raycast(start, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
                    }
                }
                return _pointIsVisible;

            }
        }

        private bool _pointIsVisible;

        private float nextCheckVisTime = 0f;

        public bool Spotted => HitInCoverCount > 2 || HitInCoverUnknownCount > 0 || HitInCoverCantSeeCount > 1;

        public int HitInCoverUnknownCount = 0;

        public int HitInCoverCantSeeCount = 0;

        public int HitInCoverCount = 0;

        public bool CheckPathSafety(out bool didCheck)
        {
            didCheck = false;
            if (SAIN.CurrentTargetPosition == null)
            {
                _isSafePath = true;
                return _isSafePath;
            }
            if (nextPathSafetyTime < Time.time)
            {
                nextPathSafetyTime = Time.time + 2f;
                didCheck = true;
                _isSafePath = SAINBotSpaceAwareness.CheckPathSafety(PathToPoint, SAIN.CurrentTargetPosition.Value + Vector3.up);
            }
            return _isSafePath;
        }

        public IEnumerator CheckPathSafety()
        {
            if (!_isSafePath && SAIN.CurrentTargetPosition == null)
            {
                _isSafePath = true;
            }
            else if (nextPathSafetyTime < Time.time)
            {
                nextPathSafetyTime = Time.time + 2f;
                yield return SAINBotSpaceAwareness.CheckPathSafety(this, SAIN.CurrentTargetPosition.Value + Vector3.up);
            }
            yield return null;
        }

        public bool IsSafePath
        {
            get
            {
                return _isSafePath;
            }
            set
            {
                //nextPathSafetyTime = Time.time + 2f;
                _isSafePath = value;
            }
        }

        private bool _isSafePath;

        public float SqrMagnitude = float.MaxValue;

        private float nextCheckStatusTime = 0f;

        public bool IsBad;

        public float CoverValue { get; private set; }

        public NavMeshPath PathToPoint { get; private set; }

        public float CoverHeight { get; private set; }

        public bool BotInThisCover()
        {
            return Status == CoverStatus.InCover;
        }

        private const float InCoverDist = 1f;
        private const float InCoverStayDist = 1.25f;
        private const float CloseCoverDist = 10f;
        private const float MidCoverDist = 20f;

        public Collider Collider { get; private set; }
        public float TimeCreated { get; private set; }
    }

    public sealed class SAINBotCoverInfo
    {
        public SAINBotCoverInfo(BotComponent sain)
        {
            SAIN = sain;
        }
        public readonly BotComponent SAIN;

        public bool BotIsUsingThis = false;
        public float TimeLastUsed = 0f;
        public Vector3 Position = Vector3.zero;
        public float nextCalcPathTime = 0f;
        public float pathLength = float.MaxValue;
        public float nextPathSafetyTime = 0f;
        public CoverStatus Status = CoverStatus.None;
        public CoverStatus LastStatus = CoverStatus.None;
        public bool PointIsVisible = false;
        public float nextCheckVisTime = 0f;
        public bool Spotted = false;
        public int HitInCoverUnknownCount = 0;
        public int HitInCoverCantSeeCount = 0;
        public int HitInCoverCount = 0;
        public bool IsSafePath = false;
        public float SqrMagnitude = float.MaxValue;
        public float nextCheckStatusTime = 0f;
    }
}