using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public enum ERaycastCheck
    {
        LineofSight,
        Shoot,
        Vision,
    }

    public class EnemyPartsClass : EnemyBase
    {
        private const float LINEOFSIGHT_TIME = 0.25f;
        private const float CANSHOOT_TIME = 0.25f;

        public EnemyPartsClass(Enemy enemy) : base(enemy)
        {
            createPartDatas(enemy.Player.PlayerBones);
            _indexMax = Parts.Count;
        }

        public bool LineOfSight => TimeSinceInLineOfSight < LINEOFSIGHT_TIME;
        public float TimeSinceInLineOfSight => Time.time - _timeLastInSight;
        public Vector3 LastSuccessLookPosition { get; private set; }

        public bool CanShoot => TimeSinceCanShoot < CANSHOOT_TIME;
        public float TimeSinceCanShoot => Time.time - _timeLastCanShoot;
        public Vector3 LastSuccessShootPosition { get; private set; }

        public Dictionary<EBodyPart, EnemyPartDataClass> Parts { get; } = new Dictionary<EBodyPart, EnemyPartDataClass>();

        public void Update()
        {
            updateParts();
        }

        private void updateParts()
        {
            bool inSight = false;
            bool canShoot = false;
            float time = Time.time;

            Vector3 eyePos = Bot.Transform.EyePosition;
            foreach (var part in Parts.Values) {
                //part.UpdateDebugGizmos(eyePos);

                if (!canShoot && part.CanShoot) {
                    canShoot = true;
                    _timeLastCanShoot = time;
                }

                if (!inSight && part.LineOfSight) {
                    inSight = true;
                    _timeLastInSight = time;
                }
            }
        }

        public void ReadRaycastResult(BotRaycastData data, ERaycastCheck type, float time)
        {
            var parts = data.Data.BodyParts;
            var colliderTypes = data.Data.ColliderTypes;
            var points = data.Data.Points;
            var hits = data.Job.Hits;
            for (int i = 0; i < parts.Length; i++) {
                if (Parts.TryGetValue(parts[i], out EnemyPartDataClass partData)) {
                    partData.SetLineOfSight(points[i], colliderTypes[i], hits[i], type, time);
                }
            }
        }

        public bool CheckCanShoot(bool canShootHead)
        {
            bool canShoot = false;
            bool isAI = Enemy.IsAI;
            Vector3 firePort = Enemy.Bot.Transform.WeaponData.FirePort;

            foreach (var part in Parts.Values) {
                if (!canShootHead && part.BodyPart == EBodyPart.Head) {
                    continue;
                }
                if (part.CheckCanShoot(firePort, isAI))
                    canShoot = true;
            }
            return canShoot;
        }

        public bool CheckBodyLineOfSight(Vector3 origin, float maxRange)
        {
            EnemyPartDataClass checkingPart = Parts[EBodyPart.Chest];
            if (checkingPart.CheckLineOfSight(origin, maxRange)) {
                return true;
            }
            return false;
        }

        public bool CheckHeadLineOfSight(Vector3 origin, float maxRange)
        {
            EnemyPartDataClass checkingPart = Parts[EBodyPart.Head];
            if (checkingPart.CheckLineOfSight(origin, maxRange)) {
                return true;
            }
            return false;
        }

        public bool CheckRandomPartLineOfSight(Vector3 origin, float maxRange)
        {
            bool inSight = false;

            if (_lastCheckSuccessPart != null) {
                if (_lastCheckSuccessPart.CheckLineOfSight(origin, maxRange)) {
                    inSight = true;
                }
                else {
                    _lastCheckSuccessPart = null;
                }
            }

            EnemyPartDataClass checkingPart = GetNextPart();

            if (checkingPart == _lastCheckSuccessPart) {
                return inSight;
            }

            if (checkingPart.CheckLineOfSight(origin, maxRange)) {
                _lastCheckSuccessPart = checkingPart;
                inSight = true;
            }
            return inSight;
        }

        public EnemyPartDataClass GetNextPart()
        {
            EnemyPartDataClass result = null;
            EBodyPart epart = (EBodyPart)_index;
            if (!Parts.TryGetValue(epart, out result)) {
                _index = 0;
                result = Parts[EBodyPart.Chest];
            }

            _index++;
            if (_index > _indexMax) {
                _index = 0;
            }

            if (result == null) {
                result = Parts.PickRandom().Value;
            }
            return result;
        }

        private void createPartDatas(PlayerBones bones)
        {
            var parts = Enemy.EnemyPlayerComponent.BodyParts.Parts;
            foreach (var bodyPart in parts) {
                Parts.Add(bodyPart.Key, new EnemyPartDataClass(bodyPart.Key, bodyPart.Value.Transform, bodyPart.Value.Colliders));
            }
        }

        private float _timeLastInSight;
        private float _timeLastCanShoot;
        private int _index;
        private readonly int _indexMax;
        private EnemyPartDataClass _lastCheckSuccessPart;
    }
}