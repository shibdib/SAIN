using EFT;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.BaseClasses
{
    public class SAINPersonTransformClass
    {
        public SAINPersonTransformClass(SAINPersonClass person)
        {
            Person = person;
            _headPart = person.IPlayer.MainParts[BodyPartType.head];
            _bodyPart = person.IPlayer.MainParts[BodyPartType.body];
        }

        public void Update()
        {
        }

        private readonly SAINPersonClass Person;
        public bool TransformNull => Person == null || Person.PlayerNull || DefaultTransform == null || Person.Player == null || Person.Player.gameObject == null;
        public BifacialTransform DefaultTransform => Person?.IPlayer?.Transform;
        public Vector3 Position => !TransformNull ? DefaultTransform.position : Vector3.zero;
        public Vector3 LookDirection => !TransformNull ? Person.IPlayer.LookDirection : Vector3.zero;
        public Vector3 Direction(Vector3 start) => !TransformNull ? Position - start : Vector3.zero;
        public Vector3 HeadPosition => _headPart.Position;
        public Vector3 CenterPosition => _bodyPart.Position;

        private EnemyPart _headPart;
        private EnemyPart _bodyPart;
    }
}