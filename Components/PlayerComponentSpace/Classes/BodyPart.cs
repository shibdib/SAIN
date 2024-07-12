using EFT;
using System.Collections.Generic;

namespace SAIN.Components
{
    public struct BodyPart
    {
        public readonly EBodyPart Type;
        public readonly BifacialTransform Transform;
        public readonly List<BodyPartCollider> Colliders;

        public BodyPart(EBodyPart bodyPart, BifacialTransform transform, List<BodyPartCollider> colliders)
        {
            Type = bodyPart;
            Transform = transform;
            Colliders = colliders;
        }
    }
}