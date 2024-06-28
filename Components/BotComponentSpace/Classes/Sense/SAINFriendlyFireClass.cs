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
            Bot.CoroutineManager.Add(friendlyFireLoop(), nameof(friendlyFireLoop));
        }

        public void Update()
        {
            if (FriendlyFireStatus == FriendlyFireStatus.FriendlyBlock)
            {
                StopShooting();
            }
        }

        private IEnumerator friendlyFireLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(FRIENDLYFIRE_FREQUENCY);
            while (true)
            {
                if (Bot.EnemyController.AtPeace)
                {
                    yield return null;
                    continue;
                }
                CheckFriendlyFire();
                yield return wait;
            }
        }

        public void Dispose()
        {
        }

        public bool CheckFriendlyFire(Vector3? target = null)
        {
            FriendlyFireStatus = checkFriendlyFire(target);
            return FriendlyFireStatus != FriendlyFireStatus.FriendlyBlock;
        }

        private FriendlyFireStatus checkFriendlyFire(Vector3? target = null)
        {
            var members = Bot.Squad?.Members;
            if (members == null || members.Count <= 1)
            {
                return FriendlyFireStatus.None;
            }

            if (target != null)
            {
                return checkFriendlyFire(target.Value);
            }

            var aimData = BotOwner?.AimingData;
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
            float distance = (target - firePort).magnitude + 1;
            float sphereCastRadius = 0.2f;
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
        private const float FRIENDLYFIRE_FPS = 30f;
    }
}