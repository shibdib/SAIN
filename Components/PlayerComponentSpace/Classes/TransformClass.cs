using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class TransformClass
    {
        //public bool TransformNull => Person == null || Person.PlayerNull || _transform == null || Person.Player == null || Person.Player.gameObject == null;

        public Vector3 Position
        {
            get
            {
                if (_transform == null)
                {
                    return Vector3.zero;
                }
                return _position;
            }
        }

        private Vector3 _position => _transform.position;

        public Vector3 LookDirection
        {
            get
            {
                if (_transform == null || Person.Player == null)
                {
                    return Vector3.zero;
                }
                return _lookDirection;
            }
        }

        private Vector3 _lookDirection => Person.Player.LookDirection;

        public Vector3 DirectionTo(Vector3 point)
        {
            if (_transform == null)
            {
                return Vector3.zero;
            }
            return _position - point;
        }

        public Vector3 Right(bool normalized = true)
        {
            return AngledLookDirection(0f, 90f, 0f, normalized);
        }

        public Vector3 Left(bool normalized = true)
        {
            return AngledLookDirection(0f, -90f, 0f, normalized);
        }

        public Vector3 Back(bool normalized = true)
        {
            return AngledLookDirection(0f, 180, 0f, normalized);
        }

        public Vector3 AngledLookDirection(float x, float y, float z, bool normalized)
        {
            if (_transform == null || Person.Player == null)
            {
                return Vector3.zero;
            }
            Vector3 lookDir = _lookDirection;
            if (normalized)
            {
                lookDir = Vector3.Normalize(lookDir);
            }
            return Quaternion.Euler(x, y, z) * lookDir;
        }

        public Vector3 HeadPosition => _headPart.Position;
        public Vector3 CenterPosition => _bodyPart.Position;

        public TransformClass(SAINPersonClass person)
        {
            Person = person;
            _headPart = person.IPlayer.MainParts[BodyPartType.head];
            _bodyPart = person.IPlayer.MainParts[BodyPartType.body];
            _transform = person.IPlayer.Transform;
        }

        private readonly SAINPersonClass Person;
        private readonly BifacialTransform _transform;
        private EnemyPart _headPart;
        private EnemyPart _bodyPart;
    }
}