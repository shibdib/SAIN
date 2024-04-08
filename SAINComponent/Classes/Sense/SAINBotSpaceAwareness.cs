using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public enum SAINBodyPart
    {
        Head,
        WeaponRoot,
        LeftShoulder,
        RightShoulder,
        LeftLeg,
        RightLeg,
        Center
    }

    public enum SAINPose
    {
        None,
        Stand,
        StandLeanLeft,
        StandLeanRight,
        Crouch,
        CrouchLeanLeft,
        CrouchLeanRight,
        Prone,
        ProneLeanLeft,
        ProneLeanRight,
    }

    public class SAINBotSpaceAwareness
    {
        public sealed class BoneOffsetClass
        {
            public Dictionary<SAINBodyPart, Vector3> Offsets = new Dictionary<SAINBodyPart, Vector3>();

            public bool Get(SAINBodyPart part, out Vector3 result)
            {
                if (Offsets.ContainsKey(part) && Offsets[part] != Vector3.zero)
                {
                    result = Offsets[part];
                    return true;
                }
                result = Vector3.zero;
                return false;
            }

            public bool Set(SAINBodyPart part, Vector3 offset)
            {
                if (Offsets.ContainsKey(part) && Offsets[part] == Vector3.zero)
                {
                    Offsets[part] = offset;
                    return true;
                }
                return false;
            }

            public void Add(SAINBodyPart part, Vector3 offset)
            {
                if (!Offsets.ContainsKey(part))
                {
                    Offsets.Add(part, offset);
                }
            }
        }

        private static Dictionary<SAINPose, BoneOffsetClass> PlayerBoneOffsets = new Dictionary<SAINPose, BoneOffsetClass>();

        public static void Update()
        {
            if (MainPlayer != null)
            {
                foreach (SAINBodyPart part in EnumValues.GetEnum<SAINBodyPart>())
                {
                    SAINPose pose = FindSAINPose(MainPlayer);
                    Vector3 offset = GetOffset(part, pose);

                    if (offset != Vector3.zero)
                    {
                        Logger.LogWarning($"{part} : {pose} : {offset.magnitude}");
                    }
                }
            }
        }

        private static Vector3 GetHeadOffset(SAINPose pose)
        {
            BoneOffsetClass offsetClass = PlayerBoneOffsets[pose];

            if (!offsetClass.Get(SAINBodyPart.Head, out Vector3 result))
            {
                Vector3 headPosition = MainPlayer.MainParts[BodyPartType.head].Position;
                Vector3 floorPosition = MainPlayer.Position;
                result = headPosition - floorPosition;

                offsetClass.Set(SAINBodyPart.Head, result);
            }
            return result;
        }

        private static Vector3 GetWeaponRootOffset(SAINPose pose)
        {
            BoneOffsetClass offsetClass = PlayerBoneOffsets[pose];

            if (!offsetClass.Get(SAINBodyPart.WeaponRoot, out Vector3 result))
            {
                Vector3 rootPos = MainPlayer.WeaponRoot.position;
                Vector3 floorPosition = MainPlayer.Position;
                result = rootPos - floorPosition;

                offsetClass.Set(SAINBodyPart.WeaponRoot, result);
            }

            return result;
        }

        private static bool CheckAddOffset(SAINPose pose, SAINBodyPart part, out Vector3 result)
        {
            if (!PlayerBoneOffsets.ContainsKey(pose))
            {
                PlayerBoneOffsets.Add(pose, new BoneOffsetClass());
            }

            BoneOffsetClass boneOffset = PlayerBoneOffsets[pose];

            if (!boneOffset.Offsets.ContainsKey(part))
            {
                boneOffset.Offsets.Add(part, Vector3.zero);
            }

            boneOffset.Get(part, out result);

            return result == Vector3.zero;

        }

        private static SAINPose FindSAINPose(Player player)
        {
            SAINPose result = SAINPose.None;
            if (player != null)
            {
                LeanSetting lean = CheckIfLeaning(player);

                switch (player.Pose)
                {
                    case EPlayerPose.Stand:
                        switch (lean)
                        {
                            case LeanSetting.None:
                                result = SAINPose.Stand;
                                break;
                            case LeanSetting.Right:
                                result = SAINPose.StandLeanRight;
                                break;
                            case LeanSetting.Left:
                                result = SAINPose.StandLeanLeft;
                                break;
                        }
                        break;

                    case EPlayerPose.Duck:
                        switch (lean)
                        {
                            case LeanSetting.None:
                                result = SAINPose.Crouch;
                                break;
                            case LeanSetting.Right:
                                result = SAINPose.CrouchLeanRight;
                                break;
                            case LeanSetting.Left:
                                result = SAINPose.CrouchLeanLeft;
                                break;
                        }
                        break;

                    case EPlayerPose.Prone:
                        switch (lean)
                        {
                            case LeanSetting.None:
                                result = SAINPose.Prone;
                                break;
                            case LeanSetting.Right:
                                result = SAINPose.ProneLeanRight;
                                break;
                            case LeanSetting.Left:
                                result = SAINPose.ProneLeanLeft;
                                break;
                        }
                        break;
                }
            }
            return result;
        }

        private static LeanSetting CheckIfLeaning(Player player)
        {
            if (player != null)
            {
                float leanNum = player.MovementContext.Tilt;
                if (leanNum >= 5)
                {
                    return LeanSetting.Right;
                }
                else if (leanNum <= -5)
                {
                    return LeanSetting.Left;
                }
            }
            return LeanSetting.None;
        }

        private static Vector3 GetOffset(SAINBodyPart bodyPart, SAINPose pose)
        {
            if (!CheckAddOffset(pose, bodyPart, out Vector3 result))
            {
                return result;
            }

            switch (bodyPart)
            {
                case SAINBodyPart.Head:
                    result = GetHeadOffset(pose);
                    break;

                case SAINBodyPart.WeaponRoot:
                    result = GetWeaponRootOffset(pose);
                    break;

                case SAINBodyPart.LeftShoulder:
                    break;

                case SAINBodyPart.RightShoulder:
                    break;

                case SAINBodyPart.LeftLeg:
                    break;

                case SAINBodyPart.RightLeg:
                    break;

                case SAINBodyPart.Center:
                    break;
            }

            return result;
        }

        private static Player MainPlayer => Singleton<GameWorld>.Instance?.MainPlayer;

        public static float CheckLineOfSightToMainPlayer(Player playerToTest)
        {
            return 0f;
        }

        public static float CheckLineOfSightToMainPlayer(CoverPoint coverPoint)
        {
            return 0f;
        }

        public static bool CheckPathSafety(NavMeshPath path, Vector3 enemyHeadPos, float ratio = 0.5f)
        {
            Vector3[] corners = path.corners;
            int max = corners.Length - 1;

            for (int i = 0; i < max - 1; i++)
            {
                Vector3 pointA = corners[i];
                Vector3 pointB = corners[i + 1];

                float ratioResult = RaycastAlongDirection(pointA, pointB, enemyHeadPos);

                if (ratioResult < ratio)
                {
                    return false;
                }
            }

            return true;
        }

        public static float GetSegmentLength(int segmentCount, Vector3 direction, float minLength, float maxLength, out float dirMagnitude, out int countResult, int maxIterations = 10)
        {
            dirMagnitude = direction.magnitude;
            countResult = 0;
            if (dirMagnitude < minLength)
            {
                return 0f;
            }

            float segmentLength = 0f;
            for (int i = 0; i < maxIterations; i++)
            {
                if (segmentCount > 0)
                {
                    segmentLength = dirMagnitude / segmentCount;
                }
                if (segmentLength > maxLength)
                {
                    segmentCount++;
                }
                if (segmentLength < minLength)
                {
                    segmentCount--;
                }
                if (segmentLength <= maxLength && segmentLength >= minLength)
                {
                    break;
                }
                if (segmentCount <= 0)
                {
                    break;
                }
            }
            countResult = segmentCount;
            return segmentLength;
        }

        private static float RaycastAlongDirection(Vector3 pointA, Vector3 pointB, Vector3 rayOrigin, int SegmentCount = 5)
        {
            const float RayHeight = 1.1f;
            const float debugExpireTime = 12f;
            const float MinSegLength = 1f;
            const float MaxSegLength = 5f;

            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            Vector3 direction = pointB - pointA;

            // Make sure we aren't raycasting too often, set to MinSegLength for each raycast along a path
            float segmentLength = GetSegmentLength(SegmentCount, direction, MinSegLength, MaxSegLength, out float dirMagnitude, out int testCount);

            if (segmentLength <= 0 || testCount <= 0)
            {
                return 1f;
            }

            Vector3 dirNormal = direction.normalized;
            Vector3 dirSegment = dirNormal * segmentLength;

            Vector3 testPoint = pointA + (Vector3.up * RayHeight);

            int i = 0;
            int hits = 0;

            for (i = 0; (i < testCount); i++)
            {
                testPoint += dirSegment;

                Vector3 enemyDir = testPoint - rayOrigin;
                float rayLength = enemyDir.magnitude;

                Color debugColor = Color.red;
                if (Physics.Raycast(rayOrigin, enemyDir, rayLength, mask))
                {
                    debugColor = Color.white;
                    hits++;
                }

                if (SAINPlugin.EditorDefaults.DebugDrawSafePaths)
                {
                    DebugGizmos.Line(rayOrigin, testPoint, debugColor, 0.025f, true, debugExpireTime, true);
                    DebugGizmos.Sphere(testPoint, 0.05f, Color.green, true, debugExpireTime);
                    //DebugGizmos.Sphere(rayOrigin, 0.1f, Color.red, true, debugExpireTime);
                }
            }

            float result = hits / i;
            return result;
        }
    }
}
