using EFT;
using SAIN.Components;
using System.Collections;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINFriendlyFireClass : SAINBase, ISAINClass
    {
        public bool ClearShot => FriendlyFireStatus != FriendlyFireStatus.FriendlyBlock;
        public FriendlyFireStatus FriendlyFireStatus { get; private set; }

        public SAINFriendlyFireClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            Bot.OnBotDisabled += endLoop;
            Bot.Decision.OnSAINStatusChanged += toggleLoop;
        }

        public void Update()
        {
            if (FriendlyFireStatus == FriendlyFireStatus.FriendlyBlock)
            {
                StopShooting();
            }
        }

        private void endLoop()
        {
            toggleLoop(false);
        }

        private void toggleLoop(bool value)
        {
            if (!value && _friendlyFireCoroutine != null)
            {
                Bot.StopCoroutine(_friendlyFireCoroutine);
                _friendlyFireCoroutine = null;
            }
            if (value && _friendlyFireCoroutine == null)
            {
                _friendlyFireCoroutine = Bot.StartCoroutine(friendlyFireLoop());
            }

        }

        private IEnumerator friendlyFireLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(FRIENDLYFIRE_FREQUENCY);
            while (true)
            {
                FriendlyFireStatus = CheckFriendlyFire();
                yield return wait;
            }
        }

        public void Dispose()
        {
            Bot.OnBotDisabled -= endLoop;
        }

        public FriendlyFireStatus CheckFriendlyFire()
        {
            if (Bot.Squad.Members.Count <= 1)
            {
                return FriendlyFireStatus.None;
            }

            var aimData = BotOwner.AimingData;
            if (aimData == null)
            {
                return FriendlyFireStatus.None;
            }

            FriendlyFireStatus friendlyFire = checkFriendlyFire(aimData.RealTargetPoint);
            if (friendlyFire != FriendlyFireStatus.FriendlyBlock)
            {
                friendlyFire = checkFriendlyFire(aimData.EndTargetPoint);
            }

            return friendlyFire;
        }

        private FriendlyFireStatus checkFriendlyFire(Vector3 target)
        {
            Collider hitCollider = sphereCast(target);
            if (hitCollider == null)
            {
                return FriendlyFireStatus.None;
            }

            if (!isColliderEnemy(hitCollider))
            {
                return FriendlyFireStatus.FriendlyBlock;
            }
            return FriendlyFireStatus.Clear;
        }

        private Collider sphereCast(Vector3 target)
        {
            Vector3 firePort = Bot.Transform.WeaponFirePort;
            float distance = (target - firePort).magnitude;
            float sphereCastRadius = BotOwner.ShootData.Shooting ? 0.2f : 0.1f;
            Physics.SphereCast(firePort, sphereCastRadius, Bot.Transform.WeaponPointDirection, out var hit, distance, LayerMaskClass.PlayerMask);
            return hit.collider;
        }

        private bool isColliderEnemy(Collider collider)
        {
            if (collider == null)
                return false;

            Player player = GameWorldComponent.Instance.GameWorld.GetPlayerByCollider(collider);
            return player != null && Bot.EnemyController.IsPlayerAnEnemy(player.ProfileId);
        }

        public void StopShooting()
        {
            BotOwner.ShootData?.EndShoot();
        }

        private const float FRIENDLYFIRE_FREQUENCY = 1f / FRIENDLYFIRE_FPS;
        private const float FRIENDLYFIRE_FPS = 20f;
        private Coroutine _friendlyFireCoroutine;
    }
}