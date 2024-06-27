using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonTransformClass
    {
        public Vector3 Position { get; private set; }
        public Vector3 LookDirection { get; private set; }
        public Vector3 HeadLookDirection { get; private set; }
        public Vector3 HeadPosition { get; private set; }
        public Vector3 EyePosition { get; private set; }
        public Vector3 BodyPosition { get; private set; }
        public Vector3 WeaponFirePort { get; private set; }
        public Vector3 WeaponPointDirection { get; private set; }

        public Vector3 DirectionToMe(Vector3 point)
        {
            return Position - point;
        }

        public Vector3 DirectionToPoint(Vector3 point)
        {
            return point - Position;
        }

        public Vector3 Right()
            => AngledLookDirection(0f, 90f, 0f);

        public Vector3 Left()
            => AngledLookDirection(0f, -90f, 0f);

        public Vector3 Back()
            => AngledLookDirection(0f, 180, 0f);

        public Vector3 AngledLookDirection(float x, float y, float z)
            => Quaternion.Euler(x, y, z) * LookDirection;

        public void UpdatePositions()
        {
            Position = _transform.position;
            EyePosition = _eyePart.Center;
            HeadPosition = _myHead.position;

            if (_nextUpdateTime > Time.time)
            {
                return;
            }

            _nextUpdateTime = Time.time + TRANSFORM_UPDATE_FREQ;


            var player = Person.Player;
            LookDirection = player.MovementContext.LookDirection;

            //HeadLookDirection = Quaternion.Euler(_myHead.localRotation.y, _myHead.localRotation.x, 0) * _myHead.forward;
            Vector3 headLookDir = Quaternion.Euler(0, _myHead.rotation.x + 90, 0) * _myHead.forward;
            headLookDir.y = LookDirection.y;
            HeadLookDirection = headLookDir;

            BodyPosition = _bodyPart.position;

            if (player.Fireport != null && player.Fireport.Original != null)
            {
                WeaponFirePort = player.Fireport.position;
                WeaponPointDirection = player.Fireport.Original.TransformDirection(player.LocalShotDirection);
            }

            PlayerVelocity = getPlayerVelocity();
        }

        private const float TRANSFORM_UPDATE_FPS = 30f;
        public const float TRANSFORM_UPDATE_FREQ = 1f / TRANSFORM_UPDATE_FPS;
        private float _nextUpdateTime;

        public PersonTransformClass(PersonClass person)
        {
            Person = person;
            _transform = person.Player.Transform;
            var _bones = person.Player.PlayerBones;
            _myHead = _bones.Head;
            _bodyPart = _bones.Ribcage;
            _eyePart = _bones.BodyPartCollidersDictionary[EBodyPartColliderType.Eyes];
        }

        public float PlayerVelocity { get; private set; }

        private float getPlayerVelocity()
        {
            const float min = 0.5f * 0.5f;
            const float max = 5f * 5f;

            var player = Person.Player;
            if (player == null)
            {
                return 0f;
            }

            float rawVelocity = player.Velocity.sqrMagnitude;
            if (rawVelocity <= min)
            {
                return 0f;
            }
            if (rawVelocity >= max)
            {
                return 1f;
            }

            float num = max - min;
            float num2 = rawVelocity - min;
            return num2 / num;
        }

        private readonly PersonClass Person;
        private readonly BifacialTransform _transform;
        private readonly BifacialTransform _myHead;
        private readonly BifacialTransform _bodyPart;
        private readonly BodyPartCollider _eyePart;
    }
}