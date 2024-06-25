using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class BotSound
    {
        public BotSound(PlayerComponent playerComponent, Vector3 originalPos, float power, float volume, SAINSoundType soundType, Enemy enemy)
        {
            PlayerComponent = playerComponent;
            IsAI = playerComponent.IsAI;
            OriginalPosition = originalPos;
            Power = power;
            Volume = volume;
            SoundType = soundType;
            Enemy = enemy;
        }

        public readonly bool IsAI;
        public readonly PlayerComponent PlayerComponent;
        public readonly Vector3 OriginalPosition;
        public readonly SAINSoundType SoundType;
        public readonly float Power;
        public readonly float Volume;
        public readonly Enemy Enemy;

        public Vector3 ProjectionPoint;
        public float ProjectionPointDistance;
        public Vector3 RandomizedPosition;

        public float SqrDistance;
        public float Distance;

        public float BaseRange;
        public float Range;
        public float OccludedRange => Range * OcclusionModifier;

        public float OcclusionModifier = 1f;
        public float BunkerReduction = 1f;
        public float RangeModifier = 1f;
        public float Dispersion = 0f;
        public float ChanceToHear = 100f;

        public bool IsGunShot;
        public bool OutOfRange;
        public bool VisibleSource;
        public bool WasHeard;
        public bool BulletFelt;
    }
}