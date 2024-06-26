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

        public readonly SoundInfoData Info;

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

    public struct BotSoundStruct
    {
        public SoundInfoData Info;
        public SoundResultsData Results;
        public BulletData? Bullet;
        public SoundRangeData Range;
        public SoundDispersionData Dispersion;
    }

    public struct SoundInfoData
    {
        public bool IsAI;
        public PlayerComponent PlayerComponent;
        public Vector3 OriginalPosition;
        public SAINSoundType SoundType;
        public float Power;
        public float Volume;
        public Enemy Enemy;
    }

    public struct SoundDispersionData
    {
        public Vector3 EstimatedPosition;
        public float DistanceDispersion;
        public float AngleDispersionX;
        public float AngleDispersionY;
        public float DispersionModifier;
        public ESoundDispersionType DispersionType;
    }

    public struct SoundRangeData
    {
        public float FinalRange;
        public float BaseRange;
        public SoundRangeModifiers Modifiers;
    }

    public struct SoundRangeModifiers
    {
        public float FinalModifier;
        public float EnvironmentModifier;
        public float ConditionModifier;
        public float OcclusionModifier;
    }

    public struct SoundResultsData
    {
        public bool Heard;
        public bool VisibleSource;
    }

    public struct BulletData
    {
        public bool BulletFelt;
        public bool BulletFiredAtMe;
        public Vector3 ProjectionPoint;
        public float ProjectionPointDistance;
        public bool Suppressed;
    }
}