using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using UnityEngine;

namespace SAIN.Layers.Combat.Squad
{
    internal class SuppressAction : SAINAction
    {
        public SuppressAction(BotOwner bot) : base(bot, nameof(SuppressAction))
        {
        }

        public override void Update()
        {
            var enemy = SAIN.Enemy;

            if (!BotOwner.WeaponManager.HaveBullets 
                || (!BotOwner.ShootData.Shooting && SAIN.Decision.SelfActionDecisions.LowOnAmmo(0.5f)))
            {
                SAIN.SelfActions.TryReload();
            }

            if (enemy != null)
            {
                if (enemy.IsVisible && enemy.CanShoot)
                {
                    SAIN.Mover.StopMove();
                    Shoot.Update();
                }
                else if (FindSuppressionTarget(out var target) && CanSeeSuppressionTarget(target))
                {
                    SAIN.Mover.StopMove();

                    bool hasMachineGun = SAIN.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun;
                    if (hasMachineGun 
                        && SAIN.Mover.Prone.ShallProne(true))
                    {
                        SAIN.Mover.Prone.SetProne(true);
                    }

                    //SAIN.Steering.LookToPoint(pos.Value);

                    if (!BotOwner.WeaponManager.HaveBullets)
                    {
                        SAIN.SelfActions.TryReload();
                    }
                    else if (
                        WaitShootTimer < Time.time 
                        && SAIN.Shoot(true, target.Value, true, SAINComponentClass.EShootReason.SquadSuppressing))
                    {
                        enemy.EnemyIsSuppressed = true;
                        float waitTime = hasMachineGun ? 0.1f : 0.5f;
                        WaitShootTimer = Time.time + (waitTime * Random.Range(0.75f, 1.25f));
                    }
                }
                else
                {
                    SAIN.Shoot(false, Vector3.zero);
                    SAIN.Steering.SteerByPriority();

                    if (enemy.LastKnownPosition != null)
                    {
                        SAIN.Mover.GoToPoint(enemy.LastKnownPosition.Value, out _);
                    }
                }
            }
        }

        private float _recalcPathTimer;

        private float WaitShootTimer;

        private bool FindSuppressionTarget(out Vector3? pos)
        {
            pos = SAIN.Enemy?.SuppressionTarget;
            return pos != null;
        }

        private bool CanSeeSuppressionTarget(Vector3? target)
        {
            if (target == null)
            {
                _canSeeSuppTarget = false;
            }
            else if (_nextCheckVisTime < Time.time)
            {
                _nextCheckVisTime = Time.time + 0.5f;
                Vector3 myHead = SAIN.Transform.Head;
                _canSeeSuppTarget = !Physics.Raycast(myHead, target.Value - myHead, (target.Value - myHead).magnitude * 0.8f);
            }
            return _canSeeSuppTarget;
        }

        private bool _canSeeSuppTarget;

        private float _nextCheckVisTime;

        public override void Start()
        {
        }

        public override void Stop()
        {
        }
    }
}