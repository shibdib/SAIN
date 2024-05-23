using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Enemy;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class BlindFireController : SAINBase, ISAINClass
    {
        public BlindFireController(Bot sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (!Bot.PatrolDataPaused)
            {
                ResetBlindFire();
                return;
            }

            SAINEnemy enemy = Bot.Enemy;
            if (enemy == null || !enemy.Seen || enemy.TimeSinceSeen > 10f || !BotOwner.WeaponManager.IsReady || !BotOwner.WeaponManager.HaveBullets || Bot.Player.IsSprintEnabled || Bot.Cover.CoverInUse == null)
            {
                ResetBlindFire();
                return;
            }
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAI
                && enemy.IsAI
                && Bot.CurrentAILimit != AILimitSetting.Close)
            {
                ResetBlindFire();
                return;
            }

            if (CurrentBlindFireSetting == 0)
            {
                WeaponPosOffset = BotOwner.WeaponRoot.position - Bot.Transform.Position;
            }

            Vector3 targetPos;
            if (enemy.IsVisible)
            {
                targetPos = enemy.EnemyChestPosition;
            }
            else if (enemy.KnownPlaces.LastKnownPlace != null)
            {
                targetPos = enemy.KnownPlaces.LastKnownPlace.Position + Vector3.up;
            }
            else
            {
                ResetBlindFire();
                BlindFireTimer = Time.time + 0.5f;
                return;
            }

            int blindfire = CheckOverHeadBlindFire(targetPos);

            if (blindfire == 0)
            {
                blindfire = CheckSideBlindFire(targetPos);
            }

            if (blindfire == 0)
            {
                ResetBlindFire();
                BlindFireTimer = Time.time + 0.5f;
                Bot.Shoot(false, Vector3.zero);
            }
            else
            {
                if (BlindFireTimer < Time.time)
                {
                    BlindFireTimer = Time.time + 1f;
                    SetBlindFire(blindfire);
                }

                Vector3 start = Bot.Position;
                Vector3 blindFireDirection = Vector.Rotate(targetPos - start, Vector.RandomRange(3), Vector.RandomRange(3), Vector.RandomRange(3));
                BlindFireTargetPos = blindFireDirection + start;
                //SAIN.Steering.LookToPoint(BlindFireTargetPos);
                Bot.Shoot(true, BlindFireTargetPos, false, Bot.EShootReason.Blindfire);
            }
        }

        public void Dispose()
        {
        }

        public void ResetBlindFire()
        {
            if (CurrentBlindFireSetting != 0)
            {
                Player.MovementContext.SetBlindFire(0);
            }
        }

        private Vector3 WeaponPosOffset;

        private Vector3 BlindFireTargetPos;

        public bool BlindFireActive => CurrentBlindFireSetting != 0;

        public int CurrentBlindFireSetting => Player.MovementContext.BlindFire;

        private float BlindFireTimer = 0f;

        private int CheckOverHeadBlindFire(Vector3 targetPos)
        {
            int blindfire = 0;
            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            Vector3 rayShoot = WeaponPosOffset + Bot.Transform.Position;
            Vector3 direction = targetPos - rayShoot;
            if (Physics.Raycast(rayShoot, direction, direction.magnitude, mask))
            {
                rayShoot = Bot.Transform.HeadPosition + Vector3.up * 0.15f;
                if (!Vector.Raycast(rayShoot, targetPos, mask))
                {
                    blindfire = 1;
                }
            }
            return blindfire;
        }

        private int CheckSideBlindFire(Vector3 targetPos)
        {
            int blindfire = 0;
            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            Vector3 rayShoot = WeaponPosOffset + Bot.Transform.Position;
            Vector3 direction = targetPos - rayShoot;
            if (Physics.Raycast(rayShoot, direction, direction.magnitude, mask))
            {
                Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
                Vector3 SideShoot = rotation * direction.normalized * 0.2f;
                rayShoot += SideShoot;
                direction = targetPos - rayShoot;
                if (!Physics.Raycast(rayShoot, direction, direction.magnitude, mask))
                {
                    blindfire = -1;
                }
            }
            return blindfire;
        }

        public void SetBlindFire(int value)
        {
            if (CurrentBlindFireSetting != value)
            {
                Player.MovementContext.SetBlindFire(value);
            }
        }
    }
}