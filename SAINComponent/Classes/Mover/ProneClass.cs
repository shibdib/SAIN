using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class ProneClass : SAINBase, ISAINClass
    {
        public ProneClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        static ProneClass()
        {
            _isProneProperty = AccessTools.Property(typeof(BotOwner), "BotLay").PropertyType.GetProperty("IsLay");
        }

        private static readonly PropertyInfo _isProneProperty;

        public bool IsProne => BotOwner.BotLay.IsLay;

        public void SetProne(bool value)
        {
            _isProneProperty.SetValue(BotLay, value);
        }

        public bool ShallProne(CoverPoint point, bool withShoot)
        {
            var status = point.Status;
            if (status == CoverStatus.FarFromCover || status == CoverStatus.None)
            {
                if (Player.MovementContext.CanProne)
                {
                    var enemy = SAINBot.Enemy;
                    if (enemy != null)
                    {
                        float distance = (enemy.EnemyPosition - SAINBot.Transform.Position).magnitude;
                        if (distance > 20f)
                        {
                            if (withShoot)
                            {
                                return CanShootFromProne(enemy.EnemyPosition);
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool ShallProne(bool withShoot, float mindist = 25f)
        {
            if (Player.MovementContext.CanProne)
            {
                var enemy = SAINBot.Enemy;
                if (enemy != null)
                {
                    float distance = (enemy.EnemyPosition - SAINBot.Position).sqrMagnitude;
                    if (distance > mindist * mindist)
                    {
                        if (withShoot)
                        {
                            return CanShootFromProne(enemy.EnemyPosition);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ShallProneHide(float mindist = 10f)
        {
            if (Player.MovementContext.CanProne)
            {
                Vector3? targetPos = SAINBot.CurrentTargetPosition;
                if (targetPos != null)
                {
                    float distance = (targetPos.Value - SAINBot.Transform.Position).magnitude;
                    if (distance > mindist)
                    {
                        if (SAINBot.Decision.CurrentSelfDecision == SelfDecision.None && !SAINBot.Suppression.IsHeavySuppressed)
                        {
                            return !CanShootFromProne(targetPos.Value);
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool ShallGetUp(float mindist = 30f)
        {
            if (BotLay.IsLay)
            {
                var enemy = SAINBot.Enemy;
                if (enemy == null)
                {
                    return true;
                }
                float distance = (enemy.EnemyPosition - SAINBot.Transform.Position).magnitude;
                if (distance > mindist)
                {
                    return !IsChestPosVisible(enemy.EnemyHeadPosition);
                }
            }
            return false;
        }

        public bool IsChestPosVisible(Vector3 enemyHeadPos)
        {
            Vector3 botPos = SAINBot.Transform.Position;
            botPos += Vector3.up * 1f;
            Vector3 direction = botPos - enemyHeadPos;
            return !Physics.Raycast(enemyHeadPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool CanShootFromProne(Vector3 target)
        {
            Vector3 vector = SAINBot.Transform.Position + Vector3.up * 0.14f;
            Vector3 vector2 = target + Vector3.up - vector;
            Vector3 from = vector2;
            from.y = vector.y;
            float num = Vector3.Angle(from, vector2);
            float lay_DOWN_ANG_SHOOT = HelpersGClass.EFTCore.LAY_DOWN_ANG_SHOOT;
            return num <= Mathf.Abs(lay_DOWN_ANG_SHOOT) && Vector.CanShootToTarget(new ShootPointClass(target, 1f), vector, BotOwner.LookSensor.Mask, true);
        }

        public BotLay BotLay => BotOwner.BotLay;
    }
}