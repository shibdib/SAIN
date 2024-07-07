using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public struct HardColliderData
    {
        public HardColliderData(Collider collider)
        {
            Collider = collider;
            Position = collider.transform.position;
        }

        public Collider Collider { get; }
        public Vector3 Position { get; }
    }
}