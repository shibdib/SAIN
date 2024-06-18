using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.WeaponFunction;
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
            var enemy = Bot.Enemy;

            if (!BotOwner.WeaponManager.HaveBullets 
                || (!BotOwner.ShootData.Shooting && Bot.Decision.SelfActionDecisions.LowOnAmmo(0.5f)))
            {
                Bot.SelfActions.TryReload();
            }

            if (enemy != null)
            {
                if (enemy.IsVisible && enemy.CanShoot)
                {
                    Bot.Mover.StopMove();
                    Shoot.Update();
                }
                else if (FindSuppressionTarget(out var target) && CanSeeSuppressionTarget(target))
                {
                    Bot.Mover.StopMove();

                    bool hasMachineGun = Bot.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun;
                    if (hasMachineGun 
                        && Bot.Mover.Prone.ShallProne(true))
                    {
                        Bot.Mover.Prone.SetProne(true);
                    }

                    //SAIN.Steering.LookToPoint(pos.Value);

                    if (!BotOwner.WeaponManager.HaveBullets)
                    {
                        Bot.SelfActions.TryReload();
                    }
                    else if (
                        WaitShootTimer < Time.time 
                        && Bot.ManualShoot.Shoot(true, target.Value, true, EShootReason.SquadSuppressing))
                    {
                        enemy.EnemyStatus.EnemyIsSuppressed = true;
                        float waitTime = hasMachineGun ? 0.1f : 0.5f;
                        WaitShootTimer = Time.time + (waitTime * Random.Range(0.75f, 1.25f));
                    }
                }
                else
                {
                    Bot.ManualShoot.Shoot(false, Vector3.zero);
                    Bot.Steering.SteerByPriority();

                    if (enemy.LastKnownPosition != null)
                    {
                        Bot.Mover.GoToPoint(enemy.LastKnownPosition.Value, out _);
                    }
                }
            }
        }

        private float WaitShootTimer;

        private bool FindSuppressionTarget(out Vector3? pos)
        {
            pos = Bot.Enemy?.SuppressionTarget;
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
                Vector3 myHead = Bot.Transform.HeadPosition;
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