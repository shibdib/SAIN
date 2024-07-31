using UnityEngine;

namespace SAIN.Components
{
    public struct BodyPartRaycast
    {
        public EBodyPart PartType;
        public EBodyPartColliderType ColliderType;
        public Vector3 CastPoint;
    }
}