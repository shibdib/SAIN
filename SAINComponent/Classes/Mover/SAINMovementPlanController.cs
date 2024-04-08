using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static UnityEngine.UI.GridLayoutGroup;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINMovementPlanController : SAINBase, ISAINClass
    {
        public SAINMovementPlanController(SAINComponentClass sain) : base(sain)
        {
        }

        public static readonly List<SAINVaultPoint> GlobalVaultPoints = new List<SAINVaultPoint>();

        public void Init()
        {
        }

        public void Update()
        {
            //TryVaulting();
        }


        public void Dispose()
        {
        }
    }

    public sealed class SAINMovementPlan
    {
        private static int PointCount = 0;

        public SAINMovementPlan(NavMeshPath directPath, Vector3 endGoal)
        {
            DirectPath = directPath;
            Goal = endGoal;
            TimeCreated = Time.time;
            ID = PointCount;
            PointCount++;
            CalcPath();
        }

        private List<SAINMovementNode> CalcPath()
        {
            if (DirectPath == null || DirectPath.status == NavMeshPathStatus.PathInvalid)
            {
                return null;
            }
            switch (DirectPath.status)
            {
                case NavMeshPathStatus.PathInvalid:
                    return null;

                case NavMeshPathStatus.PathPartial:
                    CalcNavPath(DirectPath);
                    CalcSecondPath();
                    CalcNavPath(SecondPath);
                    break;

                case NavMeshPathStatus.PathComplete:
                    CalcNavPath(DirectPath);
                    break;
            }
            return Path;
        }

        private void CalcNavPath(NavMeshPath path)
        {
            Vector3[] corners = path.corners;
            int max = corners.Length - 2;
            for (int i = 0; i <= max; i++)
            {
                Vector3 cornerA = corners[i];
                Vector3 cornerB = corners[i + 1];
                Vector3 direction = cornerA - cornerB;
                // Break down a straight line from Point A to Point B into segments, to break up movement in a straight line.
                float segmentLength = SAINBotSpaceAwareness.GetSegmentLength(3, direction, 5, 50, out float dirMagnitude, out int countResult);

                if (segmentLength > 0)
                {
                    Vector3 dirNormal = direction.normalized;
                    Vector3 dirSegment = dirNormal * segmentLength;

                    Vector3 newNode = cornerA;
                    for (int j = 0; j < countResult; j++)
                    {
                        newNode += dirSegment;
                        AddPositionToPath(newNode);
                    }
                }

                AddPositionToPath(cornerB);
            }
            DrawDebug();
        }

        private void DrawDebug()
        {
            if (SAINPlugin.DebugMode && SAINPlugin.EditorDefaults.DebugMovementPlan)
            {
                for (int i = 0; i < Path.Count - 1; i++)
                {
                    Vector3 pointA = Path[i].PathNodePosition;
                    Vector3 pointB = Path[i + 1].PathNodePosition;

                    DebugGizmos.Sphere(pointA, 0.1f, Color.white, true, 10f);
                    DebugGizmos.Ray(pointA, Vector3.up, Color.white, 1f, 0.025f, true, 10f);
                    DebugGizmos.Line(pointA, pointB, 0.05f, 10f);
                }
            }
        }

        private void AddPositionToPath(Vector3 point)
        {
            Path.Add(new SAINMovementNode(point));
        }

        private void CalcSecondPath()
        {
            SecondPath = new NavMeshPath();
            Vector3 endOfPath = DirectPath.corners[DirectPath.corners.Length - 1];
            if (NavMesh.CalculatePath(endOfPath, Goal, -1, SecondPath) && SecondPath.status == NavMeshPathStatus.PathComplete)
            {

            }
        }

        public List<SAINMovementNode> Path { get; private set; } = new List<SAINMovementNode>();
        public readonly NavMeshPath DirectPath;
        private NavMeshPath SecondPath;
        public readonly Vector3 Goal;
        public readonly float TimeCreated;
        public readonly int ID;
    }

    public sealed class SAINMovementNode
    {
        public SAINMovementNode(Vector3 pos)
        {
            PathNodePosition = pos;
        }

        public CoverPoint CoverPoint;
        public readonly Vector3 PathNodePosition;
    }
}