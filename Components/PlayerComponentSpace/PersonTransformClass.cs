using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PersonTransformClass
    {
        public Vector3 Position { get; private set; }
        public Vector3 LookDirection { get; private set; }
        public Vector3 HeadPosition { get; private set; }
        public Vector3 EyePosition { get; private set; }
        public Vector3 BodyPosition { get; private set; }

        public Vector3 DirectionTo(Vector3 point)
        {
            return Position - point;
        }

        public Vector3 Right()
            => AngledLookDirection(0f, 90f, 0f);

        public Vector3 Left()
            => AngledLookDirection(0f, -90f, 0f);

        public Vector3 Back()
            => AngledLookDirection(0f, 180, 0f);

        public Vector3 AngledLookDirection(float x, float y, float z)
            => Quaternion.Euler(x, y, z) * LookDirection;

        public void Update()
        {
            if (_eyePart != null)
                EyePosition = _eyePart.Center;

            if (_headPart != null)
                HeadPosition = _headPart.Position;

            if (_bodyPart != null)
                BodyPosition = _bodyPart.Position;

            if (getLookDir(out Vector3 lookDir))
                LookDirection = lookDir;

            if (_transform != null)
                Position = _transform.position;
        }

        private bool getLookDir(out Vector3 lookDir)
        {
            var player = Person.Player;
            if (player != null && player.MovementContext != null)
            {
                lookDir = player.MovementContext.LookDirection;
                return true;
            }
            lookDir = Vector3.zero;
            return false;
        }

        public PersonTransformClass(PersonClass person)
        {
            Person = person;
            _headPart = person.IPlayer.MainParts[BodyPartType.head];
            _bodyPart = person.IPlayer.MainParts[BodyPartType.body];
            _transform = person.IPlayer.Transform;
            _eyePart = person.IPlayer.PlayerBones.BodyPartCollidersDictionary[EBodyPartColliderType.Eyes];
        }

        private readonly PersonClass Person;
        private readonly BifacialTransform _transform;
        private readonly EnemyPart _headPart;
        private readonly EnemyPart _bodyPart;
        private readonly BodyPartCollider _eyePart;
    }
}