using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public struct BodyPartRaycast
    {
        public EBodyPart PartType;
        public EBodyPartColliderType ColliderType;

        public float MaxRange;
        public Vector3 CastPoint;

        public RaycastHit LOSRaycastHit;
        public bool LineOfSight;

        public RaycastHit ShootRayCastHit;
        public bool CanShoot;

        public RaycastHit VisionRaycastHit;
        public bool IsVisible;
    }

    public class LineOfSightJobClass : SAINControllerBase
    {
        public RaycastWorkDelegator RaycastJobDelegator { get; }

        public LineOfSightJobClass(SAINBotController botController) : base(botController)
        {
            RaycastJobDelegator = new RaycastWorkDelegator(botController);
        }

        public void Update()
        {
            RaycastJobDelegator.Update();
        }

        public void Dispose()
        {
            RaycastJobDelegator.Dispose();
        }
    }
}