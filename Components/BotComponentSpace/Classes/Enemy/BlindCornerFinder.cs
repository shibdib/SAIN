using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class BlindCornerFinder : EnemyBase
    {
        public BlindCornerFinder(SAINEnemy enemy) : base(enemy)
        {
        }

        public IEnumerator FindBlindCorner(NavMeshPath path)
        {
            _corners.Clear();
            _corners.AddRange(path.corners);
            if (_corners.Count > 2)
            {
                Vector3 lookPoint = Bot.Transform.EyePosition;
                Vector3 lookOffset = lookPoint - Bot.Position;
                yield return Bot.StartCoroutine(findBlindCorner(_corners, lookPoint, lookOffset.y));
                yield return Bot.StartCoroutine(findRealCorner(_blindCornerGround, _cornerNotVisible, lookPoint, lookOffset.y));
            }
            else
            {
                BlindCorner = null;
            }
            _corners.Clear();
        }

        private IEnumerator clearShortCorners(List<Vector3> corners, float min)
        {
            int removed = 0;
            int count = corners.Count;

            //StringBuilder stringBuilder = new StringBuilder();
            //stringBuilder.AppendLine($"Clearing Short Corners of [{count}] for [{Bot.name}] with min [{min}]...");

            for (int i = count - 2; i >= count; i--)
            {
                Vector3 cornerA = corners[i];
                Vector3 cornerB = corners[i + 1];

                float magnitude = (cornerA - cornerB).magnitude;
                //Logger.LogDebug($"{i} to {i + 1} mag: [{magnitude}] min [{min}]");

                if (magnitude > min)
                    continue;

                corners[i + 1] = Vector3.Lerp(cornerA, cornerB, 0.5f);
                corners.RemoveAt(i);

                //stringBuilder.AppendLine($"Corner [{i + 1}] replaced. Removed [{i}] because Magnitude [{magnitude}] with min [{min}]");
                removed++;
            }

            if (removed > 0)
            {
                //stringBuilder.AppendLine($"Finished Clearing Short Corners. Removed [{removed}] corners from [{count}]");
                //Logger.LogDebug(stringBuilder.ToString());
            }

            yield return null;
        }

        private IEnumerator findBlindCorner(List<Vector3> corners, Vector3 lookPoint, float height)
        {
            Vector3? result = null;
            Vector3? notVisCorner = null;
            int count = corners.Count;
            if (count > 2)
            {
                for (int i = 1; i < corners.Count; i++)
                {
                    Vector3 target = corners[i];
                    target.y += height;
                    Vector3 direction = target - lookPoint;

                    if (Physics.Raycast(lookPoint, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        result = corners[i - 1];
                        notVisCorner = corners[i];
                        //DebugGizmos.Line(target, lookPoint, Color.red, 0.05f, true, 5f, true);
                        break;
                    }
                    //DebugGizmos.Line(target, lookPoint, Color.white, 0.05f, true, 5f, true);
                    //yield return null;
                }
                if (result == null && count > 1)
                {
                    result = corners[1];
                }
                if (notVisCorner == null && count > 2)
                {
                    notVisCorner = corners[count - 1];
                }
            }
            _blindCornerGround = result ?? Vector3.zero;
            _cornerNotVisible = notVisCorner ?? Vector3.zero;
            yield return null;
        }

        private IEnumerator findRealCorner(Vector3 blindCorner, Vector3 notVisibleCorner, Vector3 lookPoint, float height, int iterations = 15)
        {
            //StringBuilder stringBuilder = new StringBuilder();
            //stringBuilder.AppendLine($"Finding Real Blind Corner for [{Bot.name}]...");

            if (blindCorner == Vector3.zero)
            {
                BlindCorner = null;
                yield break;
            }
            blindCorner.y += height;
            BlindCorner = blindCorner;
            if (notVisibleCorner == Vector3.zero)
            {
                yield break;
            }

            float sign = Vector.FindFlatSignedAngle(blindCorner, notVisibleCorner, lookPoint);
            float angle = sign <= 0 ? -10f : 10f;
            float rotationStep = angle / iterations;

            //stringBuilder.AppendLine($"Angle to check [{angle}] Step Angle [{rotationStep}]");

            Vector3 directionToBlind = blindCorner - lookPoint;

            int raycasts = 0;

            for (int i = 0; i < iterations; i++)
            {
                directionToBlind = Vector.Rotate(directionToBlind, 0, rotationStep, 0);
                if (!Physics.Raycast(lookPoint, directionToBlind, directionToBlind.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    BlindCorner = lookPoint + directionToBlind;
                }
                else
                {
                    //stringBuilder.AppendLine($"Angle where LOS broken [{rotationStep * i}] after [{i}] iterations");
                    break;
                }
                raycasts++;

                if (raycasts >= 3)
                {
                    yield return null;
                }
            }

            //stringBuilder.AppendLine("Finished Checking for real Blind Corner");
            //Logger.LogAndNotifyDebug(stringBuilder.ToString());
        }

        public Vector3? BlindCorner { get; set; }
        private Vector3 _blindCornerGround;
        private Vector3 _cornerNotVisible;
        private readonly List<Vector3> _corners = new List<Vector3>();
    }
}