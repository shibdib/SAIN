using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class ManualShootClass : SAINBase
    {
        public ManualShootClass(BotComponent bot) : base(bot) { }

        public void Update()
        {
            if (Reason != EShootReason.None && 
                (!BotOwner.WeaponManager.HaveBullets || _timeStartManualShoot + 1f < Time.time || !BotOwner.ShootData.Shooting))
            {
                Shoot(false, Vector3.zero);
            }
        }

        public bool Shoot(bool value, Vector3 targetPos, bool checkFF = true, EShootReason reason = EShootReason.None)
        {
            ShootPosition = targetPos;
            Reason = value ? reason : EShootReason.None;

            if (value)
            {
                if (checkFF && !Bot.FriendlyFireClass.ClearShot)
                {
                    Reason = EShootReason.None;
                    BotOwner.ShootData.EndShoot();
                    return false;
                }
                if (BotOwner.ShootData.Shooting)
                {
                    return true;
                }
                if (BotOwner.ShootData.Shoot())
                {
                    _timeStartManualShoot = Time.time;
                    Reason = reason;
                    return true;
                }
            }
            BotOwner.ShootData.EndShoot();
            Reason = EShootReason.None;
            return false;
        }

        private float _timeStartManualShoot;

        public Vector3 ShootPosition { get; private set; }

        public EShootReason Reason { get; private set; }

    }
}
