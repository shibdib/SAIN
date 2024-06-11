using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINMedical : SAINBase, ISAINClass
    {
        public EHitReaction HitReaction { get; private set; }

        public SAINMedical(BotComponent sain) : base(sain)
        {
            Surgery = new BotSurgery(sain);
        }

        public BotSurgery Surgery { get; private set; }

        public void TryCancelHeal()
        {
            if (_nextCancelTime < Time.time)
            {
                _nextCancelTime = Time.time + _cancelFreq;
                BotOwner.Medecine?.FirstAid?.CancelCurrent();
            }
        }

        private float _nextCancelTime;
        private float _cancelFreq = 1f;

        public void Init()
        {
            Player.BeingHitAction += GetHit;
            Surgery.Init();
        }

        public void Update()
        {
            Surgery.Update();
        }

        public void Dispose()
        {
            if (Player != null)
            {
                Player.BeingHitAction -= GetHit;
            }
            Surgery.Dispose();
        }

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
        }
    }
}