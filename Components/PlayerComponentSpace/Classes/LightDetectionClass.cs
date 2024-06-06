using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class LightDetectionClass : PlayerComponentBase
    {
        public List<FlashLightPoint> LightPoints { get; private set; } = new List<FlashLightPoint>();

        public LightDetectionClass(PlayerComponent component) : base(component)
        {
        }

        public void CreateDetectionPoints(bool visibleLight, bool onlyLaser)
        {
            if (PlayerComponent.IsAI)
            {
                return;
            }

            Vector3 sourcePosition = Player.WeaponRoot.position;
            Vector3 playerLookDirection = LookDirection;

            float detectionDistance = 75f;

            Vector3 lightDirection = getLightPointToCheck(onlyLaser);

            if (Physics.Raycast(sourcePosition, lightDirection, out RaycastHit hit, detectionDistance, LayerMaskClass.HighPolyWithTerrainMask))
            {
                LightPoints.Add(new FlashLightPoint(hit.point));

                if (SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.DebugFlash)
                {
                    if (visibleLight)
                    {
                        DebugGizmos.Sphere(hit.point, 0.1f, Color.red, true, 0.25f);
                    }
                    else
                    {
                        DebugGizmos.Sphere(hit.point, 0.1f, Color.blue, true, 0.25f);
                    }
                }
            }
        }

        private Vector3 getLightPointToCheck(bool onlyLaser)
        {
            if (_nextUpdatebeamtime < Time.time)
            {
                _nextUpdatebeamtime = Time.time + 0.5f;
                _LightBeamPoints.Clear();
                if (!onlyLaser)
                {
                    createFlashlightBeam(_LightBeamPoints);
                }
                else
                {
                    _LightBeamPoints.Add(LookDirection);
                }
            }
            return _LightBeamPoints.GetRandomItem();
        }

        private void createFlashlightBeam(List<Vector3> beamPoints)
        {
            // Define the cone angle (in degrees)
            float coneAngle = 10f;

            beamPoints.Clear();
            Vector3 lookDir = LookDirection;
            for (int i = 0; i < 10; i++)
            {
                // Generate random angles within the cone range for yaw and pitch
                float randomYawAngle = Random.Range(-coneAngle * 0.5f, coneAngle * 0.5f);
                float randomPitchAngle = Random.Range(-coneAngle * 0.5f, coneAngle * 0.5f);

                // AddColor a Quaternion rotation based on the random yaw and pitch angles
                Quaternion randomRotation = Quaternion.Euler(randomPitchAngle, randomYawAngle, 0);

                // Rotate the player's look direction by the Quaternion rotation
                Vector3 randomBeamDirection = randomRotation * lookDir;

                beamPoints.Add(randomBeamDirection);
            }
        }

        public void DetectAndInvestigateFlashlight()
        {
            if (!PlayerComponent.IsAI)
            {
                return;
            }
            if (_searchTime > Time.time)
            {
                return;
            }

            BotOwner bot = PlayerComponent.BotOwner;
            if (bot == null)
            {
                return;
            }

            bool usingNVGs = bot.NightVision?.UsingNow == true;
            Vector3 botPos = bot.MyHead.position;

            var enemies = PlayerComponent.BotComponent?.EnemyController.Enemies.Values;
            if (enemies == null)
            {
                return;
            }

            foreach (var enemy in enemies)
            {
                if (enemy?.IsValid == true &&
                    !enemy.IsAI &&
                    enemy.NextCheckFlashLightTime < Time.time)
                {
                    FlashLightClass flashLight = enemy.EnemyPlayerComponent.Flashlight;
                    // If this isn't visible light, and the bot doesn't have night vision, ignore it
                    if (!flashLight.WhiteLight &&
                        !flashLight.Laser &&
                        !usingNVGs)
                    {
                        return;
                    }

                    List<FlashLightPoint> lightPoints = flashLight.LightDetection.LightPoints;
                    if (lightPoints.Count <= 0)
                    {
                        return;
                    }

                    enemy.NextCheckFlashLightTime = Time.time + 0.1f;
                    FlashLightPoint lightPoint = lightPoints.PickRandom();

                    if ((botPos - lightPoint.Point).sqrMagnitude < 100f * 100f &&
                        canISeeLight(bot, lightPoint.Point))
                    {
                        Vector3 estimatedPosition = estimatePosition(enemy.EnemyPosition, lightPoint.Point, botPos, 10f);
                        tryToInvestigate(estimatedPosition);
                        _searchTime = Time.time + 1f;
                    }
                }
            }
        }

        private bool canISeeLight(BotOwner bot, Vector3 flashPos)
        {
            if (bot.LookSensor.IsPointInVisibleSector(flashPos))
            {
                Vector3 botPos = bot.LookSensor._headPoint;
                Vector3 direction = (flashPos - botPos).normalized;
                float rayLength = (flashPos - botPos).magnitude - 0.1f;

                return !Physics.Raycast(botPos, direction, rayLength, LayerMaskClass.HighPolyWithTerrainMask);
            }
            return false;
        }

        private void tryToInvestigate(Vector3 estimatedPosition)
        {
            var botComponent = PlayerComponent.BotComponent;
            if (botComponent != null)
            {
                botComponent.Squad.SquadInfo.AddPointToSearch
                    (estimatedPosition,
                    25f,
                    botComponent,
                    AISoundType.step,
                    Singleton<GameWorld>.Instance.MainPlayer,
                    SAIN.BotController.Classes.Squad.ESearchPointType.Flashlight);
            }
            else
            {
                PlayerComponent.BotOwner?.BotsGroup.AddPointToSearch(estimatedPosition, 20f, PlayerComponent.BotOwner, true, false);
            }
        }

        private Vector3 estimatePosition(Vector3 playerPos, Vector3 flashPos, Vector3 botPos, float dispersion)
        {
            Vector3 estimatedPosition = Vector3.Lerp(playerPos, flashPos, Random.Range(0.0f, 0.25f));

            float distance = (playerPos - botPos).magnitude;

            float maxDispersion = Mathf.Clamp(distance, 0f, 20f);

            float positionDispersion = maxDispersion / dispersion;

            float x = EFTMath.Random(-positionDispersion, positionDispersion);
            float z = EFTMath.Random(-positionDispersion, positionDispersion);

            return new Vector3(estimatedPosition.x + x, estimatedPosition.y, estimatedPosition.z + z);
        }

        private float _searchTime;
        private float _nextUpdatebeamtime;
        private readonly List<Vector3> _LightBeamPoints = new List<Vector3>();
    }
}