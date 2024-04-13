using UnityEngine;
using System;
using EFT;
using SAIN.Helpers;
using System.Collections.Generic;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class ColliderFinder
    {
        public ColliderFinder(CoverFinderComponent component)
        {
            CoverFinderComponent = component;
        }

        private CoverFinderComponent CoverFinderComponent;
        private Vector3 OriginPoint => CoverFinderComponent.OriginPoint;
        private Vector3 TargetPoint => CoverFinderComponent.TargetPoint;

        public void GetNewColliders(out int hits, Collider[] array, int iterationMax = 5, float startRadius = 5f, int hitThreshold = 50, LayerMask colliderMask = default)
        {
            const float StartCapsuleTop = 0.5f;
            const float StartCapsuleBottom = 0.25f;
            const float HeightIncreasePerIncrement = 1.5f;
            const float HeightDecreasePerIncrement = 1.5f;
            const float RadiusIncreasePerIncrement = 5f;

            ClearColliders(array);

            if (colliderMask == default)
            {
                colliderMask = LayerMaskClass.HighPolyWithTerrainMask;
            }

            // Lift the origin point off the ground slightly to avoid collecting random rubbish.
            Vector3 bottomCapsule = OriginPoint + Vector3.up * StartCapsuleBottom;
            Vector3 topCapsule = bottomCapsule + Vector3.up * StartCapsuleTop;
            float capsuleRadius = startRadius;

            hits = 0;
            for (int i = 0; i < iterationMax; i++)
            {
                int rawHits = Physics.OverlapCapsuleNonAlloc(bottomCapsule, topCapsule, capsuleRadius, array, colliderMask);
                hits = FilterColliders(array, rawHits);

                if (hits > hitThreshold)
                {
                    DebugGizmos.Capsule(bottomCapsule, capsuleRadius, topCapsule.y, Color.red, 5f);
                    return;
                }
                else
                {
                    DebugGizmos.Capsule(bottomCapsule, capsuleRadius, topCapsule.y, Color.white, 5f);
                    topCapsule += Vector3.up * HeightIncreasePerIncrement;
                    bottomCapsule += Vector3.down * HeightDecreasePerIncrement;
                    capsuleRadius += RadiusIncreasePerIncrement;
                }
            }
        }

        private void ClearColliders(Collider[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = null;
            }
        }

        /// <summary>
        /// Sorts an array of Colliders based on their Distance from bot's Position. 
        /// </summary>
        /// <param value="array">The array of Colliders to be sorted.</param>
        public void SortArrayBotDist(Collider[] array)
        {
            Array.Sort(array, ColliderArrayBotDistComparer);
        }

        private int FilterColliders(Collider[] array, int hits)
        {
            float minHeight = CoverFinderComponent.CoverMinHeight;
            const float minX = 0.1f;
            const float minZ = 0.1f;

            int hitReduction = 0;
            for (int i = 0; i < hits; i++)
            {
                Vector3 size = array[i].bounds.size;
                if (size.y < CoverFinderComponent.CoverMinHeight
                    || size.x < minX && size.z < minZ 
                    || ColliderAlreadyUsed(array[i], CoverFinderComponent.CoverPoints))
                {
                    array[i] = null;
                    hitReduction++;
                }
            }
            return hits - hitReduction;
        }

        private bool ColliderAlreadyUsed(Collider collider, List<CoverPoint> coverPoints)
        {
            for (int i = 0; i < coverPoints.Count;i++)
            {
                if (collider == coverPoints[i].Collider)
                {
                    return true;
                }
            }
            return false;
        }

        public int ColliderArrayBotDistComparer(Collider A, Collider B)
        {
            if (A == null && B != null)
            {
                return 1;
            }
            else if (A != null && B == null)
            {
                return -1;
            }
            else if (A == null && B == null)
            {
                return 0;
            }
            else
            {
                float AMag = (OriginPoint - A.transform.position).sqrMagnitude;
                float BMag = (OriginPoint - B.transform.position).sqrMagnitude;
                return AMag.CompareTo(BMag);
            }
        }
    }
}